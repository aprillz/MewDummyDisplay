namespace Aprillz.MewDummyDisplay;

/// <summary>Aspect ratio and resolution multiplier range for one dummy.</summary>
/// <remarks>
/// Carries no display text. <see cref="Id"/> is a stable key and <see cref="Kind"/> a
/// structural category; consumers map both to localized labels.
/// </remarks>
public sealed class DummyDefinition
{
    /// <summary>Smallest width or height a virtual display may have.</summary>
    public const int MIN_PIXELS = 720;

    /// <summary>Default pixel ceiling, raised to <see cref="MAX_PIXELS_16K"/> when 16K is enabled.</summary>
    public const int MAX_PIXELS = 8192;

    public const int MAX_PIXELS_16K = 16384;

    /// <summary>Stable identifier used in settings and on the command line.</summary>
    public string Id { get; }

    /// <summary>Structural category. Consumers use it to group and label definitions.</summary>
    public DummyDefinitionKind Kind { get; }

    public int AspectWidth { get; }

    public int AspectHeight { get; }

    /// <summary>Unit the multiplier is scaled by, chosen to avoid odd pixel counts.</summary>
    public int MultiplierStep { get; }

    public int MinMultiplier { get; }

    public int MaxMultiplier { get; }

    public DummyDefinition(string id, DummyDefinitionKind kind, int aspectWidth, int aspectHeight, int multiplierStep, bool enable16K = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(aspectWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(aspectHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(multiplierStep);

        Id = id;
        Kind = kind;
        AspectWidth = aspectWidth;
        AspectHeight = aspectHeight;
        MultiplierStep = multiplierStep;

        int maxPixels = enable16K ? MAX_PIXELS_16K : MAX_PIXELS;
        double stepWidth = (double)aspectWidth * multiplierStep;
        double stepHeight = (double)aspectHeight * multiplierStep;

        MinMultiplier = Math.Max(
            (int)Math.Ceiling(MIN_PIXELS / stepWidth),
            (int)Math.Ceiling(MIN_PIXELS / stepHeight));
        MaxMultiplier = Math.Min(
            (int)Math.Floor(maxPixels / stepWidth),
            (int)Math.Floor(maxPixels / stepHeight));
    }

    /// <summary>Reports whether any resolution fits between the pixel bounds.</summary>
    public bool IsUsable => MaxMultiplier >= MinMultiplier;

    /// <summary>Pixel size for one multiplier.</summary>
    public (int Width, int Height) PixelsFor(int multiplier)
        => (AspectWidth * MultiplierStep * multiplier, AspectHeight * MultiplierStep * multiplier);

    /// <summary>Every resolution this definition can produce, in ascending order.</summary>
    public IEnumerable<(int Width, int Height)> Resolutions()
    {
        for (int multiplier = MinMultiplier; multiplier <= MaxMultiplier; multiplier++)
        {
            yield return PixelsFor(multiplier);
        }
    }
}

/// <summary>Structural category of a definition, used for grouping and labeling.</summary>
public enum DummyDefinitionKind
{
    Wide,
    Standard,
    Cinema,
    UltraWide,
    DoubleWide,
    Square,
    Portrait,
    Photo,
    Tablet,
}
