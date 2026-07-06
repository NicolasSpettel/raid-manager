using System.Collections.Generic;
using System.Linq;

namespace Content;

/// <summary>A typed registry of gear items keyed by id.</summary>
public sealed class ItemRegistry
{
    private readonly Dictionary<string, ItemDef> _rows;

    public ItemRegistry(IEnumerable<ItemDef> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _rows = items.ToDictionary(i => i.Id, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<ItemDef> All => _rows.Values;

    public bool Contains(string id) => _rows.ContainsKey(id);

    public ItemDef Get(string id) => _rows[id];

    public bool TryGet(string id, out ItemDef item) => _rows.TryGetValue(id, out item!);
}
