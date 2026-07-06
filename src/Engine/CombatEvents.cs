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

/// <summary><paramref name="Victim"/> died.</summary>
public sealed record Death(Tick Tick, CombatantId Victim) : CombatEvent(Tick);

/// <summary>The encounter resolved with <paramref name="Outcome"/>.</summary>
public sealed record EncounterEnd(Tick Tick, EncounterOutcome Outcome) : CombatEvent(Tick);
