namespace Terminal.Gui.Views;

/// <summary>Navigation functionality - cursor movement and scrolling</summary>
public partial class TextView
{
    #region Public Navigation Methods

    /// <summary>Will scroll the <see cref="TextView"/> to the last line and position the cursor there.</summary>
    public void MoveEnd ()
    {
        CurrentRow = _model.Count - 1;
        List<Cell> line = GetCurrentLine ();
        CurrentColumn = line.Count;
        TrackColumn ();
        DoNeededAction ();
    }

    /// <summary>Will scroll the <see cref="TextView"/> to the first line and position the cursor there.</summary>
    public void MoveHome ()
    {
        CurrentRow = 0;
        _topRow = 0;
        CurrentColumn = 0;
        _leftColumn = 0;
        TrackColumn ();
        DoNeededAction ();
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

            if (IsInitialized && Viewport.Y != _topRow)
            {
                Viewport = Viewport with { Y = _topRow };
            }
        }
        else if (!_wordWrap)
        {
            int maxlength = _model.GetMaxVisibleLine (_topRow, _topRow + Viewport.Height, TabWidth);
            _leftColumn = Math.Max (!_wordWrap && idx > maxlength - 1 ? maxlength - 1 : idx, 0);

            if (IsInitialized && Viewport.X != _leftColumn)
            {
                Viewport = Viewport with { X = _leftColumn };
            }
        }

        SetNeedsDraw ();
    }

    #endregion

    #region Private Navigation Methods

    private void MoveBottomEnd ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveEnd ();
    }

    private void MoveBottomEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveEnd ();
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
            AdjustScrollPosition ();
        }
        else
        {
            return false;
        }

        DoNeededAction ();

        return true;
    }

    private void MoveEndOfLine ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        CurrentColumn = currentLine.Count;
        DoNeededAction ();
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

    private void MovePageDown ()
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
    }

    private void MovePageUp ()
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
    }

    private bool MoveRight ()
    {
        List<Cell> currentLine = GetCurrentLine ();

        if ((ReadOnly ? CurrentColumn + 1 : CurrentColumn) < currentLine.Count)
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

    private void MoveLeftStart ()
    {
        if (_leftColumn > 0)
        {
            SetNeedsDraw ();
        }

        CurrentColumn = 0;
        _leftColumn = 0;
        DoNeededAction ();
    }

    private void MoveTopHome ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveHome ();
    }

    private void MoveTopHomeExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveHome ();
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

    private void MoveWordBackward ()
    {
        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        DoNeededAction ();
    }

    private void MoveWordForward ()
    {
        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        DoNeededAction ();
    }

    #endregion

    #region Process Navigation Methods

    private bool ProcessMoveDown ()
    {
        ResetContinuousFindTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveDown ();
    }

    private void ProcessMoveDownExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveDown ();
    }

    private void ProcessMoveEndOfLine ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveEndOfLine ();
    }

    private void ProcessMoveRightEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveEndOfLine ();
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

    private void ProcessMoveLeftExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveLeft ();
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

    private void ProcessMoveRightExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveRight ();
    }

    private void ProcessMoveLeftStart ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveLeftStart ();
    }

    private void ProcessMoveLeftStartExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveLeftStart ();
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

    private void ProcessMoveUpExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveUp ();
    }

    private void ProcessMoveWordBackward ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveWordBackward ();
    }

    private void ProcessMoveWordBackwardExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveWordBackward ();
    }

    private void ProcessMoveWordForward ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveWordForward ();
    }

    private void ProcessMoveWordForwardExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveWordForward ();
    }

    private void ProcessPageDown ()
    {
        ResetColumnTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MovePageDown ();
    }

    private void ProcessPageDownExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MovePageDown ();
    }

    private void ProcessPageUp ()
    {
        ResetColumnTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MovePageUp ();
    }

    private void ProcessPageUpExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MovePageUp ();
    }

    #endregion

    #region Column Tracking

    // Tries to snap the cursor to the tracking column
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

        AdjustScrollPosition ();
    }

    #endregion


    private void ResetAllTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _columnTrack = -1;
        _continuousFind = false;
    }

    /// <summary>
    ///     INTERNAL: Resets the cursor position and scroll offsets to the beginning of the document (0,0)
    ///     and stops any active text selection.
    /// </summary>
    private void ResetPosition ()
    {
        _topRow = _leftColumn = CurrentRow = CurrentColumn = 0;
        StopSelecting ();
    }
}
