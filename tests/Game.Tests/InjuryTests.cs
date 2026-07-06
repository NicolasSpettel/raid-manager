using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// Injuries: a fallen raider fights weaker until they recover, so overreaching on hard content has a
/// lasting cost and roster depth matters. Everyone still fights (no benching), so there are no dead ends.
/// </summary>
public class InjuryTests
{
    [Fact]
    public void InjuredRaider_FightsAtReducedStats()
    {
        var healthy = new RaiderRecord("r:1", "Tank", "guardian", Equipped: new List<string>());
        RaiderRecord injured = healthy with { InjuryRaidsLeft = 1 };

        Assert.True(Warband.ToCombatant(injured).Stats.MaxHp < Warband.ToCombatant(healthy).Stats.MaxHp);
    }

    [Fact]
    public void DeathInARaid_CausesInjury()
    {
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z");
        EncounterDef mythic = Difficulties.Scale(Encounters.AshenKing, Difficulty.Mythic);

        SimResult wipe = Fight(guild, mythic, seed: 1);
        Assert.Equal(EncounterOutcome.Wipe, wipe.Outcome);

        (GuildSave after, _) = RaidResolver.Resolve(guild, wipe, mythic, lootSeed: 1);
        Assert.Contains(after.Roster, r => r.InjuryRaidsLeft > 0);
    }

    [Fact]
    public void Injuries_RecoverWhenTheRaiderSurvives()
    {
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z");
        var roster = guild.Roster.ToList();
        roster[2] = roster[2] with { InjuryRaidsLeft = 2 }; // a DPS the boss won't target
        guild = guild with { Roster = roster };

        SimResult win = Fight(guild, Encounters.Warden, seed: 1);
        Assert.Equal(EncounterOutcome.Kill, win.Outcome);

        (GuildSave after, _) = RaidResolver.Resolve(guild, win, Encounters.Warden, lootSeed: 1);
        RaiderRecord dps = after.Roster.First(r => r.Id == roster[2].Id);
        Assert.True(dps.InjuryRaidsLeft < 2, $"expected recovery, got {dps.InjuryRaidsLeft}");
    }

    private static SimResult Fight(GuildSave guild, EncounterDef encounter, ulong seed)
    {
        var raid = new RaidSetup(guild.Roster.Select(Warband.ToCombatant).ToList());
        return Simulator.SimulateEncounter(new SimInput(new SeededRng(seed), SimConfig.Default, raid, encounter));
    }
}
