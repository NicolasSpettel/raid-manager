using System.Collections.Generic;

namespace Engine;

/// <summary>
/// Explicit per-simulation state, passed as a parameter to engine functions. Engine code closes over
/// NOTHING — DM1's original sin was helpers capturing engine-local state, which made them inseparable
/// (BLUEPRINT §5, dm1-lessons). Internal: it never leaves the engine.
/// </summary>
internal sealed class SimContext
{
    private readonly List<CombatEvent> _events = new();

    public SimContext(SeededRng rng) => Rng = rng;

    public SeededRng Rng { get; }

    public IReadOnlyList<CombatEvent> Events => _events;

    public void Emit(CombatEvent e) => _events.Add(e);
}
