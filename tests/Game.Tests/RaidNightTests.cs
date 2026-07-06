using System.Linq;
using Content;
using Engine;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The raid-night wiring the app coordinator performs (m1-build-plan step 10): a persistent guild
/// roster is projected into combatants via the class factory and fought through the real engine.
/// This tests that path headlessly — the Godot glue on top is thin.
/// </summary>
public class RaidNightTests
{
    [Fact]
    public void GeneratedRoster_ProjectsToCombatants_AndClearsTheWarden()
    {
        GuildSave guild = Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z", rosterSize: 8);

        var raid = new RaidSetup(guild.Roster.Select(Warband.ToCombatant).ToList());

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), SimConfig.Default, raid, Encounters.Warden));

        Assert.Equal(8, raid.Raiders.Count);
        Assert.Equal(EncounterOutcome.Kill, result.Outcome); // a role-covered guild beats the Warden
    }

    [Fact]
    public void FormationRaid_FightsTheSentinelSpatially_AndRunsOutOfTheVoidZone()
    {
        GuildSave guild = Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z", rosterSize: 8);

        var raid = new RaidSetup(Formation.Place(guild.Roster.Select(Warband.ToCombatant).ToList()));

        // Formation gives every raider an arena position (nobody left stacked on the origin).
        Assert.All(raid.Raiders, r => Assert.NotEqual(Position.Origin, r.SpawnPosition));

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), SimConfig.Default, raid, Encounters.Sentinel));

        // The Sentinel drops void zones (HazardEvent) and the raid runs out of them (MoveEvent).
        Assert.Contains(result.Events, e => e is HazardEvent);
        Assert.Contains(result.Events, e => e is MoveEvent);
        Assert.Equal(EncounterOutcome.Kill, result.Outcome); // dodging keeps it winnable
    }
}
