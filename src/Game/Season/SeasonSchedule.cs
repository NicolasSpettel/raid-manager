using System.Collections.Generic;
using System.Linq;

namespace Game;

/// <summary>What kind of thing is happening on the calendar (GDD §5).</summary>
public enum CalendarEventKind
{
    RaidOpen,     // a new portal/gate opens — the race begins
    WeeklyReset,  // Monday reset — lockout clears, farm re-opens
    Holiday,      // raiders expect the day(s) off (grant = lose raid time, deny = morale hit)
    SeasonEnd,
}

/// <summary>One dated entry on the season calendar.</summary>
public sealed record CalendarEvent(int Week, CalendarEventKind Kind, string Name);

/// <summary>
/// The season calendar (GDD §5): the timeline of events the player progresses through — the raid opening,
/// the weekly resets, the holidays (which force a grant-time-off-or-take-a-morale-hit dilemma), and the
/// season's end. Config-driven length; the placeholder holidays are the GDD's (rename freely).
/// </summary>
public sealed record SeasonSchedule(int LengthWeeks, IReadOnlyList<CalendarEvent> Events)
{
    /// <summary>Build the default season timeline for a season of <paramref name="lengthWeeks"/> weeks.</summary>
    public static SeasonSchedule Build(int lengthWeeks = 12, string raidName = "The Sundering")
    {
        var events = new List<CalendarEvent> { new(1, CalendarEventKind.RaidOpen, $"{raidName} opens") };

        for (int week = 1; week <= lengthWeeks; week++)
        {
            events.Add(new CalendarEvent(week, CalendarEventKind.WeeklyReset, "Weekly reset (Monday)"));
        }

        // Placeholder holidays (GDD §5), spaced through the season; raiders expect these off.
        AddHolidayIfInSeason(events, lengthWeeks / 3, "Longnight");          // winter solstice — the big one
        AddHolidayIfInSeason(events, (lengthWeeks * 2) / 3, "Ancestors' Vigil"); // a somber remembrance day

        events.Add(new CalendarEvent(lengthWeeks, CalendarEventKind.SeasonEnd, "Season ends"));

        return new SeasonSchedule(lengthWeeks, events.OrderBy(e => e.Week).ThenBy(e => (int)e.Kind).ToList());
    }

    /// <summary>The next few events at or after <paramref name="fromWeek"/> (excluding the routine weekly reset noise).</summary>
    public IEnumerable<CalendarEvent> Upcoming(int fromWeek, int count = 3) => Events
        .Where(e => e.Week >= fromWeek && e.Kind != CalendarEventKind.WeeklyReset)
        .OrderBy(e => e.Week)
        .Take(count);

    /// <summary>The holiday in a given week, if any (raiders expect it off).</summary>
    public CalendarEvent? HolidayIn(int week) =>
        Events.FirstOrDefault(e => e.Week == week && e.Kind == CalendarEventKind.Holiday);

    private static void AddHolidayIfInSeason(List<CalendarEvent> events, int week, string name)
    {
        if (week >= 1)
        {
            events.Add(new CalendarEvent(week, CalendarEventKind.Holiday, name));
        }
    }
}
