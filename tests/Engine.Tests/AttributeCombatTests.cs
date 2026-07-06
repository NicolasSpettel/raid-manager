using System.Linq;
using Engine;
using Xunit;

namespace Engine.Tests;

/// <summary>
/// Attribute resolution in combat (GDD §8a′): scalar attributes shift realized output/damage-taken, and the
/// Movement skill drives the seeded dodge roll. This is the "management → outcome" link — a better raider
/// measurably fights better.
/// </summary>
public class AttributeCombatTests
{
    [Fact]
    public void HigherDamageScalar_DealsMore()
    {
        int hi = TotalDamageToDummy(new CombatAttributes(DamageMultPct: 120));
        int lo = TotalDamageToDummy(new CombatAttributes(DamageMultPct: 80));
        Assert.True(hi > lo, $"hi={hi} lo={lo}");
    }

    [Fact]
    public void HigherDefenseScalar_TakesLess()
    {
        int tanky = DamageTakenFromBoss(new CombatAttributes(DefenseMultPct: 80));   // takes 80%
        int squishy = DamageTakenFromBoss(new CombatAttributes(DefenseMultPct: 120)); // takes 120%
        Assert.True(tanky < squishy, $"tanky={tanky} squishy={squishy}");
    }

    [Fact]
    public void HigherMovementSkill_DodgesTheDcVoidZone()
    {
        int nimble = HazardDamage(new CombatAttributes(MovementSkill: 20)); // ≥ DC → auto-escapes
        int clumsy = HazardDamage(new CombatAttributes(MovementSkill: 1));  // low → usually fails the roll
        Assert.True(nimble < clumsy, $"nimble={nimble} clumsy={clumsy}");
    }

    private static int TotalDamageToDummy(CombatAttributes attrs)
    {
        var raider = new CombatantSpec(
            new CombatantId("r:x"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "X",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 20, AttackVariance: 0, SwingIntervalTicks: 5), Attributes: attrs);
        var dummy = new CombatantSpec(
            new CombatantId("boss:d"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "D",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        return Fight(raider, dummy, ticks: 100).Events.OfType<Damage>().Where(d => d.Target.Value == "boss:d").Sum(d => d.Amount);
    }

    private static int DamageTakenFromBoss(CombatAttributes attrs)
    {
        var raider = new CombatantSpec(
            new CombatantId("r:x"), CombatantKind.Raider, Side.Raid, CombatantRole.Tank, "X",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0), Attributes: attrs);
        var boss = new CombatantSpec(
            new CombatantId("boss:b"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "B",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 50, AttackVariance: 0, SwingIntervalTicks: 5));

        return Fight(raider, boss, ticks: 100).Events.OfType<Damage>().Where(d => d.Target.Value == "r:x").Sum(d => d.Amount);
    }

    private static int HazardDamage(CombatAttributes attrs)
    {
        var raider = new CombatantSpec(
            new CombatantId("r:x"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "X",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 1, AttackVariance: 0, SwingIntervalTicks: 5),
            SpawnPosition: Position.Origin, Attributes: attrs);
        var boss = new CombatantSpec(
            new CombatantId("boss:b"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "B",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));
        var timeline = new[]
        {
            new MechanicInstance("vz", MechanicArchetype.VoidZone, MechanicSchedule.Once(20),
                Amount: 50, Radius: 3000, DurationTicks: 60, TickIntervalTicks: 10, DodgeDc: 15),
        };

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), new SimConfig(120), new RaidSetup(new[] { raider }),
            new EncounterDef("t", "T", new[] { boss }, null, timeline)));

        return result.Events.OfType<Damage>().Where(d => d.Target.Value == "r:x").Sum(d => d.Amount);
    }

    private static SimResult Fight(CombatantSpec raider, CombatantSpec enemy, int ticks) =>
        Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), new SimConfig(ticks), new RaidSetup(new[] { raider }),
            new EncounterDef("t", "T", new[] { enemy })));
}
