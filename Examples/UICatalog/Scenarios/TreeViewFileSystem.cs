#nullable enable

using System.IO.Abstractions;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("File System Explorer", "Hierarchical file system explorer demonstrating TreeView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TreeView")]
[ScenarioCategory ("Files and IO")]
public class TreeViewFileSystem : Scenario
{
    private readonly FileSystemIconProvider _iconProvider = new ();
    private DetailsFrame? _detailsFrame;
    private CheckBox? _miArrowSymbolsCheckBox;
    private CheckBox? _miBasicIconsCheckBox;
    private CheckBox? _miColoredSymbolsCheckBox;
    private CheckBox? _miCursorCheckBox;
    private CheckBox? _miCustomColorsCheckBox;
    private CheckBox? _miFullPathsCheckBox;
    private CheckBox? _miHighlightModelTextOnlyCheckBox;
    private CheckBox? _miInvertSymbolsCheckBox;
    private CheckBox? _miLeaveLastRowCheckBox;
    private CheckBox? _miMultiSelectCheckBox;
    private CheckBox? _miNerdIconsCheckBox;
    private CheckBox? _miNoSymbolsCheckBox;
    private CheckBox? _miPlusMinusCheckBox;
    private CheckBox? _miShowLinesCheckBox;
    private CheckBox? _miUnicodeIconsCheckBox;

    /// <summary>A tree view where nodes are files and folders</summary>
    private TreeView<IFileSystemInfo>? _treeViewFiles;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ()
        {
            Title = GetName (),
            Y = 1, // menu
            Height = Dim.Fill ()
        };

        // MenuBar
        MenuBar menu = new ();

        _treeViewFiles = new () { X = 0, Y = Pos.Bottom (menu), Width = Dim.Percent (50), Height = Dim.Fill () };
        _treeViewFiles.DrawLine += TreeViewFiles_DrawLine;

        _treeViewFiles.VerticalScrollBar.AutoShow = false;

