# M2 Build Plan — Watchable Combat

> M1 made combat *playable and legible* (the log view). M2 makes it *watchable*: a 2D tactical stage
> where you see tokens fight, HP drain, and telegraphs flash — a **second pure consumer of the same
> event stream** (BLUEPRINT §8, §11, ADR-0005). The floor is structural: **the log view and the stage
> view fold the identical `SimResult`**, and the renderer sits behind a clean seam.

## Definition of done (strong floor)

1. From a raid, you can switch between **Log view** and **Stage view**; both replay the *same*
   `SimResult` on the *same* playback clock — neither drives the sim.
2. The stage shows every combatant as a token (side colour, size by kind), HP folding live from the
   stream, death, and a telegraph flash when a mechanic fires.
3. All M0/M1 gates stay green; no golden re-bless (the stage is a consumer, it changes no engine output).

## Build order

### Step 1 — v0 stage board (Godot, App-computed layout) — **in progress**
`StageView`: a `Control` that folds the event stream to a tick and draws tokens (`_Draw`: circles sized
by kind, filled by HP fraction, dark when dead) at role-based arena positions computed from the view
size — no engine change, no golden churn. Telegraph flash on recent `MechanicEvent`. Playback controls
(play/pause, 1×/2×/4×, seek) + Back + a **Log ↔ Stage toggle** wired through the coordinator, both views
loaded with the same `(SimInput, SimResult)`. *(Structural floor; visual polish needs the dev's eyes.)*

### Step 2 — positions in the engine (fixed-point 2D) — later
Promote layout into the sim: `Position` on combatants, spawn positions, `move` events (running out of
AoEs), and telegraph **geometry** (circle/cone/ring in integer math) so `spreadDamage` becomes spatial
(stand in it → hit). This is M1 step 5, deferred until the renderer that consumes it exists — now it does.
A schema addition (spawn/move events) → deliberate golden re-bless.

### Step 3 — richer stage pass — later
Sprites/particles/animation on the same renderer interface and the same stream (ADR-0005); the v0 board
and the richer pass both pass the same replay tests.

## Rules (unchanged)
Every step ends green; the stage is a pure fold over the stream (never drives the sim); commit per step.
