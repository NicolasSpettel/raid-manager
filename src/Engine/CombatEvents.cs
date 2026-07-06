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

/// <summary>
/// The engine's only output is a versioned, append-only stream of these. Every downstream feature
/// (combat log, stage renderer, damage meters, career history) is a fold over this stream; consumers
/// never re-derive a fact the engine can emit (BLUEPRINT §5, ADR-0004).
/// </summary>
public abstract record CombatEvent(Tick Tick);

/// <summary>The encounter began.</summary>
public sealed record EncounterStart(Tick Tick, string EncounterId) : CombatEvent(Tick);

/// <summary><paramref name="Source"/> dealt <paramref name="Amount"/> damage to <paramref name="Target"/>.</summary>
public sealed record Damage(Tick Tick, CombatantId Source, CombatantId Target, int Amount) : CombatEvent(Tick);

/// <summary><paramref name="Victim"/> died.</summary>
public sealed record Death(Tick Tick, CombatantId Victim) : CombatEvent(Tick);

/// <summary>The encounter resolved with <paramref name="Outcome"/>.</summary>
public sealed record EncounterEnd(Tick Tick, EncounterOutcome Outcome) : CombatEvent(Tick);
