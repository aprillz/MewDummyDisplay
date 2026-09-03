namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>
/// A menu row carrying a real NSSwitch beside its label.
/// </summary>
/// <remarks>
/// A check mark is the usual way a menu shows state, but a switch reads as something to
/// flip rather than something that merely happened, which suits turning a display on and
/// off. NSMenuItem allows an arbitrary view, so the row is a plain NSView holding a label
/// and a switch, and the switch carries the same tag the action dispatch looks up.
///
/// The view is manually laid out. A menu row is a fixed strip, so a constraint system
/// would buy nothing here.
/// </remarks>
internal static class SwitchMenuItem
{
    private const double ROW_WIDTH = 300;
    private const double ROW_HEIGHT = 26;
    private const double LABEL_LEFT = 14;
    private const double SWITCH_WIDTH = 38;
    private const double SWITCH_HEIGHT = 22;
    private const double SWITCH_RIGHT_INSET = 14;

    /// <summary>Whether this OS provides NSSwitch.</summary>
    internal static bool IsAvailable => AppKitInterop.NSSwitch != 0;

    /// <summary>Builds the view for a row and returns it, already holding the switch.</summary>
    internal static nint BuildView(string label, bool isOn, long tag)
    {
        nint view = AppKitInterop.SendPtr_Rect(
            ObjC.SendPtr(AppKitInterop.NSView, ObjC.SelAlloc),
            AppKitInterop.SelInitWithFrame,
            new CGRect(0, 0, ROW_WIDTH, ROW_HEIGHT));

        AddLabel(view, label);
        AddSwitch(view, isOn, tag);
        return view;
    }

    private static void AddLabel(nint view, string label)
    {
        nint field = AppKitInterop.SendPtr_Rect(
            ObjC.SendPtr(AppKitInterop.NSTextField, ObjC.SelAlloc),
            AppKitInterop.SelInitWithFrame,
            new CGRect(LABEL_LEFT, 4, ROW_WIDTH - LABEL_LEFT - SWITCH_WIDTH - SWITCH_RIGHT_INSET - 8, 18));

        nint text = ObjC.NewString(label);
        ObjC.SendVoid_Ptr(field, AppKitInterop.SelSetStringValue, text);
        ObjC.Release(text);

        // A plain label: no border, no background, not interactive.
        AppKitInterop.SendVoid_Byte(field, AppKitInterop.SelSetBezeled, 0);
        AppKitInterop.SendVoid_Byte(field, AppKitInterop.SelSetDrawsBackground, 0);
        AppKitInterop.SendVoid_Byte(field, AppKitInterop.SelSetEditable, 0);
        AppKitInterop.SendVoid_Byte(field, AppKitInterop.SelSetSelectable, 0);

        ObjC.SendVoid_Ptr(view, AppKitInterop.SelAddSubview, field);
        ObjC.Release(field);
    }

    private static void AddSwitch(nint view, bool isOn, long tag)
    {
        nint control = AppKitInterop.SendPtr_Rect(
            ObjC.SendPtr(AppKitInterop.NSSwitch, ObjC.SelAlloc),
            AppKitInterop.SelInitWithFrame,
            new CGRect(ROW_WIDTH - SWITCH_WIDTH - SWITCH_RIGHT_INSET, 2, SWITCH_WIDTH, SWITCH_HEIGHT));

        AppKitInterop.SendVoid_Long(control, AppKitInterop.SelSetState, isOn ? 1 : 0);
        AppKitInterop.SendVoid_Long(control, AppKitInterop.SelSetTag, tag);
        ObjC.SendVoid_Ptr(control, AppKitInterop.SelSetTarget, MenuActionTarget.Instance);
        ObjC.SendVoid_Ptr(control, AppKitInterop.SelSetAction, MenuActionTarget.Selector);

        ObjC.SendVoid_Ptr(view, AppKitInterop.SelAddSubview, control);
        ObjC.Release(control);
    }
}
