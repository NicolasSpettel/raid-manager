using System.Linq;
using Content;
using Engine;
using Game;
using Xunit;
using Xunit.Abstractions;

namespace Game.Tests;

/// <summary>
/// Cross-boss progression: a guild should be able to farm approachable content, grow (levels + gear),
/// and use that power to break into a wall it could not touch fresh. This is the diagnostic that tells
/// "hard endgame" apart from "dead, unwinnable content".
/// </summary>
public class ProgressionTests
{
    private readonly ITestOutputHelper _out;

    public ProgressionTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void AGuildThatFarmsTheAshenKing_CanThenBreakIntoTheFrostwarden()
    {
        // Farm the Ashen King on Normal — a fresh guild already clears it, so it gears and levels up.
        CampaignResult farmed = Campaign.Run(
            Guilds.CreateStarter("Progression", 7, "2026-01-01T00:00:00Z", rosterSize: 8),
            raids: 25, seed: 7, encounter: Encounters.AshenKing);

        GuildSave guild = farmed.Guild;
        int maxLevel = guild.Roster.Max(r => r.Level);
        int maxGear = guild.Roster.Max(Warband.GearPower);
        _out.WriteLine($"after 25 Ashen King raids: {farmed.Wins} wins, top level {maxLevel}, top gear {maxGear}");

        // Now bring that progressed guild to the Frostwarden (Normal) — the fresh-guild wall (0/6).
        EncounterOutcome best = EncounterOutcome.Wipe;
        for (ulong seed = 1; seed <= 6; seed++)
        {
            var raid = new RaidSetup(Formation.Place(guild.Roster.Select(Warband.ToCombatant).ToList()));
            EncounterOutcome outcome = Simulator.SimulateEncounter(new SimInput(
                new SeededRng(seed), SimConfig.Default, raid, Encounters.Frostwarden)).Outcome;
            _out.WriteLine($"  Frostwarden Normal, seed {seed}: {outcome}");
            if (outcome == EncounterOutcome.Kill)
            {
                best = EncounterOutcome.Kill;
            }
        }

        Assert.Equal(EncounterOutcome.Kill, best); // a fully-progressed guild must be able to clear it
    }
}
