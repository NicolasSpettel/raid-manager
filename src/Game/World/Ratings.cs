using System;
using System.Collections.Generic;
using Content;
using Engine;

namespace Game;

/// <summary>
/// Derived raider ratings. Stars and the headline overall are <b>functions</b> of attributes + class fit,
/// computed on read and never stored (entities §3.3), so nothing can drift. Stars are expressed in
/// half-steps (2–10 = 1.0★–5.0★, matching the GDD's 0.5-step per-role rating), capped by hidden potential.
/// </summary>
public static class Ratings
{
    /// <summary>Fixed iteration order so "best role" ties break deterministically.</summary>
    public static readonly IReadOnlyList<CombatantRole> AllRoles =
        new[] { CombatantRole.Tank, CombatantRole.Healer, CombatantRole.Melee, CombatantRole.Ranged };

    // Which attributes gate competence in each role (first-pass mapping; registry ids).
    private static readonly IReadOnlyDictionary<CombatantRole, string[]> RoleKeyAttributes =
        new Dictionary<CombatantRole, string[]>
        {
            [CombatantRole.Tank] = new[] { "composure", "cooldown_discipline", "awareness", "endurance" },
            [CombatantRole.Healer] = new[] { "resource_control", "awareness", "composure", "consistency" },
            [CombatantRole.Melee] = new[] { "mechanics", "consistency", "endurance", "cooldown_discipline" },
            [CombatantRole.Ranged] = new[] { "mechanics", "consistency", "awareness", "resource_control" },
        };

    /// <summary>Half-stars (2–10 = 1.0★–5.0★) for one role, capped by the raider's hidden potential in it.</summary>
    public static int HalfStars(RaiderRecord raider, CombatantRole role)
    {
        string[] keys = RoleKeyAttributes[role];
        int sum = 0;
        foreach (string key in keys)
        {
            sum += raider.Attributes?.Of(key) ?? 10;
        }

        int avgX10 = (sum * 10) / keys.Length;                 // mean attribute ×10 (integer math)
        int half = 2 + (((avgX10 - 50) * 8) / (180 - 50));     // map mean [5..18] → half-stars [2..10]

        string classId = raider.Vocation?.ClassId ?? raider.ClassId;
        if (NaturalRole(classId) == role)
        {
            half += 1; // a raider is sharper in their own class's role
        }

        int potential = raider.Vocation is { } v && v.PotentialByRole.TryGetValue(role, out int p) ? p : 100;
        return Math.Clamp(half, 2, Math.Min(10, PotentialCap(potential)));
    }

    /// <summary>The raider's strongest role and its half-stars — their headline rating.</summary>
    public static (CombatantRole Role, int HalfStars) Best(RaiderRecord raider)
    {
        CombatantRole bestRole = CombatantRole.Melee;
        int bestHalf = 0;
        foreach (CombatantRole role in AllRoles)
        {
            int half = HalfStars(raider, role);
            if (half > bestHalf)
            {
                bestHalf = half;
                bestRole = role;
            }
        }

        return (bestRole, bestHalf);
    }

    /// <summary>Format half-stars as a display string, e.g. 7 → "3.5★".</summary>
    public static string Format(int halfStars) => $"{halfStars / 2.0:0.0}★";

    private static int PotentialCap(int potential) => Math.Clamp(2 + ((potential * 8) / 100), 2, 10);

    private static CombatantRole NaturalRole(string classId) => Classes.Registry.Get(classId).Role;
}
