using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace Aprillz.MewDummyDisplay.App;

/// <summary>The window where dummies are created, named, resized and removed.</summary>
/// <remarks>
/// Everything denser than a toggle lives here rather than in the menu bar. Resolution is
/// the clearest example: a dummy offers hundreds of modes, which a flat menu cannot show.
///
/// Dummy rows are rebuilt explicitly rather than bound to an ItemsSource. Rows carry their
/// own editors, and swapping ItemsSource under an active editor replaces its container and
/// loses focus. Below roughly a hundred rows the explicit rebuild is the simpler and safer
/// choice; past that, move to ItemsSource with a typed ItemTemplate so containers recycle.
/// </remarks>
internal sealed class ManageWindow(DummyManager manager, Action onChanged)
{
    private readonly IReadOnlyList<DummyDefinition> _definitions = DummyDefinitionCatalog.All();
    private Window? _window;
    private StackPanel? _dummyList;
    private StackPanel? _systemList;
    private ComboBox? _definitionPicker;
    private CheckBox? _hiDpiToggle;
    private TextBox? _nameBox;

    /// <summary>Shows the window, bringing an already open one forward.</summary>
    internal void Show()
    {
        // An accessory application is not brought forward by opening a window, so the app
        // is activated first. Without it the window opens behind whatever had focus, which
        // read as the first click doing nothing.
        Interop.AppKitInterop.ActivateApplication();

        if (_window is not null)
        {
            _window.Show();
            _window.Activate();
            return;
        }

        _window = new Window()
            .Title(MewDummyDisplayStrings.WindowTitle.Value)
            .Resizable(760, 560)
            .Padding(0)
            .Content(BuildShell());

        _window.Closed += () =>
        {
            _window = null;
            _dummyList = null;
            _systemList = null;
        };

        _window.Show();
        _window.Activate();
        Refresh();
    }

    /// <summary>Whether the window is currently open.</summary>
    internal bool IsOpen => _window is not null;

    /// <summary>Rows currently shown on the dummy page.</summary>
    internal int RowCount => _dummyList?.Count ?? 0;

    /// <summary>Closes the window if it is open.</summary>
    internal void CloseIfOpen() => _window?.Close();

    /// <summary>Rebuilds both lists if the window is open.</summary>
    internal void Refresh()
    {
        RefreshDummies();
        RefreshSystemDisplays();
    }

    private NavigationView BuildShell()
    {
        NavigationPage[] pages =
        [
            new(MewDummyDisplayStrings.PageDummies.Value, Icons.Display(), BuildDummyPage),
            new(MewDummyDisplayStrings.PageSystem.Value, Icons.Sliders(), BuildSystemPage),
            new(MewDummyDisplayStrings.PageAbout.Value, Icons.Info(), BuildAboutPage),
        ];

        NavigationView navigation = new()
        {
            PaneWidth = 190,
            PaneDisplayMode = PaneDisplayMode.Inline,
        };

        navigation.Items(
            pages,
            page => page.Title,
            icon: page => page.Icon,
            content: page => new Border().Padding(24).Child(page.Build()));
        navigation.SelectedIndex = 0;

        return navigation;
    }

    // Pages

    private UIElement BuildDummyPage()
        => new DockPanel()
            .Spacing(16)
            .Children(
                Heading(MewDummyDisplayStrings.WindowCreateHeading.Value).DockTop(),
                BuildCreateForm().DockTop(),
                Heading(MewDummyDisplayStrings.WindowExisting.Value).DockTop(),
                new ScrollViewer()
                    .Content(new StackPanel().Ref(out StackPanel list).Spacing(12)))
            .Also(() => _dummyList = list);

    private UIElement BuildSystemPage()
        => new DockPanel()
            .Spacing(16)
            .Children(
                Heading(MewDummyDisplayStrings.PageSystem.Value).DockTop(),
                new StackPanel()
                    .Horizontal()
                    .DockTop()
                    .Children(new Button()
                        .Content(MewDummyDisplayStrings.SystemRefresh.Value)
                        .OnClick(RefreshSystemDisplays)),
                new ScrollViewer()
                    .Content(new StackPanel().Ref(out StackPanel list).Spacing(12)))
            .Also(() => _systemList = list);

