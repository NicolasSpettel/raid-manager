using Engine;
using Xunit;

namespace Engine.Tests;

/// <summary>
/// Golden tests pin the exact event stream each fixture produces. A refactor must keep these
/// identical; a deliberate behavior/schema change re-blesses them (and says so in the commit). These
/// two workflows are never mixed (BLUEPRINT §10, testing-strategy §1).
/// </summary>
public class GoldenTests
{
    // Blessed from `dotnet run --project src/Sim -- run <fixture> --seed 1`.
    // If either changes unexpectedly, it is a determinism bug — do NOT blindly re-bless.
    private const string DummySeed1Hash = "ac330b5fa219abde";
    private const string TrioSeed1Hash = "64d40873d7747ca7";
    private const string CasterSeed1Hash = "46075b526d3c8ba4";
    private const string RaidSeed1Hash = "5a9672081a761158";

    // Joined with an explicit '\n' (never a source multi-line literal) so the expected value can
    // never depend on this file's on-disk line endings.
    private static readonly string[] DummySeed1Lines =
    {
        "START t=0 encounter=dummy",
        "DMG   t=5 src=r:attacker dst=boss:dummy amount=10",
        "DMG   t=10 src=r:attacker dst=boss:dummy amount=11",
        "DMG   t=15 src=r:attacker dst=boss:dummy amount=9",
        "DMG   t=20 src=r:attacker dst=boss:dummy amount=12",
        "DMG   t=25 src=r:attacker dst=boss:dummy amount=11",
        "DMG   t=30 src=r:attacker dst=boss:dummy amount=12",
        "DMG   t=35 src=r:attacker dst=boss:dummy amount=12",
        "DMG   t=40 src=r:attacker dst=boss:dummy amount=8",
        "DMG   t=45 src=r:attacker dst=boss:dummy amount=8",
        "DMG   t=50 src=r:attacker dst=boss:dummy amount=9",
        "DEATH t=50 victim=boss:dummy",
        "END   t=50 outcome=Kill",
    };

    [Fact]
    public void Dummy_Seed1_MatchesBlessedHash()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Dummy(1));
        Assert.Equal(DummySeed1Hash, result.Hash());
    }

    [Fact]
    public void Dummy_Seed1_MatchesBlessedStream()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Dummy(1));

        string expected = string.Join('\n', DummySeed1Lines) + "\n";

        Assert.Equal(expected, EventStream.Serialize(result.Events));
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
    }

    [Fact]
    public void Trio_Seed1_MatchesBlessedHash()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Trio(1));
        Assert.Equal(TrioSeed1Hash, result.Hash());
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
    }

    [Fact]
    public void Caster_Seed1_MatchesBlessedHash()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Caster(1));
        Assert.Equal(CasterSeed1Hash, result.Hash());
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
    }

    [Fact]
    public void Raid_Seed1_MatchesBlessedHash()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Raid(1));
        Assert.Equal(RaidSeed1Hash, result.Hash());
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
    }
}
