namespace Aprillz.MewDummyDisplay;

/// <summary>Everything worth remembering between runs.</summary>
/// <remarks>
/// The library defines the shape and does not store it. Where it lives and how it is
/// serialized belongs to the consumer, so the CLI and the application can differ.
/// </remarks>
public sealed record DummySettings
{
    public const int CURRENT_SCHEMA_VERSION = 1;

    public int SchemaVersion { get; init; } = CURRENT_SCHEMA_VERSION;

    public List<DummyRecord> Dummies { get; init; } = [];

    public GeneralSettings General { get; init; } = new();
}

/// <summary>One remembered dummy.</summary>
/// <remarks>
/// <see cref="SerialNumber"/> is the reason this file matters beyond convenience. macOS
/// identifies a display by its serial, generates a ColorSync profile per identity, and
/// keeps that profile forever. A dummy that draws a fresh random serial every run
/// therefore looks like a brand new monitor each time and leaves a profile behind.
/// Remembering the serial keeps one dummy as one display across restarts.
/// </remarks>
public sealed record DummyRecord
{
    public required string DefinitionId { get; init; }

    public required uint SerialNumber { get; init; }

    public string? Name { get; init; }

    public bool HiDpi { get; init; } = true;

    /// <summary>Whether it was turned on when the settings were written.</summary>
    public bool Connected { get; init; } = true;

    public int ResolutionCount { get; init; } = 8;
}

/// <summary>Settings that are not tied to one dummy.</summary>
public sealed record GeneralSettings
{
    public bool Enable16K { get; init; }
}
