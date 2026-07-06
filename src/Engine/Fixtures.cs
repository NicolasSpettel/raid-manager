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

    /// <summary>
    /// One ranged caster versus a target dummy — exercises the cast/GCD/cooldown/priority system with
    /// an inline instant + an inline cast-time ability (built directly as engine <see cref="AbilityDef"/>s;
    /// the authored-in-Content version lives in Content's integration tests).
    /// </summary>
    public static SimInput Caster(ulong seed)
    {
        var zap = new AbilityDef(
            new AbilityId("fx.zap"), CastTicks: 0, GcdTicks: 15, CooldownTicks: 80, Priority: 90,
            new DirectDamage(Amount: 45, Variance: 0, DamageSchool.Magic));

        var bolt = new AbilityDef(
            new AbilityId("fx.bolt"), CastTicks: 20, GcdTicks: 15, CooldownTicks: 0, Priority: 50,
            new DirectDamage(Amount: 30, Variance: 10, DamageSchool.Magic));

        var caster = new CombatantSpec(
            new CombatantId("r:caster"), CombatantKind.Raider, Side.Raid, CombatantRole.Ranged, "Caster",
            new StatBlock(MaxHp: 500, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0),
            new[] { zap, bolt });

        var dummy = new CombatantSpec(
            new CombatantId("boss:dummy"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "Target Dummy",
            new StatBlock(MaxHp: 300, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        return new SimInput(
            new SeededRng(seed),
            SimConfig.Default,
            new RaidSetup(new[] { caster }),
            new EncounterDef("caster-dummy", "Caster Dummy", new[] { dummy }));
    }

    /// <summary>
    /// A small raid — tank + (optional) healer + melee DPS — versus a boss. The boss targets the first
    /// alive raider (spawn order), so the tank naturally holds it; the healer keeps the tank up off a
    /// mana pool. With the healer the raid wins; without, the tank dies first (a Wipe) — the whole point
    /// of step 3: management (fielding a healer) changes the outcome.
    /// </summary>
    public static SimInput Raid(ulong seed, bool withHealer = true)
    {
        var raiders = new List<CombatantSpec>
        {
            // Tank first → the boss tanks on it (spawn-order threat until real threat lands).
            new(new CombatantId("r:tank"), CombatantKind.Raider, Side.Raid, CombatantRole.Tank, "Tank",
                new StatBlock(MaxHp: 800, AttackDamage: 4, AttackVariance: 2, SwingIntervalTicks: 8)),
        };

        if (withHealer)
        {
            var mend = new AbilityDef(
                new AbilityId("heal.mend"), CastTicks: 10, GcdTicks: 10, CooldownTicks: 0, Priority: 50,
                new DirectHeal(Amount: 100, Variance: 20), ResourceCost: 120);

            raiders.Add(new CombatantSpec(
                new CombatantId("r:healer"), CombatantKind.Raider, Side.Raid, CombatantRole.Healer, "Healer",
                new StatBlock(MaxHp: 400, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
                    MaxResource: 1000, ResourceRegenPerTick: 3),
                new[] { mend }));
        }

        raiders.Add(new CombatantSpec(
            new CombatantId("r:dps"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "DPS",
            new StatBlock(MaxHp: 400, AttackDamage: 15, AttackVariance: 5, SwingIntervalTicks: 4)));

        var boss = new CombatantSpec(
            new CombatantId("boss:main"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "The Warden",
            new StatBlock(MaxHp: 1200, AttackDamage: 40, AttackVariance: 10, SwingIntervalTicks: 8));

        return new SimInput(
            new SeededRng(seed),
            SimConfig.Default,
            new RaidSetup(raiders),
            new EncounterDef("warden-raid", "The Warden", new[] { boss }));
    }

    /// <summary>
    /// A full mechanics fight: a 4-raider raid versus a two-phase boss whose timeline runs spread
    /// damage, tank busters, an extra spread in the frenzy phase (HP &lt; 50%), and a soft enrage.
    /// Exercises the whole generic mechanic runtime — and proves a boss is authored as data.
    /// </summary>
    public static SimInput Warden(ulong seed)
    {
        var mend = new AbilityDef(
            new AbilityId("heal.mend"), CastTicks: 8, GcdTicks: 7, CooldownTicks: 0, Priority: 50,
            new DirectHeal(Amount: 110, Variance: 20), ResourceCost: 100);

        var raiders = new List<CombatantSpec>
        {
            new(new CombatantId("r:tank"), CombatantKind.Raider, Side.Raid, CombatantRole.Tank, "Tank",
                new StatBlock(MaxHp: 900, AttackDamage: 5, AttackVariance: 2, SwingIntervalTicks: 8)),
            new(new CombatantId("r:healer"), CombatantKind.Raider, Side.Raid, CombatantRole.Healer, "Healer",
                new StatBlock(MaxHp: 400, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
                    MaxResource: 1200, ResourceRegenPerTick: 4),
                new[] { mend }),
            new(new CombatantId("r:dps1"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "Brawler",
                new StatBlock(MaxHp: 400, AttackDamage: 18, AttackVariance: 6, SwingIntervalTicks: 4)),
            new(new CombatantId("r:dps2"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "Rogueish",
                new StatBlock(MaxHp: 400, AttackDamage: 18, AttackVariance: 6, SwingIntervalTicks: 4)),
        };

        var boss = new CombatantSpec(
            new CombatantId("boss:main"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "The Warden",
            new StatBlock(MaxHp: 1200, AttackDamage: 25, AttackVariance: 10, SwingIntervalTicks: 8));

        var phases = new List<PhaseDef>
        {
            new(0, "Opening"),
            new(1, "Frenzy", HpBelowPct: 50),
        };

        var timeline = new List<MechanicInstance>
        {
            new("warden.spread", MechanicArchetype.SpreadDamage, MechanicSchedule.Repeating(40, 40), Amount: 25),
            new("warden.buster", MechanicArchetype.TankBuster, MechanicSchedule.Repeating(30, 30), Amount: 80),
            new("warden.frenzy_spread", MechanicArchetype.SpreadDamage, MechanicSchedule.Repeating(10, 25), Amount: 20, Phase: 1),
            new("warden.enrage", MechanicArchetype.Enrage, MechanicSchedule.Once(400), Amount: 50),
        };

        return new SimInput(
            new SeededRng(seed),
            SimConfig.Default,
            new RaidSetup(raiders),
            new EncounterDef("warden", "The Warden", new[] { boss }, phases, timeline));
    }

    /// <summary>Resolve a fixture by name for the Sim CLI. Returns null for an unknown name.</summary>
    public static SimInput? ByName(string name, ulong seed) => name switch
    {
        "dummy" => Dummy(seed),
        "trio" => Trio(seed),
        "caster" => Caster(seed),
        "raid" => Raid(seed),
        "warden" => Warden(seed),
        _ => null,
    };

    private static CombatantSpec Melee(string id, string name, int maxHp) => new(
        new CombatantId(id), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, name,
        new StatBlock(MaxHp: maxHp, AttackDamage: 6, AttackVariance: 3, SwingIntervalTicks: 6));
}
