using System.Collections.Generic;

namespace Game;

/// <summary>One boss on the season's raid: a name and an abstract difficulty (progress needed to down it).</summary>
public sealed record SeasonBoss(string Name, int Difficulty);

/// <summary>
/// The season's raid — an ordered boss ladder every guild in the world races through under the same rules
/// (GDD §5). Difficulties are a tunable first pass, calibrated with the strength model so an elite guild
/// clears in ~3 weeks and a mid-pack guild in ~3 months (the locked pacing target).
/// </summary>
public sealed record SeasonRaid(string Name, IReadOnlyList<SeasonBoss> Bosses)
{
    /// <summary>The default season raid — the four authored bosses as an increasing difficulty ladder.</summary>
    public static SeasonRaid Default { get; } = new("The Sundering", new[]
    {
        new SeasonBoss("The Warden", 10),
        new SeasonBoss("The Sentinel", 18),
        new SeasonBoss("The Ashen King", 28),
        new SeasonBoss("The Frostwarden", 44),
    });
}
