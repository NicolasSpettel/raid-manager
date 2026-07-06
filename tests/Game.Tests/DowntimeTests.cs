using System.Linq;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>Resting heals injuries a step — the recovery path when the raid is too banged up to push.</summary>
public class DowntimeTests
{
    [Fact]
    public void Rest_HealsInjuriesAStep()
    {
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z");
        var roster = guild.Roster.ToList();
        roster[0] = roster[0] with { InjuryRaidsLeft = 2 };
        roster[1] = roster[1] with { InjuryRaidsLeft = 1 };
        guild = guild with { Roster = roster };

        GuildSave rested = Downtime.Rest(guild);

        Assert.Equal(1, rested.Roster.First(r => r.Id == roster[0].Id).InjuryRaidsLeft);
        Assert.Equal(0, rested.Roster.First(r => r.Id == roster[1].Id).InjuryRaidsLeft);
    }

    [Fact]
    public void Rest_LeavesHealthyRaidersAlone()
    {
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z");

        GuildSave rested = Downtime.Rest(guild);

        Assert.All(rested.Roster, r => Assert.Equal(0, r.InjuryRaidsLeft));
    }
}
