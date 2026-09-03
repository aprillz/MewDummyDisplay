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
    /// A short list of familiar resolutions, for offering to a person.
    /// </summary>
    /// <remarks>
    /// <see cref="Resolutions"/> walks every multiplier, which for a 16:9 dummy is over two
    /// hundred sizes in sixteen pixel steps. macOS lists all of them in System Settings, so
    /// the useful ones get buried.
    ///
    /// Sizes are picked by matching a ladder of standard widths against what the ratio can
    /// actually produce, in order of how well known each one is, so a short list still keeps
    /// 1920x1080 and 2560x1440. Ratios that hit none of them fall back to an even stride.
    /// The largest is always included, because it is the reason to choose a ratio at all.
    /// </remarks>
    public IReadOnlyList<(int Width, int Height)> CommonResolutions(int targetCount = 8)
    {
        if (!IsUsable)
        {
            return [];
        }

        List<int> multipliers = MatchStandardSizes(targetCount);
        if (multipliers.Count < 3)
        {
            multipliers = MatchEvenStride(targetCount);
        }

        if (multipliers.Count == 0)
        {
            return [.. Resolutions()];
        }

        multipliers.Sort();
        if (multipliers[^1] != MaxMultiplier)
        {
            multipliers.Add(MaxMultiplier);
        }
        return [.. multipliers.Select(PixelsFor)];
    }

    /// <summary>
    /// Standard display widths, most recognizable first. The long edge is matched against
    /// these so portrait ratios land on the same familiar sizes as landscape ones.
    /// </summary>
    private static readonly int[] _standardLongEdges =
        [1920, 2560, 3840, 1280, 5120, 3200, 1600, 7680, 2048, 4096, 1440, 6400, 2880, 1680, 2240, 3440];

    /// <summary>Strides tried in order when no standard size fits the ratio.</summary>
    private static readonly int[] _strides = [1, 2, 4, 5, 8, 10, 16, 20, 25, 32, 40, 50, 64, 80, 100, 128, 160, 200];

    private List<int> MatchStandardSizes(int targetCount)
    {
        int longEdgeStep = Math.Max(AspectWidth, AspectHeight) * MultiplierStep;
        List<int> multipliers = [];

        foreach (int longEdge in _standardLongEdges)
        {
            if (multipliers.Count >= targetCount)
            {
                break;
            }

            if (longEdge % longEdgeStep != 0)
            {
                continue;
            }

            int multiplier = longEdge / longEdgeStep;
            if (multiplier >= MinMultiplier && multiplier <= MaxMultiplier && !multipliers.Contains(multiplier))
            {
                multipliers.Add(multiplier);
            }
        }

        return multipliers;
    }

    private List<int> MatchEvenStride(int targetCount)
    {
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
                return multipliers;
            }
        }

        return [];
    }

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
