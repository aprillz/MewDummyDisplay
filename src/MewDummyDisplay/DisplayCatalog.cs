using System.Runtime.InteropServices;
using Aprillz.MewDummyDisplay.Interop;

namespace Aprillz.MewDummyDisplay;

/// <summary>Enumerates attached displays and configures mirroring.</summary>
public static class DisplayCatalog
{
    /// <summary>Every online display.</summary>
    /// <summary>
    /// Starts counting display configuration changes. <see cref="ConfigurationVersion"/> moves
    /// on each one, so a caller that remembers the last value it saw knows when to look again.
    /// </summary>
    public static bool WatchConfiguration() => DisplayReconfigurationWatcher.Register();

    /// <summary>Increments on every display configuration change once watching has started.</summary>
    public static int ConfigurationVersion => DisplayReconfigurationWatcher.Version;

    public static IReadOnlyList<DisplayInfo> Online()
    {
        uint[] identifiers = new uint[CoreGraphicsInterop.MAX_DISPLAYS];
        if (CoreGraphicsInterop.CGGetOnlineDisplayList((uint)identifiers.Length, identifiers, out uint count) != 0)
        {
            return [];
        }

        List<DisplayInfo> displays = [];
        for (uint index = 0; index < count; index++)
        {
            displays.Add(Describe(identifiers[index]));
        }
        return displays;
    }

    /// <summary>Reads the current state of one display.</summary>
    public static DisplayInfo Describe(uint displayId)
    {
        int width = 0;
        int height = 0;
        int pixelWidth = 0;
        int pixelHeight = 0;
        double refreshRate = 0;

        nint mode = CoreGraphicsInterop.CGDisplayCopyDisplayMode(displayId);
        if (mode != 0)
        {
            width = (int)CoreGraphicsInterop.CGDisplayModeGetWidth(mode);
            height = (int)CoreGraphicsInterop.CGDisplayModeGetHeight(mode);
            pixelWidth = (int)CoreGraphicsInterop.CGDisplayModeGetPixelWidth(mode);
            pixelHeight = (int)CoreGraphicsInterop.CGDisplayModeGetPixelHeight(mode);
            refreshRate = CoreGraphicsInterop.CGDisplayModeGetRefreshRate(mode);
            CoreGraphicsInterop.CGDisplayModeRelease(mode);
        }
        else if (CurrentModeViaPrivateApi(displayId) is DisplayMode current)
        {
            // The public API reports nothing for a display this process created.
            width = current.Width;
            height = current.Height;
            pixelWidth = current.PixelWidth;
            pixelHeight = current.PixelHeight;
            refreshRate = current.RefreshRate;
        }

        return new DisplayInfo
        {
            DisplayId = displayId,
            Name = CoreDisplayInterop.DisplayName(displayId),
            VendorId = CoreGraphicsInterop.CGDisplayVendorNumber(displayId),
            ModelId = CoreGraphicsInterop.CGDisplayModelNumber(displayId),
            SerialNumber = CoreGraphicsInterop.CGDisplaySerialNumber(displayId),
            IsBuiltIn = CoreGraphicsInterop.CGDisplayIsBuiltin(displayId) != 0,
            IsMain = CoreGraphicsInterop.CGDisplayIsMain(displayId) != 0,
            IsActive = CoreGraphicsInterop.CGDisplayIsActive(displayId) != 0,
            IsOnline = CoreGraphicsInterop.CGDisplayIsOnline(displayId) != 0,
            IsAsleep = CoreGraphicsInterop.CGDisplayIsAsleep(displayId) != 0,
            IsInMirrorSet = CoreGraphicsInterop.CGDisplayIsInMirrorSet(displayId) != 0,
            Bounds = CoreGraphicsInterop.CGDisplayBounds(displayId) is var rect
                ? (rect.Origin.X, rect.Origin.Y, rect.Size.Width, rect.Size.Height)
                : default,
            Rotation = CoreGraphicsInterop.CGDisplayRotation(displayId),
            MirrorsDisplayId = CoreGraphicsInterop.CGDisplayMirrorsDisplay(displayId),
            Width = width,
            Height = height,
            PixelWidth = pixelWidth,
            PixelHeight = pixelHeight,
            RefreshRate = refreshRate,
        };
    }

    /// <summary>
    /// Every mode the display offers, largest first. Falls back to the private mode
    /// entry points, which are the only ones that work for a dummy this process created.
    /// </summary>
    public static IReadOnlyList<DisplayMode> Modes(uint displayId)
    {
        IReadOnlyList<DisplayMode> published = ModesViaPublicApi(displayId);
        return published.Count > 0 ? published : ModesViaPrivateApi(displayId);
    }

