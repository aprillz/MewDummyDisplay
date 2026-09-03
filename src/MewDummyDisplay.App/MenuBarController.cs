using Aprillz.MewDummyDisplay.Interop;
using Aprillz.MewUI;

namespace Aprillz.MewDummyDisplay.App;

/// <summary>Builds the menu bar menu and turns its rows into library calls.</summary>
/// <remarks>
/// The menu does one thing: turn a dummy on or off. That is the action a person repeats,
/// and a check mark next to a name says the current state without being read. Creating,
/// renaming and removing are rare and destructive by comparison, so they live in the
/// window, which also keeps the interop surface here small and flat.
/// </remarks>
internal sealed class MenuBarController : IDisposable
{
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

    /// <summary>The dummies currently defined, connected or not.</summary>
    internal IReadOnlyList<Dummy> Dummies => _manager.Dummies;

    /// <summary>The manager, so a caller can make the same library calls the window makes.</summary>
    internal DummyManager Manager => _manager;

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

        if (_manager.Dummies.Count == 0)
        {
            entries.Add(new MenuEntry { Title = MewDummyDisplayStrings.MenuNoDummies.Value });
        }
        else
        {
            entries.Add(new MenuEntry { Title = MewDummyDisplayStrings.MenuHint.Value });
            foreach (Dummy dummy in _manager.Dummies)
            {
                entries.Add(DummyEntry(dummy));
            }
        }

        entries.Add(MenuEntry.Separator);
        entries.Add(new MenuEntry
        {
            Title = MewDummyDisplayStrings.MenuManage.Value,
            Handler = _manageWindow.Show,
        });
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

    /// <summary>One dummy row. The check mark is its on or off state; selecting it toggles.</summary>
    private MenuEntry DummyEntry(Dummy dummy)
    {
        string detail;
        if (!dummy.IsConnected)
        {
            detail = MewDummyDisplayStrings.DummyOff.Value;
        }
        else
        {
            DisplayInfo info = DisplayCatalog.Describe(dummy.DisplayId);
            detail = info.Width > 0 ? $"{info.Width}x{info.Height}" : "...";
            if (info.IsMirroring)
            {
                detail += $", {MewDummyDisplayStrings.DummyMirroring.Value}";
            }
        }

        return new MenuEntry
        {
            Title = $"{dummy.Name}  ({detail})",
            IsChecked = dummy.IsConnected,
            Handler = () => Toggle(dummy),
        };
    }

    private void Toggle(Dummy dummy)
    {
        _manager.Toggle(dummy);
        _manageWindow.Refresh();
        Rebuild();
    }
}
