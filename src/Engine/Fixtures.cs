using System.Collections.Generic;

namespace Engine;

/// <summary>
/// M1 combat fixtures — hand-built test encounters (not shipped content). One place builds each, so
/// the Sim CLI, the Godot App, and the golden tests all simulate the exact same thing. Real,
/// data-authored encounters replace these at M1 step 4 (engine-spec §8).
/// </summary>
public static class Fixtures
{
    /// <summary>One melee raider beating on a target dummy that never fights back (the M0 fight, in the new model).</summary>
    public static SimInput Dummy(ulong seed)
    {
        var attacker = new CombatantSpec(
            new CombatantId("r:attacker"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "Attacker",
            new StatBlock(MaxHp: 1000, AttackDamage: 8, AttackVariance: 5, SwingIntervalTicks: 5));

        var dummy = new CombatantSpec(
            new CombatantId("boss:dummy"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "Target Dummy",
            new StatBlock(MaxHp: 100, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        return new SimInput(
            new SeededRng(seed),
            SimConfig.Default,
            new RaidSetup(new[] { attacker }),
            new EncounterDef("dummy", "Target Dummy", new[] { dummy }));
    }

    /// <summary>Three melee raiders versus a boss that hits back — exercises both sides and a possible death.</summary>
    public static SimInput Trio(ulong seed)
    {
        var raiders = new List<CombatantSpec>
        {
            Melee("r:tanky", "Tanky", maxHp: 400),
            Melee("r:dps1", "Brawler", maxHp: 200),
            Melee("r:dps2", "Rogueish", maxHp: 200),
        };

        var boss = new CombatantSpec(
            new CombatantId("boss:main"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "The Warden",
            new StatBlock(MaxHp: 400, AttackDamage: 20, AttackVariance: 10, SwingIntervalTicks: 8));

        return new SimInput(
            new SeededRng(seed),
            SimConfig.Default,
            new RaidSetup(raiders),
            new EncounterDef("warden", "The Warden", new[] { boss }));
    }

    /// <summary>Resolve a fixture by name for the Sim CLI. Returns null for an unknown name.</summary>
    public static SimInput? ByName(string name, ulong seed) => name switch
    {
        "dummy" => Dummy(seed),
        "trio" => Trio(seed),
        _ => null,
    };

    private static CombatantSpec Melee(string id, string name, int maxHp) => new(
        new CombatantId(id), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, name,
        new StatBlock(MaxHp: maxHp, AttackDamage: 6, AttackVariance: 3, SwingIntervalTicks: 6));
}
