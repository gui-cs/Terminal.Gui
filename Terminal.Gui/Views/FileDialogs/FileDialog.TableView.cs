using System.IO.Abstractions;

namespace Terminal.Gui.Views;

public partial class FileDialog
{
    private void TableViewHandleCommandNotBound (object? sender, CommandEventArgs e)
    {
        if (e.Context!.Command != Command.Context)
        {
            return;
        }

        if (e.Context.Binding is MouseBinding { MouseEvent: { } mouse })
        {
            Point? clickedCell = _tableView.ScreenToCell (mouse.Position!.Value.X, mouse.Position!.Value.Y, out int? clickedCol);

            if (clickedCol is { })
            {
                // right click in a header
                ShowHeaderContextMenu (clickedCol.Value, mouse);
            }
            else if (clickedCell is { })
            {
                // right click in rest of table
                ShowCellContextMenu (clickedCell, mouse);
            }
        }

        if (e.Context.Binding is not KeyBinding)
        {
            return;
        }

        PopoverMenu? contextMenu = new ([
                                            new MenuItem (Strings.fdCtxNew, string.Empty, New),
                                            new MenuItem (Strings.fdCtxRename, string.Empty, () => Rename (App)),
                                            new MenuItem (Strings.fdCtxDelete, string.Empty, Delete)
                                        ]);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        App!.Popovers?.Register (contextMenu);

        Point pos = new (_tableView.FrameToScreen ().X + 15, _tableView.FrameToScreen ().Y + _tableView.SelectedRow + _tableView.GetHeaderHeight ());
        contextMenu?.MakeVisible (pos);
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (string.IsNullOrWhiteSpace (_feedback))
        {
            return true;
        }

        int feedbackWidth = _feedback.GetColumns ();
        int feedbackPadLeft = (Viewport.Width - feedbackWidth) / 2 - 1;

        feedbackPadLeft = Math.Min (Viewport.Width, feedbackPadLeft);
        feedbackPadLeft = Math.Max (0, feedbackPadLeft);

        int feedbackPadRight = Viewport.Width - (feedbackPadLeft + feedbackWidth + 2);
        feedbackPadRight = Math.Min (Viewport.Width, feedbackPadRight);
        feedbackPadRight = Math.Max (0, feedbackPadRight);

        Move (0, Viewport.Height / 2);

        SetAttribute (new Attribute (Color.Red, GetAttributeForRole (VisualRole.Normal).Background));
        AddStr (new string (' ', feedbackPadLeft));
        AddStr (_feedback);
        AddStr (new string (' ', feedbackPadRight));

        return true;
    }

    internal void ApplySort ()
    {
        FileSystemInfoStats [] stats = State?.Children ?? [];

        // This portion is never reordered (always .. at top then folders)
        IOrderedEnumerable<FileSystemInfoStats> forcedOrder = stats.OrderByDescending (f => f.IsParent).ThenBy (f => f.IsDir ? -1 : 100);

        // This portion is flexible based on the column clicked (e.g. alphabetical)
        IOrderedEnumerable<FileSystemInfoStats> ordered = _currentSortIsAsc
                                                              ? forcedOrder.ThenBy (f => FileDialogTableSource.GetRawColumnValue (_currentSortColumn, f))
                                                              : forcedOrder.ThenByDescending (f => FileDialogTableSource.GetRawColumnValue (_currentSortColumn,
                                                                                                  f));

        State?.Children = ordered.ToArray ();

        _tableView.Update ();
    }

    internal void SortColumn (int col, bool isAsc)
    {
        // set a sort order
        _currentSortColumn = col;
        _currentSortIsAsc = isAsc;

        ApplySort ();
    }

#if MENU_V1
    private void AllowedTypeMenuClicked (int idx)
    {
        IAllowedType allow = AllowedTypes [idx];

        for (var i = 0; i < AllowedTypes.Count; i++)
        {
            _allowedTypeMenuItems! [i].Checked = i == idx;
        }

        _allowedTypeMenu!.Title = allow.ToString ()!;

        CurrentFilter = allow;

        _tbPath.ClearAllSelection ();
        _tbPath.Autocomplete.ClearSuggestions ();

        State?.RefreshChildren ();
        WriteStateToTableView ();
    }
#endif

    private string AspectGetter (object o)
    {
        var fsi = (IFileSystemInfo)o;

        if (o is IDirectoryInfo dir && _treeRoots.ContainsKey (dir))
        {
            // Directory has a special name e.g. 'Pictures'
            return _treeRoots [dir];
        }

        return (Style.IconProvider.GetIconWithOptionalSpace (fsi) + fsi.Name).Trim ();
    }

    private void CellActivate (object? sender, CellActivatedEventArgs obj)
    {
        if (TryAcceptMulti ())
        {
            return;
        }

        FileSystemInfoStats stats = RowToStats (obj.Row);

        if (stats.FileSystemInfo is IDirectoryInfo d)
        {
            PushState (d, true);

            //if (d == State?.Directory || d.FullName == State?.Directory.FullName)
            //{
            //    FinishAccept ();
            //}

            return;
        }

        if (stats.FileSystemInfo is IFileInfo f)
        {
            Accept (f);
        }
    }

    private void ClearFeedback () => _feedback = null;

