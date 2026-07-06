using Content;
using Engine;
using Xunit;

namespace Content.Tests;

/// <summary>
/// The four M1 classes are pure data driving the existing engine — no per-class code. These prove the
/// kits resolve, the factory builds a valid raider, and a full class raid actually fights (BLUEPRINT §11).
/// </summary>
public class ClassTests
{
    [Fact]
    public void EveryClass_HasAResolvableKit()
    {
        foreach (ClassDef cls in Classes.Registry.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(cls.Name), $"{cls.Id}: missing Name");
            Assert.NotEmpty(cls.Kit);
            foreach (string abilityId in cls.Kit)
            {
                // Throws (failing the test with the id) if the kit references a missing ability row.
                Assert.Equal(abilityId, Abilities.Registry.Def(abilityId).Id.Value);
            }
        }
    }

    [Fact]
    public void CreateRaider_BuildsASpecFromTheClass()
    {
        CombatantSpec spec = Roster.CreateRaider(Classes.Pyromancer, "r:p", "Pyra");

        Assert.Equal(CombatantRole.Ranged, spec.Role);
        Assert.NotNull(spec.Abilities);
        Assert.Equal(Classes.Pyromancer.Kit.Count, spec.Abilities!.Count);
    }

    [Fact]
    public void ClassRaid_ClearsAPassiveDummy_UsingEveryKit()
    {
        var raid = new RaidSetup(new[]
        {
            Roster.CreateRaider(Classes.Guardian, "r:tank", "Tank"),
            Roster.CreateRaider(Classes.Cleric, "r:healer", "Healer"),
            Roster.CreateRaider(Classes.Blademaster, "r:melee", "Melee"),
            Roster.CreateRaider(Classes.Pyromancer, "r:caster", "Caster"),
        });

        var dummy = new CombatantSpec(
            new CombatantId("boss:dummy"), CombatantKind.Boss, Side.Enemy, CombatantRole.Tank, "Dummy",
            new StatBlock(MaxHp: 1500, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0));

        SimResult result = Simulator.SimulateEncounter(new SimInput(
            new SeededRng(1), SimConfig.Default, raid,
            new EncounterDef("dummy", "Dummy", new[] { dummy })));

        Assert.Equal(EncounterOutcome.Kill, result.Outcome);
        Assert.Contains(result.Events, e => e is Damage d && d.Ability?.Value == "guardian.shield_slam");
        Assert.Contains(result.Events, e => e is Damage d && d.Ability?.Value == "blademaster.mortal_strike");
        Assert.Contains(result.Events, e => e is Damage d && d.Ability?.Value == "pyromancer.fireball");
        // Nobody takes damage from a passive dummy, so the Cleric smites instead of mending (offensive healing).
        Assert.Contains(result.Events, e => e is Damage d && d.Ability?.Value == "cleric.smite");
    }
}
