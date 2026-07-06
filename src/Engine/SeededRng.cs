namespace Engine;

/// <summary>
/// Deterministic pseudo-random generator (PCG-XSH-RR 64/32). The single source of randomness in the
/// sim. The seed is required — there is no parameterless constructor — so a nondeterministic path
/// cannot exist by construction (BLUEPRINT §5). Pure integer math: the same seed yields an identical
/// sequence on every machine, forever.
/// </summary>
public sealed class SeededRng
{
    private const ulong Multiplier = 6364136223846793005UL;

    private ulong _state;
    private readonly ulong _inc;

    /// <summary>The seed this generator was created with (kept for provenance in <see cref="SimResult"/>).</summary>
    public ulong Seed { get; }

    /// <param name="seed">Required. The same seed yields the same stream.</param>
    /// <param name="stream">
    /// Optional independent stream selector — two generators with the same seed but different
    /// streams never overlap. Lets subsystems draw from separate, still-deterministic sequences.
    /// </param>
    public SeededRng(ulong seed, ulong stream = 0UL)
    {
        Seed = seed;
        _inc = (stream << 1) | 1UL;
        _state = 0UL;
        NextUInt();
        _state += seed;
        NextUInt();
    }

    /// <summary>Next 32-bit value, uniform across the whole range.</summary>
    public uint NextUInt()
    {
        ulong old = _state;
        _state = (old * Multiplier) + _inc;
        uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
    }

    /// <summary>Uniform integer in [0, <paramref name="exclusiveMax"/>). Rejection-sampled: no modulo bias.</summary>
    public int NextInt(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax), exclusiveMax, "must be positive");
        }

        uint bound = (uint)exclusiveMax;
        uint threshold = (uint)((0x1_0000_0000UL - bound) % bound); // 2^32 mod bound
        while (true)
        {
            uint r = NextUInt();
            if (r >= threshold)
            {
                return (int)(r % bound);
            }
        }
    }

    /// <summary>Uniform integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "must exceed minInclusive");
        }

        return minInclusive + NextInt(maxExclusive - minInclusive);
    }

    /// <summary>Deterministic double in [0, 1). 32-bit resolution; exact integer-to-double math.</summary>
    public double NextDouble() => NextUInt() / 4294967296.0;
}
