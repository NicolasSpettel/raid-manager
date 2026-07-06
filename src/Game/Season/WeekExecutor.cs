using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>The result of executing one planned week.</summary>
public sealed record WeekResult(
    GuildSave Guild,
    Lockout Lockout,
    int FurthestBossIndex,
    int Kills,
    int Wipes,
    int GearDrops,
    int TrainingSessions,
    int Injured,
    IReadOnlyList<string> Events);

/// <summary>
/// Executes a planned <see cref="WeekSchedule"/> (GDD §6): raid nights run the real combat sim, each
/// assigned 5-man group runs a dungeon (gear), assigned raiders train (attributes), and then <b>each
/// raider's</b> condition, morale and injury roll come from <i>their own</i> booked load — so resting one
/// raider while grinding another is a real, per-person decision. The weekly lockout and the season calendar
/// (holidays) are respected.
/// </summary>
public static class WeekExecutor
{
    public static WeekResult Run(
        GuildSave guild,
        WeekSchedule schedule,
        IReadOnlyList<EncounterDef> ladder,
        int week,
        Lockout lockout,
        Difficulty difficulty,
        ulong seed,
        SeasonSchedule? calendar = null,
        bool holidayGranted = true)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(schedule);

        var events = new List<string>();

        // 1) Raid nights — reuse the real raid loop for the scheduled raid days.
        WeekOutcome raids = WeekRunner.RunWeek(guild, ladder, week, schedule.RaidDays.Count, lockout, difficulty, seed);
        guild = raids.Guild;
        lockout = raids.Lockout;
        events.AddRange(raids.Report.Events);
        int kills = raids.Report.Nights.Count(n => n.Outcome == "Kill");
        int wipes = raids.Report.Nights.Count(n => n.Outcome == "Wipe");

        var roster = guild.Roster.ToList();
        var indexById = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < roster.Count; i++)
        {
            indexById[roster[i].Id] = i;
        }

        // 2) Dungeons (per assigned group) + 3) training (per assigned raider).
        IReadOnlyList<ItemDef> dungeonPool = Loot.For("dungeon");
        int gearDrops = 0;
        int trainingSessions = 0;
        int dungeonSeed = 0;
        foreach (Assignment assignment in schedule.Assignments)
        {
            if (assignment.Type == ActivityType.Dungeon && dungeonPool.Count > 0)
            {
                var rng = new SeededRng(seed, stream: (ulong)(2000 + dungeonSeed++));
                ItemDef drop = dungeonPool[rng.NextInt(dungeonPool.Count)];
                int idx = NeediestInGroup(roster, assignment.RaiderIds, drop);
                if (idx >= 0)
                {
                    roster[idx] = Warband.EquipIfUpgrade(roster[idx], drop);
                    gearDrops++;
                    events.Add($"Dungeon ({assignment.Day}): {roster[idx].Name} looted {drop.Name}");
                }
            }
            else if (assignment.Type == ActivityType.Training)
            {
                foreach (string id in assignment.RaiderIds)
                {
                    if (indexById.TryGetValue(id, out int idx))
                    {
                        RaiderRecord trained = WeeklyActivities.Train(roster[idx]);
                        if (!ReferenceEquals(trained, roster[idx]))
                        {
                            roster[idx] = trained;
                            trainingSessions++;
                        }
                    }
                }
            }
        }

        // 4) Per-raider condition, 5) morale, 6) injuries — from each raider's own load.
        bool holidayThisWeek = calendar?.HolidayIn(week) is not null;
        int injured = 0;
        for (int i = 0; i < roster.Count; i++)
        {
            RaiderRecord raider = roster[i];
            int slots = schedule.SlotsFor(raider.Id);
            int endurance = raider.Attributes?.Of("endurance") ?? 10;

            Condition condition = ConditionModel.AfterLoad(raider.Condition ?? ConditionModel.Fresh, slots, schedule.Raided(raider.Id), endurance);
            int morale = MoraleModel.AfterWeek(condition.Morale, kills, wipes, schedule.Benched(raider.Id), holidayThisWeek, holidayGranted);
            condition = condition with { Morale = morale };

            int injuryDelta = 0;
            if (raider.InjuryRaidsLeft == 0)
            {
                int chance = Math.Clamp((Math.Max(0, 70 - condition.Freshness) / 4) + slots, 0, 30);
                if (chance > 0 && new SeededRng(seed, stream: (ulong)(5000 + i)).NextInt(100) < chance)
                {
                    injuryDelta = 1 + new SeededRng(seed, stream: (ulong)(6000 + i)).NextInt(3);
                    injured++;
                    events.Add($"{raider.Name} picked up an injury — out {injuryDelta} raid(s)");
                }
            }

            roster[i] = raider with
            {
                Condition = condition,
                InjuryRaidsLeft = injuryDelta > 0 ? injuryDelta : raider.InjuryRaidsLeft,
            };
        }

        return new WeekResult(
            guild with { Roster = roster },
            lockout,
            raids.Report.FurthestBossIndex,
            kills,
            wipes,
            gearDrops,
            trainingSessions,
            injured,
            events);
    }

    private static int NeediestInGroup(List<RaiderRecord> roster, IReadOnlyList<string> groupIds, ItemDef drop)
    {
        var group = new HashSet<string>(groupIds, StringComparer.Ordinal);
        int best = -1;
        int lowestPower = int.MaxValue;
        for (int i = 0; i < roster.Count; i++)
        {
            if (!group.Contains(roster[i].Id))
            {
                continue;
            }

            if (ReferenceEquals(Warband.EquipIfUpgrade(roster[i], drop), roster[i]))
            {
                continue;
            }

            int power = Warband.GearPower(roster[i]);
            if (power < lowestPower)
            {
                lowestPower = power;
                best = i;
            }
        }

        return best;
    }
}
