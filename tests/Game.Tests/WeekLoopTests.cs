using System.Linq;
using Content;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The player's weekly raid loop under lockout (GDD §5/§6): each boss is downable once per week (loot once
/// per boss per week), extra raid days are extra attempts at the wall, and gear farmed over weeks breaks
/// walls. Guards the lockout rule, stance effect, progression, and determinism.
/// </summary>
public class WeekLoopTests
{
    private static GuildSave StarterGuild() => Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z");

    [Fact]
    public void Lockout_TracksDownedBosses_AndResetsEachWeek()
    {
        Lockout week1 = Lockout.Empty.WithDowned("warden").WithDowned("sentinel");
        Assert.True(week1.IsDowned("warden"));
        Assert.False(week1.IsDowned("frostwarden"));
        Assert.False(Lockout.Empty.IsDowned("warden")); // a new week is a clean reset
    }

    [Fact]
    public void EachBoss_IsLootedAtMostOncePerWeek()
    {
        WeekOutcome week = WeekRunner.RunWeek(
            StarterGuild(), Encounters.All, week: 1, raidDays: 4, Lockout.Empty, Difficulty.Normal, seed: 1);

        var killsPerBoss = week.Report.Nights
            .Where(n => n.Outcome == "Kill")
            .GroupBy(n => n.BossId);

        Assert.All(killsPerBoss, g => Assert.Single(g)); // the lockout: one kill (loot) per boss per week
    }

    [Fact]
    public void MoreRaidDays_ReachAtLeastAsFar()
    {
        GuildSave guild = StarterGuild();
        int relax = WeekRunner.RunWeek(guild, Encounters.All, 1, WeekPlan.RaidDays(WeekStance.Relax), Lockout.Empty, Difficulty.Normal, 1).Report.FurthestBossIndex;
        int grind = WeekRunner.RunWeek(guild, Encounters.All, 1, WeekPlan.RaidDays(WeekStance.GrindHard), Lockout.Empty, Difficulty.Normal, 1).Report.FurthestBossIndex;

        Assert.True(grind >= relax, $"more raid days should not reduce progress: grind={grind} relax={relax}");
    }

    [Fact]
    public void FarmingGear_OverWeeks_BreaksAWall()
    {
        // A fresh guild pushed straight into the whole ladder gears up week over week and progresses.
        GuildSave guild = StarterGuild();
        Lockout lockout = Lockout.Empty;
        int furthest = -1;
        for (int week = 1; week <= 6; week++)
        {
            WeekOutcome outcome = WeekRunner.RunWeek(guild, Encounters.All, week, 2, Lockout.Empty, Difficulty.Normal, seed: 1);
            guild = outcome.Guild;
            furthest = System.Math.Max(furthest, outcome.Report.FurthestBossIndex);
        }

        Assert.Equal(Encounters.All.Count - 1, furthest); // reaches the final boss over the season
    }

    [Fact]
    public void RunWeek_IsDeterministic()
    {
        GuildSave guild = StarterGuild();
        WeekReport a = WeekRunner.RunWeek(guild, Encounters.All, 1, 2, Lockout.Empty, Difficulty.Normal, 1).Report;
        WeekReport b = WeekRunner.RunWeek(guild, Encounters.All, 1, 2, Lockout.Empty, Difficulty.Normal, 1).Report;

        Assert.Equal(a.FurthestBossIndex, b.FurthestBossIndex);
        Assert.Equal(a.Nights.Select(n => n.Outcome), b.Nights.Select(n => n.Outcome));
    }

    [Fact]
    public void Calendar_AdvancesAndEnds()
    {
        SeasonCalendar calendar = SeasonCalendar.Start(3);
        Assert.Equal(1, calendar.CurrentWeek);
        Assert.False(calendar.SeasonOver);

        calendar = calendar.Advance().Advance().Advance();
        Assert.True(calendar.SeasonOver); // week 4 > length 3
    }
}
