using System;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// <summary>
/// Between-raid actions. Resting heals injuries without fighting — the management choice of "raid now"
/// vs "let them recover", and the seed of the day/time layer (GDD §5). Pure: returns a new guild.
/// </summary>
public static class Downtime
{
    /// <summary>Advance a rest period: every injured raider recovers one step.</summary>
    public static GuildSave Rest(GuildSave guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        List<RaiderRecord> roster = guild.Roster
            .Select(r => r.InjuryRaidsLeft > 0 ? r with { InjuryRaidsLeft = r.InjuryRaidsLeft - 1 } : r)
            .ToList();

        return guild with { Roster = roster };
    }
}
