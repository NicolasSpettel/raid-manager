using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>
/// Projects persistent <see cref="RaiderRecord"/>s into combat <see cref="CombatantSpec"/>s, folding the
/// raider's class base stats with its equipped gear — the boundary where the management layer becomes a
/// combat input. Also owns the equip-upgrade rule.
/// </summary>
public static class Warband
{
    /// <summary>Percent added to HP and auto-attack per level past 1 (first-pass, tunable).</summary>
    private const int LevelBonusPct = 8;

    /// <summary>Build a combat spec from a raider: class kit + base stats + equipped gear bonuses.</summary>
    public static CombatantSpec ToCombatant(RaiderRecord raider)
    {
        ArgumentNullException.ThrowIfNull(raider);
        ClassDef cls = Classes.Registry.Get(raider.ClassId);

        int bonusHp = 0;
        int bonusAttack = 0;
        foreach (ItemDef item in EquippedItems(raider))
        {
            bonusHp += item.BonusMaxHp;
            bonusAttack += item.BonusAttackDamage;
        }

        StatBlock stats = cls.BaseStats with
        {
            MaxHp = cls.BaseStats.MaxHp + bonusHp,
            AttackDamage = cls.BaseStats.AttackDamage + bonusAttack,
        };

        // Each level past 1 adds a flat percentage to survivability and auto-attack power, so leveling is a
        // real progression axis alongside gear (first-pass, tunable). Ability amounts are still flat (no
        // spellpower stat yet), so this helps tanks/melee more than casters/healers — a known gap.
        int levelPct = 100 + ((Math.Max(1, raider.Level) - 1) * LevelBonusPct);
        stats = stats with
        {
            MaxHp = stats.MaxHp * levelPct / 100,
            AttackDamage = stats.AttackDamage * levelPct / 100,
        };

        if (raider.InjuryRaidsLeft > 0)
        {
            // An injured raider fights at reduced strength until they recover.
            stats = stats with { MaxHp = stats.MaxHp * 70 / 100, AttackDamage = stats.AttackDamage * 70 / 100 };
        }

        IReadOnlyList<AbilityDef> kit = cls.Kit.Select(Abilities.Registry.Def).ToList();
        return new CombatantSpec(
            new CombatantId(raider.Id), CombatantKind.Raider, Side.Raid, cls.Role, raider.Name, stats, kit);
    }

    /// <summary>Total power of a raider's equipped gear (0 if none).</summary>
    public static int GearPower(RaiderRecord raider) => EquippedItems(raider).Sum(i => i.Power);

    /// <summary>Equip the item if it improves the raider's slot; otherwise return the raider unchanged.</summary>
    public static RaiderRecord EquipIfUpgrade(RaiderRecord raider, ItemDef item)
    {
        ArgumentNullException.ThrowIfNull(raider);
        ArgumentNullException.ThrowIfNull(item);

        var equipped = (raider.Equipped ?? Enumerable.Empty<string>()).ToList();
        int slotIndex = equipped.FindIndex(id => Items.Registry.TryGet(id, out ItemDef existing) && existing.Slot == item.Slot);

        if (slotIndex >= 0)
        {
            if (Items.Registry.Get(equipped[slotIndex]).Power >= item.Power)
            {
                return raider; // current gear is as good or better
            }

            equipped[slotIndex] = item.Id;
        }
        else
        {
            equipped.Add(item.Id);
        }

        return raider with { Equipped = equipped };
    }

    private static IEnumerable<ItemDef> EquippedItems(RaiderRecord raider) =>
        (raider.Equipped ?? Enumerable.Empty<string>())
            .Where(Items.Registry.Contains)
            .Select(Items.Registry.Get);
}
