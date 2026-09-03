using Aprillz.MewUI;
using Aprillz.MewUI.Controls;

namespace Aprillz.MewDummyDisplay.App;

/// <summary>The window where dummies are created, resized and removed.</summary>
/// <remarks>
/// Everything denser than a toggle lives here rather than in the menu bar. Resolution is
/// the clearest example: a dummy offers hundreds of modes, which a flat menu cannot show.
///
/// The list is rebuilt explicitly rather than bound to an ItemsSource. Rows carry their
/// own editors, and rebuilding an ItemsSource under an active editor replaces its
/// container and loses focus. Below roughly a hundred rows the explicit rebuild is the
/// simpler and safer choice; past that, move to ItemsSource with a typed ItemTemplate so
/// containers are recycled.
/// </remarks>
internal sealed class ManageWindow(DummyManager manager, Action onChanged)
{
    private readonly IReadOnlyList<DummyDefinition> _definitions = DummyDefinitionCatalog.All();
    private Window? _window;
    private StackPanel? _dummyList;
    private ComboBox? _definitionPicker;
    private CheckBox? _hiDpiToggle;

    /// <summary>Shows the window, bringing an already open one forward.</summary>
    internal void Show()
    {
        if (_window is not null)
        {
            _window.Show();
            _window.Activate();
            return;
        }

        _window = new Window()
            .Title(MewDummyDisplayStrings.WindowTitle.Value)
            .Resizable(560, 520)
            .Content(BuildContent());

        _window.Closed += () =>
        {
            _window = null;
            _dummyList = null;
        };

        _window.Show();
        RefreshDummyList();
    }

    /// <summary>Whether the window is currently open.</summary>
    internal bool IsOpen => _window is not null;

    /// <summary>Rows currently shown. Lets a caller confirm what was built.</summary>
    internal int RowCount => _dummyList?.Count ?? 0;

    /// <summary>Closes the window if it is open.</summary>
    internal void CloseIfOpen() => _window?.Close();

    /// <summary>Rebuilds the list if the window is open. Safe to call when it is not.</summary>
    internal void Refresh()
    {
        if (_window is not null)
        {
            RefreshDummyList();
        }
    }

    private StackPanel BuildContent()
        => new StackPanel()
            .Padding(20)
            .Spacing(16)
            .Children(
                new TextBlock().Text(MewDummyDisplayStrings.WindowCreateHeading.Value),
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Spacing(10)
                    .Children(
                        new TextBlock().Text(MewDummyDisplayStrings.WindowAspectRatio.Value),
                        new ComboBox()
                            .Ref(out ComboBox picker)
                            .Width(220)
                            .Items([.. _definitions.Select(MewDummyDisplayStrings.Definition)])
                            .SelectedIndex(0),
                        new CheckBox()
                            .Ref(out CheckBox hiDpi)
                            .Content(MewDummyDisplayStrings.WindowHiDpi.Value)
                            .IsChecked(true),
                        new Button()
                            .Content(MewDummyDisplayStrings.WindowCreate.Value)
                            .OnClick(CreateDummy)),
                new Separator(),
                new TextBlock().Text(MewDummyDisplayStrings.WindowExisting.Value),
                new ScrollViewer()
                    .Height(300)
                    .Content(new StackPanel().Ref(out StackPanel list).Spacing(12)),
                new StackPanel()
                    .Orientation(Orientation.Horizontal)
                    .Children(new Button().Content(MewDummyDisplayStrings.WindowClose.Value).OnClick(Close)))
            .Also(() =>
            {
                _definitionPicker = picker;
                _hiDpiToggle = hiDpi;
                _dummyList = list;
            });

    private void Close() => _window?.Close();

    private void CreateDummy()
    {
        if (_definitionPicker is null || _hiDpiToggle is null)
        {
            return;
        }

        int index = Math.Max(0, _definitionPicker.SelectedIndex);
        manager.Create(new DummySpec
        {
            Definition = _definitions[index],
            HiDpi = _hiDpiToggle.IsChecked == true,
        });

        RefreshDummyList();
        onChanged();
    }

    private void RefreshDummyList()
    {
        if (_dummyList is null)
        {
            return;
        }

        _dummyList.Clear();

        if (manager.Dummies.Count == 0)
        {
            _dummyList.Add(new TextBlock().Text(MewDummyDisplayStrings.WindowNone.Value));
            return;
        }

        foreach (Dummy dummy in manager.Dummies)
        {
            _dummyList.Add(BuildDummyRow(dummy));
        }
    }

    private Border BuildDummyRow(Dummy dummy)
    {
        DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);

        // Only the HiDPI modes are offered: a dummy exists to provide a Retina resolution,
        // and listing both variants of every size would double an already long list.
        List<DisplayMode> modes = [.. dummy.Modes()
            .Where(mode => mode.IsHiDpi)
            .DistinctBy(mode => (mode.Width, mode.Height))];
        if (modes.Count == 0)
        {
            modes = [.. dummy.Modes().DistinctBy(mode => (mode.Width, mode.Height))];
        }

        int currentIndex = modes.FindIndex(mode => mode.Width == info.Width && mode.Height == info.Height);

        return new Border()
            .Padding(12)
            .Child(new StackPanel()
                .Spacing(8)
                .Children(
                    new TextBlock().Text(
                        $"{dummy.Spec.Definition.Id}  #{dummy.SerialNumber:X8}  " +
                        $"{info.Width}x{info.Height}" +
                        (info.IsHiDpi ? $" HiDPI ({info.PixelWidth}x{info.PixelHeight})" : "")),
                    new StackPanel()
                        .Orientation(Orientation.Horizontal)
                        .Spacing(8)
                        .Children(
                            new TextBlock().Text(MewDummyDisplayStrings.WindowResolution.Value),
                            new ComboBox()
                                .Ref(out ComboBox resolutionPicker)
                                .Width(200)
                                .Items([.. modes.Select(mode => $"{mode.Width}x{mode.Height}")])
                                .SelectedIndex(Math.Max(0, currentIndex)),
                            new Button()
                                .Content(MewDummyDisplayStrings.WindowApply.Value)
                                .OnClick(() => ApplyResolution(dummy, modes, resolutionPicker.SelectedIndex)),
                            new Button()
                                .Content(info.IsMirroring
                                    ? MewDummyDisplayStrings.WindowMirrorOff.Value
                                    : MewDummyDisplayStrings.WindowMirrorOn.Value)
                                .OnClick(() => ToggleMirror(dummy)),
                            new Button()
                                .Content(MewDummyDisplayStrings.WindowRemove.Value)
                                .OnClick(() => Remove(dummy)))));
    }

    private void ApplyResolution(Dummy dummy, List<DisplayMode> modes, int index)
    {
        if (index < 0 || index >= modes.Count)
        {
            return;
        }

        dummy.TrySetMode(modes[index]);
        RefreshDummyList();
        onChanged();
    }

    private void ToggleMirror(Dummy dummy)
    {
        DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);
        if (info.IsMirroring)
        {
            DisplayCatalog.ClearMirror(dummy.DisplayId);
        }
        else
        {
            DisplayInfo? main = DisplayCatalog.Online().FirstOrDefault(display => display.IsMain);
            if (main is not null)
            {
                DisplayCatalog.SetMirror(dummy.DisplayId, main.DisplayId);
            }
        }

        RefreshDummyList();
        onChanged();
    }

    private void Remove(Dummy dummy)
    {
        manager.Remove(dummy);
        RefreshDummyList();
        onChanged();
    }
}
