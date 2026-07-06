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
/// Immutable input stats (gear/traits already folded by the game layer). Deliberately tiny in M1
/// step 1 — just what auto-attacks need; it grows as casts, resources, and mitigation land.
/// </summary>
public sealed record StatBlock(int MaxHp, int AttackDamage, int AttackVariance, int SwingIntervalTicks);

/// <summary>
/// The immutable definition of one combatant, as authored/generated outside the engine. The engine
/// spawns a mutable runtime combatant from this, so a fixture can be simulated repeatedly without
/// state leaking between runs (determinism).
/// </summary>
public sealed record CombatantSpec(
    CombatantId Id,
    CombatantKind Kind,
    Side Side,
    CombatantRole Role,
    string Name,
    StatBlock Stats);

/// <summary>Mutable per-encounter runtime state for one combatant. Engine-internal.</summary>
internal sealed class Combatant
{
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
}
