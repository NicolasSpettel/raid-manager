# Raid Manager — ARCHITECTURE.md (the wiki)

> **Read this first, every session.** This file is the map of the project: what exists, where it
> lives, what's decided, what's open. It is a *living index* — any session that adds, moves, or
> decides something updates this file in the same commit. If this file and reality disagree,
> fixing this file is the first task.

## Project status

**Phase: M1 VERTICAL SLICE COMPLETE — tagged `v0.1-m1`.** M0 is tagged `v0.0-m0`. The Godot app is a full
**management loop**: load/create a guild → **roster screen** (levels + gear power, last-raid banner) →
**pick a boss** (Warden or Sentinel) → **run the real engine** → **watch the combat playback** (HP bars,
log, play/pause/speed/seek) → back → **Save** (atomic, `user://saves/`). Raids have **consequences**: a win
awards gold + XP, raiders **level up**, the boss **drops gear** that auto-equips (folding into combat stats),
and the outcome is folded into a `RaidSummary` and auto-saved — a real campaign with progression. Headless,
`sim campaign --raids N` drives the **same loop** for balance. Under it: a deterministic, data-driven combat
engine and a versioned `GuildSave` (chained through real **v1 → v2 → v3** migrations); a first-cut carved-stone
theme with a procedural stone-textured backdrop. **Auras** (DoTs + stacking debuffs) back a
tier-2 boss (the Ashen King), and **difficulty tiers** (Normal/Heroic/Mythic) scale encounters — a fresh
guild clears the Ashen King on Normal but wipes it on Mythic; fallen raiders are **injured** and fight
weaker until they recover (so roster depth matters). **M2 step 1 is in:** a 2D **stage renderer** (tokens,
live HP, telegraph flashes) sits behind a **Log ↔ Stage toggle** — a second pure consumer of the same
event stream. `dotnet build -warnaserror` + `dotnet test` green (70 tests).
**Next (post-slice):** the authored **textured `Theme`** + display font (the gritty look), threat-based
taunt-swaps + the `interruptibleCast` archetype, and **M2 step 2** — engine positions (fixed-point 2D) +
move events + telegraph geometry (stand-in-fire), then a richer sprite pass. Plans:
**[docs/m1-build-plan.md](docs/m1-build-plan.md)** · **[docs/m2-build-plan.md](docs/m2-build-plan.md)**.

