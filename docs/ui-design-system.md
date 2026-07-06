# UI Architecture & Design System — `src/App` (Godot 4, C#)

DM1's top visual complaint, in the developer's words: "we use different frames for every part."
And every screen existed twice (Desktop + `Mobile*`). Both are banned here by structure, not
discipline.

## 0. The visual-drift problem (DM1's real failure) and its fix

DM1's visuals diverged because **different LLM sessions working on different screens each
reinvented styling** — no enforced single source of visual truth. This is the #1 UI risk for an
LLM-built project and it is solved structurally, not by hoping:

1. **One Godot `Theme` resource is the ONLY styling source** (§1). Screens use themed `Control`
   nodes and shared primitive scenes — they never set colors/styleboxes inline. An inline style
   override in a screen scene is a review-fail.
2. **A living Component Gallery scene is the visual source of truth.** One always-runnable Godot
   scene renders every theme type, primitive scene, and composite in every state. **Every UI
   session — human or LLM — opens the gallery first and composes from it; it never invents a new
   look.** This is Principle 0 (template-instantiation) applied to UI: screens are *assembled
   from* the gallery's parts, not hand-styled.
3. **Screenshot verification against the gallery.** The assistant's build→screenshot loop (Godot
   AI / godot-mcp editor screenshots, [ADR-0008](adr/0008-engine-godot-csharp.md)) catches drift:
   if a new screen's controls don't match gallery theme types, it's visible immediately.
4. **Aesthetic is locked in the `Theme` + palette resource** (below), so "make it look
   futuristic" — the LLM default — is not reachable without editing the theme resource, a
   deliberate, reviewed act.

## 0b. Art direction [LOCKED: RPG, not futuristic]

**Fantasy-RPG interface, explicitly NOT the sleek/glassmorphic/neon "futuristic" look LLMs
default to.** Parchment, aged paper, dark iron and wood, gold-leaf accents, inked serif
headings, tooled-leather and metal frame chrome, wax-seal/heraldry motifs. Think an ornate guild
ledger and a medieval tournament board, not a sci-fi dashboard. The tokens (§1) encode this
palette and the frame/border treatments so the whole app inherits it; individual screens cannot
drift toward the default because they never author raw style. Art assets (portraits, frame
textures, icons) are sourced/generated to this direction — a content task tracked separately.

## 1. Design system first

`App`'s theme layer is built **before any screen** and is the only place visual style lives:

- **The `Theme` resource + a palette/metrics resource:** type scale (serif display for names,
  mono for numbers), palette (parchment-and-iron base + role/class accents + rarity colors),
  and — the RPG payload — **`StyleBoxTexture` styleboxes** for every control type (panels,
  buttons, tooltips, tabs) using aged-parchment/tooled-metal/wood textures with nine-patch
  borders. Texture and ornament live here, once.
- **Primitive scenes:** `Frame` (THE panel chrome — every boxed region uses it, theme-type
  variations not forks), `Panel`, `ListRow`, `StatBar`, `Chip`, `Tooltip`, `Modal`, `Tabs`,
  `DataTable`, `EmptyState` — reusable `Control` scenes themed by the one Theme.
- **Composite scenes:** `RaiderCard` (one scene, size variants — not per-screen forks),
  `ItemCard`, `EventFeedRow`.
- **Raider page layout reference — FM's player overview screen** (dev-provided screenshot,
  Enzo Fernández/FM23): attributes in three labeled columns with 1–20 color-coded values (ours:
  **Execution / Mental / Condition** groupings), a radar/polygon summary top-left, per-position
  star ratings bottom-left (ours: per-role stars), right rail with value estimate + contract +
  personality + media description + traits, and tabs for Contract / Transfer / Development /
  Reports / Comparison / History. This one screen is the depth benchmark: a raider must feel
  this *knowable*.

**Rules:**
- Screens compose primitive/composite scenes themed by the one `Theme`; an inline stylebox/color
  override in a screen scene is a review-fail.
- A new visual pattern is added to the theme + a primitive scene first, then used — never inlined.
- Primitives own their own layout adaptivity (a `Panel` collapses, a `DataTable` reflows) via
  Godot containers; no forked screens. (PC-only, so "responsive" here means window-resize/DPI, not
  mobile.)

## 2. Screen inventory — mapped from Football Manager's sidebar

FM's screen list (home, inbox, squad, squad planner, dynamics, tactics, data hub, staff,
training, medical centre, schedule, competitions, scouting, transfers, club info, club vision,
finances, dev. centre) is the genre's proven information architecture. Our mapping:

