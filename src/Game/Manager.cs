using System;
using System.Collections.Generic;
using System.Linq;
using Content;

namespace Game;

/// <summary>
/// The player's manager (GDD §2) — a person, not a hero: identity, chosen background, and the seven
/// attributes that gate the *quality of information and options* you get (never automate decisions).
/// </summary>
public sealed record Manager(string Name, int Age, string Region, string BackgroundId, IReadOnlyDictionary<string, int> Attributes)
{
    public int Of(string attributeId) => Attributes.TryGetValue(attributeId, out int v) ? v : ManagerProfile.BaseValue;
}

/// <summary>Builds a <see cref="Manager"/> from creation choices: baseline + background spread + point-buy.</summary>
public static class Managers
{
    public static Manager Create(string name, int age, string region, string backgroundId, IReadOnlyDictionary<string, int> pointBuy)
    {
        ArgumentNullException.ThrowIfNull(pointBuy);
        BackgroundDef background = ManagerProfile.Backgrounds.First(b => b.Id == backgroundId);

        var attributes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (ManagerAttributeDef attr in ManagerProfile.Attributes)
        {
            int value = ManagerProfile.BaseValue;
            value += background.Bonuses.TryGetValue(attr.Id, out int bonus) ? bonus : 0;
            value += pointBuy.TryGetValue(attr.Id, out int spent) ? spent : 0;
            attributes[attr.Id] = Math.Clamp(value, 1, ManagerProfile.MaxValue);
        }

        return new Manager(name, age, region, backgroundId, attributes);
    }
}
