using System.Linq;
using Engine;
using Xunit;

namespace Engine.Tests;

/// <summary>
/// Determinism guards: the whole architecture rests on "same input ⇒ byte-identical output". If an
/// accidental unseeded path slips in, these catch it (testing-strategy §1).
/// </summary>
public class DeterminismTests
{
    [Fact]
    public void SameSeed_ProducesIdenticalStream()
    {
        Assert.Equal(DummyFight.Run(1).Hash(), DummyFight.Run(1).Hash());
    }

    [Fact]
    public void DifferentSeeds_ProduceDistinctStreams()
    {
        var hashes = Enumerable.Range(1, 10)
            .Select(seed => DummyFight.Run((ulong)seed).Hash())
            .ToList();

        Assert.Equal(hashes.Count, hashes.Distinct().Count());
    }

    [Fact]
    public void SeededRng_SameSeed_SameSequence()
    {
        var a = new SeededRng(42);
        var b = new SeededRng(42);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(a.NextUInt(), b.NextUInt());
        }
    }

    [Fact]
    public void SeededRng_NextInt_StaysInRange()
    {
        var rng = new SeededRng(7);

        for (int i = 0; i < 10000; i++)
        {
            Assert.InRange(rng.NextInt(3, 9), 3, 8);
        }
    }
}
