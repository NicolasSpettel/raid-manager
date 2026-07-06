using System.Collections.Generic;
using Engine;

namespace Content;

/// <summary>
/// The authored class roster (M1). One per role, so every combat system is exercised. Working names —
/// renameable (naming is deferred in the design). Adding a class is a new row plus its kit rows.
/// </summary>
public static class Classes
{
    public static ClassDef Guardian { get; } = new(
        "guardian", "Guardian", CombatantRole.Tank,
        new StatBlock(MaxHp: 900, AttackDamage: 6, AttackVariance: 2, SwingIntervalTicks: 8),
        new[] { "guardian.shield_slam" });

    public static ClassDef Cleric { get; } = new(
        "cleric", "Cleric", CombatantRole.Healer,
        new StatBlock(MaxHp: 450, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
            MaxResource: 1200, ResourceRegenPerTick: 4),
        new[] { "cleric.mend", "cleric.smite" });

    public static ClassDef Blademaster { get; } = new(
        "blademaster", "Blademaster", CombatantRole.Melee,
        new StatBlock(MaxHp: 500, AttackDamage: 12, AttackVariance: 4, SwingIntervalTicks: 4),
        new[] { "blademaster.mortal_strike" });

    public static ClassDef Pyromancer { get; } = new(
        "pyromancer", "Pyromancer", CombatantRole.Ranged,
        new StatBlock(MaxHp: 420, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
            MaxResource: 1000, ResourceRegenPerTick: 3),
        new[] { "pyromancer.fireball", "pyromancer.scorch" });

    // Declared last so the four class properties above are initialized before the registry reads them.
    public static ClassRegistry Registry { get; } = new(new[] { Guardian, Cleric, Blademaster, Pyromancer });
}
