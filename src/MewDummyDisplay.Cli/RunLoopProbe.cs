using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Cli;

// Phase 1 research: displays created after the first are active with correct bounds,
// yet CGDisplayCopyDisplayMode returns null for them. CoreGraphics caches display
// configuration per process and refreshes it from reconfiguration notifications, which
// are delivered through the run loop. A console tool has no run loop, so this pumps one
// briefly to test whether that is the missing piece.
internal static class RunLoopProbe
{
    private const string CORE_FOUNDATION = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string LIBSYSTEM = "/usr/lib/libSystem.B.dylib";

    [DllImport(CORE_FOUNDATION)]
    private static extern int CFRunLoopRunInMode(nint mode, double seconds, byte returnAfterSourceHandled);

    [DllImport(LIBSYSTEM)]
    private static extern nint dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int mode);

    [DllImport(LIBSYSTEM)]
    private static extern nint dlsym(nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string symbol);

    /// <summary>Pumps the current thread's run loop for the given duration.</summary>
    internal static void Pump(TimeSpan duration)
    {
        nint mode = DefaultMode();
        if (mode == 0)
        {
            Thread.Sleep(duration);
            return;
        }

        DateTime deadline = DateTime.UtcNow + duration;
        while (DateTime.UtcNow < deadline)
        {
            CFRunLoopRunInMode(mode, 0.1, 0);
        }
    }

    private static nint DefaultMode()
    {
        nint library = dlopen(CORE_FOUNDATION, 2);
        if (library == 0)
        {
            return 0;
        }

        nint symbol = dlsym(library, "kCFRunLoopDefaultMode");
        return symbol == 0 ? 0 : Marshal.ReadIntPtr(symbol);
    }
}
