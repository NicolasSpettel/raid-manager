using System.Collections.Generic;

namespace Engine;

/// <summary>
/// Explicit per-simulation state, passed as a parameter to engine functions. Engine code closes over
/// NOTHING — DM1's original sin was helpers capturing engine-local state (BLUEPRINT §5). Combatants
/// live in insertion-ordered storage; the id map is for lookup only, never iterated for ordering
/// (determinism, engine-spec §5). Internal: it never leaves the engine.
/// </summary>
internal sealed class SimContext
{
    private readonly List<CombatEvent> _events = new();
    private readonly List<Combatant> _spawnOrder = new();
    private readonly Dictionary<CombatantId, Combatant> _byId = new();

    public SimContext(SeededRng rng, SimConfig config)
    {
        Rng = rng;
        Config = config;
        Queue = new ActionQueue();
    }

    /// <summary>The primary boss (first enemy spawned) — the reference for HP-based phase triggers.</summary>
    public Combatant? Boss { get; private set; }

    /// <summary>Current encounter phase (0 = opener).</summary>
    public int CurrentPhase { get; set; }

    public IReadOnlyList<PhaseDef> Phases { get; private set; } = System.Array.Empty<PhaseDef>();

    public IReadOnlyList<MechanicInstance> Timeline { get; private set; } = System.Array.Empty<MechanicInstance>();

    public void SetEncounter(IReadOnlyList<PhaseDef>? phases, IReadOnlyList<MechanicInstance>? timeline)
    {
        Phases = phases ?? System.Array.Empty<PhaseDef>();
        Timeline = timeline ?? System.Array.Empty<MechanicInstance>();
    }

    public SeededRng Rng { get; }

    public SimConfig Config { get; }

    public ActionQueue Queue { get; }

    public IReadOnlyList<CombatEvent> Events => _events;

    public IReadOnlyList<Combatant> SpawnOrder => _spawnOrder;

    public void Emit(CombatEvent e) => _events.Add(e);

    public void Spawn(Combatant c)
    {
        _byId[c.Id] = c;
        _spawnOrder.Add(c);
        if (Boss is null && c.Side == Side.Enemy)
        {
            Boss = c;
        }
    }

    public Combatant? Get(CombatantId id) => _byId.TryGetValue(id, out Combatant? c) ? c : null;

    public bool AnyAlive(Side side)
    {
        foreach (Combatant c in _spawnOrder)
        {
            if (c.Side == side && c.IsAlive)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>First alive combatant on the opposing side, in spawn order. Threat/tanking arrives later.</summary>
    public Combatant? PickEnemy(Combatant actor)
    {
        Side enemySide = actor.Side == Side.Raid ? Side.Enemy : Side.Raid;
        foreach (Combatant c in _spawnOrder)
        {
            if (c.Side == enemySide && c.IsAlive)
            {
                return c;
            }
        }

        return null;
    }

    /// <summary>The most-injured living ally (largest HP deficit), or null if nobody needs healing.</summary>
    public Combatant? PickInjuredAlly(Combatant actor)
    {
        Combatant? best = null;
        int bestDeficit = 0;
        foreach (Combatant c in _spawnOrder)
        {
            if (c.Side != actor.Side || !c.IsAlive)
            {
                continue;
            }

            int deficit = c.MaxHp - c.Hp;
            if (deficit > bestDeficit)
            {
                bestDeficit = deficit;
                best = c;
            }
        }

        return best;
    }
}
