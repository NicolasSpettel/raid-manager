using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>Size of a generated world (entities §1 targets ~6,500 characters). All numbers scalable.</summary>
public sealed record WorldConfig(int GuildCount = 200, int RosterSize = 28, int FreeAgents = 400, int CurrentSeason = 100)
{
    public static WorldConfig Default => new();
}

/// <summary>
/// The deterministic world-generation pipeline (entities-and-worldgen §3): a seed becomes ~6,500 coherent,
/// reproducible characters. Each unit is generated from its OWN rng stream keyed by (seed, guild, slot), so
/// the baseline regenerates identically and any single unit is independently reproducible — the foundation
/// of the seed+delta persistence model (§4/§5). Pure integer draws via <see cref="SeededRng"/>; no
/// <c>System.Random</c>, no clock. Coherence comes from the latent-factor projection (§3.2), not luck.
/// </summary>
public static class WorldGen
{
    /// <summary>Pinned in the save (ADR-0007): the world regenerates from (seed, version). Bump when world-gen output changes.</summary>
    public const int GeneratorVersion = 1;

    private static readonly string[] GuildNouns =
    {
        "Vanguard", "Covenant", "Wardens", "Ascendant", "Legion", "Circle",
        "Concord", "Sentinels", "Banner", "Ashen Pact", "Dawnbreakers", "Ironclad",
    };

    public static World Generate(ulong seed, WorldConfig? config = null)
    {
        config ??= WorldConfig.Default;

        var guilds = new List<Guild>(config.GuildCount);
        var raiders = new Dictionary<RaiderId, RaiderRecord>();

        for (int g = 0; g < config.GuildCount; g++)
        {
            PrestigeTier tier = TierForIndex(g, config.GuildCount);
            NamePool pool = NamePools.All[g % NamePools.All.Count];
            var guildId = new GuildId($"guild_{g:D3}");

            var guildRng = new SeededRng(seed, GuildStream(g));
            string guildName = $"{pool.Region} {GuildNouns[guildRng.NextInt(GuildNouns.Length)]}";

            IReadOnlyList<CombatantRole> slots = RosterPlan(config.RosterSize);
            var rosterIds = new List<RaiderId>(slots.Count);
            for (int s = 0; s < slots.Count; s++)
            {
                var id = new RaiderId($"{guildId.Value}_r{s:D2}");
                var rng = new SeededRng(seed, RaiderStream(g, s));
                raiders[id] = GenerateRaider(rng, id, pool, tier, slots[s], config.CurrentSeason, guildId);
                rosterIds.Add(id);
            }

            guilds.Add(new Guild(guildId, guildName, pool.Region, tier, rosterIds));
        }

        var freeAgents = new List<RaiderId>(config.FreeAgents);
        for (int i = 0; i < config.FreeAgents; i++)
        {
            NamePool pool = NamePools.All[i % NamePools.All.Count];
            var id = new RaiderId($"fa_{i:D4}");
            var rng = new SeededRng(seed, FreeAgentStream(i));
            CombatantRole role = Ratings.AllRoles[rng.NextInt(Ratings.AllRoles.Count)];
            raiders[id] = GenerateRaider(rng, id, pool, PrestigeTier.National, role, config.CurrentSeason, membership: null);
            freeAgents.Add(id);
        }

        return new World(seed, config.CurrentSeason, guilds, raiders, freeAgents);
    }

    /// <summary>
    /// Roll a coherent attribute vector for a raider not born through the full pipeline (e.g. the player's
    /// starter guild) — same latent-factor model, its own draw order (so it never touches the world golden).
    /// </summary>
    public static AttributeVector RollStarterAttributes(SeededRng rng, PrestigeTier tier)
    {
        ArchetypeDef archetype = PickArchetype(rng, tier);
        LatentProfile latents = DrawLatents(rng, archetype);
        int age = Math.Clamp(archetype.AgeMean + rng.NextInt(-archetype.AgeSpread, archetype.AgeSpread + 1), 16, 33);
        return ProjectAttributes(rng, latents, age);
    }

    // ── SlotFill (entities §3): archetype → latents → attributes → vocation → identity ──────────────────
    private static RaiderRecord GenerateRaider(
        SeededRng rng, RaiderId id, NamePool pool, PrestigeTier tier, CombatantRole role, int season, GuildId? membership)
    {
        ArchetypeDef archetype = PickArchetype(rng, tier);
        LatentProfile latents = DrawLatents(rng, archetype);
        int age = Math.Clamp(archetype.AgeMean + rng.NextInt(-archetype.AgeSpread, archetype.AgeSpread + 1), 16, 33);

        AttributeVector attributes = ProjectAttributes(rng, latents, age);
        string classId = PickClass(rng, role);
        var vocation = new Vocation(classId, PotentialByRole(latents.Talent, role));

        string name = $"{pool.First[rng.NextInt(pool.First.Count)]} {pool.Surnames[rng.NextInt(pool.Surnames.Count)]}";
        var identity = new Identity(name, pool.Region, season - age, rng.NextUInt());
        var condition = new Condition(Morale: 60 + rng.NextInt(0, 25), Freshness: 100, Sharpness: 55 + rng.NextInt(0, 25));

        return new RaiderRecord(
            id.Value, name, classId,
            Equipped: null, InjuryRaidsLeft: 0,
            Attributes: attributes, Condition: condition,
            Identity: identity, Vocation: vocation, ArchetypeId: archetype.Id, Membership: membership);
    }

