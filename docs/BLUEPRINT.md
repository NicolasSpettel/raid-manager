# Raid Manager — Architecture Blueprint

> Working title. Sequel to Dungeon Manager (DM1). This document is the spine: it states every
> architectural decision and links to the spec that details it. Decisions live in `adr/` and are
> immutable once accepted — revisiting one means writing a superseding ADR, not editing history.

**Status:** Draft v1 · July 2026 · No code exists yet, and that is deliberate.

> **Orientation:** new here? Read [/ARCHITECTURE.md](../ARCHITECTURE.md) first (the project wiki).
> WHAT we're building lives in [game-design.md](game-design.md); this document is HOW it's structured.

---

## 1. Vision & pillars

The fantasy: you are the guild master of a large raiding guild. Not five adventurers — a roster of
30–60 named raiders with careers, egos, and history, fielding 10–25 man raid teams against
mechanically real encounters.

Three pillars, in priority order:

1. **Raid-scale management.** Rosters, benches, loot councils, burnout, composition puzzles.
   The interesting decisions happen *between* fights.
2. **Watchable combat.** Football-Manager-style: you watch your decisions play out — tanks
   swapping, healers triaging, casters turreting, someone dying to the fire. Not full 3D; a
   readable animated stage.
3. **Visible emergent stories.** The sim already produces drama; DM1 buried it in a text log.
   Here, careers, post-raid reports, rivalries, and season races are first-class consumers of
   combat data from day one.

## 2. Non-goals & anti-requirements

- **No DM1 code is ported.** Lessons yes, files no ([ADR-0006](adr/0006-no-code-ported-from-dm1.md)).
- **No 3D.** Art cost kills a solo dev. The combat stage is a Godot 2D scene (simple board first,
  richer sprite/particle pass later).
- **No multiplayer, no live-service backend.** Single-player, fully offline, local saves, Steam
  Cloud. Optional opt-in Steam stats/leaderboard upload only — nothing gameplay-affecting ever
  comes from the network.
- **No browser product.** The shipped game is a standalone PC executable. Browser builds are a
  development playtest harness only.
- **No parallel Mobile\* component tree.** One responsive UI. Mobile *platform* support is a
  post-1.0 question; mobile *layouts* are a constraint on the one tree, not a second tree.
- **No demo-quality feature spikes.** Every feature lands on the floor defined by its milestone
  (§11). If the floor doesn't exist yet, build the floor first.
- **No over-abstraction.** Four packages, not ten. Archetypes require ≥2 users. Strong floor ≠
  maximal abstraction; it means the next feature has an obvious, clean place to land.

## 3. Tech stack

