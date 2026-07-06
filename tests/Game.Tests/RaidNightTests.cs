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
}
