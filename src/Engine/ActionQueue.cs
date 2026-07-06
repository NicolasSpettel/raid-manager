namespace Engine;

/// <summary>What a scheduled action does.</summary>
internal enum ActionKind
{
    /// <summary>A weapon auto-attack lands and reschedules itself.</summary>
    Swing,

    /// <summary>A combatant chooses and begins its next ability (the DPS decision point).</summary>
    Decide,

    /// <summary>A cast-time ability finishes and applies its effect.</summary>
    CastComplete,
}

/// <summary>A future action owned by a combatant. <see cref="Ability"/> is set only for <see cref="ActionKind.CastComplete"/>.</summary>
internal readonly record struct ScheduledAction(ActionKind Kind, CombatantId Actor, AbilityId? Ability = null);

/// <summary>
/// Deterministic schedule of future actions, ordered by (tick, insertion seq). Same-tick actions
/// resolve in the order they were scheduled — the seq tiebreaker keeps the ordering total, so the
/// stream never depends on heap internals. Work is scheduled, not scanned (engine-spec §3, §5).
/// </summary>
internal sealed class ActionQueue
{
    private readonly PriorityQueue<ScheduledAction, (int Tick, int Seq)> _queue = new();
    private int _seq;

    public int Count => _queue.Count;

    public void Schedule(int tick, ScheduledAction action) => _queue.Enqueue(action, (tick, _seq++));

    public bool TryPeekTick(out int tick)
    {
        if (_queue.TryPeek(out _, out (int Tick, int Seq) priority))
        {
            tick = priority.Tick;
            return true;
        }

        tick = 0;
        return false;
    }

    public bool TryDequeue(out ScheduledAction action, out int tick)
    {
        if (_queue.TryDequeue(out action, out (int Tick, int Seq) priority))
        {
            tick = priority.Tick;
            return true;
        }

        tick = 0;
        return false;
    }
}
