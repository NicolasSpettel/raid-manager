using System.Collections.Generic;
using Engine;

namespace Content;

/// <summary>
/// The authored encounter catalog. An encounter is one data row — enemies, phases, and a mechanic
/// timeline the engine's generic runtime interprets. Adding a boss is a new row here and nothing
/// else: the "one new file" definition of done (engine-spec §8, content-authoring, BLUEPRINT §11).
/// </summary>
public static class Encounters
{
    public static EncounterDef Warden { get; } = new(
        "warden", "The Warden",
        new[] { Boss("boss:warden", "The Warden", maxHp: 1200, attack: 25, swing: 8) },
        new[] { new PhaseDef(0, "Opening"), new PhaseDef(1, "Frenzy", HpBelowPct: 50) },
        new[]
        {
            new MechanicInstance("warden.spread", MechanicArchetype.SpreadDamage, MechanicSchedule.Repeating(40, 40), 25),
            new MechanicInstance("warden.buster", MechanicArchetype.TankBuster, MechanicSchedule.Repeating(30, 30), 80),
            new MechanicInstance("warden.frenzy_spread", MechanicArchetype.SpreadDamage, MechanicSchedule.Repeating(10, 25), 20, Phase: 1),
        });

    // A SECOND boss, authored entirely as data — the proof that a new encounter is one new row.
    public static EncounterDef Sentinel { get; } = new(
        "sentinel", "The Sentinel",
        new[] { Boss("boss:sentinel", "The Sentinel", maxHp: 900, attack: 18, swing: 10) },
        new[] { new PhaseDef(0, "Guard") },
        new[]
        {
            new MechanicInstance("sentinel.slam", MechanicArchetype.TankBuster, MechanicSchedule.Repeating(20, 25), 60),
            new MechanicInstance("sentinel.pulse", MechanicArchetype.SpreadDamage, MechanicSchedule.Repeating(35, 35), 18),
        });

    public static IReadOnlyList<EncounterDef> All { get; } = new[] { Warden, Sentinel };

    private static CombatantSpec Boss(string id, string name, int maxHp, int attack, int swing) => new(
        new CombatantId(id), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, name,
        new StatBlock(maxHp, attack, AttackVariance: attack / 4, SwingIntervalTicks: swing));
}
