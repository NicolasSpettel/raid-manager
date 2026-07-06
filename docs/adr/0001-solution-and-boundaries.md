# ADR-0001: .NET solution with project-reference boundaries

**Status:** Accepted · July 2026 · (Revised from the original pnpm-workspace proposal after the
engine decision, [ADR-0008](0008-engine-godot-csharp.md). The *principle* — hard, structural
boundaries with the sim unable to import the UI — is unchanged; the mechanism is now .NET.)

## Decision

One .NET solution, `RaidManager.sln`, with these projects:

```
RaidManager.sln
  src/
    Engine/    net8.0 class lib — deterministic sim core. NO Godot reference. NO project deps.
    Content/   net8.0 class lib — registries/templates. → Engine
    Game/      net8.0 class lib — guild, roster, economy, worldgen, saves. → Engine, Content
    App/       Godot 4 C# project — UI scenes, themes, renderers. → Engine, Content, Game
    Sim/       net8.0 console app — headless golden/balance/campaign. → Engine, Content, Game
```

Dependency direction: `Engine ← Content ← Game ← {App, Sim}`. `App` and `Sim` never reference
each other.

## Rationale

- **`ProjectReference` is a compile-time hard wall.** `Engine.csproj` has no reference to Godot or
  to `App`, so engine code *cannot* call the UI or the engine runtime — it won't compile.
  This is **stronger** than the pnpm-workspace plan (and far stronger than lint rules): the
  boundary is enforced by the compiler, not by convention.
- **`Engine`/`Content`/`Game` are plain .NET libraries** — they run in `Sim` (console) and in
  unit tests headlessly, with no Godot in the loop. The determinism/testability the architecture
  demands falls out for free.
- **`Sim` calls the real `Game` loop**, so the DM1 mirror-bug class (a separate reimplementation
  of game rules) is structurally impossible — the same reason as before, now via a project ref.

## Enforcement & budgets

- Boundaries: the project-reference graph (the compiler). Optional `NetArchTest`/`ArchUnitNET`
  test asserts no forbidden namespace dependencies as a second net.
- Size budgets (Roslyn analyzer / editorconfig): types stay small; one responsibility per file;
  "split when you need 'and'." Same rule as before, C# tooling.
- Determinism guard: an analyzer/test bans `System.Random`, `DateTime.Now`, and unseeded RNG in
  `Engine`/`Content`/`Game` (the seeded PRNG lives in `Engine`).

## Consequences

- Solution-level structure is trivial to reason about; new devs/LLM sessions see the wall in the
  `.csproj` files.
- `Content` templates are Godot `Resource` subclasses where they need editor authoring, but must
  stay loadable headlessly (Godot C# resources can be constructed in code without the editor) so
  `Sim`/tests don't need a running Godot instance. Where headless purity matters most (the sim
  core), templates are plain C# records in `Engine`/`Content`, projected from Godot resources at
  the `App` boundary.

## Alternatives rejected

- **Single Godot project, everything in one assembly:** no wall; the sim would rot into the UI
  exactly like DM1's engine rotted into its app.
- **Lint/analyzer-only boundaries in one project:** erodes under pressure; compile-time refs don't.
