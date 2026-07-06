using System.Collections.Generic;

namespace Engine;

/// <summary>Everything the engine needs to simulate one encounter. The rng is required (no fallback).</summary>
public sealed record SimInput(SeededRng Rng, SimConfig Config, RaidSetup Raid, EncounterDef Encounter);

/// <summary>Global knobs for a single simulation.</summary>
public sealed record SimConfig(int MaxTicks)
{
    /// <summary>Default budget: 60 seconds of battle time.</summary>
    public static SimConfig Default => new(TimeModel.SecondsToTicks(60));
}

/// <summary>The raid side: the combatants the player fields (plus, later, assignments).</summary>
public sealed record RaidSetup(IReadOnlyList<CombatantSpec> Raiders);

/// <summary>
/// The enemy content. In M1 step 1 this is just id/name + the enemy spawns; phases, timeline, and
/// mechanic archetypes arrive at step 4 (engine-spec §8).
/// </summary>
public sealed record EncounterDef(string Id, string Name, IReadOnlyList<CombatantSpec> Enemies);

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
