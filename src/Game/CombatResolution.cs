using Content;
using Engine;

namespace Game;

/// <summary>
/// Bridges the raider attribute model (GDD §8) to the engine's combat inputs, per the LOCKED §8a′
/// resolution: scalar attributes become small percent multipliers (~±1%/point), behavioural ones become
/// the base for the engine's seeded decisions. The engine stays registry-agnostic — this is the only place
/// that names an attribute.
///
/// The <b>mapping</b> below (which of the FM §8 attributes drives Damage/Defense/Movement) is a first pass:
/// §8a′ describes an execution-flavoured attribute set (Damage/Defense/Movement/…), while the §8 list is the
/// softer FM one, and reconciling the two is still an [OPEN] design call. Neutral (all 10) resolves to
/// 100/100/10 — no combat change — so this only bites once raiders have real spread.
/// </summary>
public static class CombatResolution
{
    private const int PerPointPct = 1; // §8a′: ≈ ±1% output per attribute point (tuning knob)

    public static CombatAttributes Resolve(AttributeVector? attributes)
    {
        if (attributes is null)
        {
            return CombatAttributes.Neutral;
        }

        int damageMult = 100 + ((attributes.Of("consistency") - 10) * PerPointPct);  // realized output
        int defenseMult = 100 - ((attributes.Of("composure") - 10) * PerPointPct);    // steadier → takes less
        int movementSkill = attributes.Of("awareness");                               // reacts to mechanics → dodges

        return new CombatAttributes(damageMult, defenseMult, movementSkill);
    }
}
