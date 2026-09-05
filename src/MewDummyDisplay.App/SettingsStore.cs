using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aprillz.MewDummyDisplay.App;

/// <summary>Reads and writes the settings file.</summary>
/// <remarks>
/// Load bearing rather than a convenience: the serials it remembers are what keep one
/// dummy as one display to macOS. See <see cref="DummyRecord.SerialNumber"/>.
/// </remarks>
internal static class SettingsStore
{
    private static readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MewDummyDisplay");

    private static readonly string _path = Path.Combine(_directory, "settings.json");

    internal static string Path_ => _path;

    /// <summary>Loads the settings, returning defaults when there is nothing to read.</summary>
    internal static DummySettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new DummySettings();
            }

            DummySettings? settings = JsonSerializer.Deserialize(
                File.ReadAllText(_path), SettingsJsonContext.Default.DummySettings);
            return settings ?? new DummySettings();
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not read {_path}: {error.Message}");
            return new DummySettings();
        }
    }

    /// <summary>Writes the settings, replacing the file atomically.</summary>
    internal static void Save(DummySettings settings)
    {
        try
        {
            Directory.CreateDirectory(_directory);

            // Written beside the target and moved into place, so an interrupted write
            // cannot leave a half-file that loses every remembered serial.
            string temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.DummySettings));
            File.Move(temporary, _path, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Could not write {_path}: {error.Message}");
        }
    }
}

// Source generated so serialization survives trimming and NativeAOT.
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DummySettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
