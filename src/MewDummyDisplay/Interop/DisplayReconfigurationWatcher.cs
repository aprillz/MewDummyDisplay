using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>
/// Subscribes to display reconfiguration notifications.
/// </summary>
/// <remarks>
/// A process that creates a virtual display cannot read that display's mode:
/// CGDisplayCopyDisplayMode returns null while another process sees the mode fine.
/// CoreGraphics keeps a per process snapshot of the display configuration and refreshes
/// it when the reconfiguration notification is delivered, which only happens once the
/// process has registered a callback and runs a run loop.
/// </remarks>
internal static class DisplayReconfigurationWatcher
{
    private const string COREGRAPHICS = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [DllImport(COREGRAPHICS)]
    private static extern int CGDisplayRegisterReconfigurationCallback(nint callback, nint userInfo);

    [DllImport(COREGRAPHICS)]
    private static extern int CGDisplayRemoveReconfigurationCallback(nint callback, nint userInfo);

    private static readonly Lock _gate = new();
    private static bool _registered;
    private static int _version;

    /// <summary>Increments every time the system reports a display configuration change.</summary>
    internal static int Version => Volatile.Read(ref _version);

    /// <summary>Registers the callback once per process.</summary>
    internal static bool Register()
    {
        lock (_gate)
        {
            if (_registered)
            {
                return true;
            }

            unsafe
            {
                delegate* unmanaged<uint, uint, nint, void> callback = &OnReconfigured;
                _registered = CGDisplayRegisterReconfigurationCallback((nint)callback, 0) == 0;
            }
            return _registered;
        }
    }

    [UnmanagedCallersOnly]
    private static void OnReconfigured(uint display, uint flags, nint userInfo)
        => Interlocked.Increment(ref _version);
}