**If you are a coding session, read in this order:** this file → [docs/m1-build-plan.md](docs/m1-build-plan.md)
→ [docs/BLUEPRINT.md](docs/BLUEPRINT.md) §4 (repo) & §10 (conventions) → [docs/engine-spec.md](docs/engine-spec.md)
→ [docs/testing-strategy.md](docs/testing-strategy.md). Build in `E:\raid-manager\`. Every step ends
green; determinism and boundaries are enforced from the first commit.

**Design still THIN (not needed for M0, flesh out before their milestone):** see
[docs/design-status.md](docs/design-status.md) — economy is priced, combat math researched;
contracts/staff/inbox/venues/AI-guild depth are direction-set but not specced.

## What is this game (one paragraph)

**Football Manager in an RPG world.** You are a raid guild manager, not a hero: create your
manager, take a job at one of ~100–200 unique guilds in a living world, negotiate your contract,
and race other guilds through raid-release "seasons" — scheduling raid nights, developing 10–30
unique raiders (trainable attributes, per-role star ratings, personalities, morale), farming gear
on off-days, reading combat logs, and climbing the world-first leaderboard. Fully offline,
PC-first, standalone executable; optional Steam stats/leaderboard upload only.

## Document map (what is where)

| Doc | Contents | Read when |
|---|---|---|
| **[docs/game-design.md](docs/game-design.md)** | **The game wiki**: every system, player journey from opening screen to season end, all design decisions + open questions. THE source of truth for *what we're building*. | Designing/adding any feature |
| **[docs/m0-build-plan.md](docs/m0-build-plan.md)** | **M0's step-by-step** (done): solution structure, build order, quality gates, definition of done | Reference: how the floor was laid |
| **[docs/m1-build-plan.md](docs/m1-build-plan.md)** | **The vertical-slice plan**: 10 steps from the N-combatant engine → a playable, golden-tested raid night, each a green commit boundary | Building M1 |
| [docs/design-status.md](docs/design-status.md) | Per-system maturity audit: what's build-ready (SOLID), what needs a design pass (THIN), what's blocked on the dev (DECISION) | Deciding what to work on next |
| [docs/BLUEPRINT.md](docs/BLUEPRINT.md) | Architecture spine: pillars, module structure, sim core, milestones. THE source of truth for *how it's structured*. | Any technical work |
| [docs/engine-spec.md](docs/engine-spec.md) | Combat sim: tick model, entity model, event stream union, mechanic archetypes, perf budget | Engine/combat work |
| [docs/content-authoring.md](docs/content-authoring.md) | Registry patterns + copy-this-row cookbooks (ability, encounter, trait, class) | Adding content |
| [docs/entities-and-worldgen.md](docs/entities-and-worldgen.md) | How every character is modeled (components), generated (deterministic latent-factor pipeline), and persisted (seed + deltas) | Any work touching characters, rosters, the living world, or world creation |
| [docs/save-format.md](docs/save-format.md) | Save aggregate, migration registry, storage adapters | Persistence work |
| [docs/ui-design-system.md](docs/ui-design-system.md) | Tokens, primitives, screen inventory, playback pipeline | Any UI work |
| [docs/testing-strategy.md](docs/testing-strategy.md) | Golden/drift/completeness tests, headless campaign runner, CI gates | Test/tooling work |
| [docs/research/boss-design-reference.md](docs/research/boss-design-reference.md) | WoW boss anatomy + warcraftlogs report anatomy → our archetype/event/report requirements | Designing encounters or report UIs |
| [docs/research/gear-stats-reference.md](docs/research/gear-stats-reference.md) | WoW's stat-design history (what got removed and why) → our stat recommendation | Designing items/stats/combat math |
| [docs/research/wow-damage-model-reference.md](docs/research/wow-damage-model-reference.md) | Vanilla WoW L60 damage/armor/spell formulas → our proposed combat math (very close, cleaner) | Implementing the damage model / combat formulas |
| [docs/adr/](docs/adr/) | One decision per file; immutable once code depends on it — then superseded, never edited (revisable in place only pre-code) | Questioning a decision |
| [docs/dm1-lessons.md](docs/dm1-lessons.md) | DM1's ten scars → the decision answering each | Before violating a convention |

## Decisions: locked vs provisional vs open

**Locked (by the developer):**
- Fully offline game. Optional online: Steam stats, run upload, leaderboard comparison — never required.
- PC is the platform. Standalone executable is the product. No mobile. Browser allowed as a *dev
  playtest harness* only — never the shipped product.
- This is a real commercial game, not a demo. No demo-quality tooling in the product path.
  **Features ship whole or get scrapped — never half-assed.**
- Automation pillar: every recurring decision delegatable at ~90% optimal, preferably
  diegetically via staff (§6b of the GDD).
- **Principle 0 — everything from a template, nothing hardcoded** (BLUEPRINT §10). Every
  subsystem instantiates content from data rows through generic runtimes; adding an instance is a
  data change, never a code change. The governing law of the project.
- World history retention = season-boundary compaction (snapshots) + a permanent compact
  **Chronicle** (winners, top-100 leaderboards, world-firsts); bounds save size to one season +
  Chronicle regardless of career length ([ADR-0007](docs/adr/0007-world-persistence-seed-plus-deltas.md)).
- Fresh start: no DM1 code, no DM1 tech assumptions. FM-in-an-RPG-world is the design north star.
- Design completes before code. Blueprint milestones (M0+) begin only after the GDD is accepted.
- No character levels — raider growth is attributes/stars/gear/talents; the *manager* levels.
- Combat is a real-time-under-ticks rework (swing timers, cast times, boss timers); replay is
  the warcraftlogs approach (top-down circles + telegraphs) with WCL-style reporting.
- ~2 raid releases per year with pre-patch gear windows; aging pass at season end; trade market
  with async negotiations is a headline system.
- Monday resets; days = 4×2h slots; gold salaries with FM-style budgets/revenue; investable
  guild-hall venues; FM's sidebar as the screen-inventory blueprint; the §14 anti-design
  principles (no wallet-scaling prices, no AI cheating, reachable small-to-big arc) are locked.

**Design-phase architecture decided (stack-agnostic, survives the stack call):**
- Characters modeled as composed **components** with registry-keyed attributes; generated by a
  deterministic latent-factor pipeline; world persisted as **seed + event-sourced deltas**
  ([entities-and-worldgen.md](docs/entities-and-worldgen.md), [ADR-0007](docs/adr/0007-world-persistence-seed-plus-deltas.md)).
  Same mechanism answers small-saves + cheap-6500-unit-world + AI-fidelity-tiers.

- **Engine/stack — DECIDED, [ADR-0008](docs/adr/0008-engine-godot-csharp.md):** **Godot 4 + C#**,
  native desktop exe. A real game engine, not web (DM1's web build never achieved the RPG feel).
  Godot's texture-based UI theming is the direct answer to "real RPG texture"; Godot `Resource`s
  are our template architecture natively; the pure sim/content/game are C# libs with **no Godot
  dependency** (headless-testable). LLM build+verify via Godot AI / godot-mcp / Ziva (screenshots).
- **Solution layout — [ADR-0001](docs/adr/0001-solution-and-boundaries.md):** one `.sln`;
  `Engine ← Content ← Game ← {App(Godot), Sim(console)}`; boundaries are **compile-time project
  references** (stronger than lint).
- **App state — [ADR-0002](docs/adr/0002-state-model.md):** `GameState` autoload (holds the save
  aggregate, only persistent writer) + signals; `UiState` for transient.
- **UI aesthetic — LOCKED:** fantasy-RPG (parchment/iron/wood/gold-leaf/serif), explicitly NOT
  the futuristic/glassmorphic LLM default; enforced by **one Godot `Theme` (StyleBoxTexture)** +
  a Component Gallery scene as single source of visual truth + screenshot verification (solves
  DM1's LLM visual-drift).
- **Factory API — LOCKED:** every entity type has a `createX()` factory (createCharacter,
  createEvent, createItem, createClass, createAbility…) that instantiates from template then you
  mutate. Never hand-assemble.

**Open (tracked in [game-design.md §Open Questions](docs/game-design.md)):** season/calendar
cadence (coupled to aging), manager & raider attribute lists, difficulty-tier rollout, economy
scale, combat-renderer richness pass, and more — each with a current recommendation.

## Code map

M0 foundation built and green. Each module gets one line: what it owns, what it must not know about
(the walls are `ProjectReference`s — [ADR-0001](docs/adr/0001-solution-and-boundaries.md)).

| Module | Owns (M0 state) | Must not know about |
|---|---|---|
| `src/Engine` | Deterministic sim core: `SeededRng`, `Tick`/`TimeModel`, combatant model, scheduled `ActionQueue`, abilities & casts (`DirectDamage`/`DirectHeal` + GCD/cooldown/priority/resource), role-aware targeting, `ExecutionProfile` (reaction delay), auras (`AuraDef` — DoTs + stacking damage-taken debuffs), encounter model (`EncounterDef` phases + mechanic timeline; `MechanicArchetype` runtime: spread / buster / enrage / raid-DoT / tank-debuff), `CombatEvent` union, `EventStream` (serialize + FNV-1a hash), `SimulateEncounter`, `Fixtures` | Content · Game · App · Godot — references nothing in the solution |
| `src/Content` | Ability registry (`AbilityRow` → `AbilityDef`, generated `Tooltips`), class roster (`Classes` + `createRaider` factory), encounter catalog (`Encounters`: Warden, Sentinel, Ashen King, Frostwarden — tiers 1–3), difficulty scaling (`Difficulties`: Normal/Heroic/Mythic), item catalog (`Items` + per-encounter `Loot`), `ContentFixtures` | Game · App · Godot |
| `src/Game` | The management layer: `GuildSave` aggregate (guild + roster + progression + gear + injuries + economy + raid history), `SaveMigrations` (v4, real chained v1→v2→v3→v4), `SaveSerializer`, atomic `FileStorageAdapter`, `SaveService`, `Guilds.CreateStarter`, `Warband` (projects raiders → combatants, folding gear + injury penalty), `Campaign` (headless N-raid loop), `Recruitment` (hire raiders for gold), and `RaidResolver` (folds a fight into gold / XP / levels / loot / injuries + a `RaidSummary`) | App · Godot |
| `src/Sim` | Headless CLI `run dummy --seed N` → the real Engine; future golden/probe/campaign home | App · Godot |
| `src/App` | Godot 4.7 C#: `AppTheme` (carved-stone `Theme` + procedural stone backdrop), `Main` coordinator (guild → roster → raid → Log/Stage playback → save; difficulty, rest, recruit), `RosterView`, `CombatView` (log playback), `StageView` (2D board playback). Both views are pure consumers of the same event stream. **The only Godot-referencing project.** | — |
| `tests/Engine.Tests` | Golden (dummy/trio/caster hashes + full-stream snapshot) + determinism + outcome/cast behavioral coverage | — |
| `tests/Content.Tests` | Registry completeness + tooltip drift + content→engine integration | — |
| `tests/Architecture.Tests` | NetArchTest boundary rules — second net over the ProjectReference walls | — |

**Quality gates** (root: `Directory.Build.props`, `.editorconfig`, `BannedSymbols.txt`): nullable,
warnings-as-errors, file-scoped namespaces, and the determinism banned-API guard (`System.Random`,
`DateTime.Now/UtcNow`, `Guid.NewGuid` fail the build in Engine/Content/Game). CI: `.github/workflows/ci.yml`.

## Session protocol for LLMs (and humans)

1. Read this file. Follow the link matching your task from the document map.
2. Before adding a system: check game-design.md — is it designed? Design first if not.
3. After any structural change: update this file's Code map / Decisions in the same commit.
4. Never resolve an "open" item silently — either ask the developer or record the decision in
   game-design.md's decision log with rationale.
