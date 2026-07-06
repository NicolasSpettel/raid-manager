using System;
using System.Globalization;
using System.Linq;
using Content;
using Engine;
using Game;

// Sim — the headless harness.
//   run <fixture> --seed <N>     run one encounter, print its event log + hash
//   campaign --raids <N> --seed <N>   run the real Game loop over N raids, print progression
// It calls the same Game loop the App calls (no mirror), and is the home of golden/probe verbs.
return SimCli.Run(args);

internal static class SimCli
{
    public static int Run(string[] args)
    {
        if (args is ["run", var fixture, ..])
        {
            return RunFixture(fixture, ParseSeed(args));
        }

        if (args is ["campaign", ..])
        {
            return RunCampaign(
                ParseInt(args, "--raids", 5), ParseSeed(args),
                ParseString(args, "--boss", "warden"), ParseString(args, "--difficulty", "normal"));
        }

        Console.Error.WriteLine("usage: sim run <dummy|trio|caster|raid|warden|classraid> --seed <N>");
        Console.Error.WriteLine("       sim campaign --raids <N> --seed <N> --boss <warden|sentinel|ashen_king> --difficulty <normal|heroic|mythic>");
        return 1;
    }

    private static int RunFixture(string fixture, ulong seed)
    {
        SimInput? input = Fixtures.ByName(fixture, seed) ?? ContentFixtures.ByName(fixture, seed);
        if (input is null)
        {
            Console.Error.WriteLine($"unknown fixture '{fixture}' (try: dummy, trio, caster, raid, warden, classraid)");
            return 1;
        }

        SimResult result = Simulator.SimulateEncounter(input);
        Console.WriteLine(EventStream.Serialize(result.Events).TrimEnd('\n'));
        Console.WriteLine(
            $"outcome={result.Outcome} seed={result.Seed} engine=v{result.EngineVersion} schema=v{result.EventSchemaVersion}");
        Console.WriteLine($"hash={result.Hash()}");
        return 0;
    }

    private static int RunCampaign(int raids, ulong seed, string bossId, string difficultyName)
    {
        EncounterDef baseEncounter = Encounters.All.FirstOrDefault(e => e.Id == bossId) ?? Encounters.Warden;
        Difficulty difficulty = Enum.TryParse(difficultyName, ignoreCase: true, out Difficulty d) ? d : Difficulty.Normal;
        EncounterDef encounter = Difficulties.Scale(baseEncounter, difficulty);

        GuildSave start = Guilds.CreateStarter("Campaign Guild", seed, "2026-01-01T00:00:00Z");
        Console.WriteLine($"== Campaign: {start.Roster.Count} raiders vs {encounter.Name}, {raids} raids, seed {seed} ==");

        CampaignResult run = Campaign.Run(start, raids, seed, encounter);

        int raidNumber = 0;
        foreach (RaidSummary summary in run.Raids)
        {
            raidNumber++;
            string loot = summary.LootDropped ?? "-";
            Console.WriteLine($"  raid {raidNumber,2}: {summary.Outcome,-7} +{summary.GoldAwarded} gold  loot={loot}");
        }

        Console.WriteLine($"== {run.Wins}/{raids} kills, gold {run.Guild.Economy.Gold} ==");
        foreach (RaiderRecord r in run.Guild.Roster)
        {
            Console.WriteLine(
                $"   {r.Name,-10} {Classes.Registry.Get(r.ClassId).Name,-12} Lv {r.Level,-2} gear {Warband.GearPower(r)}");
        }

        return 0;
    }

    private static ulong ParseSeed(string[] args) => (ulong)ParseInt(args, "--seed", 1);

    private static string ParseString(string[] args, string flag, string fallback)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == flag)
            {
                return args[i + 1];
            }
        }

        return fallback;
    }

    private static int ParseInt(string[] args, string flag, int fallback)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == flag && int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }
        }

        return fallback;
    }
}
