using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

// Phase 0 feasibility checks. Each check answers one open question in
// agent/better-dummy-porting/plan.md and prints a PASS or FAIL line.
//
// Display registration and teardown are asynchronous. Creating and releasing
// displays back to back makes CoreGraphics report stale or empty values, so every
// check settles after creating and cools down before the next one runs.
internal static class ProbeCommand
{
    private const string DEFINITION_ID = "16:9";
    private const int DEFAULT_SETTLE_MS = 2000;
    private const int DEFAULT_COOLDOWN_MS = 3000;

    private static int _settleMilliseconds = DEFAULT_SETTLE_MS;
    private static int _cooldownMilliseconds = DEFAULT_COOLDOWN_MS;

    internal static int Run(string[] args)
    {
        for (int index = 0; index < args.Length; index++)
        {
            if (args[index] == "--settle" && index + 1 < args.Length)
            {
                _settleMilliseconds = int.Parse(args[++index]);
            }
            else if (args[index] == "--cooldown" && index + 1 < args.Length)
            {
                _cooldownMilliseconds = int.Parse(args[++index]);
            }
        }

        Console.WriteLine($"macOS {Environment.OSVersion.Version}  runtime {Environment.Version}  arch {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"settle {_settleMilliseconds}ms  cooldown {_cooldownMilliseconds}ms");

        if (!DisplayHealth.Check("start"))
        {
            return 3;
        }

        List<(string Name, bool Passed, string Detail)> results = [];

        results.Add(CheckSurface());
        if (!DummyManager.IsSupported)
        {
            Report(results);
            return 1;
        }

        results.Add(Run(CheckSingleCreate));
        results.Add(Run(CheckMultiple));
        results.Add(Run(CheckModes));
        results.Add(Run(CheckMirroring));
        results.Add(Run(ObserveRefreshRate));
        results.Add(CheckNoOrphans());

        Report(results);
        return results.All(entry => entry.Passed) ? 0 : 1;
    }

    private static (string, bool, string) Run(Func<(string, bool, string)> check)
    {
        (string, bool, string) result = check();
        Thread.Sleep(_cooldownMilliseconds);
        return result;
    }

    private static (string, bool, string) CheckSurface()
    {
        if (DummyManager.IsSupported)
        {
            return ("surface", true, "all required classes and selectors present");
        }
        return ("surface", false, string.Join(", ", DummyManager.MissingSurface));
    }

    private static (string, bool, string) CheckSingleCreate()
    {
        using DummyManager manager = new();
        Dummy? dummy = manager.Create(new DummySpec { Definition = Require(DEFINITION_ID) });
        if (dummy is null)
        {
            return ("create", false, "applySettings returned false");
        }

        Thread.Sleep(_settleMilliseconds);
        bool visible = IsOnline(dummy.DisplayId);
        DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);

        return ("create", visible && info.IsHiDpi,
            $"id={dummy.DisplayId} points={info.Width}x{info.Height} pixels={info.PixelWidth}x{info.PixelHeight} hz={info.RefreshRate:0} hidpi={info.IsHiDpi}");
    }

    private static (string, bool, string) CheckMultiple()
    {
        using DummyManager manager = new();
        DummyDefinition definition = Require(DEFINITION_ID);

        List<uint> created = [];
        for (int index = 0; index < 3; index++)
        {
            Dummy? dummy = manager.Create(new DummySpec { Definition = definition });
            if (dummy is null)
            {
                break;
            }
            created.Add(dummy.DisplayId);
        }

        Thread.Sleep(_settleMilliseconds * 2);
        HashSet<uint> online = [.. DisplayCatalog.Online().Select(display => display.DisplayId)];
        int visible = created.Count(online.Contains);

        return ("multiple", created.Count == 3 && visible == 3 && created.Distinct().Count() == 3,
            $"created={created.Count}/3 visible={visible}/3 ids=[{string.Join(",", created)}]");
    }

