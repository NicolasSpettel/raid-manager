# Research: WoW Classic (Level 60) Damage Model → our combat math

> Delegated deep-dive: how damage scales in vanilla WoW at 60, and how close Raid Manager can go.
> **Answer: very close — and vanilla's *good* parts map cleanly onto our tick engine and locked
> stats, while the parts we already rejected are vanilla's *worst* parts anyway.** Feeds the
> hybrid damage model (GDD §7) and engine-spec; formulas to implement in C# fixed-point.

## 1. How vanilla WoW scales at level 60 (the real formulas)

### Physical (melee/ranged auto-attacks + weapon abilities)
- **Per-swing damage** = `weaponDamageRoll(min..max) + (AttackPower / 14) × weaponSpeed`.
  Key consequence: `AP / 14 = bonus DPS`, independent of weapon speed (slow weapons get more per
  swing but swing less often). **"14 AP = 1 DPS" is the master knob.**
- **Abilities** are `weaponDamage + flat` or a `% of weapon damage` (some "normalized" to a fixed
  speed so slow weapons don't overload instant attacks).
- **Crit = ×2.0** (200%) for melee/ranged.

### Armor mitigation (physical)
- `DR% = Armor / (Armor + 400 + 85 × attackerLevel)`; at attacker level 60 →
  **`Armor / (Armor + 5100)`**. Soft-capped at **75%**. Smooth diminishing returns, no hard cap
  pain. (Confirmed by search + knowledge.)

### Spell damage
- `Damage = baseDamage + coefficient × SpellPower`.
- **`coefficient = castTime / 3.5`** (a 3.5s cast gets 100% of spell power; a 3.0s cast 86%;
  **instant = 1.5/3.5 = ~43%**). Longer casts scale harder with gear.
- **DoTs:** `coefficient = duration / 15`, spread across ticks.
- **AoE:** an extra penalty (`(castTime/3.5 × 0.95) / 3`) so AoE doesn't scale like single-target.
- **Spell crit = ×1.5** (150%) — *note the asymmetry with melee's ×2*.
- Sources: [Fanbyte spell coefficient wiki](https://wow.allakhazam.com/wiki/spell_coefficient_(wow)),
  [Blizzard forums: coefficients & downranking](https://eu.forums.blizzard.com/en/wow/t/spell-coefficients-downranking-penalty-math-vs-reality/152650),
  [Ozgar's downranking guide](https://www.warcrafttavern.com/wow-classic/guides/ozgars-downranking-guide-tool/).

### Magic mitigation
- Vanilla used **resistances** → an average mitigation % plus a *partial-resist RNG roll*. Fiddly
  and swingy; widely considered one of vanilla's worse systems.

### Healing
- `Heal = base + coefficient × HealingPower`, same `castTime/3.5` (HoTs `duration/15`); crit ×1.5.

### What we'd be inheriting that we DON'T want
Hit chance / miss, glancing blows, weapon skill, dodge/parry/block RNG on attackers, downranking,
partial resists. These are exactly the systems we **already rejected** (accuracy/hit — see
[gear-stats-reference.md](gear-stats-reference.md)) or that add opaque RNG. Dropping them is a
feature: it removes vanilla's most-complained-about math.

## 2. How close we can go — the verdict

**Very close, and it's a great fit**, because vanilla's *keepers* align with decisions already
locked:

| Vanilla formula | Fit with Raid Manager |
|---|---|
| `AP/14 = 1 DPS` | Clean master knob; we already have **Attack Power** as a core stat — plug in directly (skip vanilla's Str/Agi→AP conversions; our gear gives AP directly, DM1-style). |
| Armor `A/(A+5100)` | Adopt as-is with a tunable constant `K_armor`; smooth DR, no accuracy-cap feel-bad. We have **Armor**. |
| Spell coeff `castTime/3.5` | **Synergizes with our hard-cast-commitment design (GDD §8e):** long casts hit harder *and* commit harder — risk/reward is baked into the same number. And `3.5s = 14 ticks` at our leaning 0.25s tick — neat. |
| DoT coeff `duration/15` | Adopt directly; rewards our dot-maintenance class profiles. |
| Crit multipliers | Adopt ×2 physical / ×1.5 spell (we have the **Crit** stat) — or unify (a knob, §4). |

**Adaptation we make on purpose:** replace vanilla's messy resistance/partial-resist system with
the **same armor-style curve for magic** — `MR/(MR + K_mr)` — using our **Magic Resist** stat.
Symmetric, deterministic, no swingy partial-resist RNG. This is strictly cleaner than vanilla.

We borrow vanilla's **formulas and curve shapes**, not its literal stat numbers — our values scale
by our **ilvl bands** (GDD §9), tuned by our own sim/wall-probe, not WoW's item budgets.

## 3. Proposed Raid Manager formula set (first pass)

Time in **ticks** (0.25s ⇒ 3.5s = 14 ticks). All deterministic, fixed-point, seeded rolls.

```
Physical hit   = weaponBase + (AttackPower / K_ap) × weaponSpeedTicks
                 then × (1 − Armor/(Armor + K_armor))
                 × (isCrit ? 2.0 : 1.0)

Spell hit      = spellBase + (castTicks / 14) × SpellPower        // AoE × aoePenalty
                 then × (1 − MagicResist/(MR + K_mr))
                 × (isCrit ? 1.5 : 1.0)

DoT/HoT tick   = (base + (durationTicks / 60) × Power) / numTicks  // 15s = 60 ticks

Heal           = healBase + (castTicks / 14) × HealPower × (isCrit ? 1.5 : 1.0)
```

Knobs (tuning module, per Principle 0): `K_ap` (≈14), `K_armor`, `K_mr`, crit mults, `aoePenalty`,
armor/MR soft caps. Ability rows author `weaponBase`/`spellBase`/coeff-overrides; the **execution
quality** layer (attributes/condition/posture, GDD §7) scales *realized* output on top — that's
the hybrid model: vanilla formulas set the ceiling, our attributes decide how much a raider
extracts.

## 4. Open knobs / decisions

1. **Crit multiplier:** keep vanilla's split (×2 physical / ×1.5 spell) for known feel, or unify
   to one value for simplicity? *(Rec: keep the split — it's a meaningful class-feel difference and
   free.)*
2. **Tank avoidance:** vanilla tanks also dodge/parry/block (RNG mitigation). Do our tanks get an
   avoidance layer, or is tank survival purely armor/MR + defensives + active mitigation (GDD §8g)?
   *(Rec: no RNG avoidance — active mitigation + the armor/MR curves only; keeps it deterministic
   and readable, consistent with rejecting hit/miss.)*
3. **Armor/MR constants & soft caps:** set during the balance-sim phase, not now.
4. **Level scaling:** vanilla bakes `attackerLevel` into the armor constant. We have no character
   levels (GDD §8) — so `K_armor` scales by **content tier / ilvl band**, not level. Confirm.
