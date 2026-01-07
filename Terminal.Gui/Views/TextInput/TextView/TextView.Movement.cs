namespace Terminal.Gui.Views;

public partial class TextView
{
    /// <summary>Will scroll the <see cref="TextView"/> to the last line and position the cursor there.</summary>
    public bool MoveEnd ()
    {
        CurrentRow = _model.Count - 1;
        List<Cell> line = GetCurrentLine ();
        CurrentColumn = line.Count;
        TrackColumn ();
        DoNeededAction ();

        return true;
    }

    /// <summary>Will scroll the <see cref="TextView"/> to the first line and position the cursor there.</summary>
    public bool MoveHome ()
    {
        CurrentRow = 0;
        _topRow = 0;
        CurrentColumn = 0;
        _leftColumn = 0;
        TrackColumn ();
        DoNeededAction ();

        return true;
    }

    /// <summary>
    ///     Will scroll the <see cref="TextView"/> to display the specified row at the top if <paramref name="isRow"/> is
    ///     true or will scroll the <see cref="TextView"/> to display the specified column at the left if
    ///     <paramref name="isRow"/> is false.
    /// </summary>
    /// <param name="idx">
    ///     Row that should be displayed at the top or Column that should be displayed at the left, if the value
    ///     is negative it will be reset to zero
    /// </param>
    /// <param name="isRow">If true (default) the <paramref name="idx"/> is a row, column otherwise.</param>
    public void ScrollTo (int idx, bool isRow = true)
    {
        if (idx < 0)
        {
            idx = 0;
        }

        if (isRow)
        {
            _topRow = Math.Max (idx > _model.Count - 1 ? _model.Count - 1 : idx, 0);
        }
        else if (!_wordWrap)
        {
            int maxlength =
                _model.GetMaxVisibleLine (_topRow, _topRow + Viewport.Height, TabWidth);
            _leftColumn = Math.Max (!_wordWrap && idx > maxlength - 1 ? maxlength - 1 : idx, 0);
        }

        SetNeedsDraw ();
    }

