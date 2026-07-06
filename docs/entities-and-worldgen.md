# Entities & World Generation

> How every character in the world is modeled, generated, and persisted. This is foundational —
> the roster, trade market, living world, careers, and saves all sit on it. Companion to
> [engine-spec.md](engine-spec.md) (combatants), [save-format.md](save-format.md) (persistence),
> and [game-design.md](game-design.md) (what a raider *is* to the player).
> Persistence model decided in [ADR-0007](adr/0007-world-persistence-seed-plus-deltas.md).
> **Language: C# (net8.0)** — lives in `src/Game` (+ template types in `src/Content`). TS-style
> snippets below are design pseudocode to port to C# records ([ADR-0008](adr/0008-engine-godot-csharp.md)).
> Character *templates* authorable in the Godot editor are `Resource` subclasses in `Content`,
> projected to plain C# records for the headless `Engine`/`Sim`.

## 1. The problem, precisely

At world creation we need **~6,500 characters** (~200 guilds × ~30 + ~400 free agents; scalable
higher) that are:

- **Unique & coherent** — a "veteran world-first anchor tank" must have a *consistent* bundle:
  older age, high discipline attributes, high tank stars, a matching career history, placed in a
  guild whose prestige fits. Not 40 independent random rolls that occasionally contradict.
- **Deterministic** — same world seed ⇒ the same 6,500 characters, byte-identical, on every
  machine. World-gen is part of the reproducibility contract, testable in CI.
- **Extensible without rework** — adding attribute #11, a 13th class, or a whole new component
  (relationships, sponsorships) later must **not** require rewriting 6,500 records or hand-coding
  a bespoke migration each time. This is the "solid ground so we don't redo everything" mandate.
- **Persisted compactly & versioned** — a decades-long career must not bloat the save with 6,500
  full dossiers, and the format must migrate forward.
- **Consistent with the rest of the codebase** — same determinism, registry, and event-sourcing
  patterns used for combat/content/saves. One set of ideas, applied everywhere.

## 2. Entity model — composition, not a fat record

A character is **not** a 40-field class. It is a composition of independent, plain-serializable
**components**, each with its own lifecycle and version:

```
Raider {
  id:          RaiderId          // stable, world-unique, permanent
  identity:    Identity          // name, birth-season (age is derived), nationality, portraitSeed — immutable
  vocation:    Vocation          // classId + per-role { stars, hiddenPotential } proficiencies
  attributes:  AttributeVector   // Record<AttributeId, number> — KEYED OFF THE CONTENT REGISTRY
  personality: TraitSet          // trait ids (registry-validated)
  condition:   Condition         // morale, stamina, burnout — high mutation rate
  contract:    Contract | null   // salary, term, clauses
  career:      CareerLedger      // event-sourced history + folded deltas (append-only)
  membership:  GuildId | "free_agent"
}
```

Why components, concretely:

- **Extensibility is structural.** A new attribute is a new *key* in `AttributeVector`, defaulted
  by one migration rule — not a field added to 6,500 records. A new concern (relationships) is a
  new component, added without touching the others. This is the single most important property.
- **Attributes/traits are registry-keyed data, not hardcoded fields.** "Which attributes exist"
  lives in the `content` registry, exactly like abilities and encounters (see
  [content-authoring.md](content-authoring.md)). Code never says `raider.composure` — it reads
  `attributes[AttributeId.Composure]`, and the registry is the authority on what's valid. Adding
  or renaming an attribute is a content change + a migration default, full stop.
- **Lifecycle separation matches the save-aggregate split.** `identity` never changes,
  `condition` changes weekly, `career` only grows. Different mutation rates cleanly separated —
  the same persistent-vs-transient discipline the save format already enforces.
- **Derived facts are never stored.** Stars derive from attributes + class fit; market value
  derives from stars + age + career; overall derives from the above. One fact, one source — the
  content-pipeline rule applied to entities, so nothing can drift.

## 3. Generation — a deterministic, declarative pipeline

World-gen lives in `src/Game`, is fully seeded (shares the engine's `SeededRng`, no
`System.Random`), and is a **pipeline of pure, composable stages**, not imperative spaghetti:

```
worldSeed
 └─ RegionPlan            data: how many guilds per region & prestige tier
     └─ GuildPlan         per guild: roster composition needs + a quality budget from prestige tier
         └─ SlotFill      per roster slot:
             ├─ ArchetypePick        weighted by guild tier (data-driven, §3.1)
             ├─ LatentDraw           roll the archetype's latent factors (§3.2) — the coherence engine
             ├─ AttributeProjection  project latents → AttributeVector (registry-driven)
             ├─ DeriveVocation       stars = f(attributes, class fit); class weighted by tier & role need
             ├─ IdentityGen          name from region pool, birth-season coherent with archetype, portraitSeed
             └─ BackstorySeed        coherent past events consistent with age + guild history
     └─ FreeAgentPool     same pipeline, membership = free_agent
     └─ GuildHistorySeed  each guild's past placements made consistent with its rostered quality
```

