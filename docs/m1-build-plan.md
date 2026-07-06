# M1 Build Plan — the Vertical Slice

> Turns the M0 skeleton into a **playable raid night, golden-tested end to end**. Same discipline as
> [m0-build-plan.md](m0-build-plan.md): every step is small, ends green, and is a commit boundary.
> Specs (don't duplicate): [engine-spec](engine-spec.md), [content-authoring](content-authoring.md),
> [ui-design-system](ui-design-system.md), [save-format](save-format.md), [BLUEPRINT §11](BLUEPRINT.md).

## Definition of done (the strong floor — all must be true)

1. A full raid night is playable from the Godot app: **new career → roster → assignments → run raid
   → watch the combat-log playback → loot drops → save → reload**, and it survives a reload.
2. The whole thing is **golden-tested**: the raid-night event stream hashes are pinned; determinism
   and drift suites are green; migration fixtures round-trip.
3. **Adding encounter #2 touches exactly one new file** (one Content registry row) — the proof that
   Principle 0 and the mechanic-archetype runtime actually hold.
4. All M0 gates stay green (build -warnaserror, boundaries, banned-API guard).

## Design decisions M1 needs from the dev (flag early, don't guess)

These are the DECISION items from [design-status.md](design-status.md) that M1 actually touches.
Steps that need them say so; the rest proceed now.

- **Which 4 classes / 1 spec each** for the slice (needed at step 6). Recommendation: one of each role
  — a plate tank, a healer, a melee DPS, a ranged/caster DPS — so all role policies get exercised.
- **Confirm raid size for the slice = 10** (the encounter is authored for 10). Assumed yes.
- **Raider attribute list** (needed once execution quality matters, step 3+). Working set: the
  proposed ~11 from game-design §2/§8 until confirmed.
- Not needed for the slice (defer): economy scale, contracts, armor-type breadth, professions.

## Build order (each step ends green; commit per step)

### Step 1 — N-combatant engine core *(engine only, fully unblocked)*
Generalize the sim from the M0 1-v-1 dummy to the real entity model (engine-spec §4–§6):
`Combatant` (id, kind, side, role, name, `StatBlock`, hp) stored in a `Map<CombatantId, Combatant>`;
string `CombatantId`s (`"r:thara"`, `"boss:main"`); a scheduled `ActionQueue` (priority queue keyed
by `(tick, seq)`) instead of a per-tick scan; both sides auto-attack on per-combatant swing timers;
outcome = `Kill` / `Wipe` / `Timeout`. New golden fixture (a small raid vs a boss that hits back);
re-bless the dummy golden (a deliberate schema change — string ids). **Exit:** determinism + golden
green for a multi-combatant fight; nothing hardcodes raid size.

### Step 2 — Abilities, casts & the content registry *(introduces `src/Content`)*
Ability rows as Content templates (Principle 0): numbers + effect archetype + `aiPriority` +
tooltip template. `cast_start`/`cast_end` events, GCD, a DPS priority-list role policy, one damage
ability + one instant. Registry completeness + **drift test** (tooltip number == registry row).
**Exit:** a DPS raider casts from data; adding an ability is a new row.

### Step 3 — Roles, healing & resources
`heal` events, healer triage policy (effective-HP deficit), tank threat/survival stub, mana/resource
clock. `executionProfile(raider)` v0 (reaction delay, mechanic-failure chance) from a working
attribute set. **Exit:** a healer keeps a tank alive; a sloppy raider visibly underperforms; golden-tested.

### Step 4 — Encounter model + mechanic archetypes
`EncounterDef` as one Content row (scene, enemies, phases, timeline, enrage, tuning) + the generic
mechanic runtime with the **3 M1 archetypes**: `tankSwapDebuff`, `spreadDamage`, `interruptibleCast`.
Encounter has **2 phases**. **Exit:** the boss runs its dance; **encounter #2 = one new file** (proven
by adding a throwaway second row in a test).

### Step 5 — Positions (fixed-point 2D)
Arena scenes, `move` events, telegraph geometry (circle/cone/ring, integer math) so `spreadDamage`
and forced movement are real. **Exit:** positions in the stream; geometry checks golden-tested.

### Step 6 — The 4 classes / 1 spec each *(needs the class decision)*
Author the 4 classes as Content rows (kits, resources, mobility toolkit). No new engine code — pure
data through the step 2–5 runtimes. **Exit:** a 10-raider raid of real classes clears a target dummy.

### Step 7 — Save/load + migration registry *(introduces the `src/Game` aggregate)*
`GuildSave { version, guild, roster, … }` owned by `src/Game`; atomic save to `user://saves/`
(temp+rename+`.bak`); ordered migration fold + schema validate; a v0 fixture round-trips in CI
(save-format.md). **Exit:** reload restores exact state; migration fixture green.

### Step 8 — Roster & gear screens (Godot)
Design-system primitives first (`Frame`, `Panel`, `List`, `StatBar`, `Tooltip`), one `Theme`, a
Component Gallery scene; then the roster + gear screens bound to `GameState` signals (ADR-0002).
**Exit:** screens render from the save aggregate; no per-screen frame reinvention (ui-design-system).

### Step 9 — Combat-log playback (Godot)
The log view replays the precomputed event stream with a playback clock (pause / 2× / seek), a pure
consumer on a background-thread sim (never drives it). Stable entity-id keys. **Exit:** log view
replays a golden stream; fold count asserted (testing-strategy §6).

### Step 10 — Wire the raid night
New-career flow → roster → assignments → run raid → playback → loot → save, end to end. A headless
`sim campaign --days 1` runs the same `Game` loop. **Exit:** the DoD floor above holds; tag `v0.1-m1`.

## Rules (unchanged from M0)
- Every step ends green; small commits on step boundaries.
- Refactor commits keep golden hashes identical; deliberate behavior/schema changes re-bless them
  with the diff noted in the commit — never both in one commit.
- No content hardcoded: if adding content needs new code, the template or its runtime is missing —
  build that instead.
