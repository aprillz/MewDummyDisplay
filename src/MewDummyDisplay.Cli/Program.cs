using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

// Developer tool. Output is English only and deliberately not localized.
internal static class Program
{
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsMacOS())
        {
            Console.Error.WriteLine("mdd runs on macOS only.");
            return 2;
        }

        string command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

        return command switch
        {
            "probe" => ProbeCommand.Run(args[1..]),
            "list" => ListCommand.Run(),
            "modes" => ModesCommand.Run(args[1..]),
            "setmode" => SetModeCommand.Run(args[1..]),
            "definitions" => DefinitionsCommand.Run(),
            "create" => CreateCommand.Run(args[1..]),
            "mirror" => MirrorCommand.Run(args[1..]),
            "stress" => StressCommand.Run(args[1..]),
            "help" or "--help" or "-h" => Help(),
            _ => Unknown(command),
        };
    }

    private static int Help()
    {
        Console.WriteLine("""
            mdd - MewDummyDisplay command line tool

            Commands:
              probe [--keep <seconds>]   Run the Phase 0 feasibility checks
              list                       List online displays
              modes <display-id>         List the modes a display offers
              setmode <display-id> <w> <h> [--hidpi]
                                         Switch a display to a mode
              definitions                List built-in aspect ratio definitions
              create <id> [options]      Create a dummy and hold it until Enter
              mirror <target> <source>   Mirror source onto target; source 0 clears
              stress [--count N] [--gap MS] [--hold-all]
                                         Repeated create/release health check
              help                       Show this text

            create options:
              --lodpi                    Disable HiDPI (default is enabled)
              --hold <seconds>           Hold for N seconds instead of waiting for Enter
              --refresh <hz>             Refresh rate to request (probing only)
              --sls-rotate <degrees>     SkyLight rotation probe (unresolved research)
              --sls-int                  Use the int variant of the rotation probe

            A dummy exists only while the creating process is alive.
            """);
        return 0;
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        Console.Error.WriteLine("Run 'mdd help' for usage.");
        return 2;
    }
}
