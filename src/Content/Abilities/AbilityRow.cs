using System.Collections.Generic;
using System.Globalization;
using Engine;

namespace Content;

/// <summary>Which effect archetype a row authors.</summary>
public enum AbilityKind
{
    Damage,
    Heal,
}

/// <summary>
/// An authored ability template — one row = one fact. It owns the numbers, the effect archetype, the
/// AI priority, and a tooltip template that interpolates its OWN fields, so the tooltip and the engine
/// can never drift (DM1's #1 bug class). Projects to an engine <see cref="AbilityDef"/> via
/// <see cref="ToDef"/> — the factory that returns a fully-valid instance (content-authoring, BLUEPRINT §10).
/// </summary>
public sealed record AbilityRow(
    string Id,
    string Name,
    string ClassId,
    string SpecId,
    int CastTicks,
    int GcdTicks,
    int CooldownTicks,
    int Amount,
    int Variance,
    DamageSchool School,
    int Priority,
    string Tooltip,
    AbilityKind Kind = AbilityKind.Damage,
    int ResourceCost = 0)
{
    /// <summary>The factory: a fully-valid engine ability from this row.</summary>
    public AbilityDef ToDef() => new(
        new AbilityId(Id),
        CastTicks,
        GcdTicks,
        CooldownTicks,
        Priority,
        Kind == AbilityKind.Heal ? new DirectHeal(Amount, Variance) : new DirectDamage(Amount, Variance, School),
        ResourceCost);

    /// <summary>The fields a tooltip may reference — the single source of truth is this row's own data.</summary>
    public IReadOnlyDictionary<string, string> TooltipFields() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["name"] = Name,
        ["amount"] = Amount.ToString(CultureInfo.InvariantCulture),
        ["variance"] = Variance.ToString(CultureInfo.InvariantCulture),
        ["school"] = SchoolName(School),
        ["castSeconds"] = Seconds(CastTicks),
        ["cooldownSeconds"] = Seconds(CooldownTicks),
    };

    private static string Seconds(int ticks) =>
        (ticks / (double)TimeModel.TicksPerSecond).ToString("0.#", CultureInfo.InvariantCulture);

    private static string SchoolName(DamageSchool school) => school switch
    {
        DamageSchool.Physical => "physical",
        DamageSchool.Magic => "magic",
        DamageSchool.True => "true",
        _ => school.ToString(),
    };
}
