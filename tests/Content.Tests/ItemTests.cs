using Content;
using Xunit;

namespace Content.Tests;

/// <summary>Item catalog completeness and loot-table integrity.</summary>
public class ItemTests
{
    [Fact]
    public void Registry_IsNotEmpty()
    {
        Assert.NotEmpty(Items.Registry.All);
    }

    [Fact]
    public void EveryItem_IsComplete()
    {
        foreach (ItemDef item in Items.Registry.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Name), $"{item.Id}: missing Name");
            Assert.True(item.BonusMaxHp >= 0, $"{item.Id}: BonusMaxHp must be >= 0");
            Assert.True(item.BonusAttackDamage >= 0, $"{item.Id}: BonusAttackDamage must be >= 0");
            Assert.True(item.Power > 0, $"{item.Id}: Power should be positive");
        }
    }

    [Theory]
    [InlineData("warden")]
    [InlineData("sentinel")]
    [InlineData("ashen_king")]
    public void EveryLootEntry_ResolvesToARealItem(string encounterId)
    {
        var pool = Loot.For(encounterId);
        Assert.NotEmpty(pool);
        foreach (ItemDef item in pool)
        {
            Assert.True(Items.Registry.Contains(item.Id), $"{encounterId} drops unknown item {item.Id}");
        }
    }
}
