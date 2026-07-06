using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game;

/// <summary>The result of running a campaign: the final guild and every raid's summary.</summary>
public sealed record CampaignResult(GuildSave Guild, IReadOnlyList<RaidSummary> Raids)
{
    public int Wins => Raids.Count(r => r.Outcome == nameof(EncounterOutcome.Kill));
}

/// <summary>
/// Runs the real management loop headlessly: project the roster → fight → resolve → repeat, feeding
/// each raid's rewards back into the guild. The `sim campaign` verb and the campaign tests both call
/// this — one loop, no re-implemented mirror (testing-strategy §5, BLUEPRINT §4 "the mirror rule").
/// </summary>
public static class Campaign
{
    public static CampaignResult Run(GuildSave start, int raids, ulong seed, EncounterDef encounter)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(encounter);

        GuildSave guild = start;
        var summaries = new List<RaidSummary>(raids);
        for (int i = 1; i <= raids; i++)
        {
            ulong raidSeed = seed + (ulong)i;
            var setup = new RaidSetup(guild.Roster.Select(Warband.ToCombatant).ToList());
            SimResult result = Simulator.SimulateEncounter(
                new SimInput(new SeededRng(raidSeed), SimConfig.Default, setup, encounter));

            (GuildSave next, RaidSummary summary) = RaidResolver.Resolve(guild, result, encounter, raidSeed);
            guild = next;
            summaries.Add(summary);
        }

        return new CampaignResult(guild, summaries);
    }
}
