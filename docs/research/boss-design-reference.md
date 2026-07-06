# Research: WoW Boss Anatomy & Warcraftlogs — Design Reference

> What real raid encounters and real log tooling look like, distilled into requirements for our
> encounter registry, event stream, and report screens. Companion to
> [game-design.md §7](../game-design.md) and [engine-spec.md §8](../engine-spec.md).

## 1. Anatomy of a modern raid boss

A WoW-style boss is a **scheduled timeline, interrupted by triggers**. Players experience it
through boss-mod timer bars (DBM/BigWigs) counting down to each ability. The building blocks:

**Ability categories (every boss is a mix):**

| Category | Example pattern | Who it tests | Our archetype |
|---|---|---|---|
| Tank mechanic | stacking debuff every ~15–25s → swap at 2–3 stacks | tanks' CD discipline, swap timing | `tankSwapDebuff` |
| Raid-wide burst | big AoE every ~30–45s or on energy full | healer CDs, personals, "lust here?" | `raidBurst` (add to set) |
| Positional | spread ("don't overlap"), stack ("soak together"), get-out ("drop the puddle away") | Mechanics, Awareness, movement DPS loss | `spreadDamage`, `stackSoak`, `groundHazard` |
| Interrupt | castbar every ~20s, must-kick or raid takes damage/buff | interrupt rotation, Communication | `interruptibleCast` |
| Adds | wave on timer or phase, must die before X | target swapping, cleave comp | `addWave` |
| Dispel | magic debuff on N players, dispel fast (sometimes: dispel *bounces*, so timing matters) | healer attention | `dispellableDebuff` |
| Personal responsibility | "you have the bomb — run it out" | individual Composure/Mechanics | `targetedDebuffRunOut` (add) |
| Soft/hard enrage | dmg ramp or one-shot at N minutes | overall DPS check | `enrage` |

**Timer patterns (how abilities schedule):**
- **Fixed cadence:** every N seconds from pull (± small variance). The bread and butter.
- **Phase-locked:** cadence resets or changes per phase; some abilities exist only in P2.
- **HP-triggered:** phase transitions at 70/40%, intermission at 50%, etc.
- **Energy/resource-based:** boss casts the big one at 100 energy; fast comps can *skip* casts
  by pushing phases early — this is why "go aggressive to push before the overlap" is a real
  tactic, and our posture system's payoff.
- **The real difficulty is OVERLAPS:** individually trivial mechanics become lethal when the
  spread, the add wave, and the raid burst land within the same 10 seconds. Late phases are
  hard because cadences drift into worst-case alignments. **Design rule for us: author
  mechanics so their periods interfere** (e.g. 20s and 33s cadences), and difficulty tiers can
  add/tighten overlaps rather than just inflating numbers.

**A realistic composite timeline (use as our first boss template):**

```
P1 (100→70%):  Swing timer 2.0s · Cleave every 12s (tanks)
               Shadow Bolt volley every 20s (interruptible)
               Void Puddle on 3 random ranged every 24s (drop away)
               Stacking tank dot every 16s → swap at 2 stacks
Intermission (70%): boss immune 40s · 2 add waves · raid takes rot damage
               (healer CD moment; adds must die or P2 starts buffed)
P2 (70→30%):  P1 set continues, cadence ×0.9 · NEW: Annihilation every 45s
               (raid-wide burst — pre-assigned healer CD rotation; "lust on second one"
               is a classic call) · Spread mechanic every 30s (8yd)
P3 (30→0%):   soft enrage: rot damage ramps 5%/10s · Annihilation every 30s
               puddles persist → space runs out (the "floor is full" loss condition)
Hard enrage:  10:00
```

**Boss-mod lesson (DBM):** warnings are **action-oriented** — "RUN OUT", "TANK SWAP NOW",
"KICK" — not spell names. Our equivalents: the raid-leader call events in the replay, and the
player's **spot rules** ("always potion at Annihilation #2") which are exactly pre-authored
DBM-style responses. This vocabulary should be visible in our UI.

## 2. Warcraftlogs anatomy → our report screens

How real raiders read a log (the canonical order: summary → deaths → timeline → individuals),
and what each view needs from our event stream:

| WCL view | What it shows | Our version needs (event-stream fields) |
|---|---|---|
| **Summary** | raid DPS/HPS, deaths, damage taken by source | already covered (damage/heal/death events) |
| **Damage Done** + ability expand | per-raider ability breakdown: casts, hits, crits, % of total; rotation problems show as filler overuse | `damage.ability` already present; need `cast_end` completeness to compute uptime/casts |
| **Deaths** | last-seconds reconstruction: killing blows, overkill, HP-over-time, **which defensives were NOT used** | have: damage/overkill/death. Need: cooldown-availability derivable ⇒ emit `cooldown_used` events (add to union) so "had Shield Wall available, didn't press it" is computable |
| **Problems** | auto-flagged: avoidable damage taken, personals missed on raid bursts, standing in bad | need an `avoidable: boolean` (or `hazardId`) tag on damage events from ground/telegraph sources — cheap at emit time, gold for the auto-analyst |
| **Replay** | top-down positions, movement, HP, debuffs over the fight | our stage view (ADR-0005) — positions-in-events already locked |
| **Casts/uptime** | buff/debuff uptime, CD usage timing | aura events already present; `cooldown_used` completes it |

**Two event-union additions** fall out of this research (append-only, per ADR-0004):
1. `{ kind: "cooldown_used", who, ability, category: "defensive" | "offensive" | "movement" }` —
   enables death recaps ("defensive available, unused") and CD-timing analysis.
2. `avoidable?: HazardRef` on damage events — enables the Problems-style auto-analyst, which is
   also exactly what the hireable **log analyst** (§12) surfaces for low-Judgement managers.

## 3. Design implications checklist

- [ ] Add `raidBurst` and `targetedDebuffRunOut` to the M1 mechanic archetype starter set.
- [ ] Author interference: mechanic cadences chosen to create overlap windows; difficulty tiers
      tighten overlaps, not just numbers (answers DM1's "heroic was hard to scale").
- [ ] Energy-based casts + phase-push skipping = the mechanical payoff of Aggressive posture.
- [ ] Spot rules UI speaks DBM verb language: RUN OUT / SOAK / KICK / SWAP / POTION / LUST.
- [ ] Post-raid report ships the WCL reading order as its tab order: Summary → Deaths →
      Timeline → Player detail; Problems view = the auto-analyst (staff-gated quality).
- [ ] Death recap is a first-class screen (killing blow, HP timeline, unused defensives, what
      they were standing in) — it's the single most story-generating view in real raiding.

## Sources

- [How to Read a Warcraftlogs Report — parsecard.app](https://www.parsecard.app/guides/reading-warcraftlogs-report)
- [Evaluating Raiders with Warcraft Logs — Cannot be Tamed](https://cannotbetamedblog.wordpress.com/evaluating-raiders-with-warcraft-logs/)
- [WarcraftLogs Guide: Summary, Problems, Deaths (video)](https://www.youtube.com/watch?v=WqE6pmjk7-g)
- [Raiding 101: Performance Management — raider.io](https://raider.io/news/556-raiding-101-performance-management)
- [Deadly Boss Mods — setup & design philosophy](https://noobtoboss.com/addon/deadly-boss-mode/)
- [Boss Mod Timeline WeakAura (timer-bar UX reference)](https://wago.io/t4TuhqgRo)
