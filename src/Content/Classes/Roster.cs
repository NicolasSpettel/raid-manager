using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Content;

/// <summary>Builds ready-to-fight raiders from classes — the createRaider factory (BLUEPRINT §10).</summary>
public static class Roster
{
    /// <summary>Instantiate a raider from a class: resolve its kit against the ability registry.</summary>
    public static CombatantSpec CreateRaider(ClassDef cls, string id, string name)
    {
        ArgumentNullException.ThrowIfNull(cls);
        IReadOnlyList<AbilityDef> kit = cls.Kit.Select(Abilities.Registry.Def).ToList();
        return new CombatantSpec(
            new CombatantId(id), CombatantKind.Raider, Side.Raid, cls.Role, name, cls.BaseStats, kit);
    }
}
