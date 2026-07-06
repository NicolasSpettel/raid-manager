namespace Engine;

/// <summary>
/// A 2D point in fixed-point integer units (1000 units = one "yard"). Integer-only so every geometry
/// test is deterministic — no float ever touches the sim (engine-spec: determinism). The arena centres
/// on the boss at the origin; raiders spread around it. Distance checks compare squared magnitudes
/// (never a sqrt); only placing a point at an exact radius uses <see cref="IntSqrt"/>, which stays integer.
/// </summary>
public readonly record struct Position(int X, int Y)
{
    public static readonly Position Origin = new(0, 0);

    /// <summary>Squared distance — compared against a squared radius so we avoid the sqrt entirely.</summary>
    public long DistanceSquaredTo(Position other)
    {
        long dx = X - other.X;
        long dy = Y - other.Y;
        return (dx * dx) + (dy * dy);
    }

    /// <summary>True if this point lies within <paramref name="radius"/> of <paramref name="center"/>.</summary>
    public bool WithinRadius(Position center, int radius) =>
        DistanceSquaredTo(center) <= (long)radius * radius;

    /// <summary>Deterministic integer square root (floor) via integer Newton's method — never a float.</summary>
    public static int IntSqrt(long n)
    {
        if (n <= 0)
        {
            return 0;
        }

        long x = n;
        long y = (x + 1) / 2;
        while (y < x)
        {
            x = y;
            y = (x + (n / x)) / 2;
        }

        return (int)x;
    }
}
