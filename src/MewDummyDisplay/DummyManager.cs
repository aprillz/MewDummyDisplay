using Aprillz.MewDummyDisplay.Interop;

namespace Aprillz.MewDummyDisplay;

/// <summary>Owns the creation and lifetime of dummies. Main entry point of the library.</summary>
/// <remarks>
/// Disposing releases every live dummy. This is the last line of defense against
/// leaving orphaned displays behind when the process exits.
/// </remarks>
public sealed class DummyManager : IDisposable
{
    /// <summary>
    /// Smallest gap enforced between two display operations.
    /// </summary>
    /// <remarks>
    /// Creating and releasing displays back to back makes CoreGraphics report stale or
    /// empty values and delays teardown well past twenty seconds. The cause sits in the
    /// window server rather than in this code, so the manager paces itself instead of
    /// trying to fix it.
    /// </remarks>
    public static readonly TimeSpan MinimumOperationGap = TimeSpan.FromMilliseconds(750);

    /// <summary>How long <see cref="Create"/> waits for a new display to register.</summary>
    public static readonly TimeSpan DefaultReadyTimeout = TimeSpan.FromSeconds(5);

    private readonly Lock _gate = new();
    private readonly List<Dummy> _dummies = [];
    private DateTime _lastOperationUtc = DateTime.MinValue;
    private bool _disposed;

    /// <summary>Time <see cref="Create"/> waits for registration. Zero returns immediately.</summary>
    public TimeSpan ReadyTimeout { get; init; } = DefaultReadyTimeout;

    /// <summary>Classes and selectors the surface check could not find. Empty means dummies can be created.</summary>
    public static IReadOnlyList<string> MissingSurface { get; } = SurfaceCheck.FindMissing();

    public static bool IsSupported => MissingSurface.Count == 0;

    public IReadOnlyList<Dummy> Dummies => _dummies;

    /// <summary>
    /// Whether to subscribe to display reconfiguration notifications before creating.
    /// Under investigation: the subscription may be what stops this process from
    /// reading back the modes of displays it created.
    /// </summary>
    public static bool WatchReconfiguration { get; set; } = true;

    /// <summary>Whether waiting for readiness pumps the run loop instead of sleeping.</summary>
    public static bool PumpRunLoopWhileWaiting { get; set; } = true;

