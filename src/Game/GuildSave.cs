using System.Collections.Generic;

namespace Game;

/// <summary>
/// The single versioned aggregate of all persistent state, owned and written exclusively by
/// <c>src/Game</c> (save-format.md, BLUEPRINT §7). A new persistent field is added here, in one place,
/// with a version bump if the shape breaks. UI/app preferences are deliberately NOT in the save.
/// </summary>
public sealed record GuildSave(
    int Version,
    string CreatedAtIso,
    GuildInfo Guild,
    IReadOnlyList<RaiderRecord> Roster,
    Economy Economy,
    IReadOnlyList<RaidSummary> History,
    Manager? Manager = null,
    // Persistence of the living world (ADR-0007): the world regenerates from (WorldSeed, GeneratorVersion);
    // your guild's roster above is the materialized "hot" state. SeasonWeek is where you are in the season.
    ulong WorldSeed = 0,
    int GeneratorVersion = 0,
    string? ManagerGuildId = null,
    int SeasonWeek = 1,
    int SeasonDay = 0,                          // 0=Monday … 6=Sunday within the current week
    IReadOnlyList<string>? DownedThisWeek = null); // weekly lockout: bosses looted since the last Monday reset

/// <summary>Guild identity. <see cref="BoardExpectation"/> is the season goal you agreed at signing (GDD §4).</summary>
public sealed record GuildInfo(string Name, int Reputation, string? BoardExpectation = null);

/// <summary>
/// A persistent roster member. Distinct from the engine's combat <c>CombatantSpec</c>: this is the
/// career entity (identity + class + progression); it is projected into a combatant when a raid runs.
/// </summary>
/// <summary>
/// A raider — the single entity type for both the living world and the player's guild (entities §2, the
/// composition model). Flat <see cref="Name"/>/<see cref="ClassId"/> are the always-present convenience keys;
/// the richer components (<see cref="Identity"/>, <see cref="Vocation"/>, <see cref="ArchetypeId"/>,
/// <see cref="Membership"/>) are set for world-generated raiders and carried when one joins your guild.
/// <see cref="Equipped"/> + <see cref="InjuryRaidsLeft"/> are the career/gear state the player accrues.
/// </summary>
public sealed record RaiderRecord(
    string Id,
    string Name,
    string ClassId,
    IReadOnlyList<string>? Equipped = null,
    int InjuryRaidsLeft = 0,
    AttributeVector? Attributes = null,
    Condition? Condition = null,
    Identity? Identity = null,
    Vocation? Vocation = null,
    string? ArchetypeId = null,
    GuildId? Membership = null);

/// <summary>Guild finances (M1: gold only).</summary>
public sealed record Economy(int Gold);

/// <summary>
/// A compact record of one completed raid — folded from the combat event stream (save-format.md
/// §Career history). Career data only ever grows by small deltas, so saves stay small.
/// </summary>
public sealed record RaidSummary(
    string EncounterId,
    string Outcome,
    int DurationTicks,
    int GoldAwarded,
    IReadOnlyList<RaiderContribution> Contributions,
    string? LootDropped = null);

/// <summary>What one raider did in a raid, folded from the event stream.</summary>
public sealed record RaiderContribution(string RaiderId, int DamageDone, int HealingDone, bool Died);
