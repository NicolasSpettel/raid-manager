using Game;
using Xunit;

namespace Game.Tests;

/// <summary>Recruiting spends gold and grows the roster; being broke is a clean no-op.</summary>
public class RecruitmentTests
{
    [Fact]
    public void Hiring_AddsARaider_AndSpendsGold()
    {
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z"); // starts with 1000 gold

        bool hired = Recruitment.TryHire(guild, seed: 5, out GuildSave updated);

        Assert.True(hired);
        Assert.Equal(guild.Roster.Count + 1, updated.Roster.Count);
        Assert.Equal(guild.Economy.Gold - Recruitment.Cost, updated.Economy.Gold);
    }

    [Fact]
    public void Broke_Guild_CannotHire()
    {
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z") with { Economy = new Economy(50) };

        bool hired = Recruitment.TryHire(guild, seed: 5, out GuildSave updated);

        Assert.False(hired);
        Assert.Same(guild, updated);
    }
}
