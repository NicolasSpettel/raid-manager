using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Content;
using Engine;

namespace Game;

/// <summary>
/// Builds a fresh guild + roster — the createGuild factory (BLUEPRINT §10). Deterministic: the roster
/// is generated from a required seed, and the timestamp is passed in (Game never reads wall-clock time —
/// that would break the determinism guard; the app supplies it).
/// </summary>
public static class Guilds
{
    private static readonly string[] NamePool =
    {
        "Thara", "Bolvar", "Sylvie", "Krain", "Mira", "Doran", "Vesh", "Aldric",
        "Nyla", "Corin", "Brann", "Elowen", "Torvald", "Isolde", "Garruk", "Wren",
    };

    /// <summary>Create a starter guild with a deterministically generated roster drawn from the class registry.</summary>
    public static GuildSave CreateStarter(string guildName, ulong seed, string createdAtIso, int rosterSize = 8)
    {
        var rng = new SeededRng(seed);
        List<ClassDef> classes = Classes.Registry.All.ToList();

        var roster = new List<RaiderRecord>(rosterSize);
        for (int i = 0; i < rosterSize; i++)
        {
            // Seed the roster with one of each class (guarantees role coverage), then fill randomly.
            ClassDef cls = i < classes.Count ? classes[i] : classes[rng.NextInt(classes.Count)];
            string name = NamePool[rng.NextInt(NamePool.Length)];
            string id = "r:" + (i + 1).ToString("D4", CultureInfo.InvariantCulture);
            roster.Add(new RaiderRecord(id, name, cls.Id, Equipped: new List<string>()));
        }

        return new GuildSave(
            SaveMigrations.CurrentVersion,
            createdAtIso,
            new GuildInfo(guildName, Reputation: 0),
            roster,
            new Economy(Gold: 1000),
            new List<RaidSummary>());
    }
}
