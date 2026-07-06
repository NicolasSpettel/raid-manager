using System.Globalization;

namespace Engine;

/// <summary>Fixed integer battle time. 10 ticks = 1 second (ADR-0003). No float time, ever.</summary>
public static class TimeModel
{
    public const int TicksPerSecond = 10;

    public static int SecondsToTicks(int seconds) => seconds * TicksPerSecond;
}

/// <summary>A point on the battle timeline, in integer ticks.</summary>
public readonly record struct Tick(int Value) : IComparable<Tick>
{
    public static Tick Zero => new(0);

    public Tick Plus(int ticks) => new(Value + ticks);

    public int CompareTo(Tick other) => Value.CompareTo(other.Value);

    public static bool operator <(Tick a, Tick b) => a.Value < b.Value;

    public static bool operator >(Tick a, Tick b) => a.Value > b.Value;

    public static bool operator <=(Tick a, Tick b) => a.Value <= b.Value;

    public static bool operator >=(Tick a, Tick b) => a.Value >= b.Value;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
