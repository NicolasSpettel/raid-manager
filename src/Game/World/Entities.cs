using System.Collections.Generic;
using Content;
using Engine;

namespace Game;

/// <summary>A stable, world-unique, permanent raider id (entities §5) — referenced everywhere by id, never by object.</summary>
public readonly record struct RaiderId(string Value);

/// <summary>A stable guild id.</summary>
public readonly record struct GuildId(string Value);

/// <summary>Immutable identity (entities §2). Age is NOT stored — it derives from <see cref="BirthSeason"/> vs the world clock.</summary>
public sealed record Identity(string Name, string Region, int BirthSeason, ulong PortraitSeed);

/// <summary>
/// Vocation (entities §2): the class plus a hidden per-role growth <b>potential</b> (0–100). Stars are NOT
/// stored here — they are derived on read from attributes + class fit, capped by this potential (§3.3).
/// </summary>
public sealed record Vocation(string ClassId, IReadOnlyDictionary<CombatantRole, int> PotentialByRole);

/// <summary>
/// The registry-keyed attribute vector (entities §2): attributeId → value on the 1–20 scale, baseline 10.
/// Code reads <c>attributes.Of("composure")</c>, never a hardcoded field, so adding an attribute is a data change.
/// </summary>
public sealed record AttributeVector(IReadOnlyDictionary<string, int> Values)
{
    public int Of(string attributeId) => Values.TryGetValue(attributeId, out int v) ? v : 10;
}

/// <summary>The high-mutation-rate condition component (GDD §8, FM two-axis + morale). Baseline at world-gen; the living world moves it.</summary>
public sealed record Condition(int Morale, int Freshness, int Sharpness);

/// <summary>
/// A character as a composition of components (entities §2), not a fat record. Baseline slice: identity,
/// vocation, attributes, condition, membership + the generating archetype (provenance). Contract and the
/// event-sourced career ledger are their own components, added next — composition makes that cheap.
/// </summary>
public sealed record Raider(
    RaiderId Id,
    Identity Identity,
    Vocation Vocation,
    AttributeVector Attributes,
    Condition Condition,
    string ArchetypeId,
    GuildId? Membership);

/// <summary>A guild: identity, region, prestige tier, and its roster (raiders referenced by id).</summary>
public sealed record Guild(GuildId Id, string Name, string Region, PrestigeTier Tier, IReadOnlyList<RaiderId> Roster);

/// <summary>
/// A generated world: the deterministic baseline of guilds + raiders + free agents for a seed. Age derives
/// from <see cref="CurrentSeason"/> vs each raider's birth-season, so aging is just the clock advancing.
/// </summary>
public sealed record World(
    ulong Seed,
    int CurrentSeason,
    IReadOnlyList<Guild> Guilds,
    IReadOnlyDictionary<RaiderId, Raider> Raiders,
    IReadOnlyList<RaiderId> FreeAgents)
{
    public Raider Get(RaiderId id) => Raiders[id];

    /// <summary>Age in years, derived — never stored (entities §3.3).</summary>
    public int AgeOf(Raider raider) => CurrentSeason - raider.Identity.BirthSeason;
}
