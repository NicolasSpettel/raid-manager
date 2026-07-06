using System;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// <summary>A free agent on the market: the real world character, plus what signing them costs.</summary>
public sealed record TransferListing(RaiderRecord Raider, int Fee, int WeeklyWage);

/// <summary>
/// The transfer market (GDD §8b, first slice): the ~400 guildless free agents of the living world are real,
/// inspectable characters you can sign for a performance-driven fee. Full async negotiation, delegated
/// scouting and poaching (both directions) are later layers; this is browse-and-sign a free agent.
/// </summary>
public static class TransferMarket
{
    /// <summary>
    /// The free agents available to sign — the world's guildless pool minus anyone already on your roster,
    /// priced and sorted best-first, capped to <paramref name="limit"/> so the board stays readable.
    /// </summary>
    public static IReadOnlyList<TransferListing> FreeAgents(World world, GuildSave save, int limit = 40)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(save);

        var onRoster = save.Roster.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        return world.FreeAgents
            .Select(world.Get)
            .Where(r => !onRoster.Contains(r.Id))
            .OrderByDescending(r => Ratings.Best(r).HalfStars)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .Take(limit)
            .Select(r => new TransferListing(r, Fee(r), WeeklyWage(Ratings.Best(r).HalfStars)))
            .ToList();
    }

    /// <summary>A performance-driven transfer fee (never wallet-scaling): a curve on the raider's headline stars.</summary>
    public static int Fee(RaiderRecord raider)
    {
        int best = Ratings.Best(raider).HalfStars; // 2..10 (1.0★..5.0★)
        return 100 + ((best - 2) * 220);
    }

    /// <summary>Weekly wage by star band (mirrors the job-market bands, economy-model §2).</summary>
    public static int WeeklyWage(int halfStars) => halfStars switch
    {
        >= 9 => 240,
        >= 7 => 130,
        >= 5 => 55,
        _ => 20,
    };

    /// <summary>
    /// Sign a free agent to your guild if you can afford the fee. Returns false (guild unchanged) when they're
    /// already on the roster or the bank is short.
    /// </summary>
    public static bool TrySign(GuildSave guild, RaiderRecord agent, out GuildSave updated)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(agent);
        updated = guild;

        int fee = Fee(agent);
        bool alreadyHere = guild.Roster.Any(r => r.Id == agent.Id);
        if (alreadyHere || guild.Economy.Gold < fee)
        {
            return false;
        }

        GuildId? membership = guild.ManagerGuildId is { } id ? new GuildId(id) : agent.Membership;
        updated = guild with
        {
            Roster = guild.Roster.Append(agent with { Equipped = new List<string>(), Membership = membership }).ToList(),
            Economy = new Economy(guild.Economy.Gold - fee),
        };
        return true;
    }
}
