using Engine;
using Xunit;

namespace Engine.Tests;

/// <summary>
/// Behavioral coverage of the generalized sim: all three outcomes, driven by the combatant model
/// rather than a pinned hash (so intent stays legible even when a golden re-blesses).
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
    public void NeitherSideDeals_OutcomeIsTimeout()
    {
        SimResult result = Fight(
            raider: Spec("r:pacifist", Side.Raid, maxHp: 100, damage: 0, swing: 5),
            enemy: Spec("boss:inert", Side.Enemy, maxHp: 100, damage: 0, swing: 5));

        Assert.Equal(EncounterOutcome.Timeout, result.Outcome);
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
