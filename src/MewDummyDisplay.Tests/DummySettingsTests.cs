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

    [TestMethod]
    public void GateReadsAsOpenWhenTheFileNeverStatedIt()
    {
        // The value is nullable so that "not written" is distinct from "off". A bool
        // defaulting to true would not survive the source generated serializer, which
        // skips property initializers and hands back false for a missing key.
        GeneralSettings never = new();
        GeneralSettings closed = new() { Enabled = false };

        Assert.IsNull(never.Enabled);
        Assert.IsTrue(never.IsGateOpen);
        Assert.IsFalse(closed.IsGateOpen);
    }

    [TestMethod]
    public void GateSurvivesARoundTrip()
    {
        DummySettings settings = new()
        {
            General = new GeneralSettings { Enabled = false, Enable16K = true },
            Dummies =
            [
                new DummyRecord { DefinitionId = "16:9", SerialNumber = 1, Connected = true },
                new DummyRecord { DefinitionId = "16:10", SerialNumber = 2, Connected = false },
            ],
        };

        DummySettings restored = JsonSerializer.Deserialize<DummySettings>(
            JsonSerializer.Serialize(settings))!;

        // The gate is remembered apart from each dummy's own state, so closing it does not
        // erase which dummies were on.
        Assert.IsFalse(restored.General.Enabled);
        Assert.IsTrue(restored.Dummies[0].Connected);
        Assert.IsFalse(restored.Dummies[1].Connected);
    }

    [TestMethod]
    public void GateDefaultsToOpenWhenTheFileDoesNotMentionIt()
    {
        // Settings written before the gate existed must still turn their dummies on.
        DummySettings restored = JsonSerializer.Deserialize<DummySettings>(
            """{"schemaVersion":1,"dummies":[],"general":{"enable16K":false}}""")!;

        Assert.IsNull(restored.General.Enabled);
        Assert.IsTrue(restored.General.IsGateOpen);
    }
}
