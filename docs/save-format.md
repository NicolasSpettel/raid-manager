# Save Format & Migrations — `src/Game`

## The aggregate

All persistent state is one versioned object, owned and written exclusively by `src/Game`:

```ts
interface GuildSave {
  version: number;                  // integer, starts at 1, bumped by every breaking change
  createdAtIso: string;             // set once by game, never read by sim logic
  guild: { name: string; reputation: number; /* … */ };
  roster: RaiderRecord[];           // identity, level, gear, traits, talents, morale, contract
  economy: { gold: number; consumables: ItemStack[]; bank: Item[] };
  calendar: { day: number; schedule: ScheduledActivity[]; lockouts: LockoutState[] };
  history: {
    careers: Record<RaiderId, CareerDeltas>;   // folded from event streams (see below)
    raidLog: RaidSummary[];                    // compact per-raid results
    lastRaidStream?: CompressedEventStream;    // kept for replay/report, then compacted
  };
  settings: GameplaySettings;       // difficulty, autopause rules — NOT app/UI prefs
}
```

Rules:

- Nothing persistent is born inside a component. New persistent field ⇒ it's added here, in one
  place, with a version bump if the shape breaks.
- UI preferences (theme, panel layout) are NOT in the save — separate `AppPrefs` blob, separate
  versioning, so cosmetic churn never risks save migrations.
- The `GameState` autoload holds the aggregate and exposes mutation methods; scenes never write fields
  directly (enforced by convention + a test that the aggregate type is not exported mutably).

## Migration registry

DM1's cautionary tale: an ad-hoc `normalizeSavedRunState()` accumulated six hand-inlined
migrations (class renames, id remaps, stamina rescales, dedupe repairs) with `version` stuck at 1.
Here, from day one:

```ts
export const SAVE_VERSION = 1; // current

export const migrations: Array<{
  from: number;                       // migrates from → from+1
  note: string;                       // one line: what changed and why
  migrate: (save: unknown) => unknown;
}> = [];
```

Load pipeline:

1. Parse JSON → read `version` (missing/corrupt ⇒ recovery flow, never a crash).
2. Fold `migrations` from `save.version` to `SAVE_VERSION`.
3. validate against the *current* schema (validation only at this boundary — never in hot paths).
4. Hand the typed aggregate to the store.

**Fixture policy:** every time `SAVE_VERSION` bumps, a fixture save of the *previous* version is
frozen into `src/Game.Tests/fixtures/save-v{N}.json`. CI round-trips every historical fixture
through the full pipeline. A migration that loses data fails a field-presence assertion, not a
player.

## Storage adapters

```ts
interface StorageAdapter {
  load(): Promise<string | null>;
  save(blob: string): Promise<void>;   // atomic: tmp + rename, keep one .bak (desktop)
  exportToFile?(): Promise<void>;      // player-facing backup
}
```

- Godot `user://saves/` via `FileAccess` — single file (JSON or Godot resource), atomic writes
  (temp + rename), `.bak` sibling, Steam Auto-Cloud pointed at the folder. Serialization is C#
  (System.Text.Json or Godot's own), validated at the load boundary only.
- Writes are debounced (~500ms) + flushed on quit/`pagehide`. Save/export/import/reset all live
  in Settings from M1 — recovery UX is a floor feature, not a polish feature.

## Career history: fold, compact, discard

1. Raid ends → `game` folds the `CombatEvent[]` stream into `CareerDeltas` per raider
   (damage/healing totals, deaths, interrupts, mechanic fails, clutch moments) and a
   `RaidSummary` (kill/wipe, duration, MVP-style callouts for the narrative report).
2. The raw stream is kept as `lastRaidStream` (compressed) for the post-raid report and replay.
3. Starting the next raid overwrites it. Career data only ever grows by small deltas —
   saves stay small no matter how long the campaign runs.

Budget: aggregate < 2 MB for a multi-season campaign; `lastRaidStream` < a few MB compressed.
A CI test simulates 50 folded raids and asserts the serialized aggregate stays under budget.
