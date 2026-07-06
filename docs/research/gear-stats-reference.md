# Research: Item Stat Systems — What 20 Years of WoW Teaches Us

> Delegated question (game-design §9): how deep should item stats go — armor pen? magic pen?
> mana? accuracy? — weighed by combat-calculation cost and actual fun added. Written from WoW's
> documented stat-design history; companion to [boss-design-reference.md](boss-design-reference.md).

## 1. What WoW tried and *removed* — and why it matters to us

WoW is the best natural experiment in stat design ever run: two decades of adding stats and then
deleting them. The deletions are the lesson:

| Stat | Fate in WoW | Why removed | Our takeaway |
|---|---|---|---|
| **Hit / Accuracy** | Removed (Warlords of Draenor, 2014) | A *mandatory cap*: below cap you randomly whiff (feels terrible), at cap the stat is worthless. Gearing became "reach 17% then ignore" — spreadsheet homework, zero interesting choices. | **Skip accuracy.** Misses vs raid bosses feel like the game cheating (see anti-design §14.4). "Did my rotation land" belongs to our attribute system (Consistency), where it reads as *human* error, not dice. |
| **Expertise** (melee-hit twin) | Removed (WoD) | Same cap problem, worse visibility. | Same verdict. |
| **Armor Penetration** | Removed (Cataclysm, 2010) | Players couldn't reason about it: value scaled *with itself* and with target armor, creating opaque breakpoints where it went from mediocre to overpowered. Confusion, not choice. | **Skip both penetrations.** Opaque multiplier-vs-multiplier math also makes our sim balancing harder for zero visible gameplay. |
| **Mana/Spirit on gear** | Spirit removed (Legion, 2016) | Regen-on-items forced healers into a parallel gearing economy and punished everyone else holding the item. | **Mana is a resource, not an item stat.** Regen/pool tuning lives in class/spec templates; keeps every drop evaluable by everyone. |
| **Weapon skill** | Removed (TBC, 2007) | Invisible per-weapon-type grind that mostly manifested as "why am I glancing?" | Supports deferring *trainable* weapon proficiencies (game-design §9). |
| **Resilience/PvP stats** | Removed from PvE items | Split the item pool. | No parallel stat economies. |

What WoW *kept* after the great simplification (WoD onward): primaries (Str/Agi/Int + Stamina)
plus a small secondary suite — **Critical Strike, Haste, Mastery, Versatility**. That
four-secondary model has been stable for 10+ years because each one changes how combat *plays*,
none has a mandatory cap, and all scale smoothly.

## 2. Recommendation for Raid Manager

**Core stats (keep DM1's six, renamed to taste):**
Attack Power, Spell Power, Stamina (HP), Armor, Magic Resist, Defense — proven in DM1, cheap in
the engine, readable by players.

**Add exactly three secondaries:**

| Stat | In-combat effect | Why it earns a slot |
|---|---|---|
| **Critical Strike** | chance for 2× on hits/heals | Universally understood; creates visible replay moments (big yellow number); interacts with posture (Aggressive loves variance) |
| **Haste** | speeds swing timers, cast times, resource ticks | **The perfect stat for our tick engine** — its effect is literally visible in the replay (faster cast bars) and it's the one stat that makes the FM-style watchability richer. Integer-safe implementation: haste modifies tick-costs via fixed-point multiplier, documented rounding. |
| **Mastery (per-class)** | one class-flavored effect per class (e.g. rogue: bonus from behind; oracle: heals echo) | Carries class identity inside the generic item system — items stay formula-generated (`stat = f(ilvl)`) while classes still *feel* distinct wearing them. 12 registry rows, one per class. |

**Explicitly rejected:** accuracy/hit (cap-feel-bad), armor pen / magic pen (opacity),
mana-on-gear (parallel economy), versatility (WoW's own players call it the boring stat — our
posture system already owns the offense/defense dial).

**Cost check:** 3 secondaries ≈ +3 multiplier lookups per damage/heal event and one timer
adjustment — negligible against the perf budget (engine-spec §10), and all deterministic-safe.

**Fun check:** loot choices become "crit dagger vs haste dagger" (visible playstyle change),
never "recalculate my hit cap." Item tooltips stay 5 lines. Automation ("equip best") stays a
solvable stat-weight problem for the co-leader AI — cap-stats would have broken that too.

## 3. Weapon subtype stat-affinity sketch (supports game-design §9)

Subtypes bias the generated stat rolls (templates, not rules text): daggers → crit-heavy
(rogues), maces → stamina/defense (tanks), wands/tomes → haste/spell, glaives → haste/attack
(fel striker identity), bows/guns → ranged-only crit/attack, shields → armor/defense block.
Class proficiency lists gate equipping; affinity means "the right weapon type usually *is* the
stat-right choice" — identity emerges from generation, no extra combat math.
