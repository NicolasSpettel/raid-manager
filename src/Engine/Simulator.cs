namespace Engine;

/// <summary>
/// The engine entry point: a pure function from <see cref="SimInput"/> to <see cref="SimResult"/>.
/// Deterministic by construction — every random draw comes from the required <see cref="SeededRng"/>,
/// so the same input yields a byte-identical event stream on every machine, forever.
/// </summary>
public static class Simulator
{
    /// <summary>Bumped when engine behavior changes in a way that invalidates old replays.</summary>
    public const int EngineVersion = 1;

    /// <summary>Bumped when the event schema changes (new or altered event records).</summary>
    public const int EventSchemaVersion = 1;

    public static SimResult SimulateEncounter(SimInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var ctx = new SimContext(input.Rng, input.Config);
        SpawnSide(ctx, input.Raid.Raiders);
        SpawnSide(ctx, input.Encounter.Enemies);

        ctx.Emit(new EncounterStart(Tick.Zero, input.Encounter.Id));

        foreach (Combatant c in ctx.SpawnOrder)
        {
            ScheduleNextSwing(ctx, c, fromTick: 0);
        }

        EncounterOutcome outcome = RunLoop(ctx, input.Config);
        return new SimResult(ctx.Events, outcome, input.Rng.Seed, EngineVersion, EventSchemaVersion);
    }

    private static void SpawnSide(SimContext ctx, IReadOnlyList<CombatantSpec> specs)
    {
        foreach (CombatantSpec spec in specs)
        {
            ctx.Spawn(new Combatant(spec));
        }
    }

    // Process scheduled actions in (tick, seq) order until a side is wiped or the tick budget runs out.
    private static EncounterOutcome RunLoop(SimContext ctx, SimConfig config)
    {
        while (ctx.Queue.TryPeekTick(out int nextTick))
        {
            if (nextTick > config.MaxTicks)
            {
                break;
            }

            ctx.Queue.TryDequeue(out ScheduledAction action, out int tick);
            ResolveSwing(ctx, action.Actor, tick);

            if (!ctx.AnyAlive(Side.Enemy))
            {
                return End(ctx, tick, EncounterOutcome.Kill);
            }

            if (!ctx.AnyAlive(Side.Raid))
            {
                return End(ctx, tick, EncounterOutcome.Wipe);
            }
        }

        return End(ctx, config.MaxTicks, EncounterOutcome.Timeout);
    }

    private static void ResolveSwing(SimContext ctx, CombatantId actorId, int tick)
    {
        Combatant? actor = ctx.Get(actorId);
        if (actor is null || !actor.IsAlive)
        {
            return; // dead actors don't swing and don't reschedule — they drain out of the queue
        }

        Combatant? target = ctx.PickTarget(actor);
        if (target is null)
        {
            return;
        }

        StatBlock stats = actor.Spec.Stats;
        int damage = stats.AttackDamage + (stats.AttackVariance > 0 ? ctx.Rng.NextInt(stats.AttackVariance) : 0);
        target.Hp -= damage;
        ctx.Emit(new Damage(new Tick(tick), actor.Id, target.Id, damage));

        if (!target.IsAlive)
        {
            ctx.Emit(new Death(new Tick(tick), target.Id));
        }

        ScheduleNextSwing(ctx, actor, fromTick: tick);
    }

    private static void ScheduleNextSwing(SimContext ctx, Combatant c, int fromTick)
    {
        int interval = c.Spec.Stats.SwingIntervalTicks;
        if (interval > 0)
        {
            ctx.Queue.Schedule(fromTick + interval, new ScheduledAction(ActionKind.Swing, c.Id));
        }
    }

    private static EncounterOutcome End(SimContext ctx, int tick, EncounterOutcome outcome)
    {
        ctx.Emit(new EncounterEnd(new Tick(tick), outcome));
        return outcome;
    }
}
