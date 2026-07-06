using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;
using Game;
using NetArchTest.Rules;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// The dependency walls (ADR-0001) are enforced by ProjectReference at compile time — Engine simply
/// cannot reference Godot or the UI because the reference isn't there. These tests are the second
/// net: they assert the namespace-level direction so a future accidental dependency fails loudly.
/// </summary>
public class BoundaryTests
{
    [Fact]
    public void Engine_DependsOnNothingInTheSolution_AndNotGodot()
    {
        TestResult result = Types.InAssembly(typeof(Simulator).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Content", "Game", "App", "Godot")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Engine", result));
    }

    [Fact]
    public void Content_DoesNotDependOn_Game()
    {
        TestResult result = Types.InAssembly(typeof(Abilities).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Game", "App", "Godot")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Content", result));
    }

    [Fact]
    public void Game_DoesNotDependOn_AppOrGodot()
    {
        TestResult result = Types.InAssembly(typeof(Guilds).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("App", "Godot")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Game", result));
    }

    private static string Describe(string layer, TestResult result)
    {
        IEnumerable<string> failing = result.FailingTypeNames ?? Enumerable.Empty<string>();
        return $"{layer} has a forbidden dependency. Offending types: {string.Join(", ", failing)}";
    }
}
