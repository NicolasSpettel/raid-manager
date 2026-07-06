using System.Collections.Generic;
using Content;
using Engine;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>Gear folds into combat stats, and the equip-upgrade rule only swaps for something better.</summary>
public class GearTests
{
    [Fact]
    public void EquippedGear_FoldsIntoCombatStats()
    {
        var bare = new RaiderRecord("r:1", "Tank", "guardian", Equipped: new List<string>());
        RaiderRecord geared = bare with { Equipped = new List<string> { "iron_plate" } }; // +180 max HP

        CombatantSpec bareSpec = Warband.ToCombatant(bare);
        CombatantSpec gearedSpec = Warband.ToCombatant(geared);

        Assert.Equal(bareSpec.Stats.MaxHp + 180, gearedSpec.Stats.MaxHp);
        Assert.True(Warband.GearPower(geared) > Warband.GearPower(bare));
    }

    [Fact]
    public void EquipIfUpgrade_SwapsOnlyForSomethingBetter()
    {
        var raider = new RaiderRecord("r:1", "DPS", "blademaster", Equipped: new List<string> { "iron_sword" });

        RaiderRecord upgraded = Warband.EquipIfUpgrade(raider, Items.Registry.Get("steel_greatsword"));
        RaiderRecord unchanged = Warband.EquipIfUpgrade(raider, Items.Registry.Get("rusty_blade"));

        Assert.Contains("steel_greatsword", upgraded.Equipped!);
        Assert.DoesNotContain("rusty_blade", unchanged.Equipped!);
        Assert.Same(raider, unchanged); // no upgrade returns the same instance (pure)
    }
}
