using System.Collections.Generic;
using Engine;

namespace Content;

/// <summary>
/// An authored class — identity, role, base stats, and a kit of ability ids (into the ability
/// registry). A raider is this data plus a factory call (<see cref="Roster.CreateRaider"/>) — the
/// concrete meaning of Principle 0 for characters.
/// </summary>
public sealed record ClassDef(
    string Id,
    string Name,
    CombatantRole Role,
    StatBlock BaseStats,
    IReadOnlyList<string> Kit);
