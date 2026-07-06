using System.Linq;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The season race (GDD §5): the whole world progresses the same raid in parallel. Guards determinism,
/// leaderboard ordering, and the locked pacing target — elite guilds clear in ~3 weeks, mid-pack in ~3
/// months — so a tuning change that breaks the race's shape fails loudly.
/// </summary>
public class SeasonRaceTests
{
    private static SeasonResult Race() => SeasonRace.Run(WorldGen.Generate(1), SeasonRaid.Default, weeks: 16);

    [Fact]
    public void Race_IsDeterministic()
    {
        World world = WorldGen.Generate(3);
        SeasonResult a = SeasonRace.Run(world);
        SeasonResult b = SeasonRace.Run(world);
        Assert.Equal(a.Standings.Select(g => g.Guild.Value), b.Standings.Select(g => g.Guild.Value));
    }

    [Fact]
    public void Standings_AreRankedByProgress()
    {
        var standings = Race().Standings;
        for (int i = 1; i < standings.Count; i++)
        {
            Assert.True(standings[i - 1].BossesDown >= standings[i].BossesDown, "standings must be non-increasing in bosses down");
        }
    }

    [Fact]
    public void Pacing_EliteClearFast_MidPackLags()
    {
        var standings = Race().Standings;

        int firstClear = standings.First(g => g.ClearedWeek is not null).ClearedWeek!.Value;
        Assert.InRange(firstClear, 1, 4); // elite clear in ~3 weeks (locked target)

        int hundredth = standings[99].ClearedWeek ?? 99;
        Assert.True(hundredth >= 8, $"rank #100 should lag well behind the elite, got week {hundredth}");
    }

    [Fact]
    public void TheWinner_ClearedTheWholeRaid()
    {
        GuildProgress top = Race().Standings[0];
        Assert.Equal(SeasonRaid.Default.Bosses.Count, top.BossesDown);
        Assert.NotNull(top.ClearedWeek);
    }

    [Fact]
    public void StrongerGuilds_ClearNoLaterThanWeakerOnes()
    {
        World world = WorldGen.Generate(1);
        var standings = SeasonRace.Run(world).Standings;

        GuildProgress strongest = standings.MaxBy(g => g.Strength)!;
        GuildProgress weakest = standings.MinBy(g => g.Strength)!;
        int strongWeek = strongest.ClearedWeek ?? int.MaxValue;
        int weakWeek = weakest.ClearedWeek ?? int.MaxValue;
        Assert.True(strongWeek <= weakWeek, "a stronger roster should not clear later than a weaker one");
    }

    [Fact]
    public void RegionalLeaderboard_FiltersToOneRegion()
    {
        SeasonResult result = Race();
        string region = result.Standings[0].Region;
        Assert.NotEmpty(result.Regional(region));
        Assert.All(result.Regional(region), g => Assert.Equal(region, g.Region));
    }
}
