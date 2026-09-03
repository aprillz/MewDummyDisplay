using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace Aprillz.MewDummyDisplay.App;

/// <summary>Path icons drawn from SVG path data on a 24 by 24 grid.</summary>
/// <remarks>
/// Vector rather than bitmap so the icons stay sharp on any scale factor, which matters
/// in an application whose whole subject is display scaling. Fill comes from the theme
/// palette so they follow light and dark.
/// </remarks>
internal static class Icons
{
    private const string DISPLAY = "M3 4h18v11H3z M3 4h18v11H3V4zm0 0v11h18V4H3zm6 15h6m-3-4v4";
    private const string DISPLAY_FILLED = "M2 4a1 1 0 011-1h18a1 1 0 011 1v11a1 1 0 01-1 1h-7v3h3a1 1 0 110 2H8a1 1 0 110-2h3v-3H3a1 1 0 01-1-1V4z";
    private const string PLUS = "M12 5a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H6a1 1 0 110-2h5V6a1 1 0 011-1z";
    private const string MIRROR = "M4 3h7a1 1 0 011 1v16a1 1 0 01-1 1H4a1 1 0 01-1-1V4a1 1 0 011-1zm9 0h7a1 1 0 011 1v16a1 1 0 01-1 1h-7V3z";
    private const string TRASH = "M9 3h6a1 1 0 011 1v1h4a1 1 0 110 2h-1l-1 13a2 2 0 01-2 2H9a2 2 0 01-2-2L6 7H5a1 1 0 010-2h4V4a1 1 0 011-1zm1 2h4V4h-4v1z";
    private const string INFO = "M12 2a10 10 0 100 20 10 10 0 000-20zm0 4a1.25 1.25 0 110 2.5A1.25 1.25 0 0112 6zm1 5v6a1 1 0 11-2 0v-6a1 1 0 112 0z";
    private const string SLIDERS = "M4 6h9a1 1 0 110 2H4a1 1 0 010-2zm13 0h3a1 1 0 110 2h-3a1 1 0 110-2zM4 16h3a1 1 0 110 2H4a1 1 0 110-2zm7 0h9a1 1 0 110 2h-9a1 1 0 110-2zM15 4a1 1 0 011 1v4a1 1 0 11-2 0V5a1 1 0 011-1zM9 14a1 1 0 011 1v4a1 1 0 11-2 0v-4a1 1 0 011-1z";

    internal static Element Display(double size = 18) => Accent(DISPLAY_FILLED, size);

    internal static Element DisplayOutline(double size = 18) => Muted(DISPLAY, size);

    internal static Element Add(double size = 16) => Accent(PLUS, size);

    internal static Element Mirror(double size = 16) => Muted(MIRROR, size);

    internal static Element Remove(double size = 16) => Muted(TRASH, size);

    internal static Element Info(double size = 18) => Muted(INFO, size);

    internal static Element Sliders(double size = 18) => Muted(SLIDERS, size);

    private static Element Accent(string data, double size)
        => Shape(data, size).WithTheme((theme, shape) => shape.Fill(theme.Palette.Accent));

    private static Element Muted(string data, double size)
        => Shape(data, size).WithTheme((theme, shape) => shape.Fill(theme.Palette.WindowText));

    private static PathShape Shape(string data, double size)
        => new PathShape()
            .Size(size)
            .Data(data)
            .Stretch(Stretch.Uniform)
            .CenterVertical();
}
