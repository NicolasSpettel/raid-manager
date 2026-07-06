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

        var ctx = new SimContext(input.Rng);
        EncounterOutcome outcome = RunDummyFight(ctx, input.Config, input.Encounter);

        return new SimResult(
            ctx.Events,
            outcome,
            input.Rng.Seed,
            EngineVersion,
            EventSchemaVersion);
    }

    // M0's only "content" is this hand-built dummy fight — a test fixture, not shipped content.
    // One attacker swings on a fixed cadence for seeded, variable damage until the target dies.
    // The damage roll uses the rng so different seeds produce genuinely different streams.
    private static EncounterOutcome RunDummyFight(SimContext ctx, SimConfig config, EncounterDef encounter)
    {
        const int swingEveryTicks = 5;
        const int baseDamage = 8;
        const int damageSpread = 5; // adds 0..4 per swing

        var attacker = new CombatantId(1);
        var target = new CombatantId(2);
        int targetHp = encounter.TargetHp;

        ctx.Emit(new EncounterStart(Tick.Zero, encounter.Id));

        for (int t = swingEveryTicks; t <= config.MaxTicks; t += swingEveryTicks)
        {
            var tick = new Tick(t);
            int damage = baseDamage + ctx.Rng.NextInt(damageSpread);
            targetHp -= damage;
            ctx.Emit(new Damage(tick, attacker, target, damage));

            if (targetHp <= 0)
            {
                ctx.Emit(new Death(tick, target));
                ctx.Emit(new EncounterEnd(tick, EncounterOutcome.Kill));
                return EncounterOutcome.Kill;
            }
        }

        ctx.Emit(new EncounterEnd(new Tick(config.MaxTicks), EncounterOutcome.Timeout));
        return EncounterOutcome.Timeout;
    }
}
