namespace Aprillz.MewDummyDisplay;

/// <summary>Built-in aspect ratio definitions.</summary>
public static class DummyDefinitionCatalog
{
    private static readonly (string Id, DummyDefinitionKind Kind, int Width, int Height, int Step)[] _entries =
    [
        ("16:9",    DummyDefinitionKind.Wide,       16,  9,   2),
        ("16:10",   DummyDefinitionKind.Wide,       16,  10,  2),
        ("4:3",     DummyDefinitionKind.Standard,   16,  12,  2),
        ("17:9",    DummyDefinitionKind.Cinema,     256, 135, 2),
        ("21.3:9",  DummyDefinitionKind.UltraWide,  64,  27,  2),
        ("21.5:9",  DummyDefinitionKind.UltraWide,  43,  18,  2),
        ("24:10",   DummyDefinitionKind.UltraWide,  24,  10,  1),
        ("32:10",   DummyDefinitionKind.DoubleWide, 32,  10,  1),
        ("32:9",    DummyDefinitionKind.DoubleWide, 32,  9,   2),
        ("1:1",     DummyDefinitionKind.Square,     20,  20,  2),
        ("9:16",    DummyDefinitionKind.Portrait,   9,   16,  2),
        ("10:16",   DummyDefinitionKind.Portrait,   10,  16,  2),
        ("3:4",     DummyDefinitionKind.Portrait,   12,  16,  2),
        ("9:17",    DummyDefinitionKind.Portrait,   135, 256, 2),
        ("3:2",     DummyDefinitionKind.Photo,      15,  10,  2),
        ("5:4",     DummyDefinitionKind.Photo,      15,  12,  2),
        ("15.2:10", DummyDefinitionKind.Tablet,     152, 100, 1),
        ("23:16",   DummyDefinitionKind.Tablet,     66,  41,  2),
        ("14.3:10", DummyDefinitionKind.Tablet,     199, 139, 2),
    ];

    /// <summary>Builds every definition.</summary>
    public static IReadOnlyList<DummyDefinition> All(bool enable16K = false)
        => [.. _entries.Select(entry => new DummyDefinition(entry.Id, entry.Kind, entry.Width, entry.Height, entry.Step, enable16K))];

    /// <summary>Finds a definition by identifier, or returns null.</summary>
    public static DummyDefinition? Find(string id, bool enable16K = false)
        => All(enable16K).FirstOrDefault(definition => string.Equals(definition.Id, id, StringComparison.OrdinalIgnoreCase));
}
