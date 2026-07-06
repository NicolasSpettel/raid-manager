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
        new[] { "guardian.shield_slam", "guardian.taunt" });

    public static ClassDef Cleric { get; } = new(
        "cleric", "Cleric", CombatantRole.Healer,
        new StatBlock(MaxHp: 450, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
            MaxResource: 1200, ResourceRegenPerTick: 4),
        new[] { "cleric.mend", "cleric.smite" });

    public static ClassDef Blademaster { get; } = new(
        "blademaster", "Blademaster", CombatantRole.Melee,
        new StatBlock(MaxHp: 500, AttackDamage: 12, AttackVariance: 4, SwingIntervalTicks: 4),
        new[] { "blademaster.mortal_strike", "blademaster.pummel" });

    public static ClassDef Pyromancer { get; } = new(
        "pyromancer", "Pyromancer", CombatantRole.Ranged,
        new StatBlock(MaxHp: 420, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
            MaxResource: 1000, ResourceRegenPerTick: 3),
        new[] { "pyromancer.fireball", "pyromancer.scorch" });

    public static ClassDef Ranger { get; } = new(
        "ranger", "Ranger", CombatantRole.Ranged,
        new StatBlock(MaxHp: 440, AttackDamage: 8, AttackVariance: 3, SwingIntervalTicks: 5),
        new[] { "ranger.aimed_shot", "ranger.muzzle" });

    public static ClassDef Warlock { get; } = new(
        "warlock", "Warlock", CombatantRole.Ranged,
        new StatBlock(MaxHp: 430, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
            MaxResource: 1000, ResourceRegenPerTick: 3),
        new[] { "warlock.shadow_bolt", "warlock.drain" });

    public static ClassDef Paladin { get; } = new(
        "paladin", "Paladin", CombatantRole.Tank,
        new StatBlock(MaxHp: 950, AttackDamage: 6, AttackVariance: 2, SwingIntervalTicks: 8),
        new[] { "paladin.consecrate", "paladin.hand_of_reckoning" });

    // Declared last so the class properties above are initialized before the registry reads them.
    // Order matters: the first four (one per role) seed CreateStarter's guaranteed role coverage.
    public static ClassRegistry Registry { get; } = new(
        new[] { Guardian, Cleric, Blademaster, Pyromancer, Ranger, Warlock, Paladin });
}
