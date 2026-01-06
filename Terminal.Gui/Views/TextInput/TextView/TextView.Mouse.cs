namespace Terminal.Gui.Views;

public partial class TextView
{

    /// <summary>
    ///     Gets or sets whether the word navigation should select only the word itself without spaces around it or with the
    ///     spaces at right.
    ///     Default is <c>false</c> meaning that the spaces at right are included in the selection.
    /// </summary>
    public bool SelectWordOnlyOnDoubleClick { get; set; }
    private bool _isButtonShift;
    private bool _isButtonReleased;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (mouse is { IsSingleDoubleOrTripleClicked: false, IsPressed: false, IsReleased: false, IsWheel: false }
            && !mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport)
            && !mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed | MouseFlags.Shift)
            && !mouse.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked | MouseFlags.Shift)
            && !mouse.Flags.HasFlag (ContextMenu!.MouseFlags))
        {
            return false;
        }

        if (!CanFocus)
        {
            return true;
        }

        if (!HasFocus)
        {
            SetFocus ();
        }

        _continuousFind = false;

        // Give autocomplete first opportunity to respond to mouse clicks
        if (SelectedLength == 0 && Autocomplete.OnMouseEvent (mouse, true))
        {
            return true;
        }

        if (mouse.Flags == MouseFlags.LeftButtonClicked)
        {
            if (_isButtonReleased)
            {
                _isButtonReleased = false;

                if (SelectedLength == 0)
                {
                    StopSelecting ();
                }

                return true;
            }

            if (_shiftSelecting && !_isButtonShift)
            {
                StopSelecting ();
            }

            ProcessMouseClick (mouse, out _);

            if (Used)
            {
                PositionCursor ();
            }
            else
            {
                SetNeedsDraw ();
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (mouse.Flags == MouseFlags.WheeledDown)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_topRow + 1);
        }
        else if (mouse.Flags == MouseFlags.WheeledUp)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_topRow - 1);
        }
        else if (mouse.Flags == MouseFlags.WheeledRight)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_leftColumn + 1, false);
        }
        else if (mouse.Flags == MouseFlags.WheeledLeft)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_leftColumn - 1, false);
        }
        else if (mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport))
        {
            ProcessMouseClick (mouse, out List<Cell> line);
            PositionCursor ();

            if (_model.Count > 0 && _shiftSelecting && IsSelecting)
            {
                if (CurrentRow - _topRow >= Viewport.Height - 1 && _model.Count > _topRow + CurrentRow)
                {
                    ScrollTo (_topRow + Viewport.Height);
                }
                else if (_topRow > 0 && CurrentRow <= _topRow)
                {
                    ScrollTo (_topRow - Viewport.Height);
                }
                else if (mouse.Position!.Value.Y >= Viewport.Height)
                {
                    ScrollTo (_model.Count);
                }
                else if (mouse.Position!.Value.Y < 0 && _topRow > 0)
                {
                    ScrollTo (0);
                }

                if (CurrentColumn - _leftColumn >= Viewport.Width - 1 && line.Count > _leftColumn + CurrentColumn)
                {
                    ScrollTo (_leftColumn + Viewport.Width, false);
                }
                else if (_leftColumn > 0 && CurrentColumn <= _leftColumn)
                {
                    ScrollTo (_leftColumn - Viewport.Width, false);
                }
                else if (mouse.Position!.Value.X >= Viewport.Width)
                {
                    ScrollTo (line.Count, false);
                }
                else if (mouse.Position!.Value.X < 0 && _leftColumn > 0)
                {
                    ScrollTo (0, false);
                }
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed | MouseFlags.Shift))
        {
            if (!_shiftSelecting)
            {
                _isButtonShift = true;
                StartSelecting ();
            }

            ProcessMouseClick (mouse, out _);
            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
        {
            if (_shiftSelecting)
            {
                _clickWithSelecting = true;
                StopSelecting ();
            }

            ProcessMouseClick (mouse, out _);
            PositionCursor ();

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;

            if (App?.Mouse.MouseGrabView is null)
            {
                App?.Mouse.GrabMouse (this);
            }
        }
        else if (mouse.Flags.HasFlag (MouseFlags.LeftButtonReleased))
        {
            _isButtonReleased = true;
            App?.Mouse.UngrabMouse ();
        }
        else if (mouse.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked))
        {
            if (mouse.Flags.HasFlag (MouseFlags.Shift))
            {
                if (!IsSelecting)
                {
                    StartSelecting ();
                }
            }
            else if (IsSelecting)
            {
                StopSelecting ();
            }

            ProcessMouseClick (mouse, out List<Cell> line);

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            (int startCol, int col, int row)? newPos = _model.ProcessDoubleClickSelection (SelectionStartColumn, CurrentColumn, CurrentRow, UseSameRuneTypeForWords, SelectWordOnlyOnDoubleClick);

            if (newPos.HasValue)
            {
                SelectionStartColumn = newPos.Value.startCol;
                CurrentColumn = newPos.Value.col;
                CurrentRow = newPos.Value.row;
            }

            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            SetNeedsDraw ();
        }
        else if (mouse.Flags.HasFlag (MouseFlags.LeftButtonTripleClicked))
        {
            if (IsSelecting)
            {
                StopSelecting ();
            }

            ProcessMouseClick (mouse, out List<Cell> line);
            CurrentColumn = 0;

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            CurrentColumn = line.Count;
            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            SetNeedsDraw ();
        }
        else if (mouse.Flags == ContextMenu!.MouseFlags)
        {
            ShowContextMenu (mouse.ScreenPosition);
        }

        OnUnwrappedCursorPosition ();

        return true;
    }

    private void ProcessMouseClick (Mouse mouse, out List<Cell> line)
    {
        List<Cell>? r = null;

        if (_model.Count > 0)
        {
            int maxCursorPositionableLine = Math.Max (_model.Count - 1 - _topRow, 0);

            if (Math.Max (mouse.Position!.Value.Y, 0) > maxCursorPositionableLine)
            {
                CurrentRow = maxCursorPositionableLine + _topRow;
            }
            else
            {
                CurrentRow = Math.Max (mouse.Position!.Value.Y + _topRow, 0);
            }

            r = GetCurrentLine ();
            int idx = TextModel.GetColFromX (r, _leftColumn, Math.Max (mouse.Position!.Value.X, 0), TabWidth);

            if (idx - _leftColumn >= r.Count)
            {
                CurrentColumn = Math.Max (r.Count - _leftColumn - (ReadOnly ? 1 : 0), 0);
            }
            else
            {
                CurrentColumn = idx + _leftColumn;
            }
        }

        line = r!;
    }
}