    /// <summary>The display's active mode read through the private entry points.</summary>
    public static DisplayMode? CurrentModeViaPrivateApi(uint displayId)
    {
        if (!CoreGraphicsServicesInterop.IsAvailable())
        {
            return null;
        }

        CoreGraphicsServicesInterop.CGSGetCurrentDisplayMode(displayId, out int index);
        if (index < 0)
        {
            return null;
        }

        CoreGraphicsServicesInterop.CGSGetDisplayModeDescriptionOfLength(
            displayId, index, out CGSDisplayMode raw, Marshal.SizeOf<CGSDisplayMode>());
        if (raw.Width == 0 || raw.Height == 0)
        {
            return null;
        }

        int scale = raw.Density >= 2 ? 2 : 1;
        return new DisplayMode
        {
            Width = (int)raw.Width,
            Height = (int)raw.Height,
            PixelWidth = (int)raw.Width * scale,
            PixelHeight = (int)raw.Height * scale,
            RefreshRate = raw.Frequency,
            ModeId = index,
            Source = DisplayModeSource.Private,
        };
    }

    /// <summary>Enumerates modes through the private entry points.</summary>
    public static IReadOnlyList<DisplayMode> ModesViaPrivateApi(uint displayId)
    {
        if (!CoreGraphicsServicesInterop.IsAvailable())
        {
            return [];
        }

        CoreGraphicsServicesInterop.CGSGetNumberOfDisplayModes(displayId, out int count);
        if (count <= 0)
        {
            return [];
        }

        int size = Marshal.SizeOf<CGSDisplayMode>();
        List<DisplayMode> modes = [];
        for (int index = 0; index < count; index++)
        {
            CoreGraphicsServicesInterop.CGSGetDisplayModeDescriptionOfLength(displayId, index, out CGSDisplayMode raw, size);
            if (raw.Width == 0 || raw.Height == 0)
            {
                continue;
            }

            int scale = raw.Density >= 2 ? 2 : 1;
            modes.Add(new DisplayMode
            {
                Width = (int)raw.Width,
                Height = (int)raw.Height,
                PixelWidth = (int)raw.Width * scale,
                PixelHeight = (int)raw.Height * scale,
                RefreshRate = raw.Frequency,
                ModeId = index,
                Source = DisplayModeSource.Private,
            });
        }

        return [.. modes
            .OrderByDescending(mode => (long)mode.PixelWidth * mode.PixelHeight)
            .ThenByDescending(mode => mode.RefreshRate)];
    }

    private static IReadOnlyList<DisplayMode> ModesViaPublicApi(uint displayId)
    {
        nint options = BuildModeListOptions();
        nint modeArray = CoreGraphicsInterop.CGDisplayCopyAllDisplayModes(displayId, options);
        if (options != 0)
        {
            ObjC.Release(options);
        }
        if (modeArray == 0)
        {
            return [];
        }

        try
        {
            List<DisplayMode> modes = [];
            nint count = CoreGraphicsInterop.CFArrayGetCount(modeArray);
            for (nint index = 0; index < count; index++)
            {
                nint mode = CoreGraphicsInterop.CFArrayGetValueAtIndex(modeArray, index);
                if (mode != 0)
                {
                    modes.Add(Describe(mode));
                }
            }

            return [.. modes
                .OrderByDescending(mode => (long)mode.PixelWidth * mode.PixelHeight)
                .ThenByDescending(mode => mode.Width)
                .ThenByDescending(mode => mode.RefreshRate)];
        }
        finally
        {
            CoreGraphicsInterop.CFRelease(modeArray);
        }
    }

    /// <summary>Switches the display to a mode, using whichever path produced it.</summary>
    public static bool TrySetMode(uint displayId, DisplayMode target)
        => target.Source == DisplayModeSource.Private
            ? SetModeViaPrivateApi(displayId, target)
            : SetModeViaPublicApi(displayId, target);

    private static bool SetModeViaPrivateApi(uint displayId, DisplayMode target)
    {
        if (CoreGraphicsInterop.CGBeginDisplayConfiguration(out nint configuration) != 0)
        {
            return false;
        }

        CoreGraphicsServicesInterop.CGSConfigureDisplayMode(configuration, displayId, target.ModeId);
        return CoreGraphicsInterop.CGCompleteDisplayConfiguration(
            configuration, CoreGraphicsInterop.CONFIGURE_FOR_SESSION) == 0;
    }

    private static bool SetModeViaPublicApi(uint displayId, DisplayMode target)
    {
        nint options = BuildModeListOptions();
        nint modeArray = CoreGraphicsInterop.CGDisplayCopyAllDisplayModes(displayId, options);
        if (options != 0)
        {
            ObjC.Release(options);
        }
        if (modeArray == 0)
        {
            return false;
        }

        try
        {
            nint count = CoreGraphicsInterop.CFArrayGetCount(modeArray);
            for (nint index = 0; index < count; index++)
            {
                nint mode = CoreGraphicsInterop.CFArrayGetValueAtIndex(modeArray, index);
                if (mode == 0 || !Matches(Describe(mode), target))
                {
                    continue;
                }

                if (CoreGraphicsInterop.CGBeginDisplayConfiguration(out nint configuration) != 0)
                {
                    return false;
                }

                if (CoreGraphicsInterop.CGConfigureDisplayWithDisplayMode(configuration, displayId, mode, 0) != 0)
                {
                    CoreGraphicsInterop.CGCancelDisplayConfiguration(configuration);
                    return false;
                }

                return CoreGraphicsInterop.CGCompleteDisplayConfiguration(
                    configuration, CoreGraphicsInterop.CONFIGURE_FOR_SESSION) == 0;
            }

            return false;
        }
        finally
        {
            CoreGraphicsInterop.CFRelease(modeArray);
        }
    }

