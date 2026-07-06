# Raid Manager (working title)

Football-Manager-in-an-RPG-world: manage a raiding guild racing to clear raids in a living,
offline world. Sequel to Dungeon Manager, built in **Godot 4 + C#**.

**Start here:** [ARCHITECTURE.md](ARCHITECTURE.md) — the project wiki (what's where, what's decided).

## Status — M1 vertical slice playable (`v0.1-m1`)

A full management loop runs end to end: create a guild → roster → **pick a boss and difficulty** →
**watch the fight play out** (HP bars, combat log, play/pause/speed/seek) → earn **gold, XP, levels, and
loot** → **save**. Everything is deterministic and data-driven — a new class, spell, boss, or item is one
data row. **62 tests** green; CI runs on every push.

## Run it

- **The game (Godot):** open `src/App/project.godot` in Godot 4.7 (.NET/Mono) and press **F5**.
- **Headless combat:** `dotnet run --project src/Sim -- run classraid --seed 1`
- **Headless campaign (balance tool):** `dotnet run --project src/Sim -- campaign --boss ashen_king --difficulty heroic --raids 10`
- **Build + test:** `dotnet build RaidManager.sln -warnaserror` · `dotnet test`

## Solution layout

`src/Engine` (deterministic sim core, no Godot dep) · `src/Content` (classes, abilities, bosses, items,
difficulties — all data) · `src/Game` (guild, saves + migrations, progression, loot) · `src/App` (Godot UI) ·
`src/Sim` (headless CLI). Boundaries are compile-time project references; the engine cannot see Godot.

## Map

- What the game *is*? → [docs/game-design.md](docs/game-design.md)
- How it's structured? → [docs/BLUEPRINT.md](docs/BLUEPRINT.md)
- Build plans → [docs/m0-build-plan.md](docs/m0-build-plan.md) (done) · [docs/m1-build-plan.md](docs/m1-build-plan.md)

Proprietary — see [LICENSE](LICENSE).
