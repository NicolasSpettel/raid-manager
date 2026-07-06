using System.Collections.Generic;
using System.Linq;

namespace Content;

/// <summary>A typed registry of classes keyed by id. A completeness test asserts every kit resolves.</summary>
public sealed class ClassRegistry
{
    private readonly Dictionary<string, ClassDef> _rows;

    public ClassRegistry(IEnumerable<ClassDef> classes)
    {
        ArgumentNullException.ThrowIfNull(classes);
        _rows = classes.ToDictionary(c => c.Id, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<ClassDef> All => _rows.Values;

    public ClassDef Get(string id) => _rows[id];
}
