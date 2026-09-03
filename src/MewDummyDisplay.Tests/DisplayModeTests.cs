using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Tests;

[TestClass]
public sealed class DisplayModeTests
{
    private static DisplayMode Mode(int width, int height, int pixelWidth, int pixelHeight)
        => new()
        {
            Width = width,
            Height = height,
            PixelWidth = pixelWidth,
            PixelHeight = pixelHeight,
            RefreshRate = 60,
            ModeId = 1,
            Source = DisplayModeSource.Public,
        };

    [TestMethod]
    public void IsHiDpi_WhenPixelsExceedPoints()
    {
        Assert.IsTrue(Mode(2560, 1440, 5120, 2880).IsHiDpi);
        Assert.IsFalse(Mode(2560, 1440, 2560, 1440).IsHiDpi);
    }

    [TestMethod]
    public void ToString_NamesTheHiDpiVariant()
    {
        Assert.Contains("HiDPI", Mode(2560, 1440, 5120, 2880).ToString());
        Assert.DoesNotContain("HiDPI", Mode(2560, 1440, 2560, 1440).ToString());
    }
}
