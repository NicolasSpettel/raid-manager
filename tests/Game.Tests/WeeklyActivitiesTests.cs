using System.Linq;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The off-day weekly activities (GDD §6): dungeons are a gear catch-up faucet and training is targeted
/// attribute development — the progression half of "the grind".
/// </summary>
public class WeeklyActivitiesTests
{
    private static GuildSave Guild() => Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z");

    [Fact]
    public void Dungeons_GearUpTheRoster()
    {
        GuildSave guild = Guild();
        int before = guild.Roster.Sum(Warband.GearPower);

        ActivityOutcome outcome = WeeklyActivities.Run(guild, new ActivityPlan(RaidDays: 0, DungeonDays: 3, TrainDays: 0, RestDays: 4), seed: 1);

        Assert.True(outcome.Report.GearDrops > 0);
        Assert.True(outcome.Guild.Roster.Sum(Warband.GearPower) > before, "dungeons should add gear power");
    }

    [Fact]
    public void Training_RaisesAttributes()
    {
        GuildSave guild = Guild();
        int before = TotalAttributes(guild);

        ActivityOutcome outcome = WeeklyActivities.Run(guild, new ActivityPlan(RaidDays: 0, DungeonDays: 0, TrainDays: 2, RestDays: 5), seed: 1);

        Assert.True(outcome.Report.TrainingSessions > 0);
        Assert.True(TotalAttributes(outcome.Guild) > before, "training should raise attribute totals");
    }

    [Fact]
    public void NoOffDays_NoProgression()
    {
        ActivityOutcome outcome = WeeklyActivities.Run(Guild(), new ActivityPlan(RaidDays: 4, DungeonDays: 0, TrainDays: 0, RestDays: 3), seed: 1);
        Assert.Equal(0, outcome.Report.GearDrops);
        Assert.Equal(0, outcome.Report.TrainingSessions);
    }

    [Fact]
    public void Run_IsDeterministic()
    {
        var plan = new ActivityPlan(RaidDays: 2, DungeonDays: 2, TrainDays: 1, RestDays: 2);
        int a = WeeklyActivities.Run(Guild(), plan, seed: 5).Guild.Roster.Sum(Warband.GearPower);
        int b = WeeklyActivities.Run(Guild(), plan, seed: 5).Guild.Roster.Sum(Warband.GearPower);
        Assert.Equal(a, b);
    }

    private static int TotalAttributes(GuildSave guild) => guild.Roster.Sum(r =>
        r.Attributes is null ? 0 : Content.Attributes.Registry.All.Sum(a => r.Attributes.Of(a.Id)));
}
