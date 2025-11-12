#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("Notepad", "Multi-tab text editor using the TabView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TabView")]
[ScenarioCategory ("TextView")]
public class Notepad : Scenario
{
    private TabView? _focusedTabView;
    private int _numNewTabs = 1;
    private TabView? _tabView;
    public Shortcut? LenShortcut { get; private set; }

    public override void Main ()
    {
        Application.Init ();

        Toplevel top = new ();

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new (
                              "_New",
                              "",
                              () => New (),
                              null,
                              null,
                              KeyCode.N
                              | KeyCode.CtrlMask
                              | KeyCode.AltMask
                             ),
                         new ("_Open", "", Open),
                         new ("_Save", "", Save),
                         new ("Save _As", "", () => SaveAs ()),
                         new ("_Close", "", Close),
                         new ("_Quit", "", Quit)
                     }
                    ),
                new (
                     "_About",
                     "",
                     () => MessageBox.Query ("Notepad", "About Notepad...", "Ok")
                    )
            ]
        };
        top.Add (menu);

        _tabView = CreateNewTabView ();

        _tabView.Style.ShowBorder = true;
        _tabView.ApplyStyleChanges ();

        _tabView.X = 0;
        _tabView.Y = 1;
        _tabView.Width = Dim.Fill ();
        _tabView.Height = Dim.Fill (1);

        top.Add (_tabView);
        LenShortcut = new (Key.Empty, "Len: ", null);

        var statusBar = new StatusBar (
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
        top.Add (statusBar);

        _focusedTabView = _tabView;
        _tabView.SelectedTabChanged += TabView_SelectedTabChanged;
        _tabView.HasFocusChanging += (s, e) => _focusedTabView = _tabView;

        top.Ready += (s, e) =>
                     {
                         New ();
                         LenShortcut.Title = $"Len:{_focusedTabView.Text?.Length ?? 0}";
                     };

        Application.Run (top);
        top.Dispose ();

        Application.Shutdown ();
    }

    public void Save () { Save (_focusedTabView!, _focusedTabView!.SelectedTab!); }

    public void Save (TabView tabViewToSave, Tab tabToSave)
    {
        var tab = tabToSave as OpenedFile;

        if (tab == null)
        {
            return;
        }

        if (tab.File == null)
        {
            SaveAs ();
        }

        tab.Save ();
        tabViewToSave.SetNeedsDraw ();
    }

    public bool SaveAs ()
    {
        var tab = _focusedTabView!.SelectedTab as OpenedFile;

        if (tab == null)
        {
            return false;
        }

        var fd = new SaveDialog ();
        Application.Run (fd);

        if (string.IsNullOrWhiteSpace (fd.Path))
        {
            fd.Dispose ();

            return false;
        }

        if (fd.Canceled)
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

    private void Close () { Close (_focusedTabView!, _focusedTabView!.SelectedTab!); }

    private void Close (TabView tv, Tab tabToClose)
    {
        var tab = tabToClose as OpenedFile;

        if (tab == null)
        {
            return;
        }

        _focusedTabView = tv;

        if (tab.UnsavedChanges)
        {
            int result = MessageBox.Query (
                                           "Save Changes",
                                           $"Save changes to {tab.Text.TrimEnd ('*')}",
                                           "Yes",
                                           "No",
                                           "Cancel"
                                          );

            if (result == -1 || result == 2)
            {
                // user cancelled
                return;
            }

            if (result == 0)
            {
                if (tab.File == null)
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
        var tv = new TabView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        tv.TabClicked += TabView_TabClicked;
        tv.SelectedTabChanged += TabView_SelectedTabChanged;
        tv.HasFocusChanging += (s, e) => _focusedTabView = tv;

        return tv;
    }

    private void New () { Open (null!, $"new {_numNewTabs++}"); }

    private void Open ()
    {
        var open = new OpenDialog { Title = "Open", AllowsMultipleSelection = true };

        Application.Run (open);

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
    private void Open (FileInfo fileInfo, string tabName)
    {
        var tab = new OpenedFile (this) { DisplayText = tabName, File = fileInfo };
        tab.View = tab.CreateTextView (fileInfo);
        tab.SavedText = tab.View.Text;
        tab.RegisterTextViewEvents (_focusedTabView!);

        _focusedTabView!.AddTab (tab, true);
    }

    private void Quit () { Application.RequestStop (); }

    private void TabView_SelectedTabChanged (object? sender, TabChangedEventArgs e)
    {
        LenShortcut!.Title = $"Len:{e.NewTab?.View?.Text?.Length ?? 0}";

        e.NewTab?.View?.SetFocus ();
    }

    private void TabView_TabClicked (object? sender, TabMouseEventArgs e)
    {
        // we are only interested in right clicks
        if (!e.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked))
        {
            return;
        }

        View [] items;

        if (e.Tab == null)
        {
            items = [new MenuItemv2 ("Open", "", Open)];
        }
        else
        {
            var tv = (TabView)sender!;
            var t = (OpenedFile)e.Tab;

            items =
            [
                new MenuItemv2 ("Save", "", () => Save (_focusedTabView!, e.Tab)),
                new MenuItemv2 ("Close", "", () => Close (tv, e.Tab))
            ];

            PopoverMenu? contextMenu = new (items);

            // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
            // and the context menu is disposed when it is closed.
            Application.Popover?.Register (contextMenu);
            contextMenu?.MakeVisible (e.MouseEvent.ScreenPosition);

            e.MouseEvent.Handled = true;
        }
    }

    private class OpenedFile (Notepad notepad) : Tab
    {
        private readonly Notepad _notepad = notepad;

        public OpenedFile CloneTo (TabView other)
        {
            var newTab = new OpenedFile (_notepad) { DisplayText = base.Text, File = File };
            newTab.View = newTab.CreateTextView (newTab.File!);
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
                AllowsTab = false
            };
        }

        public FileInfo? File { get; set; }

        public void RegisterTextViewEvents (TabView parent)
        {
            var textView = (TextView)View!;

            // when user makes changes rename tab to indicate unsaved
            textView.ContentsChanged += (s, k) =>
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

                                            _notepad.LenShortcut!.Title = $"Len:{textView.Text.Length}";
                                        };
        }

        /// <summary>The text of the tab the last time it was saved</summary>
        /// <value></value>
        public string? SavedText { get; set; }

        public bool UnsavedChanges => !string.Equals (SavedText, View!.Text);

        internal void Save ()
        {
            string newText = View!.Text;

            if (File is null || string.IsNullOrWhiteSpace (File.FullName))
            {
                return;
            }

            System.IO.File.WriteAllText (File.FullName, newText);
            SavedText = newText;

            DisplayText = DisplayText.TrimEnd ('*');
        }
    }
}
