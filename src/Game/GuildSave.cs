using System.Collections.Generic;

namespace Game;

/// <summary>
/// The single versioned aggregate of all persistent state, owned and written exclusively by
/// <c>src/Game</c> (save-format.md, BLUEPRINT §7). A new persistent field is added here, in one place,
/// with a version bump if the shape breaks. UI/app preferences are deliberately NOT in the save.
/// M1 carries the guild, roster, and economy; calendar/history/settings join as their systems land.
/// </summary>
public sealed record GuildSave(
    int Version,
    string CreatedAtIso,
    GuildInfo Guild,
    IReadOnlyList<RaiderRecord> Roster,
    Economy Economy);

/// <summary>Guild identity.</summary>
public sealed record GuildInfo(string Name, int Reputation);

/// <summary>
/// A persistent roster member. Distinct from the engine's combat <c>CombatantSpec</c>: this is the
/// career entity (identity + class); it is projected into a combatant when a raid runs.
/// </summary>
public sealed record RaiderRecord(string Id, string Name, string ClassId);

/// <summary>Guild finances (M1: gold only).</summary>
public sealed record Economy(int Gold);
