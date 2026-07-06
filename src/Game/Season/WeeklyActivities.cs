using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>What the off-day activities produced this week.</summary>
public sealed record ActivityReport(int GearDrops, int TrainingSessions, IReadOnlyList<string> Events);

/// <summary>The guild after a week of activities, plus the report.</summary>
public sealed record ActivityOutcome(GuildSave Guild, ActivityReport Report);

/// <summary>
/// The off-day weekly activities (GDD §6) — the *other* half of "the grind". <b>Dungeons</b> are a gear
/// catch-up faucet (they drop lower-tier items to the neediest raider), and <b>Training</b> is targeted
/// attribute development (§8: "training assignments target attributes"). This is what makes grinding pay:
/// gear + stat growth in exchange for the freshness it costs. Deterministic (seeded); first-pass numbers.
/// </summary>
public static class WeeklyActivities
{
    private const int TrainCap = 18;            // trained up to here (headroom below 20); true potential-cap needs Vocation
    private const int RaidersTrainedPerDay = 3; // the neediest few develop each training day

    public static ActivityOutcome Run(GuildSave guild, ActivityPlan plan, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(plan);

        var roster = guild.Roster.ToList();
        var events = new List<string>();
        int gearDrops = 0;
        int trainingSessions = 0;

        IReadOnlyList<ItemDef> dungeonPool = Loot.For("dungeon");
        for (int day = 0; day < plan.DungeonDays && dungeonPool.Count > 0; day++)
        {
            var rng = new SeededRng(seed, stream: (ulong)(1000 + day));
            ItemDef drop = dungeonPool[rng.NextInt(dungeonPool.Count)];
            int recipient = NeediestUpgrader(roster, drop);
            if (recipient >= 0)
            {
                roster[recipient] = Warband.EquipIfUpgrade(roster[recipient], drop);
                gearDrops++;
                events.Add($"Dungeon: {roster[recipient].Name} picked up {drop.Name}");
            }
        }

        for (int day = 0; day < plan.TrainDays; day++)
        {
            foreach (int idx in LeastDeveloped(roster, RaidersTrainedPerDay))
            {
                RaiderRecord trained = TrainWeakestAttribute(roster[idx]);
                if (!ReferenceEquals(trained, roster[idx]))
                {
                    roster[idx] = trained;
                    trainingSessions++;
                }
            }
        }

        if (trainingSessions > 0)
        {
            events.Add($"Training: {trainingSessions} attribute point(s) developed");
        }

        return new ActivityOutcome(guild with { Roster = roster }, new ActivityReport(gearDrops, trainingSessions, events));
    }

    // The lowest-geared raider this item would actually upgrade (catch-up gear goes where it helps most).
    private static int NeediestUpgrader(List<RaiderRecord> roster, ItemDef drop)
    {
        int best = -1;
        int lowestPower = int.MaxValue;
        for (int i = 0; i < roster.Count; i++)
        {
            if (ReferenceEquals(Warband.EquipIfUpgrade(roster[i], drop), roster[i]))
            {
                continue; // no upgrade for this raider
            }

            int power = Warband.GearPower(roster[i]);
            if (power < lowestPower)
            {
                lowestPower = power;
                best = i;
            }
        }

        return best;
    }

    private static List<int> LeastDeveloped(List<RaiderRecord> roster, int count) => Enumerable
        .Range(0, roster.Count)
        .OrderBy(AttributeTotal(roster))
        .ThenBy(i => roster[i].Id, StringComparer.Ordinal)
        .Take(count)
        .ToList();

    private static Func<int, int> AttributeTotal(List<RaiderRecord> roster) => i =>
    {
        AttributeVector? attrs = roster[i].Attributes;
        return attrs is null ? int.MaxValue : Attributes.Registry.All.Sum(a => attrs.Of(a.Id));
    };

    // Raise the raider's single lowest (sub-cap) attribute by one — targeted development.
    private static RaiderRecord TrainWeakestAttribute(RaiderRecord raider)
    {
        if (raider.Attributes is null)
        {
            return raider;
        }

        string? target = null;
        int lowest = int.MaxValue;
        foreach (AttributeDef a in Attributes.Registry.All)
        {
            int value = raider.Attributes.Of(a.Id);
            if (value < TrainCap && value < lowest)
            {
                lowest = value;
                target = a.Id;
            }
        }

        if (target is null)
        {
            return raider; // already at the cap everywhere
        }

        var values = Attributes.Registry.All.ToDictionary(a => a.Id, a => raider.Attributes.Of(a.Id), StringComparer.Ordinal);
        values[target] += 1;
        return raider with { Attributes = new AttributeVector(values) };
    }
}
