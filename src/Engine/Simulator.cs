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
            if (c.Abilities.Count > 0)
            {
                ctx.Queue.Schedule(0, new ScheduledAction(ActionKind.Decide, c.Id));
            }
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
            Dispatch(ctx, action, tick);

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

    private static void Dispatch(SimContext ctx, ScheduledAction action, int tick)
    {
        switch (action.Kind)
        {
            case ActionKind.Swing:
                ResolveSwing(ctx, action.Actor, tick);
                break;
            case ActionKind.Decide:
                ResolveDecide(ctx, action.Actor, tick);
                break;
            case ActionKind.CastComplete:
                ResolveCastComplete(ctx, action.Actor, action.Ability, tick);
                break;
        }
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
        DealDamage(ctx, actor, target, stats.AttackDamage + Roll(ctx, stats.AttackVariance), ability: null, tick);
        ScheduleNextSwing(ctx, actor, fromTick: tick);
    }

    // The DPS decision point: if free to act, cast the highest-priority ready ability.
    private static void ResolveDecide(SimContext ctx, CombatantId actorId, int tick)
    {
        Combatant? actor = ctx.Get(actorId);
        if (actor is null || !actor.IsAlive || actor.CastingUntilTick.HasValue)
        {
            return;
        }

        if (tick < actor.GcdReadyAt)
        {
            ScheduleDecide(ctx, actor, actor.GcdReadyAt);
            return;
        }

        AbilityDef? ability = PickAbility(actor, tick);
        if (ability is null)
        {
            ScheduleDecide(ctx, actor, SoonestReadyTick(actor, tick)); // everything on cooldown
            return;
        }

        Combatant? target = ctx.PickTarget(actor);
        if (target is null)
        {
            return; // no enemy left; the encounter is about to end
        }

        if (ability.CastTicks <= 0)
        {
            ApplyAbility(ctx, actor, target, ability, tick);
            StartCooldowns(actor, ability, tick);
            ScheduleDecide(ctx, actor, actor.GcdReadyAt);
        }
        else
        {
            ctx.Emit(new CastStart(new Tick(tick), actor.Id, ability.Id, ability.CastTicks, target.Id));
            actor.CastingUntilTick = tick + ability.CastTicks;
            ctx.Queue.Schedule(tick + ability.CastTicks, new ScheduledAction(ActionKind.CastComplete, actor.Id, ability.Id));
        }
    }

    private static void ResolveCastComplete(SimContext ctx, CombatantId actorId, AbilityId? abilityId, int tick)
    {
        Combatant? actor = ctx.Get(actorId);
        if (actor is null)
        {
            return;
        }

        actor.CastingUntilTick = null;
        if (!actor.IsAlive || abilityId is not { } id)
        {
            return;
        }

        AbilityDef? ability = FindAbility(actor, id);
        if (ability is null)
        {
            return;
        }

        Combatant? target = ctx.PickTarget(actor);
        if (target is not null)
        {
            ApplyAbility(ctx, actor, target, ability, tick);
        }

        ctx.Emit(new CastEnd(new Tick(tick), actor.Id, ability.Id, CastResult.Done));
        StartCooldowns(actor, ability, tick);
        ScheduleDecide(ctx, actor, actor.GcdReadyAt);
    }

    private static void ApplyAbility(SimContext ctx, Combatant source, Combatant target, AbilityDef ability, int tick)
    {
        switch (ability.Effect)
        {
            case DirectDamage dd:
                DealDamage(ctx, source, target, dd.Amount + Roll(ctx, dd.Variance), ability.Id, tick);
                break;
            default:
                throw new NotSupportedException($"Unhandled ability effect: {ability.Effect.GetType().Name}");
        }
    }

    private static void DealDamage(SimContext ctx, Combatant source, Combatant target, int amount, AbilityId? ability, int tick)
    {
        target.Hp -= amount;
        ctx.Emit(new Damage(new Tick(tick), source.Id, target.Id, amount, ability));
        if (!target.IsAlive)
        {
            ctx.Emit(new Death(new Tick(tick), target.Id));
        }
    }

    private static void StartCooldowns(Combatant actor, AbilityDef ability, int tick)
    {
        actor.GcdReadyAt = tick + Math.Max(ability.GcdTicks, 1); // GCD ≥ 1 guarantees forward progress
        if (ability.CooldownTicks > 0)
        {
            actor.SetCooldownReadyAt(ability.Id, tick + ability.CooldownTicks);
        }
    }

    private static AbilityDef? PickAbility(Combatant actor, int tick)
    {
        AbilityDef? best = null;
        foreach (AbilityDef ability in actor.Abilities)
        {
            if (tick >= actor.CooldownReadyAt(ability.Id) && (best is null || ability.Priority > best.Priority))
            {
                best = ability;
            }
        }

        return best;
    }

    private static int SoonestReadyTick(Combatant actor, int tick)
    {
        int soonest = int.MaxValue;
        foreach (AbilityDef ability in actor.Abilities)
        {
            int readyAt = actor.CooldownReadyAt(ability.Id);
            if (readyAt > tick && readyAt < soonest)
            {
                soonest = readyAt;
            }
        }

        return soonest == int.MaxValue ? tick + 1 : soonest;
    }

    private static AbilityDef? FindAbility(Combatant actor, AbilityId id)
    {
        foreach (AbilityDef ability in actor.Abilities)
        {
            if (ability.Id == id)
            {
                return ability;
            }
        }

        return null;
    }

    private static int Roll(SimContext ctx, int variance) => variance > 0 ? ctx.Rng.NextInt(variance) : 0;

    private static void ScheduleDecide(SimContext ctx, Combatant actor, int atTick) =>
        ctx.Queue.Schedule(atTick, new ScheduledAction(ActionKind.Decide, actor.Id));

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
