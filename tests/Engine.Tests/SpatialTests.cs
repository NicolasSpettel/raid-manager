using System.Linq;
using Engine;
using Xunit;

namespace Engine.Tests;

/// <summary>
/// Spatial combat (M2 step 2): void zones deal ground damage to whoever stands in them, and a raider
/// that can act runs out — so movement (and reaction speed) turns into survival. Plus the integer
/// geometry primitives that keep all of this deterministic.
/// </summary>
public class SpatialTests
{
    [Fact]
    public void ARaider_RunsOutOfAVoidZone_EmittingAMove()
    {
        SimResult result = VoidZoneFight(canAct: true);

        Assert.Contains(result.Events, e => e is MoveEvent);
    }

    [Fact]
    public void RunningOut_TakesLessHazardDamage_ThanStandingStill()
    {
        int nimble = HazardDamageTaken(canAct: true);   // runs out on its next action
        int rooted = HazardDamageTaken(canAct: false);  // never acts, so it eats every tick

        Assert.True(nimble < rooted, $"running out should reduce hazard damage: nimble={nimble} rooted={rooted}");
        Assert.True(rooted > 0, "a rooted raider should take the full ground damage");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 1)]      // floor(sqrt(3))=1
    [InlineData(4, 2)]      // floor(sqrt(4))=2
    [InlineData(10, 3)]     // floor(sqrt(10))=3
    [InlineData(1_000_000, 1000)]
    public void IntSqrt_IsTheIntegerFloor(long input, int expected) => Assert.Equal(expected, Position.IntSqrt(input));

    [Fact]
    public void WithinRadius_UsesSquaredDistance_Exactly()
    {
        var center = new Position(0, 0);
        Assert.True(new Position(3000, 4000).WithinRadius(center, 5000));  // 3-4-5 → exactly on the edge
        Assert.False(new Position(3000, 4000).WithinRadius(center, 4999)); // one unit short of reaching it
    }

    // One raider standing where the void zone drops. canAct=false gives it a swing interval past the
    // tick budget and no abilities, so it never gets an action and can never move out.
    private static SimResult VoidZoneFight(bool canAct)
    {
        var raider = new CombatantSpec(
            new CombatantId("r:x"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "X",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 1, AttackVariance: 0, SwingIntervalTicks: canAct ? 5 : 100_000),
            SpawnPosition: Position.Origin);

        var boss = new CombatantSpec(
            new CombatantId("boss:b"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "B",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        var timeline = new[]
        {
            new MechanicInstance("vz", MechanicArchetype.VoidZone, MechanicSchedule.Once(20),
                Amount: 50, Radius: 3000, DurationTicks: 60, TickIntervalTicks: 10),
        };

        return Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), new SimConfig(120), new RaidSetup(new[] { raider }),
            new EncounterDef("t", "T", new[] { boss }, null, timeline)));
    }

    private static int HazardDamageTaken(bool canAct) => VoidZoneFight(canAct).Events
        .OfType<Damage>()
        .Where(d => d.Target.Value == "r:x")
        .Sum(d => d.Amount);
}
