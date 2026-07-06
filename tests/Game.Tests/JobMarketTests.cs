using System.Collections.Generic;
using System.Linq;
using Content;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// The job market (GDD §4): a fresh manager is offered only struggling low-prestige guilds, and taking one
/// converts that real world guild — roster, attributes, condition — into the player's playable save.
/// </summary>
public class JobMarketTests
{
    private static Manager AnyManager() =>
        Managers.Create("Aldric Vance", 35, "Ironreach", "guild_officer", new Dictionary<string, int>());

    [Fact]
    public void Offers_AreOnlyStrugglingLowPrestigeGuilds()
    {
        var offers = JobMarket.OffersFor(WorldGen.Generate(1), count: 8);

        Assert.Equal(8, offers.Count);
        Assert.All(offers, o => Assert.Equal(PrestigeTier.Local, o.Tier));
        Assert.All(offers, o => Assert.InRange(o.Bank, 5000, 15000)); // economy-model §2 local band
    }

    [Fact]
    public void Take_ConvertsTheWorldGuild_IntoAPlayableSave()
    {
        World world = WorldGen.Generate(1);
        JobOffer offer = JobMarket.OffersFor(world, 1)[0];
        Manager manager = AnyManager();

        GuildSave save = JobMarket.Take(world, offer.GuildId, manager, "2026-01-01T00:00:00Z");

        Assert.Equal(offer.GuildName, save.Guild.Name);
        Assert.Equal(offer.RosterSize, save.Roster.Count);
        Assert.Same(manager, save.Manager);
        Assert.All(save.Roster, r => Assert.NotNull(r.Attributes)); // world attributes carried over
        Assert.All(save.Roster, r => Assert.NotNull(r.Condition));   // and condition
    }

    [Fact]
    public void TakenRoster_ProjectsToCombatants()
    {
        World world = WorldGen.Generate(1);
        JobOffer offer = JobMarket.OffersFor(world, 1)[0];
        GuildSave save = JobMarket.Take(world, offer.GuildId, AnyManager(), "2026-01-01T00:00:00Z");

        var combatants = save.Roster.Select(Warband.ToCombatant).ToList(); // class kits resolve, attributes fold
        Assert.Equal(save.Roster.Count, combatants.Count);
    }

    [Fact]
    public void Offers_AreDeterministic()
    {
        var a = JobMarket.OffersFor(WorldGen.Generate(5), 8).Select(o => o.GuildId.Value);
        var b = JobMarket.OffersFor(WorldGen.Generate(5), 8).Select(o => o.GuildId.Value);
        Assert.Equal(a, b);
    }
}
