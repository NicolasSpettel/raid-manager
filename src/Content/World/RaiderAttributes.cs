using System;
using System.Collections.Generic;
using System.Linq;

namespace Content;

/// <summary>Whether an attribute scales output directly, or drives a seeded behavioral decision (GDD §8a′).</summary>
public enum AttributeKind
{
    Scalar,
    Behavioral,
}

/// <summary>How an attribute moves with age (GDD §8): twitch falls past peak, wisdom can still rise, neutral holds.</summary>
public enum AgingClass
{
    Twitch,
    Wisdom,
    Neutral,
}

/// <summary>
/// How an attribute projects from the four latent factors (entities-and-worldgen §3.2). Integer weights;
/// each attribute is <c>10 + Σ (weight × (latent − 50)) / 100</c>, so a maxed latent (100) at weight 20
/// shifts the attribute by +10 off the baseline of 10. Designers tune this loading table, and coherence
/// falls out — a *new* attribute just declares its loadings and is automatically coherent.
/// </summary>
public sealed record LatentLoading(int Talent = 0, int Discipline = 0, int Experience = 0, int Volatility = 0);

/// <summary>
/// One trainable raider attribute (GDD §8 proposed list). Registry-keyed so the final set is a data change,
/// not code (entities §2) — adding attribute #12 is one row here plus its latent loadings. 1–20 scale,
/// baseline 10. <b>First-pass loadings</b> — tune freely; the attribute list itself is still [OPEN].
/// </summary>
public sealed record AttributeDef(string Id, string Name, AttributeKind Kind, AgingClass Aging, LatentLoading Loading);

/// <summary>Ordered, id-keyed registry of raider attributes — the authority on which attributes exist.</summary>
public sealed class AttributeRegistry
{
    private readonly Dictionary<string, AttributeDef> _byId;

    public AttributeRegistry(IEnumerable<AttributeDef> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        All = attributes.ToList();
        _byId = All.ToDictionary(a => a.Id, StringComparer.Ordinal);
    }

    /// <summary>Insertion order — stable, so world-gen projection and hashing are deterministic.</summary>
    public IReadOnlyList<AttributeDef> All { get; }

    public AttributeDef Get(string id) => _byId[id];
}
