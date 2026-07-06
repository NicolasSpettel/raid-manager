using System;
using System.Linq;

namespace Game;

/// <summary>
/// The FM two-axis condition system (GDD §8 [LOCKED], economy-model §1): <b>Condition/Freshness</b> drains
/// with raid load and recovers on rest at a rate set by Endurance (Natural-Fitness analog); <b>Sharpness</b>
/// is built by raiding and decays when benched (a rested-but-benched raider is fresh yet rusty). Low on
/// either = worse combat performance. Morale is a separate axis, moved by events (not here yet).
///
/// The <b>curves and rates below are a first pass</b> — the LOCKED part is the model (two axes, drain/
/// recover, Endurance-driven recovery, benching cost); the numbers are tuning knobs.
/// </summary>
public static class ConditionModel
{
    /// <summary>A fresh, match-ready raider (fresh guilds and new recruits start here).</summary>
    public static Condition Fresh => new(Morale: 70, Freshness: 100, Sharpness: 60);

    /// <summary>
    /// Combat performance multiplier (percent) from condition. Full freshness/sharpness = 100 (no effect);
    /// fatigue and rust each dock output. Neutral for a raider with no condition tracked (null).
    /// </summary>
    public static int PerformancePct(Condition? condition)
    {
        if (condition is null)
        {
            return 100;
        }

        int fatiguePenalty = Math.Clamp((80 - condition.Freshness) / 2, 0, 25);  // tired below ~80 freshness, cap 25%
        int rustPenalty = Math.Clamp((60 - condition.Sharpness) / 3, 0, 15);     // rusty below ~60 sharpness, cap 15%
        return 100 - fatiguePenalty - rustPenalty;
    }

    /// <summary>
    /// Advance a raider's condition over one week that used <paramref name="raidDays"/> raid days. Freshness
    /// drains with raid load and recovers by a base + Endurance; sharpness rises with raiding, decays if the
    /// raider sat out (raidDays = 0).
    /// </summary>
    public static Condition AfterWeek(Condition condition, int raidDays, int endurance)
    {
        int drain = raidDays * 12;              // raid nights are the hard drain
        int recovery = 20 + endurance;          // rest recovers; rate ∝ Endurance / Natural Fitness (§8)
        int freshness = Math.Clamp(condition.Freshness - drain + recovery, 0, 100);

        int sharpness = raidDays > 0
            ? Math.Clamp(condition.Sharpness + (raidDays * 8), 0, 100)  // playing keeps you sharp
            : Math.Clamp(condition.Sharpness - 15, 0, 100);            // benched → rusty

        return condition with { Freshness = freshness, Sharpness = sharpness };
    }

    /// <summary>
    /// Advance one raider's condition from their actual weekly load: <paramref name="slots"/> booked day-slots
    /// drain freshness (recovered by a base + Endurance), and raiding keeps sharpness up while sitting out
    /// (<paramref name="raided"/> = false) lets it rust. The per-raider version the schedule executor uses.
    /// </summary>
    public static Condition AfterLoad(Condition condition, int slots, bool raided, int endurance)
    {
        int freshness = Math.Clamp(condition.Freshness - (slots * 3) + (15 + endurance), 0, 100);
        int sharpness = raided
            ? Math.Clamp(condition.Sharpness + 12, 0, 100)
            : Math.Clamp(condition.Sharpness - 15, 0, 100);
        return condition with { Freshness = freshness, Sharpness = sharpness };
    }

    /// <summary>Advance every raider's condition after a week of <paramref name="raidDays"/> raid days (Endurance drives recovery).</summary>
    public static GuildSave AfterWeek(GuildSave guild, int raidDays)
    {
        ArgumentNullException.ThrowIfNull(guild);
        var roster = guild.Roster
            .Select(r => r with
            {
                Condition = AfterWeek(r.Condition ?? Fresh, raidDays, r.Attributes?.Of("endurance") ?? 10),
            })
            .ToList();

        return guild with { Roster = roster };
    }
}
