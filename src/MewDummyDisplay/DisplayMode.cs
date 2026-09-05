namespace Aprillz.MewDummyDisplay;

/// <summary>One selectable display mode.</summary>
/// <remarks>
/// A pure value: it holds no native handle, so it stays valid after the underlying
/// CoreGraphics mode list is released. <see cref="DisplayCatalog.TrySetMode"/> matches
/// it back against a fresh enumeration.
/// </remarks>
public sealed record DisplayMode
{
    /// <summary>Logical size, which is what the desktop is laid out in.</summary>
    public required int Width { get; init; }

    /// <inheritdoc cref="Width"/>
    public required int Height { get; init; }

    /// <summary>Physical size. Larger than the logical size on HiDPI modes.</summary>
    public required int PixelWidth { get; init; }

    /// <inheritdoc cref="PixelWidth"/>
    public required int PixelHeight { get; init; }

    public required double RefreshRate { get; init; }

    /// <summary>
    /// Mode identifier. Its meaning depends on <see cref="Source"/>: an IOKit mode id for
    /// the public path, a list index for the private one. Not stable across reboots.
    /// </summary>
    public required int ModeId { get; init; }

    /// <summary>Which enumeration produced this mode. Selection must use the same path.</summary>
    public required DisplayModeSource Source { get; init; }

    public bool IsHiDpi => PixelWidth > Width;

    public override string ToString()
        => $"{Width}x{Height}{(IsHiDpi ? " HiDPI" : "")} @{RefreshRate:0.##}Hz";
}

/// <summary>Which enumeration a mode came from.</summary>
public enum DisplayModeSource
{
    /// <summary>CGDisplayCopyAllDisplayModes. Empty for a dummy this process created.</summary>
    Public,

    /// <summary>Private CGS entry points. The only path that works for our own dummies.</summary>
    Private,
}
