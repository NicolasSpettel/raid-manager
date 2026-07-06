using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>
/// The youth program (GDD §10): every guild has one. Each season it produces a small intake of young, low-info
/// <b>prospects</b> — modest now, real hidden potential — that you can <b>promote</b> into the senior roster for
/// a nominal fee (they're already yours; the cost is kit + settling in). Investing gold to raise intake
/// quality (the "Youth Hall") is a later layer; this is the recruit-from-your-academy slice.
/// </summary>
public static class YouthProgram
{
    /// <summary>How many prospects the program surfaces each intake.</summary>
    public const int IntakeSize = 3;

    /// <summary>Promoting a prospect to the active roster (kit + settling-in) — a small gold sink.</summary>
    public const int PromoteCost = 100;

    /// <summary>
    /// The current intake for a guild in a given season — deterministic from (worldSeed, season, guild), so the
    /// same career surfaces the same faces until you sign them or a new season refreshes the pool.
    /// </summary>
    public static IReadOnlyList<RaiderRecord> Intake(ulong worldSeed, int seasonNumber, string guildId, int count = IntakeSize)
    {
        var guild = new GuildId(guildId);
        var prospects = new List<RaiderRecord>(count);
        for (int i = 0; i < count; i++)
        {
            var rng = new SeededRng(worldSeed, IntakeStream(seasonNumber, guildId, i));
            NamePool pool = NamePools.All[rng.NextInt(NamePools.All.Count)];
            CombatantRole role = Ratings.AllRoles[rng.NextInt(Ratings.AllRoles.Count)];
            var id = new RaiderId($"y:{guildId}:{seasonNumber}:{i}");
            // The world clock in season N (WorldConfig baseline 100, +1 per season) sets the prospect's age.
            int worldSeason = WorldConfig.Default.CurrentSeason + (seasonNumber - 1);
            prospects.Add(WorldGen.GenerateProspect(rng, id, pool, role, worldSeason, guild));
        }

        return prospects;
    }

    /// <summary>
    /// Promote a prospect from the intake into the senior roster, if the guild can afford it. Returns false
    /// (guild unchanged) when the prospect isn't in the pool or gold is short.
    /// </summary>
    public static bool TryPromote(GuildSave guild, string prospectId, out GuildSave updated)
    {
        ArgumentNullException.ThrowIfNull(guild);
        updated = guild;

        IReadOnlyList<RaiderRecord> pool = guild.YouthProspects ?? Array.Empty<RaiderRecord>();
        RaiderRecord? prospect = pool.FirstOrDefault(p => p.Id == prospectId);
        if (prospect is null || guild.Economy.Gold < PromoteCost)
        {
            return false;
        }

        updated = guild with
        {
            Roster = guild.Roster.Append(prospect with { Equipped = new List<string>() }).ToList(),
            YouthProspects = pool.Where(p => p.Id != prospectId).ToList(),
            Economy = new Economy(guild.Economy.Gold - PromoteCost),
        };
        return true;
    }

    // A stable per-(season, guild, slot) stream so an intake regenerates identically (FNV-1a over the key).
    private static ulong IntakeStream(int seasonNumber, string guildId, int slot)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in $"youth:{guildId}:{seasonNumber}:{slot}")
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }

        return hash;
    }
}
