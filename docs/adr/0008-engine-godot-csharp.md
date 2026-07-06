# ADR-0008: Engine — Godot 4 + C#, not web/Electron, not Unity

**Status:** **Accepted** · July 2026 (dev choice, after the web recommendation was correctly
rejected). Supersedes the earlier "provisional web stack" note in [BLUEPRINT §3](../BLUEPRINT.md)
and moots [ADR-0002](0002-state-model.md) (React-specific state).

## Decision

Build Raid Manager in **Godot 4 with C#**. The management UI is Godot `Control` scenes themed
with texture-based styles; the 2D combat replay is a Godot 2D scene; the deterministic sim,
content, and management logic are **plain C# class libraries with no Godot dependency**, so they
run headless in tests and the CLI. Ship as a native desktop executable via Godot's export.

## Context

Earlier this session the assistant recommended a web stack (TS + React + Electron) on grounds of
UI-density, LLM-competence, and a mature browser verification loop. The dev rejected it, correctly:
DM1 was already web and never achieved the tactile RPG feel — "a brown background isn't RPG" — and
they explicitly do not want a browser-game feel again. That is a product and vision constraint that
outranks the assistant's build convenience.

Facts checked: **Football Manager (FM25/26) runs on Unity (C# UI) over a C++ core**; historically a
custom C++ engine. So the genre reference itself is a real game engine, not web.

## Rationale

1. **Real native game, not a browser app.** Godot exports a standalone executable with genuine
   game feel — the non-negotiable requirement.
2. **Texture-native UI is the direct answer to the RPG-feel complaint.** Godot UI styling is
   `StyleBoxTexture` — ornate frames, parchment fills, nine-patch metal borders authored **once**
   as Theme resources and inherited by every control. Texture is first-class, not faked in CSS.
   This also structurally enforces the single-source-of-visual-truth that DM1 lacked (one Theme,
   not per-screen reinvention).
3. **Godot Resources *are* our template architecture.** `.tres` resources + `[Export]` fields are
   exactly Principle 0: a `CharacterTemplate` resource, a `createCharacter()` factory returning a
   `CharacterResource`. The data-driven design maps onto Godot natively, no impedance.
4. **C# fits the pure sim.** Statically typed, strong LLM training data, excellent for a large
   deterministic codebase. The sim/content/game are `net8.0` class libraries with **zero Godot
   references** — headless-testable and runnable from the CLI, exactly as the architecture requires.
5. **Lighter and freer than Unity.** No licensing, smaller/simpler runtime, 2D-first (ideal for
   the WCL-style replay). Unity is heavier and its UI Toolkit was FM's own pain point.
6. **LLM tooling is real and maturing fast.** godot-mcp servers, the Godot AI editor plugin
   (Claude Code ↔ live editor via MCP, ~150 ops), and agents like Ziva (scene manipulation,
   GDScript/C#, tests, **editor screenshots**) give the assistant a build-and-verify loop. Newer
   than the web Preview loop, but sufficient and improving.

## Why not the alternatives

- **Web/Electron:** produces the browser-game feel the dev rejected; CSS texture is a poor fit
  for tactile RPG chrome; DM1 is the evidence.
- **Unity:** what FM uses, biggest ecosystem — but heavier, commercial licensing, UI Toolkit
  weaker for dense data (FM's own struggle), overkill runtime for a 2D+database game.
- **Custom C++ (what FM historically did):** a 20-year investment; impossible for a solo dev + LLM.

## Consequences

- Repo becomes a **.NET solution** (`RaidManager.sln`) with project-reference boundaries
  (see [ADR-0001](0001-solution-and-boundaries.md)) — a *stronger* wall than the old pnpm plan.
- [ADR-0002](0002-state-model.md) (zustand) is moot; Godot state model replaces it (autoload
  singleton holding the save aggregate + signals; see BLUEPRINT §3).
- All spec docs' TypeScript snippets are **illustrative pseudocode to port to C#** — the *designs*
  (tick model, event stream, entity components, seed+delta, registries, factories) are
  language-agnostic and carry over 1:1. Ports happen as each part is built.
- Determinism note: anything golden-hashed uses integer/fixed-point math (no cross-platform float
  reliance); C# seeded PRNG (PCG/xoshiro) implemented in `Engine`.
- Art assets (existing class icons, etc.) are PNG/WebP textures — reused directly.

## If this proves wrong

The pure `Engine`/`Content`/`Game` C# libraries are UI-agnostic; only the Godot `App` project is
engine-specific. Swapping UI later (or even engines) leaves the sim/logic untouched — the same
insurance the layered design always gave us.
