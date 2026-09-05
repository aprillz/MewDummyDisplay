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

        return Run(selfTest: args.Contains("--self-test"), openWindow: args.Contains("--open-window"));
    }

    private static int Run(bool selfTest, bool openWindow = false)
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

                controller = new MenuBarController(persist: !selfTest);

                // A development aid: the window is otherwise only reachable through the
                // status item, which nothing but a person can click.
                if (openWindow)
                {
                    MenuBarController opener = controller;
                    DispatcherTimer show = new(TimeSpan.FromMilliseconds(400));
                    show.Tick += () => { show.Stop(); opener.ManageWindow.Show(); };
                    show.Start();
                }

                if (selfTest)
                {
                    Console.WriteLine($"supported     : {DummyManager.IsSupported}");
                    Console.WriteLine($"windows open  : {Application.Current.AllWindows.Count}");
                    Console.WriteLine($"policy        : {AppKitInterop.CurrentActivationPolicy()}");

                    // Run once startup has finished rather than inside it, so the test sees
                    // the application in its ordinary running state.
                    MenuBarController target = controller;
                    DispatcherTimer timer = new(TimeSpan.FromMilliseconds(250));
                    timer.Tick += () =>
                    {
                        timer.Stop();
                        exitCode = SelfTest.Run(target) ? 0 : 1;
                        Application.Shutdown();
                    };
                    timer.Start();
                }
            })
            .Run();

        controller?.Dispose();
        return exitCode;
    }
}
