#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Notepad", "Multi-tab text editor using the TabView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TabView")]
[ScenarioCategory ("TextView")]
public class Notepad : Scenario
{
    private IApplication? _app;
    private TabView? _focusedTabView;
    private int _numNewTabs = 1;
    private TabView? _tabView;
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
                                           Action = () => MessageBox.Query (app,  "Notepad", "About Notepad...", "Ok")
                                       }
                                   ]
                                  )
                 );

        _tabView = CreateNewTabView ();

        _tabView.Style.ShowBorder = true;
        _tabView.ApplyStyleChanges ();

        _tabView.X = 0;
        _tabView.Y = Pos.Bottom (menu);
        _tabView.Width = Dim.Fill ();
        _tabView.Height = Dim.Fill (1);

        LenShortcut = new (Key.Empty, "Len: ", null);

        // StatusBar
        StatusBar statusBar = new (
                                   [
                                       new (Application.QuitKey, "Quit", Quit),
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

        _topWindow.Add (menu, _tabView, statusBar);

        _focusedTabView = _tabView;
        _tabView.SelectedTabChanged += TabView_SelectedTabChanged;
        _tabView.HasFocusChanging += (_, _) => _focusedTabView = _tabView;

        _topWindow.IsModalChanged += (_, e) =>
                     {
                         if (e.Value)
                         {
                             New ();
                             LenShortcut.Title = $"Len:{_focusedTabView?.Text.Length ?? 0}";
                         }
                     };

        app.Run (_topWindow);
        _topWindow.Dispose ();
    }

    public void Save ()
    {
        if (_focusedTabView?.SelectedTab is not null)
        {
            Save (_focusedTabView, _focusedTabView.SelectedTab);
        }
    }

    public void Save (TabView tabViewToSave, Tab tabToSave)
    {
        if (tabToSave is not OpenedFile tab)
        {
            return;
        }

        if (tab.File is null)
        {
            SaveAs ();
        }
        else
        {
            tab.Save ();
        }

        tabViewToSave.SetNeedsDraw ();
    }

    public bool SaveAs ()
    {
        if (_focusedTabView?.SelectedTab is not OpenedFile tab)
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
        tab.Text = fd.FileName;
        tab.Save ();

        fd.Dispose ();

        return true;
    }

    private void Close ()
    {
        if (_focusedTabView?.SelectedTab is not null)
        {
            Close (_focusedTabView, _focusedTabView.SelectedTab);
        }
    }

    private void Close (TabView tv, Tab tabToClose)
    {
        if (tabToClose is not OpenedFile tab)
        {
            return;
        }

        _focusedTabView = tv;

        if (tab.UnsavedChanges)
        {
            int? result = MessageBox.Query (tv.App!,
                                            "Save Changes",
                                            $"Save changes to {tab.Text.TrimEnd ('*')}",
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
                if (tab.File is null)
                {
                    SaveAs ();
                }
                else
                {
                    tab.Save ();
                }
            }
        }

        // close and dispose the tab
        tv.RemoveTab (tab);
        tab.View?.Dispose ();
        _focusedTabView = tv;

        // If last tab is closed, open a new one
        if (tv.Tabs.Count == 0)
        {
            New ();
        }
    }

    private TabView CreateNewTabView ()
    {
        TabView tv = new () { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        tv.TabClicked += TabView_TabClicked;
        tv.SelectedTabChanged += TabView_SelectedTabChanged;
        tv.HasFocusChanging += (_, _) => _focusedTabView = tv;

        return tv;
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

                // TODO should open in focused TabView
                Open (new (path), Path.GetFileName (path));
            }
        }

        open.Dispose ();
    }

    /// <summary>Creates a new tab with initial text</summary>
    /// <param name="fileInfo">File that was read or null if a new blank document</param>
    /// <param name="tabName"></param>
    private void Open (FileInfo? fileInfo, string tabName)
    {
        if (_focusedTabView is null)
        {
            return;
        }

        OpenedFile tab = new (this) { DisplayText = tabName, File = fileInfo };
        tab.View = tab.CreateTextView (fileInfo);
        tab.SavedText = tab.View.Text;
        tab.RegisterTextViewEvents (_focusedTabView);

        _focusedTabView.AddTab (tab, true);
    }

    private void Quit () { _topWindow?.RequestStop (); }

    private void TabView_SelectedTabChanged (object? sender, TabChangedEventArgs e)
    {
        if (LenShortcut is not null)
        {
            LenShortcut.Title = $"Len:{e.NewTab?.View?.Text.Length ?? 0}";
        }

        e.NewTab?.View?.SetFocus ();
    }

    private void TabView_TabClicked (object? sender, TabMouseEventArgs e)
    {
        // we are only interested in right clicks
        if (!e.MouseEvent.Flags.HasFlag (MouseFlags.RightButtonClicked))
        {
            return;
        }

        View [] items;

        if (e.Tab is null)
        {
            items = [new MenuItem { Title = "Open", Action = Open }];
        }
        else
        {
            TabView tv = (TabView)sender!;

            items =
            [
                new MenuItem { Title = "Save", Action = () => Save (_focusedTabView!, e.Tab) },
                new MenuItem { Title = "Close", Action = () => Close (tv, e.Tab) }
            ];
        }

        PopoverMenu contextMenu = new (items);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        if (sender is TabView tabView && tabView.App?.Popover is not null)
        {
            tabView.App.Popover.Register (contextMenu);
        }

        contextMenu.MakeVisible (e.MouseEvent.ScreenPosition);

        e.MouseEvent.Handled = true;
    }

    private class OpenedFile (Notepad notepad) : Tab
    {
        private readonly Notepad _notepad = notepad;
        public OpenedFile CloneTo (TabView other)
        {
            OpenedFile newTab = new (_notepad) { DisplayText = Text, File = File };
            newTab.View = newTab.CreateTextView (newTab.File);
            newTab.SavedText = newTab.View.Text;
            newTab.RegisterTextViewEvents (other);
            other.AddTab (newTab, true);

            return newTab;
        }

        public View CreateTextView (FileInfo? file)
        {
            var initialText = string.Empty;

            if (file is { Exists: true })
            {
                initialText = System.IO.File.ReadAllText (file.FullName);
            }

            return new TextView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Text = initialText,
                TabKeyAddsTab = false
            };
        }

        public FileInfo? File { get; set; }

        public void RegisterTextViewEvents (TabView parent)
        {
            if (View is not TextView textView)
            {
                return;
            }

            // when user makes changes rename tab to indicate unsaved
            textView.ContentsChanged += (_, _) =>
                                        {
                                            // if current text doesn't match saved text
                                            bool areDiff = UnsavedChanges;

                                            if (areDiff)
                                            {
                                                if (!DisplayText.EndsWith ('*'))
                                                {
                                                    DisplayText = Text + '*';
                                                }
                                            }
                                            else
                                            {
                                                if (DisplayText.EndsWith ('*'))
                                                {
                                                    DisplayText = Text.TrimEnd ('*');
                                                }
                                            }

                                            if (_notepad.LenShortcut is not null)
                                            {
                                                _notepad.LenShortcut.Title = $"Len:{textView.Text.Length}";
                                            }
                                        };
        }

        /// <summary>The text of the tab the last time it was saved</summary>
        public string? SavedText { get; set; }

        public bool UnsavedChanges => View is not null && !string.Equals (SavedText, View.Text);

        internal void Save ()
        {
            if (View is null || File is null || string.IsNullOrWhiteSpace (File.FullName))
            {
                return;
            }

            string newText = View.Text;

            System.IO.File.WriteAllText (File.FullName, newText);
            SavedText = newText;

            DisplayText = DisplayText.TrimEnd ('*');
        }
    }
}
