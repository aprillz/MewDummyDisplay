using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

internal static class ModesCommand
{
    internal static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: mdd modes <display-id>");
            return 2;
        }

        uint displayId = uint.Parse(args[0]);
        IReadOnlyList<DisplayMode> modes = DisplayCatalog.Modes(displayId);
        if (modes.Count == 0)
        {
            Console.Error.WriteLine($"Display {displayId} reported no modes.");
            return 1;
        }

        DisplayInfo current = DisplayCatalog.Describe(displayId);
        Console.WriteLine($"{"POINTS",-12} {"PIXELS",-12} {"HZ",-6} {"HIDPI",-6} {"MODEID",-8} {"CURRENT"}");
        foreach (DisplayMode mode in modes)
        {
            bool isCurrent = mode.Width == current.Width
                && mode.Height == current.Height
                && mode.PixelWidth == current.PixelWidth;

            Console.WriteLine(
                $"{$"{mode.Width}x{mode.Height}",-12} " +
                $"{$"{mode.PixelWidth}x{mode.PixelHeight}",-12} " +
                $"{mode.RefreshRate,-6:0} " +
                $"{(mode.IsHiDpi ? "yes" : "no"),-6} " +
                $"{mode.ModeId,-8} " +
                (isCurrent ? "*" : ""));
        }
        Console.WriteLine($"\n{modes.Count} modes");
        return 0;
    }
}
