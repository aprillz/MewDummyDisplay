using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Tests;

[TestClass]
public sealed class DummyDefinitionCatalogTests
{
    [TestMethod]
    public void EveryDefinition_ProducesAtLeastOneResolution()
    {
        foreach (DummyDefinition definition in DummyDefinitionCatalog.All())
        {
            Assert.IsTrue(definition.IsUsable, $"{definition.Id} has an empty multiplier range");
            Assert.IsNotEmpty(definition.Resolutions());
        }
    }

    [TestMethod]
    public void Identifiers_AreUnique()
    {
        List<string> ids = [.. DummyDefinitionCatalog.All().Select(definition => definition.Id)];

        CollectionAssert.AreEquivalent(ids, ids.Distinct().ToList());
    }

    [TestMethod]
    public void Find_IgnoresCase()
    {
        Assert.IsNotNull(DummyDefinitionCatalog.Find("16:9"));
        Assert.IsNotNull(DummyDefinitionCatalog.Find("21.3:9"));
        Assert.IsNull(DummyDefinitionCatalog.Find("nope"));
    }

    [TestMethod]
    public void PortraitDefinitions_AreTallerThanWide()
    {
        List<DummyDefinition> portrait =
            [.. DummyDefinitionCatalog.All().Where(definition => definition.Kind == DummyDefinitionKind.Portrait)];

        Assert.IsNotEmpty(portrait);
        foreach (DummyDefinition definition in portrait)
        {
            Assert.IsGreaterThan(definition.AspectWidth, definition.AspectHeight, definition.Id);
        }
    }

    [TestMethod]
    public void SixteenByNine_OffersCommonResolutions()
    {
        DummyDefinition definition = DummyDefinitionCatalog.Find("16:9")!;
        List<(int Width, int Height)> resolutions = [.. definition.Resolutions()];

        Assert.Contains((1920, 1080), resolutions);
        Assert.Contains((2560, 1440), resolutions);
        Assert.Contains((3840, 2160), resolutions);
    }
}
