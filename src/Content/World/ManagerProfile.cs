using System.Collections.Generic;

namespace Content;

/// <summary>One of the manager's own attributes (GDD §2) — they gate information/option quality, never press buttons.</summary>
public sealed record ManagerAttributeDef(string Id, string Name, string Blurb);

/// <summary>
/// A manager background (GDD §2): a story choice that seeds an attribute spread and a small perk, instead of
/// a blank stat-buy. Bonuses are keyed by manager-attribute id.
/// </summary>
public sealed record BackgroundDef(
    string Id, string Name, string Blurb, IReadOnlyDictionary<string, int> Bonuses, string Perk);

/// <summary>
/// The manager profile catalog (GDD §2, PROPOSED): the seven manager attributes and the starting
/// backgrounds. Data rows — rename/rebalance freely. Attributes sit on a 1–20 scale; creation starts
/// everyone at <see cref="BaseValue"/>, applies the chosen background, then spends <see cref="PointBuy"/>.
/// </summary>
public static class ManagerProfile
{
    public const int BaseValue = 8;
    public const int PointBuy = 10;
    public const int MaxValue = 20;

    public static IReadOnlyList<ManagerAttributeDef> Attributes { get; } = new[]
    {
        new ManagerAttributeDef("charm", "Charm", "Contract talks, media, recruit pitches."),
        new ManagerAttributeDef("leadership", "Leadership", "Raid-night discipline, fewer panic wipes."),
        new ManagerAttributeDef("tactics", "Tactics", "Faster boss-mechanic discovery, better strategy options."),
        new ManagerAttributeDef("motivation", "Motivation", "Morale recovery, softer benching fallout."),
        new ManagerAttributeDef("negotiation", "Negotiation", "Contracts, transfers, salary talks — both ways."),
        new ManagerAttributeDef("judgement", "Judgement", "How clearly you see a raider's true stats."),
        new ManagerAttributeDef("development", "Development", "Training efficiency, young-raider growth."),
    };

    public static IReadOnlyList<BackgroundDef> Backgrounds { get; } = new[]
    {
        new BackgroundDef("former_raider", "Former World-Class Raider",
            "Respect from high-star players; weaker at the admin side.",
            new Dictionary<string, int> { ["leadership"] = 3, ["tactics"] = 2, ["charm"] = 1, ["negotiation"] = -1 },
            "Star players give you the benefit of the doubt."),
        new BackgroundDef("guild_officer", "Guild Officer for Years",
            "Logistics and morale come naturally to you.",
            new Dictionary<string, int> { ["motivation"] = 3, ["leadership"] = 2, ["development"] = 1 },
            "The room's morale recovers faster under you."),
        new BackgroundDef("theorycrafter", "Theorycrafter",
            "You read a fight like a book; charm, less so.",
            new Dictionary<string, int> { ["tactics"] = 4, ["judgement"] = 2, ["charm"] = -2 },
            "Boss mechanics get discovered faster."),
        new BackgroundDef("rich_sponsor", "Rich Kid Sponsor",
            "Deep pockets, shallow reputation.",
            new Dictionary<string, int> { ["negotiation"] = 2, ["charm"] = 1 },
            "Your guild starts with extra funds."),
    };
}
