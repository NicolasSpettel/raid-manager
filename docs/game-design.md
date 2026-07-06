# Raid Manager — Game Design Document (the game wiki)

> Source of truth for WHAT the game is. Organized along the player's journey. Every section marks
> its status: **[LOCKED]** decided by the developer · **[PROPOSED]** assistant's recommendation
> awaiting sign-off · **[OPEN]** genuinely undecided, options listed. The decision log at the
> bottom records every status change.

**Elevator pitch:** Football Manager in an RPG world. You manage a raiding guild in a living,
offline world of 100–200 guilds racing to clear each raid release first. Your job is everything
around the fight: the roster, the schedule, the contracts, the morale, the logs — and then you
watch your decisions play out on raid night.

---

## 0. Product identity **[LOCKED]**

- Fully offline single-player. Optional Steam integration: achievements, and *opt-in* upload of
  season results for global leaderboards/comparison. The game never requires a connection.
- PC-first, standalone executable. Complex, dense, keyboard-and-mouse UI. No mobile version.
  Browser builds exist only as an internal playtest harness during development.
- Real commercial product from day one — quality bar, save integrity, and recovery UX are floor
  features, not polish.

## 1. Opening screen **[PROPOSED]**

Minimal and atmospheric: **Continue** (most recent career), **Load Career**, **New Career**,
**Hall of Fame** (past seasons/achievements, later), **Settings**, **Quit**. Background: slow
ambient scene of a guild hall / portal skyline. No gameplay here — its only job is getting you
into a career fast and making Continue the one-click default.

## 2. Manager creation **[user sketch + PROPOSED details]**

You are the manager — a person, not a hero class.

- **Identity:** name, age (affects starting reputation curve — young = "prodigy" narrative, old =
  "veteran"), nationality/region, portrait (v0: curated portrait set + palette options; no 3D
  editor).
- **Background** *(pick one — replaces a blank stat-buy with a story choice; each grants a
  starting attribute spread + a small perk)*: e.g. *Former World-class Raider* (respect from
  high-star players, weaker admin skills), *Guild Officer for years* (logistics/morale bonus),
  *Theorycrafter* (tactics/log-analysis bonus, low charm), *Rich Kid Sponsor* (money, little
  respect).
- **Manager attributes** (point-buy on top of background, trainable slowly during career)
  **[PROPOSED list, ~7]:**
  1. **Charm** — contract talks, media, recruit pitches (user's example — kept).
  2. **Leadership** — raid-night discipline, fewer panic wipes.
  3. **Tactics** — speed of boss-mechanic discovery, quality of strategy options offered.
  4. **Motivation** — morale recovery, benching fallout reduction.
  5. **Negotiation** — contracts, transfers, salary talks (both directions).
  6. **Judgement** — accuracy of what you *see* about raiders (scouting fog: low judgement =
     stats shown as ranges, misread answers).
  7. **Development** — training efficiency, young-raider growth.
- Manager attributes gate *information quality and option quality*, never press buttons for you —
  FM's trick: a low-Judgement manager sees "Mechanics: 8–14" where a high one sees "11".

## 3. The world: regions & guilds **[user sketch + PROPOSED details]**

- **Regions:** launch with Europe (Germany, France, UK, Scandinavia, Iberia, Eastern Europe…);
  architecture supports adding regions (NA, Asia) as data. Region flavors: name pools,
  guild-culture modifiers (e.g. hardcore-hours culture vs work-life balance culture affecting
  morale/stamina baselines) **[OPEN: how strong these modifiers are]**.
- **The guild ecosystem:** ~100–200 guilds × ~30 raiders + ~400 guildless free agents ⇒
  **~6,000–6,500 unique persistent characters**, procedurally generated at world creation
  (seeded), then persistent for the whole career. Every guild has:
  - Identity: unique name, crest/banner (procedural heraldry system: shape × charge × palette),
    region, founding year, motto.
  - **History:** past season placements, world-first moments, famous ex-raiders, scandals —
    generated at world-gen as a coherent backstory, then *written forward* by actual play.
  - **Prestige tier** (roughly: world-elite / continental / national / local — the "league
    pyramid" without literal leagues), driving expectations, sponsor money, and recruit pull.
  - Finances: bank, weekly income (sponsors scale with prestige + results), salary load.
  - Roster of unique raiders (see §8) with guild-appropriate quality.
  - Relationships: 1–2 **rival guilds** (races against them matter more for morale/board mood)
    and friendly guilds (share dungeon runs, easier transfers) **[PROPOSED]**.
  - **Board expectation** for the season (e.g. "top 50 worldwide", "top 3 in region", "just
    survive financially") — miss badly and you're fired; beat it and better guilds come calling.

## 4. Getting the job: offers & contract negotiation **[user sketch + PROPOSED details]**

- A fresh unknown manager gets offers from struggling/low-prestige guilds only (~10 openings to
  inspect: roster, finances, history, expectation, rival situation). Reputation unlocks better
  jobs across a career — the long game is the manager's career, not one guild.
- **Contract negotiation** as a back-and-forth (user sketch kept): levers = expected result,
  salary, contract length, plus **[PROPOSED]** extras: transfer budget, training facilities
  investment, autonomy clauses (board interferes less), release clause. Guild AI accepts /
  declines / counter-offers based on their finances, desperation, and your Negotiation + rep.
  Walking away is allowed; offers expire.
- **Personal wealth & lifestyle [LOCKED: mostly cosmetic]:** your salary accrues; spend it on
  increasingly lavish, slightly silly RPG-flavored status symbols — upgrade your house, your
  "car" (a mount), eventually the yacht-equivalent (a skyship? a private portal?). Pure charm
  loop, no meaningful buffs. Separate from this: **guild funds are a real management system**,
  and the scale stays grounded — *thousands* of gold, not football's millions.
- Getting **fired** mid-season is real: severance per contract, back to the job market with a
  reputation hit (or gain, if the guild was doomed). "Thrown under the bus" PR moves (§12) can
  engineer your exit — at a cost.

## 5. The season: raid releases **[user sketch — core structure]**

The season = a **raid release**, framed diegetically: not "a patch dropped" but **a new portal /
gate is opening in the world** — scouts report it weeks ahead, guilds prepare, and when it opens
the world race begins.

- **Cadence [LOCKED direction]:** ~**2 major releases per raid year**, with the gap sized by
  real clear-time pacing (if mid-table guilds clear in ~3 months, the tier's back half is farm
  season). WoW-style **pre-patch gear window**: ~1 month before a new raid, a new item-level
  band of farmable gear releases (dungeons/catch-up) so pre-raid prep is its own exciting phase.
  Smaller content (10-man mini-raid, dungeon season) can fill farm lulls **[OPEN: how much]**.
- **Pre-release window** (the "few days/weeks" of notice): pre-farming gear, finalizing roster,
  scouting rumors about the raid's nature ("the portal breathes frost — stock fire resistance"?
  **[OPEN: how much pre-intel is knowable/buyable]**).
- **Weekly lockout** (locked by user): bosses killed this week stay dead until weekly reset —
  loot once per boss per week. Progression = how deep you get; farm = re-killing on reset for
  gear. This single rule creates the FM-style week rhythm.
- **Resets & calendar [LOCKED]:** weekly reset every **Monday**; if a raid runs bi-weekly
  lockouts, resets are **Monday and Thursday**. A **calendar screen** is a core UI: upcoming
  releases and pre-patch windows, reset days, scheduled raid nights, holidays, contract
  expiries, rival milestones.
- **Season structure [LOCKED — template-driven]:** the game runs on the **real calendar** (real
  dates); a **season** is a template-defined period laid over it, shown as a **visual timeline**
  so the player always sees where they are in the current season and what's coming. **Seasons
  per year and season length are config, not hardcoded** — changing "2 seasons/year" to 3 is a
  template edit, nothing else (this is the project-wide template principle, BLUEPRINT §10).
  Cadence is co-tuned with aging (§8).
- **History retention [LOCKED — mechanism in [ADR-0007](adr/0007-world-persistence-seed-plus-deltas.md)]:**
  detailed, replayable world state is kept only for the **recent window** (current + previous
  season); older detail is **compacted away** at each season boundary. What persists forever is
  the compact **Chronicle** — season winners, final **top-100 leaderboards**, world-first kills,
  notable retirements, title holders — the world's permanent memory that makes a 20-tier-old
  world feel storied without bloating the save.
- **Holidays [LOCKED — days off, morale hit if denied; RPG names, placeholder]:** flavored
  equivalents timed to real-world seasons; raiders expect the day(s) off — grant them (lose raid
  time) or deny them (morale hit, "giga mad"). Placeholder calendar:
  **Longnight / the Emberfeast** (late Dec — the big one), **Harvest's End** (autumn — Feast Hall
  morale event), **the Sunreach Faire** (summer solstice — recruitment/free-agent spike),
  **the Thawing** (spring — return-to-form), **Ancestors' Vigil** (a somber remembrance day).
  Holidays create schedule dilemmas during progression races ("Longnight falls in week 3 of the
  new raid…"). Names are placeholders (single-source, trivially renamed).
- **The race & leaderboard:** all 100–200 guilds progress the same raid under the same simulation
  rules. Global + regional leaderboards: bosses down, pull counts, clear timestamps. Elite guilds
  clear in ~3 weeks; guild #100 might take 3 months (user's pacing — locked as the tuning
  target). Season ends for *you* when your goals resolve, but the world keeps racing — you watch
  rivals' progress in the news feed.
- **Season rewards:** prestige movement, sponsor money, board verdict vs expectation, raider
  reputation changes (your star healer world-firsting a boss becomes poachable — §11).

## 6. The weekly loop **[user sketch + PROPOSED structure]**

The playable unit is the **week**, made of days. Continuous-flow controls like FM:

- **Day structure [LOCKED]:** each day divides into **4 slots — noon, afternoon, evening,
  night — of ~2 hours each**. Activities occupy slots: a 6-hour raid night = 3 slots
  (afternoon+evening+night), a 5-man dungeon run = 1 slot, rest = leaving slots empty. Raiders
  are people: chronic night-slot scheduling and 4-slot days feed burnout; posture toward
  work-life balance is itself a guild-culture lever.
- **Raid schedule:** choose raid days (1–7/week) and session length (1–4 slots ≈ 2–8h). More
  hours = more pulls = faster progress, but burns stamina/morale and risks injuries/burnout —
  the central tradeoff dial. Off-season/farm weeks might drop to 1 day.
- **Off-day activity assignment** (per raider or per group; templates make it one click):
  - 5-man dungeon groups (gear catch-up, trains specific attributes slightly)
  - 10-man farm raid (older content: loot + low-pressure practice)
  - Solo questing (gold, small XP), profession crafting (consumables, tradeable gear), auction
    house work (guild banker personality?), currency farming for gear upgrades
  - **Training** (targeted attribute work — §8), boss-mechanic drills (practice a known mechanic)
  - **Rest** (stamina/morale recovery), social events (guild BBQ energy from DM1, but planned)
  - PR/social media duty (§12), guild-race side events vs rivals **[OPEN]**
- **Simulation granularity (user requirement, locked):** play day-by-day OR set the week and
  auto-simulate to the next raid day / next week / next decision point, with configurable
  interrupt rules ("stop on: injury, mail marked urgent, boss kill, loot ≥ epic"). FM's
  "continue until something matters" is the model.
- **The inbox** (§11) is the connective tissue: everything that happened while simulating
  arrives as messages you can act on.

### 6b. Automation: anything can be delegated **[LOCKED — a design pillar]**

Different players enjoy different halves of this game. Therefore: **every recurring decision has
a "decide for me" option that plays at ~90% of optimal.** Two flavors, one rule:

- **Diegetic delegation (preferred):** authority granted to staff — "co-leader, you have 800g
  and authority: find us a healer" (result quality scales with their ability, arrives via
  inbox); analyst pre-reads the logs; trainer runs development; assistant answers routine mail.
  Delegation *is* the staff system (§12) — you're not toggling settings, you're running an org.
