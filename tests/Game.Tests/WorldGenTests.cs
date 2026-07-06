using System.Linq;
using Content;
using Engine;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// World generation (entities-and-worldgen §8): a seed produces a fixed, reproducible world; the
/// latent-factor pipeline yields coherent people (prestige gradient, role coverage, an age curve with no
/// twitchy veterans). These are the determinism + distribution + coherence guards the spec calls for.
/// </summary>
public class WorldGenTests
{
    // Blessed from `dotnet run --project src/Sim -- world --seed 1`.
    private const string Seed1Hash = "7fbe0232d3e10a8a";

    [Fact]
    public void World_Seed1_MatchesBlessedHash()
    {
        World world = WorldGen.Generate(1);
        Assert.Equal(Seed1Hash, WorldText.Hash(world));
    }

    [Fact]
    public void Generation_IsDeterministic()
    {
        Assert.Equal(WorldText.Hash(WorldGen.Generate(7)), WorldText.Hash(WorldGen.Generate(7)));
    }

    [Fact]
    public void World_HasRoughlyTheTargetSize()
    {
        World world = WorldGen.Generate(1);
        Assert.Equal(200, world.Guilds.Count);
        Assert.Equal(400, world.FreeAgents.Count);
        Assert.InRange(world.Raiders.Count, 6000, 6800); // ~6,500 characters (§1)
    }

    [Fact]
    public void EveryGuild_HasRaidRoleCoverage()
    {
        World world = WorldGen.Generate(1);
        foreach (Guild guild in world.Guilds)
        {
            var roles = guild.Roster
                .Select(id => Classes.Registry.Get(world.Get(id).ClassId).Role)
                .ToList();

            Assert.True(roles.Count(r => r == CombatantRole.Tank) >= 2, $"{guild.Id.Value} needs at least two tanks");
            Assert.Contains(CombatantRole.Healer, roles);
            Assert.Contains(CombatantRole.Melee, roles);
            Assert.Contains(CombatantRole.Ranged, roles);
        }
    }

    [Fact]
    public void Prestige_ProducesATalentGradient()
    {
        World world = WorldGen.Generate(1);

        double AvgStars(PrestigeTier tier) => world.Guilds
            .Where(g => g.Tier == tier)
            .SelectMany(g => g.Roster)
            .Select(world.Get)
            .Average(r => Ratings.Best(r).HalfStars);

        Assert.True(AvgStars(PrestigeTier.WorldElite) > AvgStars(PrestigeTier.Local),
            "elite guilds should field stronger raiders than local ones");
    }

    [Fact]
    public void ArchetypeSpread_IsNotDegenerate()
    {
        World world = WorldGen.Generate(1);
        int distinct = world.Raiders.Values.Select(r => r.ArchetypeId).Distinct().Count();
        Assert.Equal(Archetypes.Registry.All.Count, distinct); // every archetype appears somewhere
    }

    [Fact]
    public void Aging_IsCoherent_NoTwitchyVeterans()
    {
        World world = WorldGen.Generate(1);
        Assert.All(world.Raiders.Values, r =>
        {
            int age = world.AgeOf(r);
            Assert.InRange(age, 16, 33); // football-style band (GDD §8)
            if (age >= 30)
            {
                Assert.True(r.Attributes!.Of("mechanics") < 18, "a 30+ raider shouldn't keep elite twitch");
            }
        });
    }

    [Fact]
    public void FreeAgents_HaveNoGuild()
    {
        World world = WorldGen.Generate(1);
        Assert.NotEmpty(world.FreeAgents);
        Assert.All(world.FreeAgents, id => Assert.Null(world.Get(id).Membership));
    }
}
