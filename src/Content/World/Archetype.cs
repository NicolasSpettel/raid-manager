using System;
using System.Collections.Generic;
using System.Linq;

namespace Content;

/// <summary>The league-pyramid tier a guild sits in (GDD §3) — elite guilds draw better archetypes.</summary>
public enum PrestigeTier
{
    WorldElite,
    Continental,
    National,
    Local,
}

/// <summary>The four latent factors an archetype rolls; attributes project off these (entities §3.2). 0–100, centred at 50.</summary>
public sealed record LatentProfile(int Talent, int Discipline, int Experience, int Volatility);

/// <summary>
/// A world-gen archetype (entities §3.1): a *distribution* of person, not fixed values. It supplies the
/// latent means (rolled with <see cref="LatentSpread"/>), an age tendency, and a per-prestige-tier weight
/// so elite guilds draw more prodigies/veterans and local guilds more journeymen. Adding one = a data row.
/// </summary>
public sealed record ArchetypeDef(
    string Id,
    string Name,
    LatentProfile LatentMeans,
    int LatentSpread,
    int AgeMean,
    int AgeSpread,
    IReadOnlyList<int> PrestigeWeights) // indexed by (int)PrestigeTier
{
    /// <summary>This archetype's draw weight for a guild in <paramref name="tier"/> (0 = never appears there).</summary>
    public int WeightFor(PrestigeTier tier) => PrestigeWeights[(int)tier];
}

/// <summary>Id-keyed archetype registry.</summary>
public sealed class ArchetypeRegistry
{
    private readonly Dictionary<string, ArchetypeDef> _byId;

    public ArchetypeRegistry(IEnumerable<ArchetypeDef> archetypes)
    {
        ArgumentNullException.ThrowIfNull(archetypes);
        All = archetypes.ToList();
        _byId = All.ToDictionary(a => a.Id, StringComparer.Ordinal);
    }

    /// <summary>Insertion order — stable for deterministic weighted picks.</summary>
    public IReadOnlyList<ArchetypeDef> All { get; }

    public ArchetypeDef Get(string id) => _byId[id];
}
