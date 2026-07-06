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
        AbilityDef fireball = Abilities.Registry.Def("mage.fireball");

        var mage = new CombatantSpec(
            new CombatantId("r:mage"), CombatantKind.Raider, Side.Raid, CombatantRole.Ranged, "Mage",
            new StatBlock(MaxHp: 500, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0),
            new[] { fireball });

        var dummy = new CombatantSpec(
            new CombatantId("boss:dummy"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "Dummy",
            new StatBlock(MaxHp: 200, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1),
            SimConfig.Default,
            new RaidSetup(new[] { mage }),
            new EncounterDef("t", "T", new[] { dummy })));

        Assert.Contains(result.Events, e => e is CastStart cs && cs.Ability.Value == "mage.fireball");
        Assert.Contains(result.Events, e => e is Damage d && d.Ability?.Value == "mage.fireball");
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
    }
}
