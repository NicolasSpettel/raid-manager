using System.Collections.Generic;
using Engine;

namespace Content;

/// <summary>
/// The authored ability catalog (M1 starter set), grouped into the four class kits. Adding an ability
/// is a new row here — a data change, never a code change (Principle 0). Numbers live only here; the
/// engine and the generated tooltips both read them.
/// </summary>
public static class Abilities
{
    public static AbilityRegistry Registry { get; } = new(new List<AbilityRow>
    {
        // Guardian (tank)
        new AbilityRow(
            Id: "guardian.shield_slam", Name: "Shield Slam", ClassId: "guardian", SpecId: "protection",
            CastTicks: 0, GcdTicks: 15, CooldownTicks: 30, Amount: 30, Variance: 8, School: DamageSchool.Physical,
            Priority: 70, Tooltip: "Slams for {amount} {school} damage. {cooldownSeconds}s cooldown."),

        // Cleric (healer) — mend when someone's hurt, smite otherwise (emergent offensive healing)
        new AbilityRow(
            Id: "cleric.mend", Name: "Mend", ClassId: "cleric", SpecId: "holy",
            CastTicks: 10, GcdTicks: 10, CooldownTicks: 0, Amount: 100, Variance: 20, School: DamageSchool.Magic,
            Priority: 60, Tooltip: "Heals a friendly target for {amount} over a {castSeconds}s cast.",
            Kind: AbilityKind.Heal, ResourceCost: 120),
        new AbilityRow(
            Id: "cleric.smite", Name: "Smite", ClassId: "cleric", SpecId: "holy",
            CastTicks: 12, GcdTicks: 10, CooldownTicks: 0, Amount: 25, Variance: 8, School: DamageSchool.Magic,
            Priority: 30, Tooltip: "Smites an enemy for {amount} {school} damage after a {castSeconds}s cast.",
            ResourceCost: 60),

        // Blademaster (melee DPS)
        new AbilityRow(
            Id: "blademaster.mortal_strike", Name: "Mortal Strike", ClassId: "blademaster", SpecId: "arms",
            CastTicks: 0, GcdTicks: 15, CooldownTicks: 45, Amount: 45, Variance: 12, School: DamageSchool.Physical,
            Priority: 80, Tooltip: "A vicious strike for {amount} {school} damage. {cooldownSeconds}s cooldown."),

        // Pyromancer (caster DPS)
        new AbilityRow(
            Id: "pyromancer.fireball", Name: "Fireball", ClassId: "pyromancer", SpecId: "fire",
            CastTicks: 25, GcdTicks: 15, CooldownTicks: 0, Amount: 40, Variance: 15, School: DamageSchool.Magic,
            Priority: 50, Tooltip: "Hurls a fireball dealing {amount} {school} damage after a {castSeconds}s cast.",
            ResourceCost: 50),
        new AbilityRow(
            Id: "pyromancer.scorch", Name: "Scorch", ClassId: "pyromancer", SpecId: "fire",
            CastTicks: 0, GcdTicks: 15, CooldownTicks: 0, Amount: 15, Variance: 5, School: DamageSchool.Magic,
            Priority: 30, Tooltip: "Scorches an enemy for {amount} {school} damage.",
            ResourceCost: 30),
    });
}
