namespace Engine;

/// <summary>
/// Stable identity of a combatant, referenced by every event in the stream. Readable string ids
/// (<c>"r:thara"</c>, <c>"boss:main"</c>, <c>"add:wave2-3"</c>) make logs and replays legible and
/// keep references stable across the encounter (engine-spec §4).
/// </summary>
public readonly record struct CombatantId(string Value)
{
    public override string ToString() => Value;
}
