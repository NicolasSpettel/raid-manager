using Engine;
using Xunit;

namespace Engine.Tests;

/// <summary>
/// Golden tests pin the exact event stream the dummy fight produces. A refactor must keep these
/// identical; a deliberate behavior/balance change re-blesses them (and says so in the commit).
/// These two workflows are never mixed (BLUEPRINT §10, testing-strategy §1).
/// </summary>
public class GoldenTests
{
    // Blessed from `dotnet run --project src/Sim -- run dummy --seed 1`.
    // If this ever changes unexpectedly, it is a determinism bug — do NOT blindly re-bless.
    private const string DummySeed1Hash = "bcdf32113982c369";

    // Joined with an explicit '\n' (never a source multi-line literal) so the expected value can
    // never depend on this file's on-disk line endings.
    private static readonly string[] DummySeed1Lines =
    {
        "START t=0 encounter=dummy",
        "DMG   t=5 src=1 dst=2 amount=10",
        "DMG   t=10 src=1 dst=2 amount=11",
        "DMG   t=15 src=1 dst=2 amount=9",
        "DMG   t=20 src=1 dst=2 amount=12",
        "DMG   t=25 src=1 dst=2 amount=11",
        "DMG   t=30 src=1 dst=2 amount=12",
        "DMG   t=35 src=1 dst=2 amount=12",
        "DMG   t=40 src=1 dst=2 amount=8",
        "DMG   t=45 src=1 dst=2 amount=8",
        "DMG   t=50 src=1 dst=2 amount=9",
        "DEATH t=50 victim=2",
        "END   t=50 outcome=Kill",
    };

    [Fact]
    public void DummyFight_Seed1_MatchesBlessedHash()
    {
        SimResult result = DummyFight.Run(1);
        Assert.Equal(DummySeed1Hash, result.Hash());
    }

    [Fact]
    public void DummyFight_Seed1_MatchesBlessedStream()
    {
        SimResult result = DummyFight.Run(1);

        string expected = string.Join('\n', DummySeed1Lines) + "\n";

        Assert.Equal(expected, EventStream.Serialize(result.Events));
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
    }
}
