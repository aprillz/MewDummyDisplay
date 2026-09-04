using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>AppKit entry points needed for a menu bar only application.</summary>
internal static class AppKitInterop
{
    private const string LIBOBJC = "/usr/lib/libobjc.A.dylib";

    /// <summary>NSApplicationActivationPolicyAccessory: no Dock icon, no app switcher entry.</summary>
    internal const long ACTIVATION_POLICY_ACCESSORY = 1;

    /// <summary>NSVariableStatusItemLength.</summary>
    internal const double VARIABLE_STATUS_ITEM_LENGTH = -1.0;

    internal const long CONTROL_STATE_OFF = 0;
    internal const long CONTROL_STATE_ON = 1;

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Double(nint receiver, nint selector, double arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Ptr_Ptr_Ptr(nint receiver, nint selector, nint first, nint second, nint third);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid_Long(nint receiver, nint selector, long arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern long SendLong(nint receiver, nint selector);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern byte SendBool_Long(nint receiver, nint selector, long arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Ptr_Ptr(nint receiver, nint selector, nint first, nint second);

    // AppKit is not linked, so it has to be loaded before any of its classes resolve.
    // This field is declared first so it initializes before the class lookups below.
    private static readonly bool _frameworksLoaded = LoadFrameworks();

    private static bool LoadFrameworks()
    {
        const int RTLD_LAZY = 1;
        if (ObjC.dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", RTLD_LAZY) == 0)
        {
            throw new PlatformNotSupportedException("Could not load AppKit.");
        }
        return true;
    }

    // Classes

    internal static readonly nint NSApplication = ObjC.RequireClass("NSApplication");
    internal static readonly nint NSStatusBar = ObjC.RequireClass("NSStatusBar");
    internal static readonly nint NSMenu = ObjC.RequireClass("NSMenu");
    internal static readonly nint NSMenuItem = ObjC.RequireClass("NSMenuItem");
    internal static readonly nint NSImage = ObjC.RequireClass("NSImage");
    internal static readonly nint NSView = ObjC.RequireClass("NSView");
    internal static readonly nint NSTextField = ObjC.RequireClass("NSTextField");

    /// <summary>NSSwitch is macOS 10.15 and later, so its absence is handled rather than assumed.</summary>
    internal static readonly nint NSSwitch = ObjC.TryClass("NSSwitch");

    // Selectors

    internal static readonly nint SelSharedApplication = ObjC.Sel("sharedApplication");
    internal static readonly nint SelSetActivationPolicy = ObjC.Sel("setActivationPolicy:");
    internal static readonly nint SelRun = ObjC.Sel("run");
    internal static readonly nint SelTerminate = ObjC.Sel("terminate:");
    internal static readonly nint SelSystemStatusBar = ObjC.Sel("systemStatusBar");
    internal static readonly nint SelStatusItemWithLength = ObjC.Sel("statusItemWithLength:");
    internal static readonly nint SelButton = ObjC.Sel("button");
    internal static readonly nint SelSetTitle = ObjC.Sel("setTitle:");
    internal static readonly nint SelSetImage = ObjC.Sel("setImage:");
    internal static readonly nint SelSetMenu = ObjC.Sel("setMenu:");
    internal static readonly nint SelInitWithTitle = ObjC.Sel("initWithTitle:");
    internal static readonly nint SelInitMenuItem = ObjC.Sel("initWithTitle:action:keyEquivalent:");
    internal static readonly nint SelAddItem = ObjC.Sel("addItem:");
    internal static readonly nint SelRemoveAllItems = ObjC.Sel("removeAllItems");
    internal static readonly nint SelSeparatorItem = ObjC.Sel("separatorItem");
    internal static readonly nint SelSetTarget = ObjC.Sel("setTarget:");
    internal static readonly nint SelSetTag = ObjC.Sel("setTag:");
    internal static readonly nint SelTag = ObjC.Sel("tag");
    internal static readonly nint SelSetEnabled = ObjC.Sel("setEnabled:");
    internal static readonly nint SelImageWithSymbol = ObjC.Sel("imageWithSystemSymbolName:accessibilityDescription:");
    internal static readonly nint SelNumberOfItems = ObjC.Sel("numberOfItems");
    internal static readonly nint SelItemAtIndex = ObjC.Sel("itemAtIndex:");
    internal static readonly nint SelTitle = ObjC.Sel("title");
    internal static readonly nint SelInitWithFrame = ObjC.Sel("initWithFrame:");
    internal static readonly nint SelAddSubview = ObjC.Sel("addSubview:");
    internal static readonly nint SelSetView = ObjC.Sel("setView:");
    internal static readonly nint SelSetStringValue = ObjC.Sel("setStringValue:");
    internal static readonly nint SelSetBezeled = ObjC.Sel("setBezeled:");
    internal static readonly nint SelSetDrawsBackground = ObjC.Sel("setDrawsBackground:");
    internal static readonly nint SelSetEditable = ObjC.Sel("setEditable:");
    internal static readonly nint SelSetSelectable = ObjC.Sel("setSelectable:");
    internal static readonly nint SelSetAction = ObjC.Sel("setAction:");
    internal static readonly nint SelSetState = ObjC.Sel("setState:");

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Long(nint receiver, nint selector, long arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Rect(nint receiver, nint selector, CGRect frame);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid_Byte(nint receiver, nint selector, byte arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern byte SendBool(nint receiver, nint selector);

    internal static readonly nint SelActivationPolicy = ObjC.Sel("activationPolicy");

    /// <summary>The shared NSApplication instance.</summary>
    internal static nint SharedApplication() => ObjC.SendPtr(NSApplication, SelSharedApplication);

    /// <summary>Starts NSApplication as a menu bar only application.</summary>
    internal static nint StartAccessoryApplication()
    {
        nint application = SharedApplication();
        SendVoid_Long(application, SelSetActivationPolicy, ACTIVATION_POLICY_ACCESSORY);
        return application;
    }

    /// <summary>Reapplies the accessory policy, which other hosts may have overwritten.</summary>
    internal static void ApplyAccessoryPolicy()
        => SendVoid_Long(SharedApplication(), SelSetActivationPolicy, ACTIVATION_POLICY_ACCESSORY);

    /// <summary>Reads the current activation policy.</summary>
    internal static long CurrentActivationPolicy()
        => SendLong(SharedApplication(), SelActivationPolicy);

    /// <summary>Creates an SF Symbol image, or 0 when the symbol is unavailable.</summary>
    internal static nint SymbolImage(string symbolName, string accessibilityDescription)
    {
        nint name = ObjC.NewString(symbolName);
        nint description = ObjC.NewString(accessibilityDescription);
        try
        {
            return SendPtr_Ptr_Ptr(NSImage, SelImageWithSymbol, name, description);
        }
        finally
        {
            ObjC.Release(name);
            ObjC.Release(description);
        }
    }
}