    private Scheme ColorGetter (CellColorGetterArgs args)
    {
        FileSystemInfoStats stats = RowToStats (args.RowIndex);

        if (!Style.UseColors)
        {
            return _tableView.GetScheme ();
        }

        Color color = Style.ColorProvider.GetColor (stats.FileSystemInfo!) ?? new Color (Color.White);
        var black = new Color (Color.Black);

        // TODO: Add some kind of cache for this
        return new Scheme
        {
            Normal = new Attribute (color, black),
            HotNormal = new Attribute (color, black),
            Focus = new Attribute (black, color),
            HotFocus = new Attribute (black, color)
        };
    }

    private string GetProposedNewSortOrder (int clickedCol, out bool isAsc)
    {
        // work out new sort order
        if (_currentSortColumn == clickedCol && _currentSortIsAsc)
        {
            isAsc = false;

            return string.Format (Strings.fdCtxSortDesc, _tableView.Table!.ColumnNames [clickedCol]);
        }

        isAsc = true;

        return string.Format (Strings.fdCtxSortAsc, _tableView.Table!.ColumnNames [clickedCol]);
    }

    private void HideColumn (int clickedCol)
    {
        ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (clickedCol);
        style.Visible = false;
        _tableView.Update ();
    }

    private void OnTableViewActivating (object? sender, CommandEventArgs e)
    {
        // Only handle mouse clicks, not keyboard selections
        if (e.Context?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            return;
        }

        _tableView.ScreenToCell (mouse.Position!.Value.X, mouse.Position!.Value.Y, out int? clickedCol);

        if (clickedCol is null || !mouse.Flags.FastHasFlags (MouseFlags.LeftButtonClicked))
        {
            return;
        }

        // left click in a header
        SortColumn (clickedCol.Value);
    }

    private FileSystemInfoStats RowToStats (int rowIndex) => State?.Children [rowIndex]!;

    private void ShowCellContextMenu (Point? clickedCell, Mouse e)
    {
        if (clickedCell is null)
        {
            return;
        }

        PopoverMenu? contextMenu = new ([
                                            new MenuItem (Strings.fdCtxNew, string.Empty, New),
                                            new MenuItem (Strings.fdCtxRename, string.Empty, () => Rename (App)),
                                            new MenuItem (Strings.fdCtxDelete, string.Empty, Delete)
                                        ]);

        _tableView.SetSelection (clickedCell.Value.X, clickedCell.Value.Y, false);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        App!.Popovers?.Register (contextMenu);

        contextMenu?.MakeVisible (e.ScreenPosition);
    }

    private void ShowHeaderContextMenu (int clickedCol, Mouse e)
    {
        string sort = GetProposedNewSortOrder (clickedCol, out bool isAsc);

        PopoverMenu? contextMenu = new ([
                                            new MenuItem (string.Format (Strings.fdCtxHide, StripArrows (_tableView.Table!.ColumnNames [clickedCol])),
                                                          string.Empty,
                                                          () => HideColumn (clickedCol)),
                                            new MenuItem (StripArrows (sort), string.Empty, () => SortColumn (clickedCol, isAsc))
                                        ]);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        App!.Popovers?.Register (contextMenu);

        contextMenu?.MakeVisible (e.ScreenPosition);
    }

    private void SortColumn (int clickedCol)
    {
        GetProposedNewSortOrder (clickedCol, out bool isAsc);
        SortColumn (clickedCol, isAsc);

        _tableView.Table = new FileDialogTableSource (this, State, Style, _currentSortColumn, _currentSortIsAsc);
    }

    private static string StripArrows (string columnName) => columnName.Replace (" (▼)", string.Empty).Replace (" (▲)", string.Empty);

    private bool TableView_KeyDown (Key keyEvent)
    {
        if (keyEvent.KeyCode == KeyCode.Backspace)
        {
            return _history.Back ();
        }

        if (keyEvent.KeyCode == (KeyCode.ShiftMask | KeyCode.Backspace))
        {
            return _history.Forward ();
        }

        if (keyEvent.KeyCode == KeyCode.Delete)
        {
            Delete ();

            return true;
        }

        if (keyEvent.KeyCode == (KeyCode.CtrlMask | KeyCode.R))
        {
            Rename (App);

            return true;
        }

        if (keyEvent.KeyCode == (KeyCode.CtrlMask | KeyCode.N))
        {
            New ();

            return true;
        }

        return false;
    }

    private void TableView_SelectedCellChanged (object? sender, SelectedCellChangedEventArgs obj)
    {
        if (!_tableView.HasFocus || obj.NewRow == -1 || obj.Table.Rows == 0)
        {
            return;
        }

        if (_tableView.MultiSelect && _tableView.MultiSelectedRegions.Any ())
        {
            return;
        }

        FileSystemInfoStats stats = RowToStats (obj.NewRow);

        IFileSystemInfo? dest = stats.IsParent ? State!.Directory : stats.FileSystemInfo;

        try
        {
            _pushingState = true;

            SetPathToSelectedObject (dest);
            State!.Selected = stats;
            _tbPath.Autocomplete?.ClearSuggestions ();
        }
        finally
        {
            _pushingState = false;
        }
    }

    private void WriteStateToTableView ()
    {
        _tableView.Table = new FileDialogTableSource (this, State, Style, _currentSortColumn, _currentSortIsAsc);

        ApplySort ();
        _tableView.Update ();
    }
}
