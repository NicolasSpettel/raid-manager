using System.Collections.Generic;
using Engine;

namespace Content;

/// <summary>
/// The authored ability catalog (M1 starter set). Adding an ability is a new row here — a data change,
/// never a code change (Principle 0). Numbers live only here; the engine and tooltips both read them.
/// </summary>
public static class Abilities
{
    public static AbilityRegistry Registry { get; } = new(new List<AbilityRow>
    {
        new AbilityRow(
            Id: "warrior.mortal_strike", Name: "Mortal Strike", ClassId: "warrior", SpecId: "arms",
            CastTicks: 0, GcdTicks: 15, CooldownTicks: 60, Amount: 40, Variance: 10, School: DamageSchool.Physical,
            Priority: 80, Tooltip: "A vicious strike for {amount} {school} damage. {cooldownSeconds}s cooldown."),

        new AbilityRow(
            Id: "mage.fireball", Name: "Fireball", ClassId: "mage", SpecId: "fire",
            CastTicks: 25, GcdTicks: 15, CooldownTicks: 0, Amount: 35, Variance: 15, School: DamageSchool.Magic,
            Priority: 50, Tooltip: "Hurls a fireball dealing {amount} {school} damage after a {castSeconds}s cast."),
    });
}
