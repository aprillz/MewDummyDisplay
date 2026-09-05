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

    private readonly bool _persist;

    /// <param name="persist">
    /// When false the settings file is neither read nor written. The self test needs that:
    /// otherwise a test run would adopt the user's dummies and then overwrite their file.
    /// </param>
    internal MenuBarController(bool persist = true)
    {
        _persist = persist;
        _host = new StatusItemHost(MenuBarIcon.Create("MewDummyDisplay"), "MDD");
        _manageWindow = new ManageWindow(_manager, OnChanged);

        if (_persist)
        {
            _manager.RestoreSettings(SettingsStore.Load());
        }

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
            // The master switch leads, because it is what the application as a whole does.
            entries.Add(AllEntry());
            entries.Add(MenuEntry.Separator);
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

            DisplayInfo? showingIt = DisplayCatalog.DisplaysMirroring(dummy.DisplayId).FirstOrDefault();
            if (showingIt is not null)
            {
                detail += ", " + string.Format(
                    MewDummyDisplayStrings.DummyMirroring.Value, $"Display {showingIt.DisplayId}");
            }
        }

        return new MenuEntry
        {
            Title = $"{dummy.Name}  ({detail})",
            IsChecked = dummy.IsConnected,
            UseSwitch = true,
            Handler = () => Toggle(dummy),
        };
    }

    /// <summary>
    /// The row at the top that turns every dummy on or off at once.
    /// </summary>
    /// <remarks>
    /// It is the master switch for what the application does, so it leads the menu and
    /// keeps that position whenever there is anything to switch, rather than appearing
    /// once some number of dummies is reached.
    ///
    /// The switch reads as "all of them are on", so it is on only when none is left off,
    /// and the count says what a two state switch cannot when some are on and some are not.
    /// Selecting it turns them all on unless they already are, in which case it turns them
    /// all off, which is what a master switch is for.
    /// </remarks>
    private MenuEntry AllEntry()
    {
        int total = _manager.Dummies.Count;
        int on = _manager.Dummies.Count(dummy => dummy.IsConnected);
        string detail = string.Format(MewDummyDisplayStrings.MenuAllOnCount.Value, on, total);

        return new MenuEntry
        {
            Title = $"{MewDummyDisplayStrings.MenuMaster.Value}  ({detail})",
            IsChecked = on == total,
            UseSwitch = true,
            Handler = () => SetAll(on < total),
        };
    }

    private void Toggle(Dummy dummy)
    {
        _manager.Toggle(dummy);
        _manageWindow.Refresh();
        OnChanged();
    }

    /// <summary>Connects or disconnects every dummy, skipping the ones already there.</summary>
    private void SetAll(bool connected)
    {
        foreach (Dummy dummy in _manager.Dummies)
        {
            if (dummy.IsConnected == connected)
            {
                continue;
            }

            if (connected)
            {
                _manager.Connect(dummy);
            }
            else
            {
                _manager.Disconnect(dummy);
            }
        }

        _manageWindow.Refresh();
        OnChanged();
    }

    /// <summary>Rebuilds the menu and remembers the new state.</summary>
    internal void OnChanged()
    {
        Rebuild();
        if (_persist)
        {
            SettingsStore.Save(_manager.CaptureSettings());
        }
    }
}
