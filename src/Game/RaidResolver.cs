using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>
/// Turns a finished raid into career progress: folds the combat event stream into per-raider
/// contributions, awards gold and XP (levelling up), drops loot on a win, and appends a
/// <see cref="RaidSummary"/> — the "career history is a fold" mechanism (BLUEPRINT §7, save-format.md).
/// Pure: returns a new <see cref="GuildSave"/>, mutating nothing.
/// </summary>
public static class RaidResolver
{
    private const int WinGold = 500;
    private const int WipeGold = 100;
    private const int WinBaseXp = 100;
    private const int WipeBaseXp = 40;
    private const int InjuryDuration = 2; // raids a fallen raider fights at reduced strength

    public static (GuildSave Guild, RaidSummary Summary) Resolve(
        GuildSave guild, SimResult result, EncounterDef encounter, ulong lootSeed)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(encounter);

        var raiderIds = guild.Roster.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        var damage = new Dictionary<string, int>(StringComparer.Ordinal);
        var healing = new Dictionary<string, int>(StringComparer.Ordinal);
        var died = new HashSet<string>(StringComparer.Ordinal);

        foreach (CombatEvent e in result.Events)
        {
            switch (e)
            {
                case Damage d when raiderIds.Contains(d.Source.Value):
                    damage[d.Source.Value] = damage.GetValueOrDefault(d.Source.Value) + d.Amount;
                    break;
                case Heal h when raiderIds.Contains(h.Source.Value):
                    healing[h.Source.Value] = healing.GetValueOrDefault(h.Source.Value) + h.Amount;
                    break;
                case Death dth when raiderIds.Contains(dth.Victim.Value):
                    died.Add(dth.Victim.Value);
                    break;
            }
        }

        bool win = result.Outcome == EncounterOutcome.Kill;
        int gold = win ? WinGold : WipeGold;
        int baseXp = win ? WinBaseXp : WipeBaseXp;

        var contributions = new List<RaiderContribution>(guild.Roster.Count);
        var newRoster = new List<RaiderRecord>(guild.Roster.Count);
        foreach (RaiderRecord raider in guild.Roster)
        {
            int dealt = damage.GetValueOrDefault(raider.Id);
            int healed = healing.GetValueOrDefault(raider.Id);
            bool fell = died.Contains(raider.Id);
            contributions.Add(new RaiderContribution(raider.Id, dealt, healed, fell));

            int gain = baseXp + ((dealt + healed) / 20);
            (int level, int xp) = ApplyXp(raider.Level, raider.Xp, gain);

            int injury = fell ? InjuryDuration : Math.Max(0, raider.InjuryRaidsLeft - 1); // hurt, or recovering
            newRoster.Add(raider with { Level = level, Xp = xp, InjuryRaidsLeft = injury });
        }

        string? lootDropped = win ? DropLoot(newRoster, encounter.Id, lootSeed) : null;

        int duration = result.Events.Count == 0 ? 0 : result.Events[^1].Tick.Value;
        var summary = new RaidSummary(encounter.Id, result.Outcome.ToString(), duration, gold, contributions, lootDropped);

        GuildSave updated = guild with
        {
            Roster = newRoster,
            Economy = new Economy(guild.Economy.Gold + gold),
            History = guild.History.Append(summary).ToList(),
        };

        return (updated, summary);
    }

    // Roll one item from the encounter's loot table and equip it to the neediest raider it upgrades.
    private static string? DropLoot(List<RaiderRecord> roster, string encounterId, ulong seed)
    {
        IReadOnlyList<ItemDef> pool = Loot.For(encounterId);
        if (pool.Count == 0)
        {
            return null;
        }

        ItemDef drop = pool[new SeededRng(seed).NextInt(pool.Count)];

        int recipient = -1;
        int lowestPower = int.MaxValue;
        for (int i = 0; i < roster.Count; i++)
        {
            bool isUpgrade = !ReferenceEquals(Warband.EquipIfUpgrade(roster[i], drop), roster[i]);
            if (isUpgrade)
            {
                int power = Warband.GearPower(roster[i]);
                if (power < lowestPower)
                {
                    lowestPower = power;
                    recipient = i;
                }
            }
        }

        if (recipient < 0)
        {
            return null; // nobody benefits
        }

        roster[recipient] = Warband.EquipIfUpgrade(roster[recipient], drop);
        return drop.Id;
    }

    private static (int Level, int Xp) ApplyXp(int level, int xp, int gain)
    {
        xp += gain;
        while (xp >= XpForNextLevel(level))
        {
            xp -= XpForNextLevel(level);
            level++;
        }

        return (level, xp);
    }

    private static int XpForNextLevel(int level) => 100 * level;
}
