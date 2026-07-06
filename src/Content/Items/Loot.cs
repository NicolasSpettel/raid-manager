using System;
using System.Collections.Generic;
using System.Linq;

namespace Content;

/// <summary>Per-encounter loot tables — which items a boss can drop. Data, keyed by encounter id.</summary>
public static class Loot
{
    private static readonly Dictionary<string, string[]> Tables = new(StringComparer.Ordinal)
    {
        ["warden"] = new[] { "iron_sword", "iron_plate", "warding_charm", "steel_greatsword", "band_of_might" },
        ["sentinel"] = new[] { "rusty_blade", "leather_vest", "warding_charm" },
        ["ashen_king"] = new[] { "steel_greatsword", "iron_plate", "band_of_might" },
        ["frostwarden"] = new[] { "runed_greatblade", "dragonscale_plate", "sigil_of_power", "heart_of_winter" },
        // Dungeon catch-up gear (GDD §6): lower-tier items to gear up the bench between raids.
        ["dungeon"] = new[] { "rusty_blade", "iron_sword", "leather_vest", "warding_charm", "band_of_might", "iron_plate" },
    };

    /// <summary>The item pool a given encounter can drop from (empty if none).</summary>
    public static IReadOnlyList<ItemDef> For(string encounterId) =>
        Tables.TryGetValue(encounterId, out string[]? ids)
            ? ids.Select(Items.Registry.Get).ToList()
            : Array.Empty<ItemDef>();
}
