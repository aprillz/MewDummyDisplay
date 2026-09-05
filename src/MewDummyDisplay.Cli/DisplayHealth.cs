using System.Diagnostics;
using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

// Rapid create and release can wedge the window server for a whole login session:
// display enumeration stops returning at all, and only a reboot clears it.
//
// Every command that creates displays checks here first and aborts when the display
// subsystem is already slow. A wedged session cannot be repaired from user space, so
// the only useful behavior is to stop early and say so.
internal static class DisplayHealth
{
    /// <summary>Above this, the display subsystem is considered unhealthy.</summary>
    private const int SLOW_THRESHOLD_MS = 2000;

    /// <summary>Never create more than this many displays in one process.</summary>
    internal const int MAX_DISPLAYS_PER_RUN = 2;

    /// <summary>Never run more than this many create and release cycles in one process.</summary>
    internal const int MAX_CYCLES_PER_RUN = 6;

    /// <summary>Times a display enumeration. Returns false when the subsystem is too slow.</summary>
    internal static bool Check(string stage)
    {
        Stopwatch watch = Stopwatch.StartNew();
        int count = DisplayCatalog.Online().Count;
        watch.Stop();

        if (watch.ElapsedMilliseconds <= SLOW_THRESHOLD_MS)
        {
            return true;
        }

        Console.Error.WriteLine();
        Console.Error.WriteLine($"Display subsystem is unhealthy at stage '{stage}'.");
        Console.Error.WriteLine($"  enumerating {count} displays took {watch.ElapsedMilliseconds}ms");
        Console.Error.WriteLine("Stopping. A wedged window server cannot be repaired from user space;");
        Console.Error.WriteLine("log out and back in, or reboot, before running this again.");
        return false;
    }
}
