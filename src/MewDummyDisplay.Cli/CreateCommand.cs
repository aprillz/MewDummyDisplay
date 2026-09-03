using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

internal static class CreateCommand
{
    internal static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: mdd create <definition-id> [--lodpi] [--hold <seconds>] [--refresh <hz>]");
            return 2;
        }

        DummyDefinition? definition = DummyDefinitionCatalog.Find(args[0]);
        if (definition is null)
        {
            Console.Error.WriteLine($"Unknown definition: {args[0]}");
            Console.Error.WriteLine("Run 'mdd definitions' to see the available identifiers.");
            return 2;
        }

        bool hiDpi = true;
        int holdSeconds = 0;
        double? refreshRate = null;
        double? slsRotate = null;
        bool watch = true;
        bool pump = true;
        int pollSeconds = 0;
        string? setResolution = null;
        bool slsUseFloat = true;

        for (int index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--lodpi":
                    hiDpi = false;
                    break;
                case "--hold" when index + 1 < args.Length:
                    holdSeconds = int.Parse(args[++index]);
                    break;
                case "--refresh" when index + 1 < args.Length:
                    refreshRate = double.Parse(args[++index]);
                    break;
                case "--sls-rotate" when index + 1 < args.Length:
                    slsRotate = double.Parse(args[++index]);
                    break;
                case "--sls-int":
                    slsUseFloat = false;
                    break;
                case "--no-watch":
                    watch = false;
                    break;
                case "--no-pump":
                    pump = false;
                    break;
                case "--poll" when index + 1 < args.Length:
                    pollSeconds = int.Parse(args[++index]);
                    break;
                case "--resolution" when index + 1 < args.Length:
                    setResolution = args[++index];
                    break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[index]}");
                    return 2;
            }
        }

        if (!Diagnostics.ReportSurface())
        {
            return 1;
        }

        if (!DisplayHealth.Check("start"))
        {
            return 3;
        }

        DummyManager.WatchReconfiguration = watch;
        DummyManager.PumpRunLoopWhileWaiting = pump;
        Console.WriteLine($"watchReconfiguration={watch} pumpRunLoop={pump}");

        using DummyManager manager = new();
        DummySpec spec = new()
        {
            Definition = definition,
            HiDpi = hiDpi,
            RefreshRateOverride = refreshRate,
        };

        Dummy? dummy = manager.Create(spec);
        if (dummy is null)
        {
            Console.Error.WriteLine("Failed to create the virtual display. applySettings returned false.");
            return 1;
        }

        Diagnostics.PrintDummy(dummy);

        // Display registration is asynchronous, so let the system settle before reading it back.
        Thread.Sleep(1500);
        Console.WriteLine();
        Diagnostics.PrintDisplaySnapshot(dummy.DisplayId);

        if (slsRotate is double degrees)
        {
            Console.WriteLine();
            Console.WriteLine("SkyLight rotation probe (unresolved research):");
            RotationProbe.Run(dummy.DisplayId, degrees, slsUseFloat);
            Thread.Sleep(1500);
            Diagnostics.PrintDisplaySnapshot(dummy.DisplayId);
        }

        if (pollSeconds > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Polling the creating process's view for {pollSeconds}s:");
            DateTime deadline = DateTime.UtcNow.AddSeconds(pollSeconds);
            while (DateTime.UtcNow < deadline)
            {
                DisplayInfo snapshot = DisplayCatalog.Describe(dummy.DisplayId);
                int listed = DisplayCatalog.Online().Count;
                Console.WriteLine(
                    $"  t+{(pollSeconds - (deadline - DateTime.UtcNow).TotalSeconds):00.0}s  " +
                    $"points={snapshot.Width}x{snapshot.Height}  modes={dummy.Modes().Count}  onlineCount={listed}");
                if (snapshot.Width > 0)
                {
                    Console.WriteLine("  -> became readable");
                    break;
                }
                Thread.Sleep(2000);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Modes offered by this dummy:");
        IReadOnlyList<DisplayMode> modes = dummy.Modes();
        foreach (DisplayMode mode in modes.Take(8))
        {
            Console.WriteLine($"  {mode}");
        }
        Console.WriteLine(modes.Count > 8 ? $"  ... {modes.Count} total" : $"  {modes.Count} total");

        if (setResolution is not null)
        {
            string[] parts = setResolution.Split('x');
            int wantWidth = int.Parse(parts[0]);
            int wantHeight = int.Parse(parts[1]);

            DisplayMode? pick = modes.FirstOrDefault(mode => mode.Width == wantWidth && mode.Height == wantHeight && mode.IsHiDpi)
                ?? modes.FirstOrDefault(mode => mode.Width == wantWidth && mode.Height == wantHeight);

            Console.WriteLine();
            if (pick is null)
            {
                Console.WriteLine($"No {setResolution} mode available.");
            }
            else
            {
                bool applied = DisplayCatalog.TrySetMode(dummy.DisplayId, pick);
                Thread.Sleep(2000);
                IReadOnlyList<DisplayMode> after = dummy.Modes();
                DisplayMode? current = after.FirstOrDefault();
                Console.WriteLine($"Set to {pick} via {pick.Source}: returned={applied}");
                Console.WriteLine($"  bounds now: {DisplayCatalog.Describe(dummy.DisplayId).Bounds.Width:0}x{DisplayCatalog.Describe(dummy.DisplayId).Bounds.Height:0}");
            }
        }

        Console.WriteLine();
        ListCommand.Run();

        if (holdSeconds > 0)
        {
            Console.WriteLine($"\nHolding for {holdSeconds}s...");
            Thread.Sleep(TimeSpan.FromSeconds(holdSeconds));
        }
        else
        {
            Console.WriteLine("\nPress Enter to release the display.");
            Console.ReadLine();
        }

        Console.WriteLine("Releasing.");
        return 0;
    }

    private static string FormatStatus(int status)
        => status == int.MinValue ? "skipped" : status.ToString();
}
