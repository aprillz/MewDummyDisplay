using Aprillz.MewDummyDisplay.Interop;

namespace Aprillz.MewDummyDisplay.App;

// Checks that a menu bar item dispatches clicks into C#, including from a NativeAOT
// binary, where the callback has to be an UnmanagedCallersOnly function pointer.
//
// The self test sends the action selector to the target with each menu item as sender,
// which is exactly what AppKit does on a click, so the dispatch path is verified without
// anyone having to click.
internal static class MenuSpike
{
    internal static int Run(bool interactive)
    {
        nint application = AppKitInterop.StartAccessoryApplication();
        Console.WriteLine($"NSApplication started with the accessory policy (handle {application:X})");

        StatusItemHost host = new(MenuBarIcon.Create("MewDummyDisplay"), "MDD");
        List<string> fired = [];

        host.SetMenu(
        [
            new MenuEntry { Title = "MewDummyDisplay menu spike" },
            MenuEntry.Separator,
            new MenuEntry { Title = "First action", Handler = () => fired.Add("first") },
            new MenuEntry { Title = "Checked row", IsChecked = true, Handler = () => fired.Add("checked") },
            MenuEntry.Separator,
            new MenuEntry { Title = "Quit", KeyEquivalent = "q", Handler = () => fired.Add("quit") },
        ]);

        long itemCount = AppKitInterop.SendLong(host.Menu, AppKitInterop.SelNumberOfItems);
        Console.WriteLine($"Menu built with {itemCount} items");

        int dispatched = 0;
        for (long index = 0; index < itemCount; index++)
        {
            nint item = AppKitInterop.SendPtr_Long(host.Menu, AppKitInterop.SelItemAtIndex, index);
            long tag = AppKitInterop.SendLong(item, AppKitInterop.SelTag);
            if (tag == 0)
            {
                continue;
            }

            // Same call AppKit makes when the row is chosen.
            ObjC.SendVoid_Ptr(MenuActionTarget.Instance, MenuActionTarget.Selector, item);
            dispatched++;
        }

        Console.WriteLine($"Dispatched {dispatched} actions, handlers ran: {string.Join(", ", fired)}");

        bool passed = dispatched == 3 && fired.Count == 3;
        Console.WriteLine(passed
            ? "PASS  menu actions reach managed code"
            : "FAIL  menu actions did not reach managed code");

        if (!interactive)
        {
            return passed ? 0 : 1;
        }

        Console.WriteLine("Status item is in the menu bar. Select Quit to exit.");
        ObjC.SendVoid(application, AppKitInterop.SelRun);
        return 0;
    }
}
