namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>Owns the menu bar item and rebuilds its menu on demand.</summary>
/// <remarks>
/// The menu deliberately holds only what gets used repeatedly: a toggle per dummy, the
/// entries that open windows, and quit. Everything denser lives in a window, which keeps
/// this the small part of the interop surface.
/// </remarks>
internal sealed class StatusItemHost
{
    private readonly nint _statusItem;
    private nint _menu;

    internal StatusItemHost(string symbolName, string fallbackTitle)
    {
        nint statusBar = ObjC.SendPtr(AppKitInterop.NSStatusBar, AppKitInterop.SelSystemStatusBar);
        _statusItem = AppKitInterop.SendPtr_Double(
            statusBar, AppKitInterop.SelStatusItemWithLength, AppKitInterop.VARIABLE_STATUS_ITEM_LENGTH);

        nint button = ObjC.SendPtr(_statusItem, AppKitInterop.SelButton);
        nint image = AppKitInterop.SymbolImage(symbolName, fallbackTitle);
        if (button != 0 && image != 0)
        {
            ObjC.SendVoid_Ptr(button, AppKitInterop.SelSetImage, image);
        }
        else if (button != 0)
        {
            nint title = ObjC.NewString(fallbackTitle);
            ObjC.SendVoid_Ptr(button, AppKitInterop.SelSetTitle, title);
            ObjC.Release(title);
        }
    }

    /// <summary>The live NSMenu, so callers can inspect what was built.</summary>
    internal nint Menu => _menu;

    /// <summary>Replaces the menu with the given entries.</summary>
    internal void SetMenu(IReadOnlyList<MenuEntry> entries)
    {
        MenuActionTarget.Clear();

        nint title = ObjC.NewString("");
        nint menu = ObjC.SendPtr_Ptr(ObjC.SendPtr(AppKitInterop.NSMenu, ObjC.SelAlloc), AppKitInterop.SelInitWithTitle, title);
        ObjC.Release(title);

        foreach (MenuEntry entry in entries)
        {
            ObjC.SendVoid_Ptr(menu, AppKitInterop.SelAddItem, entry.IsSeparator ? SeparatorItem() : BuildItem(entry));
        }

        ObjC.SendVoid_Ptr(_statusItem, AppKitInterop.SelSetMenu, menu);

        if (_menu != 0)
        {
            ObjC.Release(_menu);
        }
        _menu = menu;
    }

    private static nint SeparatorItem()
        => ObjC.SendPtr(AppKitInterop.NSMenuItem, AppKitInterop.SelSeparatorItem);

    private static nint BuildItem(MenuEntry entry)
    {
        nint title = ObjC.NewString(entry.Title);
        nint keyEquivalent = ObjC.NewString(entry.KeyEquivalent);
        try
        {
            nint item = AppKitInterop.SendPtr_Ptr_Ptr_Ptr(
                ObjC.SendPtr(AppKitInterop.NSMenuItem, ObjC.SelAlloc),
                AppKitInterop.SelInitMenuItem,
                title,
                entry.Handler is null ? 0 : MenuActionTarget.Selector,
                keyEquivalent);

            if (entry.Handler is not null)
            {
                ObjC.SendVoid_Ptr(item, AppKitInterop.SelSetTarget, MenuActionTarget.Instance);
                AppKitInterop.SendVoid_Long(item, AppKitInterop.SelSetTag, MenuActionTarget.Register(entry.Handler));
            }
            else
            {
                AppKitInterop.SendBool_Long(item, AppKitInterop.SelSetEnabled, 0);
            }

            if (entry.UseSwitch && SwitchMenuItem.IsAvailable && entry.Handler is not null)
            {
                // The row draws itself: the switch inside carries the same tag, so a flip
                // dispatches through the shared action target like any other row.
                long tag = AppKitInterop.SendLong(item, AppKitInterop.SelTag);
                nint view = SwitchMenuItem.BuildView(entry.Title, entry.IsChecked, tag);
                ObjC.SendVoid_Ptr(item, AppKitInterop.SelSetView, view);
                ObjC.Release(view);
            }
            else if (entry.IsChecked)
            {
                AppKitInterop.SendVoid_Long(item, AppKitInterop.SelSetState, AppKitInterop.CONTROL_STATE_ON);
            }

            return item;
        }
        finally
        {
            ObjC.Release(title);
            ObjC.Release(keyEquivalent);
        }
    }
}

/// <summary>One row in the menu bar menu.</summary>
internal sealed record MenuEntry
{
    internal required string Title { get; init; }

    /// <summary>Null leaves the row disabled, which is how headings are drawn.</summary>
    internal Action? Handler { get; init; }

    internal bool IsChecked { get; init; }

    internal bool IsSeparator { get; init; }

    /// <summary>Draw the row with an NSSwitch instead of a check mark.</summary>
    internal bool UseSwitch { get; init; }

    internal string KeyEquivalent { get; init; } = "";

    internal static MenuEntry Separator => new() { Title = "", IsSeparator = true };
}
