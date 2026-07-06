using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>The roster after a season's aging, plus the notable events (developments, retirements).</summary>
public sealed record AgingResult(GuildSave Guild, IReadOnlyList<string> Events);

/// <summary>
/// Aging & the career arc (GDD §8): at each season boundary the world clock advances, so raiders get a year
/// older. Development years (≤22) grow toward potential; the peak (23–27) holds; past ~28 the twitch stats
/// fade while wisdom stats rise; at ~31 they retire and leave the roster (you refill from the youth intake or
/// the transfer market). Age is derived from birth-season vs the season number, so this is just the clock
/// ticking. First-pass rates, all tunable.
/// </summary>
public static class Aging
{
    private const int BaseSeason = 100; // WorldConfig.Default.CurrentSeason (raider birth-seasons are relative to this)
    private const int RetireAge = 31;
    private const int GrowthCeiling = 18;

    /// <summary>A raider's age in the given season, derived from their birth-season.</summary>
    public static int AgeOf(RaiderRecord raider, int seasonNumber) =>
        raider.Identity is { } identity ? (BaseSeason + (seasonNumber - 1)) - identity.BirthSeason : 25;

    public static AgingResult AdvanceSeason(GuildSave guild, int newSeasonNumber)
    {
        ArgumentNullException.ThrowIfNull(guild);
        var roster = new List<RaiderRecord>(guild.Roster.Count);
        var events = new List<string>();

        int i = 0;
        foreach (RaiderRecord raider in guild.Roster)
        {
            var rng = new SeededRng(guild.WorldSeed, stream: (ulong)(90000 + (newSeasonNumber * 100) + i));
            i++;

            int age = AgeOf(raider, newSeasonNumber);
            if (age >= RetireAge)
            {
                events.Add($"{raider.Name} retired at {age} — a roster spot opens up.");
                continue; // no auto-backfill; recruit from the youth intake or the transfer market
            }

            roster.Add(AgeAttributes(raider, age, rng, out string? note));
            if (note is not null)
            {
                events.Add(note);
            }
        }

        return new AgingResult(guild with { Roster = roster }, events);
    }

    private static RaiderRecord AgeAttributes(RaiderRecord raider, int age, SeededRng rng, out string? note)
    {
        note = null;
        if (raider.Attributes is null)
        {
            return raider;
        }

        var values = Attributes.Registry.All.ToDictionary(a => a.Id, a => raider.Attributes.Of(a.Id), StringComparer.Ordinal);

        if (age <= 22)
        {
            int cap = GrowthCap(raider);
            List<AttributeDef> below = Attributes.Registry.All.Where(a => values[a.Id] < cap).ToList();
            int gains = Math.Min(2, below.Count);
            for (int k = 0; k < gains; k++)
            {
                AttributeDef target = below[rng.NextInt(below.Count)];
                if (values[target.Id] < cap)
                {
                    values[target.Id]++;
                }
            }

            note = gains > 0 ? $"{raider.Name} ({age}) is developing." : null;
        }
        else if (age >= 28)
        {
            foreach (AttributeDef a in Attributes.Registry.All)
            {
                if (a.Aging == AgingClass.Twitch)
                {
                    values[a.Id] = Math.Max(1, values[a.Id] - 1);
                }
                else if (a.Aging == AgingClass.Wisdom)
                {
                    values[a.Id] = Math.Min(20, values[a.Id] + 1);
                }
            }

            note = $"{raider.Name} ({age}) is past their peak — reflexes fading, wisdom growing.";
        }

        return raider with { Attributes = new AttributeVector(values) };
    }

    // Growth stops at a cap derived from the raider's best-role potential (potential 100 → ~18, 50 → ~14).
    private static int GrowthCap(RaiderRecord raider)
    {
        int best = raider.Vocation is { } v && v.PotentialByRole.Count > 0 ? v.PotentialByRole.Values.Max() : 50;
        return Math.Clamp(10 + ((best * 8) / 100), 12, GrowthCeiling);
    }
}
