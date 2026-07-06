# ADR-0003: Fixed integer ticks, not turns

**Status:** Accepted · July 2026 · **Highest-risk decision in the blueprint — read the fallback.**

## Decision

Combat time advances in fixed integer ticks at **10 ticks per second of battle time**. All
durations, cooldowns, cast times, and timeline triggers are integer tick counts. There is no
float time anywhere in the engine. The sim runs to completion instantly regardless of tick
resolution; playback speed is purely a UI concern.

## Context

DM1 was turn-based: one loop iteration = everyone acts once. That was simple and served 5-man
fights, but raid encounters are authored in *time* — "tank swap at 0:45", "AoE every 20 seconds",
"interrupt the 2.5s cast" — and Football-Manager-style watchable playback needs cast bars,
overlapping timers, and movement, none of which map onto discrete everyone-acts turns.

## Rationale

- Boss timelines and mechanic scheduling become plain data (`atTick`, `everyTicks`) instead of
  turn-arithmetic approximations.
- Cast times, HoT ticks, travel times, and stagger windows all express naturally.
- Determinism is preserved: integer tick counter, integer durations, seeded RNG. Tick resolution
  does not touch float math.
- Playback gets smooth pacing for free: the renderer interpolates *between* event timestamps;
  the engine never runs in real time.

## Risks & mitigations

- **Complexity:** more moving parts than turns (simultaneity, action scheduling, GCD-equivalents).
  Mitigation: M0 ships a dummy fight proving the tick loop + event contract before any content
  exists; mechanic archetypes hide scheduling from content authors.
- **Perf:** more loop iterations than turns. Mitigation: per-tick work is bounded (dirty flags,
  cached effective stats, no allocation in the hot loop); most ticks process only scheduled
  actions, not all combatants.

## Fallback (recorded deliberately)

If tick-level scheduling proves too fiddly during M0/M1, coarsen to **1 tick = 1 second**. The
event contract, entity model, and all downstream consumers are unchanged; the model degenerates
toward DM1-style turns without an architecture change. The fallback is a constant, not a rewrite.

## Alternatives rejected

- **Turns (DM1):** cannot express raid timelines or watchable playback without faking sub-turn time.
- **Continuous float time:** float drift across machines breaks byte-identical determinism —
  non-negotiable.
