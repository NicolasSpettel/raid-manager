using System.Collections.Generic;
using System.Linq;
using Content;

namespace Game;

/// <summary>
/// Off-day training (GDD §6/§8): one targeted development session raises a raider's single weakest attribute
/// by a point. Dungeons (the other off-day activity — gear catch-up) are executed per-group in
/// <see cref="WeekExecutor"/>. First-pass; true potential-bounding needs the Vocation component.
/// </summary>
public static class WeeklyActivities
{
    private const int TrainCap = 18; // trained up to here (headroom below 20)

    /// <summary>Raise the raider's single lowest (sub-cap) attribute by one — one targeted training session.</summary>
    public static RaiderRecord Train(RaiderRecord raider)
    {
        if (raider.Attributes is null)
        {
            return raider;
        }

        string? target = null;
        int lowest = int.MaxValue;
        foreach (AttributeDef a in Attributes.Registry.All)
        {
            int value = raider.Attributes.Of(a.Id);
            if (value < TrainCap && value < lowest)
            {
                lowest = value;
                target = a.Id;
            }
        }

        if (target is null)
        {
            return raider; // already at the cap everywhere
        }

        var values = Attributes.Registry.All.ToDictionary(a => a.Id, a => raider.Attributes.Of(a.Id), System.StringComparer.Ordinal);
        values[target] += 1;
        return raider with { Attributes = new AttributeVector(values) };
    }
}
