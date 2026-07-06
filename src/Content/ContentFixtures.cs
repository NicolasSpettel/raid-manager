using System.Collections.Generic;
using Engine;

namespace Content;

/// <summary>
/// Content-level fixtures that need the class/ability/encounter catalogs (the Sim CLI tries these
/// after the engine's own fixtures). This is a raid of authored classes versus an authored boss —
/// the whole content pipeline end to end.
/// </summary>
public static class ContentFixtures
{
    public static SimInput? ByName(string name, ulong seed) => name switch
    {
        "classraid" => ClassRaid(seed),
        _ => null,
    };

    /// <summary>The four M1 classes, built from data via the factory, versus the authored Warden encounter.</summary>
    public static SimInput ClassRaid(ulong seed)
    {
        var raid = new RaidSetup(new List<CombatantSpec>
        {
            Roster.CreateRaider(Classes.Guardian, "r:guardian", "Guardian"),
            Roster.CreateRaider(Classes.Cleric, "r:cleric", "Cleric"),
            Roster.CreateRaider(Classes.Blademaster, "r:blademaster", "Blademaster"),
            Roster.CreateRaider(Classes.Pyromancer, "r:pyromancer", "Pyromancer"),
        });

        return new SimInput(new SeededRng(seed), SimConfig.Default, raid, Encounters.Warden);
    }
}
