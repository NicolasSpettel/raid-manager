# M1 Build Plan — the Vertical Slice

> Turns the M0 skeleton into a **playable raid night, golden-tested end to end**. Same discipline as
> [m0-build-plan.md](m0-build-plan.md): every step is small, ends green, and is a commit boundary.
> Specs (don't duplicate): [engine-spec](engine-spec.md), [content-authoring](content-authoring.md),
> [ui-design-system](ui-design-system.md), [save-format](save-format.md), [BLUEPRINT §11](BLUEPRINT.md).

## Definition of done (the strong floor — all must be true)

**Status — met, tagged `v0.1-m1`.** Deep raid *assignments* (CD plans, interrupt rotations) are the one
piece deferred: they depend on the `tankSwapDebuff` / `interruptibleCast` archetypes (which need
auras / threat / raid interrupts), so they move to the next combat-depth pass with those. The playable
loop is: new career → roster → **pick a boss** → run raid → watch playback → gold/XP/loot → save → reload.

1. A full raid night is playable from the Godot app: **new career → roster → pick boss → run raid
   → watch the combat-log playback → loot drops → save → reload**, and it survives a reload. ✅
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

### Step 4 — Encounter model + mechanic archetypes ✅ done
`EncounterDef` with enemies, ordered **phases** (tick/HP-below triggers), and a **timeline** of mechanic
instances, interpreted by **one generic runtime**. **Archetypes: `spreadDamage`, `tankBuster`,
`enrage`, plus `raidDot` and `tankDebuff`** (post-slice, once the **aura system** landed — DoTs +
stacking damage-taken debuffs; `tankDebuff` is the `tankSwapDebuff` idea minus the taunt-swap, which
needs threat) and **`interruptibleCast`** (raider interrupts vs boss casts — an interrupt-rotation
pressure). Only threat-based taunt-swaps remain deferred. The Ashen King (tier 2) and Frostwarden
(tier 3) use these. Two-phase
boss (Warden, HP&lt;50% → Frenzy, phase-gated mechanic) + a second authored boss (Sentinel) prove
**encounter #2 = one new row** ([Content/Encounters](../src/Content/Encounters/Encounters.cs)). Golden +
behavioral tests green.

### Step 5 — Positions (fixed-point 2D) — deferred (reordered after the renderer)
Arena scenes, `move` events, telegraph geometry (circle/cone/ring, integer math) so `spreadDamage`
and forced movement are real. **Deferred:** `spreadDamage` is raid-wide in v0; positions land with the
Godot stage/telegraphs (step 9+) that actually consume geometry — build the floor when the feature
needs it (BLUEPRINT §2 "no over-abstraction"). **Exit:** positions in the stream; geometry golden-tested.

### Step 6 — The 4 classes ✅ done
Guardian (tank) / Cleric (healer) / Blademaster (melee) / Pyromancer (caster), authored as `ClassDef`
rows (role, base stats, ability-id kit) + the `createRaider` factory; kits reference the ability
registry. **No engine code per class** — pure data through the step 2–4 runtimes. A 4-class raid clears
a passive dummy using every kit (the Cleric emergently smites when nobody's hurt) and beats the authored
Warden (`sim run classraid`). Working names — renameable. *(10-raider scale + spec depth come later.)*

### Step 7 — Save/load + migration registry ✅ done
`src/Game` owns `GuildSave` (guild + roster + economy); `SaveSerializer` runs parse → migrate → validate;
`FileStorageAdapter` writes atomically (temp → replace → `.bak`); `Guilds.CreateStarter` generates a
deterministic roster from the class registry (the timestamp is passed in — Game never reads wall-clock,
per the determinism guard). A frozen `save-v1.json` fixture round-trips through the pipeline in CI. Reload
restores state; corrupt/versionless blobs raise `SaveException`, never crash. Green.

### Step 8 — Theme + roster screen (Godot) — first cut
`AppTheme` = one **carved-stone** `Theme` (stone panels, beveled stone-slab buttons, engraved bars)
applied app-wide from the coordinator; the roster + combat screens are framed with it and render from the
save aggregate. Procedural `StyleBoxFlat` for now — the enrichment pass swaps in **texture-based
`StyleBoxTexture`** (parchment / iron / marble), a display/serif font, ornament, plus the gear screen and
a Component Gallery. Chosen baseline: carved stone; north-star references are Witcher 3 + Dark-and-Darker
(gritty, textured, worn).

### Step 9 — Combat-log playback (Godot) ✅ done
The `App` runs the real engine once, then **replays** the precomputed stream with a playback clock
(play/pause, 1×/2×/4×, seek) — a pure consumer that never drives the sim. HP bars fold the stream to
live health; a scrolling event log; the app still prints the byte-identical `classraid` hash (the
one-engine-two-consumers check). *(Built programmatically; the DS primitives + one RPG `Theme` and the
2D tactical stage renderer are step 8 / M2 — this is the log + HP view.)*

### Step 10 — Wire the raid night ✅ first cut
The Godot app is now a **loop, not a demo**: load/create guild → **roster screen** → **Start Raid**
(project the persistent roster into combatants via the class factory, run the real engine) → **watch the
playback** → back → **Save** (atomic, to `user://saves/`). Verified headlessly by `RaidNightTests` (a
generated role-covered guild clears the Warden). Raids now **resolve into consequences** — `RaidResolver`
folds the fight into gold + XP/levels + **loot drops** + a saved `RaidSummary` (save chained to v3), so the
campaign grows and raiders gear up. *(Remaining for the full M1 floor: raid assignments, the textured
`Theme` + gear screen (step 8), and a `sim campaign` verb; then tag `v0.1-m1`.)*

## Rules (unchanged from M0)
- Every step ends green; small commits on step boundaries.
- Refactor commits keep golden hashes identical; deliberate behavior/schema changes re-bless them
  with the diff noted in the commit — never both in one commit.
- No content hardcoded: if adding content needs new code, the template or its runtime is missing —
  build that instead.
