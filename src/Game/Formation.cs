using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game;

/// <summary>
/// Places a raid on the arena before a fight: the boss stands at the origin, tanks form the front rank,
/// melee the middle, healers/ranged the back — a simple ranked formation so spatial mechanics (void
/// zones) have room to matter and the stage view shows a real layout. Coordinates are engine fixed-point
/// units (1000 = one yard). Encounters without spatial mechanics simply ignore the positions.
/// </summary>
public static class Formation
{
    private static readonly int[] RowY = { 2000, 4200, 6500 }; // front (tanks), middle (melee), back (heal/ranged)
    private const int ColumnSpacing = 2400;

    /// <summary>Return the raiders with a <see cref="CombatantSpec.SpawnPosition"/> assigned by rank.</summary>
    public static IReadOnlyList<CombatantSpec> Place(IReadOnlyList<CombatantSpec> raiders)
    {
        var rows = new List<CombatantSpec>[] { new(), new(), new() };
        foreach (CombatantSpec r in raiders)
        {
            rows[RowOf(r.Role)].Add(r);
        }

        var positions = new Dictionary<CombatantId, Position>();
        for (int row = 0; row < rows.Length; row++)
        {
            List<CombatantSpec> members = rows[row];
            for (int i = 0; i < members.Count; i++)
            {
                int x = ((2 * i) - (members.Count - 1)) * (ColumnSpacing / 2); // symmetric, centred on 0
                positions[members[i].Id] = new Position(x, RowY[row]);
            }
        }

        // Preserve the original roster order; only the spawn position changes.
        return raiders.Select(r => r with { SpawnPosition = positions[r.Id] }).ToList();
    }

    private static int RowOf(CombatantRole role) => role switch
    {
        CombatantRole.Tank => 0,
        CombatantRole.Melee => 1,
        _ => 2, // healer / ranged
    };
}
