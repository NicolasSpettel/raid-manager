using System.Linq;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The season calendar + week planner (GDD §5/§6): a timeline of events to progress through, and a
/// granular schedule where you assign raid nights, 5-man dungeon groups, and training.
/// </summary>
public class CalendarTests
{
    [Fact]
    public void Season_HasRaidOpen_Holidays_AndEnd()
    {
        SeasonSchedule schedule = SeasonSchedule.Build(9);
        Assert.Contains(schedule.Events, e => e.Kind == CalendarEventKind.RaidOpen);
        Assert.Contains(schedule.Events, e => e.Kind == CalendarEventKind.Holiday);
        Assert.Contains(schedule.Events, e => e.Kind == CalendarEventKind.SeasonEnd);
    }

    [Fact]
    public void Upcoming_SkipsTheWeeklyResetNoise()
    {
        Assert.DoesNotContain(SeasonSchedule.Build(8).Upcoming(1, 20), e => e.Kind == CalendarEventKind.WeeklyReset);
    }

    [Fact]
    public void HolidayIn_FindsTheHoliday()
    {
        SeasonSchedule schedule = SeasonSchedule.Build(9); // Longnight lands week 3 (9/3)
        Assert.NotNull(schedule.HolidayIn(3));
        Assert.Null(schedule.HolidayIn(1));
    }

    [Fact]
    public void Planner_AssignsRaidNights_FiveManGroups_AndTraining()
    {
        GuildSave guild = Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z"); // 8 raiders
        WeekSchedule schedule = WeekPlanner.Auto(guild, raidDays: 2, dungeonDays: 2, trainDays: 1);

        Assert.Equal(2, schedule.RaidDays.Count);
        Assert.Contains(schedule.Assignments, a => a.Type == ActivityType.Dungeon && a.RaiderIds.Count == 5);
        Assert.Contains(schedule.Assignments, a => a.Type == ActivityType.Training);
    }

    private static readonly string[] OneRaider = { "r:1" };

    [Fact]
    public void FromDays_BuildsScheduleFromPerDayChoices()
    {
        GuildSave guild = Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z"); // 8 raiders
        var perDay = new[]
        {
            ActivityType.Raid, ActivityType.Dungeon, ActivityType.Training,
            ActivityType.Rest, ActivityType.Raid, ActivityType.Rest, ActivityType.Rest,
        };

        WeekSchedule schedule = WeekPlanner.FromDays(guild, perDay);

        Assert.Equal(2, schedule.RaidDays.Count);
        Assert.Contains(schedule.Assignments, a => a.Type == ActivityType.Dungeon && a.RaiderIds.Count == 5);
        Assert.Contains(schedule.Assignments, a => a.Type == ActivityType.Training);
        Assert.Equal(4, schedule.Assignments.Count); // rest days add nothing
    }

    [Fact]
    public void Schedule_TracksSlotsAndBench()
    {
        var schedule = new WeekSchedule(new[] { new Assignment(Weekday.Monday, ActivityType.Raid, OneRaider) });

        Assert.Equal(3, schedule.SlotsFor("r:1")); // a raid night is 3 of the day's 4 slots
        Assert.True(schedule.Raided("r:1"));
        Assert.True(schedule.Benched("r:2"));       // no assignment at all
        Assert.False(schedule.Benched("r:1"));
    }
}
