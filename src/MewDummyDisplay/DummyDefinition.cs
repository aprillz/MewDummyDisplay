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

    /// <summary>
    /// A short list of round resolutions, for showing to a person.
    /// </summary>
    /// <remarks>
    /// <see cref="Resolutions"/> walks every multiplier, which for a 16:9 dummy is over two
    /// hundred sizes and mostly noise like 1296x729. This keeps multipliers divisible by the
    /// smallest stride that brings the count near <paramref name="targetCount"/>, which lands
    /// on the familiar sizes: 1280x720, 1920x1080, 2560x1440, 3840x2160. The largest
    /// resolution is always included, because it is the reason to pick a definition at all.
    /// </remarks>
    public IReadOnlyList<(int Width, int Height)> CommonResolutions(int targetCount = 12)
    {
        if (!IsUsable)
        {
            return [];
        }

        foreach (int stride in _strides)
        {
            List<int> multipliers = [];
            for (int multiplier = MinMultiplier; multiplier <= MaxMultiplier; multiplier++)
            {
                if (multiplier % stride == 0)
                {
                    multipliers.Add(multiplier);
                }
            }

            if (multipliers.Count >= 3 && multipliers.Count <= targetCount)
            {
                if (multipliers[^1] != MaxMultiplier)
                {
                    multipliers.Add(MaxMultiplier);
                }
                return [.. multipliers.Select(PixelsFor)];
            }
        }

        return [.. Resolutions()];
    }

    /// <summary>Strides tried in order, chosen so the kept multipliers stay round numbers.</summary>
    private static readonly int[] _strides = [1, 2, 4, 5, 8, 10, 16, 20, 25, 32, 40, 50, 64, 80, 100, 128, 160, 200];

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
