using System.IO.Abstractions;
using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("File System Explorer", "Hierarchical file system explorer demonstrating TreeView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
[ScenarioCategory ("Files and IO")]
public class TreeViewFileSystem : Scenario
{
    private readonly FileSystemIconProvider _iconProvider = new ();
    private DetailsFrame _detailsFrame;
    private MenuItem _miArrowSymbols;
    private MenuItem _miBasicIcons;
    private MenuItem _miColoredSymbols;
    private MenuItem _miCursor;
    private MenuItem _miCustomColors;
    private MenuItem _miFullPaths;
    private MenuItem _miHighlightModelTextOnly;
    private MenuItem _miInvertSymbols;
    private MenuItem _miLeaveLastRow;
    private MenuItem _miMultiSelect;
    private MenuItem _miNerdIcons;
    private MenuItem _miNoSymbols;
    private MenuItem _miPlusMinus;
    private MenuItem _miShowLines;
    private MenuItem _miUnicodeIcons;

    /// <summary>A tree view where nodes are files and folders</summary>
    private TreeView<IFileSystemInfo> _treeViewFiles;

    public override void Main ()
    {
        Application.Init ();

        var win = new Window
        {
            Title = GetName (),
            Y = 1, // menu
            Height = Dim.Fill ()
        };
        var top = new Toplevel ();

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new (
                              "_Quit",
                              $"{Application.QuitKey}",
                              () => Quit ()
                             )
                     }
                    ),
                new (
                     "_View",
                     new []
                     {
                         _miFullPaths =
                             new ("_Full Paths", "", () => SetFullName ())
                             {
                                 Checked = false, CheckType = MenuItemCheckStyle.Checked
                             },
                         _miMultiSelect = new (
                                               "_Multi Select",
                                               "",
                                               () => SetMultiSelect ()
                                              )
                         {
                             Checked = true,
                             CheckType = MenuItemCheckStyle
                                 .Checked
                         }
                     }
                    ),
                new (
                     "_Style",
                     new []
                     {
                         _miShowLines =
                             new ("_Show Lines", "", () => ShowLines ())
                             {
                                 Checked = true, CheckType = MenuItemCheckStyle.Checked
                             },
                         null /*separator*/,
                         _miPlusMinus =
                             new (
                                  "_Plus Minus Symbols",
                                  "+ -",
                                  () => SetExpandableSymbols (
                                                              (Rune)'+',
                                                              (Rune)'-'
                                                             )
                                 ) { Checked = true, CheckType = MenuItemCheckStyle.Radio },
                         _miArrowSymbols =
                             new (
                                  "_Arrow Symbols",
                                  "> v",
                                  () => SetExpandableSymbols (
                                                              (Rune)'>',
                                                              (Rune)'v'
                                                             )
                                 ) { Checked = false, CheckType = MenuItemCheckStyle.Radio },
                         _miNoSymbols =
                             new (
                                  "_No Symbols",
                                  "",
                                  () => SetExpandableSymbols (
                                                              default (Rune),
                                                              null
                                                             )
                                 ) { Checked = false, CheckType = MenuItemCheckStyle.Radio },
                         null /*separator*/,
                         _miColoredSymbols =
                             new (
                                  "_Colored Symbols",
                                  "",
                                  () => ShowColoredExpandableSymbols ()
                                 ) { Checked = false, CheckType = MenuItemCheckStyle.Checked },
                         _miInvertSymbols =
                             new (
                                  "_Invert Symbols",
                                  "",
                                  () => InvertExpandableSymbols ()
                                 ) { Checked = false, CheckType = MenuItemCheckStyle.Checked },
                         null /*separator*/,
                         _miBasicIcons =
                             new ("_Basic Icons", null, SetNoIcons)
                             {
                                 Checked = false, CheckType = MenuItemCheckStyle.Radio
                             },
                         _miUnicodeIcons =
                             new ("_Unicode Icons", null, SetUnicodeIcons)
                             {
                                 Checked = false, CheckType = MenuItemCheckStyle.Radio
                             },
                         _miNerdIcons =
                             new ("_Nerd Icons", null, SetNerdIcons)
                             {
                                 Checked = false, CheckType = MenuItemCheckStyle.Radio
                             },
                         null /*separator*/,
                         _miLeaveLastRow =
                             new (
                                  "_Leave Last Row",
                                  "",
                                  () => SetLeaveLastRow ()
                                 ) { Checked = true, CheckType = MenuItemCheckStyle.Checked },
                         _miHighlightModelTextOnly =
                             new (
                                  "_Highlight Model Text Only",
                                  "",
                                  () => SetCheckHighlightModelTextOnly ()
                                 ) { Checked = false, CheckType = MenuItemCheckStyle.Checked },
                         null /*separator*/,
                         _miCustomColors =
                             new (
                                  "C_ustom Colors Hidden Files",
                                  "Yellow/Red",
                                  () => SetCustomColors ()
                                 ) { Checked = false, CheckType = MenuItemCheckStyle.Checked },
                         null /*separator*/,
                         _miCursor = new (
                                          "Curs_or (MultiSelect only)",
                                          "",
                                          () => SetCursor ()
                                         ) { Checked = false, CheckType = MenuItemCheckStyle.Checked }
                     }
                    )
            ]
        };
        top.Add (menu);

        _treeViewFiles = new () { X = 0, Y = 0, Width = Dim.Percent (50), Height = Dim.Fill () };
        _treeViewFiles.DrawLine += TreeViewFiles_DrawLine;

        _treeViewFiles.VerticalScrollBar.AutoShow = false;

        _detailsFrame = new (_iconProvider)
        {
            X = Pos.Right (_treeViewFiles), Y = 0, Width = Dim.Fill (), Height = Dim.Fill ()
        };

        win.Add (_detailsFrame);
        _treeViewFiles.MouseClick += TreeViewFiles_MouseClick;
        _treeViewFiles.KeyDown += TreeViewFiles_KeyPress;
        _treeViewFiles.SelectionChanged += TreeViewFiles_SelectionChanged;

        SetupFileTree ();

        win.Add (_treeViewFiles);
        top.Add (win);
        _treeViewFiles.GoToFirst ();
        _treeViewFiles.Expand ();

        //SetupScrollBar ();

        _treeViewFiles.SetFocus ();

        UpdateIconCheckedness ();

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
    }

    private string AspectGetter (IFileSystemInfo f) { return (_iconProvider.GetIconWithOptionalSpace (f) + f.Name).Trim (); }

    private void InvertExpandableSymbols ()
    {
        _miInvertSymbols.Checked = !_miInvertSymbols.Checked;

        _treeViewFiles.Style.InvertExpandSymbolColors = (bool)_miInvertSymbols.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void Quit () { Application.RequestStop (); }

    private void SetCheckHighlightModelTextOnly ()
    {
        _treeViewFiles.Style.HighlightModelTextOnly = !_treeViewFiles.Style.HighlightModelTextOnly;
        _miHighlightModelTextOnly.Checked = _treeViewFiles.Style.HighlightModelTextOnly;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void SetCursor ()
    {
        _miCursor.Checked = !_miCursor.Checked;

        _treeViewFiles.CursorVisibility =
            _miCursor.Checked == true ? CursorVisibility.Default : CursorVisibility.Invisible;
    }

    private void SetCustomColors ()
    {
        _miCustomColors.Checked = !_miCustomColors.Checked;

        if (_miCustomColors.Checked == true)
        {
            _treeViewFiles.ColorGetter = m =>
                                         {
                                             if (m is IDirectoryInfo && m.Attributes.HasFlag (FileAttributes.Hidden))
                                             {
                                                 return new ()
                                                 {
                                                     Focus = new (
                                                                  Color.BrightRed,
                                                                  _treeViewFiles.GetAttributeForRole (VisualRole.Focus).Background
                                                                 ),
                                                     Normal = new (
                                                                   Color.BrightYellow,
                                                                   _treeViewFiles.GetAttributeForRole (VisualRole.Normal).Background
                                                                  )
                                                 };

                                                 ;
                                             }

                                             if (m is IFileInfo && m.Attributes.HasFlag (FileAttributes.Hidden))
                                             {
                                                 return new ()
                                                 {
                                                     Focus = new (
                                                                  Color.BrightRed,
                                                                  _treeViewFiles.GetAttributeForRole (VisualRole.Focus).Background
                                                                 ),
                                                     Normal = new (
                                                                   Color.BrightYellow,
                                                                   _treeViewFiles.GetAttributeForRole (VisualRole.Normal).Background
                                                                  )
                                                 };

                                                 ;
                                             }

                                             return null;
                                         };
        }
        else
        {
            _treeViewFiles.ColorGetter = null;
        }

        _treeViewFiles.SetNeedsDraw ();
    }

    private void SetExpandableSymbols (Rune expand, Rune? collapse)
    {
        _miPlusMinus.Checked = expand.Value == '+';
        _miArrowSymbols.Checked = expand.Value == '>';
        _miNoSymbols.Checked = expand.Value == default (int);

        _treeViewFiles.Style.ExpandableSymbol = expand;
        _treeViewFiles.Style.CollapseableSymbol = collapse;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void SetFullName ()
    {
        _miFullPaths.Checked = !_miFullPaths.Checked;

        if (_miFullPaths.Checked == true)
        {
            _treeViewFiles.AspectGetter = f => f.FullName;
        }
        else
        {
            _treeViewFiles.AspectGetter = f => f.Name;
        }

        _treeViewFiles.SetNeedsDraw ();
    }

    private void SetLeaveLastRow ()
    {
        _miLeaveLastRow.Checked = !_miLeaveLastRow.Checked;
        _treeViewFiles.Style.LeaveLastRow = (bool)_miLeaveLastRow.Checked;
    }

    private void SetMultiSelect ()
    {
        _miMultiSelect.Checked = !_miMultiSelect.Checked;
        _treeViewFiles.MultiSelect = (bool)_miMultiSelect.Checked;
    }

    private void SetNerdIcons ()
    {
        _iconProvider.UseNerdIcons = true;
        UpdateIconCheckedness ();
    }

    private void SetNoIcons ()
    {
        _iconProvider.UseUnicodeCharacters = false;
        _iconProvider.UseNerdIcons = false;
        UpdateIconCheckedness ();
    }

    private void SetUnicodeIcons ()
    {
        _iconProvider.UseUnicodeCharacters = true;
        UpdateIconCheckedness ();
    }

    private void SetupFileTree ()
    {
        // setup how to build tree
        var fs = new FileSystem ();

        IEnumerable<IDirectoryInfo> rootDirs =
            DriveInfo.GetDrives ().Select (d => fs.DirectoryInfo.New (d.RootDirectory.FullName));
        _treeViewFiles.TreeBuilder = new FileSystemTreeBuilder ();
        _treeViewFiles.AddObjects (rootDirs);

        // Determines how to represent objects as strings on the screen
        _treeViewFiles.AspectGetter = AspectGetter;

        _iconProvider.IsOpenGetter = _treeViewFiles.IsExpanded;
    }

    //private void SetupScrollBar ()
    //{
    //    // When using scroll bar leave the last row of the control free (for over-rendering with scroll bar)
    //    _treeViewFiles.Style.LeaveLastRow = true;

    //    var scrollBar = new ScrollBarView (_treeViewFiles, true);

    //    scrollBar.ChangedPosition += (s, e) =>
    //                                 {
    //                                     _treeViewFiles.ScrollOffsetVertical = scrollBar.Position;

    //                                     if (_treeViewFiles.ScrollOffsetVertical != scrollBar.Position)
    //                                     {
    //                                         scrollBar.Position = _treeViewFiles.ScrollOffsetVertical;
    //                                     }

    //                                     _treeViewFiles.SetNeedsDraw ();
    //                                 };

    //    scrollBar.OtherScrollBarView.ChangedPosition += (s, e) =>
    //                                                    {
    //                                                        _treeViewFiles.ScrollOffsetHorizontal = scrollBar.OtherScrollBarView.Position;

    //                                                        if (_treeViewFiles.ScrollOffsetHorizontal != scrollBar.OtherScrollBarView.Position)
    //                                                        {
    //                                                            scrollBar.OtherScrollBarView.Position = _treeViewFiles.ScrollOffsetHorizontal;
    //                                                        }

    //                                                        _treeViewFiles.SetNeedsDraw ();
    //                                                    };

    //    _treeViewFiles.DrawingContent += (s, e) =>
    //                                  {
    //                                      scrollBar.Size = _treeViewFiles.ContentHeight;
    //                                      scrollBar.Position = _treeViewFiles.ScrollOffsetVertical;
    //                                      scrollBar.OtherScrollBarView.Size = _treeViewFiles.GetContentWidth (true);
    //                                      scrollBar.OtherScrollBarView.Position = _treeViewFiles.ScrollOffsetHorizontal;
    //                                      scrollBar.Refresh ();
    //                                  };
    //}

    private void ShowColoredExpandableSymbols ()
    {
        _miColoredSymbols.Checked = !_miColoredSymbols.Checked;

        _treeViewFiles.Style.ColorExpandSymbol = (bool)_miColoredSymbols.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void ShowContextMenu (Point screenPoint, IFileSystemInfo forObject)
    {
        PopoverMenu? contextMenu = new ([new ("Properties", $"Show {forObject.Name} properties", () => ShowPropertiesOf (forObject))]);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        Application.Popover?.Register (contextMenu);

        Application.Invoke (() => contextMenu?.MakeVisible (screenPoint));
    }

    private void ShowLines ()
    {
        _miShowLines.Checked = !_miShowLines.Checked;

        _treeViewFiles.Style.ShowBranchLines = (bool)_miShowLines.Checked!;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void ShowPropertiesOf (IFileSystemInfo fileSystemInfo) { _detailsFrame.FileInfo = fileSystemInfo; }

    private void TreeViewFiles_DrawLine (object sender, DrawTreeViewLineEventArgs<IFileSystemInfo> e)
    {
        // Render directory icons in yellow
        if (e.Model is IDirectoryInfo d)
        {
            if (_iconProvider.UseNerdIcons || _iconProvider.UseUnicodeCharacters)
            {
                if (e.IndexOfModelText > 0 && e.IndexOfModelText < e.Cells.Count)
                {
                    Cell cell = e.Cells [e.IndexOfModelText];

                    cell.Attribute = new Attribute (
                                                    Color.BrightYellow,
                                                    cell.Attribute!.Value.Background,
                                                    cell.Attribute!.Value.Style
                                                   );
                }
            }
        }
    }

    private void TreeViewFiles_KeyPress (object sender, Key obj)
    {
        if (obj.KeyCode == (KeyCode.R | KeyCode.CtrlMask))
        {
            IFileSystemInfo selected = _treeViewFiles.SelectedObject;

            // nothing is selected
            if (selected == null)
            {
                return;
            }

            int? location = _treeViewFiles.GetObjectRow (selected);

            //selected object is offscreen or somehow not found
            if (location == null || location < 0 || location > _treeViewFiles.Frame.Height)
            {
                return;
            }

            ShowContextMenu (
                             new (
                                  5 + _treeViewFiles.Frame.X,
                                  location.Value + _treeViewFiles.Frame.Y + 2
                                 ),
                             selected
                            );
        }
    }

    private void TreeViewFiles_MouseClick (object sender, MouseEventArgs obj)
    {
        // if user right clicks
        if (obj.Flags.HasFlag (MouseFlags.Button3Clicked))
        {
            IFileSystemInfo rightClicked = _treeViewFiles.GetObjectOnRow (obj.Position.Y);

            // nothing was clicked
            if (rightClicked == null)
            {
                return;
            }

            ShowContextMenu (
                             new (
                                  obj.Position.X + _treeViewFiles.Frame.X,
                                  obj.Position.Y + _treeViewFiles.Frame.Y + 2
                                 ),
                             rightClicked
                            );
        }
    }

    private void TreeViewFiles_SelectionChanged (object sender, SelectionChangedEventArgs<IFileSystemInfo> e) { ShowPropertiesOf (e.NewValue); }

    private void UpdateIconCheckedness ()
    {
        _miBasicIcons.Checked = !_iconProvider.UseNerdIcons && !_iconProvider.UseUnicodeCharacters;
        _miUnicodeIcons.Checked = _iconProvider.UseUnicodeCharacters;
        _miNerdIcons.Checked = _iconProvider.UseNerdIcons;
        _treeViewFiles.SetNeedsDraw ();
    }

    private class DetailsFrame : FrameView
    {
        private readonly FileSystemIconProvider _iconProvider;
        private IFileSystemInfo _fileInfo;

        public DetailsFrame (FileSystemIconProvider iconProvider)
        {
            Title = "Details";
            Visible = true;
            CanFocus = true;
            _iconProvider = iconProvider;
        }

        public IFileSystemInfo FileInfo
        {
            get => _fileInfo;
            set
            {
                _fileInfo = value;
                StringBuilder sb = null;

                if (_fileInfo is IFileInfo f)
                {
                    Title = $"{_iconProvider.GetIconWithOptionalSpace (f)}{f.Name}".Trim ();
                    sb = new ();
                    sb.AppendLine ($"Path:\n {f.FullName}\n");
                    sb.AppendLine ($"Size:\n {f.Length:N0} bytes\n");
                    sb.AppendLine ($"Modified:\n {f.LastWriteTime}\n");
                    sb.AppendLine ($"Created:\n {f.CreationTime}");
                }

                if (_fileInfo is IDirectoryInfo dir)
                {
                    Title = $"{_iconProvider.GetIconWithOptionalSpace (dir)}{dir.Name}".Trim ();
                    sb = new ();
                    sb.AppendLine ($"Path:\n {dir?.FullName}\n");
                    sb.AppendLine ($"Modified:\n {dir.LastWriteTime}\n");
                    sb.AppendLine ($"Created:\n {dir.CreationTime}\n");
                }

                Text = sb.ToString ();
            }
        }
    }
}
