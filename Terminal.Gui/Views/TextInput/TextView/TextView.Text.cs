namespace Terminal.Gui.Views;

public partial class TextView
{
    private TextModel _model = new ();
    private int _currentColumn;
    private int _currentRow;

    /// <summary>Gets the cursor column.</summary>
    /// <value>The cursor column.</value>
    public int CurrentColumn
    {
        get => _currentColumn;
        private set
        {
            _currentColumn = value;
            _cursorPosition = new (_currentColumn, _currentRow);
        }
    }

    /// <summary>Gets the current cursor row.</summary>
    public int CurrentRow
    {
        get => _currentRow;
        private set
        {
            _currentRow = value;
            _cursorPosition = new (_currentColumn, _currentRow);
        }
    }

    private Point _cursorPosition;

    /// <summary>Sets or gets the current cursor position.</summary>
    public Point CursorPosition
    {
        get => _cursorPosition;
        set
        {
            List<Cell> line = _model.GetLine (Math.Max (Math.Min (value.Y, _model.Count - 1), 0));

            CurrentColumn = value.X < 0 ? 0 :
                            value.X > line.Count ? line.Count : value.X;

            CurrentRow = value.Y < 0 ? 0 :
                         value.Y > _model.Count - 1 ? Math.Max (_model.Count - 1, 0) : value.Y;
            SetNeedsDraw ();
            Adjust ();
        }
    }

    /// <summary>
    ///     Indicates whatever the text was changed or not. <see langword="true"/> if the text was changed
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool IsDirty
    {
        get => _historyText.IsDirty (_model.GetAllLines ());
        set => _historyText.Clear (_model.GetAllLines ());
    }

    private int _leftColumn;

    /// <summary>Gets or sets the left column.</summary>
    public int LeftColumn
    {
        get => _leftColumn;
        set
        {
            if (value > 0 && _wordWrap)
            {
                return;
            }

            _leftColumn = Math.Max (Math.Min (value, Maxlength - 1), 0);
        }
    }

    /// <summary>Gets the number of lines.</summary>
    public int Lines => _model.Count;

    /// <summary>Gets the maximum visible length line.</summary>
    public int Maxlength => _model.GetMaxVisibleLine (_topRow, _topRow + Viewport.Height, TabWidth);

    private bool _multiline = true;

    /// <summary>Gets or sets a value indicating whether this <see cref="TextView"/> is a multiline text view.</summary>
    public bool Multiline
    {
        get => _multiline;
        set
        {
            _multiline = value;

            if (_multiline && !_allowsTab)
            {
                AllowsTab = true;
            }

            if (_multiline && !_allowsReturn)
            {
                AllowsReturn = true;
            }

            if (!_multiline)
            {
                AllowsReturn = false;
                AllowsTab = false;
                WordWrap = false;
                CurrentColumn = 0;
                CurrentRow = 0;
                _savedHeight = Height;

                Height = Dim.Auto (DimAutoStyle.Text, 1);

                if (!IsInitialized)
                {
                    _model.LoadString (Text);
                }

                SetNeedsDraw ();
            }
            else if (_multiline && _savedHeight is { })
            {
                Height = _savedHeight;
                SetNeedsDraw ();
            }

            KeyBindings.Remove (Key.Enter);
            KeyBindings.Add (Key.Enter, Multiline ? Command.NewLine : Command.Accept);
        }
    }

    private bool _isReadOnly;

