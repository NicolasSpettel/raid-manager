using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Content;

/// <summary>
/// A typed registry of ability rows keyed by id (content-authoring §4). The engine, the AI, and the
/// UI all read the same row. A completeness test asserts every row is whole.
/// </summary>
public sealed class AbilityRegistry
{
    private readonly Dictionary<string, AbilityRow> _rows;

    public AbilityRegistry(IEnumerable<AbilityRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _rows = rows.ToDictionary(r => r.Id, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<AbilityRow> All => _rows.Values;

    public AbilityRow Get(string id) => _rows[id];

    /// <summary>The engine-ready ability for an id (the factory, via the row).</summary>
    public AbilityDef Def(string id) => Get(id).ToDef();
}