    private bool MoveBottomEnd ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveEnd ();
    }

    private bool MoveBottomEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveEnd ();
    }

    private bool MoveDown ()
    {
        if (CurrentRow + 1 < _model.Count)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow++;

            if (CurrentRow >= _topRow + Viewport.Height)
            {
                _topRow++;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }
        else if (CurrentRow > Viewport.Height)
        {
            Adjust ();
        }
        else
        {
            return false;
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveEndOfLine ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        CurrentColumn = currentLine.Count;
        DoNeededAction ();

        return true;
    }

    private bool MoveLeft ()
    {
        if (CurrentColumn > 0)
        {
            CurrentColumn--;
        }
        else
        {
            if (CurrentRow > 0)
            {
                CurrentRow--;

                if (CurrentRow < _topRow)
                {
                    _topRow--;
                    SetNeedsDraw ();
                }

                List<Cell> currentLine = GetCurrentLine ();
                CurrentColumn = Math.Max (currentLine.Count - (ReadOnly ? 1 : 0), 0);
            }
            else
            {
                return false;
            }
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveLeftStart ()
    {
        if (_leftColumn > 0)
        {
            SetNeedsDraw ();
        }

        CurrentColumn = 0;
        _leftColumn = 0;
        DoNeededAction ();

        return true;
    }

    private bool MovePageDown ()
    {
        int nPageDnShift = Viewport.Height - 1;

        if (CurrentRow >= 0 && CurrentRow < _model.Count)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow = CurrentRow + nPageDnShift > _model.Count
                             ? _model.Count > 0 ? _model.Count - 1 : 0
                             : CurrentRow + nPageDnShift;

            if (_topRow < CurrentRow - nPageDnShift)
            {
                _topRow = CurrentRow >= _model.Count
                              ? CurrentRow - nPageDnShift
                              : _topRow + nPageDnShift;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }

        DoNeededAction ();

        return true;
    }

    private bool MovePageUp ()
    {
        int nPageUpShift = Viewport.Height - 1;

        if (CurrentRow > 0)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow = CurrentRow - nPageUpShift < 0 ? 0 : CurrentRow - nPageUpShift;

            if (CurrentRow < _topRow)
            {
                _topRow = _topRow - nPageUpShift < 0 ? 0 : _topRow - nPageUpShift;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveRight ()
    {
        List<Cell> currentLine = GetCurrentLine ();

        if (CurrentColumn < currentLine.Count)
        {
            CurrentColumn++;
        }
        else
        {
            if (CurrentRow + 1 < _model.Count)
            {
                CurrentRow++;
                CurrentColumn = 0;

                if (CurrentRow >= _topRow + Viewport.Height)
                {
                    _topRow++;
                    SetNeedsDraw ();
                }
            }
            else
            {
                return false;
            }
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveTopHome ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveHome ();

        return true;
    }

    private bool MoveTopHomeExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveHome ();

        return true;
    }

    private bool MoveUp ()
    {
        if (CurrentRow > 0)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow--;

            if (CurrentRow < _topRow)
            {
                _topRow--;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }
        else
        {
            return false;
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveWordBackward ()
    {
        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        DoNeededAction ();

        return true;
    }

    private bool MoveWordForward ()
    {
        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        DoNeededAction ();

        return true;
    }

    private bool ProcessMoveDown ()
    {
        ResetContinuousFindTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveDown ();
    }

    private bool ProcessMoveDownExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();

        return MoveDown ();
    }

    private bool ProcessMoveEndOfLine ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveEndOfLine ();
    }

    private bool ProcessMoveLeft ()
    {
        // if the user presses Left (without any control keys) and they are at the start of the text
        if (CurrentColumn == 0 && CurrentRow == 0)
        {
            if (IsSelecting)
            {
                StopSelecting ();

                return true;
            }

            // do not respond (this lets the key press fall through to navigation system - which usually changes focus backward)
            return false;
        }

        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveLeft ();

        return true;
    }

    private bool ProcessMoveLeftExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveLeft ();
    }

    private bool ProcessMoveLeftStart ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveLeftStart ();
    }

    private bool ProcessMoveLeftStartExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveLeftStart ();
    }

    private bool ProcessMoveRight ()
    {
        // if the user presses Right (without any control keys)
        // determine where the last cursor position in the text is
        int lastRow = _model.Count - 1;
        int lastCol = _model.GetLine (lastRow).Count;

        // if they are at the very end of all the text do not respond (this lets the key press fall through to navigation system - which usually changes focus forward)
        if (CurrentColumn == lastCol && CurrentRow == lastRow)
        {
            // Unless they have text selected
            if (IsSelecting)
            {
                // In which case clear
                StopSelecting ();

                return true;
            }

            return false;
        }

        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveRight ();

        return true;
    }

    private bool ProcessMoveRightEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveEndOfLine ();
    }

    private bool ProcessMoveRightExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveRight ();
    }

    private bool ProcessMoveUp ()
    {
        ResetContinuousFindTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveUp ();
    }

    private bool ProcessMoveUpExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();

        return MoveUp ();
    }

    private bool ProcessMoveWordBackward ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveWordBackward ();
    }

    private bool ProcessMoveWordBackwardExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveWordBackward ();
    }

    private bool ProcessMoveWordForward ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveWordForward ();
    }

    private bool ProcessMoveWordForwardExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();

        return MoveWordForward ();
    }

    private bool ProcessPageDown ()
    {
        ResetColumnTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MovePageDown ();
    }

    private bool ProcessPageDownExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();

        return MovePageDown ();
    }

    private bool ProcessPageUp ()
    {
        ResetColumnTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MovePageUp ();
    }

    private bool ProcessPageUpExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();

        return MovePageUp ();
    }
}
