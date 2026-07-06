using System.Collections.Generic;
using System.Linq;
using Content;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The day-granular calendar loop (GDD §6): a day's four slots are guild activities. A raid slot runs a raid
/// night (under the weekly lockout); training develops attributes; busy days drain freshness while rest
/// recovers it.
/// </summary>
public class DayExecutorTests
{
    private static readonly ActivityType[] RaidDay = { ActivityType.Raid, ActivityType.Raid, ActivityType.Raid, ActivityType.Rest };
    private static readonly ActivityType[] TrainDay = { ActivityType.Training, ActivityType.Training, ActivityType.Rest, ActivityType.Rest };
    private static readonly ActivityType[] RestDay = { ActivityType.Rest, ActivityType.Rest, ActivityType.Rest, ActivityType.Rest };

    private static GuildSave Guild() => Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z");

    private static DayResult Run(GuildSave guild, ActivityType[] slots, int day, IReadOnlyList<string> downed) =>
        DayExecutor.RunDay(guild, slots, Encounters.All, week: 1, day: day, downed, Difficulty.Normal, seed: 1);

    [Fact]
    public void RaidDay_FightsBosses_AndLocksThem()
    {
        DayResult result = Run(Guild(), RaidDay, 0, new List<string>());
        Assert.True(result.Kills > 0);
        Assert.NotEmpty(result.DownedThisWeek);
    }

    [Fact]
    public void TrainingDay_DevelopsAttributes()
    {
        GuildSave guild = Guild();
        int before = TotalAttributes(guild);

        DayResult result = Run(guild, TrainDay, 0, new List<string>());

        Assert.True(result.TrainingSessions > 0);
        Assert.True(TotalAttributes(result.Guild) > before);
    }

    [Fact]
    public void BusyDayDrainsFreshness_RestDayKeepsItUp()
    {
        GuildSave guild = Guild();
        int afterRaid = MinFreshness(Run(guild, RaidDay, 0, new List<string>()).Guild);
        int afterRest = MinFreshness(Run(guild, RestDay, 0, new List<string>()).Guild);
        Assert.True(afterRaid < afterRest, $"raid={afterRaid} rest={afterRest}");
    }

    [Fact]
    public void Lockout_CarriesAcrossDaysWithinAWeek()
    {
        GuildSave guild = Guild();
        DayResult day1 = Run(guild, RaidDay, 0, new List<string>());
        DayResult day2 = DayExecutor.RunDay(day1.Guild, RaidDay, Encounters.All, 1, 1, day1.DownedThisWeek, Difficulty.Normal, 2);

        Assert.True(day2.DownedThisWeek.Count >= day1.DownedThisWeek.Count); // the week's lockout only grows
    }

    private static int TotalAttributes(GuildSave guild) => guild.Roster.Sum(r =>
        r.Attributes is null ? 0 : Content.Attributes.Registry.All.Sum(a => r.Attributes.Of(a.Id)));

    private static int MinFreshness(GuildSave guild) => guild.Roster.Min(r => (r.Condition ?? ConditionModel.Fresh).Freshness);
}
