using Content;
using Engine;
using Xunit;

namespace Content.Tests;

/// <summary>Difficulty scales boss stats up while keeping the encounter id (so loot still resolves).</summary>
public class DifficultyTests
{
    [Fact]
    public void Normal_ReturnsTheEncounterUnchanged()
    {
        Assert.Same(Encounters.Warden, Difficulties.Scale(Encounters.Warden, Difficulty.Normal));
    }

    [Fact]
    public void HigherDifficulty_ScalesBossStatsUp()
    {
        int normalHp = Encounters.Warden.Enemies[0].Stats.MaxHp;
        int heroicHp = Difficulties.Scale(Encounters.Warden, Difficulty.Heroic).Enemies[0].Stats.MaxHp;
        int mythicHp = Difficulties.Scale(Encounters.Warden, Difficulty.Mythic).Enemies[0].Stats.MaxHp;

        Assert.True(heroicHp > normalHp, "heroic should raise boss HP");
        Assert.True(mythicHp > heroicHp, "mythic should raise it further");

        int mythicDmg = Difficulties.Scale(Encounters.Warden, Difficulty.Mythic).Enemies[0].Stats.AttackDamage;
        Assert.True(mythicDmg > Encounters.Warden.Enemies[0].Stats.AttackDamage, "mythic should raise boss damage");
    }

    [Fact]
    public void Scaling_KeepsTheEncounterId_SoLootStillResolves()
    {
        EncounterDef mythic = Difficulties.Scale(Encounters.Warden, Difficulty.Mythic);

        Assert.Equal(Encounters.Warden.Id, mythic.Id);
        Assert.NotEmpty(Loot.For(mythic.Id));
    }
}
