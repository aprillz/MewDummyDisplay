using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

// Guards the fragile part of the private mode path. CGSDisplayMode has two runs of
// unknown padding, so its layout can shift between OS releases and would then be
// read as garbage. A real display can be enumerated both ways, so comparing the two
// results tells us whether the layout still holds. Run this after every OS upgrade.
internal static class VerifyModesCommand
{
    internal static int Run(string[] args)
    {
        uint displayId = args.Length > 0
            ? uint.Parse(args[0])
            : DisplayCatalog.Online().FirstOrDefault(display => display.IsMain)?.DisplayId ?? 0;

        if (displayId == 0)
        {
            Console.Error.WriteLine("No display to verify against.");
            return 2;
        }

        IReadOnlyList<DisplayMode> publicModes = DisplayCatalog.Modes(displayId);
        IReadOnlyList<DisplayMode> privateModes = DisplayCatalog.ModesViaPrivateApi(displayId);

        Console.WriteLine($"display {displayId}");
        Console.WriteLine($"  public  path: {publicModes.Count} modes");
        Console.WriteLine($"  private path: {privateModes.Count} modes");

        if (publicModes.Count == 0)
        {
            Console.Error.WriteLine("The public path returned nothing, so there is no reference to compare against.");
            Console.Error.WriteLine("Pick a real display, not one this process created.");
            return 2;
        }

        if (privateModes.Count == 0)
        {
            Console.Error.WriteLine("The private path returned nothing. The entry points or the struct layout changed.");
            return 1;
        }

        // The private path is a superset: it exposes modes CoreGraphics hides from the
        // public list, such as the DCI sizes. So the test is that every public size shows
        // up in the private list, not the reverse, plus every decoded value being sane.
        HashSet<(int, int)> privateSizes = [.. privateModes.Select(mode => (mode.Width, mode.Height))];
        List<DisplayMode> missing = [.. publicModes.Where(mode => !privateSizes.Contains((mode.Width, mode.Height)))];

        int sane = privateModes.Count(mode => mode.Width is > 0 and < 32768 && mode.Height is > 0 and < 32768);
        int plausibleRefresh = privateModes.Count(mode => mode.RefreshRate is >= 0 and <= 480);
        int extra = privateSizes.Count - publicModes.Select(mode => (mode.Width, mode.Height)).Distinct().Count();

        Console.WriteLine($"  sizes within range      : {sane}/{privateModes.Count}");
        Console.WriteLine($"  refresh within range    : {plausibleRefresh}/{privateModes.Count}");
        Console.WriteLine($"  public sizes not covered: {missing.Count}");
        Console.WriteLine($"  extra sizes seen only privately: {extra}");

        foreach (DisplayMode mode in missing.Take(5))
        {
            Console.WriteLine($"    missing: {mode}");
        }

        bool layoutHolds = sane == privateModes.Count
            && plausibleRefresh == privateModes.Count
            && missing.Count == 0;

        Console.WriteLine();
        Console.WriteLine(layoutHolds
            ? "PASS  CGSDisplayMode layout still matches this OS."
            : "FAIL  CGSDisplayMode layout looks wrong on this OS. Re-derive it before trusting the private path.");
        return layoutHolds ? 0 : 1;
    }
}
