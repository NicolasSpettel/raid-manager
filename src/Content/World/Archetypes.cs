using System.Collections.Generic;

namespace Content;

/// <summary>
/// The archetype catalog (entities §3.1). Quality/personality profiles, not roles — the slot's role need
/// is decided later in the pipeline. Prestige weights (order: WorldElite, Continental, National, Local)
/// bias who shows up where: elite rosters skew prodigy/veteran, local rosters skew journeyman. First-pass
/// distributions — tune freely.
/// </summary>
public static class Archetypes
{
    public static ArchetypeRegistry Registry { get; } = new(new List<ArchetypeDef>
    {
        new("rising_prodigy", "Rising Prodigy",
            new LatentProfile(Talent: 82, Discipline: 45, Experience: 20, Volatility: 60), LatentSpread: 12,
            AgeMean: 18, AgeSpread: 2, PrestigeWeights: new[] { 30, 26, 20, 12 }),

        new("disciplined_veteran", "Disciplined Veteran",
            new LatentProfile(Talent: 55, Discipline: 82, Experience: 86, Volatility: 18), LatentSpread: 10,
            AgeMean: 28, AgeSpread: 2, PrestigeWeights: new[] { 38, 30, 22, 12 }),

        new("journeyman_filler", "Journeyman",
            new LatentProfile(Talent: 45, Discipline: 50, Experience: 55, Volatility: 45), LatentSpread: 14,
            AgeMean: 24, AgeSpread: 3, PrestigeWeights: new[] { 10, 24, 40, 56 }),

        new("volatile_carry", "Volatile Carry",
            new LatentProfile(Talent: 80, Discipline: 34, Experience: 42, Volatility: 82), LatentSpread: 14,
            AgeMean: 22, AgeSpread: 3, PrestigeWeights: new[] { 12, 12, 10, 12 }),

        new("burnout_risk_star", "Burnout-Risk Star",
            new LatentProfile(Talent: 84, Discipline: 56, Experience: 66, Volatility: 68), LatentSpread: 11,
            AgeMean: 25, AgeSpread: 3, PrestigeWeights: new[] { 10, 8, 8, 8 }),
    });
}
