namespace Terminal.Gui.Views;

public partial class TextView
{

    /// <summary>Get or sets whether the user is currently selecting text.</summary>
    public bool IsSelecting { get; set; }

    /// <summary>
    ///     Gets or sets whether the word navigation should select only the word itself without spaces around it or with the
    ///     spaces at right.
    ///     Default is <c>false</c> meaning that the spaces at right are included in the selection.
    /// </summary>
    public bool SelectWordOnlyOnDoubleClick { get; set; }

    /// <summary>Start row position of the selected text.</summary>
    public int SelectionStartRow
    {
        get => _selectionStartRow;
        set
        {
            _selectionStartRow = value < 0 ? 0 :
                                 value > _model.Count - 1 ? Math.Max (_model.Count - 1, 0) : value;
            IsSelecting = true;
            SetNeedsDraw ();
            AdjustScrollPosition ();
        }
    }

    /// <summary>Start column position of the selected text.</summary>
    public int SelectionStartColumn
    {
        get => _selectionStartColumn;
        set
        {
            List<Cell> line = _model.GetLine (_selectionStartRow);

            _selectionStartColumn = value < 0 ? 0 :
                                    value > line.Count ? line.Count : value;
            IsSelecting = true;
            SetNeedsDraw ();
            AdjustScrollPosition ();
        }
    }

    private void StartSelecting ()
    {
        if (_shiftSelecting && IsSelecting)
        {
            return;
        }

        _shiftSelecting = true;
        IsSelecting = true;
        _selectionStartColumn = CurrentColumn;
        _selectionStartRow = CurrentRow;
    }

    private void StopSelecting ()
    {
        if (IsSelecting)
        {
            SetNeedsDraw ();
        }

        _shiftSelecting = false;
        IsSelecting = false;
        _isButtonShift = false;
    }


    /// <summary>Length of the selected text.</summary>
    public int SelectedLength => GetSelectedLength ();

    /// <summary>
    ///     Gets the selected text as
    ///     <see>
    ///         <cref>List{List{Cell}}</cref>
    ///     </see>
    /// </summary>
    public List<List<Cell>> SelectedCellsList
    {
        get
        {
            GetRegion (out List<List<Cell>> selectedCellsList);

            return selectedCellsList;
        }
    }

    /// <summary>The selected text.</summary>
    public string SelectedText
    {
        get
        {
            if (!IsSelecting || (_model.Count == 1 && _model.GetLine (0).Count == 0))
            {
                return string.Empty;
            }

            return GetSelectedRegion ();
        }
    }


    // Returns an encoded region start..end (top 32 bits are the row, low32 the column)
    private void GetEncodedRegionBounds (
        out long start,
        out long end,
        int? startRow = null,
        int? startCol = null,
        int? cRow = null,
        int? cCol = null
    )
    {
        long selection;
        long point;

        if (startRow is null || startCol is null || cRow is null || cCol is null)
        {
            selection = ((long)(uint)_selectionStartRow << 32) | (uint)_selectionStartColumn;
            point = ((long)(uint)CurrentRow << 32) | (uint)CurrentColumn;
        }
        else
        {
            selection = ((long)(uint)startRow << 32) | (uint)startCol;
            point = ((long)(uint)cRow << 32) | (uint)cCol;
        }

        if (selection > point)
        {
            start = point;
            end = selection;
        }
        else
        {
            start = selection;
            end = point;
        }
    }

    //
    // Returns a string with the text in the selected 
    // region.
    //
    internal string GetRegion (
        out List<List<Cell>> cellsList,
        int? sRow = null,
        int? sCol = null,
        int? cRow = null,
        int? cCol = null,
        TextModel? model = null
    )
    {
        GetEncodedRegionBounds (out long start, out long end, sRow, sCol, cRow, cCol);

        cellsList = [];

        if (start == end)
        {
            return string.Empty;
        }

        var startRow = (int)(start >> 32);
        var maxRow = (int)(end >> 32);
        var startCol = (int)(start & 0xffffffff);
        var endCol = (int)(end & 0xffffffff);
        List<Cell> line = model is null ? _model.GetLine (startRow) : model.GetLine (startRow);
        List<Cell> cells;

        if (startRow == maxRow)
        {
            cells = line.GetRange (startCol, endCol - startCol);
            cellsList.Add (cells);

            return StringFromCells (cells);
        }

        cells = line.GetRange (startCol, line.Count - startCol);
        cellsList.Add (cells);
        string res = StringFromCells (cells);

        for (int row = startRow + 1; row < maxRow; row++)
        {
            cellsList.AddRange ([]);
            cells = model == null ? _model.GetLine (row) : model.GetLine (row);
            cellsList.Add (cells);

            res = res
                  + Environment.NewLine
                  + StringFromCells (cells);
        }

        line = model is null ? _model.GetLine (maxRow) : model.GetLine (maxRow);
        cellsList.AddRange ([]);
        cells = line.GetRange (0, endCol);
        cellsList.Add (cells);
        res = res + Environment.NewLine + StringFromCells (cells);

        return res;
    }

