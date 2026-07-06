using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>
/// Hiring raiders — the gold sink that gives the economy a purpose and deepens the bench (which the
/// injury system makes matter). A recruit is generated deterministically from a seed: you pay the cost,
/// you get who you get (a random class), fresh at level 1.
/// </summary>
public static class Recruitment
{
    public const int Cost = 400;

    private static readonly string[] Names =
    {
        "Sigrun", "Osric", "Freya", "Halvar", "Ingrid", "Rurik", "Astrid", "Leif", "Dagny", "Ivar",
    };

    /// <summary>Hire a generated recruit if the guild can afford it. Returns false (and the guild unchanged) if broke.</summary>
    public static bool TryHire(GuildSave guild, ulong seed, out GuildSave updated)
    {
        ArgumentNullException.ThrowIfNull(guild);

        if (guild.Economy.Gold < Cost)
        {
            updated = guild;
            return false;
        }

        var rng = new SeededRng(seed);
        List<ClassDef> classes = Classes.Registry.All.ToList();
        ClassDef cls = classes[rng.NextInt(classes.Count)];
        string name = Names[rng.NextInt(Names.Length)];
        string id = "r:" + (guild.Roster.Count + 1).ToString("D4", CultureInfo.InvariantCulture);

        var recruit = new RaiderRecord(id, name, cls.Id, Equipped: new List<string>());

        updated = guild with
        {
            Roster = guild.Roster.Append(recruit).ToList(),
            Economy = new Economy(guild.Economy.Gold - Cost),
        };
        return true;
    }
}
