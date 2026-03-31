#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Notepad", "Multi-tab text editor using the Tabs control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Tabs")]
[ScenarioCategory ("TextView")]
public class Notepad : Scenario
{
    private IApplication? _app;
    private Tabs? _focusedTabs;
    private int _numNewTabs = 1;
    private Tabs? _tabs;
    private Window? _topWindow;
    public Shortcut? LenShortcut { get; private set; }

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        _topWindow = new ()
        {
            BorderStyle = LineStyle.None,
        };

        // MenuBar
        MenuBar menu = new ();

        menu.Add (
                  new MenuBarItem (
                                   Strings.menuFile,
                                   [
                                       new MenuItem
                                       {
                                           Title = Strings.cmdNew,
                                           Key = Key.N.WithCtrl.WithAlt,
                                           Action = New
                                       },
                                       new MenuItem
                                       {
                                           Title = Strings.cmdOpen,
                                           Action = Open
                                       },
                                       new MenuItem
                                       {
                                           Title = Strings.cmdSave,
                                           Action = Save
                                       },
                                       new MenuItem
                                       {
                                           Title = "Save _As",
                                           Action = () => SaveAs ()
                                       },
                                       new MenuItem
                                       {
                                           Title = Strings.cmdClose,
                                           Action = Close
                                       },
                                       new MenuItem
                                       {
                                           Title = Strings.cmdQuit,
                                           Action = Quit
                                       }
                                   ]
                                  )
                 );

        menu.Add (
                  new MenuBarItem (
                                   "_About",
                                   [
                                       new MenuItem
                                       {
                                           Title = "_About",
                                           Action = () => MessageBox.Query (app, "Notepad", "About Notepad...", "Ok")
                                       }
                                   ]
                                  )
                 );

        _tabs = new () { X = 0, Y = Pos.Bottom (menu), Width = Dim.Fill (), Height = Dim.Fill (1) };

        LenShortcut = new (Key.Empty, "Len: ", null);

        // StatusBar
        StatusBar statusBar = new (
                                   [
                                       new (Application.GetDefaultKey (Command.Quit), "Quit", Quit),
                                       new (Key.F2, "Open", Open),
                                       new (Key.F1, "New", New),
                                       new (Key.F3, "Save", Save),
                                       new (Key.F6, "Close", Close),
                                       LenShortcut
                                   ]
                                  )
        {
            AlignmentModes = AlignmentModes.IgnoreFirstOrLast
        };

        _topWindow.Add (menu, _tabs, statusBar);

        _focusedTabs = _tabs;
        _tabs.ValueChanged += Tabs_ValueChanged;
        _tabs.HasFocusChanging += (_, _) => _focusedTabs = _tabs;

        _topWindow.IsModalChanged += (_, e) =>
                     {
                         if (e.Value)
                         {
                             New ();
                             LenShortcut.Title = $"Len:{GetSelectedTextLength ()}";
                         }
                     };

