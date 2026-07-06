using System.Collections.Generic;
using System.Linq;
using Engine;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The FM two-axis condition system (GDD §8 / economy-model §1): freshness drains with raid load and
/// recovers on rest (∝ Endurance), sharpness is built by raiding and lost on the bench, and low condition
/// means worse combat. The model is LOCKED; the numbers here are the first-pass tuning.
/// </summary>
public class ConditionTests
{
    [Fact]
    public void Performance_IsNeutralWhenFresh_AndDropsWithFatigue()
    {
        Assert.Equal(100, ConditionModel.PerformancePct(ConditionModel.Fresh));
        Assert.Equal(100, ConditionModel.PerformancePct(null)); // untracked = neutral
        Assert.True(ConditionModel.PerformancePct(new Condition(Morale: 50, Freshness: 30, Sharpness: 20)) < 100);
    }

    [Fact]
    public void FatiguedRaider_DealsLessDamage()
    {
        int fresh = DummyDamage(ConditionModel.Fresh);
        int tired = DummyDamage(new Condition(Morale: 50, Freshness: 20, Sharpness: 20));
        Assert.True(tired < fresh, $"tired={tired} fresh={fresh}");
    }

    [Fact]
    public void GrindingDrainsFreshness_RestingRecoversIt()
    {
        var start = new Condition(Morale: 70, Freshness: 80, Sharpness: 60);
        Condition grinded = ConditionModel.AfterWeek(start, raidDays: 4, endurance: 10);
        Condition rested = ConditionModel.AfterWeek(start, raidDays: 0, endurance: 10);

        Assert.True(grinded.Freshness < start.Freshness, "four raid nights should drain freshness");
        Assert.True(rested.Freshness > start.Freshness, "a rest week should recover it");
    }

    [Fact]
    public void Raiding_BuildsSharpness_BenchingDecaysIt()
    {
        var start = new Condition(Morale: 70, Freshness: 100, Sharpness: 50);
        Assert.True(ConditionModel.AfterWeek(start, 2, 10).Sharpness > start.Sharpness); // played → sharper
        Assert.True(ConditionModel.AfterWeek(start, 0, 10).Sharpness < start.Sharpness); // benched → rusty
    }

    [Fact]
    public void HigherEndurance_RecoversFaster()
    {
        var start = new Condition(Morale: 70, Freshness: 50, Sharpness: 60);
        int low = ConditionModel.AfterWeek(start, raidDays: 1, endurance: 2).Freshness;
        int high = ConditionModel.AfterWeek(start, raidDays: 1, endurance: 18).Freshness;
        Assert.True(high > low, $"Endurance should govern recovery: high={high} low={low}");
    }

    [Fact]
    public void GuildAfterWeek_AdvancesEveryRaider()
    {
        GuildSave guild = Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z");
        GuildSave after = ConditionModel.AfterWeek(guild, raidDays: 4);
        Assert.All(after.Roster, r => Assert.True((r.Condition ?? ConditionModel.Fresh).Freshness < 100)); // all drained
    }

    private static int DummyDamage(Condition condition)
    {
        var raider = new RaiderRecord("r:x", "X", "blademaster", Equipped: new List<string>(), Condition: condition);
        CombatantSpec spec = Warband.ToCombatant(raider);
        var dummy = new CombatantSpec(
            new CombatantId("boss:d"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "D",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), new SimConfig(100), new RaidSetup(new[] { spec }),
            new EncounterDef("t", "T", new[] { dummy })));

        return result.Events.OfType<Damage>().Where(d => d.Target.Value == "boss:d").Sum(d => d.Amount);
    }
}
