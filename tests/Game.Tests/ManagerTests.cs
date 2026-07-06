using System.Collections.Generic;
using System.Linq;
using Content;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>Manager creation (GDD §2): baseline + chosen background spread + point-buy, clamped 1–20.</summary>
public class ManagerTests
{
    [Fact]
    public void Create_AppliesBase_Background_AndPointBuy()
    {
        var pointBuy = new Dictionary<string, int> { ["charm"] = 2 };
        Manager manager = Managers.Create("Aldric Vance", 35, "Ironreach", "guild_officer", pointBuy);

        // guild_officer background: motivation +3, leadership +2, development +1.
        Assert.Equal(ManagerProfile.BaseValue + 3, manager.Of("motivation"));
        Assert.Equal(ManagerProfile.BaseValue + 2, manager.Of("leadership"));
        Assert.Equal(ManagerProfile.BaseValue + 2, manager.Of("charm")); // base + 2 point-buy, no bg bonus
        Assert.Equal("Aldric Vance", manager.Name);
        Assert.Equal("guild_officer", manager.BackgroundId);
    }

    [Fact]
    public void Create_ClampsEveryAttributeIntoRange()
    {
        var lopsided = ManagerProfile.Attributes.ToDictionary(a => a.Id, _ => 50, System.StringComparer.Ordinal);
        Manager manager = Managers.Create("Over Spender", 30, "Sunmere", "theorycrafter", lopsided);

        Assert.All(manager.Attributes.Values, v => Assert.InRange(v, 1, ManagerProfile.MaxValue));
    }

    [Fact]
    public void EveryBackgroundBonus_ReferencesARealAttribute()
    {
        var attributeIds = ManagerProfile.Attributes.Select(a => a.Id).ToHashSet(System.StringComparer.Ordinal);

        foreach (BackgroundDef background in ManagerProfile.Backgrounds)
        {
            foreach (string key in background.Bonuses.Keys)
            {
                Assert.Contains(key, attributeIds);
            }
        }
    }
}
