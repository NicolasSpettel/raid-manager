using System.Collections.Generic;
using System.Linq;
using Engine;
using Xunit;

namespace Engine.Tests;

/// <summary>
/// Interruptible boss casts: a raider with a ready interrupt stops the cast (no raid damage); with no
/// interrupt available the cast lands (raid-wide hit) — the interrupt-rotation pressure (engine-spec §8).
/// </summary>
public class InterruptTests
{
    [Fact]
    public void InterruptibleCast_IsStopped_WhenARaiderCanInterrupt()
    {
        var kick = new AbilityDef(new AbilityId("i.kick"), CastTicks: 0, GcdTicks: 15, CooldownTicks: 100, Priority: 0, new InterruptEffect());
        var raider = new CombatantSpec(
            new CombatantId("r:1"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "R",
            new StatBlock(MaxHp: 1000, AttackDamage: 5, AttackVariance: 0, SwingIntervalTicks: 5),
            new[] { kick });

        SimResult result = RunCast(raider, castAmount: 500, atTick: 10);

        Assert.Contains(result.Events, e => e is MechanicEvent m && m.Note == "interrupted");
        Assert.DoesNotContain(result.Events, e => e is Damage d && d.Ability?.Value == "m.cast");
    }

    [Fact]
    public void InterruptibleCast_Lands_WhenNoInterruptIsReady()
    {
        var raider = new CombatantSpec(
            new CombatantId("r:1"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "R",
            new StatBlock(MaxHp: 1000, AttackDamage: 5, AttackVariance: 0, SwingIntervalTicks: 5)); // no interrupt

        SimResult result = RunCast(raider, castAmount: 50, atTick: 10);

        Assert.Contains(result.Events, e => e is MechanicEvent m && m.Note == "landed");
        Assert.Contains(result.Events, e => e is Damage d && d.Ability?.Value == "m.cast");
    }

    private static SimResult RunCast(CombatantSpec raider, int castAmount, int atTick)
    {
        var boss = new CombatantSpec(
            new CombatantId("boss:c"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "C",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));
        var timeline = new[]
        {
            new MechanicInstance("m.cast", MechanicArchetype.InterruptibleCast, MechanicSchedule.Once(atTick), castAmount),
        };

        return Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), new SimConfig(40), new RaidSetup(new[] { raider }),
            new EncounterDef("t", "T", new[] { boss }, Phases: null, Timeline: timeline)));
    }
}
