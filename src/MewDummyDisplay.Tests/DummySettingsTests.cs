using System.Text.Json;
using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Tests;

// The serial is the point of these. macOS identifies a display by it and keeps a
// ColorSync profile per identity forever, so a dummy that draws a fresh serial each run
// registers as a new monitor every time and leaves a profile behind.
[TestClass]
public sealed class DummySettingsTests
{
    [TestMethod]
    public void Record_SurvivesAJsonRoundTrip()
    {
        DummySettings original = new()
        {
            Dummies =
            [
                new DummyRecord
                {
                    DefinitionId = "16:9",
                    SerialNumber = 0xA1B2C3D4,
                    Name = "Desk",
                    HiDpi = true,
                    Connected = false,
                    ResolutionCount = 8,
                },
            ],
            General = new GeneralSettings { Enable16K = true },
        };

        string json = JsonSerializer.Serialize(original);
        DummySettings restored = JsonSerializer.Deserialize<DummySettings>(json)!;

        Assert.HasCount(1, restored.Dummies);
        Assert.AreEqual(0xA1B2C3D4u, restored.Dummies[0].SerialNumber);
        Assert.AreEqual("16:9", restored.Dummies[0].DefinitionId);
        Assert.AreEqual("Desk", restored.Dummies[0].Name);
        Assert.IsFalse(restored.Dummies[0].Connected);
        Assert.IsTrue(restored.General.Enable16K);
    }

    [TestMethod]
    public void SchemaVersion_IsWritten()
    {
        string json = JsonSerializer.Serialize(new DummySettings());

        Assert.Contains("SchemaVersion", json);
    }

    [TestMethod]
    public void UnknownDefinition_IsSkippedRatherThanThrowing()
    {
        DummySettings settings = new()
        {
            Dummies = [new DummyRecord { DefinitionId = "no such ratio", SerialNumber = 1 }],
        };

        // Restoring runs against the catalog only until it needs a display, so an entry
        // naming a ratio that no longer exists must be dropped quietly.
        Assert.IsNull(DummyDefinitionCatalog.Find(settings.Dummies[0].DefinitionId));
    }
}