| FM screen | Our screen | Contents | Milestone |
|---|---|---|---|
| Home | **Home** | next-action strip, headlines, next raid countdown, quick continue | M1 |
| Inbox | **Inbox** | all events/mail/negotiation threads (game-design §11) | M1 |
| Squad | **Roster** | 10–60 raiders: sortable table + RaiderCard detail pane | M1 |
| Squad Planner | **Roster Planner** | role-depth chart per star rating, contract-expiry timeline, aging outlook | M3 |
| Dynamics | **Guild Dynamics** | morale map, cliques/mentorships, loot-drama temperature | M4 |
| Tactics | **Raid Planner** | comp ("formation"), posture, assignments, spot rules | M1 |
| Data Hub | **Log Center** | WCL-style analysis across pulls/weeks (research/boss-design-reference §2) | M3 |
| Staff | **Staff** | co-manager, analyst, scouts, trainers — hire/assign (§12) | M3 |
| Training | **Training** | attribute assignments, mechanic drills, role retraining | M2 |
| Medical Centre | **Infirmary** | injuries, burnout meters, rest planning | M3 |
| Schedule | **Calendar** | releases, Monday resets, raid nights, holidays, day-slot planner (4×2h) | M1 |
| Competitions | **The Race** | leaderboards (world/region/tier), rival tracker, season history | M2 |
| Scouting | **Scouting** | briefs to scouts, shortlists, fog-of-war player reports | M3 |
| Transfers | **Trade Market** | search, offers, async negotiation threads (§8b) | M3 |
| Club Info | **Guild Profile** | crest, history, prestige, rivals | M2 |
| Club Vision | **Board Room** | expectations, standing, budget requests, contract | M2 |
| Finances | **Finances** | wage bill, budgets, revenue streams, projections (§9b) | M2 |
| Dev. Centre | **Academy** *(if youth intake lands)* | newcomer pipeline | post-1.0? |
| — (stadium) | **Guild Hall** | venue upgrades & returns (§9c) | M3 |
| — | **Raid Night** | live playback: stage + log + meters + unit frames, pause/2×/seek | M1/M2 |
| — | **Post-Raid Report** | Summary → Deaths → Timeline → Players tab order; Problems view = analyst | M2 |

One sidebar, FM-style, grouping these; every screen answers "what should I do next?"
(anti-design principle #7, game-design §14).

## 3. State management

- Godot-native ([ADR-0002](adr/0002-state-model.md)): a `GameState` autoload holds the `GuildSave`
  aggregate + mutation methods (the only persistent-state writer) and emits **signals**; a
  `UiState` autoload/scene-local state holds panels, selection, the playback clock.
- Scenes subscribe to the narrow signals they need; a UI update caused by an unrelated signal is a
  bug. Persistent mutations go through `GameState` methods (which append the delta + emit).
- Stable identity keys everywhere: entity ids, event `(t, seq)` — never list indexes (DM1's
  index-keyed combat log re-rendered rows on every playback window slide).

## 4. Combat playback pipeline

```
RaidSetup ──► background thread: SimulateEncounter() ──► SimResult (full event stream)
                                                        │
                    ┌───────────────┬───────────────────┼──────────────┐
                    ▼               ▼                   ▼              ▼
               CombatLog       StageRenderer       Meters fold    UnitFrames fold
               (text fold)     (ADR-0005 iface)    (dmg/heal)     (hp/auras @tick)
                    └───────────────┴───────┬───────────┴──────────────┘
                                            ▼
                              playback clock (UiState): pause / 2× / seek(tick)
```

- The sim runs to completion on a **background thread** (C# `Task`/thread — the sim is pure and
  Godot-free, so this is trivial) before playback begins ("the raid is underway…"). The UI never
  drives or mutates the sim.
- All views are folds over the same stream, positioned by the playback clock. `seek` rebuilds
  folds from the nearest engine snapshot keyframe (engine-spec §6).
- The thread boundary is also the perf boundary: a 25-man sim never stalls the main (render) thread.

## 5. Renderer strategy & the Raid Night screen layout

Per [ADR-0005](adr/0005-hybrid-renderer.md): a `CombatStageRenderer` interface with the combat
view as a **Godot 2D scene** consuming the event stream (circles, telegraph shapes, movement
tweens). The stage is a *view* — clicking a unit selects it in `UiState`; no game logic in the
renderer, ever. (The renderer-behind-an-interface design still holds — a simpler node-based board
first, a richer particle/shader pass later — but both are Godot 2D now; the old DOM→PixiJS split
is moot.)

**Raid Night layout (locked vision — reference: warcraftlogs Replay tab screenshots from the
developer):**

```
┌────────────────────────────────────────────────┬──────────────┐
│              ARENA (top-down)                  │ boss frame   │
│  raider circles (class color + icon)           │ HP% + cast   │
│  boss circle · telegraph shapes (circle/cone/  │ bar          │
│  ring) · movement · death markers              ├──────────────┤
│                                                │ LIVE METERS  │
│                                                │ dps/hps/taken│
├──────────────────────┬─────────────────────────┤ interrupts/  │
│ UNIT FRAMES (grid)   │ ENCOUNTER TIMELINE      │ dispels tabs │
│ hp bars, flash on    │ progress bar + upcoming │              │
│ hit, aura pips       │ DISCOVERED mechanics    │              │
└──────────────────────┴─────────────────────────┴──────────────┘
              playback: pause / ½× 1× 2× 4× / seek
```

- Telegraphs render from mechanic events (shape+center+radius in the stream); units interpolate
  between `move` events.
- The timeline strip shows only *discovered* entries (game-design §7 knowledge system) — the
  player sees what the guild knows, not the truth.
- Meters and frames are the same folds the post-raid report uses — live view is just folding
  up to the playback clock's tick.
- Death moment = slow-flash marker on the arena + entry in the death recap drawer. The "greedy
  melee died in the ring" story must be readable at a glance.

## 6. Perf discipline (DM1's lessons, applied preventively)

- Signals stay narrow; derived data cached at the fold layer (keyed by `(streamRef, tick)`), not
  recomputed per frame.
- Long lists (roster 60, log thousands) use virtualized/recycled lists (`ItemList`/`Tree` or a
  recycling container) from the first implementation.
- Playback tick updates flow through the clock only; static panels don't rebuild during playback.
- File/type size budget kept small (analyzer). If a screen needs "and" to describe, split it.
