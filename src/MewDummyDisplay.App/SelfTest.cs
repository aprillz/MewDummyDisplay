using Aprillz.MewDummyDisplay.Interop;

namespace Aprillz.MewDummyDisplay.App;

// Drives the real menu the way AppKit does, by sending the action selector to the target
// with a menu item as sender. That exercises the whole path, from the row a person would
// click through to the virtual display, with nobody clicking.
internal static class SelfTest
{
    internal static bool Run(MenuBarController controller)
    {
        bool passed = true;

        passed &= Check("no dummies at startup", controller.Dummies.Count == 0);

        passed &= Check("add row found and dispatched", Select(controller, "16:9 ("));
        passed &= Check("one dummy created", controller.Dummies.Count == 1);

        if (controller.Dummies.Count == 1)
        {
            Dummy dummy = controller.Dummies[0];
            DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);
            Console.WriteLine($"  dummy {dummy.SerialNumber:X8} at {info.Width}x{info.Height}, {dummy.Modes().Count} modes");
            passed &= Check("dummy has a desktop rectangle", info.Bounds.Width > 0);
            passed &= Check("dummy reports modes", dummy.Modes().Count > 0);

            passed &= Check("mirror row dispatched", Select(controller, "16:9  "));
            passed &= Check("mirroring turned on", DisplayCatalog.Describe(dummy.DisplayId).IsMirroring);

            passed &= Check("mirror row dispatched again", Select(controller, "16:9  "));
            passed &= Check("mirroring turned off", !DisplayCatalog.Describe(dummy.DisplayId).IsMirroring);
        }

        passed &= Check("manage row dispatched", Select(controller, MewDummyDisplayStrings.MenuManage.Value));
        passed &= Check("management window opened", controller.ManageWindow.IsOpen);
        passed &= Check("window lists the dummy", controller.ManageWindow.RowCount == 1);
        controller.ManageWindow.CloseIfOpen();
        passed &= Check("management window closed", !controller.ManageWindow.IsOpen);

        passed &= Check("remove all dispatched", Select(controller, MewDummyDisplayStrings.MenuRemoveAll.Value));
        passed &= Check("no dummies left", controller.Dummies.Count == 0);

        Console.WriteLine(passed ? "PASS  menu bar flow works end to end" : "FAIL  see the rows above");
        return passed;
    }

    /// <summary>Sends the action for the first row whose title starts with the prefix.</summary>
    private static bool Select(MenuBarController controller, string titlePrefix)
    {
        nint menu = controller.Host.Menu;
        long count = AppKitInterop.SendLong(menu, AppKitInterop.SelNumberOfItems);

        for (long index = 0; index < count; index++)
        {
            nint item = AppKitInterop.SendPtr_Long(menu, AppKitInterop.SelItemAtIndex, index);
            if (AppKitInterop.SendLong(item, AppKitInterop.SelTag) == 0)
            {
                continue;
            }

            string title = ObjC.ReadString(ObjC.SendPtr(item, AppKitInterop.SelTitle));
            if (!title.StartsWith(titlePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            ObjC.SendVoid_Ptr(MenuActionTarget.Instance, MenuActionTarget.Selector, item);
            return true;
        }

        return false;
    }

    private static bool Check(string what, bool condition)
    {
        Console.WriteLine($"  [{(condition ? "ok" : "NO")}] {what}");
        return condition;
    }
}
