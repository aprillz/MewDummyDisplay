using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>Minimal CFRunLoop access, used to let queued notifications be delivered.</summary>
internal static class RunLoop
{
    private const string CORE_FOUNDATION = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(CORE_FOUNDATION)]
    private static extern int CFRunLoopRunInMode(nint mode, double seconds, byte returnAfterSourceHandled);

    private static readonly nint _defaultMode = LoadDefaultMode();

    /// <summary>Runs the current thread's run loop for one short slice.</summary>
    internal static void PumpOnce(double seconds = 0.05)
    {
        if (_defaultMode == 0)
        {
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            return;
        }
        CFRunLoopRunInMode(_defaultMode, seconds, 0);
    }

    private static nint LoadDefaultMode()
    {
        nint library = ObjC.dlopen(CORE_FOUNDATION, 2);
        if (library == 0)
        {
            return 0;
        }

        nint symbol = ObjC.dlsym(library, "kCFRunLoopDefaultMode");
        return symbol == 0 ? 0 : Marshal.ReadIntPtr(symbol);
    }
}
