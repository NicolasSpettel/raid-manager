namespace Content;

/// <summary>Equipment slot. One item per slot per raider (M1).</summary>
public enum ItemSlot
{
    Weapon,
    Armor,
    Trinket,
}

/// <summary>
/// An authored gear item — flat stat bonuses that fold into a raider's combat stats when equipped
/// (Principle 0: an item is a data row). <see cref="Power"/> is a single score used to decide upgrades.
/// </summary>
public sealed record ItemDef(string Id, string Name, ItemSlot Slot, int BonusMaxHp, int BonusAttackDamage)
{
    /// <summary>Rough item power for upgrade comparison (attack is weighted since it is scarcer than HP).</summary>
    public int Power => BonusMaxHp + (BonusAttackDamage * 12);
}
