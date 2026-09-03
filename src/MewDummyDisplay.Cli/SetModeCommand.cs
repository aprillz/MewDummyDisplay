using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

internal static class SetModeCommand
{
    internal static int Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: mdd setmode <display-id> <width> <height> [--hidpi]");
            return 2;
        }

        uint displayId = uint.Parse(args[0]);
        int width = int.Parse(args[1]);
        int height = int.Parse(args[2]);
        bool requireHiDpi = args.Contains("--hidpi");

        DisplayMode? target = DisplayCatalog.Modes(displayId)
            .FirstOrDefault(mode => mode.Width == width && mode.Height == height && (!requireHiDpi || mode.IsHiDpi));

        if (target is null)
        {
            Console.Error.WriteLine($"No {width}x{height}{(requireHiDpi ? " HiDPI" : "")} mode on display {displayId}.");
            Console.Error.WriteLine("Run 'mdd modes <display-id>' to see what is available.");
            return 1;
        }

        if (!DisplayCatalog.TrySetMode(displayId, target))
        {
            Console.Error.WriteLine($"Failed to switch display {displayId} to {target}.");
            return 1;
        }

        Console.WriteLine($"Display {displayId} switched to {target}.");
        return 0;
    }
}
