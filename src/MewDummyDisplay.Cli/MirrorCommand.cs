using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

internal static class MirrorCommand
{
    internal static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: mdd mirror <target-display-id> <source-display-id>");
            Console.Error.WriteLine("Pass 0 as the source to turn mirroring off.");
            return 2;
        }

        uint target = uint.Parse(args[0]);
        uint source = uint.Parse(args[1]);

        bool applied = source == 0
            ? DisplayCatalog.ClearMirror(target)
            : DisplayCatalog.SetMirror(target, source);

        if (!applied)
        {
            Console.Error.WriteLine("Display configuration failed.");
            return 1;
        }

        Console.WriteLine(source == 0
            ? $"Display {target} no longer mirrors."
            : $"Display {target} now mirrors {source}.");
        return 0;
    }
}