- **Plain auto-options everywhere else:** auto-assign optimal dungeon farms, auto-set training,
  auto-distribute loot by policy (§9), auto-reply categories for the inbox, auto-sim raid
  nights with standing orders (§7).
- The rule: automation is *near*-optimal (90%), never perfect — hands-on play is always
  slightly rewarded, delegation is never punished. Every automation shows its work afterwards
  (what was decided and why), so trust builds and control can be taken back per-system at any
  time. FM's "delegate everything you don't enjoy" done deliberately from day one.
- **One-button coverage is a hard requirement [LOCKED]:** *almost every decision has a single
  button.* Examples: **plan my week** (pick a stance — Relax = lots of time off · Balanced ·
  Grind Hard = 4 raid days + dungeon grind — and the week fills itself); **equip everyone**
  (auto-assign best gear); **find players I need** (a shortlist of gaps). Where a button leans on
  a person, its *quality scales with that staff's ability* — a better co-leader's auto-shortlist
  is closer to what you'd pick by hand; a weak one's is rougher, nudging you to search yourself.
- **Manual is first-class too:** a strong **filter/search system** (class, role, stars, attributes,
  price, age, availability, social fit) so hands-on players can find raiders/gear themselves — the
  button just runs a good default query for you.

## 7. Raid night **[user sketch + PROPOSED details]**

