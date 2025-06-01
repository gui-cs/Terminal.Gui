using System.IO;
using System.Linq;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Notepad", "Multi-tab text editor using the TabView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TabView")]
[ScenarioCategory ("TextView")]
public class Notepad : Scenario
{
    private TabView _focusedTabView;
    public Shortcut LenShortcut { get; private set; }
    private int _numNewTabs = 1;
    private TabView _tabView;

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

        // Start with only a single view but support splitting to show side by side
        var split = new TileView (1) { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };
        split.Tiles.ElementAt (0).ContentView.Add (_tabView);
        split.LineStyle = LineStyle.None;

        top.Add (split);
        LenShortcut = new (Key.Empty, "Len: ", null);

        var statusBar = new StatusBar (new [] {
                                           new (Application.QuitKey, $"Quit", Quit),
                                           new Shortcut(Key.F2, "Open", Open),
                                           new Shortcut(Key.F1, "New", New),
                                           new (Key.F3, "Save", Save),
                                           new (Key.F6, "Close", Close),
                                           LenShortcut
                                       }
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

    public void Save () { Save (_focusedTabView, _focusedTabView.SelectedTab); }

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
        var tab = _focusedTabView.SelectedTab as OpenedFile;

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

    private void Close () { Close (_focusedTabView, _focusedTabView.SelectedTab); }

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
        tab.View.Dispose ();
        _focusedTabView = tv;

        if (tv.Tabs.Count == 0)
        {
            var split = (TileView)tv.SuperView.SuperView;

            // if it is the last TabView on screen don't drop it or we will
            // be unable to open new docs!
            if (split.IsRootTileView () && split.Tiles.Count == 1)
            {
                return;
            }

            int tileIndex = split.IndexOf (tv);
            split.RemoveTile (tileIndex);

            if (split.Tiles.Count == 0)
            {
                TileView parent = split.GetParentTileView ();

                if (parent == null)
                {
                    return;
                }

                int idx = parent.IndexOf (split);

                if (idx == -1)
                {
                    return;
                }

                parent.RemoveTile (idx);
            }
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

    private void New () { Open (null, $"new {_numNewTabs++}"); }

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
    private void Open (FileInfo fileInfo, string tabName)
    {
        var tab = new OpenedFile (this) { DisplayText = tabName, File = fileInfo };
        tab.View = tab.CreateTextView (fileInfo);
        tab.SavedText = tab.View.Text;
        tab.RegisterTextViewEvents (_focusedTabView);

        _focusedTabView.AddTab (tab, true);
    }

    private void Quit () { Application.RequestStop (); }

    private void Split (int offset, Orientation orientation, TabView sender, OpenedFile tab)
    {
        var split = (TileView)sender.SuperView.SuperView;
        int tileIndex = split.IndexOf (sender);

        if (tileIndex == -1)
        {
            return;
        }

        if (orientation != split.Orientation)
        {
            split.TrySplitTile (tileIndex, 1, out split);
            split.Orientation = orientation;
            tileIndex = 0;
        }

        Tile newTile = split.InsertTile (tileIndex + offset);
        TabView newTabView = CreateNewTabView ();
        tab.CloneTo (newTabView);
        newTile.ContentView.Add (newTabView);

        newTabView.FocusDeepest (NavigationDirection.Forward, null);
        newTabView.AdvanceFocus (NavigationDirection.Forward, null);
    }

    private void SplitDown (TabView sender, OpenedFile tab) { Split (1, Orientation.Horizontal, sender, tab); }
    private void SplitLeft (TabView sender, OpenedFile tab) { Split (0, Orientation.Vertical, sender, tab); }
    private void SplitRight (TabView sender, OpenedFile tab) { Split (1, Orientation.Vertical, sender, tab); }
    private void SplitUp (TabView sender, OpenedFile tab) { Split (0, Orientation.Horizontal, sender, tab); }

    private void TabView_SelectedTabChanged (object sender, TabChangedEventArgs e)
    {
        LenShortcut.Title = $"Len:{e.NewTab?.View?.Text?.Length ?? 0}";

        e.NewTab?.View?.SetFocus ();
    }

    private void TabView_TabClicked (object sender, TabMouseEventArgs e)
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
            var tv = (TabView)sender;
            var t = (OpenedFile)e.Tab;

            items =
            [
                new MenuItemv2 ("Save", "", () => Save (_focusedTabView, e.Tab)),
                new MenuItemv2 ("Close", "", () => Close (tv, e.Tab)),
                new Line (),
                new MenuItemv2 ("Split Up", "", () => SplitUp (tv, t)),
                new MenuItemv2 ("Split Down", "", () => SplitDown (tv, t)),
                new MenuItemv2 ("Split Right", "", () => SplitRight (tv, t)),
                new MenuItemv2 ("Split Left", "", () => SplitLeft (tv, t))
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
        private Notepad _notepad = notepad;

        public OpenedFile CloneTo (TabView other)
        {
            var newTab = new OpenedFile (_notepad) { DisplayText = base.Text, File = File };
            newTab.View = newTab.CreateTextView (newTab.File);
            newTab.SavedText = newTab.View.Text;
            newTab.RegisterTextViewEvents (other);
            other.AddTab (newTab, true);

            return newTab;
        }

        public View CreateTextView (FileInfo file)
        {
            var initialText = string.Empty;

            if (file != null && file.Exists)
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

        public FileInfo File { get; set; }

        public void RegisterTextViewEvents (TabView parent)
        {
            var textView = (TextView)View;

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
                                            _notepad.LenShortcut.Title = $"Len:{textView.Text.Length}";
                                        };
        }

        /// <summary>The text of the tab the last time it was saved</summary>
        /// <value></value>
        public string SavedText { get; set; }

        public bool UnsavedChanges => !string.Equals (SavedText, View.Text);

        internal void Save ()
        {
            string newText = View.Text;

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
