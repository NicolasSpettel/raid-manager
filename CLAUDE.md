# CLAUDE.md

Raid Manager — a commercial single-player game (Football Manager in an RPG world), **Godot 4 + C#**.
This file is the entry point Claude Code auto-loads; the real map is the wiki.

## Read first, every session

1. **[ARCHITECTURE.md](ARCHITECTURE.md)** — the project wiki: what exists, where it lives, what's
   decided, what's open. Start here and follow the link matching your task from its document map.
2. If you are a **coding session**, then read in order:
   [docs/m0-build-plan.md](docs/m0-build-plan.md) → [docs/BLUEPRINT.md](docs/BLUEPRINT.md) §4 (repo)
   & §10 (conventions) → [docs/engine-spec.md](docs/engine-spec.md) →
   [docs/testing-strategy.md](docs/testing-strategy.md).

## Non-negotiables (the floor)

- **Principle 0** — everything instantiates from a data template through a generic runtime; adding
  content is a data change, never a code change ([BLUEPRINT §10](docs/BLUEPRINT.md)).
- **Determinism** — the sim is a pure function of a seeded RNG. Banned in Engine/Content/Game:
  `System.Random`, `DateTime.Now`/`UtcNow`, `Guid.NewGuid` (Roslyn banned-API analyzer).
- **Boundaries** — `Engine ← Content ← Game ← {App, Sim}`, enforced by project references. Only
  `App` may reference Godot ([ADR-0001](docs/adr/0001-solution-and-boundaries.md)).
- **Every step ends green** — `dotnet build -warnaserror` + `dotnet test`. No red build, no
  `// TODO: fix determinism`, no half-shipped feature.

## Session protocol

1. Read ARCHITECTURE.md; follow the doc-map link for your task.
2. Before adding a system, check [docs/game-design.md](docs/game-design.md) — is it designed?
   Design first if not.
3. After any structural change, update ARCHITECTURE.md's Code map / Decisions **in the same commit**.
4. Never resolve an "open" item silently — ask the developer or record the decision in
   game-design.md's decision log with rationale.
5. ADRs are immutable once code depends on them; supersede, don't edit.

Build everything under `E:\raid-manager\`. Status: design complete on core systems, ready to
build **M0**. No code yet, deliberately.
