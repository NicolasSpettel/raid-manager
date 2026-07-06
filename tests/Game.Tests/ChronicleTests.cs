using System.Linq;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The season Chronicle (GDD §5): the compact permanent record folded from a season race — champion,
/// world-firsts, and the top-N leaderboard.
/// </summary>
public class ChronicleTests
{
    private static SeasonChronicle Record() =>
        Chronicle.Record(season: 1, SeasonRace.Run(WorldGen.Generate(1), SeasonRaid.Default, weeks: 16));

    [Fact]
    public void Champion_IsTheLeaderWhoCleared()
    {
        SeasonResult result = SeasonRace.Run(WorldGen.Generate(1), SeasonRaid.Default, weeks: 16);
        SeasonChronicle chronicle = Chronicle.Record(1, result);

        Assert.Equal(result.Standings[0].Name, chronicle.Champion); // the overall leader, who did clear
        Assert.NotNull(chronicle.Champion);
    }

    [Fact]
    public void WorldFirsts_CoverEveryBoss_AndAreTheEarliestKill()
    {
        SeasonResult result = SeasonRace.Run(WorldGen.Generate(1), SeasonRaid.Default, weeks: 16);
        SeasonChronicle chronicle = Chronicle.Record(1, result);

        Assert.Equal(result.Raid.Bosses.Count, chronicle.WorldFirsts.Count);

        // each world-first is genuinely the earliest week that boss went down anywhere in the world
        for (int i = 0; i < result.Raid.Bosses.Count; i++)
        {
            int earliest = result.Standings.Where(g => g.BossKillWeeks.Count > i).Min(g => g.BossKillWeeks[i]);
            Assert.Equal(earliest, chronicle.WorldFirsts[i].Week);
        }
    }

    [Fact]
    public void WorldFirstProgression_IsMonotonic()
    {
        // a later boss can't be world-first-killed before an earlier one
        var weeks = Record().WorldFirsts.Select(w => w.Week).ToList();
        for (int i = 1; i < weeks.Count; i++)
        {
            Assert.True(weeks[i] >= weeks[i - 1], "a harder boss should not fall before an easier one");
        }
    }

    [Fact]
    public void Top_IsRankedAndCapped()
    {
        SeasonChronicle chronicle = Record();
        Assert.Equal(100, chronicle.Top.Count);
        Assert.Equal(1, chronicle.Top[0].Rank);
        Assert.Equal(100, chronicle.Top[^1].Rank);
    }
}