    /// <summary>
    /// Defines a dummy and connects it. Returns null when the system refuses the display.
    /// Waits up to <see cref="ReadyTimeout"/> for registration; check
    /// <see cref="Dummy.IsReady"/> to tell whether it finished.
    /// </summary>
    public Dummy? Create(DummySpec spec)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsSupported)
        {
            throw new PlatformNotSupportedException(
                $"Private virtual display API surface is missing: {string.Join(", ", MissingSurface)}");
        }

        if (!spec.Definition.IsUsable)
        {
            return null;
        }

        uint serial = spec.SerialNumber != 0 ? spec.SerialNumber : NewSerialNumber();
        DummySpec resolved = spec with { SerialNumber = serial };
        string name = resolved.Name is { Length: > 0 } custom ? custom : BuildName(resolved.Definition, serial);

        Dummy dummy = new(resolved, name, OpenDisplay);
        if (!Connect(dummy))
        {
            dummy.Dispose();
            return null;
        }

        _dummies.Add(dummy);
        return dummy;
    }

    /// <summary>Connects a dummy that is currently disconnected.</summary>
    public bool Connect(Dummy dummy)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (dummy.IsConnected)
        {
            return true;
        }

        if (WatchReconfiguration)
        {
            DisplayReconfigurationWatcher.Register();
        }
        WaitForTurn();

        if (!dummy.Connect())
        {
            return false;
        }

        if (ReadyTimeout > TimeSpan.Zero)
        {
            dummy.WaitUntilReady(ReadyTimeout);
        }
        return true;
    }

    /// <summary>Disconnects a dummy, keeping its definition.</summary>
    public void Disconnect(Dummy dummy)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!dummy.IsConnected)
        {
            return;
        }

        WaitForTurn();
        dummy.Disconnect();
    }

    /// <summary>Connects a disconnected dummy or disconnects a connected one.</summary>
    public bool Toggle(Dummy dummy)
    {
        if (dummy.IsConnected)
        {
            Disconnect(dummy);
            return false;
        }
        return Connect(dummy);
    }

    /// <summary>Builds the virtual display for a dummy. Passed to each dummy as its opener.</summary>
    private VirtualDisplay? OpenDisplay(Dummy dummy)
    {
        DummyDefinition definition = dummy.Spec.Definition;
        (int maxWidth, int maxHeight) = definition.PixelsFor(definition.MaxMultiplier);
        double refreshRate = dummy.Spec.RefreshRateOverride ?? DummySpec.FIXED_REFRESH_RATE;

        // Only the curated sizes go to the display. Offering every multiplier floods the
        // System Settings resolution list with hundreds of near-identical entries.
        List<VirtualDisplayMode> modes = [];
        foreach ((int width, int height) in definition.CommonResolutions(dummy.Spec.ResolutionCount))
        {
            modes.Add(new VirtualDisplayMode((uint)width, (uint)height, refreshRate));
        }

        return VirtualDisplayFactory.Create(new VirtualDisplayRequest(
            Name: dummy.Name,
            SerialNumber: dummy.SerialNumber,
            VendorId: DummySpec.VENDOR_ID,
            ProductId: BuildProductId(definition),
            PhysicalSize: PhysicalSize(definition, dummy.Spec.DiagonalInches),
            MaxPixelsWide: (uint)maxWidth,
            MaxPixelsHigh: (uint)maxHeight,
            Modes: modes,
            HiDpi: dummy.Spec.HiDpi));
    }

    /// <summary>
    /// Renames a dummy by recreating it. Returns the replacement, or null if it failed.
    /// </summary>
    /// <remarks>
    /// The name lives in the descriptor and is fixed once the display exists, so the only
    /// way to change it is to build a new display. The serial is carried over so the
    /// replacement is recognisable as the same dummy.
    /// </remarks>
    public Dummy? Rename(Dummy dummy, string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DummySpec spec = dummy.Spec with { Name = name };
        bool wasConnected = dummy.IsConnected;
        if (!Remove(dummy))
        {
            return null;
        }

        Dummy? replacement = Create(spec);
        if (replacement is not null && !wasConnected)
        {
            Disconnect(replacement);
        }
        return replacement;
    }

    /// <summary>Releases one dummy.</summary>
    public bool Remove(Dummy dummy)
    {
        if (!_dummies.Remove(dummy))
        {
            return false;
        }

        WaitForTurn();
        dummy.Dispose();
        return true;
    }

    /// <summary>Sleeps until enough time has passed since the previous display operation.</summary>
    private void WaitForTurn()
    {
        lock (_gate)
        {
            TimeSpan elapsed = DateTime.UtcNow - _lastOperationUtc;
            if (elapsed < MinimumOperationGap)
            {
                Thread.Sleep(MinimumOperationGap - elapsed);
            }
            _lastOperationUtc = DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        foreach (Dummy dummy in _dummies)
        {
            dummy.Dispose();
        }
        _dummies.Clear();
    }

    private static string BuildName(DummyDefinition definition, uint serial)
        => $"{DummySpec.NAME_PREFIX} {definition.Id} #{serial:X8}";

    private static uint NewSerialNumber()
    {
        uint serial = 0;
        while (serial == 0)
        {
            serial = (uint)Random.Shared.NextInt64(1, uint.MaxValue);
        }
        return serial;
    }

    private static uint BuildProductId(DummyDefinition definition)
    {
        uint width = (uint)Math.Min(definition.AspectWidth - 1, 255);
        uint height = (uint)Math.Min(definition.AspectHeight - 1, 255);
        return (width * 256) + height;
    }

    private static CGSize PhysicalSize(DummyDefinition definition, double diagonalInches)
    {
        double diagonalMillimeters = diagonalInches * 25.4;
        double aspectDiagonal = Math.Sqrt(
            ((double)definition.AspectWidth * definition.AspectWidth) +
            ((double)definition.AspectHeight * definition.AspectHeight));
        double ratio = diagonalMillimeters / aspectDiagonal;
        return new CGSize(definition.AspectWidth * ratio, definition.AspectHeight * ratio);
    }
}
