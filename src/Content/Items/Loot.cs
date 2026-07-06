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
    };

    /// <summary>The item pool a given encounter can drop from (empty if none).</summary>
    public static IReadOnlyList<ItemDef> For(string encounterId) =>
        Tables.TryGetValue(encounterId, out string[]? ids)
            ? ids.Select(Items.Registry.Get).ToList()
            : Array.Empty<ItemDef>();
}
