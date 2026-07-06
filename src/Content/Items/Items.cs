using System.Collections.Generic;

namespace Content;

/// <summary>The authored gear catalog (M1). Adding an item is a new row.</summary>
public static class Items
{
    public static ItemRegistry Registry { get; } = new(new List<ItemDef>
    {
        new("rusty_blade", "Rusty Blade", ItemSlot.Weapon, BonusMaxHp: 0, BonusAttackDamage: 4),
        new("iron_sword", "Iron Sword", ItemSlot.Weapon, BonusMaxHp: 0, BonusAttackDamage: 8),
        new("steel_greatsword", "Steel Greatsword", ItemSlot.Weapon, BonusMaxHp: 0, BonusAttackDamage: 14),
        new("leather_vest", "Leather Vest", ItemSlot.Armor, BonusMaxHp: 80, BonusAttackDamage: 0),
        new("iron_plate", "Iron Plate", ItemSlot.Armor, BonusMaxHp: 180, BonusAttackDamage: 0),
        new("warding_charm", "Warding Charm", ItemSlot.Trinket, BonusMaxHp: 60, BonusAttackDamage: 0),
        new("band_of_might", "Band of Might", ItemSlot.Trinket, BonusMaxHp: 0, BonusAttackDamage: 5),

        // Tier-3 gear (Frostwarden drops)
        new("runed_greatblade", "Runed Greatblade", ItemSlot.Weapon, BonusMaxHp: 0, BonusAttackDamage: 20),
        new("dragonscale_plate", "Dragonscale Plate", ItemSlot.Armor, BonusMaxHp: 280, BonusAttackDamage: 0),
        new("sigil_of_power", "Sigil of Power", ItemSlot.Trinket, BonusMaxHp: 0, BonusAttackDamage: 10),
        new("heart_of_winter", "Heart of Winter", ItemSlot.Trinket, BonusMaxHp: 140, BonusAttackDamage: 0),
    });
}
