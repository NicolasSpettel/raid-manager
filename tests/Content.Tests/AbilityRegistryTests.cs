using Content;
using Engine;
using Xunit;

namespace Content.Tests;

/// <summary>
/// Registry completeness: a half-filled ability row fails loudly with a message naming what's missing
/// (content-authoring §4). Adding content is guided by red tests, not discovered at runtime.
/// </summary>
public class AbilityRegistryTests
{
    [Fact]
    public void Registry_IsNotEmpty()
    {
        Assert.NotEmpty(Abilities.Registry.All);
    }

    [Fact]
    public void EveryRow_IsComplete()
    {
        foreach (AbilityRow row in Abilities.Registry.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(row.Name), $"{row.Id}: missing Name");
            Assert.False(string.IsNullOrWhiteSpace(row.ClassId), $"{row.Id}: missing ClassId");
            Assert.False(string.IsNullOrWhiteSpace(row.SpecId), $"{row.Id}: missing SpecId");
            Assert.False(string.IsNullOrWhiteSpace(row.Tooltip), $"{row.Id}: missing Tooltip");
            Assert.True(row.GcdTicks >= 1, $"{row.Id}: GCD must be >= 1 (forward progress)");
            Assert.True(row.Amount >= 0, $"{row.Id}: Amount must be >= 0");
            Assert.True(row.Variance >= 0, $"{row.Id}: Variance must be >= 0");
        }
    }

    [Fact]
    public void EveryRow_ProjectsToValidEngineDef()
    {
        foreach (AbilityRow row in Abilities.Registry.All)
        {
            AbilityDef def = row.ToDef();
            Assert.Equal(row.Id, def.Id.Value);
            Assert.True(def.GcdTicks >= 1, $"{row.Id}: projected GCD must be >= 1");
        }
    }
}
