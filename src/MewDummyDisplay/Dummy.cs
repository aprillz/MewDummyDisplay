using Aprillz.MewDummyDisplay.Interop;

namespace Aprillz.MewDummyDisplay;

/// <summary>
/// One dummy display. It stays defined while the manager holds it, and separately it is
/// either connected, meaning a virtual display exists, or disconnected, meaning it does not.
/// </summary>
/// <remarks>
/// The two states are separate on purpose. Disconnecting removes the display from the
/// desktop without forgetting how it was configured, which is what a person wants when a
/// dummy is temporarily in the way. Removing it is the destructive one.
/// </remarks>
public sealed class Dummy : IDisposable
{
    private readonly Func<Dummy, VirtualDisplay?> _connect;
    private VirtualDisplay? _display;
    private bool _disposed;

    internal Dummy(DummySpec spec, string name, Func<Dummy, VirtualDisplay?> connect)
    {
        Spec = spec;
        Name = name;
        _connect = connect;
    }

    public DummySpec Spec { get; }

    /// <summary>Name macOS shows for this display.</summary>
    public string Name { get; }

    public uint SerialNumber => Spec.SerialNumber;

    /// <summary>
    /// Whether this dummy is meant to be on. Its own state, kept apart from whether a
    /// display exists for it: the manager's gate can be off while this stays true, and
    /// turning the gate back on is what brings the display back.
    /// </summary>
    public bool IsEnabled { get; internal set; } = true;

    /// <summary>Whether a virtual display currently exists for this dummy.</summary>
    public bool IsConnected => _display is not null;

    /// <summary>Display identifier assigned by macOS. Zero while disconnected.</summary>
    public uint DisplayId => _display?.DisplayId ?? 0;

    /// <summary>
    /// Whether CoreGraphics has finished registering the display. Registration is
    /// asynchronous: right after creation the display has no desktop rectangle yet.
    /// </summary>
    /// <remarks>
    /// Keyed off the bounds rather than the display mode. The public mode API reports
    /// nothing for a display this process created, so it would never signal ready.
    /// </remarks>
    public bool IsReady => IsConnected && DisplayCatalog.Describe(DisplayId).Bounds.Width > 0;

    /// <summary>Blocks until the display registers, or the timeout expires.</summary>
    /// <remarks>
    /// Pumps the run loop where <see cref="DummyManager.PumpRunLoopWhileWaiting"/> allows
    /// it, so queued notifications are delivered instead of slept through.
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
                RunLoop.PumpOnce();
            }
            else
            {
                Thread.Sleep(100);
            }
        }
        return IsReady;
    }

    /// <summary>Modes this dummy offers. Empty while disconnected or still registering.</summary>
    public IReadOnlyList<DisplayMode> Modes()
        => IsConnected ? DisplayCatalog.Modes(DisplayId) : [];

    /// <summary>Switches this dummy to a specific mode.</summary>
    public bool TrySetMode(DisplayMode mode)
        => IsConnected && DisplayCatalog.TrySetMode(DisplayId, mode);

    /// <summary>Creates the virtual display. Does nothing when already connected.</summary>
    internal bool Connect()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsConnected)
        {
            return true;
        }

        _display = _connect(this);
        return IsConnected;
    }

    /// <summary>Removes the virtual display, keeping the definition.</summary>
    internal void Disconnect()
    {
        _display?.Dispose();
        _display = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Disconnect();
    }
}