- **"Formation" = composition + posture** (user's framing — locked as the design):
  - Composition: role counts (2 tanks / 5 healers / 6 melee / 12 ranged…) — validated against
    encounter needs you *believe* you know; some bosses genuinely want 3 tanks or 6+ healers.
  - **Role caps per boss [LOCKED]:** each encounter sets **min/max** per role — especially a
    **healer cap** — so you can't trivialize a fight by stacking healers (the classic "just
    bring more healers and never die" exploit). Caps make composition a real constraint: within
    the healer limit, is *this* boss a "run min healers, race the enrage" fight or a "max
    healers, survive the damage" fight? The cap turns the healer-count risk dial (§8h) into a
    bounded, per-boss puzzle. Some bosses also *floor* roles (must bring ≥3 tanks).
  - **Posture** (per boss, changeable between pulls): *Aggressive* (max damage, more mechanic
    deaths, higher chance to push into unseen phases — high variance), *Balanced*, *Safe* (extra
    healer, conservative cooldowns, fewer deaths, slower kills, better for *learning*).
  - Assignments: tank order, interrupt rotation, soak groups, raid leader (§7b).
- **Boss learning system (the progression heart):**
  - Unseen content is fog: you know the boss's name and whatever scouting gave you. Phases
    reveal on reach ("at 70% it submerged and adds came — new phase discovered").
  - Guild-level **strategy knowledge** per boss (discovered phases, counter-tactics unlocked by
    your Tactics attribute + log analysis + wipes on that phase) and per-raider **mechanic
    execution familiarity** (improves with pulls & drills).
  - **Deliberately fuzzy readout (user requirement — locked):** no clean progress bars. You ask
    raiders in conversation — "Do you have the soak timing down?" — and their *answer quality
    depends on their personality* (a cocky raider says "easy" at 40% mastery; an anxious perfect
    player hedges at 95%). Your Judgement attribute sharpens what you can read. Logs (below) are
    the objective counterweight.
- **Combat model [LOCKED: full rework from DM1's turns]:** real-time-under-the-hood simulation —
  weapons with attack speeds, spells with cast times, bosses with attack timers and ability
  cooldowns, mechanics triggering on timers, elapsed time, and HP thresholds. This is exactly
  what the blueprint's tick model was designed for ([ADR-0003](adr/0003-tick-time-model.md);
  user's "0.25s ticks" vs the spec's 0.1s is a resolution constant to finalize in M0 —
  **[OPEN: tick resolution]**).
- **Damage model [LOCKED direction: hybrid — details deferred until engine work]:** spells
  definitely deal authored damage (class identity stays designed), but raider attributes shape
  *realized* output: the user's canonical example — a raider with high Movement/Mechanics loses
  less DPS during a movement phase; low Cooldown Discipline burns bloodlust badly; low
  Consistency swings pull-to-pull. So: abilities/class templates author the ceiling, attributes
  + condition + posture determine how much of it a raider extracts each pull, FM-style. Full
  formula design is an engine-phase task, not a today task.
- **Watchable combat view [LOCKED: warcraftlogs replay approach — full vision]:** top-down
  arena ("a small D&D map players move around on"): raiders as circles in class color with
  class icon, boss as a big circle, **telegraph shapes** (the boss's AoE circle appears →
  players inside must move, losing uptime; the melee-explosion ring around the boss → melees
  run out — and the one greedy DPS who stays too long visibly dies), HP unit frames bottom-left
  flashing on damage, boss frame with HP%, **live meters** bottom-right (DPS / healing / damage
  taken / interrupts / dispels, switchable mid-fight), and the **encounter progress bar** along
  the bottom showing discovered upcoming mechanics. **Familiarity is visible:** raiders who
  know the fight pre-spread before the splash, hold their movement tool ready, pre-pot the
  burst window — you literally watch experience. This *is* ADR-0005's renderer, now the product
  direction. Positions upgraded to true 2D coordinates to support it (engine-spec §7).
- **Feasibility triage [assistant's call, LOCKED as scoping]:**
  - *Core & buildable (M2):* arena + circles + telegraphs + movement + frames + meters +
    progress bar — all pure folds over the event stream; movement is straight-line with per-
    raider speed. The "greedy melee dies" moment is emergent from execution profiles, free.
  - *Later archetypes (M3/M4, cheap by then):* interaction mechanics — `carryObject` (pick
    something up, bring it to/away from the boss), positional arrangements (mechanicPhase
    variants). Just movement + state on the same rails.
  - *Deliberately never:* pathfinding and unit collision. Straight-line (and orbit-arc)
    movement only — the boring 20% we skip to afford the 80% that reads great.
  - *Door left open [dev]:* **simple terrain** — pillars/objects to hide behind (line-of-sight
    soak mechanics). Feasible without pathfinding: an occluder is geometry plus a designated
    safe spot raiders move to; a `hideBehind` archetype can land in any later tier.
  - **Melee movement quality [LOCKED]:** on keep-moving mechanics, good melee **orbit the
    boss** (arc path, uptime preserved); panicking melee run *away* radially and lose uptime.
    Orbit-vs-flee is a per-raider outcome of attributes/composure — visible in the replay.
- **Combat logs & reporting (locked):** every pull produces a log — the engine's event stream —
  surfaced WCL-style: damage/healing/interrupt/mechanic-fail tables per raider, death recaps
  (what killed them, what they were standing in, what CDs were unused), potion/cooldown usage,
  timeline scrubber synced to the replay view, week-over-week improvement views. Log analysis is
  a *gameplay verb*: reading it well (or hiring an analyst §12) is how you find who's failing
  the soak. Boss design draws direct inspiration from real WoW encounter timers/damage patterns.
- **Pulls per raid night [LOCKED: time budget]:** session hours are the budget; a pull costs
  roughly on the order of **1–2 hours of session time** (recovery, run-back, re-strategizing
  baked in — exact rate is tuning), so a 8h night is ~4–8 meaningful attempts and a 2h night is
  1–2. Deeper wipes eat more clock; Endurance matters late.
- **Between-pull adjustments [LOCKED — the raid-night verb set]:** after each wipe you can adjust
  before the next pull, or auto-simulate the whole raid day with standing orders:
  - posture shift ("play safer" / "go harder"),
  - **spot rules**: "always health-potion at *that* AoE", "defensive CDs on the brutal phase-2
    overlap", "bloodlust/hero on phase 3 push" — per-boss, per-phase tactical assignments that
    persist and improve with knowledge,
  - substitutions (fresh raider in, tilted raider out), consumable spend policy,
  - or hand the night to the raid leader / auto-sim and read the report after.
  This is the FM touchline-instructions moment; depth here comes from real boss-timeline design
  (see [research/boss-design-reference.md](research/boss-design-reference.md)).
  Whatever happens: **progress persists** — phase knowledge, per-raider familiarity, and "we
  wiped on X, drill X this week / sub someone in" are the between-nights loop.
- **The discovered boss timeline [LOCKED — the strategy UI]:** each boss has a **knowledge bar**
  that fills with pulls/analysis, and a **timeline view that reveals entries as you encounter
  them**: first time you survive to 0:30 and the absorb shield goes up, "0:30 — Barrier" appears
  on the timeline — and from the *outside*, before the next pull, you can now attach assignments
  to it (hold burst CDs for the barrier; potions at the heavy AoE; healer CD rotation across the
  raid-damage entries; tank defensives ladder for the enrage; healer-to-tank assignments). The
  timeline is the same data the engine runs (engine-spec §8) — you're literally annotating the
  encounter's real schedule as you uncover it.
- **Cooldown management & learning [LOCKED — the signature loop]:** the canonical arc, in the
  user's own example: *boss has a barrier at 0:30, heavy AoE at 1:00, enrage low. Pull 1:
  everyone dumps CDs on pull, barrier arrives, no burst left, wipe. Healers did the same —
  nothing for the heavy AoE, wipe again.* Then three intertwined fixes:
  1. **Raiders adapt on their own** at different speeds — Learning attribute (and
     "learner"-type traits) governs how fast each raider re-times their CDs for a known
     timeline entry; naturally some are fast, some never quite get it.
  2. **You manage it from outside** — timeline assignments as above ("all burst held for
     0:30"), at the cost of rigidity (held CDs = lower overall DPS; the max-dps-vs-guaranteed-
     burst middle ground is the puzzle).
  3. **The log tells you who defected** — post-raid you see exactly who dumped CDs into the
     wrong window; train them (drills), bench them, or accept the variance.
  The same triangle applies to healers (CD rotation vs the damage schedule), tanks (defensive
  ladder vs tank-buster/enrage entries), and personal survival (self-defensives and potion
  usage are attribute-driven: some raiders trade a little DPS to live, some don't — visible in
  death recaps).
- **Fight math shape [LOCKED]:** one big pre-pull computation from everything known (roster
  stats, gear, class kits, posture, assignments, per-raider boss familiarity, guild strategy
  knowledge) → the tick sim runs it out with mechanics testing the raid every few seconds →
  post-pull, boss-specific state updates (knowledge bar, familiarity, revealed timeline
  entries). Target boss length ~2 minutes early-tier with a mechanic touching the raid every
  handful of seconds; CD periods (~1 min) against fight length create the use-now-or-hold
  tension deliberately.
- **Between-pull flow:** wipes cost time within the raid session; you spend the session budget on
  pulls; between pulls: posture change, quick substitutions, a motivational call (Leadership),
  consumable policy (potions cost gold — spend on progression pulls or save?).
- **7b. Raid leader [user idea, PROPOSED]:** assign a raider (or hire a dedicated non-playing
  raid leader — unique character type) whose Leadership/Communication runs the night moment to
  moment; you set strategy, they execute. Bad raid leader = your plan degrades in the noise.

## 8. Raiders **[user sketch + PROPOSED details — the FM player-page depth]**

Every raider is a persistent unique person. The raider page is the game's FM-player-screen moment.

- **No character levels [LOCKED].** Leveling doesn't fit the FM frame. Raider power = attributes
  (trained), per-role stars (developed), gear (farmed), and talents. The season's power curve
  comes from gear item-level bands, not XP. *(One exception: the **manager** levels — you gain
  XP from milestones and assign skill points to your own §2 attributes.)*
- **Classes & roles [LOCKED: all 12].** The full DM1 roster returns (arcanist, oracle,
  hexweaver, rogue, fel striker, naturalist, martial sage, elementalist, ranger, oathkeeper,
  warrior, grave knight), redesigned for this game's roles (tank/healer/melee/ranged), all as
  registry rows because more classes will be added later. **[OPEN: armor types**
  (cloth/leather/mail/plate gating gear competition between classes — classic loot-drama
  generator)**]**.
- **Talents [LOCKED direction]:** kept, but **not level-gated** and rebalanced away from passive
  +damage% rows toward **fewer, higher-impact choices** (playstyle switches, mechanic tools,
  cooldown reworks) — build identity, not stat taxes.
- **Per-role star rating (locked):** 1–5 stars in 0.5 steps, *per role* — a raider can be a
  4★ tank / 3★ healer / 2.5★ DPS. Stars = current competence ceiling in that role;
  **role retraining** (locked): you can invest weeks converting your 4★-tank-who-hates-tanking
  into the healer they could be. Hidden *potential* per role caps growth (scouting/Judgement
  reveals it fuzzily) **[PROPOSED]**.
- **~10 trainable attributes, 1–20 scale** (user requirement; FM-style. Proposed list):
  1. **Mechanics** — executes known dances cleanly
  2. **Awareness** — reacts to *new/unseen* mechanics (progression MVP stat)
  3. **Cooldown Discipline** — defensive/offensive CD timing
  4. **Resource Control** — mana/potion/consumable efficiency
  5. **Consistency** — pull-to-pull variance (low = coin-flip raider)
  6. **Composure** — the user's "tilt-proof": performance after wipes/deaths
  7. **Communication** — calls out, helps others' mechanics, feeds the raid leader
  8. **Learning** — how fast familiarity grows per pull/drill
  9. **Preparation** — shows up flasked, on time; attendance reliability
  10. **Teamplay** — follows assignments, unselfish (loot drama modifier)
  11. **Endurance** — performance decay over long sessions (hour 6 of 8)
  *(11 listed; trim or merge to taste — e.g. Resource Control into CD Discipline)* **[OPEN: final list]**
- **Personality traits** (DM1's layer, kept *on top of* attributes): flavor + behavioral hooks —
  loot-greedy, drama queen, mentor, clutch, homesick — driving events, morale interactions, and
  answer-style in conversations (§7).
- **Condition system [LOCKED — FM-modeled, the stamina rework over DM1]:** DM1's single stamina
  bar is replaced by FM's proven **two-axis** split (see [economy-model.md §1](economy-model.md)):
  - **Condition (freshness):** drains as a raider fills activity slots — raid nights and
    back-to-back full days drain hardest — and recovers on empty/rest slots. **Recovery rate is a
    raider attribute** (Endurance/Natural-Fitness-analog) boosted by the Sleeping Quarters venue
    (§9c). Low condition = worse performance and higher injury risk.
  - **Sharpness (readiness):** built by *actually raiding*, decays when benched. So a rested-but-
    benched raider is fresh yet **rusty** — you can't just rotate freely; benching has a real cost,
    which drives roster tension and morale.
  - **Morale** stays its own axis (wants to raid? needs motivating?). Three axes, distinct.
- **Injuries [LOCKED — FM-modeled, no permadeath]:** low condition + heavy load raises injury
  chance, modulated by an **Injury-Proneness** trait; severities scale from a knock (a slot or two
  off) to serious (weeks out), RPG-flavored (a lingering curse, a training wound). The **Infirmary**
  venue + medical staff + rest/recovery activities cut risk and duration. Injuries arrive via inbox;
  a rare **career-ending injury** is the dramatic ceiling (ties to aging §8). No raider ever dies.
- **Development:** training assignments target attributes; playing time grows stars;
  punishments/rewards (bench, fine, holiday, public praise) move morale and behavior (user
  idea — kept).
- **Aging & career arc [LOCKED — the model that keeps the world self-sustaining across 10–20+
  tiers]:**
  - **Age band, football-style:** raiders enter at **16–18**, peak mid-20s, and **retire around
    28–30** — ~30 is the effective ceiling. A ~12-year career at our raid cadence is roughly
    **~30 raids** in a lifetime.
  - **Growth is an age curve, not flat points** — this is the key to "not capped too fast, not
    absurd totals". Each raider has a hidden **potential** per role (already in the `Vocation`
    component — see [entities-and-worldgen.md §2](entities-and-worldgen.md)). Current attributes
    *approach* potential quickly in **development years (16–22)**, are stable at **peak (23–27)**,
    and **decline past ~28**: twitch attributes (Mechanics, Awareness, Endurance) fall while
    wisdom attributes (Composure, Preparation, Communication, Teamplay) can still rise. Total
    career gain is **bounded by potential**, not by summing a flat per-season rate — so a long
    career never produces runaway numbers, yet there are always meaningful development years.
    Injury-proneness rises late; reliability rises with it. Veterans become mentors/raid-leaders,
    not dead weight.
  - **Cadence drives the clock; the architecture doesn't care.** Age is derived from birth-season
    vs the world clock ([entities §3.3](entities-and-worldgen.md)), so whatever raids-per-year we
    settle on, the model just works — only the *balance* (growth-per-raid × career length) needs
    tuning, never the code. **Coupling to flag:** raids/year, career span, and the growth curve
    must be co-tuned so a career is a satisfying arc (see the calendar open question). More raids
    per year ⇒ faster aging in raids ⇒ lower growth-per-raid to hit the same lifetime total.
  - **Conveyed through the world, not a tooltip:** the player learns the ceiling by *seeing* that
    almost no active raider is over ~30, and via **newspaper/inbox retirement announcements**
    ("Veteran tank hangs up the shield at 31, citing the toll of the fight"). Aging is felt as
    world texture (visible-stories pillar), and youth intake (§10) refills the base.
- **Contracts & transfers:** raiders have salaries and contract ends; other guilds poach (§11);
  you can sell ("release for a transfer fee"), buy, or recruit free agents (~400 in the pool).

### 8a′. Attribute resolution — how 1–20 becomes combat outcomes **[user — LOCKED]**

The bridge between the attribute system and the sim (the execution layer of the hybrid model,
§7). **Baseline is 10** = neutral. Two attribute kinds:

- **Scalar attributes → a small multiplier around 10.** Each point above/below 10 shifts output
  by a tuned per-point amount (≈±1%): `mult = 1 + (attr − 10) × perPoint`. So 10 = 1.00×,
  11 = 1.01×, 9 = 0.99×. Applies to e.g. **Damage** (output) and **Defense** (damage-taken
  reduction). Cheap, readable, and the same shape for every scalar stat.
- **Behavioral attributes → *decisions* the sim resolves, deterministically (seeded):**
  - **Consumable use (potion timing):** every raider carries one combat health-potion; the
    attribute sets *when/whether* they use it well — 20 pops it at the perfect moment, 1 uses it
    near-randomly or never (wasted).
  - **Movement:** governs the uptime-vs-death tradeoff on movement mechanics — 20 calculates
    whether they can finish the cast then move, holding melee uptime until the last safe instant;
    10 either bails early (loses DPS) or too late (takes damage); 1 barely moves (takes the hit).
  - **Defensive use:** whether they fire their defensive CD at a good moment or waste it.
  - **Mechanics (targeted "run out / run in / LoS"):** resolved by a **Baldur's-Gate-style
    roll** — the mechanic has a difficulty (DC); the raider's relevant attribute is the base;
    a seeded roll decides pass/fail. Example: DC 17, attribute 14 → gap 3 → a d20 roll needs to
    beat 3 (~85% pass). Attribute ≥ DC = automatic pass; low attribute = needs a high roll. All
    rolls seeded → golden-testable.

Per-point values, DCs, and die size are **tuning knobs** (Principle 0), set in the balance-sim
phase. This model *is* the greed↔panic spectrum (§8d) and the hybrid damage layer made concrete —
scalars set realized output, behaviors set who lives, dpses through, or dies. It also confirms the
attribute list is **execution-flavored** (Damage, Defense, Movement, Consumable-use,
Defensive-use, Mechanics/Awareness, plus the softer ones — Learning, Composure, Communication,
Preparation, Endurance); final list stays registry-flexible (add/remove by a row).

### 8b. The trade market **[user: "a huge part again" — LOCKED as a major system]**

The FM transfer-market moment, RPG-flavored:

- **Deep player profiles:** everything from §8 (attributes as your Judgement lets you see them,
  per-role stars, traits, condition) PLUS **career history**: previous guilds, past season
  placements, world-first kills they were part of, notable logs, injury history, why they left
  their last guild (drama flag). All of it real data from the living-world sim, not flavor text.
- **Asynchronous negotiation [LOCKED]:** conversations take world-time. Send an approach today;
  the reply lands in your inbox tomorrow (or in three days — personality-dependent). Multi-round:
  interest probe → salary/role expectations → formal offer → counter → agree/collapse. Rushing
  (deadline offers) works on some personalities, insults others.
- **Delegated scouting [LOCKED]:** brief your co-manager/scout — "find me a tank, ≥3.5★,
  budget X, must fit our culture" — pay the fee, and days later a shortlist arrives with their
  (quality-dependent) assessment. Better scouts = truer shortlists.
- **Both directions:** rival guilds approach *your* raiders (loyalty, morale, and your Charm
  decide whether you even hear about it in time to counter); selling a star for guild funds is
  a real, painful lever.
- This system is the biggest consumer of the ~6,500-unique-characters requirement — every
  shortlist candidate needs a real history to inspect.

### 8c. Class & combat design philosophy **[user — LOCKED]**

- **Unique but structurally similar.** Every class normalizes to the same throughput skeleton so
  balance stays tractable: in one time window, a rogue hits twice with daggers for *x* each, a
  warrior lands one 2-hander for *2x*, a caster finishes one cast for *2x*. Class uniqueness
  lives in *texture* (cadence, tools, mobility), not in raw budget. Damage scales primarily with
  gear (§9) — the class defines the shape, gear defines the size, attributes define how much of
  it survives contact with mechanics (§7 hybrid model).
- **Mobility toolkits are the identity layer.** The same movement mechanic hits classes
  differently, by design: a *big AoE splash on ranged* → the hunter-analog keeps full DPS on
  the move, the mage-analog spends a blink then turrets on, the priest-analog walks and bleeds
  damage. Melee mirror it on *boss explosion* mechanics: some melee are better at getting out,
  some at getting back in (uptime recovery). Each class gets a small authored toolkit (mobile
  shooting / blink / dash / immunity bubble / passive soak…) that the mechanic runtime consults —
  this is where "unique but similar" pays off in the replay: you *see* the blink.
- **Mechanic × class × attribute interaction is the sim's core formula:** mechanic archetype
  defines the demand (move for 3s, spread, run out), class toolkit defines the discount
  (blink = 0.5s loss instead of 3s), raider attributes (Mechanics, Awareness, Movement-ish)
  define the execution, posture defines the risk tolerance (stand and cast through it?).
- **Tier sets [LOCKED direction]:** set bonuses come from a designed **rotating pool** of
  generic, class-flavored bonuses (registry rows, reused across seasons with new ilvl bands) —
  no infinite bespoke authoring. **Validity window mirrors ilvl banding: tier-N set bonuses stay
  active through tier N+1, dead at N+2** (season 8's set works in 9, stops in 10) — so "break my
  old set for the new one?" is a real mid-tier decision, and legacy farming stays useful but
  never abusive.

### 8d. Class kit framework **[user — LOCKED structure]**

No rotations to press — everything auto-plays — so a class is defined by how it behaves across
**combat states** and what tools it owns. The engine evaluates each raider at every **action
slot** (GCD ≈ 1–1.5s):

- **States (computed from what's happening to the raider):**
  - **Turret** — free to execute the max-DPS priority.
  - **Light movement** — repositioning; instants/ranged-capable spells only, small delays.
  - **Heavy movement** — survival footwork; damage near zero, instants on the run at best.
  - **Mechanic** — executing a job (soak, carry, kick); damage contribution per the mechanic.
- **The action-slot decision (user's canonical scenarios, kept):** a telegraph lands under a
  turret'd caster; safety is 2 move-actions away and impact is 4 slots out → move+instant,
  cast, cast, move+instant — near-zero loss. Same telegraph with 2 slots of warning → move,
  move, done — that's heavy movement. Or the raider **finishes the cast they already started**
  (greed), leaving only 1 slot to cover 2 tiles → they die, *unless* they own a blink (2 tiles
  in 0 actions) or a sprint (double movement). Where a raider sits on the
  **greed↔panic spectrum** is personality + attributes: the max-DPS player tanks the hit and
  hopes; the panicker drops to zero DPS even in light movement. Evaluated per slot,
  deterministic, and the source of every good replay story.
- **Class differentiation axes (each class picks a profile per axis):**
  filler style (hard-cast / instant-weave / dot-refresh) · hit profile (few heavy vs many
  small) · instants available while moving (none → several) · dot maintenance load ·
  mobility tool (blink / sprint / leap / passive speed / mobile casting) · defensive profile
  (long-CD big DR vs short-CD small DR vs self-heal vs just more HP) · **every class has a DPS
  cooldown** (burst window to plan around).
- **Tanks:** mostly positioning + survival (tank-and-spank baseline), pick up add waves,
  tank-swap debuffs; their kit depth is the defensive-CD ladder against the boss timeline.
- **Healers:** same state/decision system with heals — cast vs instant triage, AoE tools,
  **smart-heal targeting** (no triple-flash on one target). **Mana is the encounter clock
  [LOCKED]:** tuned so perfect mechanical play lasts the full ~2min fight and sloppy play
  (avoidable damage, no self-mitigation) runs healers dry early — "are we playing mechanics
  before the healers go oom" IS the wipe condition most nights.
- **Talents [LOCKED shape]:** no level gates. Per class, three small columns —
  **damage style** (e.g. dot-amp vs consistent vs burst/execute), **survivability**,
  **movement** — each a real either/or, not a stat tax (per §8 talent philosophy).
- **Reference kits (user-authored, the template density for all 12):**
  - *Rogue:* Mutilate filler (~1s GCD) · a bigger hit every 5th filler (a **fixed cadence baked
    into the kit — not a managed resource bar**; the AI may hold it 1–2 slots for a planned
    burst window, §7) · poison dot (5s, refreshed by filler) · Shadow Blades DPS CD (bonus
    shadow damage window) · 25% DR on ~30s CD · Sprint ~30s CD (double movement 5s).
  - *Warrior:* Rend (10s dot) · Mortal Strike auto-cast · **Execute replaces Mortal Strike
    below 25% boss HP** (execute-window class identity) · Recklessness (crit-chance CD) ·
    60% DR on 2min CD · Leap 1min CD (instant 3-tile relocation).

### 8e. Raid utility & class capabilities **[decided this pass]**

The composition-depth layer — classes doing things *for each other* so bringing one of each has
value, not just "start my 20 highest ratings":

- **Raid buffs [LOCKED]:** each class provides a **unique, impactful** passive raid aura, so
  bringing one of every class has real value (coverage vs raw parses). Not stackable dupes —
  distinct effects (e.g. a stamina aura, a crit aura, a magic-damage-taken aura on the boss).
- **Externals [LOCKED]:** some classes cast a shield / damage-reduction / heal on *another*
  raider — assignable to the boss timeline exactly like CDs ("external the tank on the 0:30
  buster"). Cheap to build, big assignment depth.
- **Raid burst [LOCKED — needs a non-WoW name; working term "Rally"]:** a once-per-fight
  raid-wide **damage amp** for a duration (a *damage* boost, not attack-speed — haste is parked
  §9). One or more classes bring it; **timing is a choice: manual (drop it on a specific
  timeline entry) or auto (default: on pull, or held for a barrier/push window)**. This is the
  canonical "when do we pop it" decision and it plugs into the shield-break math (§7).
- **Battle-rez analog [REJECTED for now]:** no mid-fight revives at launch. Revisit later; its
  absence keeps death consequences sharp.
- **Interrupts [LOCKED IN]:** class-gated capability to interrupt boss/add casts (the
  `interruptibleCast` archetype already exists engine-side). A must-kick boss becomes a roster
  constraint (enough interrupters in the 20); interrupt *timing/reliability* is a raider trait
  (some snap-kick, some are late). Working ability name: **"Counter"**.
- **Dispels [LOCKED IN]:** class-gated debuff removal. Pattern: a debuff mechanic lands on ~10
  raiders, ~4 are *dispellable* (removed by dispel classes) and the rest must be **healed
  through / mitigated with self-defensives** — dispel capacity and healing throughput both
  tested. Dispel *speed* is a raider trait (some clear fast, some lag). Working name:
  **"Cleanse"**. Costs an action (a dispel is a heal/DPS not cast that slot).
- **Target profile [LOCKED]:** single-target / cleave / AoE is a class trait, and *swap
  discipline* is a raider trait — some snap onto priority adds, some keep padding the boss and
  let adds run wild (dying passively to them). Classes carry AoE tools of varying shape:
  spammable AoE, long-CD AoE nuke, AoE dots. Makes add-heavy bosses and specific farm dungeons
  prefer specific comps.
- **Cast commitment [LOCKED]:** mid-cast, the raider decides by **how far into the cast they
  are** — nearly done → finish it (greedy but efficient); just started → cancel to move/defend.
  Progress-weighted, personality-tuned; this is the risk axis of the whole state machine at zero
  extra system cost.
- **Pets/summons [LOCKED — deliberately minimal]:** pets get **no icon, no independent AI** —
  modeled like a dot (a steady damage component tied to the owner). Optional wrinkle: a pet may
  carry a small HP bar so it can *soak* a mechanic, then die. Nothing more; kept simple on
  purpose.
- **Off-healing / hybrid flex [minor, PROPOSED]:** some DPS/tanks can throw an emergency heal —
  blurs the healer-count math, rewards flexible rosters. Nice-to-have.
- **Downtime behavior [emergent]:** during untargetable phases, dot classes keep ticking (retain
  value), pure-burst classes idle — falls out of the kit model for free; intermission-heavy
  bosses will favor different comps naturally.

**Net effect:** composition becomes "do I have buff coverage, a raid-burst, enough
interrupters/dispellers, and the right target profile for *this* boss" — FM squad-building depth,
the opposite of DM1's "highest OVR wins".

### 8f. Healer kits **[user — LOCKED structure; names coined]**

Same state-machine and per-action-slot decisions as DPS (§8d), but the output is healing and the
governing clock is **mana** (§8d: tuned so flawless mechanical play lasts the ~2min fight; sloppy
play — avoidable damage, no self-mitigation — runs healers dry early). The wipe question most
nights is *"do we play mechanics before the healers go oom?"*

- **Kit shape (every healer):** an efficient **filler** heal · a **HoT** · a reactive
  **instant/smart** heal · an **AoE** heal · a **healing cooldown** (big throughput burst) ·
  a class-gated **Cleanse** (§8e). Plus the AoE-tool variety DPS have (spammable vs long-CD).
- **Mana-cost hierarchy [LOCKED]:** costs are tuned so *how* you heal is the decision —
  - **AoE heals: expensive.** Blanket-healing a raid eating avoidable damage drains mana fast —
    this is the enforcement mechanism: if DPS just tank mechanics, healers burn AoE mana and go
    oom quickly (the wipe). Some AoE/smart heals also gated behind **cooldowns**, not just cost.
  - **Slow single-target heals: cheap**, mostly kept on the tanks (the steady, affordable
    baseline).
  - **HoTs: mana-conserving** — pre-applied, they cover steady damage far cheaper than reactive
    AoE. Proactive healing = efficient healing.
  So the raid's *mechanical* play directly sets the healers' mana burn: clean play → cheap
  tank-healing + HoTs last the fight; sloppy play → expensive emergency AoE spam → oom → wipe.
- **Core healer decision — efficiency vs throughput [LOCKED]:** big fast heals cost more mana
  and heal more; efficient heals sip mana and heal less. A greedy healer over-heals and goes oom
  before the enrage; a disciplined one paces mana to the timeline. Attribute/trait-driven,
  visible in logs (overheal %, mana curve).
- **Smart-heal targeting [LOCKED]:** heals auto-pick real targets so healers don't all flash the
  same low raider (wasted overheal). Assignment **roles** you can set (or auto): **tank healer**,
  **raid healer** (AoE/spot), **hots/proactive**, **cooldown healer** — mirrors real raid
  healing assignments and makes healer composition a puzzle, not a pile.
- **Offensive healing [LOCKED]:** when the raid is topped, healers contribute DPS — another
  greed/safety dial (pad the damage meter vs bank mana for the next burst). **Requires
  cast-cancel logic:** a healer mid-DPS-cast must be able to abort it to react to sudden raid
  damage — reaction quality is attribute-driven (a slow healer eats the cast and someone dies;
  a sharp one cancels and saves them). Same cancel logic as the DPS cast-commitment decision
  (§8e), pointed at healing.
- **Reference kits (template density for all healer-capable classes):**
  - *Oracle (light/direct healer):* **Mend** (efficient filler cast) · **Solace** (HoT) ·
    **Flash Mercy** (fast, expensive instant — the "oh no" button) · **Sanctified Ring** (AoE
    burst heal around a target) · **Devout Chant** (CD: channel, big heal every ~1s for 5s).
    Identity: reactive, burst, direct — strong at spot-saving, mana-hungry.
  - *Naturalist (HoT/proactive healer):* **Bloom** (cast: direct heal + leaves a HoT) ·
    **Renewal** (instant HoT) · **Grovewind** (spreads HoTs across hurt raiders) · **Serenity**
    (CD: channeled raid-wide HoT wave). Identity: proactive, HoT-blanket, efficient — strong at
    steady raid damage, weak at sudden spike-saves.
  - (The efficiency-vs-throughput and direct-vs-HoT contrast between these two is the axis every
    other healer class picks a spot on.)

### 8g. Tank kits **[user — LOCKED structure; names coined]**

Baseline is tank-and-spank: hold the boss, survive the tank mechanics. Depth is the **defensive
ladder** against the boss timeline and the **swap dance**.

- **Kit shape (every tank):** a **filler** that generates **active mitigation** (used well =
  less damage taken; let it drop = spikes — a real skill expression) · a **short-CD defensive**
  (small DR, frequent) · a **long-CD defensive** (big DR, saved for busters/enrage) · a
  **movement** tool · a class-gated **Counter** (interrupt) · a **Taunt** · **add pickup**
  (grab add waves — swap discipline trait applies).
- **Taunt logic — the taunt window [LOCKED, revised]:** hard tank-swaps are mostly *not* a
  thing. Instead, when a swap is needed there's a **~10-second window** in which either tank may
  taunt, and the tanks **decide within it based on state they can read** — primarily the *other
  tank's* status: their debuff stacks/duration and whether their defensive CD is up. Good
  behavior: "the other tank still has a defensive up and low stacks → I hold; their stacks are
  peaking and CD's down → I taunt now." Trait/attribute-driven quality: a sharp tank reads it
  and taunts at the right moment in the window; a poor one taunts too early (wastes it / grabs
  before ready), too late (edge of the window — spike risk), or misreads the other tank's CDs.
  The window makes swaps a *judgment* the raider makes, not a scripted stack-count — trainable
  and benchable, and a genuine reason two competent tanks matter.
- **Defensive-CD usage [LOCKED]:** tanks spend defensives **only while actively tanking** (not
  while off-tank), and ideally align the long-CD to timeline busters/enrage (assignable, §7).
  A bad tank blows the big CD early and eats the buster bare.
- **Threat kept minimal [LOCKED — scope guard]:** no deep threat/aggro sim. "Active tank" = whoever
  holds the boss; taunt swaps it. No DPS-pulls-aggro minigame — that's simulation cost with
  little management payoff.
- **Reference kits:**
  - *Warrior (physical/active-mitigation tank):* **Shield Block** (filler-driven active
    mitigation vs physical) · **Bulwark** (short-CD DR) · **Last Stand** (long-CD big DR + HP) ·
    **Charge** (movement/gap-close) · **Counter** · **Taunt** · add-grab cleave.
  - *Grave Knight (magic/self-sustain tank):* mitigation via a **self-shield** that scales with
    damage dealt · **Bone Ward** (short-CD magic DR) · **Deathwall** (long-CD) · **Grip**
    (pull an add to them) · **Counter** · **Taunt**. Identity: magic-damage tank, self-healing,
    contrasts the warrior's physical/block profile.

### 8h. What else — healer/tank layer **[assistant — noted]**

Smaller things that fall out of the above, flagged so they're not forgotten:

- **Overheal & mana curves are first-class log data** (post-raid: who overhealed, whose mana
  cratered) — feeds training and benching decisions, same as DPS parses.
- **Dispel/interrupt opportunity cost** is real: a healer dispelling isn't healing that slot; an
  add-kicking DPS loses uptime — so heavy-dispel/heavy-kick fights implicitly tax throughput.
- **Tank co-dependence:** a swap fight needs *two* competent tanks; one weak tank is a roster
  hole a boss can target. Mirrors FM's "you need two good center-backs."
- **Healer count as the core raid-size lever:** the composition/"formation" decision (§7) is
  mostly "how many healers can we afford to drop for DPS and still outpace the damage before
  oom" — the central risk dial, now grounded in the mana clock.

## 9. Gear **[LOCKED as a headline system — user: "gear will be a big point of the game"]**

Gear is the second progression axis: item level gates raid viability; pre-farm before release,
then farm lower difficulties to fuel the top one. **Quality bar (user, locked as a project rule):
no bare-bones version of this — features here ship whole or get scrapped.**

- **Item slots [LOCKED]:** helmet, shoulders, necklace, chest, gloves, pants, boots, rings,
  trinkets, and **two weapon slots**. **[OPEN: ring/trinket counts — assume 2 rings + 2 trinkets
  (classic loot-pressure doublers) unless vetoed]** — ~13–14 slots per raider vs DM1's 3.
- **Acquisition ladder [LOCKED]:** many sources, ilvl-ranked —
  `solo questing < grinding/currency < 5-man dungeons < professions & AH (BoE) < raids (top)`,
  with each raid difficulty a band above the last. Characters span a much wider ilvl range than
  DM1; per-slot upgrade-hunting across sources is the farm game.
- **Binding [LOCKED]:** **bind-on-pickup** (only participants of the run can use it; equipping
  soulbinds) vs **bind-on-equip** (tradeable, feeds the auction house until worn). Each raider
  has a personal bank for their soulbound pieces — kept *shallow* deliberately: no inventory
  Tetris, and gear-flow automation (below) means you only touch it when you care.
- **Scalable by formula [LOCKED]:** items are generated from slot/rarity templates where
  `stat = coefficient × ilvl` (curves in one tuning module). New season = new ilvl band, every
  item source scales automatically. No hand-authored stat blocks except uniques/trinket procs.
- **5-man dungeons:** a roster of distinct farmable dungeons (unique bosses/mechanics — an easy
  DM1-scale version of the raid engine), each with its own loot table, so *which* dungeon a
  group farms is a real choice ("force the weapon dungeon" vs auto-optimal). Loot is BoP to the
  group of five.
- **Loot assignment & drama [LOCKED, with the quality bar applied]:** when a piece drops that
  two raiders want, distribution is a decision: set a standing policy (need-based auto — the
  default automation) or intervene case-by-case (council mode). Passed-over raiders react per
  personality — Teamplay/traits decide who shrugs and who becomes drama ("not everyone is loot
  drama andy"); reactions scale with drought length, item significance, and rival recipients.
  Most loot flows silently; drama is the exception that makes headlines. If this can't be made
  to feel real, it gets scrapped, not stubbed.
- **Content tiers & the difficulty lever [LOCKED — one lever, scales content]:**
  - **Farming / questing: no tier, free** — reliable gold/gear faucets, no fail state.
  - **5-man dungeons: low tier, near-guaranteed clear** — meet the min-ilvl and you clear (DM1's
    model); failure % is tiny. A **single scale lever** can spin up heroic/mythic dungeon variants
    later purely by scaling — no rebuild.
  - **Raids: the real step up** — measured in **"raid units"** (one 2h slot of one raid day).
    A top guild raiding 7 days × 4 slots = **28 raid units/week**; an elite boss costs them
    ~8 units, so a 4-boss raid ≈ 32 units ≈ clears in ~8 days. An average guild needs ~12 units
    *per boss* early. **Over the season the boss gets easier** as you gain gear (more DPS) and
    knowledge (learned thresholds), so unit-cost drops week over week.
  - **Free knowledge drift:** a few days into a release, some boss-experience accrues *for free*
    (the world is figuring it out) — or **pay gold to pre-discover thresholds**, or ask a friendly
    top-guild leader for tips ([economy-model §3](economy-model.md)). **Killing a raid boss in one
    day is essentially impossible** — you must progress it: fill the bar, see the thresholds, use
    the right abilities, don't waste them.
  - **Raid difficulty tiers** (Normal/Heroic/Mythic-analog) remain a **data multiplier block +
    per-tier mechanic additions** (engine-spec `tuning` + difficulty-tagged timeline entries);
    higher tiers tighten mechanic *overlaps*, not just numbers (research/boss-design-reference §3).
    At launch: one tier, but the lever exists.
- **Item stats [decided]:** the 6 core stats (attack/magic/stamina/armor/MR/defense) +
  **Critical Strike [LOCKED]**. Accuracy and armor/magic penetration are **rejected** (WoW
  removed hit/expertise/ArP for reasons that apply doubly to us — see
  [research/gear-stats-reference.md](research/gear-stats-reference.md)). **Haste [PARKED]** —
  dev verdict "might be too complicated for our endeavours"; revisit only if the tick engine
  makes it nearly free in practice. **Mastery [PARKED]** — "adds a huge layer", needs thought;
  class identity can live in weapon-subtype affinities and kits meanwhile.
- **Item anatomy [LOCKED]:** WoW/DM1-style — items carry a **stamina/HP baseline** for their
  slot+ilvl, plus main stats whose count/size scales with **rarity/quality** (common → epic).
  All from the `stat = f(ilvl)` templates; rarity changes the budget multiplier and stat count,
  not the formula system.
- **Seasonal ilvl banding [LOCKED]:** bands overlap across seasons by design — **season 1 raid
  loot ≈ season 2 quest/grind loot**. Each season shifts the whole acquisition ladder up one
  band, so day-one of a new tier re-values everything without invalidating the *shape* of what
  you farmed (your S1 raid gear carries you into S2 prep, exactly like WoW).
- **Weapon types [PROPOSED, feasibility-filtered]:** categories 1-hand / 2-hand / shield /
  ranged, with subtypes (sword, axe, mace, dagger, wand, tome, fist, glaive, bow…). Class
  proficiencies are data ("rogues: daggers, fists, 1h-swords"); **subtypes carry stat-weight
  templates** so daggers naturally roll rogue-flavored stats — identity through generation, not
  rules text. *Trainable* proficiencies ("teach them glaives") deferred — fun but scope-risky;
  revisit post-M3. **[awaiting dev sign-off]**
- Consumables crafted/bought, AH price simulation lite, currency/badge system for deterministic
  gear catch-up **[OPEN: depth]**.

### 9b. Economy & finances **[user: "managing money will be an important part"]**

Adapted from FM's club-finance model (board-set budgets, multiple revenue streams, wage
discipline), scaled to **thousands of gold**, not millions:

- **Raider salaries [LOCKED: gold]:** every raider has a weekly gold wage per contract. The
  wage bill is the guild's dominant cost — exactly like FM. Star raiders are expensive;
  overpaying a declining veteran is the classic trap.
- **Budgets:** the board grants a **wage budget** and a **transfer/gear budget** per season,
  scaled by prestige + results + your negotiated contract clauses (§4). Exceeding wage budget
  needs board approval (relationship cost).
- **Revenue streams (mapped from FM):**
  - *Sponsors/patrons* — nobles, merchant houses, kingdoms back prestigious guilds (FM's
    sponsorships; scales with prestige tier and race results).
  - *Guild-hall income* — the "stadium gate receipts" analog, §9c.
  - *Race prizes* — placement rewards per raid release; world/region-first bounties.
  - *Transfer income* — selling raiders.
  - *Loot economy* — vendoring/AH-ing surplus drops, crafted goods from professions.
- **Costs:** wages, staff salaries (§12), consumables/repairs per raid night (progression pulls
  literally burn gold — flask policy is a real decision), venue upkeep, transfer fees, severance.
- **Financial pressure is content:** a struggling guild taking you as manager *because* they're
  broke is a valid career chapter; administration/collapse of AI guilds happens in the living
  world (their raiders flood the free-agent pool).

### 9c. The guild hall **[user: stadium-equivalent — LOCKED as a system, venues PROPOSED]**

The investable home base: spend spare gold now, earn compounding returns — FM's stadium/facilities
loop. Four upgradable venues (5 tiers each):

| Venue | Return on investment | FM analog |
|---|---|---|
| **Feast Hall** | Hosts feasts/holiday events (morale recovery efficiency); at higher tiers, *paying guests* attend famous guilds' feasts — direct gold income scaling with prestige (the "gate receipts") | Stadium + hospitality |
| **Sleeping Quarters** | Rest quality: stamina recovery per empty slot, burnout resistance; higher tiers reduce the morale cost of long schedules | Training facilities (recovery) |
| **Weapons Chamber** | Gear efficiency: cheaper repairs/enchants, small crafting bonuses, faster gear catch-up for new recruits | Data/sports-science facilities |
| **Training Grounds** | Attribute-training efficiency (§8), mechanic-drill quality; unlocks extra simultaneous training assignments | Training ground |

Possible fifth: **Trophy Hall** (displays world-first kills and season placements; passive
prestige + sponsor-negotiation bonus — and pure player pride). **[OPEN: exact venue list/tiers]**

### 9d. Professions **[user sketch — LOCKED direction]**

- **Gathering + crafting split**, RPG-generic names (deliberately not WoW's): e.g. Prospecting
  (ore), Foraging (plants), Fleshcraft/Skinning-analog (hides), Fishing; crafts: Smithing,
  Alchemy, Enchanting (a profession, per user), Cooking (with Fishing feeding it for recovery/
  feast buffs). **[OPEN: final profession list]**
- **Off-day integration:** gathering is an activity slot assignment (§6) — "send three to the
  ore fields this afternoon" — feeding materials to your crafters; or buy mats on the AH.
- **Incremental skill, per tier [LOCKED]:** skill starts at 0; each raid tier raises the cap by
  10. Craft ~5 items → skill 5 → unlock the tier's better recipes → craft ~5 more → cap at 10 →
  unlock the tier's best. Every new season adds a few chase recipes and raises the cap — small,
  predictable ladders across many tiers.
- **Scaling is free:** crafted items use the same `stat = f(ilvl)` templates as everything else
  (§9); a season's recipe refresh is mostly "same recipes, next ilvl band" plus a handful of
  new uniques. Profession BoEs sit near the top of the pre-raid ladder, which makes crafting
  economically relevant every single pre-patch window.
- **Material tiers [LOCKED]:** materials are tier-stamped; season-2 recipes require season-2
  mats — **old mats cannot craft current-power items, ever** (no stockpile abuse). Old mats go
  stale gracefully: vendor them, or **[PROPOSED]** a transmute-down sink (bulk-convert old mats
  into small gold/catch-up currency) so hoards don't feel like pure loss.
- **Legacy raids [LOCKED]:** old raids stay farmable (catch-up gear for new recruits, trophy/
  prestige kills, completionism) but drop *their* tier's loot and mats — legacy farming can
  never feed current-tier power.
- Automation applies (§6b): a standing order like "keep the guild stocked on flasks; sell
  surplus" is a one-time instruction to a steward/co-leader.

## 10. The living world (AI guilds) **[user sketch + PROPOSED]**

- All AI guilds run the **same simulation rules** (headless engine runs — no faked dice, per the
  no-mirror principle): same raid, same lockouts, their own schedules/postures/rosters.
  Performance budget matters: ~200 guild-weeks simulated per player week **[OPEN: fidelity
  tiers — full sim for nearby-ranked + rival guilds, statistical fast-path for distant ones;
  PROPOSED: yes, tiered]**.
- Leaderboards: world + region + prestige tier; pull counts and timestamps; weekly "power
  rankings" news.
- Transfer market between AI guilds (raiders move, careers happen without you); news feed
  generates stories from real sim results (world-firsts, roster drama, guild collapses).
- **Full inspection of any guild [LOCKED]:** browse all ~200 guilds anytime — their raid
  progress this season, roster with quick-glance ratings (spot their carries for poaching),
  finances/prestige, history. Iconic elite guilds sit at the top with the best players,
  fighting for top-10 clear times — the aspirational ceiling of a career.
- **Transfer rhythm [LOCKED]:** raiders normally move *between* tiers (after a clear or before
  a release), not mid-race — unless their guild is failing badly. Mid-race poaching of a
  contending guild's raider is rare, expensive, and dramatic.
- **Ecosystem churn [LOCKED]:** guilds disband after sustained failure (raiders flood the free
  agent pool), raiders retire (aging §8), and new young raiders enter via **youth programs**:
  every guild has one; you can **invest gold in yours** (facility-like: better intake quality)
  and recruit from it — resolves the academy question. Same world persists across your whole
  career (§4): quit, get fired, take a new job — the world remembers.
- **Nationality [OPEN — maybe cut]:** regions exist for flavor/name pools; whether raiders
  carry mechanical nationality (visa-like limits? cultural fit?) is undecided — *(Rec: keep as
  pure flavor at launch; no roster-composition rules.)*

## 11. Inbox & events **[user sketch — locked as a core system]**

FM's inbox is the game's heartbeat; same here. Everything lands as mail/messages: raider sick
notes, poaching offers for your star tank (accept the gold? morale hit either way), raiders
asking for role changes/raises/rest, rival trash talk before a race week, the 1% mount/item drop
celebration, board check-ins vs expectation, dungeon-run disasters ("group wiped 4×, morale
down"), sponsorship offers. Every event is: flavor text + choice(s) + consequences wired into
real systems (morale, gold, attributes, relationships). Event registry is content
(content-authoring.md pattern) so adding events stays one-row cheap.

## 12. Support staff & PR **[user ideas — kept, PROPOSED shapes]**

- **Co-manager / assistants (hire, salaried):** assistant manager (auto-handles chosen weekly
  chores — the "auto" layer is *diegetic*: you delegate to a person whose quality varies),
  log analyst (better post-raid readouts), recruiter/scout (better market info), trainer
  (training efficiency), physio-analog (injury/burnout care). Staff are unique characters too.
- **Social media / PR mini-layer:** interview prompts after milestones; answers trade off guild
  morale, board mood, your reputation, and rival heat — including the self-serving "throw the
  guild under the bus to court a better job" play (user idea — kept; costs are real).

## 13. Steam & meta **[LOCKED direction, PROPOSED details]**

Offline-first always. Optional: achievements; opt-in upload of finished-season summaries to
global leaderboards ("compare your world ranking / clear speed vs other players' careers").
Nothing gameplay-affecting comes down from the network. **[OPEN: ghost-season sharing — race
against a friend's uploaded guild results — later.]**

## 14. Anti-design: pain points we must not repeat **[from real FM player reviews — LOCKED as principles]**

The developer collected scathing FM reviews; each complaint becomes a testable design principle:

1. **The market must visibly respond to performance.** *(Complaint: "good performances, even
   constantly, don't translate into value; valuations stuck and hidden.")* Raider value is a
   published formula over visible inputs — stars, attributes, age, recent logs, titles — and the
   profile shows the trend and *why* ("value ↑ 15% since world-#40 kill"). No hidden stuck values.
2. **Prices must never scale off the player's wallet.** *(Complaint: "if you have 100k spared,
   the same player who wanted 30k now wants 100k.")* Wage/fee demands derive from the raider's
   market position, guild prestige gap, and personality — **never from reading the player's bank
   balance**. This is cheap to enforce (the negotiation AI simply has no access to that field —
   a package-boundary guarantee) and worth stating in marketing.
3. **Small-to-big must be a reachable arc.** *(Complaint: "you literally can't make a big club
   out of a small club; the game just puts you in bigger difficulty without a way out.")* The
   campaign bot must demonstrate it: a bottom-tier guild playing well compounds (prestige →
   sponsors → budget → recruits) into the top 20 within N seasons. This is a CI-checked balance
   target, not a hope.
4. **No AI cheating, ever — and say so.** *(Complaint: "the game cheats and hinders you; the
   match engine has the AI cheat.")* Structurally guaranteed here: AI guilds run the *identical*
   simulation (no-mirror principle). No rubber-banding, no hidden difficulty scaling. Losing
   streaks are diagnosable in the logs, not vibes. Make this a trust pillar in the store copy.
5. **Boards behave believably.** *(Complaint: "managers not sacked believably.")* Firing/
   patience logic follows the published expectation + trajectory + board personality; the board
   tells you where you stand monthly (inbox), so a sacking is never a surprise mechanic.
6. **No single dominant tactic.** *(Complaint: "match engine primarily rewards high pressing;
   corners too effective.")* The balance harness explicitly checks **posture diversity**: across
   the encounter suite, Aggressive/Balanced/Safe must each be optimal somewhere. A meta-solved
   posture is a red CI metric, DM1's "no class dead-last" rule generalized.
7. **Clarity for the amateur.** *(Complaint: "lots of settings, not clear where to click.")*
   Every screen answers "what should I do next?" — FM's genre-defining weakness is our
   onboarding opportunity: a suggested-actions strip, a "delegate everything you don't enjoy"
   assistant, and progressive disclosure of depth.

## 15. Spotted gaps — flagged by the assistant, not yet designed **[all OPEN]**

Things the journey walkthrough hasn't covered yet; each needs a design pass before M-planning:

1. ~~Tier sets~~ **RESOLVED** → §8c: rotating pool of generic set bonuses; tier-N sets live
   through tier N+1, die at N+2.
2. **Item enhancement layer.** Do items have enchant slots (feeds the Enchanting profession a
   per-item sink) and/or gem-socket analogs? DM1 had enchants; decide scope.
3. **Consumable loadout per raid night.** Flask/potion/food policy exists (§7, §9b costs) but
   the *loadout UI* and food-feast mechanics need a shape.
4. **Season awards ceremony.** Individual awards at season end — Tank of the Year, best
   parses, MVP — feeding raider reputation/value and manager prestige. (Mythic Manager
   does an "awards race" — the idea is proven in-genre.) Cheap: it's a fold over career data.
5. **Permadeath stance.** Wipes obviously aren't deaths — but *can* a raider ever die/be lost
   permanently (hardcore mode?), or is the worst case injury/burnout/quit? *(Rec: no permadeath
   in base game; "career-ending injury" as the rare dramatic ceiling.)*
6. **Guild bank.** Shared storage for BoEs/mats/gold with officer permissions — where loot
   policy meets economy; likely simple, needs deciding.
7. **Manager difficulty settings / ironman.** Board patience sliders, economy strictness,
   save-scumming stance (ironman mode?).
8. **Onboarding.** Anti-design §14.7 promises "amateur clarity" — needs an actual first-session
   plan (guided first week? mentor guild? tutorial career?).
9. ~~World persistence~~ **RESOLVED** → same living world across the whole career (§10);
   youth programs also resolved (§10: investable, recruitable).

---

## Open questions (top of the stack)

1. Tick resolution constant: 0.1s (spec) vs 0.25s (user's number) — decide in M0, golden-locked after.
2. Final raider attribute list (trim the 11 to ~10) and manager attribute list (7 proposed).
3. Armor types (cloth/leather/mail/plate) in or out?
3b. Item-stat recommendation sign-off (Crit/Haste/Mastery, no accuracy/pen) and weapon-subtype
    affinities — see [research/gear-stats-reference.md](research/gear-stats-reference.md).
3c. Ring/trinket slot counts (assumed 2+2). Final profession list.
3d. Final names (working terms in use): raid-burst "Rally", interrupt "Counter", dispel
    "Cleanse", plus the per-class raid buff set and the 10 classes' ability names.
3e. Offensive healing (healers DPS when raid topped): confirm in (recommended).
4. Hybrid damage model *formulas* (direction locked; design during engine phase with the
   boss-design research as input).
5b. **Raids-per-year cadence — now coupled to the aging model (§8).** 2/year ≈ 24-raid career
    over the 16→~30 band; 3/year ≈ 36. Cadence sets the growth-per-raid needed to hit a bounded
    lifetime total, so decide cadence + growth curve *together*. *(Leaning: 2–3 major/year.)*
6. AI-world fidelity tiers and the perf budget for simulating ~200 guilds weekly. *(Note: the
   seed+delta entity model — [ADR-0007](adr/0007-world-persistence-seed-plus-deltas.md) — is the
   same mechanism; fidelity tiers = how many deltas each guild accrues.)*
7. ~~Youth intake~~ **RESOLVED** → §10: every guild has an investable, recruitable youth program.
8. Economy depth: AH simulation lite vs vendor-style. *(Rec: lite — prices drift, no order book.)*
9. Region culture modifiers: flavor-only or mechanical?
10. Off-season filler content volume (mini-raids, dungeon seasons) between the 2 yearly releases.
11. Multiplayer-adjacent: ghost seasons via Steam — post-1.0 question.
12. Working title: "Raid Manager" still placeholder; naming matters in this niche (see DM1
    market notes on Mythic/Guild Manager collisions).

## Decision log

| Date | Decision | Status |
|---|---|---|
| 2026-07-06 | Offline-first, PC-only, standalone exe; Steam stats optional; browser = dev harness only | LOCKED (dev) |
| 2026-07-06 | FM-in-RPG-world direction; manager career (creation, contracts, firings) is core | LOCKED (dev) |
| 2026-07-06 | Season = raid release framed as portal opening; weekly lockouts; world race leaderboard vs 100–200 AI guilds | LOCKED (dev) |
| 2026-07-06 | Raid days 1–7 × 2–8h as the central schedule dial; off-day activity assignment; auto-sim to next decision point | LOCKED (dev) |
| 2026-07-06 | Formation = composition + posture; boss learning with fuzzy human feedback (no clean progress bars); logs of every pull | LOCKED (dev) |
| 2026-07-06 | Raiders: ~10 trainable 1–20 attributes; per-role star rating 1–5 in 0.5 steps; role retraining; morale/stamina | LOCKED (dev) |
| 2026-07-06 | Multiple raid difficulties must scale as data from day one | LOCKED (dev) |
| 2026-07-06 | Tech stack formally deferred until GDD acceptance; no DM1 inheritance by habit | LOCKED (dev) |
| 2026-07-06 | Calendar: ~2 releases/raid-year, gap sized by clear-time reality; WoW-style pre-patch gear ilvl window ~1 month before each raid | LOCKED (dev) |
| 2026-07-06 | Aging on: end-of-season aging pass — twitch stats decline with age, wisdom stats (patience/composure) can rise, injury-prone up, reliability up | LOCKED (dev) |
| 2026-07-06 | No character levels; growth = attributes/stars/gear/talents. Manager levels instead (self-assigned skill points). Talents not level-gated, fewer passives, higher impact | LOCKED (dev) |
| 2026-07-06 | DM1's full class roster returns (11–12, list TBC); lifestyle loop mostly cosmetic (house/mount/yacht-equivalent); guild economy in thousands, not millions | LOCKED (dev) |
| 2026-07-06 | Combat rework: real-time-under-ticks — weapon attack speeds, cast times, boss timers, time/HP-triggered mechanics | LOCKED (dev) |
| 2026-07-06 | Replay = warcraftlogs approach: top-down circles (class color/icon/letter), big boss circle, mechanic telegraphs; WCL-style post-raid reporting with death recaps | LOCKED (dev) |
| 2026-07-06 | Trade market as major system: deep profiles w/ real career history, async multi-day negotiations, delegated scout briefs | LOCKED (dev) |
| 2026-07-06 | Raid nights: time-budget pulls (~1–2h/pull scale); between-pull adjustments (posture, spot rules like "potion at that AoE" / "lust P3", subs) or auto-sim with standing orders | LOCKED (dev) |
| 2026-07-06 | All 12 DM1 classes return; system stays open for more | LOCKED (dev) |
| 2026-07-06 | Damage model direction: hybrid (authored spell damage × attribute-driven execution); formulas deferred to engine phase | LOCKED (dev) |
| 2026-07-06 | Monday weekly reset (Mon+Thu if bi-weekly); calendar screen core UI; RPG holidays timed to real-world seasons | LOCKED (dev) |
| 2026-07-06 | Day = 4×2h slots (noon/afternoon/evening/night); activities & raid sessions occupy slots | LOCKED (dev) |
| 2026-07-06 | Salaries paid in gold; FM-style budgets (wage vs transfer/gear) and revenue streams; guild hall as investable venue system (Feast Hall, Sleeping Quarters, Weapons Chamber, Training Grounds) | LOCKED (dev, venue details proposed) |
| 2026-07-06 | Anti-design principles from FM review pain points (§14): performance-driven visible valuations, no wallet-scaling prices, reachable small-to-big arc (CI-checked), no AI cheating, believable boards, posture diversity, next-action clarity | LOCKED (dev) |
| 2026-07-06 | Automation pillar (§6b): every recurring decision delegatable at ~90% optimal, preferably diegetically via staff; automation shows its work | LOCKED (dev) |
| 2026-07-06 | Gear: ~14 slots incl. 2 weapons; ilvl acquisition ladder (quest<grind<dungeon<crafted/BoE<raid); BoP/BoE + soulbind-on-equip + personal banks; formula-scaled items; loot policy/council with personality-driven drama; "ship whole or scrap" quality bar | LOCKED (dev) |
| 2026-07-06 | Professions: gathering+crafting split, enchanting/cooking/fishing in; per-tier skill caps +10 with craft-5-to-level ladders; seasonal recipe refresh rides ilvl bands | LOCKED (dev) |
| 2026-07-06 | Item stats: 6 core + Crit locked; accuracy/penetration rejected; Haste & Mastery PARKED (complexity concerns) | LOCKED (dev) |
| 2026-07-06 | Item anatomy: stamina baseline + rarity-scaled main stats; seasonal ilvl bands overlap (S1 raid ≈ S2 quest/grind) | LOCKED (dev) |
| 2026-07-06 | Materials tier-stamped — old mats never craft current-power items; legacy raids farmable but drop only their tier's loot/mats | LOCKED (dev) |
| 2026-07-06 | Classes: unique-but-structurally-similar (normalized throughput skeleton; identity via cadence/mobility toolkits); mechanic×class×attribute interaction is the core formula | LOCKED (dev) |
| 2026-07-06 | Tier sets: rotating pool of generic class-flavored bonuses; tier-N sets active through N+1, dead at N+2 | LOCKED (dev) |
| 2026-07-06 | Discovered boss timeline UI (knowledge bar + entries revealed on encounter, assignments attach to entries) and the CD-learning triangle (raiders self-adapt by Learning; manager assigns; logs expose defectors) | LOCKED (dev) |
| 2026-07-06 | Living world: inspect any guild anytime (progress/roster/quick ratings); iconic elite guilds; transfers between tiers not mid-race; disbands; investable+recruitable youth programs; same world across the whole career | LOCKED (dev) |
| 2026-07-06 | Combat view: WCL-replay-style arena (telegraphs, movement, unit frames, live meters, discovered-mechanics progress bar; familiarity visibly changes behavior); positions upgraded to fixed-point 2D; movement straight-line + orbit arcs; simple line-of-sight terrain door left open | LOCKED (dev + assistant scoping) |
| 2026-07-06 | Class kit framework: no rotations; combat states (turret/light/heavy movement/mechanic) + per-action-slot decisions on a greed↔panic spectrum; class differentiation axes; mana as healer encounter clock; 3-column talents (damage/survival/movement), no level gates; Rogue & Warrior reference kits | LOCKED (dev) |
| 2026-07-06 | Raid utility (§8e): unique impactful raid buffs (one-of-each-class value) LOCKED; externals LOCKED; raid-burst damage-amp ("Rally", manual/auto timing) LOCKED; battle-rez REJECTED for now; target profile + swap discipline LOCKED; cast-commitment LOCKED; pets minimal (no icon/AI, dot-like, optional soak HP) LOCKED; resource-economy archetypes REJECTED (out of scope for auto-combat) | LOCKED (dev) |
| 2026-07-06 | Interrupts ("Counter") & dispels ("Cleanse") LOCKED IN — class-gated, reliability/speed are raider traits, cost an action; dispel pattern ~10 debuffs/~4 dispellable/rest healed | LOCKED (dev) |
| 2026-07-06 | Healer kits (§8f): filler/HoT/instant/AoE/CD + Cleanse; mana = clock; efficiency-vs-throughput core decision; smart-heal targeting + assignment roles; offensive healing PROPOSED; Oracle (direct/burst) & Naturalist (HoT/proactive) reference kits | LOCKED (dev) |
| 2026-07-06 | Tank kits (§8g): filler active-mitigation + short/long defensives + movement + Counter + Taunt + add pickup; defensives only while tanking; threat sim kept minimal; Warrior (physical/block) & Grave Knight (magic/self-shield) reference kits | LOCKED (dev) |
| 2026-07-06 | Healer mana-cost hierarchy: AoE heals expensive (the oom-enforcement lever), slow single-heals cheap (tank baseline), HoTs conserve, some heals CD-gated; offensive healing LOCKED IN with cast-cancel logic | LOCKED (dev) |
| 2026-07-06 | Tank swaps = ~10s taunt window; tanks decide within it by reading the other tank's debuff stacks + defensive-CD state; quality is trait-driven (not a scripted stack count) | LOCKED (dev) |
| 2026-07-06 | World persistence = seed + event-sourced deltas (ADR-0007 ACCEPTED); entity/component model; deterministic latent-factor world-gen | LOCKED (dev) |
| 2026-07-06 | Aging: enter 16–18, retire ~28–30 (~30 ceiling, ~30 raids/career); growth = age curve toward hidden potential (dev→peak→decline), bounded by potential not flat points; conveyed via world (no >30) + retirement newspapers; cadence×growth co-tuned | LOCKED (dev) |
| 2026-07-06 | Calendar: real-calendar substrate; season = template-defined period (count/length = config, not hardcoded) with visual timeline; history retention = season-boundary compaction + permanent compact Chronicle (winners, top-100, world-firsts) | LOCKED (dev) |
| 2026-07-06 | **Principle 0: everything from a template, nothing hardcoded** — project-wide governing law (BLUEPRINT §10); API shape = `createX()` factory per entity type (createCharacter/Event/Item/Class/Ability…) | LOCKED (dev) |
| 2026-07-06 | Economy scale LOCKED: single gold currency, "thousands not millions", 10–20× wage spread (each of 20 raiders less pivotal than an 11-man XI), elite transfers 15–40k; priced model in [economy-model.md](economy-model.md) | LOCKED (dev) |
| 2026-07-06 | Slots for NOW: 1 ring + 1 trinket (was 2+2). Armor types: NOT now, but must stay easily integrateable. Both easily changed later | LOCKED (dev) |
| 2026-07-06 | Offensive healing: YES — healers deal moderate damage during no-heal phases | LOCKED (dev) |
| 2026-07-06 | No permadeath; injuries/time-off system instead (FM-modeled — see condition/injury below) | LOCKED (dev) |
| 2026-07-06 | One difficulty at launch, but a single easy scale lever so heroic dungeons / mythic raids can be added by scaling, not rebuilding | LOCKED (dev) |
| 2026-07-06 | Tick resolution leaning 0.25s (neat chunks: a cast ~8 ticks ≈ 2s, a move ~2 ticks); finalize in M0 | LEANING (dev) |
| 2026-07-06 | Damage/stat model: adopt vanilla WoW L60 formulas closely — AP/14=DPS, armor `A/(A+K)`, spell coeff `castTicks/14`, DoT `dur/60`, crit ×2 phys/×1.5 spell; magic uses the armor-style MR curve (cleaner than vanilla resists); drop hit/miss/glancing/partial-resist (already rejected). Formulas in [research/wow-damage-model-reference.md](research/wow-damage-model-reference.md); constants tuned in balance-sim phase | DIRECTION SET (assistant) — research done |
| 2026-07-06 | Raids/year: leaning 4, still open (coupled to aging) | OPEN (dev) |
| 2026-07-06 | Manager attributes: ~7 unique is fine; manager levels slowly (≈1 skill point/season) into charisma/negotiation etc. | LOCKED (dev) |
| 2026-07-06 | Raider & manager attribute lists must be add/remove-by-a-button (registry-keyed — already the entity architecture) | LOCKED (dev) |
| 2026-07-06 | Naming is placeholder throughout; single-source so one edit renames project-wide (Principle 0) — do NOT scatter literals | LOCKED (dev) |
| 2026-07-06 | One-button coverage is a hard requirement: every recurring decision has a single button (plan-week stance, equip-all, find-players); quality scales with staff ability; strong manual filter/search alongside | LOCKED (dev) |
| 2026-07-06 | Condition rework (FM two-axis): Condition (freshness, drains per slot, recovers on rest — rate = raider attr + Sleeping Quarters) + Sharpness (readiness, built by raiding, decays benched) + Morale; injuries FM-modeled (low-condition×load + proneness → knock..weeks; Infirmary/staff cut it), no permadeath | LOCKED (dev) |
| 2026-07-06 | Content tiers: farming/questing free · dungeons near-guaranteed clear (one scale lever for heroic later) · raids measured in "raid units" (~8/boss elite, ~12 avg), ease over season via gear+knowledge; one-day boss kill impossible; buy/earn/ask for threshold intel | LOCKED (dev) |
| 2026-07-06 | Boss learning: threshold-bar unlocked by *reaching* each threshold (enrage/barrier/aoe/adds/movement); per-role, per-mechanic "learning" attribute sets adapt speed (20 ≈ instant, 1 ≈ ~3 pulls); knowledge → correct ability usage | LOCKED (dev) |
| 2026-07-06 | Staff: hire from a small co-leader/manager pool; assignable to make 5-man groups, farm missing-slot loot, suggest raid comp, scout youth, talk to sponsors; ability gates auto-decision quality | LOCKED (dev) |
| 2026-07-06 | Youth program: low-info prospects you invest gold into for a future payoff; a Youth Hall venue (spend little → more/better intake) | LOCKED (dev) |
| 2026-07-06 | Guild-hall venues: stadium-style long-run gold via small multipliers (~1.05×) on dungeon/raid gold + Feast Hall guest income | LOCKED (dev) |
| 2026-07-06 | Awards: top-10 finishes → mostly gold + a Trophy Room to view achievements. PR: occasional interview questions with hidden-correct answers that move guild morale. Holidays: RPG-named days off; deny them → morale hit | LOCKED (dev) |
| 2026-07-06 | Self-contract: your salary (1/10 scale) + guild expectation (e.g. top-50); meet it → extend ~3 seasons (more money) · beat it → better offers · fail hard → fired. Easy-going feature | LOCKED (dev) |
| 2026-07-06 | Social graph on raiders (friends/foes/mentors) needed for transfer friction + inbox drama; a relationships component on the entity model | LOCKED (dev) |
| 2026-07-06 | Attribute resolution (§8a′): baseline 10; scalar attrs = ±~1%/point multiplier (Damage, Defense); behavioral attrs resolved by seeded decision/roll (potion timing, movement uptime-vs-death, defensive use, mechanics via BG-style DC-vs-attribute roll) | LOCKED (dev) |
| 2026-07-06 | Engine/stack (ADR-0008): **Godot 4 + C#**, native exe — real game feel (not web/browser), texture-based UI theming for real RPG look, Godot Resources = template architecture, pure C# sim libs (no Godot dep), LLM build+verify via Godot AI/godot-mcp. Web/Electron rejected; Unity considered and passed | LOCKED (dev) |
| 2026-07-06 | UI aesthetic: fantasy-RPG (parchment/iron/wood/gold), NOT futuristic default; anti-drift via tokens + Component Gallery (single source of visual truth) + screenshot verification | LOCKED (dev) |
| 2026-07-06 | Per-boss role caps/floors (esp. healer cap) prevent healer-stacking trivialization; composition is a bounded per-boss puzzle | LOCKED (dev) |
| 2026-07-06 | Everything still marked [PROPOSED] above | awaiting dev review |
| 2026-07-06 | **M1 build — combat depth (first-pass, tunable):** mechanic archetypes implemented = spreadDamage, tankBuster, enrage, raidDot, tankDebuff (a stacking damage-taken aura = the tankSwapDebuff idea minus the taunt-swap), interruptibleCast (raider interrupts vs boss casts), threat/tanking (enemies focus highest-threat; tanks ×4 threat hold aggro), taunt (reactive — a tank seizes ~120% of the top threat to claw aggro back when pulled; the mechanism for tank-swaps). Aura system = DoTs + stacking debuffs. The threat model is the substrate, not a DPS-throttling minigame — real tanks out-threat via ×4 + abilities, so a DPS only pulls if there's no real tank. Proactive **tank-swap AI** now implemented (first-pass): a fresh off-tank taunts when the active tank's damage-taken debuff hits 3 stacks (the ~10s taunt-window mechanic) — no-op with a single tank, so fielding two tanks is a real choice. Still to come: trait-driven *quality* (a sharp tank swaps at the ideal moment, a poor one too early/late — the attribute layer, like `ExecutionProfile` for movement). | M1 BUILD (assistant) — tunable |
| 2026-07-06 | **M1 build — difficulty (implements the "one scale lever" call above):** Normal/Heroic/Mythic scale boss HP/damage/mechanic amounts by percent (155/135, 230/175); keeps encounter id so loot resolves. Verified: a fresh 8-raider guild clears the Ashen King on Normal, wipes on Mythic. | M1 BUILD (assistant) — tunable |
| 2026-07-06 | **M1 build — injuries (first-pass of the FM condition/injury call above):** a raider who dies fights at 70% HP/damage for 2 raids, recovers on surviving a raid or a Rest action; everyone still fights (no benching) so no dead-ends. Full FM two-axis Condition/Sharpness/Morale still to come. | M1 BUILD (assistant) — tunable |
| 2026-07-06 | **M1 build — economy/progression (first-pass numbers):** raid gold 500 win / 100 wipe; loot auto-equips-if-upgrade (Power score); recruitment flat 400 gold. All placeholder for the priced economy-model.md pass. | M1 BUILD (assistant) — tunable |
| 2026-07-06 | **M1 build — classes/content (working names):** 7 classes (Guardian/Cleric/Blademaster/Pyromancer/Ranger/Warlock/Paladin), 4 bosses across tiers 1–3 (Warden/Sentinel/Ashen King/Frostwarden), 11 gear items — all data rows; rename freely. | M1 BUILD (assistant) — tunable |
| 2026-07-06 | **M2 build — spatial combat (step 2):** engine now has fixed-point 2D positions (integer-only, 1000 units = 1 yd) and a `VoidZone` ground-hazard archetype — drops a circular zone under a random raider; raiders spend an action to run out (`MoveEvent`), slow ones eat per-tick damage → reaction speed = survival. In-game a raid fights on a ranked **formation** and the **Sentinel** is the spatial showcase (drops "scorched earth"); the stage view draws the zone + the dodge. Opt-in: existing goldens byte-identical. Boss telegraph *timing/quality* (warn windows, reaction-from-attributes) still to tune. | M2 BUILD (assistant) — tunable |
| 2026-07-06 | **Removed the raider XP/Level system (off-design, dev call).** `Level`/`Xp` had crept into the M1 progression loop, but the design's raider development is **FM attributes + training + form/morale + aging**, not RPG levels. Ripped out `Level`/`Xp` from `RaiderRecord`, the resolver's `ApplyXp`, and level→stat scaling; raider combat stats now come from class base + gear (+ injury) only. The attribute model is the real progression system (still to build, per design-status). | CORRECTION (dev-directed) |
| 2026-07-06 | **World-gen — first slice built (`src/Game/World`).** The entities-and-worldgen model, to spec: composition `Raider` (identity/vocation/attributes/condition components), the deterministic latent-factor pipeline (archetypes → latents → registry-keyed attributes → derived stars), ~6,500 characters from a seed, prestige gradient + role coverage + a 16–30 age curve, all golden-hashed. Uses the **proposed 11 attributes** (GDD §8, still [OPEN] — the model is attribute-count-agnostic) and the existing 7 classes. First-pass data: attribute latent loadings, archetype distributions, star-mapping/potential curve — all tunable. Contracts, career ledger (seed+delta), and the condition/aging sim are the next world layers. | WORLD BUILD (dev-directed) — data tunable |
| 2026-07-06 | **Season race — first slice built (`SeasonRace`).** The living-world race (GDD §5): all 200 guilds progress the same raid ladder in parallel, week by week; a guild banks strength (raid-group avg stars × prestige) and downs the next boss when it has enough → a ranked global/regional leaderboard. Calibrated to the **locked pacing target** — elite clear ~week 3, rank #100 ~week 13 (~3 months). This is the cheap background-fidelity tier (entities §7), the outcome only — NOT the AI decision model (recruiting/scheduling is still undefined, design-status THIN §9). Boss difficulties + strength/tier formula are tunable. `sim season --seed N`. | WORLD BUILD (dev-directed) — data tunable |
| 2026-07-06 | **Player week loop — first slice built (`src/Game/Season`).** The LOCKED §5/§6 structure: `SeasonCalendar` (config season length, weekly reset), `Lockout` (loot once per boss per week; resets weekly), "plan my week" stances (Relax/Balanced/GrindHard → 1/2/4 raid days, GDD §6b), and `WeekRunner` — plays the guild up the raid ladder (`Encounters.All`) using the REAL combat sim + `RaidResolver`, so a raid night is the same fight the player watches. Gear farmed over weeks breaks walls (Balanced starter reaches the Frostwarden ~week 3, purely on gear — no levels). `sim play --stance grind`. NOT built (THIN/needs entity model): the 4-slot intra-day granularity, condition/stamina cost, and off-day activities (training/dungeon/rest effects). Raid-day counts tunable. | WEEK-LOOP BUILD (dev-directed) — tunable |
| 2026-07-06 | **Attributes → combat wired (§8a′, first slice).** The management→outcome link is live: `RaiderRecord` now carries an `AttributeVector` (starter guilds roll coherent ones via the world-gen model); `CombatResolution` projects it to engine `CombatAttributes` per the LOCKED §8a′ — scalars become ±%/point multipliers (output + damage-taken), and Movement/Awareness becomes the base for a seeded d20 dodge check (skill ≥ DC auto-passes). The Sentinel's void zone now has a `DodgeDc`, so Awareness decides who escapes. Engine stays registry-agnostic; neutral defaults (attr 10, DC 0) keep every golden byte-identical. **OPEN:** which FM §8 attribute drives which combat effect (first-pass: Damage←Consistency, Defense←Composure, Movement←Awareness) — §8a′'s execution-flavored list vs the §8 FM list still needs reconciling. Also still two raider models (`RaiderRecord` vs world `Raider`) — unify later. | ATTR-COMBAT BUILD (dev-directed) — mapping OPEN |
| 2026-07-06 | **Condition/stamina — first slice built (`ConditionModel`).** The LOCKED FM two-axis system (§8, economy-model §1): `RaiderRecord` gains a `Condition` (Morale/Freshness/Sharpness); raiding drains Freshness (recovered on rest, rate ∝ Endurance) and builds Sharpness (decays when benched); low condition folds into combat as a performance multiplier (fatigue → less output + worse dodge). Applied per week in the loop, so the §6 tradeoff emerges: `play --stance grind` burns the roster to 0 freshness and stalls, `--stance relax` stays fresh and reaches further. Fresh = neutral, so goldens/tests unchanged. **First-pass numbers, and grind is currently strictly dominated — needs balancing** (grind should be a viable-but-risky push, not always worse). Morale-as-events + condition-driven injury still to come. | CONDITION BUILD (dev-directed) — numbers need balancing |
| 2026-07-06 | **Weekly activities + injuries — built (dev-directed: "grind = do activities → gear + stat increases").** `WeeklyActivities` (`src/Game/Season`): **dungeons** are a gear catch-up faucet (drop lower-tier items to the neediest raider) and **training** develops the least-developed raiders' weakest attribute +1/session (capped 18; true potential-bounding needs the Vocation component). `Injuries.RollWeek`: fatigue (low freshness) + heavy raid load → seeded injury chance (1–3 raids out). Stances now map to an `ActivityPlan` (raid/dungeon/train/rest days). So "grind" now = raids + dungeons + training → real gear+stat progression, paid for in freshness + injuries. Grounded in §6 (activity list) / §8 (training→attributes, low-condition→injury). **First-pass rates need balancing** — grind is now ~break-even with relax (fatigue offsets the extra dungeon gear); should be a viable-but-risky push. Dungeon loot pool, train cap, injury rates all tunable. | ACTIVITIES BUILD (dev-directed) — rates need balancing |
| 2026-07-06 | **Real calendar + granular week planner + morale (dev-directed: "a real calendar you progress through, plan the week, assign 5-man groups / single activities; stop with grind/relax").** Dropped the stance (Relax/Balanced/GrindHard) framing entirely. New: `SeasonSchedule` (a calendar of events — raid opens, weekly resets, holidays Longnight/Ancestors' Vigil — with `Upcoming()`); `WeekSchedule`/`Assignment`/`WeekPlanner` (the granular plan — assign raid nights, **5-man dungeon groups**, **training** to days, per raider/group, with a one-button auto-fill that writes the same editable assignments); `WeekExecutor` runs a planned week and computes **per-raider** condition/injury/**morale** from each raider's own booked load (resting one while working another is a real decision). `MoraleModel` = morale as its own axis (kills/wipes/benching/holidays → gentle combat modifier). `sim play --raids N` runs a season on the calendar. Fresh/70-morale = neutral, so goldens unchanged. First-pass rates. | CALENDAR+PLANNER+MORALE BUILD (dev-directed) — rates tunable |
