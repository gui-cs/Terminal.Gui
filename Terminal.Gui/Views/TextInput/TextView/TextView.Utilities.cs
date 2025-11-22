namespace Terminal.Gui.Views;

/// <summary>Utility and helper methods for TextView</summary>
public partial class TextView
{
    /// <summary>
    ///     INTERNAL: Adjusts the scroll position and cursor to ensure the cursor is visible in the viewport.
    ///     This method handles both horizontal and vertical scrolling, word wrap considerations, and syncs
    ///     the internal scroll fields with the Viewport property.
    /// </summary>
    private void AdjustScrollPosition ()
    {
        (int width, int height) offB = GetViewportClipping ();
        List<Cell> line = GetCurrentLine ();
        bool need = NeedsDraw || _wrapNeeded || !Used;
        (int size, int length) tSize = TextModel.DisplaySize (line, -1, -1, false, TabWidth);
        (int size, int length) dSize = TextModel.DisplaySize (line, _leftColumn, CurrentColumn, true, TabWidth);

        if (!_wordWrap && CurrentColumn < _leftColumn)
        {
            _leftColumn = CurrentColumn;
            need = true;
        }
        else if (!_wordWrap
                 && (CurrentColumn - _leftColumn + 1 > Viewport.Width + offB.width || dSize.size + 1 >= Viewport.Width + offB.width))
        {
            _leftColumn = TextModel.CalculateLeftColumn (
                                                         line,
                                                         _leftColumn,
                                                         CurrentColumn,
                                                         Viewport.Width + offB.width,
                                                         TabWidth
                                                        );
            need = true;
        }
        else if ((_wordWrap && _leftColumn > 0) || (dSize.size < Viewport.Width + offB.width && tSize.size < Viewport.Width + offB.width))
        {
            if (_leftColumn > 0)
            {
                _leftColumn = 0;
                need = true;
            }
        }

        if (CurrentRow < _topRow)
        {
            _topRow = CurrentRow;
            need = true;
        }
        else if (CurrentRow - _topRow >= Viewport.Height + offB.height)
        {
            _topRow = Math.Min (Math.Max (CurrentRow - Viewport.Height + 1, 0), CurrentRow);
            need = true;
        }
        else if (_topRow > 0 && CurrentRow < _topRow)
        {
            _topRow = Math.Max (_topRow - 1, 0);
            need = true;
        }

        // Sync Viewport with the internal scroll position
        if (IsInitialized && (_leftColumn != Viewport.X || _topRow != Viewport.Y))
        {
            Viewport = new Rectangle (_leftColumn, _topRow, Viewport.Width, Viewport.Height);
        }

        if (need)
        {
            if (_wrapNeeded)
            {
                WrapTextModel ();
                _wrapNeeded = false;
            }

            SetNeedsDraw ();
        }
        else
        {
            if (IsInitialized)
            {
                PositionCursor ();
            }
        }

        OnUnwrappedCursorPosition ();
    }

    /// <summary>
    ///     INTERNAL: Determines if a redraw is needed based on selection state, word wrap needs, and Used flag.
    ///     If a redraw is needed, calls <see cref="AdjustScrollPosition"/>; otherwise positions the cursor and updates
    ///     the unwrapped cursor position.
    /// </summary>
    private void DoNeededAction ()
    {
        if (!NeedsDraw && (IsSelecting || _wrapNeeded || !Used))
        {
            SetNeedsDraw ();
        }

        if (NeedsDraw)
        {
            AdjustScrollPosition ();
        }
        else
        {
            PositionCursor ();
            OnUnwrappedCursorPosition ();
        }
    }

    /// <summary>
    ///     INTERNAL: Calculates the viewport clipping caused by the view extending beyond the SuperView's boundaries.
    ///     Returns negative width and height offsets when the viewport extends beyond the SuperView, representing
    ///     how much of the viewport is clipped.
    /// </summary>
    /// <returns>A tuple containing the width and height clipping offsets (negative when clipped).</returns>
    private (int width, int height) GetViewportClipping ()
    {
        var w = 0;
        var h = 0;

        if (SuperView?.Viewport.Right - Viewport.Right < 0)
        {
            w = SuperView!.Viewport.Right - Viewport.Right - 1;
        }

        if (SuperView?.Viewport.Bottom - Viewport.Bottom < 0)
        {
            h = SuperView!.Viewport.Bottom - Viewport.Bottom - 1;
        }

        return (w, h);
    }

    /// <summary>
    ///     INTERNAL: Updates the content size based on the text model dimensions.
    ///     When word wrap is enabled, content width equals viewport width.
    ///     Otherwise, calculates the maximum line width from the entire text model.
    ///     Content height is always the number of lines in the model.
    /// </summary>
    private void UpdateContentSize ()
    {
        int contentHeight = Math.Max (_model.Count, 1);

        // For horizontal size: if word wrap is enabled, content width equals viewport width
        // Otherwise, calculate the maximum line width (but only if we have a reasonable viewport)
        int contentWidth;

        if (_wordWrap)
        {
            // Word wrap: content width follows viewport width
            contentWidth = Math.Max (Viewport.Width, 1);
        }
        else
        {
            // No word wrap: calculate max line width
            // Cache the current value to avoid recalculating on every call
            contentWidth = Math.Max (_model.GetMaxVisibleLine (0, _model.Count, TabWidth), 1);
        }

        SetContentSize (new Size (contentWidth, contentHeight));
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
