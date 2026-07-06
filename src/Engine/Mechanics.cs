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

    /// <summary>Applies a damage-over-time aura (<c>Amount</c> damage per second) to every raider.</summary>
    RaidDot,

    /// <summary>Stacks a debuff on the boss's current target that adds <c>+Amount%</c> damage taken per stack.</summary>
    TankDebuff,

    /// <summary>The boss casts a dangerous spell; a raider with a ready interrupt stops it, else it lands (<c>Amount</c> raid-wide).</summary>
    InterruptibleCast,

    /// <summary>Drops a ground hazard under a raider: <c>Amount</c> damage per tick to anyone still inside its <c>Radius</c>. Run out.</summary>
    VoidZone,
}

/// <summary>When a mechanic fires: once at a tick, or repeating from a start tick.</summary>
public sealed record MechanicSchedule(int? AtTick = null, int StartTick = 0, int? EveryTicks = null, int? Count = null)
{
    public static MechanicSchedule Once(int atTick) => new(AtTick: atTick);

    public static MechanicSchedule Repeating(int startTick, int everyTicks, int? count = null) =>
        new(StartTick: startTick, EveryTicks: everyTicks, Count: count);
}

/// <summary>
/// One entry on an encounter's timeline: a mechanic archetype, its schedule, and its parameters.
/// <see cref="Amount"/> is the one number every archetype uses (damage/percent). The spatial trio
/// (<see cref="Radius"/>, <see cref="DurationTicks"/>, <see cref="TickIntervalTicks"/>) is read only by
/// geometry archetypes like <see cref="MechanicArchetype.VoidZone"/> and defaults to 0 for the rest, so
/// existing rows are unchanged. <see cref="Phase"/> gates it to one phase (null = every phase).
/// </summary>
public sealed record MechanicInstance(
    string Id,
    MechanicArchetype Archetype,
    MechanicSchedule Schedule,
    int Amount = 0,
    int? Phase = null,
    int Radius = 0,
    int DurationTicks = 0,
    int TickIntervalTicks = 0);

/// <summary>
/// An ordered encounter phase. Phase 0 is the opener (no trigger). Later phases advance when their
/// trigger is met — a tick reached or the boss dropping below an HP percentage.
/// </summary>
public sealed record PhaseDef(int Index, string Name, int? AtTick = null, int? HpBelowPct = null);
