using System;
using System.Collections.Generic;
using Content;
using Engine;

namespace Game;

/// <summary>
/// A guild's seeded backstory (GDD §3): what they've been and done, before you write the next chapter.
/// Generated off the world seed + guild id, so a guild's past is stable and consistent with its prestige.
/// First-pass flavour.
/// </summary>
public sealed record GuildLore(int FoundedSeasonsAgo, string Motto, IReadOnlyList<string> PastEndeavours)
{
    private static readonly string[] Mottos =
    {
        "Through the gate, together.", "No wipe unlearned.", "The last to fall.", "Steel, and patience.",
        "We remember every pull.", "Glory is a long night's work.", "Hold the line.", "Ashes to embers.",
    };

    public static GuildLore For(World world, Guild guild)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(guild);

        var rng = new SeededRng(world.Seed, StableStream(guild.Id.Value));
        int foundedSeasonsAgo = rng.NextInt(3, 40);
        string motto = Mottos[rng.NextInt(Mottos.Length)];

        var past = new List<string>();

        int bestFinish = guild.Tier switch
        {
            PrestigeTier.Local => rng.NextInt(6, 15),
            PrestigeTier.National => rng.NextInt(3, 8),
            _ => rng.NextInt(1, 4),
        };
        past.Add($"Best finish: #{bestFinish} in {guild.Region}, {rng.NextInt(2, foundedSeasonsAgo)} seasons ago.");

        NamePool pool = FindPool(guild.Region);
        string legend = $"{pool.First[rng.NextInt(pool.First.Count)]} {pool.Surnames[rng.NextInt(pool.Surnames.Count)]}";
        past.Add($"Once home to {legend}, a name still spoken in {guild.Region} — long since retired.");

        past.Add(guild.Tier == PrestigeTier.Local
            ? "Fallen on hard times: tight funds, a thin bench, and a board that just wants to survive."
            : "Steady if unspectacular — the kind of guild a new manager can make a name at.");

        return new GuildLore(foundedSeasonsAgo, motto, past);
    }

    private static NamePool FindPool(string region)
    {
        foreach (NamePool pool in NamePools.All)
        {
            if (pool.Region == region)
            {
                return pool;
            }
        }

        return NamePools.All[0];
    }

    private static ulong StableStream(string id)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in id)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }

        return hash + 7UL; // distinct from JobMarket's finance stream for the same guild
    }
}
