# ADR-0007: World persistence — seed + event-sourced deltas

**Status:** **Accepted** · July 2026 (dev sign-off: "b it is") · *The load-bearing decision in
the entity architecture. Full model in [entities-and-worldgen.md](../entities-and-worldgen.md).*
Chosen so the world stays self-sustaining across 10-20+ tiers — players retiring, newcomers
entering — with bounded save growth.

## Decision

Persist the ~6,500-character living world as **(world seed) + (append-only delta log)**, not as
6,500 fully materialized records. On load: deterministically regenerate the baseline world from
the seed, then fold the delta log to reach current state.

## Context

The world is large (~6,500 characters now, more later), persistent across a decades-long career,
and mostly *inert* at any given moment — the player touches ~30 raiders closely; the other ~6,470
change slowly and only some are ever inspected. We need small saves, reproducibility, and a model
that scales without bloating.

## Options considered

**A — Materialize everything.** Store all 6,500 full component records in the save.
- Pro: dead simple access; no regeneration; generator can change freely afterward.
- Con: large, ever-growing saves; every inert unit serialized in full; save diffs are noise;
  world not reproducible from a bug report.

**B — Seed + deltas (chosen).** Store the seed + only what diverged from the deterministic
baseline (transfers, training gains, retirements, contracts, injuries, aging).
- Pro: tiny, diffable saves; world reproducible from seed (a bug report + seed = the exact
  world); **same event-sourcing pattern already chosen for combat→career**, so one idea covers
  the whole codebase; naturally tiers fidelity (player's guild = many deltas/high fidelity;
  background = near-baseline/cheap) — which also answers the AI-world fidelity question
  (game-design §10) with the *same* mechanism.
- Con: load does regeneration + fold (bounded, cacheable); **the generator becomes load-bearing
  forever** — its determinism is now a permanent contract, like the engine's.

## Why B

The event-sourcing pattern is already committed for combat and career history. Choosing B makes
persistence *one* consistent idea end-to-end instead of two, and it collapses three separate
requirements — small saves, a cheap 6,500-unit world, and full-sim-only-where-it-matters — into a
single mechanism. That consolidation is exactly the "solid foundation, no redo" mandate.

## The pinning rule (the cost, made explicit)

Because the baseline is regenerated from the generator, **the world generator is versioned and
pinned in the save**, exactly like `engineVersion`/`eventSchemaVersion`. Consequences:

- Changing/improving the generator (e.g. adding attribute #11, retuning archetypes) would shift
  the baseline for *untouched* units. That is sometimes desirable (a richer world) and sometimes
  not (don't silently rewrite an established career).
- **Migration policy:** a save pins its generator version. On a generator change, the migration
  either (a) re-runs the new generator, accepting baseline shifts for units with no deltas, or
  (b) **materializes-and-freezes** affected units at migration time (converts baseline → explicit
  deltas so they never move again). Default: materialize-and-freeze anything the player has
  interacted with; re-generate the untouched deep background. CI round-trips a vN world through
  the vN+1 migration and asserts no career-delta loss.

## Churn: newcomers and departures (important — clarified)

The seed regenerates only the **original** baseline population. Characters born *after* world
creation (youth intake, world newcomers) are not derivable from the seed, so they are stored in
full as `create` deltas in the log (with runtime-counter IDs). This means:

- **Creating a newcomer is materialized in both A and B** — it is genuinely new data. B's
  "regenerate from seed" advantage applies to the static baseline, *not* to newcomers.
- **B's real churn advantage is bounded save growth over a long career.** In A, everyone who
  ever mattered stays a full dossier forever → the save grows without bound across decades of
  retirements/transfers/intakes. In B, a departed baseline unit is a tiny `retired@season` delta
  on a still-regenerable person; untouched background costs nothing; newcomers are compact
  create-deltas. Compact deltas beat full mutable dossiers-with-history, so B's save grows far
  slower.
- **Uniformity:** every world mutation — transfer, training gain, retirement, intake, injury,
  aging — is the same shape (an append to one event log). A needs in-place mutation + appends +
  tombstones (three mechanisms). One mechanism is what stays maintainable at scale.
- **Honest ceiling:** a world with enormous churn stores many create-deltas; B is smaller than A
  but not free the way the inert baseline is. B pays in proportion to what actually happens —
  which is the correct thing to pay for.

## Compaction & history retention (bounds the one cost of B)

B's only real cost is unbounded delta-log growth over a decades-long career. **Season-boundary
compaction** bounds it, and it is the safe implementation of the dev's "delete everything older
than last season, keep a history of events" — you cannot naively drop deltas (current state *is*
their fold), but you can snapshot then prune. Three retention tiers:

1. **Live deltas** — changes since the last snapshot (the current + previous season). Full
   fidelity, fully replayable.
2. **Season snapshots** — at each season boundary, fold all deltas into a fresh materialized
   baseline (a new "season-N snapshot"), then **discard the raw deltas before it**. Current state
   = latest snapshot + live deltas. *The original world seed is simply the season-0 snapshot* —
   so this is not a new model, just the same one advancing its baseline. This bounds the log to
   ~one season of detail regardless of career length.
3. **The Chronicle** — a permanent, compact, append-only record kept forever (it is tiny):
   season winners, final top-100 leaderboards, world-first kills, notable retirements, title
   holders. This is the world's *memory* — what makes a 20-tier-old world feel like it has a
   past — and is deliberately separate from operational deltas so pruning never touches it.

Net: detailed, replayable state exists only for the recent window; the deep past survives as
Chronicle summaries; save size is bounded by *one season of activity + the Chronicle*, not by
total career length. This makes B strictly better than A over a long career, not just cheaper.

## Consequences

- All cross-references are by `RaiderId`, never by object reference (required for regeneration).
- A load-time cache materializes hot units (player's guild, inspected characters); the deep
  background stays lazy.
- `src/Sim` can spin up any world from a seed for balance/soak runs at zero storage cost.

## If B proves painful

Fallback is A (materialize), reachable without redesign: run the generator once at new-game,
write all units out, drop the regeneration path. The entity/component model and all downstream
code are identical — only the save assembly changes. So B is not a one-way door.
