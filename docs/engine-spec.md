# Engine Spec — `src/Engine`

> **Language: C# (net8.0).** Code snippets below are TypeScript-flavored pseudocode from the
> design phase — the *designs* are language-agnostic and port 1:1 to C# (records, discriminated
> unions → sealed record hierarchies / `OneOf`, `Map` → `Dictionary`). Port as each piece is built
> ([ADR-0008](adr/0008-engine-godot-csharp.md)).

The deterministic simulation core. **No Godot reference, no `System.Random`, no `DateTime.Now`.**
Everything here runs identically on a background thread in the `App`, in `src/Sim` (console), and
in CI.

Related decisions: [ADR-0003 ticks](adr/0003-tick-time-model.md),
[ADR-0004 event stream](adr/0004-event-stream-contract.md).

## 1. Entry point

```ts
export function simulateEncounter(input: {
  rng: SeededRng;            // required — no default, no fallback
  config: SimConfig;         // engine tunables (tick rate, caps, debug flags)
  raid: RaidSetup;           // combatants + composition + assignments
  encounter: EncounterDef;   // from src/Content
}): SimResult;

export interface SimResult {
  events: CombatEvent[];
  outcome: "kill" | "wipe" | "enrage_wipe" | "timeout";
  seed: number;
  engineVersion: string;      // build-injected
  eventSchemaVersion: number; // append-only union version
}
```

Pure function: same input ⇒ byte-identical `events`, on every machine, forever. The golden suite
depends on this literally (stream hash).

## 2. Determinism rules (non-negotiable)

- `SeededRng` (Mulberry32 or PCG — pick once in M0, never change silently; the choice is part of
  the golden contract) is threaded explicitly. Any code path needing randomness receives the rng;
  nothing constructs its own.
- Lint bans `Math.random`, `Date.now`, `performance.now` in engine/content/game. One
  allow-listed file in `app` for cosmetic-only effects (screen shake jitter etc.).
- Iteration order is deterministic: combatants live in insertion-ordered `Map`s; any sort has a
  total ordering with id tiebreaker. No object-key iteration for anything order-sensitive.
- Floats are allowed in *values* (damage math) but never in *time*; rounding happens at defined
  points (documented per formula) so refactors preserve rounding shape — a DM1 golden-test lesson.

## 3. Time model

- `tick: number` (integer), 10 ticks = 1s battle time (`TICKS_PER_SEC = 10`, changeable only via
  ADR because golden hashes depend on it; the developer has floated 0.25s resolution — final
  constant is an M0 decision, golden-locked after).
- The tick model exists to carry (game-design §7, locked): **weapon swing timers** (per-weapon
  attack speed), **spell cast times**, **boss attack timers and ability cooldowns**, and
  mechanics triggered by elapsed time, timers, or HP thresholds — all integer tick counts.
- The main loop advances tick-by-tick to completion. Per-tick work is *scheduled*, not scanned:
  a priority queue (binary heap keyed by `(tick, seq)`) of pending actions — casts completing,
  auras expiring, timeline triggers, GCDs freeing. Most ticks touch only the queue head.
- Hard cap: `config.maxTicks` (default 15 min battle time) ⇒ `outcome: "timeout"`.

## 4. Entity model

```ts
type CombatantId = string; // stable within an encounter: "r:thara", "boss:main", "add:wave2-3", "pet:r:thara-1"

interface Combatant {
  id: CombatantId;
  kind: "raider" | "boss" | "add" | "pet";
  side: "raid" | "enemy";
  role: "tank" | "healer" | "melee" | "ranged"; // authored for raiders, informative for enemies
  name: string;
  base: StatBlock;             // immutable input stats (gear/level/traits already folded by game)
  position: Position;          // see §7
  // mutable combat state:
  hp: number; resource: number;
  auras: Map<AuraId, AuraInstance>;
  casting: CastState | null;
  gcdReadyAt: number;          // tick
}
```

- Storage: `Map<CombatantId, Combatant>` in `SimContext`. Never arrays indexed by slot; never
  slot-name records. **Raid size is a runtime value.**
- **Effective stats are cached:** `effective(c)` reads a cache entry invalidated only when an
  aura/gear/stance change touches that combatant (dirty flag). Nothing recomputes stats per tick.
  With ~60 combatants this is the difference between fine and quadratic sludge (DM1 audit: ~260
  redundant trait scans *per turn* at 5-man scale).

## 5. SimContext — the anti-closure rule

