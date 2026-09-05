using Aprillz.MewDummyDisplay.Interop;

namespace Aprillz.MewDummyDisplay.App;

// Drives the real menu the way AppKit does, by sending the action selector to the target
// with a menu item as sender. That exercises the whole path, from the row a person would
// click through to the virtual display, with nobody clicking.
//
// Turning a display on and off is driven through the menu, because that dispatch is the
// fragile part. Creating and removing goes through the library, as the window does.
internal static class SelfTest
{
    /// <summary>
    /// A fixed identity for the test display.
    /// </summary>
    /// <remarks>
    /// Fixed so that repeated runs reuse one identity. See
    /// <see cref="DummyRecord.SerialNumber"/>.
    /// </remarks>
    private const uint TEST_SERIAL = 0x5E1F7E57;

    internal static bool Run(MenuBarController controller)
    {
        bool passed = true;

        passed &= Check("no dummies at startup", controller.Dummies.Count == 0);

        DummyDefinition definition = DummyDefinitionCatalog.Find("16:9")!;
        Dummy? dummy = controller.Manager.Create(new DummySpec
        {
            Definition = definition,
            Name = "MewDummy self test",
            SerialNumber = TEST_SERIAL,
        });
        controller.Rebuild();

        passed &= Check("dummy created", dummy is not null && controller.Dummies.Count == 1);
        if (dummy is null)
        {
            Console.WriteLine("FAIL  could not create a dummy");
            return false;
        }

        DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);
        Console.WriteLine($"  {dummy.Name}: {info.Width}x{info.Height}, {dummy.Modes().Count} modes, " +
            $"{definition.CommonResolutions().Count} offered in the picker");
        passed &= Check("connected on creation", dummy.IsConnected);
        passed &= Check("has a desktop rectangle", info.Bounds.Width > 0);
        passed &= Check("reports modes", dummy.Modes().Count > 0);

        passed &= Check("tray row dispatched", Select(controller, dummy.Name));
        passed &= Check("turned off", !dummy.IsConnected);
        passed &= Check("still defined while off", controller.Dummies.Count == 1);
        passed &= Check("display id cleared while off", dummy.DisplayId == 0);

        passed &= Check("tray row dispatched again", Select(controller, dummy.Name));
        passed &= Check("turned back on", dummy.IsConnected);
        passed &= Check("reports modes again", dummy.Modes().Count > 0);

        passed &= Check("manage row dispatched", Select(controller, MewDummyDisplayStrings.MenuManage.Value));

        // The row opens the window inline. Showing it activates the application, which the
        // window server only grants to one the user launched, so a binary driven from a
        // terminal cannot assert visibility. What the test guards is that the window builds
        // and lists what it should.
        controller.ManageWindow.Show();
        passed &= Check("management window opened", controller.ManageWindow.IsOpen);
        passed &= Check("window lists the dummy", controller.ManageWindow.RowCount == 1);

        controller.ManageWindow.CloseIfOpen();
        passed &= Check("management window closed", !controller.ManageWindow.IsOpen);

        controller.Manager.Remove(dummy);
        controller.Rebuild();
        passed &= Check("removed", controller.Dummies.Count == 0);

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
