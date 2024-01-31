#region

using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Terminal.Gui;

#endregion

namespace UICatalog.Scenarios;

[ScenarioMetadata ("File System Explorer", "Hierarchical file system explorer demonstrating TreeView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
[ScenarioCategory ("Files and IO")]
public class TreeViewFileSystem : Scenario {
    /// <summary>
    /// A tree view where nodes are files and folders
    /// </summary>
    TreeView<IFileSystemInfo> _treeViewFiles;

    MenuItem _miShowLines;
    MenuItem _miPlusMinus;
    MenuItem _miArrowSymbols;
    MenuItem _miNoSymbols;
    MenuItem _miColoredSymbols;
    MenuItem _miInvertSymbols;
    MenuItem _miBasicIcons;
    MenuItem _miUnicodeIcons;
    MenuItem _miNerdIcons;
    MenuItem _miFullPaths;
    MenuItem _miLeaveLastRow;
    MenuItem _miHighlightModelTextOnly;
    MenuItem _miCustomColors;
    MenuItem _miCursor;
    MenuItem _miMultiSelect;
    DetailsFrame _detailsFrame;
    FileSystemIconProvider _iconProvider = new ();

    public override void Setup () {
        Win.Title = GetName ();
        Win.Y = 1; // menu
        Win.Height = Dim.Fill ();

        var menu = new MenuBar (
                                new MenuBarItem[] {
                                                      new (
                                                           "_File",
                                                           new MenuItem[] {
                                                                              new (
                                                                               "_Quit",
                                                                               $"{Application.QuitKey}",
                                                                               () => Quit ())
                                                                          }),
                                                      new (
                                                           "_View",
                                                           new MenuItem[] {
                                                                              _miFullPaths =
                                                                                  new MenuItem (
                                                                                   "_Full Paths",
                                                                                   "",
                                                                                   () => SetFullName ()) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Checked
                                                                                  },
                                                                              _miMultiSelect =
                                                                                  new MenuItem (
                                                                                   "_Multi Select",
                                                                                   "",
                                                                                   () => SetMultiSelect ()) {
                                                                                      Checked = true,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Checked
                                                                                  }
                                                                          }),
                                                      new (
                                                           "_Style",
                                                           new MenuItem[] {
                                                                              _miShowLines =
                                                                                  new MenuItem (
                                                                                   "_Show Lines",
                                                                                   "",
                                                                                   () => ShowLines ()) {
                                                                                      Checked = true,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Checked
                                                                                  },
                                                                              null /*separator*/,
                                                                              _miPlusMinus =
                                                                                  new MenuItem (
                                                                                   "_Plus Minus Symbols",
                                                                                   "+ -",
                                                                                   () => SetExpandableSymbols (
                                                                                    (Rune)'+',
                                                                                    (Rune)'-')) {
                                                                                      Checked = true,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Radio
                                                                                  },
                                                                              _miArrowSymbols =
                                                                                  new MenuItem (
                                                                                   "_Arrow Symbols",
                                                                                   "> v",
                                                                                   () => SetExpandableSymbols (
                                                                                    (Rune)'>',
                                                                                    (Rune)'v')) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Radio
                                                                                  },
                                                                              _miNoSymbols =
                                                                                  new MenuItem (
                                                                                   "_No Symbols",
                                                                                   "",
                                                                                   () => SetExpandableSymbols (
                                                                                    default,
                                                                                    null)) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Radio
                                                                                  },
                                                                              null /*separator*/,
                                                                              _miColoredSymbols =
                                                                                  new MenuItem (
                                                                                   "_Colored Symbols",
                                                                                   "",
                                                                                   () =>
                                                                                       ShowColoredExpandableSymbols ()) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Checked
                                                                                  },
                                                                              _miInvertSymbols =
                                                                                  new MenuItem (
                                                                                   "_Invert Symbols",
                                                                                   "",
                                                                                   () => InvertExpandableSymbols ()) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Checked
                                                                                  },
                                                                              null /*separator*/,
                                                                              _miBasicIcons =
                                                                                  new MenuItem (
                                                                                   "_Basic Icons",
                                                                                   null,
                                                                                   SetNoIcons) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Radio
                                                                                  },
                                                                              _miUnicodeIcons =
                                                                                  new MenuItem (
                                                                                   "_Unicode Icons",
                                                                                   null,
                                                                                   SetUnicodeIcons) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Radio
                                                                                  },
                                                                              _miNerdIcons =
                                                                                  new MenuItem (
                                                                                   "_Nerd Icons",
                                                                                   null,
                                                                                   SetNerdIcons) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Radio
                                                                                  },
                                                                              null /*separator*/,
                                                                              _miLeaveLastRow =
                                                                                  new MenuItem (
                                                                                   "_Leave Last Row",
                                                                                   "",
                                                                                   () => SetLeaveLastRow ()) {
                                                                                      Checked = true,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Checked
                                                                                  },
                                                                              _miHighlightModelTextOnly =
                                                                                  new MenuItem (
                                                                                   "_Highlight Model Text Only",
                                                                                   "",
                                                                                   () =>
                                                                                       SetCheckHighlightModelTextOnly ()) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Checked
                                                                                  },
                                                                              null /*separator*/,
                                                                              _miCustomColors =
                                                                                  new MenuItem (
                                                                                   "C_ustom Colors Hidden Files",
                                                                                   "Yellow/Red",
                                                                                   () => SetCustomColors ()) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Checked
                                                                                  },
                                                                              null /*separator*/,
                                                                              _miCursor =
                                                                                  new MenuItem (
                                                                                   "Curs_or (MultiSelect only)",
                                                                                   "",
                                                                                   () => SetCursor ()) {
                                                                                      Checked = false,
                                                                                      CheckType = MenuItemCheckStyle
                                                                                          .Checked
                                                                                  }
                                                                          })
                                                  });
        Application.Top.Add (menu);

        _treeViewFiles = new TreeView<IFileSystemInfo> () {
                                                              X = 0,
                                                              Y = 0,
                                                              Width = Dim.Percent (50),
                                                              Height = Dim.Fill ()
                                                          };
        _treeViewFiles.DrawLine += TreeViewFiles_DrawLine;

        _detailsFrame = new DetailsFrame (_iconProvider) {
                                                             X = Pos.Right (_treeViewFiles),
                                                             Y = 0,
                                                             Width = Dim.Fill (),
                                                             Height = Dim.Fill ()
                                                         };

        Win.Add (_detailsFrame);
        _treeViewFiles.MouseClick += TreeViewFiles_MouseClick;
        _treeViewFiles.KeyDown += TreeViewFiles_KeyPress;
        _treeViewFiles.SelectionChanged += TreeViewFiles_SelectionChanged;

        SetupFileTree ();

        Win.Add (_treeViewFiles);
        _treeViewFiles.GoToFirst ();
        _treeViewFiles.Expand ();

        SetupScrollBar ();

        _treeViewFiles.SetFocus ();

        UpdateIconCheckedness ();
    }

    void SetNoIcons () {
        _iconProvider.UseUnicodeCharacters = false;
        _iconProvider.UseNerdIcons = false;
        UpdateIconCheckedness ();
    }

    void SetUnicodeIcons () {
        _iconProvider.UseUnicodeCharacters = true;
        UpdateIconCheckedness ();
    }

    void SetNerdIcons () {
        _iconProvider.UseNerdIcons = true;
        UpdateIconCheckedness ();
    }

    void UpdateIconCheckedness () {
        _miBasicIcons.Checked = !_iconProvider.UseNerdIcons && !_iconProvider.UseUnicodeCharacters;
        _miUnicodeIcons.Checked = _iconProvider.UseUnicodeCharacters;
        _miNerdIcons.Checked = _iconProvider.UseNerdIcons;
        _treeViewFiles.SetNeedsDisplay ();
    }

    void TreeViewFiles_SelectionChanged (object sender, SelectionChangedEventArgs<IFileSystemInfo> e) =>
        ShowPropertiesOf (e.NewValue);

    void TreeViewFiles_DrawLine (object sender, DrawTreeViewLineEventArgs<IFileSystemInfo> e) {
        // Render directory icons in yellow
        if (e.Model is IDirectoryInfo d) {
            if (_iconProvider.UseNerdIcons || _iconProvider.UseUnicodeCharacters) {
                if (e.IndexOfModelText > 0 && e.IndexOfModelText < e.RuneCells.Count) {
                    var cell = e.RuneCells[e.IndexOfModelText];
                    cell.ColorScheme = new ColorScheme (
                                                        new Attribute (
                                                                       Color.BrightYellow,
                                                                       cell.ColorScheme.Normal.Background)
                                                       );
                }
            }
        }
    }

    void TreeViewFiles_KeyPress (object sender, Key obj) {
        if (obj.KeyCode == (KeyCode.R | KeyCode.CtrlMask)) {
            var selected = _treeViewFiles.SelectedObject;

            // nothing is selected
            if (selected == null) {
                return;
            }

            int? location = _treeViewFiles.GetObjectRow (selected);

            //selected object is offscreen or somehow not found
            if (location == null || location < 0 || location > _treeViewFiles.Frame.Height) {
                return;
            }

            ShowContextMenu (
                             new Point (
                                        5 + _treeViewFiles.Frame.X,
                                        location.Value + _treeViewFiles.Frame.Y + 2),
                             selected);
        }
    }

    void TreeViewFiles_MouseClick (object sender, MouseEventEventArgs obj) {
        // if user right clicks
        if (obj.MouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)) {
            var rightClicked = _treeViewFiles.GetObjectOnRow (obj.MouseEvent.Y);

            // nothing was clicked
            if (rightClicked == null) {
                return;
            }

            ShowContextMenu (
                             new Point (
                                        obj.MouseEvent.X + _treeViewFiles.Frame.X,
                                        obj.MouseEvent.Y + _treeViewFiles.Frame.Y + 2),
                             rightClicked);
        }
    }

    void ShowContextMenu (Point screenPoint, IFileSystemInfo forObject) {
        var menu = new ContextMenu ();
        menu.Position = screenPoint;

        menu.MenuItems =
            new MenuBarItem (new[] { new MenuItem ("Properties", null, () => ShowPropertiesOf (forObject)) });

        Application.Invoke (menu.Show);
    }

    class DetailsFrame : FrameView {
        IFileSystemInfo fileInfo;
        FileSystemIconProvider _iconProvider;

        public DetailsFrame (FileSystemIconProvider iconProvider) {
            Title = "Details";
            Visible = true;
            CanFocus = true;
            _iconProvider = iconProvider;
        }

        public IFileSystemInfo FileInfo {
            get => fileInfo;
            set {
                fileInfo = value;
                StringBuilder sb = null;

                if (fileInfo is IFileInfo f) {
                    Title = $"{_iconProvider.GetIconWithOptionalSpace (f)}{f.Name}".Trim ();
                    sb = new StringBuilder ();
                    sb.AppendLine ($"Path:\n {f.FullName}\n");
                    sb.AppendLine ($"Size:\n {f.Length:N0} bytes\n");
                    sb.AppendLine ($"Modified:\n {f.LastWriteTime}\n");
                    sb.AppendLine ($"Created:\n {f.CreationTime}");
                }

                if (fileInfo is IDirectoryInfo dir) {
                    Title = $"{_iconProvider.GetIconWithOptionalSpace (dir)}{dir.Name}".Trim ();
                    sb = new StringBuilder ();
                    sb.AppendLine ($"Path:\n {dir?.FullName}\n");
                    sb.AppendLine ($"Modified:\n {dir.LastWriteTime}\n");
                    sb.AppendLine ($"Created:\n {dir.CreationTime}\n");
                }

                Text = sb.ToString ();
            }
        }
    }

    void ShowPropertiesOf (IFileSystemInfo fileSystemInfo) => _detailsFrame.FileInfo = fileSystemInfo;

    void SetupScrollBar () {
        // When using scroll bar leave the last row of the control free (for over-rendering with scroll bar)
        _treeViewFiles.Style.LeaveLastRow = true;

        var scrollBar = new ScrollBarView (_treeViewFiles, true);

        scrollBar.ChangedPosition += (s, e) => {
            _treeViewFiles.ScrollOffsetVertical = scrollBar.Position;
            if (_treeViewFiles.ScrollOffsetVertical != scrollBar.Position) {
                scrollBar.Position = _treeViewFiles.ScrollOffsetVertical;
            }

            _treeViewFiles.SetNeedsDisplay ();
        };

        scrollBar.OtherScrollBarView.ChangedPosition += (s, e) => {
            _treeViewFiles.ScrollOffsetHorizontal = scrollBar.OtherScrollBarView.Position;
            if (_treeViewFiles.ScrollOffsetHorizontal != scrollBar.OtherScrollBarView.Position) {
                scrollBar.OtherScrollBarView.Position = _treeViewFiles.ScrollOffsetHorizontal;
            }

            _treeViewFiles.SetNeedsDisplay ();
        };

        _treeViewFiles.DrawContent += (s, e) => {
            scrollBar.Size = _treeViewFiles.ContentHeight;
            scrollBar.Position = _treeViewFiles.ScrollOffsetVertical;
            scrollBar.OtherScrollBarView.Size = _treeViewFiles.GetContentWidth (true);
            scrollBar.OtherScrollBarView.Position = _treeViewFiles.ScrollOffsetHorizontal;
            scrollBar.Refresh ();
        };
    }

    void SetupFileTree () {
        // setup how to build tree
        var fs = new FileSystem ();
        var rootDirs = DriveInfo.GetDrives ().Select (d => fs.DirectoryInfo.New (d.RootDirectory.FullName));
        _treeViewFiles.TreeBuilder = new FileSystemTreeBuilder ();
        _treeViewFiles.AddObjects (rootDirs);

        // Determines how to represent objects as strings on the screen
        _treeViewFiles.AspectGetter = AspectGetter;

        _iconProvider.IsOpenGetter = _treeViewFiles.IsExpanded;
    }

    string AspectGetter (IFileSystemInfo f) => (_iconProvider.GetIconWithOptionalSpace (f) + f.Name).Trim ();

    void ShowLines () {
        _miShowLines.Checked = !_miShowLines.Checked;

        _treeViewFiles.Style.ShowBranchLines = (bool)_miShowLines.Checked;
        _treeViewFiles.SetNeedsDisplay ();
    }

    void SetExpandableSymbols (Rune expand, Rune? collapse) {
        _miPlusMinus.Checked = expand.Value == '+';
        _miArrowSymbols.Checked = expand.Value == '>';
        _miNoSymbols.Checked = expand.Value == default;

        _treeViewFiles.Style.ExpandableSymbol = expand;
        _treeViewFiles.Style.CollapseableSymbol = collapse;
        _treeViewFiles.SetNeedsDisplay ();
    }

    void ShowColoredExpandableSymbols () {
        _miColoredSymbols.Checked = !_miColoredSymbols.Checked;

        _treeViewFiles.Style.ColorExpandSymbol = (bool)_miColoredSymbols.Checked;
        _treeViewFiles.SetNeedsDisplay ();
    }

    void InvertExpandableSymbols () {
        _miInvertSymbols.Checked = !_miInvertSymbols.Checked;

        _treeViewFiles.Style.InvertExpandSymbolColors = (bool)_miInvertSymbols.Checked;
        _treeViewFiles.SetNeedsDisplay ();
    }

    void SetFullName () {
        _miFullPaths.Checked = !_miFullPaths.Checked;

        if (_miFullPaths.Checked == true) {
            _treeViewFiles.AspectGetter = (f) => f.FullName;
        } else {
            _treeViewFiles.AspectGetter = (f) => f.Name;
        }

        _treeViewFiles.SetNeedsDisplay ();
    }

    void SetLeaveLastRow () {
        _miLeaveLastRow.Checked = !_miLeaveLastRow.Checked;
        _treeViewFiles.Style.LeaveLastRow = (bool)_miLeaveLastRow.Checked;
    }

    void SetCursor () {
        _miCursor.Checked = !_miCursor.Checked;
        _treeViewFiles.DesiredCursorVisibility =
            _miCursor.Checked == true ? CursorVisibility.Default : CursorVisibility.Invisible;
    }

    void SetMultiSelect () {
        _miMultiSelect.Checked = !_miMultiSelect.Checked;
        _treeViewFiles.MultiSelect = (bool)_miMultiSelect.Checked;
    }

    void SetCustomColors () {
        _miCustomColors.Checked = !_miCustomColors.Checked;

        if (_miCustomColors.Checked == true) {
            _treeViewFiles.ColorGetter = (m) => {
                if (m is IDirectoryInfo && m.Attributes.HasFlag (FileAttributes.Hidden)) {
                    return new ColorScheme {
                                               Focus = new Attribute (
                                                                      Color.BrightRed,
                                                                      _treeViewFiles.ColorScheme.Focus.Background),
                                               Normal = new Attribute (
                                                                       Color.BrightYellow,
                                                                       _treeViewFiles.ColorScheme.Normal.Background)
                                           };

                    ;
                }

                if (m is IFileInfo && m.Attributes.HasFlag (FileAttributes.Hidden)) {
                    return new ColorScheme {
                                               Focus = new Attribute (
                                                                      Color.BrightRed,
                                                                      _treeViewFiles.ColorScheme.Focus.Background),
                                               Normal = new Attribute (
                                                                       Color.BrightYellow,
                                                                       _treeViewFiles.ColorScheme.Normal.Background)
                                           };

                    ;
                }

                return null;
            };
        } else {
            _treeViewFiles.ColorGetter = null;
        }

        _treeViewFiles.SetNeedsDisplay ();
    }

    void SetCheckHighlightModelTextOnly () {
        _treeViewFiles.Style.HighlightModelTextOnly = !_treeViewFiles.Style.HighlightModelTextOnly;
        _miHighlightModelTextOnly.Checked = _treeViewFiles.Style.HighlightModelTextOnly;
        _treeViewFiles.SetNeedsDisplay ();
    }

    void Quit () => Application.RequestStop ();
}
