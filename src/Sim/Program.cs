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

        if (args is ["world", ..])
        {
            return RunWorld(ParseSeed(args), ParseInt(args, "--guilds", WorldConfig.Default.GuildCount));
        }

        if (args is ["season", ..])
        {
            return RunSeason(ParseSeed(args), ParseInt(args, "--weeks", 16));
        }

        if (args is ["play", ..])
        {
            return RunPlay(ParseSeed(args), ParseString(args, "--stance", "balanced"), ParseString(args, "--difficulty", "normal"));
        }

        Console.Error.WriteLine("usage: sim run <dummy|trio|caster|raid|warden|spatial|classraid> --seed <N>");
        Console.Error.WriteLine("       sim campaign --raids <N> --seed <N> --boss <id> --difficulty <normal|heroic|mythic>");
        Console.Error.WriteLine("       sim balance --raids <N> --seed <N>   (win-rate matrix over every boss x difficulty)");
        Console.Error.WriteLine("       sim world --seed <N> --guilds <N>    (generate the living world, print distributions + hash)");
        Console.Error.WriteLine("       sim season --seed <N> --weeks <N>    (race the world through the season raid, print the leaderboard)");
        Console.Error.WriteLine("       sim play --seed <N> --stance <relax|balanced|grind> --difficulty <normal|heroic|mythic>  (the player's weekly raid loop under lockout)");
        return 1;
    }

    // The player's season: advance the calendar week by week, raiding the ladder under a weekly lockout.
    private static int RunPlay(ulong seed, string stanceName, string difficultyName)
    {
        WeekStance stance = stanceName.ToLowerInvariant() switch
        {
            "relax" => WeekStance.Relax,
            "grind" or "grindhard" or "grind_hard" => WeekStance.GrindHard,
            _ => WeekStance.Balanced,
        };
        Difficulty difficulty = Enum.TryParse(difficultyName, ignoreCase: true, out Difficulty d) ? d : Difficulty.Normal;

        GuildSave guild = Guilds.CreateStarter("Your Guild", seed, "2026-01-01T00:00:00Z");
        SeasonCalendar calendar = SeasonCalendar.Start(8);
        var ladder = Encounters.All;
        ActivityPlan plan = WeekPlan.Plan(stance);
        int bestBoss = -1;

        Console.WriteLine(
            $"== Season: {stance} (raid {plan.RaidDays} / dungeon {plan.DungeonDays} / train {plan.TrainDays} / rest {plan.RestDays}), {difficulty} ==");
        while (!calendar.SeasonOver)
        {
            int week = calendar.CurrentWeek;

            WeekOutcome raids = WeekRunner.RunWeek(guild, ladder, week, plan.RaidDays, Lockout.Empty, difficulty, seed);
            guild = raids.Guild;
            bestBoss = Math.Max(bestBoss, raids.Report.FurthestBossIndex);

            ActivityOutcome activities = WeeklyActivities.Run(guild, plan, seed + (ulong)(week * 31)); // dungeons + training
            guild = activities.Guild;
            guild = ConditionModel.AfterWeek(guild, plan.RaidDays);                                    // freshness/sharpness
            (guild, var injuries) = Injuries.RollWeek(guild, plan.RaidDays, seed + (ulong)(week * 7));  // fatigue → injuries

            int downed = raids.Report.Nights.Count(n => n.Outcome == "Kill");
            string frontier = raids.Report.FurthestBossIndex >= 0 ? ladder[raids.Report.FurthestBossIndex].Name : "—";
            int avgFreshness = (int)guild.Roster.Average(r => (r.Condition ?? ConditionModel.Fresh).Freshness);
            int avgGear = (int)guild.Roster.Average(Warband.GearPower);
            int hurt = guild.Roster.Count(r => r.InjuryRaidsLeft > 0);
            Console.WriteLine(
                $"  week {week,2}: {downed} kills, furthest {frontier}, gold {guild.Economy.Gold}, " +
                $"fresh {avgFreshness}, gear {avgGear}, +{activities.Report.GearDrops} loot/+{activities.Report.TrainingSessions} train, {hurt} hurt");
            calendar = calendar.Advance();
        }

        string reached = bestBoss >= 0 ? ladder[bestBoss].Name : "nothing";
        int finalGear = (int)guild.Roster.Average(Warband.GearPower);
        Console.WriteLine($"\nseason over — progression: {bestBoss + 1}/{ladder.Count} bosses (reached {reached}); gold {guild.Economy.Gold}, avg gear {finalGear}");
        return 0;
    }

    // Race the whole generated world through the season raid and print the global leaderboard + pacing.
    private static int RunSeason(ulong seed, int weeks)
    {
        World world = WorldGen.Generate(seed);
        SeasonResult result = SeasonRace.Run(world, SeasonRaid.Default, weeks);

        Console.WriteLine($"== Season race: {result.Standings.Count} guilds, raid \"{result.Raid.Name}\" ({result.Raid.Bosses.Count} bosses), {weeks} weeks ==");
        Console.WriteLine("  #  guild                         tier          str  bosses  cleared");
        int rank = 0;
        foreach (GuildProgress g in result.Standings.Take(12))
        {
            rank++;
            string cleared = g.ClearedWeek is { } w ? $"week {w}" : "—";
            Console.WriteLine($"{rank,3}  {g.Name,-28} {g.Tier,-12} {g.Strength,4} {g.BossesDown,6}/{result.Raid.Bosses.Count}  {cleared}");
        }

        SeasonChronicle chronicle = Chronicle.Record(season: 1, result);
        Console.WriteLine($"\nchampion: {chronicle.Champion ?? "— (nobody cleared)"}");
        Console.WriteLine("world-firsts:");
        foreach (WorldFirst wf in chronicle.WorldFirsts)
        {
            Console.WriteLine($"   {wf.Boss,-18} {wf.Guild} ({wf.Region}) — week {wf.Week}");
        }

        var cleared100 = result.Standings.Where(g => g.ClearedWeek is not null).ToList();
        int hundredth = result.Standings.Count >= 100 ? (result.Standings[99].ClearedWeek ?? 0) : 0;
        Console.WriteLine($"\npacing: {cleared100.Count} guilds cleared in {weeks} weeks; " +
            $"rank #100 cleared {(hundredth > 0 ? $"week {hundredth}" : "not yet")}");
        return 0;
    }

    // Generate a deterministic world and print its shape: guilds per tier, star curve per tier, role coverage.
    private static int RunWorld(ulong seed, int guilds)
    {
        World world = WorldGen.Generate(seed, WorldConfig.Default with { GuildCount = guilds });
        var all = world.Raiders.Values.ToList();

        Console.WriteLine(
            $"== World seed {seed}: {world.Guilds.Count} guilds, {all.Count} raiders ({world.FreeAgents.Count} free agents) ==");

        Console.WriteLine("tier            guilds  avg★   5★-role raiders");
        foreach (PrestigeTier tier in Enum.GetValues<PrestigeTier>())
        {
            var members = world.Guilds.Where(g => g.Tier == tier).SelectMany(g => g.Roster).Select(world.Get).ToList();
            if (members.Count == 0)
            {
                continue;
            }

            double avgStars = members.Average(r => Ratings.Best(r).HalfStars) / 2.0;
            int elites = members.Count(r => Ratings.Best(r).HalfStars >= 9); // 4.5★+
            Console.WriteLine($"{tier,-14} {world.Guilds.Count(g => g.Tier == tier),6} {avgStars,6:0.00} {elites,10}");
        }

        Console.WriteLine("\narchetype spread:");
        foreach (IGrouping<string, Raider> grp in all.GroupBy(r => r.ArchetypeId).OrderByDescending(g => g.Count()))
        {
            Console.WriteLine($"   {grp.Key,-20} {grp.Count(),5}  ({grp.Count() * 100 / all.Count}%)");
        }

        int oldest = all.Max(world.AgeOf);
        int youngest = all.Min(world.AgeOf);
        Console.WriteLine($"\nage range {youngest}–{oldest}; hash={WorldText.Hash(world)}");
        return 0;
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