    private UIElement BuildAboutPage()
        => new StackPanel()
            .Spacing(14)
            .Children(
                new StackPanel()
                    .Horizontal()
                    .Spacing(12)
                    .Children(
                        Icons.Display(32),
                        new StackPanel()
                            .Spacing(2)
                            .Children(
                                new TextBlock().Text(MewDummyDisplayStrings.WindowTitle.Value).FontSize(20).Bold(),
                                Muted(MewDummyDisplayStrings.AboutSummary.Value))),
                new Separator(),
                Muted(MewDummyDisplayStrings.AboutLicense.Value),
                Muted(MewDummyDisplayStrings.AboutReference.Value),
                new Separator(),
                new TextBlock().Text(MewDummyDisplayStrings.AboutTheme.Value).Bold(),
                new StackPanel()
                    .Horizontal()
                    .Spacing(8)
                    .Children(
                        new Button().Content(MewDummyDisplayStrings.AboutThemeSystem.Value)
                            .OnClick(() => Application.Current.SetThemeMode(ThemeVariant.System)),
                        new Button().Content(MewDummyDisplayStrings.AboutThemeLight.Value)
                            .OnClick(() => Application.Current.SetThemeMode(ThemeVariant.Light)),
                        new Button().Content(MewDummyDisplayStrings.AboutThemeDark.Value)
                            .OnClick(() => Application.Current.SetThemeMode(ThemeVariant.Dark))));

    // Create form

    private UIElement BuildCreateForm()
        => Card(new Grid()
            .Columns("110,*,Auto")
            .Rows("Auto,Auto,Auto")
            .Spacing(10)
            .Children(
                new TextBlock().Text(MewDummyDisplayStrings.WindowAspectRatio.Value).CenterVertical(),
                new ComboBox()
                    .Ref(out ComboBox picker)
                    .Column(1)
                    .Items([.. _definitions.Select(MewDummyDisplayStrings.Definition)])
                    .SelectedIndex(0),
                new CheckBox()
                    .Ref(out CheckBox hiDpi)
                    .Column(2)
                    .Content(MewDummyDisplayStrings.WindowHiDpi.Value)
                    .IsChecked(true)
                    .CenterVertical(),
                new TextBlock().Text(MewDummyDisplayStrings.WindowName.Value).Row(1).CenterVertical(),
                new TextBox()
                    .Ref(out TextBox nameBox)
                    .Row(1)
                    .Column(1)
                    .Placeholder(MewDummyDisplayStrings.WindowNamePlaceholder.Value),
                new Button()
                    .Row(1)
                    .Column(2)
                    .Content(MewDummyDisplayStrings.WindowCreate.Value)
                    .OnClick(CreateDummy)))
            .Also(() =>
            {
                _definitionPicker = picker;
                _hiDpiToggle = hiDpi;
                _nameBox = nameBox;
            });

    private void CreateDummy()
    {
        if (_definitionPicker is null || _hiDpiToggle is null)
        {
            return;
        }

        manager.Create(new DummySpec
        {
            Definition = _definitions[Math.Max(0, _definitionPicker.SelectedIndex)],
            HiDpi = _hiDpiToggle.IsChecked == true,
            Name = string.IsNullOrWhiteSpace(_nameBox?.Text) ? null : _nameBox.Text.Trim(),
        });

        if (_nameBox is not null)
        {
            _nameBox.Text = "";
        }

        Refresh();
        onChanged();
    }

    // Dummy list

    private void RefreshDummies()
    {
        if (_dummyList is null)
        {
            return;
        }

        _dummyList.Clear();

        if (manager.Dummies.Count == 0)
        {
            _dummyList.Add(Muted(MewDummyDisplayStrings.WindowNone.Value));
            return;
        }

        foreach (Dummy dummy in manager.Dummies)
        {
            _dummyList.Add(BuildDummyCard(dummy));
        }
    }

    private Border BuildDummyCard(Dummy dummy)
    {
        if (!dummy.IsConnected)
        {
            return BuildDisconnectedCard(dummy);
        }

        DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);

        // The display is already created with a curated set of sizes, so this only drops
        // the low resolution twin of each one: supplying a Retina resolution is why a dummy
        // exists, and showing both variants of every size doubles the list for nothing.
        List<DisplayMode> modes = [.. dummy.Modes()
            .Where(mode => mode.IsHiDpi)
            .DistinctBy(mode => (mode.Width, mode.Height))
            .OrderBy(mode => (long)mode.Width * mode.Height)];
        if (modes.Count == 0)
        {
            modes = [.. dummy.Modes()
                .DistinctBy(mode => (mode.Width, mode.Height))
                .OrderBy(mode => (long)mode.Width * mode.Height)];
        }

        int current = modes.FindIndex(mode => mode.Width == info.Width && mode.Height == info.Height);

