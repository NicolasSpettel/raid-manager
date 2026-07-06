using Content;
using Engine;
using Xunit;

namespace Content.Tests;

/// <summary>
/// Proves the whole point of the content pipeline: an ability authored as a data row drives the
/// engine's cast system with zero engine edits — the concrete meaning of Principle 0.
/// </summary>
public class AbilityIntegrationTests
{
    [Fact]
    public void AuthoredAbility_DrivesTheEngine()
    {
        AbilityDef fireball = Abilities.Registry.Def("pyromancer.fireball");

        var mage = new CombatantSpec(
            new CombatantId("r:mage"), CombatantKind.Raider, Side.Raid, CombatantRole.Ranged, "Mage",
            new StatBlock(MaxHp: 500, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
                MaxResource: 1000, ResourceRegenPerTick: 5),
            new[] { fireball });

        var dummy = new CombatantSpec(
            new CombatantId("boss:dummy"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "Dummy",
            new StatBlock(MaxHp: 200, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1),
            SimConfig.Default,
            new RaidSetup(new[] { mage }),
            new EncounterDef("t", "T", new[] { dummy })));

        Assert.Contains(result.Events, e => e is CastStart cs && cs.Ability.Value == "pyromancer.fireball");
        Assert.Contains(result.Events, e => e is Damage d && d.Ability?.Value == "pyromancer.fireball");
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
    }

    [Fact]
    public void AuthoredHeal_DrivesTheEngine()
    {
        AbilityDef mend = Abilities.Registry.Def("cleric.mend");

        var tank = new CombatantSpec(
            new CombatantId("r:tank"), CombatantKind.Raider, Side.Raid, CombatantRole.Tank, "Tank",
            new StatBlock(MaxHp: 500, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        var healer = new CombatantSpec(
            new CombatantId("r:healer"), CombatantKind.Raider, Side.Raid, CombatantRole.Healer, "Healer",
            new StatBlock(MaxHp: 400, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
                MaxResource: 1000, ResourceRegenPerTick: 5),
            new[] { mend });

        var boss = new CombatantSpec(
            new CombatantId("boss:main"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "Boss",
            new StatBlock(MaxHp: 100_000, AttackDamage: 20, AttackVariance: 0, SwingIntervalTicks: 5));

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1),
            new SimConfig(200),
            new RaidSetup(new[] { tank, healer }),
            new EncounterDef("t", "T", new[] { boss })));

        Assert.Contains(result.Events, e => e is Heal h && h.Ability.Value == "cleric.mend" && h.Amount > 0);
    }
}
