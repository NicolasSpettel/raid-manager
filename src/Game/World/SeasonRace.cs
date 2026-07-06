using System.Collections.Generic;
using System.Linq;
using Content;

namespace Game;

/// <summary>How far a guild got in the season's raid, and when (GDD §5 leaderboard: bosses down + timestamps).</summary>
public sealed record GuildProgress(
    GuildId Guild,
    string Name,
    string Region,
    PrestigeTier Tier,
    int Strength,
    int BossesDown,
    int? ClearedWeek,
    int LastKillWeek);

/// <summary>A finished (or in-progress) season race: the raid, the weeks simulated, and the ranked standings.</summary>
public sealed record SeasonResult(SeasonRaid Raid, int Weeks, IReadOnlyList<GuildProgress> Standings)
{
    /// <summary>Standings filtered to one region (regional leaderboard).</summary>
    public IEnumerable<GuildProgress> Regional(string region) => Standings.Where(g => g.Region == region);
}

/// <summary>
/// The living-world race (GDD §5): every guild progresses the same raid under the same rules, in parallel,
/// week by week — "the world keeps racing, you watch". This is the cheap, abstract fidelity tier for
/// background guilds (entities §7): a guild banks <c>Strength</c> progress per week and downs the next boss
/// when it has enough, so stronger rosters clear faster. Deterministic (a pure fold over the generated
/// world). NOT the AI decision model (recruiting/scheduling is still undefined, THIN §9) — only the race
/// outcome, calibrated to the locked pacing target.
/// </summary>
public static class SeasonRace
{
    // Prestige multiplier (×10) on roster strength — the spread that makes elite guilds pull away.
    private static int TierMultX10(PrestigeTier tier) => tier switch
    {
        PrestigeTier.WorldElite => 60,
        PrestigeTier.Continental => 40,
        PrestigeTier.National => 22,
        _ => 14,
    };

    private const int RaidRosterSize = 20; // the strongest 20 carry the progression (a raid group)

    public static SeasonResult Run(World world, SeasonRaid? raid = null, int weeks = 16)
    {
        raid ??= SeasonRaid.Default;

        var tracks = world.Guilds.Select(g => new Track(g, Strength(world, g))).ToList();

        for (int week = 1; week <= weeks; week++)
        {
            foreach (Track t in tracks)
            {
                if (t.ClearedWeek is not null)
                {
                    continue;
                }

                t.Credit += t.Strength;
                while (t.BossesDown < raid.Bosses.Count && t.Credit >= raid.Bosses[t.BossesDown].Difficulty)
                {
                    t.Credit -= raid.Bosses[t.BossesDown].Difficulty;
                    t.BossesDown++;
                    t.LastKillWeek = week;
                    if (t.BossesDown == raid.Bosses.Count)
                    {
                        t.ClearedWeek = week;
                    }
                }
            }
        }

        List<GuildProgress> standings = tracks
            .Select(t => new GuildProgress(
                t.Guild.Id, t.Guild.Name, t.Guild.Region, t.Guild.Tier, t.Strength, t.BossesDown, t.ClearedWeek, t.LastKillWeek))
            .OrderByDescending(g => g.BossesDown)
            .ThenBy(g => g.ClearedWeek ?? int.MaxValue)
            .ThenBy(g => g.LastKillWeek == 0 ? int.MaxValue : g.LastKillWeek)
            .ThenBy(g => g.Guild.Value, System.StringComparer.Ordinal)
            .ToList();

        return new SeasonResult(raid, weeks, standings);
    }

    /// <summary>A guild's weekly progression power: its raid group's average stars scaled by prestige tier.</summary>
    public static int Strength(World world, Guild guild)
    {
        List<int> starsDesc = guild.Roster
            .Select(world.Get)
            .Select(r => Ratings.Best(r).HalfStars)
            .OrderByDescending(h => h)
            .Take(RaidRosterSize)
            .ToList();

        int avgHalfStars = starsDesc.Count == 0 ? 0 : starsDesc.Sum() / starsDesc.Count;
        return (avgHalfStars * TierMultX10(guild.Tier)) / 10;
    }

    private sealed class Track
    {
        public Track(Guild guild, int strength)
        {
            Guild = guild;
            Strength = strength;
        }

        public Guild Guild { get; }

        public int Strength { get; }

        public int Credit { get; set; }

        public int BossesDown { get; set; }

        public int LastKillWeek { get; set; }

        public int? ClearedWeek { get; set; }
    }
}
