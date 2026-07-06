namespace Engine;

/// <summary>
/// A boss mechanic archetype. One generic runtime interprets all of them, so a new boss is a new
/// data row referencing these — never new code (engine-spec §8, BLUEPRINT §6). The M1 v0 set is what
/// the current engine can fully execute; debuff/interrupt archetypes join when auras/threat/interrupts
/// land (see docs/m1-build-plan.md step 4).
/// </summary>
public enum MechanicArchetype
{
    /// <summary>Raid-wide hit: <c>Amount</c> damage to every alive raider (pressures the healer).</summary>
    SpreadDamage,

    /// <summary>Heavy hit of <c>Amount</c> to the boss's current target (the tank must be topped).</summary>
    TankBuster,

    /// <summary>The boss gains a permanent <c>+Amount%</c> to the damage it deals (a soft enrage).</summary>
    Enrage,
}

/// <summary>When a mechanic fires: once at a tick, or repeating from a start tick.</summary>
public sealed record MechanicSchedule(int? AtTick = null, int StartTick = 0, int? EveryTicks = null, int? Count = null)
{
    public static MechanicSchedule Once(int atTick) => new(AtTick: atTick);

    public static MechanicSchedule Repeating(int startTick, int everyTicks, int? count = null) =>
        new(StartTick: startTick, EveryTicks: everyTicks, Count: count);
}

/// <summary>
/// One entry on an encounter's timeline: a mechanic archetype, its schedule, and its (single, typed)
/// parameter. <see cref="Phase"/> gates it to one phase (null = every phase). The richer per-archetype
/// param bag (engine-spec §8) generalizes this later; M1 needs only one number.
/// </summary>
public sealed record MechanicInstance(
    string Id,
    MechanicArchetype Archetype,
    MechanicSchedule Schedule,
    int Amount = 0,
    int? Phase = null);

/// <summary>
/// An ordered encounter phase. Phase 0 is the opener (no trigger). Later phases advance when their
/// trigger is met — a tick reached or the boss dropping below an HP percentage.
/// </summary>
public sealed record PhaseDef(int Index, string Name, int? AtTick = null, int? HpBelowPct = null);
