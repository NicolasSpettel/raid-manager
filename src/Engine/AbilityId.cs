namespace Engine;

/// <summary>Stable id of an ability (e.g. <c>"mage.fireball"</c>). Authored in Content, executed here.</summary>
public readonly record struct AbilityId(string Value)
{
    public override string ToString() => Value;
}