        _detailsFrame = new (_iconProvider)
        {
            X = Pos.Right (_treeViewFiles),
            Y = Pos.Top (_treeViewFiles),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        win.Add (_detailsFrame);
        _treeViewFiles.Activating += TreeViewFiles_Selecting;
        _treeViewFiles.KeyDown += TreeViewFiles_KeyPress;
        _treeViewFiles.SelectionChanged += TreeViewFiles_SelectionChanged;

        SetupFileTree ();

        // Setup menu checkboxes
        _miFullPathsCheckBox = new ()
        {
            Title = "_Full Paths"
        };
        _miFullPathsCheckBox.CheckedStateChanged += (_, _) => SetFullName ();

        _miMultiSelectCheckBox = new ()
        {
            Title = "_Multi Select",
            //CheckedState = CheckState.Checked
        };
        _miMultiSelectCheckBox.CheckedStateChanged += (_, _) => SetMultiSelect ();

        _miShowLinesCheckBox = new ()
        {
            Title = "_Show Lines",
            CheckedState = CheckState.Checked
        };
        _miShowLinesCheckBox.CheckedStateChanged += (_, _) => ShowLines ();

        _miPlusMinusCheckBox = new ()
        {
            Title = "_Plus Minus Symbols",
            CheckedState = CheckState.Checked
        };
        _miPlusMinusCheckBox.CheckedStateChanged += (_, _) => SetExpandableSymbols ((Rune)'+', (Rune)'-');

        _miArrowSymbolsCheckBox = new ()
        {
            Title = "_Arrow Symbols"
        };
        _miArrowSymbolsCheckBox.CheckedStateChanged += (_, _) => SetExpandableSymbols ((Rune)'>', (Rune)'v');

        _miNoSymbolsCheckBox = new ()
        {
            Title = "_No Symbols"
        };
        _miNoSymbolsCheckBox.CheckedStateChanged += (_, _) => SetExpandableSymbols (default (Rune), null);

        _miColoredSymbolsCheckBox = new ()
        {
            Title = "_Colored Symbols"
        };
        _miColoredSymbolsCheckBox.CheckedStateChanged += (_, _) => ShowColoredExpandableSymbols ();

        _miInvertSymbolsCheckBox = new ()
        {
            Title = "_Invert Symbols"
        };
        _miInvertSymbolsCheckBox.CheckedStateChanged += (_, _) => InvertExpandableSymbols ();

        _miBasicIconsCheckBox = new ()
        {
            Title = "_Basic Icons"
        };
        _miBasicIconsCheckBox.CheckedStateChanged += (_, _) => SetNoIcons ();

        _miUnicodeIconsCheckBox = new ()
        {
            Title = "_Unicode Icons"
        };
        _miUnicodeIconsCheckBox.CheckedStateChanged += (_, _) => SetUnicodeIcons ();

        _miNerdIconsCheckBox = new ()
        {
            Title = "_Nerd Icons"
        };
        _miNerdIconsCheckBox.CheckedStateChanged += (_, _) => SetNerdIcons ();

        _miLeaveLastRowCheckBox = new ()
        {
            Title = "_Leave Last Row",
            CheckedState = CheckState.Checked
        };
        _miLeaveLastRowCheckBox.CheckedStateChanged += (_, _) => SetLeaveLastRow ();

        _miHighlightModelTextOnlyCheckBox = new ()
        {
            Title = "_Highlight Model Text Only",
            CheckedState = CheckState.Checked
        };
        SetCheckHighlightModelTextOnly ();
        _miHighlightModelTextOnlyCheckBox.CheckedStateChanged += (_, _) => SetCheckHighlightModelTextOnly ();

        _miCustomColorsCheckBox = new ()
        {
            Title = "C_ustom Colors Hidden Files"
        };
        _miCustomColorsCheckBox.CheckedStateChanged += (_, _) => SetCustomColors ();

        _miCursorCheckBox = new ()
        {
            Title = "Curs_or",
            //CheckedState = CheckState.Checked
        };
        SetCursor ();
        _miCursorCheckBox.CheckedStateChanged += (_, _) => SetCursor ();

        menu.Add (
                  new MenuBarItem (
                                   Strings.menuFile,
                                   [
                                       new MenuItem
                                       {
                                           Title = Strings.cmdQuit,
                                           Key = Application.QuitKey,
                                           Action = Quit
                                       }
                                   ]
                                  )
                 );

        menu.Add (
                  new MenuBarItem (
                                   "_View",
                                   [
                                       new MenuItem
                                       {
                                           CommandView = _miFullPathsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miMultiSelectCheckBox
                                       }
                                   ]
                                  )
                 );

        menu.Add (
                  new MenuBarItem (
                                   "_Style",
                                   [
                                       new MenuItem
                                       {
                                           CommandView = _miShowLinesCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miPlusMinusCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miArrowSymbolsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miNoSymbolsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miColoredSymbolsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miInvertSymbolsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miBasicIconsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miUnicodeIconsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miNerdIconsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miLeaveLastRowCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miHighlightModelTextOnlyCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miCustomColorsCheckBox
                                       },
                                       new MenuItem
                                       {
                                           CommandView = _miCursorCheckBox
                                       }
                                   ]
                                  )
                 );

        win.Add (menu, _treeViewFiles);
        _treeViewFiles.GoToFirst ();
        _treeViewFiles.Expand ();

        _treeViewFiles.SetFocus ();

        UpdateIconCheckedness ();

        app.Run (win);
    }

    private string AspectGetter (IFileSystemInfo f) => (_iconProvider.GetIconWithOptionalSpace (f) + f.Name).Trim ();

