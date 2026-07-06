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

    // A tier-2 boss showcasing the aura mechanics: a stacking tank debuff (the tank takes escalating
    // damage — the tankSwapDebuff idea) and a raid-wide damage-over-time. Meant for a geared guild.
    public static EncounterDef AshenKing { get; } = new(
        "ashen_king", "The Ashen King",
        new[] { Boss("boss:ashen", "The Ashen King", maxHp: 2000, attack: 28, swing: 8) },
        new[] { new PhaseDef(0, "Cinders"), new PhaseDef(1, "Conflagration", HpBelowPct: 40) },
        new[]
        {
            new MechanicInstance("ashen.debuff", MechanicArchetype.TankDebuff, MechanicSchedule.Repeating(22, 22), 12),
            new MechanicInstance("ashen.buster", MechanicArchetype.TankBuster, MechanicSchedule.Repeating(30, 30), 85),
            new MechanicInstance("ashen.dot", MechanicArchetype.RaidDot, MechanicSchedule.Repeating(40, 40), 6),
            new MechanicInstance("ashen.spread", MechanicArchetype.SpreadDamage, MechanicSchedule.Repeating(15, 22), 24, Phase: 1),
            new MechanicInstance("ashen.enrage", MechanicArchetype.Enrage, MechanicSchedule.Once(500), 50),
        });

    // A tier-3 wall: heavier tank debuff, more frequent DoT, and a punishing frenzy phase.
    public static EncounterDef Frostwarden { get; } = new(
        "frostwarden", "The Frostwarden",
        new[] { Boss("boss:frost", "The Frostwarden", maxHp: 2800, attack: 34, swing: 8) },
        new[] { new PhaseDef(0, "Rime"), new PhaseDef(1, "Blizzard", HpBelowPct: 35) },
        new[]
        {
            new MechanicInstance("frost.debuff", MechanicArchetype.TankDebuff, MechanicSchedule.Repeating(18, 18), 14),
            new MechanicInstance("frost.icebolt", MechanicArchetype.InterruptibleCast, MechanicSchedule.Repeating(24, 24), 130),
            new MechanicInstance("frost.buster", MechanicArchetype.TankBuster, MechanicSchedule.Repeating(26, 26), 100),
            new MechanicInstance("frost.dot", MechanicArchetype.RaidDot, MechanicSchedule.Repeating(30, 30), 8),
            new MechanicInstance("frost.spread", MechanicArchetype.SpreadDamage, MechanicSchedule.Repeating(12, 18), 30, Phase: 1),
            new MechanicInstance("frost.enrage", MechanicArchetype.Enrage, MechanicSchedule.Once(600), 40),
        });

    public static IReadOnlyList<EncounterDef> All { get; } = new[] { Warden, Sentinel, AshenKing, Frostwarden };

    private static CombatantSpec Boss(string id, string name, int maxHp, int attack, int swing) => new(
        new CombatantId(id), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, name,
        new StatBlock(maxHp, attack, AttackVariance: attack / 4, SwingIntervalTicks: swing));
}
