using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Engine;
using Game;
using Xunit;

namespace Game.Tests;

/// <summary>
/// Aging & the career arc (GDD §8): age derives from birth-season; young raiders develop, veterans decline
/// (twitch down, wisdom up), and past ~31 they retire with a youth prospect stepping in. Plus targeted
/// training (choose the stat).
/// </summary>
public class AgingTests
{
    // A raider whose age at season 5 is exactly `age` (base season 100 → season 5 clock is 104).
    private static RaiderRecord AtAge(string id, int age, IReadOnlyDictionary<string, int>? attrs = null)
    {
        var values = attrs ?? Attributes.Registry.All.ToDictionary(a => a.Id, _ => 10, StringComparer.Ordinal);
        return new RaiderRecord(id, "Name " + id, "blademaster",
            Attributes: new AttributeVector(new Dictionary<string, int>(values, StringComparer.Ordinal)),
            Condition: ConditionModel.Fresh,
            Identity: new Identity("Name", "Ironreach", 104 - age, 0),
            Vocation: new Vocation("blademaster", new Dictionary<CombatantRole, int> { [CombatantRole.Melee] = 90 }));
    }

    private static GuildSave GuildOf(params RaiderRecord[] roster) => new(
        SaveMigrations.CurrentVersion, "2026-01-01T00:00:00Z", new GuildInfo("G", 0),
        roster.ToList(), new Economy(1000), new List<RaidSummary>(), WorldSeed: 1);

    [Fact]
    public void AgeOf_DerivesFromBirthSeason()
    {
        Assert.Equal(25, Aging.AgeOf(AtAge("r", 25), seasonNumber: 5));
    }

    [Fact]
    public void OldRaiders_Retire_AndLeaveTheRoster()
    {
        GuildSave guild = GuildOf(AtAge("vet", 33), AtAge("kid", 20));

        AgingResult result = Aging.AdvanceSeason(guild, newSeasonNumber: 5);

        Assert.Single(result.Guild.Roster); // the retiree just leaves; no auto-backfill
        Assert.Contains(result.Events, e => e.Contains("retired", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Guild.Roster, r => r.Id == "vet"); // the veteran is gone
        Assert.Contains(result.Guild.Roster, r => r.Id == "kid");       // the young one stays
    }

    [Fact]
    public void Veterans_Decline_TwitchDown_WisdomUp()
    {
        var attrs = Attributes.Registry.All.ToDictionary(a => a.Id, _ => 12, StringComparer.Ordinal);
        GuildSave guild = GuildOf(AtAge("vet", 29, attrs));

        AttributeVector after = Aging.AdvanceSeason(guild, 5).Guild.Roster[0].Attributes!;

        Assert.True(after.Of("mechanics") < 12, "a twitch stat should fade");   // Mechanics = Twitch
        Assert.True(after.Of("composure") > 12, "a wisdom stat should rise");    // Composure = Wisdom
    }

    [Fact]
    public void YoungRaiders_Develop()
    {
        var attrs = Attributes.Registry.All.ToDictionary(a => a.Id, _ => 8, StringComparer.Ordinal);
        GuildSave guild = GuildOf(AtAge("kid", 18, attrs));

        int total = Aging.AdvanceSeason(guild, 5).Guild.Roster[0].Attributes!.Values.Values.Sum();
        Assert.True(total > 8 * Attributes.Registry.All.Count, "a developing youth should gain attribute points");
    }

    [Fact]
    public void TrainToward_RaisesTheChosenStat_Only()
    {
        var attrs = new AttributeVector(Attributes.Registry.All.ToDictionary(a => a.Id, _ => 10, StringComparer.Ordinal));
        var raider = new RaiderRecord("r", "N", "blademaster", Attributes: attrs);

        RaiderRecord trained = WeeklyActivities.TrainToward(raider, "composure");

        Assert.Equal(11, trained.Attributes!.Of("composure"));
        Assert.Equal(10, trained.Attributes.Of("mechanics"));
    }
}
