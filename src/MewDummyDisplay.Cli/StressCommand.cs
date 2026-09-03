using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

// Answers a Phase 1 question: does repeated create/release degrade CoreGraphics
// because of how fast operations arrive, or because of how many have happened in
// this process? A gap that fixes it means rate; failure regardless of gap means
// the process accumulates state, which matters for a long running GUI app.
internal static class StressCommand
{
    internal static int Run(string[] args)
    {
        int count = 12;
        int gapMilliseconds = 0;
        bool holdAll = false;
        bool pumpRunLoop = false;
        bool vary = false;
        int holdSeconds = 0;

        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--count" when index + 1 < args.Length:
                    count = int.Parse(args[++index]);
                    break;
                case "--gap" when index + 1 < args.Length:
                    gapMilliseconds = int.Parse(args[++index]);
                    break;
                case "--hold-all":
                    holdAll = true;
                    break;
                case "--pump":
                    pumpRunLoop = true;
                    break;
                case "--vary":
                    vary = true;
                    break;
                case "--hold-seconds" when index + 1 < args.Length:
                    holdSeconds = int.Parse(args[++index]);
                    break;
            }
        }

        if (count > DisplayHealth.MAX_CYCLES_PER_RUN)
        {
            Console.Error.WriteLine($"count is capped at {DisplayHealth.MAX_CYCLES_PER_RUN} per process.");
            count = DisplayHealth.MAX_CYCLES_PER_RUN;
        }

        if (holdAll && count > DisplayHealth.MAX_DISPLAYS_PER_RUN)
        {
            Console.Error.WriteLine($"holding is capped at {DisplayHealth.MAX_DISPLAYS_PER_RUN} displays.");
            count = DisplayHealth.MAX_DISPLAYS_PER_RUN;
        }

        if (!DisplayHealth.Check("start"))
        {
            return 3;
        }

        Console.WriteLine($"count={count} gap={gapMilliseconds}ms holdAll={holdAll} pump={pumpRunLoop} vary={vary}");
        Console.WriteLine($"{"N",-4} {"DEF",-9} {"ID",-6} {"READY",-6} {"POINTS",-12} {"MODES",-7} {"ELAPSED"}");

        using DummyManager manager = new();
        List<Dummy> held = [];
        int failures = 0;

        for (int index = 1; index <= count; index++)
        {
            DateTime started = DateTime.UtcNow;
            DummyDefinition definition = vary ? _varied[(index - 1) % _varied.Length] : Require("16:9");
            Dummy? dummy = manager.Create(new DummySpec { Definition = definition, SerialNumber = ToolSerials.For(definition.Id) });
            if (dummy is null)
            {
                Console.WriteLine($"{index,-4} {definition.Id,-9} {"-",-6} {"-",-6} {"createFailed",-12}");
                failures++;
                continue;
            }

            if (pumpRunLoop)
            {
                RunLoopProbe.Pump(TimeSpan.FromSeconds(2));
            }


            DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);
            int modeCount = dummy.Modes().Count;
            bool healthy = dummy.IsReady && modeCount > 0;
            if (!healthy)
            {
                failures++;
            }

            Console.WriteLine(
                $"{index,-4} {definition.Id,-9} {dummy.DisplayId,-6} {(dummy.IsReady ? "yes" : "NO"),-6} " +
                $"{$"{info.Width}x{info.Height}",-12} {modeCount,-7} " +
                $"{(DateTime.UtcNow - started).TotalMilliseconds:0}ms {(healthy ? "" : "   <- DEGRADED")}");

            if (holdAll)
            {
                held.Add(dummy);
            }
            else
            {
                manager.Remove(dummy);
            }

            if (!DisplayHealth.Check($"after cycle {index}"))
            {
                return 3;
            }

            if (gapMilliseconds > 0)
            {
                Thread.Sleep(gapMilliseconds);
            }
        }

        if (held.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("State of every held display:");
            Console.WriteLine($"{"ID",-6} {"ACTIVE",-7} {"ONLINE",-7} {"ASLEEP",-7} {"MIRRORSET",-10} {"MIRRORS",-8} {"BOUNDS"}");
            foreach (Dummy entry in held)
            {
                DisplayInfo state = DisplayCatalog.Describe(entry.DisplayId);
                Console.WriteLine(
                    $"{state.DisplayId,-6} {(state.IsActive ? "yes" : "NO"),-7} {(state.IsOnline ? "yes" : "NO"),-7} " +
                    $"{(state.IsAsleep ? "YES" : "no"),-7} {(state.IsInMirrorSet ? "YES" : "no"),-10} " +
                    $"{state.MirrorsDisplayId,-8} " +
                    $"{state.Bounds.X:0},{state.Bounds.Y:0} {state.Bounds.Width:0}x{state.Bounds.Height:0}");
            }
        }

        if (holdSeconds > 0)
        {
            Console.WriteLine($"\nHolding {held.Count} displays for {holdSeconds}s so another process can inspect them.");
            Thread.Sleep(TimeSpan.FromSeconds(holdSeconds));
        }

        Console.WriteLine();
        Console.WriteLine($"{count - failures}/{count} healthy");
        return failures == 0 ? 0 : 1;
    }

    private static readonly DummyDefinition[] _varied =
    [
        Require("16:9"), Require("16:10"), Require("4:3"), Require("21.3:9"),
        Require("3:2"), Require("5:4"), Require("32:9"), Require("1:1"),
    ];

    private static DummyDefinition Require(string id)
        => DummyDefinitionCatalog.Find(id) ?? throw new InvalidOperationException($"definition missing: {id}");
}
