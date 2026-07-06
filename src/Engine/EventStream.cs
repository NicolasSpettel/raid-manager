using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Engine;

/// <summary>
/// Canonical, deterministic text serialization of the event stream, plus a stable content hash.
/// This is what golden tests pin. Formatting is invariant-culture and newline-fixed ('\n') so the
/// hash never depends on OS, locale, or git checkout settings.
/// </summary>
public static class EventStream
{
    /// <summary>One canonical line per event, newline-separated.</summary>
    public static string Serialize(IReadOnlyList<CombatEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var sb = new StringBuilder();
        foreach (CombatEvent e in events)
        {
            sb.Append(FormatLine(e)).Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>Stable 64-bit FNV-1a hash of the serialized stream, as 16 lowercase hex digits.</summary>
    public static string Hash(IReadOnlyList<CombatEvent> events)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        ulong hash = offsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes(Serialize(events)))
        {
            hash ^= b;
            hash *= prime;
        }

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    private static string FormatLine(CombatEvent e) => e switch
    {
        EncounterStart s => $"START t={s.Tick} encounter={s.EncounterId}",
        CastStart c => $"CAST  t={c.Tick} src={c.Source} ability={c.Ability} dur={Int(c.DurationTicks)} dst={c.Target}",
        CastEnd c => $"CEND  t={c.Tick} src={c.Source} ability={c.Ability} result={c.Result}",
        Damage d => FormatDamage(d),
        Death d => $"DEATH t={d.Tick} victim={d.Victim}",
        EncounterEnd x => $"END   t={x.Tick} outcome={x.Outcome}",
        _ => throw new NotSupportedException($"Unknown event type: {e.GetType().Name}"),
    };

    // The ability token appears only for ability damage, so weapon auto-attack lines are unchanged.
    private static string FormatDamage(Damage d) => d.Ability is { } ability
        ? $"DMG   t={d.Tick} src={d.Source} dst={d.Target} ability={ability} amount={Int(d.Amount)}"
        : $"DMG   t={d.Tick} src={d.Source} dst={d.Target} amount={Int(d.Amount)}";

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);
}
