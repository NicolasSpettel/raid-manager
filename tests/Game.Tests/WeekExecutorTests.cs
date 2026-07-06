using System.Linq;
using Content;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// Executing a planned week (GDD §6): raids progress the ladder and gear the roster, and each raider's
/// condition/morale/injury come from their OWN booked load — resting one raider while working another is a
/// real per-person decision.
/// </summary>
public class WeekExecutorTests
{
    private static GuildSave Guild() => Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z");

    [Fact]
    public void ExecutingAWeek_KillsBosses_AndGearsUp()
    {
        GuildSave guild = Guild();
        int gearBefore = guild.Roster.Sum(Warband.GearPower);

        WeekSchedule schedule = WeekPlanner.Auto(guild, raidDays: 3, dungeonDays: 3, trainDays: 1);
        WeekResult result = WeekExecutor.Run(guild, schedule, Encounters.All, 1, Lockout.Empty, Difficulty.Normal, seed: 1);

        Assert.True(result.Kills > 0);
        Assert.True(result.Guild.Roster.Sum(Warband.GearPower) > gearBefore, "raids + dungeons should add gear");
    }

    [Fact]
    public void WorkedRaidersDrainFreshness_BenchedRaidersLoseSharpness()
    {
        GuildSave guild = Guild();
        string benchedId = guild.Roster[^1].Id;
        string workedId = guild.Roster[0].Id;
        var raidGroup = guild.Roster.Select(r => r.Id).Where(id => id != benchedId).ToList();

        var raidDays = new[] { Weekday.Monday, Weekday.Tuesday, Weekday.Wednesday, Weekday.Thursday, Weekday.Friday };
        var schedule = new WeekSchedule(raidDays.Select(d => new Assignment(d, ActivityType.Raid, raidGroup)).ToList());

        WeekResult result = WeekExecutor.Run(guild, schedule, Encounters.All, 1, Lockout.Empty, Difficulty.Normal, seed: 1);

        Condition worked = result.Guild.Roster.First(r => r.Id == workedId).Condition!;
        Condition benched = result.Guild.Roster.First(r => r.Id == benchedId).Condition!;

        Assert.True(worked.Freshness < benched.Freshness, "a raider working five nights should be less fresh than a rested one");
        Assert.True(benched.Sharpness < ConditionModel.Fresh.Sharpness, "the benched raider should go rusty");
    }

    [Fact]
    public void HolidayGranted_LiftsMoraleOverDenied()
    {
        GuildSave guild = Guild();
        SeasonSchedule calendar = SeasonSchedule.Build(9); // Longnight in week 3
        WeekSchedule schedule = WeekPlanner.Auto(guild, raidDays: 2, dungeonDays: 0, trainDays: 0);

        int granted = AvgMorale(WeekExecutor.Run(guild, schedule, Encounters.All, 3, Lockout.Empty, Difficulty.Normal, 1, calendar, holidayGranted: true).Guild);
        int denied = AvgMorale(WeekExecutor.Run(guild, schedule, Encounters.All, 3, Lockout.Empty, Difficulty.Normal, 1, calendar, holidayGranted: false).Guild);

        Assert.True(granted > denied, $"granted={granted} denied={denied}");
    }

    [Fact]
    public void Run_IsDeterministic()
    {
        GuildSave guild = Guild();
        WeekSchedule schedule = WeekPlanner.Auto(guild, 3, 3, 1);
        int a = WeekExecutor.Run(guild, schedule, Encounters.All, 1, Lockout.Empty, Difficulty.Normal, 1).Guild.Roster.Sum(Warband.GearPower);
        int b = WeekExecutor.Run(guild, schedule, Encounters.All, 1, Lockout.Empty, Difficulty.Normal, 1).Guild.Roster.Sum(Warband.GearPower);
        Assert.Equal(a, b);
    }

    private static int AvgMorale(GuildSave guild) => (int)guild.Roster.Average(r => (r.Condition ?? ConditionModel.Fresh).Morale);
}