> **DECIDED — [ADR-0008](adr/0008-engine-godot-csharp.md):** **Godot 4 + C#**, exported as a
> native desktop executable. A real game engine, not web — chosen because the product must feel
> like a native RPG-styled game (DM1's web build never achieved that), Godot's **texture-based UI
> theming** is the direct answer to the "real RPG texture, not flat brown" requirement, Godot
> **Resources are our template architecture natively**, and C# suits the large deterministic sim.
> The pure sim/content/game are C# libraries with **no Godot dependency** (headless-testable). The
> stack-agnostic designs in this blueprint (event stream, tick model, registries, renderer
> interface, migration registry, seed+delta) carry over 1:1 — only the language is C#, and TS
> snippets in the specs are pseudocode to port.

| Layer | Choice | Why (short) | ADR |
|---|---|---|---|
| Engine/runtime | Godot 4 (.NET/C#) | Native game feel; texture-first UI; 2D-strong for combat replay; free | [0008](adr/0008-engine-godot-csharp.md) |
| Language | C# (net8.0), nullable enabled, warnings-as-errors | Typed, strong LLM support, great for a big deterministic codebase | [0008](adr/0008-engine-godot-csharp.md) |
| Solution | one `.sln`, project-reference boundaries | Compile-time hard walls (engine can't reference Godot/UI) | [0001](adr/0001-solution-and-boundaries.md) |
| App state | `GameState` autoload + signals | Godot-native per-consumer updates; one home for the save aggregate | [0002](adr/0002-state-model.md) |
| UI styling | Godot `Theme` + `StyleBoxTexture` | Ornate RPG frames authored once, inherited everywhere; single source of visual truth | [ui-design-system](ui-design-system.md) |
| Content templates | Godot `Resource` (`.tres`) + plain C# records | Data-driven Principle 0 native to the engine | [content-authoring](content-authoring.md) |
| Combat stage | Renderer interface; Godot 2D scene | Watchability without lock-in | [0005](adr/0005-hybrid-renderer.md) |
| Desktop/Steam | Godot export templates | Native standalone exe; Steam packaging | — |

## 4. Repo architecture

```
raid-manager/
  RaidManager.sln
  src/
    Engine/    net8.0 lib — deterministic sim core. NO Godot ref. NO project deps. No unseeded RNG/DateTime.Now.
    Content/   net8.0 lib — registries/templates: classes, specs, abilities, traits, encounters, items, archetypes. → Engine
    Game/      net8.0 lib — guild, roster, economy, calendar, worldgen, saves+migrations, day loop. → Engine, Content
    App/       Godot 4 (C#) — UI scenes/themes, combat renderer, GameState autoload. → Engine, Content, Game
    Sim/       net8.0 console — golden runner, balance probes, campaign bot; calls the REAL Game loop. → Engine, Content, Game
  docs/
```

**Dependency rules (the walls) — [ADR-0001](adr/0001-solution-and-boundaries.md):**

- `Engine` references nothing. `Content` → Engine. `Game` → Engine, Content. `App` and `Sim` →
  all three; never each other.
- Enforced by **`ProjectReference` at compile time** — `Engine` *cannot* call Godot or the UI
  because it doesn't reference them; it won't build. Stronger than lint, stronger than the old
  pnpm plan. A `NetArchTest` test is the optional second net for namespace rules/cycles.
- **The mirror rule:** `Sim` calls the same `Game` loop the `App` calls — DM1's 2,843-line
  reimplemented-rules mirror (and its drift bugs) is structurally impossible.

**Size budgets (analyzer warnings):** types/files stay small; *split when you need "and" to
describe what a file does* (DM1 refactor-roadmap rule, unchanged).

**Headless purity note:** `Engine`/`Content`/`Game` never depend on a running Godot instance.
Content authored as Godot `Resource`s is projected into plain C# records at the `App` boundary,
so `Sim` and unit tests run without the editor.

## 5. Simulation core

Full spec: [engine-spec.md](engine-spec.md) · Decisions: [ADR-0003](adr/0003-tick-time-model.md),
[ADR-0004](adr/0004-event-stream-contract.md)

The engine is a pure function:

```ts
simulateEncounter(input: { rng: SeededRng; config: SimConfig; raid: RaidSetup; encounter: EncounterDef }): SimResult
```

- **Deterministic by construction.** The RNG is a required argument with no fallback. Lint bans
  `Math.random`, `Date.now`, `performance.now` across engine/content/game (one allow-listed
  cosmetic file in `app`). Same inputs ⇒ byte-identical output, forever, on every machine.
- **Time is fixed integer ticks** (10 ticks = 1 second of battle time), not turns. Boss timelines
  ("tank swap at 0:45", "AoE every 20s"), cast bars, and overlapping timers are natural in ticks
  and awkward in turns — and watchable playback needs them. The sim still runs to completion
  instantly; playback speed is purely a UI concern. Fallback if tick complexity bites: coarsen to
  1s ticks, which degenerates to DM1-style turns without an architecture change (ADR-0003).
- **The event stream is the product.** The engine's only output is a versioned, append-only
  `CombatEvent[]` — stable entity ids, tick timestamps, and **positions from day one** (coarse
  lane/ring coordinates in v0). Every downstream feature is a fold over this stream: combat log,
  stage renderer, damage meters, wipe analysis, career history, post-raid narrative, rival-guild
  results. If a consumer needs a fact, the engine emits it; consumers never re-derive.
- **N is a value, not a schema.** One `Combatant` type covers raiders, bosses, adds, pets
  (`kind` + `side`); combatants live in `Map<CombatantId, Combatant>`. Raid size 10–25 is runtime
  data; composition rules are data (`{ size, tanks: {min}, healers: {ratio} }`). DM1's
  `tank/dps_1..3/healer` slot-name Record is the named anti-pattern this kills.
- **Encounters are single registry rows** owning metadata, phases, and a timeline of mechanic
  instances that reference **mechanic archetypes** (`tankSwapDebuff`, `spreadDamage`, `stackSoak`,
  `interruptibleCast`, `addWave`, `dispellableDebuff`, `enrage`, …) interpreted by one generic
  runtime — with a `custom` named-handler escape hatch declared in the same row. Adding a boss =
  one new file, zero edits elsewhere.
- **Perf floor for ~60 combatants** (6× DM1): id→entity maps everywhere, effective stats cached
  and invalidated on change (never recomputed per tick), no per-tick closure or factory
  construction. Engine modules are plain functions over an explicit `SimContext` parameter —
  DM1's original sin was helpers closing over engine-local state, which made them inseparable.

## 6. Content pipeline

Full spec: [content-authoring.md](content-authoring.md)

- **One row = one fact.** An ability row carries its numbers, its effect archetype reference, its
  AI priority, and a **tooltip template that interpolates the same fields**
  (`"Heals {healMult}% of spell power over {durationSec}s"`). Tooltips are generated, never
  hand-written. DM1's #1 bug class — tooltip says 187%, engine does 2× — becomes structurally
  impossible for templated text; a drift test covers whatever prose remains.
- **Archetype at ≥2 users.** Below two users, behavior goes in a `custom` handler registered by
  id (never name-string matching). This boundary is a DM1 lesson: healer archetypes paid off,
  but forcing every odd DPS ability into a schema would have been all escape-hatch.
- **Completeness tests per registry.** A class declared without a kit, an encounter without
  phases, a talent without an effect — each fails CI with a message naming the missing row.
- The authoring doc contains literal copy-this-row examples for: ability, talent, trait,
  encounter, item affix. Adding content should feel like filling in a form.

## 7. Management layer & saves

Full spec: [save-format.md](save-format.md)

- All persistent state is one versioned aggregate owned by `src/Game`:
  `GuildSave { version, guild, roster, economy, calendar, history, settings }`.
- **Migration registry from day one:** an ordered list of `{ from, migrate }` folded at load,
  then schema-validated. A fixture save from every historical version round-trips in CI. DM1's
  ad-hoc `normalizeSavedRunState` with six hand-inlined migrations is the cautionary tale.
- Persisted to Godot `user://saves/` via `FileAccess` (atomic temp+rename, `.bak`), Steam
  Auto-Cloud pointed at the folder. Seed + delta-log, compacted at season boundaries (ADR-0007).
- **Career history is a fold, not bookkeeping.** After each raid, `Game` folds the event stream
  into compact per-raider career deltas (kills, deaths, clutch saves, interrupts, parses). The
  last raid's raw stream is kept for replay and the post-raid report, then compacted. Budget:
  one raid's stream serializes to < a few MB.

## 8. UI architecture & combat rendering

Full spec: [ui-design-system.md](ui-design-system.md) · Decision: [ADR-0005](adr/0005-hybrid-renderer.md)

- **Design system before any screen.** `app/src/ds/` holds tokens (spacing, type scale, palette)
  and primitives (`Frame`, `Panel`, `List`, `StatBar`, `Tooltip`, `Modal`). Screens compose
  primitives only. DM1's top visual complaint — "different frames for every part" — is a lint
  error here, not a cleanup task.
- **No `Mobile*` duplicates, ever.** PC-only; layout adapts to window/DPI via the primitives.
- **Playback is a pure consumer.** The sim runs to completion on a background thread (engine
  purity — no Godot deps — makes this free); the UI replays the precomputed stream with a
  playback clock (pause / 2× / skip / seek). The UI never drives the sim. State: `GameState`/
  `UiState` autoloads + signals; stable entity-id keys on every list (DM1's index-keyed combat
  log caused false re-renders on every window slide).
- **Renderer behind an interface:**
  `ICombatStageRenderer { LoadEncounter(scene); ApplyEvents(events, tick); Seek(tick) }` — a
  Godot 2D scene: v0 a simple tactical board (Node2D tokens, drawn telegraphs, tween movement),
  M4 a richer sprite/particle pass on the same interface and same event stream. Because both are
  pure consumers of the stream, the upgrade is scene-local.

## 9. Testing & tooling

Full spec: [testing-strategy.md](testing-strategy.md)

From commit 1, CI requires all of: `dotnet build -warnaserror` (nullable + analyzers:
no-unseeded-RNG/DateTime.Now, size budgets) · `dotnet test` (unit + registry + drift + migration
+ golden + `NetArchTest` boundaries).

1. **Golden tests** — hash of the full event stream per (encounter, roster fixture, seed).
   Refactors keep hashes identical; balance changes re-bless deliberately. These two workflows
   are never mixed in one commit.
2. **Determinism test** — same seed twice ⇒ identical stream; different seeds ⇒ different.
3. **Drift tests** — every rendered tooltip/guide number asserted against its registry row.
4. **Registry completeness tests** — half-added content fails loudly.
5. **Headless campaign runner** (`src/Sim`) — scripted greedy bot first, neural later, always
   through the real `game` loop. Plus a wall-probe: canonical-power beatability sweep per
   encounter, the ground truth balance tool.

## 10. Conventions & budgets

**Principle 0 — everything is instantiated from a template. Nothing is hardcoded.** This is the
governing law of the whole project, stated by the developer and binding on every subsystem.
Seasons, encounters, bosses, mechanics, classes, specs, abilities, items, affixes, traits,
attributes, archetypes, guilds, events, holidays — each is a **data row (template) run through a
generic runtime**, never bespoke code. Adding an instance (a new season count, a 13th class, the
next raid tier, a new event) is a **data change, never a code change**; scaling a tier is bumping
a number band, not editing logic. If you find yourself writing a new code path to add *content*,
stop — the template or its runtime is missing, and that is the thing to build. This is why the
codebase can still absorb features 20 tiers later without a rewrite; it is the concrete meaning of
"solid foundation."

**API shape:** each entity type exposes one **factory** — `createCharacter()`, `createEvent()`,
`createItem()`, `createClass()`, `createAbility()`, `createEncounter()`, `createGuild()`, … — that
instantiates a fully-valid instance from its template/defaults, ready to mutate. You never
hand-assemble an entity; you call its factory and adjust. Factories validate against the registry
so a malformed instance is impossible to create. (Realized by the content registries — [content-authoring.md](content-authoring.md) —
the entity archetypes — [entities-and-worldgen.md](entities-and-worldgen.md) — and the encounter
model — [engine-spec.md](engine-spec.md) §8.)

- Persistent state has exactly one home (`game`'s save aggregate). Transient UI state lives in
  UI stores. Nothing persistent is born inside a component.
- Pure logic lives in engine/content/game; components render.
- One responsibility per module; split when you need "and".
- Components < 300 lines, pure modules < 400 (lint warning, reviewed at PR).
- Every content fact exists in exactly one row. If you're typing a number a second time, stop.
- Refactor commits keep golden hashes identical. Balance commits re-bless them. Never both.
- No `Math.random`, `Date.now`, `performance.now` outside the one allow-listed app file.

## 11. Milestone roadmap

Each milestone has a **strong-floor definition** — the state that must be true before features
stack on top. A milestone isn't done when its features demo; it's done when its floor holds.

| # | Name | Scope | Strong floor (exit criteria) |
|---|---|---|---|
| M0 | Foundation | `.sln` + projects + CI gates, engine skeleton (RNG, tick loop, event contract, one dummy fight), save + migration registry, base `Theme`, golden harness | `sim run dummy --seed 1` prints an event log headless AND the Godot app replays the *identical* stream; golden + determinism tests green |
| M1 | Vertical slice | 10-man raid; ~4 classes / 1 spec each; 1 encounter with 2 phases using tankSwap + spreadDamage + interruptibleCast archetypes; roster & gear screens; combat-log playback; save/load | A full raid night is playable and golden-tested; **adding encounter #2 touches exactly one new file** |
| M2 | Watchable combat | Godot 2D stage renderer (v0 board), playback controls (pause/2×/seek), positions rendered, unit frames | Log view and stage view consume the identical stream; renderer interface proven |
| M3 | Breadth & depth | Full class roster, first raid tier (4–6 encounters), loot/economy loop, campaign calendar, balance harness in anger | Campaign bot clears the tier headlessly; drift suite covers 100% of shipped content |
| M4 | Stories & spectacle | Career history, post-raid narrative report, richer combat-stage pass, rival-guild groundwork (rival raids = same engine run headless, results only), Godot/Steam packaging pass | Career page renders from folded history; the richer stage passes the same replay tests as the v0 board |

## 12. Open questions (deliberately undecided)

- **Final name.** "Raid Manager" is a working title — and generic titles blur in this niche
  (see DM1 market notes: Mythic Manager, three Guild Managers).
- **Art direction** for portraits, stage sprites, UI dressing.
- **v0 scope of adjacent systems:** professions/crafting, PvP, a player-character avatar — all
  out of M0–M2, undecided beyond that.
- **Difficulty tiers** (normal/heroic per encounter vs per tier) and **raid calendar granularity**
  (day-based like DM1, or week-based like FM seasons).
- **Mobile** — one responsive tree keeps the option open; platform decision post-1.0.
- **Telemetry** — DM1 has a setup worth re-designing; privacy posture TBD before any public build.

## 13. Appendix

- [game-design.md](game-design.md) — the game wiki: every system along the player journey, with
  locked/proposed/open status per decision. Design completes and is accepted there before M0 begins.
- [dm1-lessons.md](dm1-lessons.md) — the ten lessons from DM1, each mapped to the decision above
  that answers it. If a future change violates a lesson, that file says why we cared.
