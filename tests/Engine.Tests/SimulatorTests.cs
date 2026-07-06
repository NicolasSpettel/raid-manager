using System.Linq;
using Engine;
using Xunit;

namespace Engine.Tests;

/// <summary>
/// Behavioral coverage of the sim: outcomes, the cast system, healing/roles, and the execution model —
/// intent stated as behavior rather than a pinned hash (so it stays legible even when a golden re-blesses).
/// </summary>
public class SimulatorTests
{
    [Fact]
    public void BossDies_OutcomeIsKill()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Trio(1));
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
    }

    [Fact]
    public void RaidTooWeak_OutcomeIsWipe()
    {
        SimResult result = Fight(
            raider: Spec("r:weak", Side.Raid, maxHp: 30, damage: 1, swing: 10),
            enemy: Spec("boss:strong", Side.Enemy, maxHp: 100_000, damage: 50, swing: 5));

        Assert.Equal(EncounterOutcome.Wipe, result.Outcome);
    }

    [Fact]
    public void Caster_EmitsCastEvents_AndAbilityDamage()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Caster(1));

        Assert.Contains(result.Events, e => e is CastStart);
        Assert.Contains(result.Events, e => e is CastEnd ce && ce.Result == CastResult.Done);
        Assert.Contains(result.Events, e => e is Damage d && d.Ability is not null);
    }

    [Fact]
    public void Healer_KeepsTankAlive_RaidWins()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Raid(1, withHealer: true));
        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
    }

    [Fact]
    public void WithoutHealer_TankDiesFirst_RaidWipes()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Raid(1, withHealer: false));
        Assert.Equal(EncounterOutcome.Wipe, result.Outcome);
    }

    [Fact]
    public void Raid_EmitsHealAndResourceSpend()
    {
        SimResult result = Simulator.SimulateEncounter(Fixtures.Raid(1));

        Assert.Contains(result.Events, e => e is Heal h && h.Amount > 0);
        Assert.Contains(result.Events, e => e is ResourceChange rc && rc.Delta < 0);
    }

    // Execution quality: a disciplined raider loses less uptime and out-damages a sloppy one over
    // the same window — the link where management becomes combat outcome (engine-spec §9).
    [Fact]
    public void CrispRaider_OutDamages_SloppyRaider()
    {
        int crisp = TotalAbilityDamage(reactionTicks: 0);
        int sloppy = TotalAbilityDamage(reactionTicks: 5);

        Assert.True(crisp > sloppy, $"crisp {crisp} should exceed sloppy {sloppy}");
    }

    [Fact]
    public void NeitherSideDeals_OutcomeIsTimeout()
    {
        SimResult result = Fight(
            raider: Spec("r:pacifist", Side.Raid, maxHp: 100, damage: 0, swing: 5),
            enemy: Spec("boss:inert", Side.Enemy, maxHp: 100, damage: 0, swing: 5));

        Assert.Equal(EncounterOutcome.Timeout, result.Outcome);
    }

    private static int TotalAbilityDamage(int reactionTicks)
    {
        var bolt = new AbilityDef(
            new AbilityId("t.bolt"), CastTicks: 0, GcdTicks: 10, CooldownTicks: 0, Priority: 50,
            new DirectDamage(20, 0, DamageSchool.Magic));

        var caster = new CombatantSpec(
            new CombatantId("r:c"), CombatantKind.Raider, Side.Raid, CombatantRole.Ranged, "C",
            new StatBlock(MaxHp: 1000, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0),
            new[] { bolt },
            new ExecutionProfile(reactionTicks));

        var target = new CombatantSpec(
            new CombatantId("boss:d"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "D",
            new StatBlock(MaxHp: 1_000_000, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1),
            new SimConfig(200),
            new RaidSetup(new[] { caster }),
            new EncounterDef("t", "T", new[] { target })));

        return result.Events.OfType<Damage>().Sum(d => d.Amount);
    }

    private static SimResult Fight(CombatantSpec raider, CombatantSpec enemy) =>
        Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1),
            SimConfig.Default,
            new RaidSetup(new[] { raider }),
            new EncounterDef("test", "Test", new[] { enemy })));

    private static CombatantSpec Spec(string id, Side side, int maxHp, int damage, int swing) => new(
        new CombatantId(id),
        side == Side.Raid ? CombatantKind.Raider : CombatantKind.Boss,
        side,
        CombatantRole.Melee,
        id,
        new StatBlock(maxHp, damage, AttackVariance: 0, SwingIntervalTicks: swing));
}