        app.Run (_topWindow);
        _topWindow.Dispose ();
    }

    public void Save ()
    {
        if (_focusedTabs?.Value is OpenedFile tab)
        {
            Save (_focusedTabs, tab);
        }
    }

    private void Save (Tabs tabsToSave, OpenedFile tabToSave)
    {
        if (tabToSave.File is null)
        {
            SaveAs ();
        }
        else
        {
            tabToSave.Save ();
        }

        tabsToSave.SetNeedsDraw ();
    }

    public bool SaveAs ()
    {
        if (_focusedTabs?.Value is not OpenedFile tab)
        {
            return false;
        }

        SaveDialog fd = new ();
        _app?.Run (fd);

        if (string.IsNullOrWhiteSpace (fd.Path) || fd.Canceled)
        {
            fd.Dispose ();

            return false;
        }

        tab.File = new (fd.Path);
        tab.Title = fd.FileName;
        tab.Save ();

        fd.Dispose ();

        return true;
    }

    private void Close ()
    {
        if (_focusedTabs?.Value is OpenedFile tab)
        {
            Close (_focusedTabs, tab);
        }
    }

    private void Close (Tabs tabs, OpenedFile tabToClose)
    {
        _focusedTabs = tabs;

        if (tabToClose.UnsavedChanges)
        {
            int? result = MessageBox.Query (tabs.App!,
                                            "Save Changes",
                                            $"Save changes to {tabToClose.Title.TrimEnd ('*')}",
                                            "Yes",
                                            "No",
                                            "Cancel"
                                           );

            if (result is null or 2)
            {
                // user cancelled
                return;
            }

            if (result == 0)
            {
                if (tabToClose.File is null)
                {
                    SaveAs ();
                }
                else
                {
                    tabToClose.Save ();
                }
            }
        }

        // close and dispose the tab
        tabs.Remove (tabToClose);
        tabToClose.Dispose ();
        _focusedTabs = tabs;

        // If last tab is closed, open a new one
        if (!tabs.TabCollection.Any ())
        {
            New ();
        }
    }

    private void New () { Open (null!, $"new {_numNewTabs++}"); }

    private void Open ()
    {
        OpenDialog open = new () { Title = "Open", AllowsMultipleSelection = true };

        _app?.Run (open);

        bool canceled = open.Canceled;

        if (!canceled)
        {
            foreach (string path in open.FilePaths)
            {
                if (string.IsNullOrEmpty (path) || !File.Exists (path))
                {
                    break;
                }

                Open (new (path), Path.GetFileName (path));
            }
        }

        open.Dispose ();
    }

    /// <summary>Creates a new tab with initial text.</summary>
    /// <param name="fileInfo">File that was read or null if a new blank document.</param>
    /// <param name="tabName">Display name for the tab.</param>
    private void Open (FileInfo? fileInfo, string tabName)
    {
        if (_focusedTabs is null)
        {
            return;
        }

        OpenedFile tab = new (this) { Title = tabName, File = fileInfo };
        tab.CreateAndAddTextView (fileInfo);
        tab.RegisterTextViewEvents ();

        _focusedTabs.Add (tab);
        _focusedTabs.Value = tab;
    }

    private void Quit () { _topWindow?.RequestStop (); }

    private int GetSelectedTextLength ()
    {
        if (_focusedTabs?.Value is OpenedFile tab)
        {
            return tab.TextView?.Text.Length ?? 0;
        }

        return 0;
    }

    private void Tabs_ValueChanged (object? sender, ValueChangedEventArgs<View?> e)
    {
        if (LenShortcut is not null)
        {
            var len = 0;

            if (e.NewValue is OpenedFile tab)
            {
                len = tab.TextView?.Text.Length ?? 0;
            }

            LenShortcut.Title = $"Len:{len}";
        }

        if (e.NewValue is OpenedFile openedTab)
        {
            openedTab.TextView?.SetFocus ();
        }
    }

    private class OpenedFile (Notepad notepad) : View
    {
        private readonly Notepad _notepad = notepad;

        public FileInfo? File { get; set; }

        public TextView? TextView { get; private set; }

        /// <summary>The text of the tab the last time it was saved.</summary>
        public string? SavedText { get; set; }

        public bool UnsavedChanges => TextView is not null && !string.Equals (SavedText, TextView.Text);

        public void CreateAndAddTextView (FileInfo? file)
        {
            var initialText = string.Empty;

            if (file is { Exists: true })
            {
                initialText = System.IO.File.ReadAllText (file.FullName);
            }

            TextView = new ()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Text = initialText,
                TabKeyAddsTab = false
            };

            SavedText = initialText;

            Add (TextView);
        }

        public void RegisterTextViewEvents ()
        {
            if (TextView is null)
            {
                return;
            }

            // when user makes changes rename tab to indicate unsaved
            TextView.ContentsChanged += (_, _) =>
                                        {
                                            // if current text doesn't match saved text
                                            bool areDiff = UnsavedChanges;

                                            if (areDiff)
                                            {
                                                if (!Title.EndsWith ('*'))
                                                {
                                                    Title = Title + "*";
                                                }
                                            }
                                            else
                                            {
                                                if (Title.EndsWith ('*'))
                                                {
                                                    Title = Title.TrimEnd ('*');
                                                }
                                            }

                                            if (_notepad.LenShortcut is not null)
                                            {
                                                _notepad.LenShortcut.Title = $"Len:{TextView.Text.Length}";
                                            }
                                        };
        }

        internal void Save ()
        {
            if (TextView is null || File is null || string.IsNullOrWhiteSpace (File.FullName))
            {
                return;
            }

            string newText = TextView.Text;

            System.IO.File.WriteAllText (File.FullName, newText);
            SavedText = newText;

            Title = Title.TrimEnd ('*');
        }
    }
}
