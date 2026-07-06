using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>One raid night's result on the player's ladder.</summary>
public sealed record RaidNight(int Day, string BossId, string BossName, string Outcome, string? Loot, IReadOnlyList<string> Fallen);

/// <summary>What happened across one lockout week — the raid nights, how far the guild got, and inbox-style events.</summary>
public sealed record WeekReport(int Week, int RaidDays, IReadOnlyList<RaidNight> Nights, int FurthestBossIndex, IReadOnlyList<string> Events);

/// <summary>A finished week: the updated guild, the (now-populated) lockout, and the report.</summary>
public sealed record WeekOutcome(GuildSave Guild, Lockout Lockout, WeekReport Report);

/// <summary>
/// The player's weekly raid loop (GDD §5/§6). Within a lockout week the guild raids on <c>raidDays</c>
/// nights, clearing up its raid ladder; each boss is downable once per week (loot once per boss per week —
/// the lockout), so extra raid days are extra attempts at the progression wall. Reuses the real combat sim
/// + `RaidResolver` (gold/loot/injuries), so a raid night here is the same fight the player watches. The
/// 4-slot intra-day model and the condition/stamina cost of raiding hard are the next layer (need the
/// entity/condition system) — not modelled here yet.
/// </summary>
public static class WeekRunner
{
    public static WeekOutcome RunWeek(
        GuildSave guild,
        IReadOnlyList<EncounterDef> ladder,
        int week,
        int raidDays,
        Lockout lockout,
        Difficulty difficulty,
        ulong seed)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(ladder);

        var names = guild.Roster.ToDictionary(r => r.Id, r => r.Name, StringComparer.Ordinal);
        var nights = new List<RaidNight>();
        var events = new List<string>();
        int furthest = -1;

        for (int day = 1; day <= raidDays; day++)
        {
            for (int b = 0; b < ladder.Count; b++)
            {
                if (lockout.IsDowned(ladder[b].Id))
                {
                    continue; // already cleared this lockout — move to the next boss
                }

                EncounterDef boss = Difficulties.Scale(ladder[b], difficulty);
                ulong raidSeed = seed + (ulong)((week * 1000) + (day * 100) + b);
                var raid = new RaidSetup(Formation.Place(guild.Roster.Select(Warband.ToCombatant).ToList()));
                SimResult result = Simulator.SimulateEncounter(new SimInput(new SeededRng(raidSeed), SimConfig.Default, raid, boss));

                (GuildSave updated, RaidSummary summary) = RaidResolver.Resolve(guild, result, boss, raidSeed);
                guild = updated;

                var fallen = summary.Contributions
                    .Where(c => c.Died)
                    .Select(c => names.GetValueOrDefault(c.RaiderId, c.RaiderId))
                    .ToList();

                if (result.Outcome == EncounterOutcome.Kill)
                {
                    lockout = lockout.WithDowned(ladder[b].Id);
                    furthest = Math.Max(furthest, b);
                    nights.Add(new RaidNight(day, ladder[b].Id, ladder[b].Name, "Kill", summary.LootDropped, fallen));
                    events.Add($"Week {week}, night {day}: downed {ladder[b].Name}"
                        + (summary.LootDropped is { } loot ? $" — looted {loot}" : string.Empty));
                }
                else
                {
                    nights.Add(new RaidNight(day, ladder[b].Id, ladder[b].Name, "Wipe", null, fallen));
                    events.Add($"Week {week}, night {day}: wiped on {ladder[b].Name}");
                    break; // hit the wall — the night ends here
                }
            }

            if (lockout.DownedBosses.Count >= ladder.Count)
            {
                break; // whole raid cleared this week; remaining nights would just be farm
            }
        }

        return new WeekOutcome(guild, lockout, new WeekReport(week, raidDays, nights, furthest, events));
    }
}
