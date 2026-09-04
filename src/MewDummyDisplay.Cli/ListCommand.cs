using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

internal static class ListCommand
{
    internal static int Run()
    {
        IReadOnlyList<DisplayInfo> displays = DisplayCatalog.Online();
        if (displays.Count == 0)
        {
            Console.WriteLine("No displays reported.");
            return 1;
        }

        Console.WriteLine($"{"ID",-10} {"NAME",-26} {"POINTS",-12} {"PIXELS",-12} {"HZ",-6} {"HIDPI",-6} {"FLAGS"}");
        foreach (DisplayInfo display in displays)
        {
            List<string> flags = [];
            if (display.IsBuiltIn)
            {
                flags.Add("builtin");
            }
            if (display.IsMain)
            {
                flags.Add("main");
            }
            if (display.IsMirroring)
            {
                flags.Add($"mirrors:{display.MirrorsDisplayId}");
            }
            if (display.Rotation != 0)
            {
                flags.Add($"rotation:{display.Rotation:0}");
            }

            Console.WriteLine(
                $"{display.DisplayId,-10} " +
                $"{display.Name ?? "-",-26} " +
                $"{$"{display.Width}x{display.Height}",-12} " +
                $"{$"{display.PixelWidth}x{display.PixelHeight}",-12} " +
                $"{display.RefreshRate,-6:0} " +
                $"{(display.IsHiDpi ? "yes" : "no"),-6} " +
                string.Join(" ", flags));
        }
        return 0;
    }
}
