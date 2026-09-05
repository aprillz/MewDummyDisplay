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
    private GeneralSettings _general = new();
    private bool _disposed;

    /// <summary>Time <see cref="Create"/> waits for registration. Zero returns immediately.</summary>
    public TimeSpan ReadyTimeout { get; init; } = DefaultReadyTimeout;

    /// <summary>Classes and selectors the surface check could not find. Empty means dummies can be created.</summary>
    public static IReadOnlyList<string> MissingSurface { get; } = SurfaceCheck.FindMissing();

    public static bool IsSupported => MissingSurface.Count == 0;

    public IReadOnlyList<Dummy> Dummies => _dummies;

    /// <summary>
    /// The master gate over every dummy.
    /// </summary>
    /// <remarks>
    /// It gates rather than overwrites. Closing it disconnects every dummy and leaves each
    /// one's <see cref="Dummy.IsEnabled"/> alone, so opening it again returns them to the
    /// arrangement they were in rather than turning everything on.
    /// </remarks>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Opens or closes the gate, bringing every dummy in line with it.</summary>
    public void SetEnabled(bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsEnabled == enabled)
        {
            return;
        }

        IsEnabled = enabled;
        foreach (Dummy dummy in _dummies)
        {
            Apply(dummy);
        }
    }

    /// <summary>
    /// Whether to subscribe to display reconfiguration notifications before creating.
    /// The subscription is what refreshes this process's snapshot of the display
    /// configuration, so its own displays report their bounds.
    /// </summary>
    public static bool WatchReconfiguration { get; set; } = true;

    /// <summary>Whether waiting for readiness pumps the run loop instead of sleeping.</summary>
    public static bool PumpRunLoopWhileWaiting { get; set; } = true;

    /// <summary>
    /// Defines a dummy and connects it. Returns null when the system refuses the display.
    /// Waits up to <see cref="ReadyTimeout"/> for registration; check
    /// <see cref="Dummy.IsReady"/> to tell whether it finished.
    /// </summary>
    /// <param name="enabled">
    /// The dummy's own state. False defines it without connecting, which is also what
    /// happens to an enabled one while the gate is closed.
    /// </param>
    public Dummy? Create(DummySpec spec, bool enabled = true)
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

        Dummy dummy = new(resolved, name, OpenDisplay) { IsEnabled = enabled };
        if (IsEnabled && enabled && !ConnectCore(dummy))
        {
            dummy.Dispose();
            return null;
        }

        _dummies.Add(dummy);
        return dummy;
    }

    /// <summary>Writes the current dummies into a settings record.</summary>
    public DummySettings CaptureSettings(GeneralSettings? general = null) => new()
    {
        // The gate is the manager's to report; the rest is whatever it was restored with,
        // so settings the manager does not act on are not dropped by a save.
        General = (general ?? _general) with { Enabled = IsEnabled },
        Dummies =
        [
            .. _dummies.Select(dummy => new DummyRecord
            {
                DefinitionId = dummy.Spec.Definition.Id,
                SerialNumber = dummy.SerialNumber,
                Name = dummy.Spec.Name,
                HiDpi = dummy.Spec.HiDpi,
                Connected = dummy.IsEnabled,
                ResolutionCount = dummy.Spec.ResolutionCount,
            }),
        ],
    };

    /// <summary>
    /// Recreates the dummies described by a settings record, reusing their serials so
    /// macOS sees the same displays it saw last time.
    /// </summary>
    public void RestoreSettings(DummySettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _general = settings.General;
        IsEnabled = settings.General.IsGateOpen;

        foreach (DummyRecord record in settings.Dummies)
        {
            DummyDefinition? definition = DummyDefinitionCatalog.Find(record.DefinitionId, settings.General.Enable16K);
            if (definition is null)
            {
                continue;
            }

            Create(
                new DummySpec
                {
                    Definition = definition,
                    SerialNumber = record.SerialNumber,
                    Name = record.Name,
                    HiDpi = record.HiDpi,
                    ResolutionCount = record.ResolutionCount,
                },
                enabled: record.Connected);
        }
    }

    /// <summary>Turns a dummy on. It connects unless the gate is closed.</summary>
    public bool Connect(Dummy dummy)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        dummy.IsEnabled = true;
        Apply(dummy);
        return dummy.IsConnected;
    }

    /// <summary>Turns a dummy off, keeping its definition.</summary>
    public void Disconnect(Dummy dummy)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        dummy.IsEnabled = false;
        Apply(dummy);
    }

    /// <summary>Flips a dummy's own state. Returns the state it is now in.</summary>
    public bool Toggle(Dummy dummy)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        dummy.IsEnabled = !dummy.IsEnabled;
        Apply(dummy);
        return dummy.IsEnabled;
    }

    /// <summary>Brings one dummy's display in line with the gate and its own state.</summary>
    private void Apply(Dummy dummy)
    {
        bool wanted = IsEnabled && dummy.IsEnabled;
        if (wanted == dummy.IsConnected)
        {
            return;
        }

        if (wanted)
        {
            ConnectCore(dummy);
        }
        else
        {
            DisconnectCore(dummy);
        }
    }

    private bool ConnectCore(Dummy dummy)
    {
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

    private void DisconnectCore(Dummy dummy)
    {
        WaitForTurn();
        dummy.Disconnect();
    }

    /// <summary>Builds the virtual display for a dummy. Passed to each dummy as its opener.</summary>
    private VirtualDisplay? OpenDisplay(Dummy dummy)
    {
        DummyDefinition definition = dummy.Spec.Definition;
        (int maxWidth, int maxHeight) = definition.PixelsFor(definition.MaxMultiplier);
        double refreshRate = dummy.Spec.RefreshRateOverride ?? DummySpec.FIXED_REFRESH_RATE;

        // Only the curated sizes go to the display, not every multiplier.
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
        // Its own state carries over, not whether a display happened to exist: a dummy that
        // is on while the gate is closed must still be on after the rename.
        bool wasEnabled = dummy.IsEnabled;
        if (!Remove(dummy))
        {
            return null;
        }

        return Create(spec, enabled: wasEnabled);
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