    /// <summary>Gets or sets whether the <see cref="TextView"/> is in read-only mode or not</summary>
    /// <value>Boolean value(Default false)</value>
    public bool ReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (value != _isReadOnly)
            {
                _isReadOnly = value;

                SetNeedsDraw ();
                WrapTextModel ();
                Adjust ();
            }
        }
    }

    private int _tabWidth = 4;

    /// <summary>Gets or sets a value indicating the number of whitespace when pressing the TAB key.</summary>
    public int TabWidth
    {
        get => _tabWidth;
        set
        {
            _tabWidth = Math.Max (value, 0);

            if (_tabWidth > 0 && !AllowsTab)
            {
                AllowsTab = true;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>Sets or gets the text in the <see cref="TextView"/>.</summary>
    /// <remarks>
    ///     The <see cref="View.TextChanged"/> event is fired whenever this property is set. Note, however, that Text is not
    ///     set by <see cref="TextView"/> as the user types.
    /// </remarks>
    public override string Text
    {
        get
        {
            if (_wordWrap)
            {
                return _wrapManager!.Model.ToString ();
            }

            return _model.ToString ();
        }
        set
        {
            ResetPosition ();
            _model.LoadString (value);

            if (_wordWrap)
            {
                _wrapManager = new (_model);
                _model = _wrapManager.WrapModel (Viewport.Width, out _, out _, out _, out _);
            }

            OnTextChanged ();
            SetNeedsDraw ();

            _historyText.Clear (_model.GetAllLines ());
        }
    }

    /// <summary>Gets or sets the top row.</summary>
    public int TopRow
    {
        get => _topRow;
        set => _topRow = Math.Max (Math.Min (value, Lines - 1), 0);
    }

    /// <summary>
    ///     Tracks whether the text view should be considered "used", that is, that the user has moved in the entry, so
    ///     new input should be appended at the cursor position, rather than clearing the entry
    /// </summary>
    public bool Used { get; set; }

    /// <summary>
    ///     Gets or sets whether the word forward and word backward navigation should use the same or equivalent rune type.
    ///     Default is <c>false</c> meaning using equivalent rune type.
    /// </summary>
    public bool UseSameRuneTypeForWords { get; set; }

    /// <summary>Gets all lines of characters.</summary>
    /// <returns></returns>
    public List<List<Cell>> GetAllLines () { return _model.GetAllLines (); }

    /// <summary>
    ///     Returns the characters on the current line (where the cursor is positioned). Use <see cref="CurrentColumn"/>
    ///     to determine the position of the cursor within that line
    /// </summary>
    /// <returns></returns>
    public List<Cell> GetCurrentLine () { return _model.GetLine (CurrentRow); }

    /// <summary>Returns the characters on the <paramref name="line"/>.</summary>
    /// <param name="line">The intended line.</param>
    /// <returns></returns>
    public List<Cell> GetLine (int line) { return _model.GetLine (line); }

    /// <summary>
    ///     Inserts the given <paramref name="toAdd"/> text at the current cursor position exactly as if the user had just
    ///     typed it
    /// </summary>
    /// <param name="toAdd">Text to add</param>
    public void InsertText (string toAdd)
    {
        foreach (char ch in toAdd)
        {
            Key key;

            try
            {
                key = new (ch);
            }
            catch (Exception)
            {
                throw new ArgumentException (
                                             $"Cannot insert character '{ch}' because it does not map to a Key"
                                            );
            }

            InsertText (key);

            if (NeedsDraw)
            {
                Adjust ();
            }
            else
            {
                PositionCursor ();
            }
        }
    }

    /// <summary>Replaces all the text based on the match case.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool ReplaceAllText (
        string textToFind,
        bool matchCase = false,
        bool matchWholeWord = false,
        string? textToReplace = null
    )
    {
        if (_isReadOnly || _model.Count == 0)
        {
            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos =
            _model.ReplaceAllText (textToFind, matchCase, matchWholeWord, textToReplace);

        return SetFoundText (textToFind, foundPos, textToReplace, false, true);
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

        _historyText.Add ([[.. line]], new (startCol, startRow));

        List<List<Cell>> removedLines = [];

        if (startRow == maxrow)
        {
            removedLines.Add ([.. line]);

            line.RemoveRange (startCol, endCol - startCol);
            CurrentColumn = startCol;

            SetNeedsDraw ();

            _historyText.Add (
                              [.. removedLines],
                              CursorPosition,
                              TextEditingLineStatus.Removed
                             );

            UpdateWrapModel ();

            return;
        }

        removedLines.Add ([.. line]);

        line.RemoveRange (startCol, line.Count - startCol);
        List<Cell> line2 = _model.GetLine (maxrow);
        line.AddRange (line2.Skip (endCol));

        for (int row = startRow + 1; row <= maxrow; row++)
        {
            removedLines.Add ([.. _model.GetLine (startRow + 1)]);

            _model.RemoveLine (startRow + 1);
        }

        if (currentEncoded == end)
        {
            CurrentRow -= maxrow - startRow;
        }

        CurrentColumn = startCol;

        _historyText.Add (
                          [.. removedLines],
                          CursorPosition,
                          TextEditingLineStatus.Removed
                         );

        UpdateWrapModel ();

        SetNeedsDraw ();
    }

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

    private void Insert (Cell cell)
    {
        List<Cell> line = GetCurrentLine ();

        if (!Used)
        {
            if (CurrentColumn < line.Count)
            {
                line.RemoveAt (CurrentColumn);
            }
        }

        line.Insert (Math.Min (CurrentColumn, line.Count), cell);

        if (!_wrapNeeded)
        {
            SetNeedsDraw ();
        }
    }

    private string? _copiedText;
    private List<List<Cell>> _copiedCellsList = [];

    private void InsertAllText (string text, bool fromClipboard = false)
    {
        if (string.IsNullOrEmpty (text))
        {
            return;
        }

        List<List<Cell>> lines;

        if (fromClipboard && text == _copiedText)
        {
            lines = _copiedCellsList;
        }
        else
        {
            // Get selected attribute
            Attribute? attribute = GetSelectedAttribute (CurrentRow, CurrentColumn);
            lines = Cell.StringToLinesOfCells (text, attribute);
        }

        if (lines.Count == 0)
        {
            return;
        }

        SetWrapModel ();

        List<Cell> line = GetCurrentLine ();

        _historyText.Add ([[.. line]], CursorPosition);

        // Optimize single line
        if (lines.Count == 1)
        {
            line.InsertRange (CurrentColumn, lines [0]);
            CurrentColumn += lines [0].Count;

            _historyText.Add (
                              [[.. line]],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (!_wordWrap && CurrentColumn - _leftColumn > Viewport.Width)
            {
                _leftColumn = Math.Max (CurrentColumn - Viewport.Width + 1, 0);
            }

            SetNeedsDraw ();
            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        List<Cell>? rest = null;
        var lastPosition = 0;

        if (_model.Count > 0 && line.Count > 0 && !_copyWithoutSelection)
        {
            // Keep a copy of the rest of the line
            int restCount = line.Count - CurrentColumn;
            rest = line.GetRange (CurrentColumn, restCount);
            line.RemoveRange (CurrentColumn, restCount);
        }

        // First line is inserted at the current location, the rest is appended
        line.InsertRange (CurrentColumn, lines [0]);

        List<List<Cell>> addedLines = [[..line]];

        for (var i = 1; i < lines.Count; i++)
        {
            _model.AddLine (CurrentRow + i, lines [i]);

            addedLines.Add ([.. lines [i]]);
        }

        if (rest is { })
        {
            List<Cell> last = _model.GetLine (CurrentRow + lines.Count - 1);
            lastPosition = last.Count;
            last.InsertRange (last.Count, rest);

            addedLines.Last ().InsertRange (addedLines.Last ().Count, rest);
        }

        _historyText.Add (addedLines, CursorPosition, TextEditingLineStatus.Added);

        // Now adjust column and row positions
        CurrentRow += lines.Count - 1;
        CurrentColumn = rest is { } ? lastPosition : lines [^1].Count;
        Adjust ();

        _historyText.Add ([[..line]], CursorPosition, TextEditingLineStatus.Replaced);

        UpdateWrapModel ();
        OnContentsChanged ();
    }

    private void InsertText (Key a, Attribute? attribute = null)
    {
        //So that special keys like tab can be processed
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        _historyText.Add ([[..GetCurrentLine ()]], CursorPosition);

        if (IsSelecting)
        {
            ClearSelectedRegion ();
        }

        if ((uint)a.KeyCode == '\n')
        {
            _model.AddLine (CurrentRow + 1, []);
            CurrentRow++;
            CurrentColumn = 0;
        }
        else if ((uint)a.KeyCode == '\r')
        {
            CurrentColumn = 0;
        }
        else
        {
            if (Used)
            {
                Insert (new () { Grapheme = a.AsRune.ToString (), Attribute = attribute });
                CurrentColumn++;

                if (CurrentColumn >= _leftColumn + Viewport.Width)
                {
                    _leftColumn++;
                    SetNeedsDraw ();
                }
            }
            else
            {
                Insert (new () { Grapheme = a.AsRune.ToString (), Attribute = attribute });
                CurrentColumn++;
            }
        }

        _historyText.Add (
                          [[..GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();
        OnContentsChanged ();
    }

    private void Model_LinesLoaded (object sender, EventArgs e)
    {
        // This call is not needed. Model_LinesLoaded gets invoked when
        // model.LoadString (value) is called. LoadString is called from one place
        // (Text.set) and historyText.Clear() is called immediately after.
        // If this call happens, HistoryText_ChangeText will get called multiple times
        // when Text is set, which is wrong.
        //historyText.Clear (Text);

        if (!_multiline && !IsInitialized)
        {
            CurrentColumn = Text.GetRuneCount ();
            _leftColumn = CurrentColumn > Viewport.Width + 1 ? CurrentColumn - Viewport.Width + 1 : 0;
        }
    }

    private int _columnTrack = -1;
    private bool _lastWasKill;

    private void ResetAllTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _columnTrack = -1;
        _continuousFind = false;
    }

    private void ResetColumnTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _columnTrack = -1;
    }

    private bool _continuousFind;

    private void ResetContinuousFind ()
    {
        if (!_continuousFind)
        {
            int col = IsSelecting ? _selectionStartColumn : CurrentColumn;
            int row = IsSelecting ? _selectionStartRow : CurrentRow;
            _model.ResetContinuousFind (new (col, row));
        }
    }

    private void ResetContinuousFindTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _continuousFind = false;
    }

    private int _topRow;

    private void ResetPosition ()
    {
        _topRow = _leftColumn = CurrentRow = CurrentColumn = 0;
        StopSelecting ();
    }

    private bool SetFoundText (
        string text,
        (Point current, bool found) foundPos,
        string? textToReplace = null,
        bool replace = false,
        bool replaceAll = false
    )
    {
        if (foundPos.found)
        {
            StartSelecting ();
            _selectionStartColumn = foundPos.current.X;
            _selectionStartRow = foundPos.current.Y;

            if (!replaceAll)
            {
                CurrentColumn = _selectionStartColumn + text.GetRuneCount ();
            }
            else
            {
                CurrentColumn = _selectionStartColumn + textToReplace!.GetRuneCount ();
            }

            CurrentRow = foundPos.current.Y;

            if (!_isReadOnly && replace)
            {
                Adjust ();
                ClearSelectedRegion ();
                InsertAllText (textToReplace!);
                StartSelecting ();
                _selectionStartColumn = CurrentColumn - textToReplace!.GetRuneCount ();
            }
            else
            {
                UpdateWrapModel ();
                SetNeedsDraw ();
                Adjust ();
            }

            _continuousFind = true;

            return foundPos.found;
        }

        UpdateWrapModel ();
        _continuousFind = false;

        return foundPos.found;
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

        var encoded = new byte [size];
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

    private void TrackColumn ()
    {
        // Now track the column
        List<Cell> line = GetCurrentLine ();

        if (line.Count < _columnTrack)
        {
            CurrentColumn = line.Count;
        }
        else if (_columnTrack != -1)
        {
            CurrentColumn = _columnTrack;
        }
        else if (CurrentColumn > line.Count)
        {
            CurrentColumn = line.Count;
        }

        Adjust ();
    }
}
