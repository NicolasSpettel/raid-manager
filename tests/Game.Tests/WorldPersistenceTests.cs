using System.Collections.Generic;
using System.Linq;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// World + season persistence (ADR-0007): a career pins its world by <b>seed</b> (+ generator version),
/// materialises the player's guild, and remembers the season week. On reload the world regenerates
/// byte-identically from the seed — so a career stays in the same living world across sessions.
/// </summary>
public class WorldPersistenceTests
{
    private static GuildSave Career()
    {
        World world = WorldGen.Generate(12345);
        JobOffer offer = JobMarket.OffersFor(world, 1)[0];
        Manager manager = Managers.Create("Aldric", 35, "Ironreach", "guild_officer", new Dictionary<string, int>());

        GuildSave guild = JobMarket.Take(world, offer.GuildId, manager, "2026-01-01T00:00:00Z");
        return guild with
        {
            WorldSeed = world.Seed,
            GeneratorVersion = WorldGen.GeneratorVersion,
            ManagerGuildId = offer.GuildId.Value,
            SeasonWeek = 4,
        };
    }

    [Fact]
    public void Career_RoundTrips_TheWorldSeedAndSeason()
    {
        GuildSave career = Career();

        GuildSave loaded = SaveSerializer.Load(SaveSerializer.Serialize(career));

        Assert.Equal(career.WorldSeed, loaded.WorldSeed);
        Assert.Equal(career.GeneratorVersion, loaded.GeneratorVersion);
        Assert.Equal(career.ManagerGuildId, loaded.ManagerGuildId);
        Assert.Equal(4, loaded.SeasonWeek);
        Assert.Equal(career.Roster.Count, loaded.Roster.Count); // the materialised guild persists too
    }

    [Fact]
    public void RegeneratingFromThePinnedSeed_GivesTheSameWorld()
    {
        GuildSave loaded = SaveSerializer.Load(SaveSerializer.Serialize(Career()));

        World regenerated = WorldGen.Generate(loaded.WorldSeed);
        Assert.Equal(WorldText.Hash(WorldGen.Generate(12345)), WorldText.Hash(regenerated)); // byte-identical world
    }

    [Fact]
    public void JoinedRaiders_KeepTheirWorldComponents()
    {
        // the shared-component payoff: a raider joining from the world carries identity/vocation/attributes.
        GuildSave career = Career();
        Assert.All(career.Roster, r =>
        {
            Assert.NotNull(r.Attributes);
            Assert.NotNull(r.Identity);
            Assert.NotNull(r.Vocation);
        });
    }

    [Fact]
    public void OldSaves_WithoutAWorld_DefaultCleanly()
    {
        // a pre-persistence save (no world fields) loads with WorldSeed 0 and SeasonWeek 1.
        var legacy = new GuildSave(SaveMigrations.CurrentVersion, "2026-01-01T00:00:00Z",
            new GuildInfo("Old Guild", 0), new List<RaiderRecord>(), new Economy(1000), new List<RaidSummary>());

        GuildSave loaded = SaveSerializer.Load(SaveSerializer.Serialize(legacy));
        Assert.Equal(0UL, loaded.WorldSeed);
        Assert.Equal(1, loaded.SeasonWeek);
    }
}
