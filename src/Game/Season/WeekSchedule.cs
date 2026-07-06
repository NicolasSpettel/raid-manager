using System;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// <summary>The seven days of the raid week (weekly reset falls on Monday, GDD §5).</summary>
public enum Weekday
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday,
}

/// <summary>What a group/individual is doing. Each day has four 2h slots (GDD §6); activities cost slots.</summary>
public enum ActivityType
{
    Raid,     // the raid group — a raid night is 3 slots
    Dungeon,  // a 5-man group — 1 slot (gear catch-up + light training)
    Training, // targeted attribute work for the assigned raiders — 1 slot
    Rest,     // recover (empty slots)
}

/// <summary>
/// One scheduled activity: who is doing it, on which day, and (for dungeons/training) the target. This is
/// the planning granularity the FM week is about (GDD §6, "per raider or per group") — you assign a 5-man
/// dungeon group here, or send individuals to training.
/// </summary>
public sealed record Assignment(Weekday Day, ActivityType Type, IReadOnlyList<string> RaiderIds, string? Target = null)
{
    /// <summary>Day-slots this activity consumes (of the 4 per day). Raid night = 3, dungeon/training = 1.</summary>
    public int Slots => Type switch
    {
        ActivityType.Raid => 3,
        ActivityType.Dungeon => 1,
        ActivityType.Training => 1,
        _ => 0,
    };
}

/// <summary>
/// A planned week — the list of assignments across the seven days. The player builds/edits this (assign
/// groups, send people to train, leave days to rest); <see cref="WeekPlanner"/> can fill it in one click.
/// </summary>
public sealed record WeekSchedule(IReadOnlyList<Assignment> Assignments)
{
    public static WeekSchedule Empty => new(Array.Empty<Assignment>());

    /// <summary>Distinct days that hold a raid night.</summary>
    public IReadOnlyList<Weekday> RaidDays => Assignments
        .Where(a => a.Type == ActivityType.Raid)
        .Select(a => a.Day)
        .Distinct()
        .OrderBy(d => (int)d)
        .ToList();

    /// <summary>Total day-slots a raider is booked for this week — their load (drives condition drain).</summary>
    public int SlotsFor(string raiderId) => Assignments
        .Where(a => a.RaiderIds.Contains(raiderId))
        .Sum(a => a.Slots);

    /// <summary>True if the raider is in at least one raid this week (else they're benched → rusty).</summary>
    public bool Raided(string raiderId) => Assignments.Any(a => a.Type == ActivityType.Raid && a.RaiderIds.Contains(raiderId));

    /// <summary>True if the raider has no assignment at all this week.</summary>
    public bool Benched(string raiderId) => !Assignments.Any(a => a.RaiderIds.Contains(raiderId));

    public WeekSchedule With(Assignment assignment) => new(Assignments.Append(assignment).ToList());
}

/// <summary>
/// "Plan my week" (GDD §6b, LOCKED one-button) — fills a <see cref="WeekSchedule"/> from a coverage brief,
/// assigning raid nights to the whole group and rotating 5-man dungeon groups + training onto the off days.
/// It writes the same granular assignments the player would, so the plan stays fully editable.
/// </summary>
public static class WeekPlanner
{
    public static WeekSchedule Auto(GuildSave guild, int raidDays, int dungeonDays, int trainDays)
    {
        ArgumentNullException.ThrowIfNull(guild);
        Weekday[] days = Enum.GetValues<Weekday>();
        List<string> roster = guild.Roster.Select(r => r.Id).ToList();

        var assignments = new List<Assignment>();
        int day = 0;

        for (int i = 0; i < raidDays && day < days.Length; i++, day++)
        {
            assignments.Add(new Assignment(days[day], ActivityType.Raid, roster));
        }

        for (int i = 0; i < dungeonDays && day < days.Length; i++, day++)
        {
            IReadOnlyList<string> group = FiveManGroup(roster, i);
            assignments.Add(new Assignment(days[day], ActivityType.Dungeon, group, "dungeon"));
        }

        for (int i = 0; i < trainDays && day < days.Length; i++, day++)
        {
            assignments.Add(new Assignment(days[day], ActivityType.Training, roster.Take(3).ToList()));
        }

        return new WeekSchedule(assignments); // remaining days rest (no assignment)
    }

    private static List<string> FiveManGroup(List<string> roster, int rotation)
    {
        if (roster.Count <= 5)
        {
            return roster;
        }

        int start = (rotation * 5) % roster.Count;
        return Enumerable.Range(0, 5).Select(k => roster[(start + k) % roster.Count]).ToList();
    }
}
