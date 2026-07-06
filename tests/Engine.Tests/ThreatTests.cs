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

    [Fact]
    public void ATank_WithATaunt_PullsAggroBack_FromAHigherThreatDps()
    {
        // Same low-threat tank vs a runaway DPS — a taunt should claw the boss back onto the tank.
        int withTaunt = TankBossShare(giveTaunt: true);
        int withoutTaunt = TankBossShare(giveTaunt: false);

        Assert.True(withTaunt > withoutTaunt,
            $"taunt should raise the tank's damage share: with={withTaunt} without={withoutTaunt}");
    }

    [Fact]
    public void TwoTanks_SwapOnDebuffStacks_SoTheBossHitsBoth()
    {
        AbilityDef taunt = new(new AbilityId("t.taunt"), CastTicks: 0, GcdTicks: 15, CooldownTicks: 20,
            Priority: 0, new TauntEffect());

        CombatantSpec Tank(string id) => new(
            new CombatantId(id), CombatantKind.Raider, Side.Raid, CombatantRole.Tank, id,
            new StatBlock(MaxHp: 200_000, AttackDamage: 8, AttackVariance: 0, SwingIntervalTicks: 8), new[] { taunt });

        var boss = new CombatantSpec(
            new CombatantId("boss:b"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "B",
            new StatBlock(MaxHp: 500_000, AttackDamage: 10, AttackVariance: 0, SwingIntervalTicks: 6));

        var timeline = new[]
        {
            new MechanicInstance("dbg.debuff", MechanicArchetype.TankDebuff, MechanicSchedule.Repeating(10, 12), Amount: 20),
        };

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), new SimConfig(200), new RaidSetup(new[] { Tank("r:tankA"), Tank("r:tankB") }),
            new EncounterDef("t", "T", new[] { boss }, null, timeline)));

        Assert.Contains(result.Events, e => e is MechanicEvent m && m.Note == "taunt-swap");
        Assert.True(BossDamageOn(result, "r:tankA") > 0 && BossDamageOn(result, "r:tankB") > 0,
            "after a swap the boss should have hit both tanks");
    }

    private static int TankBossShare(bool giveTaunt)
    {
        AbilityDef taunt = new(new AbilityId("test.taunt"), CastTicks: 0, GcdTicks: 15,
            CooldownTicks: 30, Priority: 0, new TauntEffect());
        var tank = new CombatantSpec(new CombatantId("r:tank"), CombatantKind.Raider, Side.Raid, CombatantRole.Tank, "Tank",
            new StatBlock(MaxHp: 100_000, AttackDamage: 4, AttackVariance: 0, SwingIntervalTicks: 8), // low threat
            giveTaunt ? new[] { taunt } : null);

        return BossDamageOn(Fight(tank, Melee("r:dps", damage: 40)), "r:tank"); // dps out-threats the tank
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
