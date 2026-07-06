using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>An open guild position a fresh manager can inspect and take (GDD §4).</summary>
public sealed record JobOffer(
    GuildId GuildId,
    string GuildName,
    string Region,
    PrestigeTier Tier,
    int RosterSize,
    int AvgHalfStars,
    int Bank,
    int WeeklyWages,
    string Expectation,
    string Rival);

/// <summary>
/// The job market (GDD §4): a fresh, unknown manager is only offered **struggling, low-prestige** guilds.
/// Each offer is a real guild from the generated world, surfaced with the things you inspect before signing —
/// roster quality, finances (bank + wage bill), the board's expectation, and a regional rival. Deterministic:
/// the same world yields the same offers. (Full contract negotiation is a later layer; for now you take a job.)
/// </summary>
public static class JobMarket
{
    public static IReadOnlyList<JobOffer> OffersFor(World world, int count = 8)
    {
        ArgumentNullException.ThrowIfNull(world);

        return world.Guilds
            .Where(g => g.Tier == PrestigeTier.Local)
            .Take(count)
            .Select(g => BuildOffer(world, g))
            .ToList();
    }

    /// <summary>Convert a chosen world guild into the player's <see cref="GuildSave"/> — its real roster and finances.</summary>
    public static GuildSave Take(World world, GuildId guildId, Manager manager, string createdAtIso)
    {
        ArgumentNullException.ThrowIfNull(world);
        Guild guild = world.Guilds.First(g => g.Id == guildId);

        var roster = guild.Roster
            .Select(world.Get)
            .Select(r => new RaiderRecord(
                r.Id.Value, r.Identity.Name, r.Vocation.ClassId,
                Equipped: new List<string>(), InjuryRaidsLeft: 0,
                Attributes: r.Attributes, Condition: r.Condition))
            .ToList();

        return new GuildSave(
            SaveMigrations.CurrentVersion,
            createdAtIso,
            new GuildInfo(guild.Name, Reputation: (int)PrestigeTier.Local),
            roster,
            new Economy(BankFor(world, guild)),
            new List<RaidSummary>(),
            manager);
    }

    private static JobOffer BuildOffer(World world, Guild guild)
    {
        var stars = guild.Roster.Select(world.Get).Select(r => Ratings.Best(r).HalfStars).ToList();
        int avgHalfStars = stars.Count == 0 ? 0 : stars.Sum() / stars.Count;
        int wages = guild.Roster.Select(world.Get).Sum(r => WeeklyWage(Ratings.Best(r).HalfStars));

        return new JobOffer(
            guild.Id, guild.Name, guild.Region, guild.Tier,
            guild.Roster.Count, avgHalfStars, BankFor(world, guild), wages,
            Expectation(avgHalfStars, guild.Region), RivalFor(world, guild));
    }

    // Local-tier balance (economy-model §2: 5,000–15,000), deterministic per guild.
    private static int BankFor(World world, Guild guild) =>
        new SeededRng(world.Seed, StableStream(guild.Id.Value)).NextInt(5000, 15001);

    // Weekly wage by star band (economy-model §2 ratios, compressed for a 20-30 person roster).
    private static int WeeklyWage(int halfStars) => halfStars switch
    {
        >= 9 => 240, // 4.5★+
        >= 7 => 130, // 3.5–4★
        >= 5 => 55,  // 2.5–3★
        _ => 20,     // fringe
    };

    private static string Expectation(int avgHalfStars, string region) => avgHalfStars switch
    {
        >= 7 => $"Push for a top-3 finish in {region}.",
        >= 6 => $"Finish mid-table in {region} and stay solvent.",
        _ => "Just survive the season — keep the guild out of the red.",
    };

    private static string RivalFor(World world, Guild guild)
    {
        Guild? rival = world.Guilds.FirstOrDefault(g =>
            g.Id != guild.Id && g.Region == guild.Region && g.Tier == PrestigeTier.Local);
        return rival?.Name ?? "no established rival yet";
    }

    // A stable rng-stream id from a guild id string (FNV-1a), so finances regenerate identically.
    private static ulong StableStream(string id)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in id)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }

        return hash;
    }
}
