using Content;
using Engine;
using Xunit;

namespace Content.Tests;

/// <summary>
/// The "one new file" definition of done: every authored encounter runs through the SAME engine and
/// fires its mechanics with zero engine code per boss. "Sentinel" sitting beside "Warden" is the proof —
/// it was one new row (BLUEPRINT §11, content-authoring).
/// </summary>
public class EncounterCatalogTests
{
    [Fact]
    public void Catalog_IsNotEmpty()
    {
        Assert.NotEmpty(Encounters.All);
    }

    [Fact]
    public void EveryEncounter_RunsThroughTheEngine_AndFiresMechanics()
    {
        foreach (EncounterDef encounter in Encounters.All)
        {
            SimResult result = Simulator.SimulateEncounter(new SimInput(
                new SeededRng(1), SimConfig.Default, StandardRaid(), encounter));

            Assert.Contains(result.Events, e => e is MechanicEvent);
        }
    }

    private static RaidSetup StandardRaid() => new(new[]
    {
        new CombatantSpec(new CombatantId("r:tank"), CombatantKind.Raider, Side.Raid, CombatantRole.Tank, "Tank",
            new StatBlock(MaxHp: 900, AttackDamage: 6, AttackVariance: 2, SwingIntervalTicks: 8)),
        new CombatantSpec(new CombatantId("r:healer"), CombatantKind.Raider, Side.Raid, CombatantRole.Healer, "Healer",
            new StatBlock(MaxHp: 400, AttackDamage: 0, AttackVariance: 0, SwingIntervalTicks: 0,
                MaxResource: 1200, ResourceRegenPerTick: 4),
            new[] { Abilities.Registry.Def("cleric.mend") }),
        new CombatantSpec(new CombatantId("r:dps"), CombatantKind.Raider, Side.Raid, CombatantRole.Melee, "DPS",
            new StatBlock(MaxHp: 400, AttackDamage: 18, AttackVariance: 6, SwingIntervalTicks: 4)),
    });
}
