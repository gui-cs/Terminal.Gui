namespace Terminal.Gui.Views;

public partial class TextView
{
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
                AdjustScrollPosition ();
            }
            else
            {
                PositionCursor ();
            }
        }
    }

    private void Insert (Cell cell)
    {
        List<Cell> line = GetCurrentLine ();

        if (Used)
        {
            line.Insert (Math.Min (CurrentColumn, line.Count), cell);
        }
        else
        {
            if (CurrentColumn < line.Count)
            {
                line.RemoveAt (CurrentColumn);
            }

            line.Insert (Math.Min (CurrentColumn, line.Count), cell);
        }

        int prow = CurrentRow - _topRow;

        if (!_wrapNeeded)
        {
            // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
            //SetNeedsDraw (new (0, prow, Math.Max (Viewport.Width, 0), Math.Max (prow + 1, 0)));
            SetNeedsDraw ();
        }
    }

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

        _historyText.Add ([new (line)], CursorPosition);

        // Optimize single line
        if (lines.Count == 1)
        {
            line.InsertRange (CurrentColumn, lines [0]);
            CurrentColumn += lines [0].Count;

            _historyText.Add (
                              [new (line)],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (!_wordWrap && CurrentColumn - _leftColumn > Viewport.Width)
            {
                _leftColumn = Math.Max (CurrentColumn - Viewport.Width + 1, 0);
            }

            if (_wordWrap)
            {
                SetNeedsDraw ();
            }
            else
            {
                // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
                //SetNeedsDraw (new (0, currentRow - topRow, Viewport.Width, Math.Max (currentRow - topRow + 1, 0)));
                SetNeedsDraw ();
            }

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

        //model.AddLine (currentRow, lines [0]);

        List<List<Cell>> addedLines = [new (line)];

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
        AdjustScrollPosition ();

        _historyText.Add (
                          [new (line)],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();
        OnContentsChanged ();
    }

    private bool InsertText (Key a, Attribute? attribute = null)
    {
        //So that special keys like tab can be processed
        if (_isReadOnly)
        {
            return true;
        }

        SetWrapModel ();

        _historyText.Add ([new (GetCurrentLine ())], CursorPosition);

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
                          [new (GetCurrentLine ())],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();
        OnContentsChanged ();

        return true;
    }

    #region History Event Handlers

    private void HistoryText_ChangeText (object sender, HistoryTextItemEventArgs obj)
    {
        SetWrapModel ();

        if (obj is { })
        {
            int startLine = obj.CursorPosition.Y;

            if (obj.RemovedOnAdded is { })
            {
                int offset;

                if (obj.IsUndoing)
                {
                    offset = Math.Max (obj.RemovedOnAdded.Lines.Count - obj.Lines.Count, 1);
                }
                else
                {
                    offset = obj.RemovedOnAdded.Lines.Count - 1;
                }

                for (var i = 0; i < offset; i++)
                {
                    if (Lines > obj.RemovedOnAdded.CursorPosition.Y)
                    {
                        _model.RemoveLine (obj.RemovedOnAdded.CursorPosition.Y);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            for (var i = 0; i < obj.Lines.Count; i++)
            {
                if (i == 0 || obj.LineStatus == TextEditingLineStatus.Original || obj.LineStatus == TextEditingLineStatus.Attribute)
                {
                    _model.ReplaceLine (startLine, obj.Lines [i]);
                }
                else if (obj is { IsUndoing: true, LineStatus: TextEditingLineStatus.Removed }
                                or { IsUndoing: false, LineStatus: TextEditingLineStatus.Added })
                {
                    _model.AddLine (startLine, obj.Lines [i]);
                }
                else if (Lines > obj.CursorPosition.Y + 1)
                {
                    _model.RemoveLine (obj.CursorPosition.Y + 1);
                }

                startLine++;
            }

            CursorPosition = obj.FinalCursorPosition;
        }

        UpdateWrapModel ();

        AdjustScrollPosition ();
        OnContentsChanged ();
    }

    #endregion
}
