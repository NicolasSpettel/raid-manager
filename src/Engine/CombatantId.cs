using System.Globalization;

namespace Engine;

/// <summary>Stable identity of a combatant, referenced by every event in the stream.</summary>
public readonly record struct CombatantId(int Value)
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
