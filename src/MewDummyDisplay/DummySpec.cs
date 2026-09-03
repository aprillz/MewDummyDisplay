namespace Aprillz.MewDummyDisplay;

/// <summary>Request values for creating one dummy.</summary>
public sealed record DummySpec
{
    /// <summary>Refresh rate is fixed at 60Hz. Custom refresh rates are out of scope.</summary>
    public const double FIXED_REFRESH_RATE = 60.0;

    /// <summary>Vendor identifier, chosen so dummies from other tools are not mistaken for ours.</summary>
    public const uint VENDOR_ID = 0xF0F1;

    /// <summary>Prefix of the name macOS shows for the display. Not localized: it doubles as an identifier.</summary>
    public const string NAME_PREFIX = "MewDummy";

    public required DummyDefinition Definition { get; init; }

    /// <summary>A random value is assigned on creation when this is zero.</summary>
    public uint SerialNumber { get; init; }

    public bool HiDpi { get; init; } = true;

    /// <summary>Diagonal size in inches, used only to compute the reported physical size.</summary>
    public double DiagonalInches { get; init; } = 24.0;

    /// <summary>
    /// Overrides <see cref="FIXED_REFRESH_RATE"/> for probing only. Custom refresh rates are
    /// not a supported feature; this exists so the CLI can observe what macOS accepts.
    /// </summary>
    public double? RefreshRateOverride { get; init; }
}
