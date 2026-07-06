namespace Engine;

/// <summary>How an encounter ended.</summary>
public enum EncounterOutcome
{
    /// <summary>The boss/target was defeated.</summary>
    Kill,

    /// <summary>The raid died.</summary>
    Wipe,

    /// <summary>Neither side resolved before the tick budget ran out.</summary>
    Timeout,
}

/// <summary>How a cast finished.</summary>
public enum CastResult
{
    Done,
    Interrupted,
    Cancelled,
}

/// <summary>
/// The engine's only output is a versioned, append-only stream of these. Every downstream feature
/// (combat log, stage renderer, damage meters, career history) is a fold over this stream; consumers
/// never re-derive a fact the engine can emit (BLUEPRINT §5, ADR-0004).
/// </summary>
public abstract record CombatEvent(Tick Tick);

/// <summary>The encounter began.</summary>
public sealed record EncounterStart(Tick Tick, string EncounterId) : CombatEvent(Tick);

/// <summary>The encounter advanced to a new phase.</summary>
public sealed record PhaseChange(Tick Tick, int Phase, string Name) : CombatEvent(Tick);

/// <summary>A boss mechanic fired. <paramref name="Note"/> is a short label (e.g. "spread", "buster").</summary>
public sealed record MechanicEvent(Tick Tick, string Mechanic, string Note) : CombatEvent(Tick);

/// <summary>An aura was applied to (or refreshed on) <paramref name="Target"/>, now at <paramref name="Stacks"/> stacks.</summary>
public sealed record AuraApply(Tick Tick, CombatantId Target, string Aura, int Stacks) : CombatEvent(Tick);

/// <summary>An aura expired from <paramref name="Target"/>.</summary>
public sealed record AuraExpire(Tick Tick, CombatantId Target, string Aura) : CombatEvent(Tick);

/// <summary><paramref name="Source"/> began casting <paramref name="Ability"/> at <paramref name="Target"/>.</summary>
public sealed record CastStart(Tick Tick, CombatantId Source, AbilityId Ability, int DurationTicks, CombatantId Target)
    : CombatEvent(Tick);

/// <summary><paramref name="Source"/>'s cast of <paramref name="Ability"/> finished with <paramref name="Result"/>.</summary>
public sealed record CastEnd(Tick Tick, CombatantId Source, AbilityId Ability, CastResult Result) : CombatEvent(Tick);

/// <summary>
/// <paramref name="Source"/> dealt <paramref name="Amount"/> damage to <paramref name="Target"/>.
/// <paramref name="Ability"/> is null for a weapon auto-attack, set for ability damage.
/// </summary>
public sealed record Damage(Tick Tick, CombatantId Source, CombatantId Target, int Amount, AbilityId? Ability = null)
    : CombatEvent(Tick);

/// <summary>
/// <paramref name="Source"/> healed <paramref name="Target"/> for <paramref name="Amount"/> effective
/// health (<paramref name="Overheal"/> was wasted above the target's max).
/// </summary>
public sealed record Heal(Tick Tick, CombatantId Source, CombatantId Target, int Amount, AbilityId Ability, int Overheal)
    : CombatEvent(Tick);

/// <summary><paramref name="Who"/>'s resource changed by <paramref name="Delta"/> (now at <paramref name="Now"/>).</summary>
public sealed record ResourceChange(Tick Tick, CombatantId Who, int Delta, int Now) : CombatEvent(Tick);

/// <summary><paramref name="Victim"/> died.</summary>
public sealed record Death(Tick Tick, CombatantId Victim) : CombatEvent(Tick);

/// <summary>Whether a ground hazard is appearing or clearing.</summary>
public enum HazardState
{
    Spawn,
    Expire,
}

/// <summary>
/// A ground hazard (void zone) at <paramref name="At"/> with <paramref name="Radius"/> — the telegraph
/// geometry a spatial renderer draws, and the shape raiders must run out of. Emitted only by spatial
/// encounters, so non-spatial fights carry no hazard events (their streams are unchanged).
/// </summary>
public sealed record HazardEvent(Tick Tick, string Id, Position At, int Radius, HazardState State) : CombatEvent(Tick);

/// <summary><paramref name="Who"/> moved to <paramref name="To"/> (running out of a hazard).</summary>
public sealed record MoveEvent(Tick Tick, CombatantId Who, Position To) : CombatEvent(Tick);

/// <summary>The encounter resolved with <paramref name="Outcome"/>.</summary>
public sealed record EncounterEnd(Tick Tick, EncounterOutcome Outcome) : CombatEvent(Tick);
