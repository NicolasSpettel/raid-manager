namespace Engine;

/// <summary>What a scheduled action does. Only auto-attacks in M1 step 1; casts/timeline triggers join later.</summary>
internal enum ActionKind
{
    Swing,
}

/// <summary>A future action owned by a combatant.</summary>
internal readonly record struct ScheduledAction(ActionKind Kind, CombatantId Actor);

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
