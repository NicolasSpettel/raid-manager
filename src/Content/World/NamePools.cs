using System.Collections.Generic;

namespace Content;

/// <summary>A region's name pool (entities §3 IdentityGen). Names are drawn deterministically per raider.</summary>
public sealed record NamePool(string Region, IReadOnlyList<string> First, IReadOnlyList<string> Surnames);

/// <summary>
/// Regional name pools — flavour source for generated raiders (working names; final sourcing/uniqueness
/// policy is [OPEN] in entities §9). Each region has its own first names and surnames so a roster reads
/// as coming from somewhere.
/// </summary>
public static class NamePools
{
    public static IReadOnlyList<NamePool> All { get; } = new List<NamePool>
    {
        new("Ironreach",
            new[] { "Bran", "Doran", "Kael", "Aldric", "Torvald", "Merrick", "Halvard", "Osric", "Rurik", "Gunnar", "Edric", "Wulfric" },
            new[] { "Stonehand", "Ironford", "Blackfell", "Grimmel", "Hawthorne", "Ardent", "Vance", "Draymoor" }),

        new("Sunmere",
            new[] { "Elowen", "Cael", "Lysa", "Aveline", "Rowan", "Sable", "Ferran", "Isolde", "Marek", "Nadia", "Perrin", "Selene" },
            new[] { "Ashdown", "Sunmere", "Larkspur", "Vael", "Duskwood", "Fairwind", "Rell", "Marlowe" }),

        new("Frosthold",
            new[] { "Sigrun", "Halla", "Ivar", "Yrsa", "Bjorn", "Astrid", "Sten", "Frida", "Ulf", "Runa", "Egil", "Solveig" },
            new[] { "Frostborn", "Wintermere", "Skarsgard", "Icevale", "Thornkeep", "Bleakmoor", "Holt", "Varga" }),

        new("Ashvale",
            new[] { "Cassian", "Vira", "Dorian", "Mira", "Alaric", "Thea", "Corvin", "Elara", "Roderic", "Nyra", "Silas", "Wren" },
            new[] { "Emberfell", "Ashvale", "Cinderly", "Duskbane", "Ravenwood", "Solmer", "Kestrel", "Voss" }),
    };
}
