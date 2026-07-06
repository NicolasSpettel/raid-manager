using System;
using System.Collections.Generic;

namespace Game;

/// <summary>
/// The season clock (GDD §5, LOCKED — template-driven, config not hardcoded). A season is a fixed number
/// of weeks; each advance is a new **weekly lockout** period (the Monday reset). Real-calendar dates are a
/// presentation concern layered on later; the model is the week counter + season length.
/// </summary>
public sealed record SeasonCalendar(int SeasonLengthWeeks, int CurrentWeek)
{
    public static SeasonCalendar Start(int seasonLengthWeeks = 12) => new(seasonLengthWeeks, 1);

    public bool SeasonOver => CurrentWeek > SeasonLengthWeeks;

    /// <summary>Advance to next week — a fresh lockout (weekly reset).</summary>
    public SeasonCalendar Advance() => this with { CurrentWeek = CurrentWeek + 1 };
}

/// <summary>
/// The weekly lockout (GDD §5, LOCKED — "bosses killed this week stay dead until weekly reset; loot once
/// per boss per week"). Tracks which bosses were downed this reset so they aren't re-looted; a new week
/// starts <see cref="Empty"/> (the reset), re-opening them for farm.
/// </summary>
public sealed record Lockout(IReadOnlySet<string> DownedBosses)
{
    public static Lockout Empty => new(new HashSet<string>(StringComparer.Ordinal));

    public bool IsDowned(string bossId) => DownedBosses.Contains(bossId);

    public Lockout WithDowned(string bossId)
    {
        var next = new HashSet<string>(DownedBosses, StringComparer.Ordinal) { bossId };
        return new Lockout(next);
    }
}