        return Card(new StackPanel()
            .Spacing(10)
            .Children(
                PowerHeader(
                    dummy,
                    Icons.Display(20),
                    $"{dummy.Spec.Definition.Id}  {info.Width}x{info.Height}" +
                        (info.IsHiDpi ? $"  HiDPI {info.PixelWidth}x{info.PixelHeight}" : "") +
                        MirrorSuffix(dummy)),
                new Grid()
                    .Columns("Auto,*,Auto")
                    .Rows("Auto,Auto")
                    .Spacing(8)
                    .Children(
                        new TextBlock().Text(MewDummyDisplayStrings.WindowResolution.Value).CenterVertical(),
                        new ComboBox()
                            .Ref(out ComboBox resolution)
                            .Column(1)
                            .Items([.. modes.Select(mode => $"{mode.Width} x {mode.Height}")])
                            .SelectedIndex(Math.Max(0, current)),
                        new Button()
                            .Column(2)
                            .Content(MewDummyDisplayStrings.WindowApply.Value)
                            .OnClick(() => ApplyResolution(dummy, modes, resolution.SelectedIndex)),
                        new TextBlock().Text(MewDummyDisplayStrings.WindowName.Value).Row(1).CenterVertical(),
                        new TextBox().Ref(out TextBox rename).Row(1).Column(1).Text(dummy.Name),
                        new Button()
                            .Row(1)
                            .Column(2)
                            .Content(MewDummyDisplayStrings.WindowRename.Value)
                            .OnClick(() => RenameDummy(dummy, rename.Text))),
                BuildMirrorRow(dummy),
                new StackPanel()
                    .Horizontal()
                    .Spacing(8)
                    .Children(
                        IconButton(MewDummyDisplayStrings.WindowRemove.Value, Icons.Remove(), () => Remove(dummy)))));
    }

    /// <summary>
    /// Chooses which monitor shows this dummy's picture.
    /// </summary>
    /// <remarks>
    /// Worth stating plainly because the direction is the whole point and easy to invert.
    /// A dummy supplies a resolution the monitor cannot offer by itself, so the monitor is
    /// what mirrors the dummy. Naming the monitor in the list, rather than offering an on
    /// and off button against an unnamed "main display", is what makes that readable.
    /// </remarks>
    private Grid BuildMirrorRow(Dummy dummy)
    {
        List<DisplayInfo> candidates =
        [
            .. DisplayCatalog.Online()
                .Where(display => display.VendorId != DummySpec.VENDOR_ID)
                .Where(display => display.DisplayId != dummy.DisplayId),
        ];

        DisplayInfo? showingIt = DisplayCatalog.DisplaysMirroring(dummy.DisplayId).FirstOrDefault();
        List<string> options = [MewDummyDisplayStrings.WindowMirrorNone.Value, .. candidates.Select(DisplayLabel)];
        int selected = showingIt is null
            ? 0
            : candidates.FindIndex(display => display.DisplayId == showingIt.DisplayId) + 1;

        return new Grid()
            .Columns("Auto,*")
            .Rows("Auto,Auto")
            .Spacing(8)
            .Children(
                new TextBlock().Text(MewDummyDisplayStrings.WindowMirrorLabel.Value).CenterVertical(),
                new ComboBox()
                    .Column(1)
                    .Items([.. options])
                    .SelectedIndex(Math.Max(0, selected))
                    .OnSelectionChanged(value => ApplyMirror(dummy, candidates, options.IndexOf(value as string ?? ""))),
                Muted(MewDummyDisplayStrings.WindowMirrorHint.Value).Row(1).Column(1));
    }

    private static string DisplayLabel(DisplayInfo display)
    {
        List<string> tags = [];
        if (display.IsBuiltIn)
        {
            tags.Add(MewDummyDisplayStrings.SystemBuiltIn.Value);
        }
        if (display.IsMain)
        {
            tags.Add(MewDummyDisplayStrings.SystemMain.Value);
        }
        return $"Display {display.DisplayId}  {display.Width}x{display.Height}" +
            (tags.Count > 0 ? $"  ({string.Join(", ", tags)})" : "");
    }

    private static string MirrorSuffix(Dummy dummy)
    {
        DisplayInfo? showingIt = DisplayCatalog.DisplaysMirroring(dummy.DisplayId).FirstOrDefault();
        return showingIt is null
            ? ""
            : "  " + string.Format(MewDummyDisplayStrings.DummyMirroring.Value, $"Display {showingIt.DisplayId}");
    }

    /// <summary>A dummy that is defined but turned off has no display to describe.</summary>
    private Border BuildDisconnectedCard(Dummy dummy)
        => Card(new StackPanel()
            .Spacing(10)
            .Children(
                PowerHeader(dummy, Icons.DisplayOutline(20), $"{dummy.Spec.Definition.Id}  {MewDummyDisplayStrings.DummyOff.Value}"),
                new StackPanel()
                    .Horizontal()
                    .Spacing(8)
                    .Children(
                        IconButton(MewDummyDisplayStrings.WindowRemove.Value, Icons.Remove(), () => Remove(dummy)))));

    /// <summary>
    /// Card header: icon, name, one line of detail, and the switch that turns the display
    /// on or off. A switch rather than a button, because this is a state that stays, not an
    /// action that happens once.
    /// </summary>
    private Grid PowerHeader(Dummy dummy, Element icon, string detail)
        => new Grid()
            .Columns("Auto,*,Auto")
            .Children(
                icon,
                new StackPanel()
                    .Column(1)
                    .Spacing(2)
                    .Margin(10, 0, 10, 0)
                    .Children(
                        new TextBlock().Text(dummy.Name).Bold(),
                        Muted(detail)),
                new ToggleSwitch()
                    .Column(2)
                    .CenterVertical()
                    .IsChecked(dummy.IsConnected)
                    .OnCheckedChanged(_ => ToggleConnected(dummy)));

    private void ToggleConnected(Dummy dummy)
    {
        manager.Toggle(dummy);
        Refresh();
        onChanged();
    }

    // System displays

    private void RefreshSystemDisplays()
    {
        if (_systemList is null)
        {
            return;
        }

        _systemList.Clear();

        foreach (DisplayInfo display in DisplayCatalog.Online())
        {
            List<string> tags = [];
            if (display.IsBuiltIn)
            {
                tags.Add(MewDummyDisplayStrings.SystemBuiltIn.Value);
            }
            if (display.IsMain)
            {
                tags.Add(MewDummyDisplayStrings.SystemMain.Value);
            }
            if (display.VendorId == DummySpec.VENDOR_ID)
            {
                tags.Add(MewDummyDisplayStrings.SystemVirtual.Value);
            }

            int modeCount = DisplayCatalog.Modes(display.DisplayId).Count;

            _systemList.Add(Card(new StackPanel()
                .Horizontal()
                .Spacing(10)
                .Children(
                    Icons.DisplayOutline(20),
                    new StackPanel()
                        .Spacing(2)
                        .Children(
                            new TextBlock().Text($"Display {display.DisplayId}" +
                                (tags.Count > 0 ? $"  ({string.Join(", ", tags)})" : "")).Bold(),
                            Muted($"{display.Width}x{display.Height}" +
                                (display.IsHiDpi ? $"  HiDPI {display.PixelWidth}x{display.PixelHeight}" : "") +
                                $"  {display.RefreshRate:0}Hz  {modeCount} {MewDummyDisplayStrings.SystemModes.Value}")))));
        }
    }

    // Actions

    private void ApplyResolution(Dummy dummy, List<DisplayMode> modes, int index)
    {
        if (index < 0 || index >= modes.Count)
        {
            return;
        }

        dummy.TrySetMode(modes[index]);
        Refresh();
        onChanged();
    }

    private void RenameDummy(Dummy dummy, string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim() == dummy.Name)
        {
            return;
        }

        manager.Rename(dummy, name.Trim());
        Refresh();
        onChanged();
    }

    private void ApplyMirror(Dummy dummy, List<DisplayInfo> candidates, int optionIndex)
    {
        // Whatever was showing this dummy stops first, so switching monitors does not leave
        // the previous one stuck on it.
        foreach (DisplayInfo showing in DisplayCatalog.DisplaysMirroring(dummy.DisplayId))
        {
            DisplayCatalog.ClearMirror(showing.DisplayId);
        }

        int candidateIndex = optionIndex - 1;
        if (candidateIndex >= 0 && candidateIndex < candidates.Count)
        {
            DisplayCatalog.SetMirror(candidates[candidateIndex].DisplayId, dummy.DisplayId);
        }

        Refresh();
        onChanged();
    }

    private void Remove(Dummy dummy)
    {
        manager.Remove(dummy);
        Refresh();
        onChanged();
    }

    // Shared visuals

    private static TextBlock Heading(string text) => new TextBlock().Text(text).FontSize(16).Bold();

    private static TextBlock Muted(string text)
        => new TextBlock().Text(text).WithTheme((theme, block) => block.Foreground(theme.Palette.DisabledText));

    private static Border Card(UIElement content)
        => new Border()
            .Padding(14)
            .WithTheme((theme, border) =>
            {
                border.Background(theme.Palette.ControlBackground);
                border.BorderBrush(theme.Palette.ControlBorder);
                border.BorderThickness(theme.Metrics.ControlBorderThickness);
                border.CornerRadius(theme.Metrics.ControlCornerRadius);
            })
            .Child(content);

    private static Button IconButton(string text, Element icon, Action onClick)
        => new Button()
            .OnClick(onClick)
            .Content(new StackPanel()
                .Horizontal()
                .Spacing(6)
                .Children(icon, new TextBlock().Text(text).CenterVertical()));

    private sealed record NavigationPage(string Title, Element Icon, Func<UIElement> Build);
}
