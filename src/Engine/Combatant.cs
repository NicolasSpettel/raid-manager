using System.Collections.Generic;

namespace Engine;

/// <summary>Which team a combatant fights for.</summary>
public enum Side
{
    Raid,
    Enemy,
}

/// <summary>What a combatant is. Behavior differs; the type does not — N is a value, not a schema.</summary>
public enum CombatantKind
{
    Raider,
    Boss,
    Add,
    Pet,
}

/// <summary>Authored for raiders, informative for enemies.</summary>
public enum CombatantRole
{
    Tank,
    Healer,
    Melee,
    Ranged,
}

/// <summary>
/// Immutable input stats (gear/traits already folded by the game layer). Deliberately tiny — just
/// what auto-attacks need; it grows as resources and mitigation land.
/// </summary>
public sealed record StatBlock(int MaxHp, int AttackDamage, int AttackVariance, int SwingIntervalTicks);

/// <summary>
/// The immutable definition of one combatant, as authored/generated outside the engine. The engine
/// spawns a mutable runtime combatant from this, so a fixture can be simulated repeatedly without
/// state leaking between runs (determinism). <see cref="Abilities"/> is null for pure auto-attackers.
/// </summary>
public sealed record CombatantSpec(
    CombatantId Id,
    CombatantKind Kind,
    Side Side,
    CombatantRole Role,
    string Name,
    StatBlock Stats,
    IReadOnlyList<AbilityDef>? Abilities = null);

/// <summary>Mutable per-encounter runtime state for one combatant. Engine-internal.</summary>
internal sealed class Combatant
{
    private readonly Dictionary<AbilityId, int> _cooldownReadyAt = new();

    public Combatant(CombatantSpec spec)
    {
        Spec = spec;
        Hp = spec.Stats.MaxHp;
    }

    public CombatantSpec Spec { get; }

    public CombatantId Id => Spec.Id;

    public Side Side => Spec.Side;

    public int Hp { get; set; }

    public bool IsAlive => Hp > 0;

    public IReadOnlyList<AbilityDef> Abilities => Spec.Abilities ?? System.Array.Empty<AbilityDef>();

    /// <summary>Tick at which the global cooldown frees up (the next Decide may act at or after this).</summary>
    public int GcdReadyAt { get; set; }

    /// <summary>Set while a cast-time ability is in flight; null otherwise.</summary>
    public int? CastingUntilTick { get; set; }

    public int CooldownReadyAt(AbilityId id) => _cooldownReadyAt.TryGetValue(id, out int t) ? t : 0;

    public void SetCooldownReadyAt(AbilityId id, int tick) => _cooldownReadyAt[id] = tick;
}