DM1's original sin: engine helpers closed over engine-local state, welding them into one file.
Here, **no module closes over sim state.** Every system is a plain function taking an explicit
context parameter:

```ts
interface SimContext {
  rng: SeededRng; config: SimConfig; tick: number;
  combatants: Map<CombatantId, Combatant>;
  byside: { raid: CombatantId[]; enemy: CombatantId[] }; // maintained, not rebuilt
  queue: ActionQueue;
  events: EventSink;          // push() normalizes + stamps tick; the ONLY way events exist
  encounter: EncounterRuntime; // phase index, timeline cursor, mechanic state
}

// systems are verbs over the context:
resolveCast(ctx, casterId, abilityRow): void
applyDamage(ctx, packet: DamagePacket): void
tickAuras(ctx): void
advanceTimeline(ctx): void
```

Rules: no factories constructed per tick, no allocation in the hot loop beyond events actually
emitted, alive-lists maintained incrementally (dead combatants move lists on the death event, not
via per-call filtering — DM1's `getAliveTargets` rebuilt closures 15–20×/turn).

## 6. Event stream

See ADR-0004 for the contract philosophy. The union (v1, append-only):

```ts
type CombatEvent =
  | { t: number; kind: "encounter_start" | "encounter_end"; ... }
  | { t: number; kind: "phase_change"; phase: number; name: string }
  | { t: number; kind: "cast_start"; source: CombatantId; ability: AbilityId; durationTicks: number; target?: CombatantId }
  | { t: number; kind: "cast_end"; source: CombatantId; ability: AbilityId; result: "done" | "interrupted" | "cancelled"; by?: CombatantId }
  | { t: number; kind: "damage"; source: CombatantId; target: CombatantId; ability?: AbilityId;
      amount: number; school: "physical" | "magic" | "true"; crit: boolean; overkill: number; absorbed: number }
  | { t: number; kind: "heal"; source: CombatantId; target: CombatantId; ability?: AbilityId;
      amount: number; crit: boolean; overheal: number }
  | { t: number; kind: "aura_apply" | "aura_refresh" | "aura_expire" | "aura_dispel"; target: CombatantId; aura: AuraId; stacks?: number; by?: CombatantId }
  | { t: number; kind: "move"; who: CombatantId; from: Position; to: Position; reason: "mechanic" | "assignment" | "spawn" }
  | { t: number; kind: "death" | "resurrect"; who: CombatantId; by?: CombatantId; ability?: AbilityId }
  | { t: number; kind: "spawn" | "despawn"; who: CombatantId; at: Position }
  | { t: number; kind: "mechanic"; mechanic: MechanicInstanceId; note: MechanicNote } // soaked, dodged, failed, assigned…
  | { t: number; kind: "resource"; who: CombatantId; delta: number; now: number }
  | { t: number; kind: "cooldown_used"; who: CombatantId; ability: AbilityId; category: "defensive" | "offensive" | "movement" };
// damage events additionally carry `avoidable?: HazardRef` when sourced from telegraphed ground
// effects — both additions come from the warcraftlogs research (death recaps: "defensive was
// available, unused"; Problems-style auto-analysis). See docs/research/boss-design-reference.md §2.
```

- All events flow through `EventSink.push()` which stamps `t`, validates ids in dev builds, and
  is the single choke point for schema versioning.
- Emit meaningful moments, never per-tick state. Periodic `snapshot` keyframes (every N seconds,
  compact HP/position vector) are the one sanctioned exception — they exist so `seek(tick)` and
  crash-resume are cheap, and they're flagged so folds can skip them.

## 7. Positions (v1: fixed-point 2D — upgraded from coarse zones)

*Amended 2026-07-06: the combat-view vision (game-design §7 — telegraph circles, running out of
AoEs, the greedy melee dying at the ring's edge) requires real coordinates; coarse zones were
superseded before any code existed.*

```ts
type Position = { x: number; y: number }; // fixed-point integers (centimeter-scale units) — no floats in space, same rule as time
```

- **Arena:** each encounter's `EncounterScene` defines an abstract 2D arena (bounded circle or
  rect, in "meters"), spawn positions, and named zones (melee ring, ranged arc, safe spots) that
  are *derived labels over coordinates* — role policies use zones, mechanics use geometry.
- **Movement model — deliberately simple:** straight-line movement at per-raider speed (base ±
  class toolkit ± attributes), no pathfinding, no unit collision, no terrain. Movement starts as
  a decision (mechanic response, assignment) and emits `move` events with from/to/duration;
  renderers interpolate between them.
- **Geometry checks are cheap and deterministic:** telegraph = shape (circle/cone/ring) +
  center + radius in fixed-point; "inside?" is integer math. Distance checks (melee range,
  spread radius, splash overlap) likewise. Squared-distance comparisons avoid roots entirely.
- **Casting-while-moving is the class-toolkit hook (§8c of the GDD):** movers with
  `mobileCasting` keep acting; blink teleports (one move event, near-zero duration); everyone
  else loses uptime for the travel ticks — which is exactly the DPS-loss-during-movement-phase
  math the hybrid damage model wants.

## 8. Encounter model

An encounter is **one registry row in `content`** owning everything:

```ts
interface EncounterDef {
  id: string; name: string; tier: number;
  scene: EncounterScene;           // arena zones/spawn points for renderers
  enemies: EnemySpawn[];           // boss + initial adds, stat blocks by reference
  phases: PhaseDef[];              // ordered; trigger: { hpBelowPct } | { atTick } | { addsDead }
  timeline: MechanicInstance[];    // the raid-dance script
  enrage: { atTick: number; effect: MechanicRef };
  tuning: TuningBlock;             // per-difficulty multipliers
}

interface MechanicInstance {
  id: MechanicInstanceId;
  archetype: MechanicArchetypeId | { custom: CustomHandlerId }; // custom handlers registered by id
  phase?: number;
  schedule: { atTick?: number; everyTicks?: number; startTick?: number; count?: number };
  params: Record<string, number | string>; // validated per-archetype in tests, not at runtime
}
```

**Mechanic archetypes (M1 starter set, expanded per game-design §7/§8c and the boss research):**
`tankSwapDebuff`, `spreadDamage`, `stackSoak`, `interruptibleCast`, `addWave`,
`dispellableDebuff`, `groundHazard`, `enrageSoft`, `raidBurst` (heavy AoE demanding healer CDs),
`absorbShield` (burst check — the 0:30 barrier), `forcedMovement` (keep-walking for N ticks or
take heavy damage), `rangedSplash` (targets ranged positions; class mobility toolkits discount
the DPS loss), `meleeExplosion` (melee must leave and return; out-speed and re-entry differ per
raider/class), `mechanicPhase` (boss untargetable; N raiders — assigned, auto-picked, or random
per encounter def — must execute a task), `targetedDebuffRunOut`; M3/M4 additions:
`carryObject` (spawn object at position; carrier moves it to/from a target zone — pure
movement + state on existing rails). One generic runtime interprets
all of them; each consults the **class mobility toolkit** (registry data: mobileCasting, blink,
dash, immunity, …) and the raider's execution profile to price the mechanic's DPS/HP cost. New
archetype requires ≥2 prospective users; otherwise it's a `custom` handler declared in the same
row. **Adding a boss = one new file. Zero edits elsewhere.**

**Cooldown-timing plans (game-design §7):** `RaidSetup` assignments include per-timeline-entry
CD plans (hold burst for entry X, healer CD rotation across entries, tank defensive ladder,
potion spots). Execution is per-raider: adherence derives from Learning/discipline attributes
and per-boss familiarity — defection (dumping CDs early) is emitted via `cooldown_used` events
so the log shows exactly who broke the plan.

## 9. Raider behavior model

How the roster "plays" the fight — where management decisions become combat outcomes:

- **Assignments** come in via `RaidSetup` (tank order, interrupt rotation, healer targets,
  soak groups) — the player's pre-pull decisions, data not code.
- **Role policies** (generic, per role): tanks maintain threat/survival CDs, healers triage by
  effective-HP deficit with configurable style (proactive/reactive), DPS execute priority lists
  from ability rows' `aiPriority`.
- **Execution quality** is where raiders differ: reaction delay, mechanic-failure chance, CD
  timing discipline — all derived from stats/traits/morale via one documented function
  (`executionProfile(raider)`). A sloppy raider *visibly* dodges late on the stage; a disciplined
  one interrupts on time. This link is the game's heart and gets its own golden fixtures.

## 10. Performance budget

- Target: 25-man, 8-minute fight (4,800 ticks), < 150ms sim time in the worker, < 50k events.
- No allocation inside per-tick system loops except emitted events; queue nodes pooled if
  profiling demands (not preemptively).
- `src/Sim` ships a `--profile` flag from M0 (tick-loop timing histogram) so perf regressions
  are measured, not vibed.
