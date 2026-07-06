using System.Collections.Generic;

namespace Engine;

/// <summary>Everything the engine needs to simulate one encounter. The rng is required (no fallback).</summary>
public sealed record SimInput(SeededRng Rng, SimConfig Config, EncounterDef Encounter);

/// <summary>Global knobs for a single simulation.</summary>
public sealed record SimConfig(int MaxTicks)
{
    /// <summary>Default budget: 60 seconds of battle time.</summary>
    public static SimConfig Default => new(TimeModel.SecondsToTicks(60));
}

/// <summary>
/// A minimal encounter definition. In M0 this is only the dummy target's health; the full,
/// registry-driven encounter model (phases, mechanic archetypes) arrives with M1 (engine-spec §8).
/// </summary>
public sealed record EncounterDef(string Id, int TargetHp);

/// <summary>The engine's result: the event stream plus provenance for versioned, golden replay.</summary>
public sealed record SimResult(
    IReadOnlyList<CombatEvent> Events,
    EncounterOutcome Outcome,
    ulong Seed,
    int EngineVersion,
    int EventSchemaVersion)
{
    /// <summary>The canonical stable hash of this result's event stream.</summary>
    public string Hash() => EventStream.Hash(Events);
}
