namespace Terminal.Gui.Views;

/// <summary>Find and Replace functionality</summary>
public partial class TextView
{
    #region Public Find/Replace Methods

    /// <summary>Find the next text based on the match case with the option to replace it.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="gaveFullTurn"><c>true</c>If all the text was forward searched.<c>false</c>otherwise.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <param name="replace"><c>true</c>If is replacing.<c>false</c>otherwise.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool FindNextText (
        string textToFind,
        out bool gaveFullTurn,
        bool matchCase = false,
        bool matchWholeWord = false,
        string? textToReplace = null,
        bool replace = false
    )
    {
        if (_model.Count == 0)
        {
            gaveFullTurn = false;

            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos =
            _model.FindNextText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

        return SetFoundText (textToFind, foundPos, textToReplace, replace);
    }

    /// <summary>Find the previous text based on the match case with the option to replace it.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="gaveFullTurn"><c>true</c>If all the text was backward searched.<c>false</c>otherwise.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <param name="replace"><c>true</c>If the text was found.<c>false</c>otherwise.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool FindPreviousText (
        string textToFind,
        out bool gaveFullTurn,
        bool matchCase = false,
        bool matchWholeWord = false,
        string? textToReplace = null,
        bool replace = false
    )
    {
        if (_model.Count == 0)
        {
            gaveFullTurn = false;

            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos =
            _model.FindPreviousText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

        return SetFoundText (textToFind, foundPos, textToReplace, replace);
    }

    /// <summary>Reset the flag to stop continuous find.</summary>
    public void FindTextChanged () { _continuousFind = false; }

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

    #endregion

    #region Private Find Helper Methods

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
                AdjustScrollPosition ();
                ClearSelectedRegion ();
                InsertAllText (textToReplace!);
                StartSelecting ();
                _selectionStartColumn = CurrentColumn - textToReplace!.GetRuneCount ();
            }
            else
            {
                UpdateWrapModel ();
                SetNeedsDraw ();
                AdjustScrollPosition ();
            }

            _continuousFind = true;

            return foundPos.found;
        }

        UpdateWrapModel ();
        _continuousFind = false;

        return foundPos.found;
    }

    private IEnumerable<(int col, int row, Cell rune)> ForwardIterator (int col, int row)
    {
        if (col < 0 || row < 0)
        {
            yield break;
        }

        if (row >= _model.Count)
        {
            yield break;
        }

        List<Cell> line = GetCurrentLine ();

        if (col >= line.Count)
        {
            yield break;
        }

        while (row < _model.Count)
        {
            for (int c = col; c < line.Count; c++)
            {
                yield return (c, row, line [c]);
            }

            col = 0;
            row++;
            line = GetCurrentLine ();
        }
    }

    #endregion
}
