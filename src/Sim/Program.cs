using System.Globalization;
using Engine;

// Sim — the headless harness. The M1 verb is `run <fixture> --seed <N>` (fixtures: dummy, trio):
// it runs the real Engine.SimulateEncounter and prints the event log + the stream hash. This is both
// the headless proof of the engine and the future home of golden/probe/campaign verbs.
return SimCli.Run(args);

internal static class SimCli
{
    public static int Run(string[] args)
    {
        if (args is ["run", var fixture, ..])
        {
            ulong seed = ParseSeed(args);
            SimInput? input = Fixtures.ByName(fixture, seed);
            if (input is null)
            {
                Console.Error.WriteLine($"unknown fixture '{fixture}' (try: dummy, trio, caster)");
                return 1;
            }

            SimResult result = Simulator.SimulateEncounter(input);
            Console.WriteLine(EventStream.Serialize(result.Events).TrimEnd('\n'));
            Console.WriteLine(
                $"outcome={result.Outcome} seed={result.Seed} engine=v{result.EngineVersion} schema=v{result.EventSchemaVersion}");
            Console.WriteLine($"hash={result.Hash()}");
            return 0;
        }

        Console.Error.WriteLine("usage: sim run <dummy|trio|caster> --seed <N>");
        return 1;
    }

    private static ulong ParseSeed(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--seed")
            {
                return ulong.Parse(args[i + 1], CultureInfo.InvariantCulture);
            }
        }

        return 1UL;
    }
}
