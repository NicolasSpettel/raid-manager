# Content Authoring — `src/Content`

> **Language: C# (net8.0)**, with editor-authorable templates as Godot `Resource` subclasses.
> Snippets below are design pseudocode to port. Each entity type has a `createX()` factory
> (Principle 0, BLUEPRINT §10) that returns a valid instance from its template, ready to mutate.

Cookbooks for adding content. The design goal: **adding content feels like filling in a form**,
and a half-filled form fails CI with a message naming the missing row.

Principles (from DM1's content-pipeline retrospective):

1. **One row = one fact.** Numbers live in exactly one place; engine, AI, and UI all read the row.
2. **Tooltips are generated.** Every player-facing description is a template interpolating the
   row's own fields. Hand-written numeric prose is banned — DM1's #1 bug class was tooltip/engine
   drift (tooltip "187%", engine 2×).
3. **Archetype at ≥2 users.** Generic interpreters cover the common shapes; one-off behavior goes
   in a `custom` handler *registered by id* (name-string matching is banned).
4. **Registries are typed records with completeness tests.** `Record<Id, Row>` + a test that
   every declared class has kits, every ability has an implementation path, every encounter has
   phases/timeline/scene.

## Registry inventory

| Registry | Row owns | Interpreted by |
|---|---|---|
| `classes` | identity, roles, resource type, base stats, spec list | game (roster gen), UI |
| `specs` | rotation priority refs, role, mastery hook | engine role policies |
| `abilities` | numbers, effect archetype ref, cost/cooldown/cast ticks, aiPriority, tooltip template | engine + UI + AI |
| `talents` | tree position, ability mods or grants (by ref) | engine (stat/ability folds), UI |
| `traits` | personality effects (combat mults, morale hooks, execution-profile mods), tooltip template | game + engine via executionProfile |
| `encounters` | scene, enemies, phases, timeline, enrage, tuning (see engine-spec §8) | engine mechanic runtime |
| `items` | slot, budget curve ref, affix pool refs, tooltip template | game (loot gen), UI |
| `affixes` | stat grants, proc refs, tooltip fragment | game + engine |

## Cookbook: add an ability

```ts
// src/Content/Abilities/Pyromancer.cs  (illustrative; snippet is TS-flavored pseudocode)
defineAbility({
  id: "pyro.living_flame",
  class: "pyromancer", spec: "inferno",
  castTicks: 15,                 // 1.5s
  cooldownTicks: 0, gcd: true,
  cost: { resource: "mana", amount: 300 },
  effect: { archetype: "nuke", school: "magic", coeff: 1.85, critBonus: 0.1 },
  aiPriority: { weight: 60, conditions: [{ targetAuraMissing: "pyro.ignite" }] },
  tooltip: "Hurls living flame, dealing {coeff:%} spell power as {school} damage. +{critBonus:%} crit chance.",
});
```

That's the whole task. The engine resolves `archetype: "nuke"` through the generic effect
runtime; the UI renders `tooltip` by interpolating the same row; the drift test walks every
rendered tooltip and asserts each `{field}` resolved from the row (unresolvable token or stray
hand-typed number in a tooltip = red CI).

**If the ability does something no archetype covers** (< 2 users): keep the row, point effect at
a custom handler, implement it next to the registration:

```ts
effect: { custom: "pyro.phoenix_rebirth" },
// src/Content/Abilities/Custom/PhoenixRebirth.cs
registerCustomAbility("pyro.phoenix_rebirth", (ctx, cast) => { /* engine SimContext verbs only */ });
```

## Cookbook: add an encounter

One new file exporting one `defineEncounter({...})` row (shape in engine-spec §8): scene, enemy
spawns, phases, mechanic timeline, enrage, tuning. Register it in the tier index (single line,
and the completeness test tells you if you forget). Golden fixture is auto-generated on first
`sim golden --update encounters/<id>` and reviewed like code.

**Definition of done for a boss (M1 floor):** one new file + one index line. If you touched a
third file, the archetype set is missing something — file that instead.

## Cookbook: add a trait

```ts
defineTrait({
  id: "hothead",
  name: "Hothead",
  category: "personality",
  combat: { damageDoneMult: 1.06, mechanicFailChanceAdd: 0.04 }, // folded into executionProfile
  morale: { onBench: -2, onWipe: -3 },
  conflictsWith: ["stoic"],
  tooltip: "+{combat.damageDoneMult:%d} damage done, +{combat.mechanicFailChanceAdd:%} chance to fail mechanics. Hates the bench.",
});
```

No string-matching on trait ids inside multiplier functions (DM1 pattern to avoid) — traits
declare structured effects; `executionProfile` and the morale system fold them generically.

## Cookbook: add a class

1. `defineClass` row (identity, resource, base curves, spec ids).
2. One file per spec: `defineSpec` + its `defineAbility` rows + talent rows.
3. Run the registry completeness test — it enumerates exactly what's missing (kit for role,
   rotation priorities, tooltip fields) until the class is whole.

## Tooltip template rules

- Interpolation tokens reference row fields: `{coeff:%}` (percent), `{castTicks:s}` (seconds),
  `{cost.amount}`.
- Formatters live in one module shared by UI and drift test.
- Free prose is allowed *around* tokens ("Hates the bench") — flavor is human, numbers are not.
- The drift test fails on: unresolved tokens, digits in tooltip prose that aren't tokens
  (allowlist for genuinely non-mechanical numbers, used sparingly).

## Balance data vs content data

Tuning multipliers (per-difficulty, per-tier scaling curves) live in dedicated `tuning` blocks —
never inline in ability coefficients. Balance passes edit tuning blocks and re-bless goldens;
content passes add rows. Different commits, per the conventions in BLUEPRINT §10.
