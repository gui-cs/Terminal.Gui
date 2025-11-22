namespace Terminal.Gui.Views;

public partial class TextView
{
    /// <summary>Deletes all text.</summary>
    public void DeleteAll ()
    {
        if (Lines == 0)
        {
            return;
        }

        _selectionStartColumn = 0;
        _selectionStartRow = 0;
        MoveBottomEndExtend ();
        DeleteCharLeft ();
        SetNeedsDraw ();
    }

    /// <summary>Deletes all the selected or a single character at left from the position of the cursor.</summary>
    public void DeleteCharLeft ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        if (IsSelecting)
        {
            _historyText.Add (new () { new (GetCurrentLine ()) }, CursorPosition);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        if (DeleteTextBackwards ())
        {
            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();
    }

    /// <summary>Deletes all the selected or a single character at right from the position of the cursor.</summary>
    public void DeleteCharRight ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        if (IsSelecting)
        {
            _historyText.Add (new () { new (GetCurrentLine ()) }, CursorPosition);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        if (DeleteTextForwards ())
        {
            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();
    }

    private bool DeleteTextBackwards ()
    {
        SetWrapModel ();

        if (CurrentColumn > 0)
        {
            // Delete backwards 
            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add (new () { new (currentLine) }, CursorPosition);

            currentLine.RemoveAt (CurrentColumn - 1);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn--;

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (CurrentColumn < _leftColumn)
            {
                _leftColumn--;
                SetNeedsDraw ();
            }
            else
            {
                // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
                //SetNeedsDraw (new (0, currentRow - topRow, 1, Viewport.Width));
                SetNeedsDraw ();
            }
        }
        else
        {
            // Merges the current line with the previous one.
            if (CurrentRow == 0)
            {
                return true;
            }

            int prowIdx = CurrentRow - 1;
            List<Cell> prevRow = _model.GetLine (prowIdx);

            _historyText.Add (new () { new (prevRow) }, CursorPosition);

            List<List<Cell>> removedLines = new () { new (prevRow) };

            removedLines.Add (new (GetCurrentLine ()));

            _historyText.Add (
                              removedLines,
                              new (CurrentColumn, prowIdx),
                              TextEditingLineStatus.Removed
                             );

            int prevCount = prevRow.Count;
            _model.GetLine (prowIdx).AddRange (GetCurrentLine ());
            _model.RemoveLine (CurrentRow);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentRow--;

            _historyText.Add (
                              new () { GetCurrentLine () },
                              new (CurrentColumn, prowIdx),
                              TextEditingLineStatus.Replaced
                             );

            CurrentColumn = prevCount;
            SetNeedsDraw ();
        }

        UpdateWrapModel ();

        return false;
    }

    private bool DeleteTextForwards ()
    {
        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        if (CurrentColumn == currentLine.Count)
        {
            if (CurrentRow + 1 == _model.Count)
            {
                UpdateWrapModel ();

                return true;
            }

            _historyText.Add (new () { new (currentLine) }, CursorPosition);

            List<List<Cell>> removedLines = new () { new (currentLine) };

            List<Cell> nextLine = _model.GetLine (CurrentRow + 1);

            removedLines.Add (new (nextLine));

            _historyText.Add (removedLines, CursorPosition, TextEditingLineStatus.Removed);

            currentLine.AddRange (nextLine);
            _model.RemoveLine (CurrentRow + 1);

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, CurrentRow - _topRow + 1));
        }
        else
        {
            _historyText.Add ([ [.. currentLine]], CursorPosition);

            currentLine.RemoveAt (CurrentColumn);

            _historyText.Add (
                              [ [.. currentLine]],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            DoSetNeedsDraw (
                            new (
                                 CurrentColumn - _leftColumn,
                                 CurrentRow - _topRow,
                                 Viewport.Width,
                                 Math.Max (CurrentRow - _topRow + 1, 0)
                                )
                           );
        }

        UpdateWrapModel ();

        return false;
    }

    private void ProcessKillWordForward ()
    {
        ResetColumnTrack ();
        StopSelecting ();
        KillWordForward ();
    }

    private void ProcessKillWordBackward ()
    {
        ResetColumnTrack ();
        KillWordBackward ();
    }

    private void ProcessDeleteCharRight ()
    {
        ResetColumnTrack ();
        DeleteCharRight ();
    }

    private void ProcessDeleteCharLeft ()
    {
        ResetColumnTrack ();
        DeleteCharLeft ();
    }

    private void KillWordForward ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([ [.. GetCurrentLine ()]], CursorPosition);

        if (currentLine.Count == 0 || CurrentColumn == currentLine.Count)
        {
            DeleteTextForwards ();

            _historyText.ReplaceLast (
                                      [ [.. GetCurrentLine ()]],
                                      CursorPosition,
                                      TextEditingLineStatus.Replaced
                                     );

            UpdateWrapModel ();

            return;
        }

        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);
        var restCount = 0;

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            restCount = newPos.Value.col - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
        }
        else if (newPos.HasValue)
        {
            restCount = currentLine.Count - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
        }

        if (_wordWrap)
        {
            _wrapNeeded = true;
        }

        _historyText.Add (
                          [ [.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));
        DoNeededAction ();
    }

    private void KillWordBackward ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([ [.. GetCurrentLine ()]], CursorPosition);

        if (CurrentColumn == 0)
        {
            DeleteTextBackwards ();

            _historyText.ReplaceLast (
                                      [ [.. GetCurrentLine ()]],
                                      CursorPosition,
                                      TextEditingLineStatus.Replaced
                                     );

            UpdateWrapModel ();

            return;
        }

        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            int restCount = CurrentColumn - newPos.Value.col;
            currentLine.RemoveRange (newPos.Value.col, restCount);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn = newPos.Value.col;
        }
        else if (newPos.HasValue)
        {
            int restCount;

            if (newPos.Value.row == CurrentRow)
            {
                restCount = currentLine.Count - CurrentColumn;
                currentLine.RemoveRange (CurrentColumn, restCount);
            }
            else
            {
                while (CurrentRow != newPos.Value.row)
                {
                    restCount = currentLine.Count;
                    currentLine.RemoveRange (0, restCount);

                    CurrentRow--;
                    currentLine = GetCurrentLine ();
                }
            }

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        _historyText.Add (
                          [ [.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));
        DoNeededAction ();
    }

    private void KillToLeftStart ()
    {
        if (_isReadOnly)
        {
            return;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == 0)
        {
            UpdateWrapModel ();

            DeleteTextBackwards ();

            return;
        }

        _historyText.Add ([ [.. currentLine]], CursorPosition);

        if (currentLine.Count == 0)
        {
            if (CurrentRow > 0)
            {
                _model.RemoveLine (CurrentRow);

                if (_model.Count > 0 || _lastWasKill)
                {
                    string val = Environment.NewLine;

                    if (_lastWasKill)
                    {
                        AppendClipboard (val);
                    }
                    else
                    {
                        SetClipboard (val);
                    }
                }

                if (_model.Count == 0)
                {
                    // Prevents from adding line feeds if there is no more lines.
                    setLastWasKill = false;
                }

                CurrentRow--;
                currentLine = _model.GetLine (CurrentRow);

                List<List<Cell>> removedLine =
                [
                    [..currentLine],
                    []
                ];

                _historyText.Add (
                                  [.. removedLine],
                                  CursorPosition,
                                  TextEditingLineStatus.Removed
                                 );

                CurrentColumn = currentLine.Count;
            }
        }
        else
        {
            int restCount = CurrentColumn;
            List<Cell> rest = currentLine.GetRange (0, restCount);
            var val = string.Empty;
            val += StringFromCells (rest);

            if (_lastWasKill)
            {
                AppendClipboard (val);
            }
            else
            {
                SetClipboard (val);
            }

            currentLine.RemoveRange (0, restCount);
            CurrentColumn = 0;
        }

        _historyText.Add (
                          [ [.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));

        _lastWasKill = setLastWasKill;
        DoNeededAction ();
    }

    private void KillToEndOfLine ()
    {
        if (_isReadOnly)
        {
            return;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == currentLine.Count)
        {
            UpdateWrapModel ();

            DeleteTextForwards ();

            return;
        }

        _historyText.Add (new () { new (currentLine) }, CursorPosition);

        if (currentLine.Count == 0)
        {
            if (CurrentRow < _model.Count - 1)
            {
                List<List<Cell>> removedLines = new () { new (currentLine) };

                _model.RemoveLine (CurrentRow);

                removedLines.Add (new (GetCurrentLine ()));

                _historyText.Add (
                                  new (removedLines),
                                  CursorPosition,
                                  TextEditingLineStatus.Removed
                                 );
            }

            if (_model.Count > 0 || _lastWasKill)
            {
                string val = Environment.NewLine;

                if (_lastWasKill)
                {
                    AppendClipboard (val);
                }
                else
                {
                    SetClipboard (val);
                }
            }

            if (_model.Count == 0)
            {
                // Prevents from adding line feeds if there is no more lines.
                setLastWasKill = false;
            }
        }
        else
        {
            int restCount = currentLine.Count - CurrentColumn;
            List<Cell> rest = currentLine.GetRange (CurrentColumn, restCount);
            var val = string.Empty;
            val += StringFromCells (rest);

            if (_lastWasKill)
            {
                AppendClipboard (val);
            }
            else
            {
                SetClipboard (val);
            }

            currentLine.RemoveRange (CurrentColumn, restCount);
        }

        _historyText.Add (
                          [ [.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));

        _lastWasKill = setLastWasKill;
        DoNeededAction ();
    }

    /// <summary>
    ///     INTERNAL: Resets the column tracking state and last kill operation flag.
    ///     Column tracking is used to maintain the desired cursor column position when moving up/down
    ///     through lines of different lengths.
    /// </summary>
    private void ResetColumnTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _columnTrack = -1;
    }
}
