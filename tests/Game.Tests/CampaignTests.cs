using Content;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The headless campaign loop (the balance/tooling harness) grows a guild deterministically over
/// several raids — the same loop the app and `sim campaign` drive.
/// </summary>
public class CampaignTests
{
    [Fact]
    public void Campaign_GrowsTheGuild_OverRaids()
    {
        GuildSave start = Guilds.CreateStarter("Camp", 1, "2026-01-01T00:00:00Z");

        CampaignResult run = Campaign.Run(start, raids: 5, seed: 1, Encounters.Warden);

        Assert.Equal(5, run.Raids.Count);
        Assert.Equal(5, run.Guild.History.Count);
        Assert.True(run.Guild.Economy.Gold > start.Economy.Gold);
        Assert.True(run.Wins >= 1);
        Assert.Contains(run.Guild.Roster, r => r.Level >= 3);                 // levelled up
        Assert.Contains(run.Guild.Roster, r => r.Equipped is { Count: > 0 }); // geared up
    }

    [Fact]
    public void Campaign_IsDeterministic()
    {
        GuildSave start = Guilds.CreateStarter("Camp", 1, "2026-01-01T00:00:00Z");

        CampaignResult a = Campaign.Run(start, raids: 3, seed: 7, Encounters.Warden);
        CampaignResult b = Campaign.Run(start, raids: 3, seed: 7, Encounters.Warden);

        Assert.Equal(a.Guild.Economy.Gold, b.Guild.Economy.Gold);
        Assert.Equal(a.Wins, b.Wins);
    }
}