    private static (string, bool, string) CheckModes()
    {
        using DummyManager manager = new();
        Dummy? dummy = manager.Create(new DummySpec { Definition = Require(DEFINITION_ID) });
        if (dummy is null)
        {
            return ("modes", false, "could not create a dummy to enumerate");
        }

        IReadOnlyList<DisplayMode> modes = dummy.Modes();
        if (modes.Count == 0)
        {
            return ("modes", false, "no modes reported");
        }

        int hiDpiCount = modes.Count(mode => mode.IsHiDpi);
        DisplayMode before = modes.First();

        // Switch to the smallest mode, then confirm CoreGraphics reports the change.
        DisplayMode target = modes[^1];
        bool switched = dummy.TrySetMode(target);
        Thread.Sleep(_settleMilliseconds);
        DisplayInfo after = DisplayCatalog.Describe(dummy.DisplayId);
        bool applied = after.Width == target.Width && after.Height == target.Height;

        return ("modes", switched && applied,
            $"count={modes.Count} hidpi={hiDpiCount} largest={before} switchedTo={target} nowReports={after.Width}x{after.Height}");
    }

    private static (string, bool, string) CheckMirroring()
    {
        DisplayInfo? main = DisplayCatalog.Online().FirstOrDefault(display => display.IsMain);
        if (main is null)
        {
            return ("mirror", false, "no main display found");
        }

        using DummyManager manager = new();
        Dummy? dummy = manager.Create(new DummySpec { Definition = Require(DEFINITION_ID) });
        if (dummy is null)
        {
            return ("mirror", false, "could not create the dummy to mirror onto");
        }

        Thread.Sleep(_settleMilliseconds);
        bool applied = DisplayCatalog.SetMirror(dummy.DisplayId, main.DisplayId);
        Thread.Sleep(_settleMilliseconds);
        DisplayInfo after = DisplayCatalog.Describe(dummy.DisplayId);

        DisplayCatalog.ClearMirror(dummy.DisplayId);
        Thread.Sleep(_settleMilliseconds);
        DisplayInfo cleared = DisplayCatalog.Describe(dummy.DisplayId);

        return ("mirror", applied && after.MirrorsDisplayId == main.DisplayId && !cleared.IsMirroring,
            $"configure={applied} mirrorsAfterSet={after.MirrorsDisplayId} mirrorsAfterClear={cleared.MirrorsDisplayId}");
    }

    private static (string, bool, string) ObserveRefreshRate()
    {
        List<string> observations = [];
        bool allHonored = true;

        foreach (double candidate in (double[])[60, 120, 144])
        {
            using DummyManager manager = new();
            Dummy? dummy = manager.Create(new DummySpec { Definition = Require(DEFINITION_ID), RefreshRateOverride = candidate });
            if (dummy is null)
            {
                observations.Add($"{candidate:0}:createFailed");
                allHonored = false;
                continue;
            }

            Thread.Sleep(_settleMilliseconds);
            DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);
            allHonored &= Math.Abs(info.RefreshRate - candidate) < 0.5;
            observations.Add($"{candidate:0}:reported={info.RefreshRate:0.##}");
            Thread.Sleep(_cooldownMilliseconds);
        }

        return ($"refresh {(allHonored ? "(honored)" : "(observe)")}", true, string.Join(" ", observations));
    }

    private static (string, bool, string) CheckNoOrphans()
    {
        const int TIMEOUT_SECONDS = 20;

        List<uint> suspects = [];
        int elapsed = 0;
        while (elapsed < TIMEOUT_SECONDS)
        {
            suspects = [.. DisplayCatalog.Online()
                .Where(display => display.VendorId == DummySpec.VENDOR_ID)
                .Select(display => display.DisplayId)];
            if (suspects.Count == 0)
            {
                return ("no orphans", true, $"cleared after {elapsed}s");
            }
            Thread.Sleep(1000);
            elapsed++;
        }

        return ("no orphans", false, $"still present after {TIMEOUT_SECONDS}s: {string.Join(",", suspects)}");
    }

    private static bool IsOnline(uint displayId)
        => DisplayCatalog.Online().Any(display => display.DisplayId == displayId);

    private static DummyDefinition Require(string id)
        => DummyDefinitionCatalog.Find(id) ?? throw new InvalidOperationException($"definition missing: {id}");

    private static void Report(List<(string Name, bool Passed, string Detail)> results)
    {
        Console.WriteLine();
        foreach ((string name, bool passed, string detail) in results)
        {
            Console.WriteLine($"[{(passed ? "PASS" : "FAIL")}] {name,-18} {detail}");
        }
        Console.WriteLine();
        Console.WriteLine($"{results.Count(entry => entry.Passed)}/{results.Count} passed");
    }
}
