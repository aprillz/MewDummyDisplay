using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>
/// Private CoreGraphics Services mode APIs.
/// </summary>
/// <remarks>
/// The public CGDisplayCopyAllDisplayModes returns nothing for a virtual display when
/// asked by the process that created it, while any other process sees the modes fine.
/// These private entry points are the only way for the creating process to enumerate and
/// select modes on its own dummies, which is why BetterDisplay still imports them.
/// </remarks>
internal static class CoreGraphicsServicesInterop
{
    private const string COREGRAPHICS = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [DllImport(COREGRAPHICS)]
    internal static extern void CGSGetNumberOfDisplayModes(uint display, out int count);

    [DllImport(COREGRAPHICS)]
    internal static extern void CGSGetCurrentDisplayMode(uint display, out int modeIndex);

    [DllImport(COREGRAPHICS)]
    internal static extern void CGSGetDisplayModeDescriptionOfLength(uint display, int index, out CGSDisplayMode mode, int length);

    [DllImport(COREGRAPHICS)]
    internal static extern void CGSConfigureDisplayMode(nint configuration, uint display, int modeIndex);

    /// <summary>Reports whether the private mode entry points exist.</summary>
    internal static bool IsAvailable()
    {
        nint library = ObjC.dlopen(COREGRAPHICS, 2);
        return library != 0
            && ObjC.dlsym(library, "CGSGetNumberOfDisplayModes") != 0
            && ObjC.dlsym(library, "CGSGetDisplayModeDescriptionOfLength") != 0
            && ObjC.dlsym(library, "CGSConfigureDisplayMode") != 0;
    }
}

/// <summary>
/// Layout of the private CGSDisplayMode structure.
/// </summary>
/// <remarks>
/// The padding runs are genuinely unknown fields, so the layout is the fragile part of
/// this path. Verify it against a known display before trusting a new OS release:
/// a real display's reported sizes must match what the public API says.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct CGSDisplayMode
{
    internal uint ModeNumber;
    internal uint Flags;
    internal uint Width;
    internal uint Height;
    internal uint Depth;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 170)]
    internal byte[] Unknown;

    internal ushort Frequency;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal byte[] MoreUnknown;

    internal float Density;
}
