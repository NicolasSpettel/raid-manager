using Game;
using Xunit;

namespace Game.Tests;

/// <summary>Morale (GDD §8), its own axis: moved by results, benching, and holidays; a gentle combat modifier.</summary>
public class MoraleTests
{
    [Fact]
    public void FreshMorale_IsNeutralInCombat() => Assert.Equal(100, MoraleModel.PerformancePct(70));

    [Fact]
    public void LowMoraleHurts_HighMoraleHelps() =>
        Assert.True(MoraleModel.PerformancePct(0) < MoraleModel.PerformancePct(100));

    [Fact]
    public void Kills_Raise_Wipes_Lower()
    {
        Assert.True(MoraleModel.AfterWeek(50, kills: 4, wipes: 0, benched: false, holidayThisWeek: false, holidayGranted: false) > 50);
        Assert.True(MoraleModel.AfterWeek(50, kills: 0, wipes: 4, benched: false, holidayThisWeek: false, holidayGranted: false) < 50);
    }

    [Fact]
    public void BenchedRaider_Sulks() =>
        Assert.True(MoraleModel.AfterWeek(50, 0, 0, benched: true, holidayThisWeek: false, holidayGranted: false) < 50);

    [Fact]
    public void Holiday_GrantedBeatsDenied() =>
        Assert.True(
            MoraleModel.AfterWeek(50, 0, 0, false, holidayThisWeek: true, holidayGranted: true) >
            MoraleModel.AfterWeek(50, 0, 0, false, holidayThisWeek: true, holidayGranted: false));
}
