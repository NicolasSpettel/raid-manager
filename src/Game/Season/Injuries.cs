using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game;

/// <summary>
/// Condition-driven injuries (GDD §8 [LOCKED], economy-model §1): low freshness + heavy raid load raise the
/// chance a raider gets hurt, and the injury sits them at reduced strength for a few raids (the existing
/// <see cref="RaiderRecord.InjuryRaidsLeft"/> penalty). Seeded → deterministic. First-pass rates; the
/// Injury-Proneness trait and graded severities (knock → serious) come with the trait/entity layer.
/// </summary>
public static class Injuries
{
    public static (GuildSave Guild, IReadOnlyList<string> Events) RollWeek(GuildSave guild, int raidDays, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(guild);

        var roster = guild.Roster.ToList();
        var events = new List<string>();

        for (int i = 0; i < roster.Count; i++)
        {
            RaiderRecord raider = roster[i];
            if (raider.InjuryRaidsLeft > 0)
            {
                continue; // already carrying a knock
            }

            int freshness = (raider.Condition ?? ConditionModel.Fresh).Freshness;
            int chancePct = ChancePct(freshness, raidDays);
            if (chancePct <= 0)
            {
                continue;
            }

            var rng = new SeededRng(seed, stream: (ulong)(5000 + i));
            if (rng.NextInt(100) < chancePct)
            {
                int severity = 1 + rng.NextInt(3); // 1–3 raids out (knock → longer)
                roster[i] = raider with { InjuryRaidsLeft = severity };
                events.Add($"{raider.Name} picked up an injury — out {severity} raid(s)");
            }
        }

        return (guild with { Roster = roster }, events);
    }

    // Fresh + light week ≈ 0; exhausted + heavy load climbs toward ~30% (first-pass).
    private static int ChancePct(int freshness, int raidDays)
    {
        int fatigue = Math.Max(0, 70 - freshness); // only bites below ~70 freshness
        int load = raidDays * 2;
        return Math.Clamp((fatigue / 4) + load, 0, 30);
    }
}
