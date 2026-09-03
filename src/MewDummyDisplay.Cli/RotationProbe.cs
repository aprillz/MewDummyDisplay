using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Cli;

// Unresolved research, kept out of the library on purpose.
//
// BetterDisplay rotates displays through SkyLight's SLSSetDisplayRotation rather than
// CGVirtualDisplaySettings.setRotation:, which macOS accepts and then ignores. The
// SkyLight function exists and the WindowServer connection id is valid, but every call
// returns 1001 (kCGErrorIllegalArgument) for every rotation value including zero, with
// both a float and an int third argument. So the rejected argument is not the rotation
// value: either the signature has a different shape, or the display must be identified
// some other way.
//
// This stays in the CLI until it actually works. Promoting it into the library would put
// an unproven experiment on the public API surface.
internal static class RotationProbe
{
    private const string SKYLIGHT = "/System/Library/PrivateFrameworks/SkyLight.framework/SkyLight";

    [DllImport(SKYLIGHT)]
    internal static extern int SLSMainConnectionID();

    [DllImport(SKYLIGHT, EntryPoint = "SLSSetDisplayRotation")]
    private static extern int SetRotationFloat(int connection, uint display, float degrees);

    [DllImport(SKYLIGHT, EntryPoint = "SLSSetDisplayRotation")]
    private static extern int SetRotationInt(int connection, uint display, int degrees);

    [DllImport("/usr/lib/libSystem.B.dylib")]
    private static extern nint dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int mode);

    [DllImport("/usr/lib/libSystem.B.dylib")]
    private static extern nint dlsym(nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string symbol);

    internal static bool IsAvailable()
    {
        nint handle = dlopen(SKYLIGHT, 2);
        return handle != 0 && dlsym(handle, "SLSSetDisplayRotation") != 0;
    }

    /// <summary>Attempts a rotation and reports the raw status code.</summary>
    internal static void Run(uint displayId, double degrees, bool useFloat)
    {
        int connection = SLSMainConnectionID();
        Console.WriteLine($"  available : {IsAvailable()}");
        Console.WriteLine($"  connection: {connection}");

        int status = useFloat
            ? SetRotationFloat(connection, displayId, (float)degrees)
            : SetRotationInt(connection, displayId, (int)degrees);

        Console.WriteLine($"  variant   : {(useFloat ? "float" : "int")} degrees={degrees}");
        Console.WriteLine($"  status    : {status} ({Describe(status)})");
    }

    private static string Describe(int status) => status switch
    {
        0 => "success",
        1000 => "kCGErrorFailure",
        1001 => "kCGErrorIllegalArgument",
        1002 => "kCGErrorInvalidConnection",
        1003 => "kCGErrorInvalidContext",
        _ => "unknown",
    };
}
