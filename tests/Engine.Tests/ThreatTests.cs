using System.Linq;
using Engine;
using Xunit;

namespace Engine.Tests;

/// <summary>Threat: an enemy focuses the highest-threat raider, and a tank (×4 threat) holds aggro over a higher-damage DPS.</summary>
public class ThreatTests
{
    [Fact]
    public void Boss_FocusesTheHighestThreatRaider()
    {
        SimResult result = Fight(
            a: Melee("r:low", damage: 2),
            b: Melee("r:high", damage: 40));

        Assert.True(BossDamageOn(result, "r:high") > BossDamageOn(result, "r:low"));
    }

    [Fact]
    public void Tank_HoldsAggro_OverAHigherDamageDps()
    {
        SimResult result = Fight(
            a: new CombatantSpec(new CombatantId("r:tank"), CombatantKind.Raider, Side.Raid, CombatantRole.Tank, "Tank",
                new StatBlock(MaxHp: 100_000, AttackDamage: 12, AttackVariance: 0, SwingIntervalTicks: 5)), // 12×4 threat
            b: Melee("r:dps", damage: 30)); // 30×1 threat — more damage, less threat

        Assert.True(BossDamageOn(result, "r:tank") > BossDamageOn(result, "r:dps"));
    }

    private static int BossDamageOn(SimResult result, string target) => result.Events
        .OfType<Damage>()
        .Where(d => d.Source.Value == "boss:b" && d.Target.Value == target)
        .Sum(d => d.Amount);

    private static CombatantSpec Melee(string id, int damage) => new(
        new CombatantId(id), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, id,
        new StatBlock(MaxHp: 100_000, AttackDamage: damage, AttackVariance: 0, SwingIntervalTicks: 5));

    private static SimResult Fight(CombatantSpec a, CombatantSpec b)
    {
        var boss = new CombatantSpec(
            new CombatantId("boss:b"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "B",
            new StatBlock(MaxHp: 100_000, AttackDamage: 10, AttackVariance: 0, SwingIntervalTicks: 8));

        return Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), new SimConfig(120), new RaidSetup(new[] { a, b }),
            new EncounterDef("t", "T", new[] { boss })));
    }
}
