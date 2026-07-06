using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Content;

/// <summary>Encounter difficulty. The mechanism is a data multiplier (GDD §14, design-status §10).</summary>
public enum Difficulty
{
    Normal,
    Heroic,
    Mythic,
}

/// <summary>
/// Scales an authored encounter to a difficulty — boss HP, boss damage, and mechanic amounts, by an
/// integer percent (deterministic). Same encounter id (so loot tables still resolve); only the name
/// carries the tier for display. Mechanic *additions* per tier are a later pass.
/// </summary>
public static class Difficulties
{
    public static IReadOnlyList<Difficulty> All { get; } = new[] { Difficulty.Normal, Difficulty.Heroic, Difficulty.Mythic };

    public static EncounterDef Scale(EncounterDef encounter, Difficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        (int hpPct, int dmgPct) = Multipliers(difficulty);
        if (hpPct == 100 && dmgPct == 100)
        {
            return encounter;
        }

        var enemies = encounter.Enemies
            .Select(e => e with
            {
                Stats = e.Stats with
                {
                    MaxHp = e.Stats.MaxHp * hpPct / 100,
                    AttackDamage = e.Stats.AttackDamage * dmgPct / 100,
                },
            })
            .ToList();

        List<MechanicInstance>? timeline = encounter.Timeline?
            .Select(m => m with { Amount = m.Amount * dmgPct / 100 })
            .ToList();

        return encounter with
        {
            Name = $"{encounter.Name} ({difficulty})",
            Enemies = enemies,
            Timeline = timeline,
        };
    }

    private static (int HpPct, int DmgPct) Multipliers(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Heroic => (155, 135),
        Difficulty.Mythic => (230, 175),
        _ => (100, 100),
    };
}
