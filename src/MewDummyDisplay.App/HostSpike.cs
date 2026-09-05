using Aprillz.MewDummyDisplay.Interop;
using Aprillz.MewUI;

namespace Aprillz.MewDummyDisplay.App;

// Checks that MewUI can host a menu bar only application. Two things have to hold. MewUI must run with no window open, and the accessory
// activation policy has to survive: MewUI sets NSApplicationActivationPolicyRegular
// while starting up, which would put a Dock icon on a menu bar utility.
internal static class HostSpike
{
    internal static int Run(bool interactive)
    {
        MacOSPlatform.Register();
        MewVGMacOSBackend.Register();

        StatusItemHost? host = null;
        int exitCode = 1;

        Application.Create()
            .WithShutdownMode(ShutdownMode.OnExplicitShutdown)
            .OnStartup(_ =>
            {
                long policyAfterMewUI = AppKitInterop.CurrentActivationPolicy();
                Console.WriteLine($"activation policy after MewUI startup: {policyAfterMewUI}");

                // Reassert it: MewUI hardcodes the regular policy while starting.
                AppKitInterop.ApplyAccessoryPolicy();
                long policyAfterReassert = AppKitInterop.CurrentActivationPolicy();
                Console.WriteLine($"activation policy after reasserting  : {policyAfterReassert}");

                host = new StatusItemHost(MenuBarIcon.Create("MewDummyDisplay"), "MDD");
                host.SetMenu(
                [
                    new MenuEntry { Title = "MewDummyDisplay host spike" },
                    MenuEntry.Separator,
                    new MenuEntry { Title = "Quit", KeyEquivalent = "q", Handler = Application.Shutdown },
                ]);

                int windows = Application.Current.AllWindows.Count;
                Console.WriteLine($"user windows open: {windows}");
                Console.WriteLine($"status item created: {host.Menu != 0}");

                bool passed = policyAfterReassert == AppKitInterop.ACTIVATION_POLICY_ACCESSORY
                    && windows == 0
                    && host.Menu != 0;
                exitCode = passed ? 0 : 1;
                Console.WriteLine(passed
                    ? "PASS  MewUI hosts a windowless accessory app with a status item"
                    : "FAIL  see the values above");

                if (!interactive)
                {
                    Application.Shutdown();
                }
            })
            .Run();

        return exitCode;
    }
}
