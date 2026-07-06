# M0 Build Plan — the Foundation milestone

> The step-by-step for the first coding session. M0 builds the **skeleton + the deterministic
> core + the quality gates** — no gameplay content. Its whole point is that every professional
> guarantee (determinism, boundaries, tests, no-debt) is enforced *from the first commit*, so
> nothing built later can rot the floor. Execute in order; each step is small and verifiable.
> Specs referenced (don't duplicate them): [BLUEPRINT §4](BLUEPRINT.md), [engine-spec](engine-spec.md),
> [testing-strategy](testing-strategy.md), [ADR-0001](adr/0001-solution-and-boundaries.md).

## Definition of done (the strong floor — all must be true)

1. `dotnet build -warnaserror` is green across the solution.
2. `dotnet test` is green: a **determinism** test (same seed ⇒ identical event stream; different
   seeds ⇒ different) and a **golden** test (event-stream hash pinned) both pass.
3. `dotnet run --project src/Sim -- run dummy --seed 1` prints the dummy-fight event log headless.
4. The **Godot `App`** runs the *same* `Engine.SimulateEncounter` and shows the **byte-identical**
   event stream (compare hash to the Sim's).
5. Boundary test (NetArchTest) proves `Engine` has zero dependency on Godot/`App`.
6. Banned-API analyzer fails the build if `System.Random`/`DateTime.Now` appear in Engine/Content/Game.

If all six hold, M0 is done and M1 can stack on a floor that cannot silently accrue debt.

## Repo location

Everything under **`E:\raid-manager\`** (the `docs\` folder is already there). Root gets
`RaidManager.sln`, `Directory.Build.props`, `.editorconfig`, `.gitignore`, `README.md`, and `src\`.

## Solution structure (create exactly this)

```
RaidManager.sln
Directory.Build.props        # nullable, warnaserror, analyzers — applies to ALL projects
.editorconfig                # style + analyzer severities
src/
  Engine/          Engine.csproj            (net8.0, no deps)
  Content/         Content.csproj           → Engine
  Game/            Game.csproj              → Engine, Content
  Sim/             Sim.csproj  (console)    → Engine, Content, Game
  App/             project.godot + App.csproj (Godot 4.7 C#)  → Engine, Content, Game
tests/
  Engine.Tests/    → Engine        (xUnit)
  Architecture.Tests/ → Engine, Content, Game, (ref App csproj) (xUnit + NetArchTest)
```

Boundaries are **`ProjectReference`s** — the compiler is the wall (ADR-0001). `App` is the only
project referencing Godot.

## Build order (each step ends green)

### Step 1 — Quality gates first (before any game code)
- `Directory.Build.props`: `<Nullable>enable</Nullable>`,
  `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<LangVersion>latest</LangVersion>`,
  `<AnalysisLevel>latest-Recommended</AnalysisLevel>`, `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`.
- Add `Microsoft.CodeAnalysis.BannedApiAnalyzers`; a `BannedSymbols.txt` in Engine/Content/Game
  banning `System.Random`, `System.DateTime.get_Now`, `System.DateTime.get_UtcNow`,
  `System.Guid.NewGuid` → determinism enforced by the compiler.
- `.editorconfig` with file-scoped namespaces, `var` rules, and analyzer severities as errors.
- Empty solution + empty projects wired with references. `dotnet build -warnaserror` green.

### Step 2 — Deterministic primitives (`Engine`)
- `SeededRng` — a small **PCG/xoshiro256\*\*** implementation (pure, integer-based; NOT
  `System.Random`). Required constructor seed, no default. Methods return deterministic values.
- `Tick` conventions: integer ticks, `TicksPerSecond = 10` (or 4 if 0.25s is confirmed) as a
  const. No float time anywhere.
- Unit test: same seed ⇒ same sequence; determinism guard.

### Step 3 — The event stream contract (`Engine`)
- `CombatEvent` as a **sealed record hierarchy** (the union in [engine-spec §6](engine-spec.md)):
  start with `EncounterStart`, `Damage`, `Death`, `EncounterEnd` (enough for the dummy fight;
  append-only later). Each carries `Tick` and stable `CombatantId`s.
- A canonical **serializer + stable hash** (e.g. FNV/SHA over a deterministic text form) — this
  is what golden tests pin.

### Step 4 — The dummy fight (`Engine`)
- `SimulateEncounter(input) → SimResult { Events, Outcome, Seed, EngineVersion, EventSchemaVersion }`.
- Dummy: one attacker vs one dummy target; attacker swings every N ticks for fixed integer damage;
  emit `EncounterStart`, `Damage`×k, `Death`, `EncounterEnd`; deterministic from the seed.
- This exercises: tick loop, event sink, the `SimContext`-as-parameter rule (no closures over
  state — engine-spec §5), the result shape.

### Step 5 — Golden + determinism tests (`Engine.Tests`)
- Golden: run dummy at seed 1, assert the stream hash equals the pinned value (bless once).
- Determinism: seed 1 twice ⇒ identical; seeds 1..10 ⇒ 10 distinct hashes.

### Step 6 — The Sim CLI (`Sim`)
- Minimal arg parse: `run dummy --seed N` → run `Engine.SimulateEncounter`, print the event log +
  the stream hash. This is the headless proof and the future home of golden/probe/campaign verbs.

### Step 7 — Architecture tests (`Architecture.Tests`)
- NetArchTest: `Engine` depends on nothing in the solution; no namespace cycles; `App` is the only
  Godot-referencing project. Fails loudly on a boundary violation.

### Step 8 — The Godot `App` shell
- A minimal Godot 4.7 C# project referencing `Engine`. On run, it calls the *same*
  `Engine.SimulateEncounter(seed: 1)`, computes the stream hash, and displays the event log +
  hash in a `RichTextLabel`.
- **Verify via the godot-mcp `run_project` + `get_debug_output`**: the hash printed by Godot must
  equal the hash printed by `Sim` — proving one engine, two consumers, byte-identical (the M0 floor).

### Step 9 — CI gate + tag
- A `ci` script / GitHub Actions running `dotnet build -warnaserror` + `dotnet test`. Even with no
  remote yet, this is the documented gate. Commit, tag `v0.0-m0`.

## Rules for the coding session (keep the floor clean)

- **No file exceeds its budget** (analyzer): if a type needs "and" to describe, split it.
- **No content is hardcoded** — but M0 has almost no content; the dummy fight is the *only*
  hand-built thing, and it's a test fixture, not shipped content.
- **Every step ends green.** Never move on with a red build/test. No `// TODO: fix determinism`.
- **Determinism is sacred:** if a golden hash changes, it was intentional (bless deliberately) or
  it's a bug — never blindly re-bless.
- Small commits per step; the plan's steps are the commit boundaries.
