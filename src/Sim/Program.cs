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

        if (args is ["balance", ..])
        {
            return RunBalance(ParseInt(args, "--raids", 6), ParseSeed(args));
        }

        Console.Error.WriteLine("usage: sim run <dummy|trio|caster|raid|warden|spatial|classraid> --seed <N>");
        Console.Error.WriteLine("       sim campaign --raids <N> --seed <N> --boss <id> --difficulty <normal|heroic|mythic>");
        Console.Error.WriteLine("       sim balance --raids <N> --seed <N>   (win-rate matrix over every boss x difficulty)");
        return 1;
    }

    private static int RunFixture(string fixture, ulong seed)
    {
        SimInput? input = Fixtures.ByName(fixture, seed) ?? ContentFixtures.ByName(fixture, seed);
        if (input is null)
        {
            Console.Error.WriteLine($"unknown fixture '{fixture}' (try: dummy, trio, caster, raid, warden, spatial, classraid)");
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
                $"   {r.Name,-10} {Classes.Registry.Get(r.ClassId).Name,-12} gear {Warband.GearPower(r)}");
        }

        return 0;
    }

    // A balance matrix: a fresh guild's win rate per boss x difficulty — a quick tuning read.
    private static int RunBalance(int raids, ulong seed)
    {
        Console.WriteLine($"== Balance matrix (fresh 8-raider guild, {raids} raids each, seed {seed}) ==");
        Console.WriteLine($"{"boss",-16} {"Normal",-8} {"Heroic",-8} {"Mythic",-8}");

        foreach (EncounterDef boss in Encounters.All)
        {
            var cells = Difficulties.All.Select(difficulty =>
            {
                EncounterDef encounter = Difficulties.Scale(boss, difficulty);
                GuildSave guild = Guilds.CreateStarter("Bal", seed, "2026-01-01T00:00:00Z");
                return $"{Campaign.Run(guild, raids, seed, encounter).Wins}/{raids}";
            }).ToArray();

            Console.WriteLine($"{boss.Name,-16} {cells[0],-8} {cells[1],-8} {cells[2],-8}");
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
