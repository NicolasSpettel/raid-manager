using System.Collections.Generic;

namespace Content;

/// <summary>
/// The raider attribute catalog — the GDD §8 proposed list (11; final set [OPEN], registry-flexible so
/// trimming to ~10 or merging is one edit). Each row declares its latent loadings (entities §3.2); the
/// weights below are a coherent first pass (twitch stats lean on talent, wisdom stats on experience/
/// discipline, Consistency/Composure penalise volatility). Tune freely.
/// </summary>
public static class Attributes
{
    public static AttributeRegistry Registry { get; } = new(new List<AttributeDef>
    {
        new("mechanics", "Mechanics", AttributeKind.Behavioral, AgingClass.Twitch,
            new LatentLoading(Talent: 16, Discipline: 6)),
        new("awareness", "Awareness", AttributeKind.Behavioral, AgingClass.Twitch,
            new LatentLoading(Talent: 18, Volatility: 2)),
        new("cooldown_discipline", "Cooldown Discipline", AttributeKind.Behavioral, AgingClass.Neutral,
            new LatentLoading(Discipline: 16, Experience: 6)),
        new("resource_control", "Resource Control", AttributeKind.Scalar, AgingClass.Neutral,
            new LatentLoading(Discipline: 14, Experience: 6)),
        new("consistency", "Consistency", AttributeKind.Scalar, AgingClass.Neutral,
            new LatentLoading(Discipline: 12, Volatility: -14)),
        new("composure", "Composure", AttributeKind.Behavioral, AgingClass.Wisdom,
            new LatentLoading(Discipline: 8, Experience: 12, Volatility: -10)),
        new("communication", "Communication", AttributeKind.Scalar, AgingClass.Wisdom,
            new LatentLoading(Experience: 14, Discipline: 6)),
        new("learning", "Learning", AttributeKind.Scalar, AgingClass.Twitch,
            new LatentLoading(Talent: 14, Volatility: 4)),
        new("preparation", "Preparation", AttributeKind.Scalar, AgingClass.Wisdom,
            new LatentLoading(Discipline: 16, Experience: 6)),
        new("teamplay", "Teamplay", AttributeKind.Scalar, AgingClass.Wisdom,
            new LatentLoading(Discipline: 8, Experience: 8, Volatility: -8)),
        new("endurance", "Endurance", AttributeKind.Scalar, AgingClass.Twitch,
            new LatentLoading(Talent: 10, Discipline: 8)),
    });
}