    private int GetSelectedLength () { return SelectedText.Length; }

    private string GetSelectedRegion ()
    {
        int cRow = CurrentRow;
        int cCol = CurrentColumn;
        int startRow = _selectionStartRow;
        int startCol = _selectionStartColumn;
        TextModel model = _model;

        if (_wordWrap)
        {
            cRow = _wrapManager!.GetModelLineFromWrappedLines (CurrentRow);
            cCol = _wrapManager.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
            startRow = _wrapManager.GetModelLineFromWrappedLines (_selectionStartRow);
            startCol = _wrapManager.GetModelColFromWrappedLines (_selectionStartRow, _selectionStartColumn);
            model = _wrapManager.Model;
        }

        OnUnwrappedCursorPosition (cRow, cCol);

        return GetRegion (out _, startRow, startCol, cRow, cCol, model);
    }


    private string StringFromCells (List<Cell> cells)
    {
        ArgumentNullException.ThrowIfNull (cells);

        var size = 0;
        foreach (Cell cell in cells)
        {
            string t = cell.Grapheme;
            size += Encoding.Unicode.GetByteCount (t);
        }

        byte [] encoded = new byte [size];
        var offset = 0;
        foreach (Cell cell in cells)
        {
            string t = cell.Grapheme;
            int bytesWritten = Encoding.Unicode.GetBytes (t, 0, t.Length, encoded, offset);
            offset += bytesWritten;
        }

        // decode using the same encoding and the bytes actually written
        return Encoding.Unicode.GetString (encoded, 0, offset);
    }

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        Text = """
               TextView provides a fully featured multi-line text editor.
               It supports word wrap and history for undo.
               """;

        return true;
    }


    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing && ContextMenu is { })
        {
            ContextMenu.Visible = false;
            ContextMenu.Dispose ();
            ContextMenu = null;
        }

        base.Dispose (disposing);
    }

    private void ClearRegion ()
    {
        SetWrapModel ();

        long start, end;
        long currentEncoded = ((long)(uint)CurrentRow << 32) | (uint)CurrentColumn;
        GetEncodedRegionBounds (out start, out end);
        var startRow = (int)(start >> 32);
        var maxrow = (int)(end >> 32);
        var startCol = (int)(start & 0xffffffff);
        var endCol = (int)(end & 0xffffffff);
        List<Cell> line = _model.GetLine (startRow);

        _historyText.Add (new () { new (line) }, new (startCol, startRow));

        List<List<Cell>> removedLines = new ();

        if (startRow == maxrow)
        {
            removedLines.Add (new (line));

            line.RemoveRange (startCol, endCol - startCol);
            CurrentColumn = startCol;

            if (_wordWrap)
            {
                SetNeedsDraw ();
            }
            else
            {
                //QUESTION: Is the below comment still relevant?
                // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
                //SetNeedsDraw (new (0, startRow - topRow, Viewport.Width, startRow - topRow + 1));
                SetNeedsDraw ();
            }

            _historyText.Add (
                              new (removedLines),
                              CursorPosition,
                              TextEditingLineStatus.Removed
                             );

            UpdateWrapModel ();

            return;
        }

        removedLines.Add (new (line));

        line.RemoveRange (startCol, line.Count - startCol);
        List<Cell> line2 = _model.GetLine (maxrow);
        line.AddRange (line2.Skip (endCol));

        for (int row = startRow + 1; row <= maxrow; row++)
        {
            removedLines.Add (new (_model.GetLine (startRow + 1)));

            _model.RemoveLine (startRow + 1);
        }

        if (currentEncoded == end)
        {
            CurrentRow -= maxrow - startRow;
        }

        CurrentColumn = startCol;

        _historyText.Add (
                          new (removedLines),
                          CursorPosition,
                          TextEditingLineStatus.Removed
                         );

        UpdateWrapModel ();

        SetNeedsDraw ();
    }

    private void ClearSelectedRegion ()
    {
        SetWrapModel ();

        if (!_isReadOnly)
        {
            ClearRegion ();
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();
    }

    /// <summary>Select all text.</summary>
    public void SelectAll ()
    {
        if (_model.Count == 0)
        {
            return;
        }

        StartSelecting ();
        _selectionStartColumn = 0;
        _selectionStartRow = 0;
        CurrentColumn = _model.GetLine (_model.Count - 1).Count;
        CurrentRow = _model.Count - 1;
        SetNeedsDraw ();
    }

    private void ProcessSelectAll ()
    {
        ResetColumnTrack ();
        SelectAll ();
    }

    private bool PointInSelection (int col, int row)
    {
        long start, end;
        GetEncodedRegionBounds (out start, out end);
        long q = ((long)(uint)row << 32) | (uint)col;

        return q >= start && q <= end - 1;
    }

    private void ToggleSelecting ()
    {
        ResetColumnTrack ();
        IsSelecting = !IsSelecting;
        _selectionStartColumn = CurrentColumn;
        _selectionStartRow = CurrentRow;
    }
}
