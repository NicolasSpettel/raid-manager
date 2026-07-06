using System.Linq;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// Condition-driven injuries (GDD §8): a fatigued roster under heavy raid load gets hurt; a fresh, light
/// week mostly doesn't. Seeded → deterministic.
/// </summary>
public class InjuriesTests
{
    private static GuildSave Starter() => Guilds.CreateStarter("Test", 1, "2026-01-01T00:00:00Z");

    private static GuildSave Exhausted(GuildSave guild) => guild with
    {
        Roster = guild.Roster.Select(r => r with { Condition = new Condition(Morale: 50, Freshness: 10, Sharpness: 20) }).ToList(),
    };

    [Fact]
    public void ExhaustedRoster_UnderHeavyLoad_GetsInjured()
    {
        (GuildSave after, var events) = Injuries.RollWeek(Exhausted(Starter()), raidDays: 4, seed: 1);

        Assert.NotEmpty(events);
        Assert.Contains(after.Roster, r => r.InjuryRaidsLeft > 0);
    }

    [Fact]
    public void FreshRoster_LightWeek_StaysHealthy()
    {
        (GuildSave after, _) = Injuries.RollWeek(Starter(), raidDays: 1, seed: 1); // fresh (100) + light load

        Assert.DoesNotContain(after.Roster, r => r.InjuryRaidsLeft > 0);
    }

    [Fact]
    public void AlreadyInjured_AreNotRolledAgain()
    {
        GuildSave guild = Starter() with
        {
            Roster = Starter().Roster.Select(r => r with { InjuryRaidsLeft = 2 }).ToList(),
        };

        (_, var events) = Injuries.RollWeek(Exhausted(guild), raidDays: 4, seed: 1);
        Assert.Empty(events); // everyone's already hurt — no new injuries
    }

    [Fact]
    public void RollWeek_IsDeterministic()
    {
        GuildSave guild = Exhausted(Starter());
        int a = Injuries.RollWeek(guild, 4, 3).Guild.Roster.Count(r => r.InjuryRaidsLeft > 0);
        int b = Injuries.RollWeek(guild, 4, 3).Guild.Roster.Count(r => r.InjuryRaidsLeft > 0);
        Assert.Equal(a, b);
    }
}
