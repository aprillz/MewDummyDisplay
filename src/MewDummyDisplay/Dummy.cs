using Aprillz.MewDummyDisplay.Interop;

namespace Aprillz.MewDummyDisplay;

/// <summary>A live dummy. Disposing it removes the display from the system.</summary>
public sealed class Dummy : IDisposable
{
    private readonly VirtualDisplay _display;
    private bool _disposed;

    internal Dummy(DummySpec spec, VirtualDisplay display, string name)
    {
        Spec = spec;
        Name = name;
        _display = display;
    }

    public DummySpec Spec { get; }

    /// <summary>Name macOS shows for this display.</summary>
    public string Name { get; }

    public uint SerialNumber => Spec.SerialNumber;

    /// <summary>Display identifier assigned by macOS.</summary>
    public uint DisplayId => _display.DisplayId;

    /// <summary>
    /// Whether CoreGraphics has finished registering the display. Registration is
    /// asynchronous: right after creation the display reports no mode at all.
    /// </summary>
    public bool IsReady => DisplayCatalog.Describe(DisplayId).Width > 0;

    /// <summary>Blocks until the display registers, or the timeout expires.</summary>
    /// <remarks>
    /// Pumps the run loop rather than sleeping. CoreGraphics only refreshes this
    /// process's copy of the display configuration when the reconfiguration
    /// notification is delivered, and delivery needs a running run loop.
    /// </remarks>
    public bool WaitUntilReady(TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (IsReady)
            {
                return true;
            }
            if (DummyManager.PumpRunLoopWhileWaiting)
            {
                Interop.RunLoop.PumpOnce();
            }
            else
            {
                Thread.Sleep(100);
            }
        }
        return IsReady;
    }

    /// <summary>Modes this dummy offers. Empty until the display finishes registering.</summary>
    public IReadOnlyList<DisplayMode> Modes() => DisplayCatalog.Modes(DisplayId);

    /// <summary>Switches this dummy to a specific mode.</summary>
    public bool TrySetMode(DisplayMode mode) => DisplayCatalog.TrySetMode(DisplayId, mode);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _display.Dispose();
    }
}
