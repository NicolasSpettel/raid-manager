# ADR-0005: Combat stage behind a renderer interface (Godot 2D)

**Status:** Accepted · July 2026 · (Revised after [ADR-0008](0008-engine-godot-csharp.md): both
implementations are now Godot 2D scenes, not DOM/PixiJS. The *interface* decision — a swappable
renderer consuming the event stream — is unchanged.)

## Decision

Combat visualization is implemented behind a small C# interface, consumed identically by the text
log, meters, and stage:

```csharp
interface ICombatStageRenderer {
  void LoadEncounter(EncounterScene scene);
  void ApplyEvents(IReadOnlyList<CombatEvent> events, int tick);
  void Seek(int tick);
}
```

Two planned implementations, **both Godot 2D scenes**:

1. **v0 (M2): tactical board** — simple `Node2D` tokens (class-colored circles + icons), telegraph
   shapes as drawn primitives, movement via tweens, unit frames. Cheap, readable for 10-man.
2. **Richer pass (M4): the FM-style watchable battle** — sprites, particle telegraphs, cast bars,
   floating combat text, camera, on the same interface and same event stream.

Both consume the identical event stream. Full 3D is explicitly rejected. (Because the stage is a
pure consumer of `CombatEvent[]` + positions, the richer pass is a scene-local upgrade — the "no
rewrite" property holds within Godot.)

## Context

The developer's wish: "seeing units casting spells, healing, tanks tanking, bosses fighting would
be insanely cool" — Football-Manager-style, not full 3D. The stack was deliberately left open at
blueprint time. DM1's combat view was text-log-first with light CSS FX (`combat-fx.tsx`), which
players read as stale.

## Rationale

- **The contract, not the visuals, satisfies "no rewrite later".** Because renderers are pure
  consumers of `CombatEvent[]` + positions (ADR-0004), upgrading the v0 board to the richer stage
  is a scene-local change; nothing upstream knows.
- **Simple board first** proves the interface with two consumers (log + board) at minimal cost and
  ships watchable-ish combat one milestone early.
- **Richer stage later:** 25 raiders + boss + adds + projectiles + floating text ≈ 60–100
  animated objects — Godot 2D (Node2D/sprites/particles) handles this comfortably; the v0 board
  is the same interface at lower fidelity.
- **No 3D:** asset production cost is the solo-dev killer; readability, not fidelity, is the pillar.

## Consequences

- Renderer tests: both implementations replay the same golden stream; a headless assertion
  checks final scene state matches event-implied state (positions, deaths, auras).
- The `EncounterScene` (arena layout, spawn points) is authored per encounter in `content` —
  scene data rides in the encounter registry row like everything else.
- Playback clock (pause / 2× / seek) lives outside the renderer; `seek(tick)` must be cheap
  (renderers rebuild from the nearest keyframe fold — `game` provides periodic state snapshots
  folded from events).

## Alternatives rejected

- **Rich stage from day one:** front-loads art/particle work before the sim exists; the v0 board
  de-risks the interface and event contract first.
- **3D:** art cost, camera complexity, zero pillar support.