    // LatentDraw (§3.2): roll each latent from the archetype's mean ± spread.
    private static LatentProfile DrawLatents(SeededRng rng, ArchetypeDef a)
    {
        int Draw(int mean) => Math.Clamp(mean + rng.NextInt(-a.LatentSpread, a.LatentSpread + 1), 0, 100);
        return new LatentProfile(
            Draw(a.LatentMeans.Talent), Draw(a.LatentMeans.Discipline),
            Draw(a.LatentMeans.Experience), Draw(a.LatentMeans.Volatility));
    }

    // AttributeProjection (§3.2): each attribute is a weighted projection of the latents + an age curve + small noise.
    private static AttributeVector ProjectAttributes(SeededRng rng, LatentProfile lat, int age)
    {
        var values = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (AttributeDef attr in Attributes.Registry.All)
        {
            LatentLoading w = attr.Loading;
            int proj = ((w.Talent * (lat.Talent - 50))
                + (w.Discipline * (lat.Discipline - 50))
                + (w.Experience * (lat.Experience - 50))
                + (w.Volatility * (lat.Volatility - 50))) / 100;
            int ageMod = AgeModifier(attr.Aging, age);
            int noise = rng.NextInt(-1, 2); // −1, 0, +1
            values[attr.Id] = Math.Clamp(10 + proj + ageMod + noise, 1, 20);
        }

        return new AttributeVector(values);
    }

    // Age curve (§3.3 / GDD §8): twitch stats fall past the peak, wisdom stats rise into veteran years.
    private static int AgeModifier(AgingClass aging, int age) => aging switch
    {
        AgingClass.Twitch => -Math.Min(5, Math.Max(0, age - 26)),
        AgingClass.Wisdom => Math.Min(5, Math.Max(0, age - 19) / 2),
        _ => 0,
    };

    // ArchetypePick (§3.1): weighted by the guild's prestige tier.
    private static ArchetypeDef PickArchetype(SeededRng rng, PrestigeTier tier)
    {
        int total = 0;
        foreach (ArchetypeDef a in Archetypes.Registry.All)
        {
            total += a.WeightFor(tier);
        }

        int roll = rng.NextInt(total);
        foreach (ArchetypeDef a in Archetypes.Registry.All)
        {
            roll -= a.WeightFor(tier);
            if (roll < 0)
            {
                return a;
            }
        }

        return Archetypes.Registry.All[^1];
    }

    // DeriveVocation (§3): a class whose natural role matches the slot need (ordered for determinism).
    private static string PickClass(SeededRng rng, CombatantRole role)
    {
        List<string> options = Classes.Registry.All
            .Where(c => c.Role == role)
            .Select(c => c.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        return options[rng.NextInt(options.Count)];
    }

    private static Dictionary<CombatantRole, int> PotentialByRole(int talent, CombatantRole natural)
    {
        var potential = new Dictionary<CombatantRole, int>();
        foreach (CombatantRole role in Ratings.AllRoles)
        {
            potential[role] = Math.Clamp(role == natural ? talent + 15 : talent - 25, 0, 100);
        }

        return potential;
    }

    // GuildPlan (§3): a role composition that guarantees raid coverage (≥2 tanks, ≥4 healers).
    private static List<CombatantRole> RosterPlan(int size)
    {
        int tanks = Math.Max(2, size / 9);
        int healers = Math.Max(4, size / 5);
        int dps = Math.Max(0, size - tanks - healers);
        int melee = dps / 2;
        int ranged = dps - melee;

        var slots = new List<CombatantRole>(size);
        slots.AddRange(Enumerable.Repeat(CombatantRole.Tank, tanks));
        slots.AddRange(Enumerable.Repeat(CombatantRole.Healer, healers));
        slots.AddRange(Enumerable.Repeat(CombatantRole.Melee, melee));
        slots.AddRange(Enumerable.Repeat(CombatantRole.Ranged, ranged));
        return slots;
    }

    // RegionPlan (§3): a league pyramid — a few elite guilds, most local.
    private static PrestigeTier TierForIndex(int g, int count)
    {
        int elite = Math.Max(1, (count * 4) / 100);
        int continental = Math.Max(1, (count * 13) / 100);
        int national = Math.Max(1, (count * 30) / 100);

        if (g < elite)
        {
            return PrestigeTier.WorldElite;
        }

        if (g < elite + continental)
        {
            return PrestigeTier.Continental;
        }

        if (g < elite + continental + national)
        {
            return PrestigeTier.National;
        }

        return PrestigeTier.Local;
    }

    // Distinct rng streams per unit → independent, regenerable draws (entities §5).
    private static ulong GuildStream(int g) => 1UL + (ulong)g;

    private static ulong RaiderStream(int g, int s) => 0x1000_0000UL + ((ulong)g << 12) + (ulong)s;

    private static ulong FreeAgentStream(int i) => 0x8000_0000_0000UL + (ulong)i;
}
