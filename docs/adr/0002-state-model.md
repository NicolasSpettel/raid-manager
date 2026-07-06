# ADR-0002: App state model (Godot) — supersedes the zustand proposal

**Status:** Accepted · July 2026 · (Replaces the original zustand ADR, which was React-specific
and mooted by the Godot engine decision, [ADR-0008](0008-engine-godot-csharp.md).)

## Decision

App state in the Godot `App` project follows Godot-native patterns, not a web state library:

- **One autoload singleton `GameState`** holds the single persistent `GuildSave` aggregate (the
  only writer of persistent state — the save-format rule, unchanged). It exposes typed accessors
  and **emits signals** on change; UI scenes subscribe to the signals they care about.
- **Transient UI state** (open panels, selection, playback position) lives in scene-local nodes
  or a separate `UiState` autoload — never mixed into the save aggregate.
- **Persistent-state mutations go through `GameState` methods**, which apply the change, append to
  the delta log (per [ADR-0007](0007-world-persistence-seed-plus-deltas.md)), and emit the signal.
  No scene writes the aggregate directly.

## Rationale

- This is the idiomatic Godot equivalent of "one store, many selective subscribers": signals give
  per-consumer updates without a UI scene re-rendering the world on unrelated changes — the same
  goal the zustand choice had (avoiding DM1's whole-tree re-render), achieved with the engine's
  own tools.
- Keeping the persistent aggregate in one autoload preserves the save-format invariant (one home
  for persistent state) and keeps the delta-log append in one place.

## Consequences

- The persistent/transient split is a hard rule: `GuildSave` (in `GameState`) vs UI state.
- Signals are typed and named per concern (e.g. `RosterChanged`, `GoldChanged`, `RaidTickAdvanced`)
  so subscribers stay narrow.
- The playback clock (combat replay) is `UiState`, driving folds over the precomputed event stream
  — the UI still never drives the sim ([engine-spec](../engine-spec.md)).
