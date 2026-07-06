using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>What one simulated day produced.</summary>
public sealed record DayResult(
    GuildSave Guild,
    IReadOnlyList<string> DownedThisWeek,
    int Kills,
    int GearDrops,
    int TrainingSessions,
    int Injured,
    IReadOnlyList<string> Events);

/// <summary>
/// Runs a single day of the 4-slot calendar (GDD §6): each of the day's four slots is a guild activity
/// (raid / dungeon / training / rest). A day with any raid slot runs one raid night on the ladder (under the
/// weekly lockout); dungeon slots drop catch-up gear; training slots develop the weakest; and every raider's
/// freshness/sharpness plus an injury roll come from how busy the day was. Day-granular so "simulate a day"
/// and "simulate a week" (seven days) share one path. Deterministic given the seed.
/// </summary>
public static class DayExecutor
{
    public static DayResult RunDay(
        GuildSave guild,
        IReadOnlyList<ActivityType> slots,
        IReadOnlyList<EncounterDef> ladder,
        int week,
        int day,
        IReadOnlyList<string> downedThisWeek,
        Difficulty difficulty,
        ulong seed)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(slots);

        int raidSlots = slots.Count(s => s == ActivityType.Raid);
        int dungeonSlots = slots.Count(s => s == ActivityType.Dungeon);
        int trainSlots = slots.Count(s => s == ActivityType.Training);
        int busySlots = raidSlots + dungeonSlots + trainSlots;

        var events = new List<string>();
        var lockout = new Lockout(new HashSet<string>(downedThisWeek, StringComparer.Ordinal));
        int kills = 0;

        // Raid night: any raid slot means one night on the ladder, respecting the weekly lockout.
        if (raidSlots > 0)
        {
            WeekOutcome raid = WeekRunner.RunWeek(guild, ladder, (week * 10) + day, raidDays: 1, lockout, difficulty, seed);
            guild = raid.Guild;
            lockout = raid.Lockout;
            kills = raid.Report.Nights.Count(n => n.Outcome == "Kill");
            events.AddRange(raid.Report.Events);
        }

        var roster = guild.Roster.ToList();

        int gearDrops = 0;
        IReadOnlyList<ItemDef> pool = Loot.For("dungeon");
        for (int i = 0; i < dungeonSlots && pool.Count > 0; i++)
        {
            var rng = new SeededRng(seed, stream: (ulong)(3000 + i));
            ItemDef drop = pool[rng.NextInt(pool.Count)];
            int idx = NeediestUpgrader(roster, drop);
            if (idx >= 0)
            {
                roster[idx] = Warband.EquipIfUpgrade(roster[idx], drop);
                gearDrops++;
                events.Add($"Dungeon: {roster[idx].Name} looted {drop.Name}");
            }
        }

        int trainingSessions = 0;
        for (int i = 0; i < trainSlots; i++)
        {
            foreach (int idx in TrainingPicks(roster, 4))
            {
                RaiderRecord before = roster[idx];
                RaiderRecord trained = before.TrainingTarget is { } target
                    ? WeeklyActivities.TrainToward(before, target) // the raider trains their chosen stat
                    : WeeklyActivities.Train(before);              // otherwise auto-train the weakest
                if (!ReferenceEquals(trained, before))
                {
                    roster[idx] = trained;
                    trainingSessions++;
                }
            }
        }

        int injured = 0;
        for (int i = 0; i < roster.Count; i++)
        {
            RaiderRecord r = roster[i];
            int endurance = r.Attributes?.Of("endurance") ?? 10;
            Condition condition = ConditionModel.AfterDay(r.Condition ?? ConditionModel.Fresh, busySlots, raidSlots > 0, endurance);

            int injuryLeft = r.InjuryRaidsLeft;
            if (injuryLeft == 0)
            {
                int chance = Math.Clamp((Math.Max(0, 70 - condition.Freshness) / 8) + (busySlots * 2), 0, 15);
                if (chance > 0 && new SeededRng(seed, stream: (ulong)(7000 + i)).NextInt(100) < chance)
                {
                    injuryLeft = 1 + new SeededRng(seed, stream: (ulong)(8000 + i)).NextInt(3);
                    injured++;
                    events.Add($"{r.Name} picked up an injury.");
                }
            }

            roster[i] = r with { Condition = condition, InjuryRaidsLeft = injuryLeft };
        }

        return new DayResult(
            guild with { Roster = roster }, lockout.DownedBosses.ToList(), kills, gearDrops, trainingSessions, injured, events);
    }

    private static int NeediestUpgrader(List<RaiderRecord> roster, ItemDef drop)
    {
        int best = -1;
        int lowest = int.MaxValue;
        for (int i = 0; i < roster.Count; i++)
        {
            if (ReferenceEquals(Warband.EquipIfUpgrade(roster[i], drop), roster[i]))
            {
                continue;
            }

            int power = Warband.GearPower(roster[i]);
            if (power < lowest)
            {
                lowest = power;
                best = i;
            }
        }

        return best;
    }

    // Who trains on a training slot: raiders with a chosen focus first, then the least-developed to fill.
    private static List<int> TrainingPicks(List<RaiderRecord> roster, int count)
    {
        var targeted = Enumerable.Range(0, roster.Count).Where(i => roster[i].TrainingTarget is not null).ToList();
        if (targeted.Count >= count)
        {
            return targeted.Take(count).ToList();
        }

        var fill = LeastDeveloped(roster, roster.Count).Where(i => !targeted.Contains(i)).Take(count - targeted.Count);
        return targeted.Concat(fill).ToList();
    }

    private static List<int> LeastDeveloped(List<RaiderRecord> roster, int count) => Enumerable
        .Range(0, roster.Count)
        .OrderBy(i => roster[i].Attributes is { } a ? Content.Attributes.Registry.All.Sum(x => a.Of(x.Id)) : int.MaxValue)
        .ThenBy(i => roster[i].Id, StringComparer.Ordinal)
        .Take(count)
        .ToList();
}
