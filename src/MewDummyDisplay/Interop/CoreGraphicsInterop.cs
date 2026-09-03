using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>Public CoreGraphics display APIs and the libdispatch queue accessor.</summary>
internal static class CoreGraphicsInterop
{
    private const string COREGRAPHICS = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string LIBSYSTEM = "/usr/lib/libSystem.B.dylib";
    private const string CORE_FOUNDATION = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    internal const int QOS_CLASS_USER_INTERACTIVE = 0x21;
    internal const int MAX_DISPLAYS = 32;

    /// <summary>kCGNullDirectDisplay, passed as the source to turn mirroring off.</summary>
    internal const uint NULL_DISPLAY = 0;

    /// <summary>kCGConfigureForSession.</summary>
    internal const uint CONFIGURE_FOR_SESSION = 1;

    /// <summary>kCGConfigurePermanently.</summary>
    internal const uint CONFIGURE_PERMANENTLY = 2;

    [DllImport(LIBSYSTEM)]
    internal static extern nint dispatch_get_global_queue(nint identifier, nuint flags);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGGetOnlineDisplayList(uint maxDisplays, [Out] uint[] displays, out uint count);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGGetActiveDisplayList(uint maxDisplays, [Out] uint[] displays, out uint count);

    [DllImport(COREGRAPHICS)]
    internal static extern CGRect CGDisplayBounds(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGDisplayIsBuiltin(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGDisplayIsMain(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGDisplayIsActive(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGDisplayIsOnline(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGDisplayIsAsleep(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGDisplayIsInMirrorSet(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern uint CGDisplayVendorNumber(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern uint CGDisplayModelNumber(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern uint CGDisplaySerialNumber(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern double CGDisplayRotation(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern uint CGDisplayMirrorsDisplay(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern nint CGDisplayCopyDisplayMode(uint display);

    [DllImport(COREGRAPHICS)]
    internal static extern nuint CGDisplayModeGetWidth(nint mode);

    [DllImport(COREGRAPHICS)]
    internal static extern nuint CGDisplayModeGetHeight(nint mode);

    [DllImport(COREGRAPHICS)]
    internal static extern nuint CGDisplayModeGetPixelWidth(nint mode);

    [DllImport(COREGRAPHICS)]
    internal static extern nuint CGDisplayModeGetPixelHeight(nint mode);

    [DllImport(COREGRAPHICS)]
    internal static extern double CGDisplayModeGetRefreshRate(nint mode);

    [DllImport(COREGRAPHICS)]
    internal static extern void CGDisplayModeRelease(nint mode);

    [DllImport(COREGRAPHICS)]
    internal static extern nint CGDisplayCopyAllDisplayModes(uint display, nint options);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGDisplayModeGetIODisplayModeID(nint mode);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGConfigureDisplayWithDisplayMode(nint configuration, uint display, nint mode, nint options);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGBeginDisplayConfiguration(out nint configuration);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGConfigureDisplayMirrorOfDisplay(nint configuration, uint display, uint master);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGCompleteDisplayConfiguration(nint configuration, uint option);

    [DllImport(COREGRAPHICS)]
    internal static extern int CGCancelDisplayConfiguration(nint configuration);

    [DllImport(CORE_FOUNDATION)]
    internal static extern nint CFArrayGetCount(nint array);

    [DllImport(CORE_FOUNDATION)]
    internal static extern nint CFArrayGetValueAtIndex(nint array, nint index);

    [DllImport(CORE_FOUNDATION)]
    internal static extern void CFRelease(nint reference);

    /// <summary>
    /// Reads kCGDisplayShowDuplicateLowResolutionModes. Without it CGDisplayCopyAllDisplayModes
    /// hides the scaled variants that make HiDPI selectable.
    /// </summary>
    internal static nint ShowDuplicateLowResolutionModesKey()
    {
        nint library = ObjC.dlopen(COREGRAPHICS, 2);
        if (library == 0)
        {
            return 0;
        }

        nint symbol = ObjC.dlsym(library, "kCGDisplayShowDuplicateLowResolutionModes");
        if (symbol == 0)
        {
            return 0;
        }

        // The symbol addresses the CFStringRef variable, so dereference it.
        return Marshal.ReadIntPtr(symbol);
    }
}
