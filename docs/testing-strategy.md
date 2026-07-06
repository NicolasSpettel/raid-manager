# Testing & Tooling Strategy

DM1's highest-ROI tooling, redesigned in: golden streams, drift tests, registry completeness,
and a headless campaign runner — this time with **no mirror** (the runner imports the real game
loop; DM1's 2,843-line sim mirror caused rule-drift bugs by construction).

## CI gates — all green from commit 1

```
dotnet build -warnaserror     # C# nullable + analyzers (no unseeded RNG/DateTime.Now in Engine/Content/Game)
dotnet test                   # xUnit: unit + registry + drift + migration fixtures + golden + NetArchTest boundaries
dotnet run --project src/Sim -- golden   # event-stream hashes (also run in test)
```
(Tooling is the .NET CLI; boundary/cycle checks are compile-time project refs + a NetArchTest
suite. Commands below written as `sim <verb>` are `dotnet run --project src/Sim -- <verb>`.)

A red gate blocks merge. There is no "we'll fix CI later" phase — M0's floor includes all gates.

## 1. Golden tests (part of `dotnet test`; also `sim golden`)

- For each (encounter fixture, raid fixture, seed): run `simulateEncounter`, hash the serialized
  event stream, compare to the blessed hash. A small set of *full-stream* snapshots (not just
  hashes) for the flagship fixtures makes diffs reviewable when they change.
- **Two workflows, never mixed** (BLUEPRINT §10): refactor commits keep hashes identical; balance
  commits re-bless via `sim golden --update` with the diff summarized in the commit body.
- Determinism guards alongside: same seed twice ⇒ identical stream; 10 different seeds ⇒ 10
  different streams (catches accidental rng bypass); run under node AND worker (same hash) to
  catch environment leaks.

## 2. Drift tests

- Render every tooltip/guide template against its registry row. Fail on: unresolved token, any
  digit in prose that isn't a token (small explicit allowlist).
- DM1's version of this caught a live bug on first run; it is the cheapest high-value test in
  the suite.

## 3. Registry completeness tests

Every registry ships a completeness spec: class ⇒ specs ⇒ rotation + kit; ability ⇒ resolvable
effect (archetype or registered custom handler); encounter ⇒ scene + phases + timeline + tuning +
golden fixture; trait ⇒ structured effects only. Failure messages name the missing row — adding
content is guided by red tests.

## 4. Migration fixtures

Every historical save version round-trips through the migration fold + schema validation
(save-format.md). Plus the size-budget test: 50 folded raids, aggregate under budget.

## 5. `src/Sim` — the headless harness

One CLI, importing engine/content/game directly:

```
sim run <encounter> --raid fixtures/10man-balanced --seed 1 [--profile] [--trace]
sim golden [--update pattern]
sim probe <tier>        # wall-probe: canonical-power beatability sweep, the balance ground truth
sim campaign --days 60 --policy greedy --seeds 200   # full campaign via Game.RunDay()
sim report              # report: win rates, class tables, death causes, economy curves
```
(`sim <verb>` = `dotnet run --project src/Sim -- <verb>`.)

- **Campaign bot:** scripted greedy policy first (roster, gear, schedule heuristics); a learned
  policy is an M3+ option, not a dependency. Because the bot calls `game.runDay()`, every rule
  change is automatically in the bot — the mirror-bug class is extinct.
- **Tuning discipline** (DM1 lesson, verbatim): never tune from bot behavior alone; the
  wall-probe's canonical-power numbers are ground truth, the bot finds exploits and walls.
- `--profile` from M0: tick-loop timing histogram against the perf budget (engine-spec §10), so
  a 25-man perf regression is a number in CI output, not a player report.

## 6. Renderer replay tests (M2+)

Both stage renderers replay flagship golden streams and assert final scene state — positions,
alive/dead, aura pips — matches the event-implied state (via Godot's headless/`--headless` scene
run, or by asserting the renderer's derived model without a window). The log view asserts its fold
count against the stream. This is what "renderer is a pure consumer" means when it's tested rather
than promised.

## 7. What we deliberately don't test

- No snapshot tests of styled component markup (churn without safety).
- No unit tests re-proving what golden streams already pin — unit tests are for tricky pure
  functions (rounding shapes, migration folds, template formatter), not for re-simulating combat.
