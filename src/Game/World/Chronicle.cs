using System;
using System.Collections.Generic;
using System.Linq;
using Content;

namespace Game;

/// <summary>A world-first: the first guild to down a boss this season, and the week they did it.</summary>
public sealed record WorldFirst(string Boss, string Guild, string Region, int Week);

/// <summary>A retained leaderboard row — the compact snapshot the Chronicle keeps forever.</summary>
public sealed record ChronicleStanding(int Rank, string Guild, string Region, PrestigeTier Tier, int BossesDown, int? ClearedWeek);

/// <summary>
/// The permanent record of one season (GDD §5): the champion, the world-first kills, and the top-N final
/// leaderboard. This is the compact "what persists forever" — full detail is compacted away at each season
/// boundary, but the Chronicle survives (ADR-0007). Small and diffable.
/// </summary>
public sealed record SeasonChronicle(
    int Season,
    string RaidName,
    string? Champion,
    IReadOnlyList<WorldFirst> WorldFirsts,
    IReadOnlyList<ChronicleStanding> Top);

/// <summary>Folds a finished season race into its permanent <see cref="SeasonChronicle"/> entry.</summary>
public static class Chronicle
{
    public static SeasonChronicle Record(int season, SeasonResult result, int topN = 100)
    {
        ArgumentNullException.ThrowIfNull(result);
        IReadOnlyList<GuildProgress> standings = result.Standings;

        // The champion is the overall leader — but only if they actually cleared the raid.
        string? champion = standings.Count > 0 && standings[0].ClearedWeek is not null ? standings[0].Name : null;

        // World-first per boss = the earliest kill-week for that boss index across all guilds.
        var worldFirsts = new List<WorldFirst>();
        for (int i = 0; i < result.Raid.Bosses.Count; i++)
        {
            int bossIndex = i;
            GuildProgress? first = standings
                .Where(g => g.BossKillWeeks.Count > bossIndex)
                .OrderBy(g => g.BossKillWeeks[bossIndex])
                .ThenBy(g => g.Guild.Value, StringComparer.Ordinal)
                .FirstOrDefault();

            if (first is not null)
            {
                worldFirsts.Add(new WorldFirst(result.Raid.Bosses[bossIndex].Name, first.Name, first.Region, first.BossKillWeeks[bossIndex]));
            }
        }

        var top = standings
            .Take(topN)
            .Select((g, idx) => new ChronicleStanding(idx + 1, g.Name, g.Region, g.Tier, g.BossesDown, g.ClearedWeek))
            .ToList();

        return new SeasonChronicle(season, result.Raid.Name, champion, worldFirsts, top);
    }
}
