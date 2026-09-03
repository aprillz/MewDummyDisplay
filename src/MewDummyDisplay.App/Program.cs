using Aprillz.MewDummyDisplay.Interop;
using Aprillz.MewUI;

namespace Aprillz.MewDummyDisplay.App;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsMacOS())
        {
            Console.Error.WriteLine("MewDummyDisplay runs on macOS only.");
            return 2;
        }

        if (args.Contains("--menu-spike"))
        {
            return MenuSpike.Run(interactive: args.Contains("--interactive"));
        }

        if (args.Contains("--host-spike"))
        {
            return HostSpike.Run(interactive: args.Contains("--interactive"));
        }

        return Run(selfTest: args.Contains("--self-test"));
    }

    private static int Run(bool selfTest)
    {
        MacOSPlatform.Register();
        MewVGMacOSBackend.Register();

        MenuBarController? controller = null;
        int exitCode = 0;

        Application.Create()
            .WithShutdownMode(ShutdownMode.OnExplicitShutdown)
            .OnStartup(_ =>
            {
                // MewUI applies the regular activation policy while starting, which would
                // give a menu bar utility a Dock icon, so it is reasserted here.
                AppKitInterop.ApplyAccessoryPolicy();

                controller = new MenuBarController();

                if (selfTest)
                {
                    Console.WriteLine($"supported     : {DummyManager.IsSupported}");
                    Console.WriteLine($"windows open  : {Application.Current.AllWindows.Count}");
                    Console.WriteLine($"policy        : {AppKitInterop.CurrentActivationPolicy()}");
                    exitCode = SelfTest.Run(controller) ? 0 : 1;
                    Application.Shutdown();
                }
            })
            .Run();

        controller?.Dispose();
        return exitCode;
    }
}
