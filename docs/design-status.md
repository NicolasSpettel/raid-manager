# Design Status — maturity audit

> Honest per-system maturity, so we know what's build-ready, what needs a real design pass, and
> what's blocked on a dev decision. Updated as systems mature. Tiers:
> **SOLID** (specced deep enough to build) · **THIN** (direction locked, needs a real design pass
> before building) · **DECISION** (blocked on the dev).

## SOLID — deep enough to build from

| System | Where |
|---|---|
| Combat model — tick engine, state machine (turret/light/heavy/mechanic), action-slot greed↔panic decision, event stream | engine-spec, GDD §7/§8d |
| Mechanic archetypes + discovered-timeline + cooldown-learning loop | engine-spec §8, GDD §7 |
| Class kit *framework* (axes, states, raid utility, interrupts/dispels, cast-commitment) | GDD §8d/§8e |
| Healer & tank *templates* (kit shape, mana clock, taunt window) + 2 reference kits each | GDD §8f/§8g |
| Combat replay view (WCL-style arena, telegraphs, feasibility triage, layout) | GDD §7, ui-design-system §5 |
| Entity/component model + latent-factor world-gen | entities-and-worldgen — **first slice BUILT** (`src/Game/World`): deterministic `WorldGen` (~6,500 chars/seed), composition `Raider`, derived stars, world-gen golden. Contracts/career-ledger + season sim still to come. |
| Persistence: seed + deltas + season compaction + Chronicle | ADR-0007, save-format |
| Aging & career arc | GDD §8 |
| Architecture: engine, Godot+C# stack, boundaries, testing, Principle 0 | BLUEPRINT, ADRs, testing-strategy |
| Gear frame: slots, ilvl bands, binding, formula scaling, tier-set rotation | GDD §9/§8c |
| Professions ladder + material tiering | GDD §9d |
| Season/calendar structure + weekly lockout + history retention | GDD §5 — **race BUILT** (`SeasonRace`): the world progresses the raid in parallel to a leaderboard, calibrated to the locked pacing (elite ~wk3, #100 ~wk13). Player-facing calendar/week loop + Chronicle still to come. |

## THIN — direction locked, needs a real design pass before building

Each is a *paragraph* today; each needs to become a *spec* (mechanics, numbers, UI, and how it
plugs into the sim/economy). Rough priority order for when we build:

1. **Economy & finances** — **FIRST PASS DONE** → [economy-model.md](economy-model.md): FM
   research + a concrete priced gold model (wages, transfer fees, guild balances, revenue,
   costs, valuation formula). Awaiting dev reaction on the scale, then it's SOLID.
   *(Condition/injury system also fleshed FM-modeled — GDD §8, now near-SOLID.)*
   *(Difficulty/raid-units, boss-learning thresholds, 1-button automation, staff tasks, youth,
   awards/PR/holidays, self-contract, social graph — all got locked design direction this pass;
   promoted from bare-paragraph to "direction set, ready for their build-time spec.")*
2. **Trade-market valuation** — the market is well-specced EXCEPT the raider-value formula, which
   anti-design §14.1/2 makes load-bearing (performance-driven, never wallet-scaling). Needs the
   actual formula + how offers/wages derive from it.
3. **Contract negotiation** — levers and back-and-forth sketched; needs the real
   offer/counter/accept AI, clause effects, and the firing/patience model (§14.5).
4. **Inbox & events** — the system and registry pattern are locked; there's almost no actual
   event *content* yet, nor the choice→consequence authoring shape. Needs an event template spec
   + a starter set.
5. **Staff / co-manager & automation** — delegation is a pillar (§6b) but the staff quality model,
   what each role automates, and the "decide for me at ~90%" logic per system are undefined.
6. **Weekly activity mechanics** — the week **structure** is BUILT (`src/Game/Season`: `SeasonCalendar`,
   weekly `Lockout`, "plan my week" stances → raid days, `WeekRunner` raiding the ladder with the real sim).
   Still THIN: the 4-slot intra-day model, the condition/stamina cost of raiding hard, and the *off-day*
   activities (dungeon farm, training, professions, questing, rest, drills) — each needs its actual
   inputs→outputs (what it costs, what it yields, how it touches attributes/gear/morale).
7. **Boss learning UX** — the knowledge-bar + fuzzy-feedback loop is locked in principle; needs
   the concrete model (how pulls/analysis fill knowledge, how it converts to combat effect, how
   the "ask a raider" conversation reads).
8. **Guild hall venues** — 4 venues + ROI direction; needs tiers, costs, and exact returns.
9. **Living world / AI guilds** — leaderboards, rivalries, fidelity tiers are directionally set;
   the AI guild *decision model* (how they schedule, recruit, progress) is undefined.
10. **Difficulty tiers** — "data multiplier + mechanic additions" is the mechanism; no actual
    tier design (what Heroic/Mythic-analog add) yet.
11. **Manager progression** — attributes proposed and manager-leveling mentioned; needs the
    actual XP sources, skill-point effects, and how manager attributes gate info/options.
12. **Smaller/thin:** youth program, awards ceremony, PR/social-media layer, holidays,
    lifestyle loop, onboarding/first-session flow.

## DECISION — blocked on the dev (see the running list at the bottom of game-design.md)

Grouped by urgency:

**Quick calls that unblock design (a sentence each):**
- Raider attribute list: confirm/trim the proposed 11 → ~10.
- Manager attribute list: confirm the proposed 7.
- Armor types (cloth/leather/mail/plate): in or out?
- Ring/trinket slot counts (assumed 2 + 2): confirm.
- Raids per year: 2 or 3 (coupled to aging — see GDD §8).
- Offensive healing (healers DPS when raid topped): confirm in (recommended).
- Permadeath stance: none in base game + rare career-ending injury (recommended)?
- Difficulty/ironman modes: any at launch?

**Can defer to the build/engine phase (not blocking now):**
- Tick resolution 0.1s vs 0.25s (M0).
- Hybrid damage-model formulas (engine phase).
- AI-world fidelity perf budget (when we build the world sim).

**Naming (batch later; working terms in use):** Rally (raid burst), Counter (interrupt),
Cleanse (dispel), the 12 classes' ability names, per-class raid buffs, the game's title.
