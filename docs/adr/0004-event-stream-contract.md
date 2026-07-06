# ADR-0004: The combat event stream is the engine's only output

**Status:** Accepted · July 2026

## Decision

`simulateEncounter` returns `SimResult = { events: CombatEvent[], seed, engineVersion,
eventSchemaVersion }` and nothing else of substance. Every downstream feature — combat log, stage
renderer, damage meters, wipe analysis, career history, post-raid narrative report, rival-guild
results — is a pure fold over the event stream. The event union is versioned and **append-only**:
new event kinds may be added; changing an existing shape is a schema migration.

Every event carries: stable entity ids (source/target), integer tick timestamp, and **position
data from day one** (coarse lane/ring coordinates in v0).

## Context

DM1 converged on this shape late: its `TurnEvent[]` stream ended up feeding the log, floating
combat text, damage meters, and barks — the most leveraged data structure in the game — but it
was grown, not designed. Facts consumers needed but events lacked (positions, stable ids early
on) were bolted on or re-derived, and re-derivation is where drift lives.

## Rationale

- **One contract, many consumers** is what makes future features cheap: a career page or a rival
  guild's raid result is a new fold, not a new engine capability.
- **Positions from day one** is the single most important enabler of the watchable-combat wish.
  Retrofitting positions into thousands of authored events and every archetype *is* the rewrite
  scenario this blueprint exists to avoid. *(Amended 2026-07-06: originally coarse lane/ring
  coordinates; upgraded pre-code to fixed-point 2D arena coordinates once the WCL-replay-style
  combat view became the locked product direction — see engine-spec §7.)*
- **Consumers never re-derive.** If the UI needs a fact ("that heal was a crit on a dying
  target"), the engine emits it. Re-derivation in two places was DM1's drift engine.
- Golden tests hash this stream — the contract doubles as the regression net.

## Consequences

- Streams are sizeable (~60 combatants × thousands of ticks). Budget: one raid serializes to
  < a few MB. Discipline: emit *meaningful* events, never per-tick state dumps; `game` compacts
  streams into career deltas after the post-raid report.
- Schema changes are deliberate acts (version bump + migration note), not drive-by edits.

## Alternatives rejected

- **Rich return object (final state + stats + events):** consumers immediately depend on
  pre-chewed aggregates that drift from the events; aggregates are folds in `game`, not engine
  outputs.
- **Positions later "when the stage needs them":** the named rewrite trap.