    /// <summary>
    /// Makes <paramref name="displayId"/> show the contents of <paramref name="sourceDisplayId"/>.
    /// </summary>
    /// <remarks>
    /// Direction matters and is easy to get backwards. A dummy exists to supply a resolution
    /// the real monitor cannot offer on its own, so the useful arrangement is the real
    /// display mirroring the dummy, not the other way around.
    /// </remarks>
    public static bool SetMirror(uint displayId, uint sourceDisplayId)
        => Configure(displayId, sourceDisplayId);

    /// <summary>Displays currently showing the contents of <paramref name="sourceDisplayId"/>.</summary>
    public static IReadOnlyList<DisplayInfo> DisplaysMirroring(uint sourceDisplayId)
        => [.. Online().Where(display => display.MirrorsDisplayId == sourceDisplayId)];

    /// <summary>Turns mirroring off for one display.</summary>
    public static bool ClearMirror(uint displayId)
        => Configure(displayId, CoreGraphicsInterop.NULL_DISPLAY);

    private static DisplayMode Describe(nint mode) => new()
    {
        Width = (int)CoreGraphicsInterop.CGDisplayModeGetWidth(mode),
        Height = (int)CoreGraphicsInterop.CGDisplayModeGetHeight(mode),
        PixelWidth = (int)CoreGraphicsInterop.CGDisplayModeGetPixelWidth(mode),
        PixelHeight = (int)CoreGraphicsInterop.CGDisplayModeGetPixelHeight(mode),
        RefreshRate = CoreGraphicsInterop.CGDisplayModeGetRefreshRate(mode),
        ModeId = CoreGraphicsInterop.CGDisplayModeGetIODisplayModeID(mode),
        Source = DisplayModeSource.Public,
    };

    private static bool Matches(DisplayMode candidate, DisplayMode target)
        => candidate.Width == target.Width
        && candidate.Height == target.Height
        && candidate.PixelWidth == target.PixelWidth
        && candidate.PixelHeight == target.PixelHeight
        && Math.Abs(candidate.RefreshRate - target.RefreshRate) < 0.5;

    private static nint BuildModeListOptions()
    {
        nint key = CoreGraphicsInterop.ShowDuplicateLowResolutionModesKey();
        if (key == 0)
        {
            return 0;
        }

        nint value = ObjC.NewBoolean(true);
        try
        {
            return ObjC.NewDictionary(key, value);
        }
        finally
        {
            ObjC.Release(value);
        }
    }

    private static bool Configure(uint displayId, uint sourceDisplayId)
    {
        if (CoreGraphicsInterop.CGBeginDisplayConfiguration(out nint configuration) != 0)
        {
            return false;
        }

        if (CoreGraphicsInterop.CGConfigureDisplayMirrorOfDisplay(configuration, displayId, sourceDisplayId) != 0)
        {
            CoreGraphicsInterop.CGCancelDisplayConfiguration(configuration);
            return false;
        }

        return CoreGraphicsInterop.CGCompleteDisplayConfiguration(
            configuration, CoreGraphicsInterop.CONFIGURE_FOR_SESSION) == 0;
    }
}

/// <summary>Snapshot of one display.</summary>
public sealed record DisplayInfo
{
    public required uint DisplayId { get; init; }

    /// <summary>The product name the display reports, or null when it has none.</summary>
    public string? Name { get; init; }

    public required uint VendorId { get; init; }
    public required uint ModelId { get; init; }
    public required uint SerialNumber { get; init; }
    public required bool IsBuiltIn { get; init; }
    public required bool IsMain { get; init; }

    /// <summary>Whether the display is part of the desktop. An online display can still be inactive.</summary>
    public required bool IsActive { get; init; }

    public required bool IsOnline { get; init; }
    public required bool IsAsleep { get; init; }
    public required bool IsInMirrorSet { get; init; }

    /// <summary>Global desktop rectangle, in points.</summary>
    public required (double X, double Y, double Width, double Height) Bounds { get; init; }
    public required double Rotation { get; init; }
    public required uint MirrorsDisplayId { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int PixelWidth { get; init; }
    public required int PixelHeight { get; init; }
    public required double RefreshRate { get; init; }

    /// <summary>A pixel size larger than the point size means the mode is HiDPI.</summary>
    public bool IsHiDpi => PixelWidth > Width;

    public bool IsMirroring => MirrorsDisplayId != 0;
}
