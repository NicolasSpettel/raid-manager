using System.Collections.Generic;
using System.Linq;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// Recruitment paths (GDD §8b/§10): the youth academy's intake (promote a prospect) and the living-world
/// transfer market (sign a free agent). Both spend gold, grow the roster, and no-op cleanly when broke.
/// </summary>
public class RecruitmentTests
{
    // ── Youth program (GDD §10) ──────────────────────────────────────────────────────────────────────
    [Fact]
    public void Intake_IsDeterministic_AndYoung()
    {
        IReadOnlyList<RaiderRecord> a = YouthProgram.Intake(worldSeed: 7, seasonNumber: 1, guildId: "guild_050");
        IReadOnlyList<RaiderRecord> b = YouthProgram.Intake(worldSeed: 7, seasonNumber: 1, guildId: "guild_050");

        Assert.Equal(YouthProgram.IntakeSize, a.Count);
        Assert.Equal(a.Select(p => p.Id), b.Select(p => p.Id)); // same seed → same faces
        foreach (RaiderRecord p in a)
        {
            Assert.Equal("youth", p.ArchetypeId);
            int age = Aging.AgeOf(p, seasonNumber: 1);
            Assert.InRange(age, 16, 18);
        }
    }

    [Fact]
    public void Promote_MovesProspectToRoster_AndSpendsGold()
    {
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z"); // 1000 gold
        IReadOnlyList<RaiderRecord> intake = YouthProgram.Intake(1, 1, "guild_050");
        guild = guild with { YouthProspects = intake };
        string id = intake[0].Id;

        bool promoted = YouthProgram.TryPromote(guild, id, out GuildSave updated);

        Assert.True(promoted);
        Assert.Equal(guild.Roster.Count + 1, updated.Roster.Count);
        Assert.Contains(updated.Roster, r => r.Id == id);
        Assert.DoesNotContain(updated.YouthProspects!, p => p.Id == id); // left the pool
        Assert.Equal(guild.Economy.Gold - YouthProgram.PromoteCost, updated.Economy.Gold);
    }

    [Fact]
    public void Promote_Broke_IsNoOp()
    {
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z") with { Economy = new Economy(50) };
        IReadOnlyList<RaiderRecord> intake = YouthProgram.Intake(1, 1, "guild_050");
        guild = guild with { YouthProspects = intake };

        bool promoted = YouthProgram.TryPromote(guild, intake[0].Id, out GuildSave updated);

        Assert.False(promoted);
        Assert.Same(guild, updated);
    }

    // ── Transfer market (GDD §8b) ────────────────────────────────────────────────────────────────────
    [Fact]
    public void FreeAgents_ArePriced_SortedBest_AndExcludeYourRoster()
    {
        World world = WorldGen.Generate(1);
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z");

        IReadOnlyList<TransferListing> agents = TransferMarket.FreeAgents(world, guild);

        Assert.NotEmpty(agents);
        Assert.All(agents, a => Assert.True(a.Fee > 0));
        // best-first ordering
        for (int i = 1; i < agents.Count; i++)
        {
            Assert.True(Ratings.Best(agents[i - 1].Raider).HalfStars >= Ratings.Best(agents[i].Raider).HalfStars);
        }

        // A free agent already on your roster is not offered.
        RaiderRecord onRoster = agents[0].Raider;
        GuildSave withThem = guild with { Roster = guild.Roster.Append(onRoster).ToList() };
        Assert.DoesNotContain(TransferMarket.FreeAgents(world, withThem), a => a.Raider.Id == onRoster.Id);
    }

    [Fact]
    public void Sign_AddsAgent_AndSpendsFee()
    {
        World world = WorldGen.Generate(1);
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z") with { Economy = new Economy(10000) };
        RaiderRecord agent = TransferMarket.FreeAgents(world, guild)[0].Raider;
        int fee = TransferMarket.Fee(agent);

        bool signed = TransferMarket.TrySign(guild, agent, out GuildSave updated);

        Assert.True(signed);
        Assert.Contains(updated.Roster, r => r.Id == agent.Id);
        Assert.Equal(guild.Economy.Gold - fee, updated.Economy.Gold);
    }

    [Fact]
    public void Sign_Broke_IsNoOp()
    {
        World world = WorldGen.Generate(1);
        GuildSave guild = Guilds.CreateStarter("T", 1, "2026-01-01T00:00:00Z") with { Economy = new Economy(10) };
        RaiderRecord agent = TransferMarket.FreeAgents(world, guild)[0].Raider;

        bool signed = TransferMarket.TrySign(guild, agent, out GuildSave updated);

        Assert.False(signed);
        Assert.Same(guild, updated);
    }
}
