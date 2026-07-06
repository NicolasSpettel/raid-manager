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

    // How often an idle actor re-checks whether it can act (e.g. a healer waiting for someone to get hurt).
    private const int IdlePollTicks = 5;

    public static SimResult SimulateEncounter(SimInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var ctx = new SimContext(input.Rng, input.Config);
        SpawnSide(ctx, input.Raid.Raiders);
        SpawnSide(ctx, input.Encounter.Enemies);

        ctx.Emit(new EncounterStart(Tick.Zero, input.Encounter.Id));
        ctx.SetEncounter(input.Encounter.Phases, input.Encounter.Timeline);
        ScheduleMechanics(ctx, input.Config.MaxTicks);

        foreach (Combatant c in ctx.SpawnOrder)
        {
            ScheduleNextSwing(ctx, c, fromTick: 0);
            if (c.Abilities.Count > 0)
            {
                ScheduleDecide(ctx, c, c.ReactionTicks); // reaction delay applies to the opener too
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
            MaybeAdvancePhase(ctx, tick);

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
            case ActionKind.Mechanic:
                ResolveMechanic(ctx, action.MechanicIndex, tick);
                break;
            case ActionKind.AuraTick:
                ResolveAuraTick(ctx, action.Actor, action.AuraKey, tick);
                break;
            case ActionKind.AuraExpire:
                ResolveAuraExpire(ctx, action.Actor, action.AuraKey, tick);
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

        Combatant? target = ctx.PickEnemy(actor);
        if (target is null)
        {
            return;
        }

        StatBlock stats = actor.Spec.Stats;
        DealDamage(ctx, actor, target, Scale(actor, stats.AttackDamage + Roll(ctx, stats.AttackVariance)), ability: null, tick);
        ScheduleNextSwing(ctx, actor, fromTick: tick);
    }

    // The role decision point: cast the highest-priority ready, affordable ability that has a target.
    private static void ResolveDecide(SimContext ctx, CombatantId actorId, int tick)
    {
        Combatant? actor = ctx.Get(actorId);
        if (actor is null || !actor.IsAlive || actor.CastingUntilTick.HasValue)
        {
            return;
        }

        RegenResource(actor, tick);

        if (tick < actor.GcdReadyAt)
        {
            ScheduleDecide(ctx, actor, actor.GcdReadyAt);
            return;
        }

        (AbilityDef Ability, Combatant Target)? choice = ChooseAction(ctx, actor, tick);
        if (choice is not { } action)
        {
            ScheduleDecide(ctx, actor, tick + IdlePollTicks); // nothing to do now; re-check soon
            return;
        }

        SpendResource(ctx, actor, action.Ability, tick);

        if (action.Ability.CastTicks <= 0)
        {
            ApplyAbility(ctx, actor, action.Target, action.Ability, tick);
            StartCooldowns(actor, action.Ability, tick);
            ScheduleDecideAfterAction(ctx, actor);
        }
        else
        {
            ctx.Emit(new CastStart(new Tick(tick), actor.Id, action.Ability.Id, action.Ability.CastTicks, action.Target.Id));
            actor.CastingUntilTick = tick + action.Ability.CastTicks;
            ctx.Queue.Schedule(tick + action.Ability.CastTicks, new ScheduledAction(ActionKind.CastComplete, actor.Id, action.Ability.Id));
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

        Combatant? target = PickTargetFor(ctx, actor, ability.Effect); // re-pick: heal lands on whoever's hurt now
        if (target is not null)
        {
            ApplyAbility(ctx, actor, target, ability, tick);
        }

        ctx.Emit(new CastEnd(new Tick(tick), actor.Id, ability.Id, CastResult.Done));
        StartCooldowns(actor, ability, tick);
        ScheduleDecideAfterAction(ctx, actor);
    }

    // Highest-priority ability that is off cooldown, affordable, and has a valid target for its effect.
    private static (AbilityDef Ability, Combatant Target)? ChooseAction(SimContext ctx, Combatant actor, int tick)
    {
        AbilityDef? bestAbility = null;
        Combatant? bestTarget = null;
        foreach (AbilityDef ability in actor.Abilities)
        {
            if (tick < actor.CooldownReadyAt(ability.Id) || actor.Resource < ability.ResourceCost)
            {
                continue;
            }

            Combatant? target = PickTargetFor(ctx, actor, ability.Effect);
            if (target is null)
            {
                continue;
            }

            if (bestAbility is null || ability.Priority > bestAbility.Priority)
            {
                bestAbility = ability;
                bestTarget = target;
            }
        }

        return bestAbility is null ? null : (bestAbility, bestTarget!);
    }

    private static Combatant? PickTargetFor(SimContext ctx, Combatant actor, AbilityEffect effect) => effect switch
    {
        DirectHeal => ctx.PickInjuredAlly(actor),
        _ => ctx.PickEnemy(actor),
    };

    private static void ApplyAbility(SimContext ctx, Combatant source, Combatant target, AbilityDef ability, int tick)
    {
        switch (ability.Effect)
        {
            case DirectDamage dd:
                DealDamage(ctx, source, target, Scale(source, dd.Amount + Roll(ctx, dd.Variance)), ability.Id, tick);
                break;
            case DirectHeal dh:
                ApplyHeal(ctx, source, target, dh, ability.Id, tick);
                break;
            default:
                throw new NotSupportedException($"Unhandled ability effect: {ability.Effect.GetType().Name}");
        }
    }

    private static void DealDamage(SimContext ctx, Combatant source, Combatant target, int amount, AbilityId? ability, int tick)
    {
        int taken = amount * target.DamageTakenMultPct / 100; // debuff stacks raise damage taken
        target.Hp -= taken;
        ctx.Emit(new Damage(new Tick(tick), source.Id, target.Id, taken, ability));
        if (!target.IsAlive)
        {
            ctx.Emit(new Death(new Tick(tick), target.Id));
        }
    }

    private static void ApplyAuraTo(SimContext ctx, Combatant target, AuraDef def, int tick)
    {
        bool wasActive = target.GetAura(def.Id) is not null;
        AuraInstance instance = target.ApplyAura(def, tick);
        ctx.Emit(new AuraApply(new Tick(tick), target.Id, def.Id, instance.Stacks));

        // Every apply schedules a fresh expiry; stale ones no-op (they check ExpiresAtTick).
        ctx.Queue.Schedule(instance.ExpiresAtTick, new ScheduledAction(ActionKind.AuraExpire, target.Id, AuraKey: def.Id));

        // Start the damage-over-time chain only on a NEW application (a refresh already has one running).
        if (!wasActive && def.TickIntervalTicks > 0 && def.DamagePerTick > 0)
        {
            ctx.Queue.Schedule(tick + def.TickIntervalTicks, new ScheduledAction(ActionKind.AuraTick, target.Id, AuraKey: def.Id));
        }
    }

    private static void ResolveAuraTick(SimContext ctx, CombatantId bearerId, string? auraKey, int tick)
    {
        if (auraKey is null)
        {
            return;
        }

        Combatant? bearer = ctx.Get(bearerId);
        AuraInstance? aura = bearer?.GetAura(auraKey);
        if (bearer is null || !bearer.IsAlive || aura is null || tick > aura.ExpiresAtTick)
        {
            return;
        }

        Combatant source = ctx.Boss ?? bearer; // damage-over-time is attributed to the boss
        DealDamage(ctx, source, bearer, aura.Def.DamagePerTick, new AbilityId(auraKey), tick);

        int next = tick + aura.Def.TickIntervalTicks;
        if (next <= aura.ExpiresAtTick)
        {
            ctx.Queue.Schedule(next, new ScheduledAction(ActionKind.AuraTick, bearerId, AuraKey: auraKey));
        }
    }

    private static void ResolveAuraExpire(SimContext ctx, CombatantId bearerId, string? auraKey, int tick)
    {
        if (auraKey is null)
        {
            return;
        }

        Combatant? bearer = ctx.Get(bearerId);
        AuraInstance? aura = bearer?.GetAura(auraKey);
        if (bearer is null || aura is null || tick < aura.ExpiresAtTick)
        {
            return; // refreshed later — not actually expired yet
        }

        bearer.RemoveAura(auraKey);
        ctx.Emit(new AuraExpire(new Tick(tick), bearerId, auraKey));
    }

    private static void ApplyHeal(SimContext ctx, Combatant source, Combatant target, DirectHeal heal, AbilityId ability, int tick)
    {
        int raw = heal.Amount + Roll(ctx, heal.Variance);
        int before = target.Hp;
        int after = Math.Min(target.MaxHp, before + raw);
        target.Hp = after;
        ctx.Emit(new Heal(new Tick(tick), source.Id, target.Id, after - before, ability, raw - (after - before)));
    }

    private static void SpendResource(SimContext ctx, Combatant actor, AbilityDef ability, int tick)
    {
        if (ability.ResourceCost <= 0)
        {
            return;
        }

        actor.Resource -= ability.ResourceCost;
        ctx.Emit(new ResourceChange(new Tick(tick), actor.Id, -ability.ResourceCost, actor.Resource));
    }

    private static void RegenResource(Combatant actor, int tick)
    {
        int regenPerTick = actor.Spec.Stats.ResourceRegenPerTick;
        if (regenPerTick <= 0)
        {
            return;
        }

        int elapsed = tick - actor.LastResourceTick;
        if (elapsed > 0)
        {
            actor.Resource = Math.Min(actor.Spec.Stats.MaxResource, actor.Resource + (elapsed * regenPerTick));
            actor.LastResourceTick = tick;
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

    private static int Scale(Combatant source, int amount) => amount * source.DamageDealtMultPct / 100;

    private static void ScheduleMechanics(SimContext ctx, int maxTicks)
    {
        for (int i = 0; i < ctx.Timeline.Count; i++)
        {
            foreach (int t in FiringTicks(ctx.Timeline[i].Schedule, maxTicks))
            {
                ctx.Queue.Schedule(t, new ScheduledAction(ActionKind.Mechanic, MechanicIndex: i));
            }
        }
    }

    private static IEnumerable<int> FiringTicks(MechanicSchedule schedule, int maxTicks)
    {
        if (schedule.AtTick is { } at)
        {
            if (at <= maxTicks)
            {
                yield return at;
            }

            yield break;
        }

        if (schedule.EveryTicks is { } every && every > 0)
        {
            int limit = schedule.Count ?? int.MaxValue;
            int fired = 0;
            for (int t = schedule.StartTick; t <= maxTicks && fired < limit; t += every)
            {
                yield return t;
                fired++;
            }
        }
    }

    // One generic runtime interprets every mechanic archetype — adding a boss is data, not code.
    private static void ResolveMechanic(SimContext ctx, int index, int tick)
    {
        if (index < 0 || index >= ctx.Timeline.Count)
        {
            return;
        }

        MechanicInstance mechanic = ctx.Timeline[index];
        if (mechanic.Phase is { } phase && phase != ctx.CurrentPhase)
        {
            return; // gated to a phase we're not in
        }

        Combatant? boss = ctx.Boss;
        if (boss is null || !boss.IsAlive)
        {
            return;
        }

        switch (mechanic.Archetype)
        {
            case MechanicArchetype.SpreadDamage:
                ctx.Emit(new MechanicEvent(new Tick(tick), mechanic.Id, "spread"));
                foreach (Combatant r in ctx.SpawnOrder)
                {
                    if (r.Side == Side.Raid && r.IsAlive)
                    {
                        DealDamage(ctx, boss, r, Scale(boss, mechanic.Amount), new AbilityId(mechanic.Id), tick);
                    }
                }

                break;

            case MechanicArchetype.TankBuster:
                Combatant? tankTarget = ctx.PickEnemy(boss);
                if (tankTarget is not null)
                {
                    ctx.Emit(new MechanicEvent(new Tick(tick), mechanic.Id, "buster"));
                    DealDamage(ctx, boss, tankTarget, Scale(boss, mechanic.Amount), new AbilityId(mechanic.Id), tick);
                }

                break;

            case MechanicArchetype.Enrage:
                boss.DamageDealtMultPct += mechanic.Amount;
                ctx.Emit(new MechanicEvent(new Tick(tick), mechanic.Id, "enrage"));
                break;

            case MechanicArchetype.RaidDot:
                ctx.Emit(new MechanicEvent(new Tick(tick), mechanic.Id, "dot"));
                var dot = new AuraDef($"aura:{mechanic.Id}", DurationTicks: 50, TickIntervalTicks: 10, DamagePerTick: mechanic.Amount);
                foreach (Combatant r in ctx.SpawnOrder)
                {
                    if (r.Side == Side.Raid && r.IsAlive)
                    {
                        ApplyAuraTo(ctx, r, dot, tick);
                    }
                }

                break;

            case MechanicArchetype.TankDebuff:
                Combatant? debuffTarget = ctx.PickEnemy(boss);
                if (debuffTarget is not null)
                {
                    ctx.Emit(new MechanicEvent(new Tick(tick), mechanic.Id, "debuff"));
                    var debuff = new AuraDef(
                        $"aura:{mechanic.Id}", DurationTicks: 80, DamageTakenBonusPctPerStack: mechanic.Amount, MaxStacks: 5);
                    ApplyAuraTo(ctx, debuffTarget, debuff, tick);
                }

                break;
        }
    }

    private static void MaybeAdvancePhase(SimContext ctx, int tick)
    {
        while (ctx.CurrentPhase + 1 < ctx.Phases.Count)
        {
            PhaseDef next = ctx.Phases[ctx.CurrentPhase + 1];
            bool byTick = next.AtTick is { } at && tick >= at;
            bool byHp = next.HpBelowPct is { } pct && ctx.Boss is { } boss && boss.Hp * 100 <= pct * boss.MaxHp;
            if (!byTick && !byHp)
            {
                break;
            }

            ctx.CurrentPhase++;
            ctx.Emit(new PhaseChange(new Tick(tick), ctx.CurrentPhase, next.Name));
        }
    }

    private static void ScheduleDecideAfterAction(SimContext ctx, Combatant actor) =>
        ScheduleDecide(ctx, actor, actor.GcdReadyAt + actor.ReactionTicks);

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
