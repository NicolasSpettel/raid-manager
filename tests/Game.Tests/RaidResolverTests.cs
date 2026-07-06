using System.Linq;
using Content;
using Engine;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The progression loop: a finished raid folds into gold, XP/levels, and a history entry — the "career
/// history is a fold" mechanism (save-format.md). The resolver is pure — the input guild is untouched.
/// </summary>
public class RaidResolverTests
{
    [Fact]
    public void WinningARaid_AwardsGold_LevelsRaiders_AndRecordsHistory()
    {
        GuildSave guild = Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z", rosterSize: 8);
        SimResult result = Fight(guild, seed: 1);
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);

        (GuildSave updated, RaidSummary summary) = RaidResolver.Resolve(guild, result, Encounters.Warden);

        Assert.True(updated.Economy.Gold > guild.Economy.Gold);
        Assert.Single(updated.History);
        Assert.Equal("Kill", summary.Outcome);
        Assert.Contains(updated.Roster, r => r.Level > 1);      // a win levels raiders up
        Assert.Empty(guild.History);                            // input guild not mutated
    }

    [Fact]
    public void Contributions_AreFoldedPerRaider()
    {
        GuildSave guild = Guilds.CreateStarter("Test", 2, "2026-01-01T00:00:00Z", rosterSize: 8);
        SimResult result = Fight(guild, seed: 1);

        (_, RaidSummary summary) = RaidResolver.Resolve(guild, result, Encounters.Warden);

        Assert.Equal(guild.Roster.Count, summary.Contributions.Count);
        Assert.Contains(summary.Contributions, c => c.DamageDone > 0);  // DPS dealt damage
        Assert.Contains(summary.Contributions, c => c.HealingDone > 0); // the healer healed
    }

    private static SimResult Fight(GuildSave guild, ulong seed)
    {
        var raid = new RaidSetup(guild.Roster
            .Select(r => Roster.CreateRaider(Classes.Registry.Get(r.ClassId), r.Id, r.Name))
            .ToList());
        return Simulator.SimulateEncounter(new SimInput(
            new SeededRng(seed), SimConfig.Default, raid, Encounters.Warden));
    }
}
