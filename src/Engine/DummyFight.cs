namespace Engine;

/// <summary>
/// The canonical M0 dummy-fight fixture. One place builds it, so the Sim CLI, the Godot App, and the
/// golden tests all simulate the exact same thing — a single source of truth for the fixture. This is
/// the only place the "one engine, many consumers, byte-identical" M0 floor can be proven.
/// </summary>
public static class DummyFight
{
    public const string EncounterId = "dummy";
    public const int TargetHp = 100;

    /// <summary>Build the standard dummy-fight input for a given seed.</summary>
    public static SimInput CreateInput(ulong seed) =>
        new(new SeededRng(seed), SimConfig.Default, new EncounterDef(EncounterId, TargetHp));

    /// <summary>Simulate the dummy fight at a given seed.</summary>
    public static SimResult Run(ulong seed) => Simulator.SimulateEncounter(CreateInput(seed));
}
