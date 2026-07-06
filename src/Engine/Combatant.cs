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
/// Immutable input stats (gear/traits already folded by the game layer). Grows per M1 step; resources
/// are 0 for combatants that don't spend (most melee/tanks in M1).
/// </summary>
public sealed record StatBlock(
    int MaxHp,
    int AttackDamage,
    int AttackVariance,
    int SwingIntervalTicks,
    int MaxResource = 0,
    int ResourceRegenPerTick = 0);

/// <summary>
/// How well a raider executes — the link where management (attributes/traits/morale) becomes combat
/// outcome (engine-spec §9). v0 is a fixed reaction delay; the game layer will derive it from
/// attributes later. A crisp raider (0) acts on the GCD; a sloppy one loses uptime every action.
/// </summary>
public sealed record ExecutionProfile(int ReactionTicks);

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
    IReadOnlyList<AbilityDef>? Abilities = null,
    ExecutionProfile? Execution = null);

/// <summary>Mutable per-encounter runtime state for one combatant. Engine-internal.</summary>
internal sealed class Combatant
{
    private readonly Dictionary<AbilityId, int> _cooldownReadyAt = new();
    private readonly Dictionary<string, AuraInstance> _auras = new();

    public Combatant(CombatantSpec spec)
    {
        Spec = spec;
        Hp = spec.Stats.MaxHp;
        Resource = spec.Stats.MaxResource;
    }

    public CombatantSpec Spec { get; }

    public CombatantId Id => Spec.Id;

    public Side Side => Spec.Side;

    public int Hp { get; set; }

    public int MaxHp => Spec.Stats.MaxHp;

    public bool IsAlive => Hp > 0;

    /// <summary>Percent multiplier on damage this combatant deals (100 = normal). Raised by Enrage.</summary>
    public int DamageDealtMultPct { get; set; } = 100;

    /// <summary>Accumulated threat. An enemy targets the highest-threat raider (tanking).</summary>
    public int Threat { get; set; }

    /// <summary>Threat generated per point of damage/healing — tanks generate far more, so they hold aggro.</summary>
    public int ThreatMult => Spec.Role == CombatantRole.Tank ? 4 : 1;

    /// <summary>Percent multiplier on damage this combatant takes (100 = normal). Raised by debuff stacks.</summary>
    public int DamageTakenMultPct
    {
        get
        {
            int pct = 100;
            foreach (AuraInstance aura in _auras.Values)
            {
                pct += aura.Stacks * aura.Def.DamageTakenBonusPctPerStack;
            }

            return pct;
        }
    }

    /// <summary>Apply or refresh an aura, returning the live instance (with its current stack count).</summary>
    public AuraInstance ApplyAura(AuraDef def, int tick)
    {
        if (_auras.TryGetValue(def.Id, out AuraInstance? existing))
        {
            existing.Stacks = System.Math.Min(def.MaxStacks, existing.Stacks + 1);
            existing.ExpiresAtTick = tick + def.DurationTicks;
            return existing;
        }

        var instance = new AuraInstance(def, stacks: 1, expiresAtTick: tick + def.DurationTicks);
        _auras[def.Id] = instance;
        return instance;
    }

    public AuraInstance? GetAura(string id) => _auras.TryGetValue(id, out AuraInstance? a) ? a : null;

    public void RemoveAura(string id) => _auras.Remove(id);

    /// <summary>Current spendable resource (mana). Regenerated lazily at decision points.</summary>
    public int Resource { get; set; }

    /// <summary>Tick at which resource was last regenerated (lazy regen bookkeeping).</summary>
    public int LastResourceTick { get; set; }

    public int ReactionTicks => Spec.Execution?.ReactionTicks ?? 0;

    public IReadOnlyList<AbilityDef> Abilities => Spec.Abilities ?? System.Array.Empty<AbilityDef>();

    /// <summary>Tick at which the global cooldown frees up (the next decision may act at or after this).</summary>
    public int GcdReadyAt { get; set; }

    /// <summary>Set while a cast-time ability is in flight; null otherwise.</summary>
    public int? CastingUntilTick { get; set; }

    public int CooldownReadyAt(AbilityId id) => _cooldownReadyAt.TryGetValue(id, out int t) ? t : 0;

    public void SetCooldownReadyAt(AbilityId id, int tick) => _cooldownReadyAt[id] = tick;

    /// <summary>Spend a ready interrupt ability (putting it on cooldown) if this combatant has one. Reactive.</summary>
    public bool TryUseInterrupt(int tick)
    {
        foreach (AbilityDef ability in Abilities)
        {
            if (ability.Effect is InterruptEffect && tick >= CooldownReadyAt(ability.Id))
            {
                SetCooldownReadyAt(ability.Id, tick + ability.CooldownTicks);
                return true;
            }
        }

        return false;
    }

    /// <summary>Spend a ready taunt ability if this combatant has one. Reactive.</summary>
    public bool TryUseTaunt(int tick)
    {
        foreach (AbilityDef ability in Abilities)
        {
            if (ability.Effect is TauntEffect && tick >= CooldownReadyAt(ability.Id))
            {
                SetCooldownReadyAt(ability.Id, tick + ability.CooldownTicks);
                return true;
            }
        }

        return false;
    }
}
