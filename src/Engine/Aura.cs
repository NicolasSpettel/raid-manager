namespace Engine;

/// <summary>
/// A timed effect on a combatant — a damage-over-time and/or a stat modifier (engine-spec §4). Applied
/// by boss mechanics (and later, abilities); it ticks and expires on schedule. One <c>AuraDef</c>, many
/// live instances. A reapplication refreshes the duration and adds a stack up to <see cref="MaxStacks"/>.
/// </summary>
public sealed record AuraDef(
    string Id,
    int DurationTicks,
    int TickIntervalTicks = 0,          // 0 = no periodic damage
    int DamagePerTick = 0,
    int DamageTakenBonusPctPerStack = 0,
    int MaxStacks = 1);

/// <summary>A live aura on one combatant. Engine-internal runtime state.</summary>
internal sealed class AuraInstance
{
    public AuraInstance(AuraDef def, int stacks, int expiresAtTick)
    {
        Def = def;
        Stacks = stacks;
        ExpiresAtTick = expiresAtTick;
    }

    public AuraDef Def { get; }

    public int Stacks { get; set; }

    public int ExpiresAtTick { get; set; }
}