    private void InvertExpandableSymbols ()
    {
        if (_treeViewFiles is null || _miInvertSymbolsCheckBox is null)
        {
            return;
        }

        _treeViewFiles.Style.InvertExpandSymbolColors = _miInvertSymbolsCheckBox.CheckedState == CheckState.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void Quit () { _treeViewFiles?.App?.RequestStop (); }

    private void SetCheckHighlightModelTextOnly ()
    {
        if (_treeViewFiles is null || _miHighlightModelTextOnlyCheckBox is null)
        {
            return;
        }

        _treeViewFiles.Style.HighlightModelTextOnly = _miHighlightModelTextOnlyCheckBox.CheckedState == CheckState.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void SetCursor ()
    {
        if (_treeViewFiles is null || _miCursorCheckBox is null)
        {
            return;
        }

        if (_miCursorCheckBox.CheckedState == CheckState.Checked)
        {
            // Provide a non-null position to enable the cursor
            _treeViewFiles.Cursor = _treeViewFiles.Cursor with { Position = Point.Empty, Style = CursorStyle.BlinkingBlock };
        }
        else
        {
            _treeViewFiles.Cursor = _treeViewFiles.Cursor with { Position = null };
        }
    }

    private void SetCustomColors ()
    {
        if (_treeViewFiles is null || _miCustomColorsCheckBox is null)
        {
            return;
        }

        if (_miCustomColorsCheckBox.CheckedState == CheckState.Checked)
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
        if (_treeViewFiles is null)
        {
            return;
        }

        if (_miPlusMinusCheckBox is not null)
        {
            _miPlusMinusCheckBox.CheckedState = expand.Value == '+' ? CheckState.Checked : CheckState.UnChecked;
        }

        if (_miArrowSymbolsCheckBox is not null)
        {
            _miArrowSymbolsCheckBox.CheckedState = expand.Value == '>' ? CheckState.Checked : CheckState.UnChecked;
        }

        if (_miNoSymbolsCheckBox is not null)
        {
            _miNoSymbolsCheckBox.CheckedState = expand.Value == default (int) ? CheckState.Checked : CheckState.UnChecked;
        }

        _treeViewFiles.Style.ExpandableSymbol = expand;
        _treeViewFiles.Style.CollapseableSymbol = collapse;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void SetFullName ()
    {
        if (_treeViewFiles is null || _miFullPathsCheckBox is null)
        {
            return;
        }

        if (_miFullPathsCheckBox.CheckedState == CheckState.Checked)
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
        if (_treeViewFiles is null || _miLeaveLastRowCheckBox is null)
        {
            return;
        }

        _treeViewFiles.Style.LeaveLastRow = _miLeaveLastRowCheckBox.CheckedState == CheckState.Checked;
    }

    private void SetMultiSelect ()
    {
        if (_treeViewFiles is null || _miMultiSelectCheckBox is null)
        {
            return;
        }

        _treeViewFiles.MultiSelect = _miMultiSelectCheckBox.CheckedState == CheckState.Checked;
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
        if (_treeViewFiles is null)
        {
            return;
        }

        // setup how to build tree
        FileSystem fs = new ();

        IEnumerable<IDirectoryInfo> rootDirs =
            DriveInfo.GetDrives ().Select (d => fs.DirectoryInfo.New (d.RootDirectory.FullName));
        _treeViewFiles.TreeBuilder = new FileSystemTreeBuilder ();
        _treeViewFiles.AddObjects (rootDirs);

        // Determines how to represent objects as strings on the screen
        _treeViewFiles.AspectGetter = AspectGetter;

        _iconProvider.IsOpenGetter = _treeViewFiles.IsExpanded;
    }

    private void ShowColoredExpandableSymbols ()
    {
        if (_treeViewFiles is null || _miColoredSymbolsCheckBox is null)
        {
            return;
        }

        _treeViewFiles.Style.ColorExpandSymbol = _miColoredSymbolsCheckBox.CheckedState == CheckState.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void ShowContextMenu (Point screenPoint, IFileSystemInfo forObject)
    {
        PopoverMenu contextMenu = new ([new ("Properties", $"Show {forObject.Name} properties", () => ShowPropertiesOf (forObject))]);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        _detailsFrame?.App?.Popover?.Register (contextMenu);

        _detailsFrame?.App?.Invoke (() => contextMenu.MakeVisible (screenPoint));
    }

    private void ShowLines ()
    {
        if (_treeViewFiles is null || _miShowLinesCheckBox is null)
        {
            return;
        }

        _treeViewFiles.Style.ShowBranchLines = _miShowLinesCheckBox.CheckedState == CheckState.Checked;
        _treeViewFiles.SetNeedsDraw ();
    }

    private void ShowPropertiesOf (IFileSystemInfo fileSystemInfo)
    {
        if (_detailsFrame is not null)
        {
            _detailsFrame.FileInfo = fileSystemInfo;
        }
    }

    private void TreeViewFiles_DrawLine (object? sender, DrawTreeViewLineEventArgs<IFileSystemInfo> e)
    {
        // Render directory icons in yellow
        if (e.Model is not IDirectoryInfo)
        {
            return;
        }

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

    private void TreeViewFiles_KeyPress (object? sender, Key obj)
    {
        if (_treeViewFiles is null)
        {
            return;
        }

        if (obj.KeyCode == (KeyCode.R | KeyCode.CtrlMask))
        {
            IFileSystemInfo? selected = _treeViewFiles.SelectedObject;

            // nothing is selected
            if (selected is null)
            {
                return;
            }

            int? location = _treeViewFiles.GetObjectRow (selected);

            //selected object is offscreen or somehow not found
            if (location is null || location < 0 || location > _treeViewFiles.Frame.Height)
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

    private void TreeViewFiles_Selecting (object? sender, CommandEventArgs e)
    {
        if (_treeViewFiles is null)
        {
            return;
        }

        // Only handle mouse clicks
        if (e.Context is not CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouse })
        {
            return;
        }

        // if user right clicks
        if (mouse.Flags.HasFlag (MouseFlags.RightButtonClicked))
        {
            IFileSystemInfo? rightClicked = _treeViewFiles.GetObjectOnRow (mouse.Position!.Value.Y);

            // nothing was clicked
            if (rightClicked is null)
            {
                return;
            }

            ShowContextMenu (
                             new (
                                  mouse.Position!.Value.X + _treeViewFiles.Frame.X,
                                  mouse.Position!.Value.Y + _treeViewFiles.Frame.Y + 2
                                 ),
                             rightClicked
                            );
        }
    }

    private void TreeViewFiles_SelectionChanged (object? sender, SelectionChangedEventArgs<IFileSystemInfo> e) { ShowPropertiesOf (e.NewValue); }

    private void UpdateIconCheckedness ()
    {
        if (_miBasicIconsCheckBox is not null)
        {
            _miBasicIconsCheckBox.CheckedState = !_iconProvider.UseNerdIcons && !_iconProvider.UseUnicodeCharacters
                                                     ? CheckState.Checked
                                                     : CheckState.UnChecked;
        }

        if (_miUnicodeIconsCheckBox is not null)
        {
            _miUnicodeIconsCheckBox.CheckedState = _iconProvider.UseUnicodeCharacters ? CheckState.Checked : CheckState.UnChecked;
        }

        if (_miNerdIconsCheckBox is not null)
        {
            _miNerdIconsCheckBox.CheckedState = _iconProvider.UseNerdIcons ? CheckState.Checked : CheckState.UnChecked;
        }

        _treeViewFiles?.SetNeedsDraw ();
    }

    private class DetailsFrame : FrameView
    {
        private readonly FileSystemIconProvider _iconProvider;
        private IFileSystemInfo? _fileInfo;

        public DetailsFrame (FileSystemIconProvider iconProvider)
        {
            Title = "Details";
            base.Visible = true;
            CanFocus = true;
            _iconProvider = iconProvider;
        }

        public IFileSystemInfo? FileInfo
        {
            get => _fileInfo;
            set
            {
                _fileInfo = value;
                StringBuilder? sb = null;

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
                    sb.AppendLine ($"Path:\n {dir.FullName}\n");
                    sb.AppendLine ($"Modified:\n {dir.LastWriteTime}\n");
                    sb.AppendLine ($"Created:\n {dir.CreationTime}\n");
                }

                Text = sb?.ToString () ?? string.Empty;
            }
        }
    }
}
