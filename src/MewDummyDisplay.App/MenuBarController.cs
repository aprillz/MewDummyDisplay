using Aprillz.MewDummyDisplay.Interop;
using Aprillz.MewUI;

namespace Aprillz.MewDummyDisplay.App;

/// <summary>Builds the menu bar menu and turns its rows into library calls.</summary>
/// <remarks>
/// The menu holds only what gets used repeatedly: one row per dummy, a short list of
/// ratios to add, and quit. Denser editing belongs in a window. Rows are flat by design,
/// with no submenus, which keeps the interop surface small.
/// </remarks>
internal sealed class MenuBarController : IDisposable
{
    /// <summary>Ratios offered directly in the menu, chosen to cover the common cases.</summary>
    private static readonly string[] _quickAddIds = ["16:9", "16:10", "21.3:9", "9:16"];

    private readonly DummyManager _manager = new();
    private readonly StatusItemHost _host;
    private readonly ManageWindow _manageWindow;

    internal MenuBarController()
    {
        _host = new StatusItemHost("display.2", "MDD");
        _manageWindow = new ManageWindow(_manager, Rebuild);
        Rebuild();
    }

    /// <summary>The status item, so a caller can inspect or drive the menu it built.</summary>
    internal StatusItemHost Host => _host;

    /// <summary>The dummies currently alive.</summary>
    internal IReadOnlyList<Dummy> Dummies => _manager.Dummies;

    /// <summary>The management window, so a caller can drive or inspect it.</summary>
    internal ManageWindow ManageWindow => _manageWindow;

    /// <summary>Rebuilds the menu from the current set of dummies.</summary>
    internal void Rebuild()
    {
        List<MenuEntry> entries = [];

        if (!DummyManager.IsSupported)
        {
            entries.Add(new MenuEntry { Title = MewDummyDisplayStrings.MenuUnsupported.Value });
            entries.Add(MenuEntry.Separator);
            entries.Add(QuitEntry());
            _host.SetMenu(entries);
            return;
        }

        IReadOnlyList<Dummy> dummies = _manager.Dummies;
        if (dummies.Count == 0)
        {
            entries.Add(new MenuEntry { Title = MewDummyDisplayStrings.MenuNoDummies.Value });
        }
        else
        {
            foreach (Dummy dummy in dummies)
            {
                entries.Add(DummyEntry(dummy));
            }
            entries.Add(MenuEntry.Separator);
            entries.Add(new MenuEntry
            {
                Title = MewDummyDisplayStrings.MenuRemoveAll.Value,
                Handler = RemoveAll,
            });
        }

        entries.Add(MenuEntry.Separator);
        entries.Add(new MenuEntry
        {
            Title = MewDummyDisplayStrings.MenuManage.Value,
            Handler = _manageWindow.Show,
        });

        entries.Add(MenuEntry.Separator);
        entries.Add(new MenuEntry { Title = MewDummyDisplayStrings.MenuAddHeading.Value });
        foreach (string id in _quickAddIds)
        {
            DummyDefinition? definition = DummyDefinitionCatalog.Find(id);
            if (definition is null)
            {
                continue;
            }
            entries.Add(new MenuEntry
            {
                Title = MewDummyDisplayStrings.Definition(definition),
                Handler = () => Add(definition),
            });
        }

        entries.Add(MenuEntry.Separator);
        entries.Add(QuitEntry());

        _host.SetMenu(entries);
    }

    public void Dispose() => _manager.Dispose();

    private static MenuEntry QuitEntry() => new()
    {
        Title = MewDummyDisplayStrings.MenuQuit.Value,
        KeyEquivalent = "q",
        Handler = Application.Shutdown,
    };

    /// <summary>One dummy row. Selecting it toggles mirroring of the main display.</summary>
    private MenuEntry DummyEntry(Dummy dummy)
    {
        DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);
        string size = info.Width > 0 ? $"{info.Width}x{info.Height}" : "...";
        string suffix = info.IsMirroring ? $"  {MewDummyDisplayStrings.DummyMirroring.Value}" : "";

        return new MenuEntry
        {
            Title = $"{dummy.Spec.Definition.Id}  {size}  #{dummy.SerialNumber:X8}{suffix}",
            IsChecked = info.IsMirroring,
            Handler = () => ToggleMirror(dummy),
        };
    }

    private void Add(DummyDefinition definition)
    {
        _manager.Create(new DummySpec { Definition = definition });
        _manageWindow.Refresh();
        Rebuild();
    }

    private void RemoveAll()
    {
        foreach (Dummy dummy in _manager.Dummies.ToArray())
        {
            _manager.Remove(dummy);
        }
        _manageWindow.Refresh();
        Rebuild();
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
        Rebuild();
    }
}
