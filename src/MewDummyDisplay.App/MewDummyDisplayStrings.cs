using Aprillz.MewUI;

namespace Aprillz.MewDummyDisplay.App;

/// <summary>
/// Centralized UI strings for localization, following the MewUIStrings pattern.
/// Defaults are English. Assign <see cref="ObservableValue{T}.Value"/> at runtime to
/// change one, and call <see cref="ResetToDefaults"/> to restore every default.
/// </summary>
/// <remarks>
/// Lives in the application rather than the library: the library carries stable
/// identifiers and structural facts, never display text, so that it keeps no dependency
/// on a UI framework.
/// </remarks>
public static class MewDummyDisplayStrings
{
    private static readonly List<Action> _resetters = [];

    private static ObservableValue<string> Define(string defaultValue)
    {
        ObservableValue<string> value = new(defaultValue);
        _resetters.Add(() => value.Value = defaultValue);
        return value;
    }

    /// <summary>Restores every string to its built-in English default.</summary>
    public static void ResetToDefaults()
    {
        foreach (Action reset in _resetters)
        {
            reset();
        }
    }

    // Menu structure
    public static ObservableValue<string> MenuNoDummies { get; } = Define("No dummy displays yet");
    public static ObservableValue<string> MenuHint { get; } = Define("Select a display to turn it on or off");
    public static ObservableValue<string> MenuAllDisplays { get; } = Define("All displays");
    public static ObservableValue<string> MenuAllOnCount { get; } = Define("{0} of {1} on");
    public static ObservableValue<string> MenuQuit { get; } = Define("Quit MewDummyDisplay");
    public static ObservableValue<string> MenuUnsupported { get; } = Define("Virtual displays are unavailable on this system");

    // Window
    public static ObservableValue<string> MenuManage { get; } = Define("Manage dummy displays...");
    public static ObservableValue<string> WindowTitle { get; } = Define("MewDummyDisplay");
    public static ObservableValue<string> WindowCreateHeading { get; } = Define("Create a dummy display");
    public static ObservableValue<string> WindowAspectRatio { get; } = Define("Aspect ratio");
    public static ObservableValue<string> WindowHiDpi { get; } = Define("HiDPI (Retina)");
    public static ObservableValue<string> WindowCreate { get; } = Define("Create");
    public static ObservableValue<string> WindowExisting { get; } = Define("Dummy displays");
    public static ObservableValue<string> WindowNone { get; } = Define("None yet. Create one above.");
    public static ObservableValue<string> WindowResolution { get; } = Define("Resolution");
    public static ObservableValue<string> WindowApply { get; } = Define("Apply");
    public static ObservableValue<string> WindowMirrorLabel { get; } = Define("Mirror to");
    public static ObservableValue<string> WindowMirrorHint { get; } = Define(
        "The monitor shows this dummy's picture and runs at the dummy's resolution.");
    public static ObservableValue<string> WindowMirrorNone { get; } = Define("None");
    public static ObservableValue<string> WindowRemove { get; } = Define("Remove");
    public static ObservableValue<string> WindowRemoveConfirm { get; } = Define("Remove {0}?");
    public static ObservableValue<string> WindowClose { get; } = Define("Close");

    // Navigation
    public static ObservableValue<string> PageDummies { get; } = Define("Dummy displays");
    public static ObservableValue<string> PageSystem { get; } = Define("System displays");
    public static ObservableValue<string> PageAbout { get; } = Define("About");

    // Create form
    public static ObservableValue<string> WindowName { get; } = Define("Name");
    public static ObservableValue<string> WindowNamePlaceholder { get; } = Define("Leave empty to name it automatically");
    public static ObservableValue<string> WindowRename { get; } = Define("Rename");
    public static ObservableValue<string> WindowRenameHint { get; } = Define("Renaming recreates the display, because macOS fixes the name when it is created.");

    // System displays page
    public static ObservableValue<string> SystemBuiltIn { get; } = Define("Built in");
    public static ObservableValue<string> SystemMain { get; } = Define("Main");
    public static ObservableValue<string> SystemVirtual { get; } = Define("Virtual");
    public static ObservableValue<string> SystemModes { get; } = Define("modes");
    public static ObservableValue<string> SystemRefresh { get; } = Define("Refresh");

    // About page
    public static ObservableValue<string> AboutSummary { get; } = Define(
        "Creates virtual displays on macOS so any monitor can use a HiDPI resolution.");
    public static ObservableValue<string> AboutLicense { get; } = Define("MIT licensed. Copyright (c) 2026 Aprillz.");
    public static ObservableValue<string> AboutReference { get; } = Define(
        "Written with reference to BetterDummy 1.0.11, also MIT. See the NOTICE file in the bundle.");
    public static ObservableValue<string> AboutTheme { get; } = Define("Appearance");
    public static ObservableValue<string> AboutThemeSystem { get; } = Define("System");
    public static ObservableValue<string> AboutThemeLight { get; } = Define("Light");
    public static ObservableValue<string> AboutThemeDark { get; } = Define("Dark");

    // Dummy rows
    public static ObservableValue<string> DummyMirroring { get; } = Define("mirrored to {0}");
    public static ObservableValue<string> DummyOff { get; } = Define("off");
    public static ObservableValue<string> WindowConnect { get; } = Define("Turn on");
    public static ObservableValue<string> WindowDisconnect { get; } = Define("Turn off");
    public static ObservableValue<string> DummyRemove { get; } = Define("Remove");

    // Aspect ratio kinds
    public static ObservableValue<string> KindWide { get; } = Define("Wide");
    public static ObservableValue<string> KindStandard { get; } = Define("Standard");
    public static ObservableValue<string> KindCinema { get; } = Define("Cinema");
    public static ObservableValue<string> KindUltraWide { get; } = Define("Ultrawide");
    public static ObservableValue<string> KindDoubleWide { get; } = Define("Double wide");
    public static ObservableValue<string> KindSquare { get; } = Define("Square");
    public static ObservableValue<string> KindPortrait { get; } = Define("Portrait");
    public static ObservableValue<string> KindPhoto { get; } = Define("Photography");
    public static ObservableValue<string> KindTablet { get; } = Define("Tablet");

    /// <summary>Localized label for a definition category.</summary>
    public static string Kind(DummyDefinitionKind kind) => kind switch
    {
        DummyDefinitionKind.Wide => KindWide.Value,
        DummyDefinitionKind.Standard => KindStandard.Value,
        DummyDefinitionKind.Cinema => KindCinema.Value,
        DummyDefinitionKind.UltraWide => KindUltraWide.Value,
        DummyDefinitionKind.DoubleWide => KindDoubleWide.Value,
        DummyDefinitionKind.Square => KindSquare.Value,
        DummyDefinitionKind.Portrait => KindPortrait.Value,
        DummyDefinitionKind.Photo => KindPhoto.Value,
        DummyDefinitionKind.Tablet => KindTablet.Value,
        _ => kind.ToString(),
    };

    /// <summary>Label for one definition in the add list, for example "16:9 (Wide)".</summary>
    public static string Definition(DummyDefinition definition)
        => $"{definition.Id} ({Kind(definition.Kind)})";
}
