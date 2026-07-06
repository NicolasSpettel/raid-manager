# Economy Model — research + a concrete priced first pass

> Deep-research deliverable. Researches Football Manager (and the real football economy it mirrors)
> and proposes a **gold scale with prices attached to every system**, for the dev to react to.
> All numbers are a **first-pass proposal** to argue with, not locked. The *proportions* (from FM)
> matter more than the absolute numbers. Feeds [game-design.md §9b](game-design.md).

## 1. What FM / real football actually does (research)

- **Wages are the dominant cost.** A well-run club's wage bill runs ~50–70% of revenue (UEFA's
  squad-cost cap is 70%). Wages, not transfers, are the real budget pressure.
- **Star vs squad wage spread is ~10–20×.** In-game examples: a star teen ~£15–18k/week, a
  marquee signing ~£40k/week, fringe players a few £k/week; real elites £200–500k/week. The
  *ratio* between a fringe player and a superstar is what we copy, not the currency.
- **Transfer value is a low/mid/high *range*** from a weighted formula:
  `Base = Position × League × AgeCurve`, then `× (1 + Performance + Contract + Caps + Demand)`.
  Value roughly doubles moving up a tier. Fees for a prime player land around a season or two of
  their wages (heavily amortized in real football; we don't need amortization).
- **Revenue sources:** broadcast/TV, gate receipts (attendance), commercial/sponsorship, prize
  money, player sales. The board converts revenue into a **wage budget** and a **transfer
  budget**; overspend → warnings/embargoes.
- **Condition ≠ Sharpness (FM's modern split — directly relevant to our stamina rework):**
  *Match Condition* = physical freshness (drains with activity/high-intensity load, recovers with
  rest; recovery rate governed by **Natural Fitness**). *Match Sharpness* = readiness, built by
  actually playing and decaying with inactivity. **Stamina** only affects in-match drain, not
  recovery. A rested-but-benched player is fresh yet *unsharp*.
- **Injuries:** driven up by fatigue/low condition + heavy load; an injury-proneness attribute;
  severities from knocks (days) to serious (months); recovery training / medical staff reduce
  risk and duration.

Sources: [FM24 wage discussion](https://steamcommunity.com/app/2252570/discussions/0/603020878754663546/),
[FM transfer value factors](https://community.sports-interactive.com/forums/topic/581892-how-are-transfer-values-calculated/),
[FM transfer-fee calc](https://community.sports-interactive.com/forums/topic/403707-transfer-fee-calculation/),
[FM21 fatigue & injuries](https://www.footballmanager.com/the-byline/fighting-fatigue-and-preventing-injuries-fm21),
[FM21 conditioning overhaul](https://realsport101.com/football-manager/football-manager-202-conditioning-overhauled-ahead-of-release-match-sharpness-draining-fatigue/).

## 2. The gold scale (proposed)

Goal (dev): numbers feel like **thousands of gold**, not millions — roughly **1/1000** of
football, with FM's *ratios* preserved. One currency: **gold**.

### Raider weekly wages (gold/week) — **LOCKED scale (dev confirmed "thousands", 10–20× spread)**

| Tier | Wage/wk | Notes |
|---|---|---|
| Youth / fringe | 10–25 | academy graduates, benchwarmers |
| Midcore (≈3★) | 30–70 | the roster backbone |
| Strong (≈4★) | 90–180 | carries |
| Elite (≈5★, world-first caliber) | 200–400 | rare; a marquee wage |

Fringe→elite spread ≈ **12–18×** (FM's ratio, deliberately *not* wider — with 20 raiders each is
less pivotal than an 11-man football XI, so no one raider dominates). 25-man weekly wage bill:
mid guild ≈ **1,200–3,000/wk**, top guild ≈ **5,000–9,000/wk** — still the dominant cost.

### Transfer / buyout fees (one-off, gold)

Roughly **1–2 seasons of wages** (~40 weeks/year), tier-scaled:

| Tier | Fee |
|---|---|
| Fringe | 400–1,200 |
| Midcore | 1,500–4,000 |
| Strong | 6,000–15,000 |
| Elite | 15,000–40,000 (a genuine event, but not fortune-breaking) |

### Guild balances by prestige tier

| Tier | Typical balance |
|---|---|
| Local (where you start) | 5,000–15,000 |
| National | 30,000–80,000 |
| Continental | 80,000–200,000 |
| World-elite | 200,000–600,000 |

### Manager (you) salary — "1/10" personal scale

Your pay is deliberately an order below guild flows (it funds only the cosmetic lifestyle loop):
**200–1,500 gold/week** by guild prestige. Spend on house/mount/yacht-equivalents (§9c cosmetic).

## 3. Prices attached to everything

| Thing | Cost (gold) | Rationale |
|---|---|---|
| Flask/potion per raider per raid night | 5–15 | 25-man night = **125–375** in consumables — a real per-pull decision |
| Repair after a wipe | small (5–30/night) | wipe tax |
| Craft mats (per tier band) | 5–50 each | profession inputs |
| Youth prospect investment | 300–3,000 | small cost, big-future-return bet (§youth) |
| Youth-hall upgrade | 2,000–20,000 (tiered) | more/better intake |
| Guild-hall venue upgrade | T1 ~2,000 → T5 ~50,000+ | long-run ROI (§4) |
| Buy raid strat intel (skip some learning) | 1,000–10,000 by tier | optional shortcut (§boss learning) |
| Recruit/scout brief fee | 200–2,000 | pay the co-leader to shortlist |
| Stamina/rest refresh (emergency) | scaling | discourage spamming |

## 4. Revenue model

| Stream | Amount | Scales with |
|---|---|---|
| Sponsors / patrons (weekly) | local ~500 → top ~12,000/wk | prestige + recent results |
| Raid boss kill | 50–500 | raid tier |
| World/region-first bounty | 2,000–50,000 | tier + placement |
| Season race placement prize | local few-thousand → top tens-of-thousands | final rank |
| Dungeon clear | 10–50 | trivial; it's a gear faucet, not income |
| Guild-hall (Feast Hall guests) | scales, high-prestige only | Feast Hall tier × prestige — the "gate receipts" |
| Item/loot sales, AH | variable | BoE drops, crafted goods |
| Selling a raider | see transfer fees | painful lever |

Wage bill should sit ~50–65% of a healthy guild's income (FM's ratio) — so overspending on wages
is the classic trap, and the board grants a **wage budget** + **transfer/gear budget** you can
exceed only with approval (relationship cost). This is the whole "manage money" pressure.

## 5. Trade-market valuation formula (anti-design-critical)

Per anti-design §14.1/2 — **visible, performance-driven, never wallet-scaling.** Proposed:

```
Value = Base(role, tier) × AgeCurve(age, potential) × (1 + PerfScore + FormScore + ContractFactor)
```

- `PerfScore` from real logs/results (parses, world-firsts, clears) — so playing well *visibly*
  raises value; the profile shows the trend and the reason ("+15% since the world-#40 kill").
- **Wage/fee demands derive from Value + prestige gap + personality — NEVER from your bank
  balance** (the negotiation code in `Game` is simply not passed the guild's gold; enforced by the
  package boundary). This kills FM's most-hated bug structurally.
- Offers have a **low/mid/high range**; your **Negotiation** attribute + the co-leader let you
  land nearer the low end. Bidding below range is very hard (insulting) unless a **social factor**
  helps (the target is a friend of one of your raiders); overpaying above range closes instantly.

## 6. Social graph (needed by trade + inbox)

A lightweight **relationship layer** on raiders — friends/foes/mentors, seeded at world-gen and
grown by shared guild history — feeds: transfer friction (a foe of your star won't join cheaply),
morale interactions, and inbox drama. Small component on the entity model ([entities §2](entities-and-worldgen.md)),
flagged there as the "relationships component."

## 7. Open levers / decisions for the dev

1. ~~Scale check~~ **LOCKED:** "thousands" confirmed; 10–20× wage spread confirmed; elite
   transfers compressed to 15–40k (not fortune-breaking).
2. Wage-bill target as % of income (60% proposed) — the core difficulty knob.
4. Amortization/contracts: keep fees as simple one-offs (proposed) — confirm.
5. Personal wealth: purely cosmetic (locked) — confirm the salary band feels right.
