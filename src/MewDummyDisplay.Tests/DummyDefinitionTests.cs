using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Tests;

// Pure arithmetic, so these run anywhere. Anything that touches CoreGraphics is
// verified with the mdd CLI on a real machine instead.
[TestClass]
public sealed class DummyDefinitionTests
{
    [TestMethod]
    public void MultiplierRange_StaysWithinPixelBounds()
    {
        DummyDefinition definition = new("16:9", DummyDefinitionKind.Wide, 16, 9, 2);

        (int minWidth, int minHeight) = definition.PixelsFor(definition.MinMultiplier);
        (int maxWidth, int maxHeight) = definition.PixelsFor(definition.MaxMultiplier);

        Assert.IsGreaterThanOrEqualTo(DummyDefinition.MIN_PIXELS, minWidth);
        Assert.IsGreaterThanOrEqualTo(DummyDefinition.MIN_PIXELS, minHeight);
        Assert.IsLessThanOrEqualTo(DummyDefinition.MAX_PIXELS, maxWidth);
        Assert.IsLessThanOrEqualTo(DummyDefinition.MAX_PIXELS, maxHeight);
    }

    [TestMethod]
    public void MultiplierRange_ExcludesTheStepJustOutsideTheBounds()
    {
        DummyDefinition definition = new("16:9", DummyDefinitionKind.Wide, 16, 9, 2);

        (int belowWidth, int belowHeight) = definition.PixelsFor(definition.MinMultiplier - 1);
        (int aboveWidth, int aboveHeight) = definition.PixelsFor(definition.MaxMultiplier + 1);

        Assert.IsTrue(
            belowWidth < DummyDefinition.MIN_PIXELS || belowHeight < DummyDefinition.MIN_PIXELS,
            $"one step below the minimum still fits: {belowWidth}x{belowHeight}");
        Assert.IsTrue(
            aboveWidth > DummyDefinition.MAX_PIXELS || aboveHeight > DummyDefinition.MAX_PIXELS,
            $"one step above the maximum still fits: {aboveWidth}x{aboveHeight}");
    }

    [TestMethod]
    public void Enable16K_WidensTheRange()
    {
        DummyDefinition standard = new("16:9", DummyDefinitionKind.Wide, 16, 9, 2);
        DummyDefinition wide = new("16:9", DummyDefinitionKind.Wide, 16, 9, 2, enable16K: true);

        Assert.IsGreaterThan(standard.MaxMultiplier, wide.MaxMultiplier);
        Assert.AreEqual(standard.MinMultiplier, wide.MinMultiplier);
    }

    [TestMethod]
    public void PixelsFor_MultipliesAspectByStep()
    {
        DummyDefinition definition = new("16:9", DummyDefinitionKind.Wide, 16, 9, 2);

        Assert.AreEqual((16 * 2 * 40, 9 * 2 * 40), definition.PixelsFor(40));
    }

    [TestMethod]
    public void Resolutions_CoversEveryMultiplierInAscendingOrder()
    {
        DummyDefinition definition = new("16:9", DummyDefinitionKind.Wide, 16, 9, 2);

        List<(int Width, int Height)> resolutions = [.. definition.Resolutions()];

        Assert.HasCount(definition.MaxMultiplier - definition.MinMultiplier + 1, resolutions);
        Assert.AreEqual(definition.PixelsFor(definition.MinMultiplier), resolutions[0]);
        Assert.AreEqual(definition.PixelsFor(definition.MaxMultiplier), resolutions[^1]);

        for (int index = 1; index < resolutions.Count; index++)
        {
            Assert.IsGreaterThan(resolutions[index - 1].Width, resolutions[index].Width);
        }
    }

    [TestMethod]
    public void Resolutions_KeepTheAspectRatio()
    {
        DummyDefinition definition = new("21.3:9", DummyDefinitionKind.UltraWide, 64, 27, 2);
        double expected = 64.0 / 27.0;

        foreach ((int width, int height) in definition.Resolutions())
        {
            Assert.AreEqual(expected, (double)width / height, 0.0001, $"{width}x{height}");
        }
    }

    [TestMethod]
    public void ExtremeAspectRatio_IsReportedUnusableRatherThanThrowing()
    {
        // Nothing fits between 720 and 8192 at this ratio, so the range inverts.
        DummyDefinition definition = new("absurd", DummyDefinitionKind.Wide, 1, 4000, 1);

        Assert.IsFalse(definition.IsUsable);
    }

    [TestMethod]
    [DataRow(0, 9, 2)]
    [DataRow(16, 0, 2)]
    [DataRow(16, 9, 0)]
    public void InvalidDimensions_Throw(int width, int height, int step)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new DummyDefinition("bad", DummyDefinitionKind.Wide, width, height, step));
    }

    [TestMethod]
    public void EmptyId_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => new DummyDefinition("", DummyDefinitionKind.Wide, 16, 9, 2));
    }
}