### 3.1 Archetypes are data

An **archetype** is a `content` registry row: "veteran anchor tank", "rising prodigy",
"journeyman filler", "volatile carry", "burnout-risk star". Each defines *distributions and
correlations*, not fixed values, plus a prestige-tier weighting (elite guilds draw more
prodigies/veterans, local guilds draw more journeymen). Adding an archetype = one data row.
Rebalancing the world's talent spread = editing distributions, never code.

### 3.2 Latent-factor model — where coherence comes from

Coherence is **structural, not luck**. Instead of rolling 11 attributes independently (which
produces incoherent people), each archetype rolls a handful of **latent factors** — e.g.
`talent`, `discipline`, `experience`, `volatility` — and attributes are a **weighted projection**
of those latents plus small noise. So "disciplined veteran" ⇒ high `discipline` + high
`experience` ⇒ Composure, Preparation, Communication all rise *together*, twitch stats fall with
age. One coherent knob-set drives many correlated attributes.

This is cheaper, more human-authorable, and more robust than a full covariance matrix: designers
tune a small `latent → attribute` loading table (registry data), and coherence falls out. It also
means a *new* attribute just declares which latents it loads on — automatically coherent, no
per-archetype editing.

### 3.3 Invariants enforced by construction

- Stars, value, and overall are **functions**, computed on read — never generated and stored.
- Age is derived from `birth-season` vs the world clock — so aging (§8 of the GDD) is just the
  clock advancing, no per-unit age field to migrate.
- History is seeded as coherent *past events*, then the living world **writes forward** by
  appending real events (combat/transfer/award folds) — the same event-sourcing as combat.

## 4. Persistence — seed + deltas

**Decided in [ADR-0007](adr/0007-world-persistence-seed-plus-deltas.md).** In brief: the save
stores the **world seed** (which regenerates the deterministic baseline for all ~6,500 characters
on load) plus an **append-only delta log** of everything that diverged from baseline (transfers,
training gains, retirements, contract changes, injuries, aging effects). Load = regenerate
baseline + fold deltas.

- Saves stay tiny and diffable (a season of changes, not 6,500 dossiers).
- The world is reproducible for debugging (a bug report + seed = the exact world).
- It is the **same event-sourcing pattern** already chosen for combat→career, applied
  world-wide — one idea, not two.
- Cost & the pinning rule (generator version becomes load-bearing) are in the ADR.

## 5. Identity & IDs

- `RaiderId` is stable and permanent — referenced by guilds, contracts, career, and every event
  by **id, never by object reference** (mandatory for the seed+delta model to work).
- Baseline units get **deterministic ids** derived from the world seed + guild/slot index (so the
  baseline regenerates identically). Runtime-created units (youth intake, world newcomers) get
  ids from a persisted monotonic counter in the delta log. Hybrid scheme, no collisions.

## 6. Package boundaries

| Package | Owns |
|---|---|
| `content` | attribute registry, trait registry, **archetype registry**, latent→attribute loadings, class registry, name pools per region |
| `engine` | nothing here — but world-gen reuses its `SeededRng` primitive and determinism discipline |
| `game` | the world-gen pipeline, the entity/component model, persistence (seed + deltas), fold logic, derived-stat functions |
| `app` | renders characters; **never generates or derives** — reads folded state |
| `src/Sim` | generates a headless world and asserts distributions; world-gen is headless-testable like everything else |

## 7. One mechanism serves two open problems

The seed+delta model **is** the answer to the AI-world fidelity tiers question (game-design §10):

- The **player's guild** accumulates many deltas — fully materialized, high fidelity.
- **Background characters** carry few or no deltas until they do something noteworthy — cheap,
  mostly baseline, materialized on demand when inspected or when their guild is simulated at full
  fidelity.

So "small save", "cheap 6,500-character world", and "full sim only where it matters" are **one
architecture**, not three. That unification is the point of doing this properly now.

## 8. Testing

- **World-gen golden test:** seed 1 ⇒ a fixed world hash. Regeneration determinism across
  Sim/App/CI.
- **Distribution tests:** generated worlds match archetype targets — no accidental all-5-star
  world, star curve per prestige tier within tolerance, role coverage guaranteed per guild.
- **Coherence assertions:** e.g. no unit with top twitch attributes *and* max age; stars track
  attributes within the derivation's tolerance.
- **Migration round-trip:** a world saved under generator vN loads under vN+1 per the ADR's
  pinning rule (materialize-affected-units policy) without losing career deltas.

## 9. Open questions

- Final attribute list (feeds `AttributeVector` keys) — still the GDD's open item; the model is
  attribute-count-agnostic by design, so this is not blocking.
- Portrait system: layered procedural composition vs curated set + deterministic pick
  (`portraitSeed` supports either; decide when art direction is chosen).
- Name-pool sourcing per region and uniqueness policy (accept rare duplicates vs enforce unique).
- Relationship/rivalry component: in the entity model from the start, or an added component
  later? (Composition makes "later" cheap — but if it shapes world-gen coherence, earlier is
  better.)
