using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Content;
using Engine;

namespace Game;

/// <summary>
/// Canonical, deterministic text serialization of a generated world, plus a stable content hash — the
/// world-gen golden (entities §8): seed 1 ⇒ a fixed hash, byte-identical across Sim/App/CI. Same FNV-1a
/// scheme and invariant-culture discipline as the combat <c>EventStream</c>.
/// </summary>
public static class WorldText
{
    public static string Serialize(World world)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"WORLD seed={world.Seed} season={world.CurrentSeason} guilds={world.Guilds.Count} freeAgents={world.FreeAgents.Count}\n");

        foreach (Guild guild in world.Guilds)
        {
            sb.Append(CultureInfo.InvariantCulture, $"GUILD {guild.Id.Value} tier={guild.Tier} region={guild.Region} name={guild.Name} roster={guild.Roster.Count}\n");
            foreach (RaiderId id in guild.Roster)
            {
                sb.Append(RaiderLine(world, world.Get(id)));
            }
        }

        sb.Append("FREE_AGENTS\n");
        foreach (RaiderId id in world.FreeAgents)
        {
            sb.Append(RaiderLine(world, world.Get(id)));
        }

        return sb.ToString();
    }

    /// <summary>Stable 64-bit FNV-1a hash of the serialized world, as 16 lowercase hex digits.</summary>
    public static string Hash(World world)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        ulong hash = offsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes(Serialize(world)))
        {
            hash ^= b;
            hash *= prime;
        }

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    private static string RaiderLine(World world, RaiderRecord r)
    {
        (CombatantRole role, int half) = Ratings.Best(r);
        var sb = new StringBuilder();
        sb.Append(
            CultureInfo.InvariantCulture,
            $"  R {r.Id} name={r.Name} age={world.AgeOf(r)} class={r.ClassId} arch={r.ArchetypeId} best={role}:{half}");
        foreach (AttributeDef a in Attributes.Registry.All)
        {
            sb.Append(' ').Append(a.Id).Append('=').Append((r.Attributes?.Of(a.Id) ?? 10).ToString(CultureInfo.InvariantCulture));
        }

        sb.Append('\n');
        return sb.ToString();
    }
}
