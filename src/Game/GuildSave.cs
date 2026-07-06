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
    IReadOnlyList<RaidSummary> History);

/// <summary>Guild identity.</summary>
public sealed record GuildInfo(string Name, int Reputation);

/// <summary>
/// A persistent roster member. Distinct from the engine's combat <c>CombatantSpec</c>: this is the
/// career entity (identity + class + progression); it is projected into a combatant when a raid runs.
/// </summary>
public sealed record RaiderRecord(string Id, string Name, string ClassId, int Level = 1, int Xp = 0);

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
    IReadOnlyList<RaiderContribution> Contributions);

/// <summary>What one raider did in a raid, folded from the event stream.</summary>
public sealed record RaiderContribution(string RaiderId, int DamageDone, int HealingDone, bool Died);
