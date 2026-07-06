namespace Engine;

/// <summary>Damage type. Mitigation differs per school later; carried here from the start (engine-spec §6).</summary>
public enum DamageSchool
{
    Physical,
    Magic,
    True,
}

/// <summary>
/// What an ability does, as an engine-interpretable archetype. The generic effect runtime resolves
/// these — content never ships behavior code, only rows that reference an archetype (BLUEPRINT §6,
/// content-authoring). New archetype requires ≥2 users; otherwise a custom handler (added later).
/// </summary>
public abstract record AbilityEffect;

/// <summary>Deal <c>Amount + [0, Variance)</c> damage of <paramref name="School"/> to the target.</summary>
public sealed record DirectDamage(int Amount, int Variance, DamageSchool School) : AbilityEffect;

/// <summary>
/// The mechanical definition of an ability, as the engine executes it. Content authors richer rows
/// (name, tooltip, cost) and projects them to this via a factory — the engine stays Content-agnostic.
/// </summary>
public sealed record AbilityDef(
    AbilityId Id,
    int CastTicks,      // 0 = instant
    int GcdTicks,       // global cooldown started on use (must be ≥ 1)
    int CooldownTicks,  // 0 = no cooldown
    int Priority,       // higher = preferred by the DPS role policy
    AbilityEffect Effect);
