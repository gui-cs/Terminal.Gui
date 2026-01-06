namespace Terminal.Gui.Views;

public partial class TextField
{
    /// <summary>
    ///     Positions the cursor based on a mouse event by converting the mouse's screen X coordinate
    ///     to a logical text position.
    /// </summary>
    /// <param name="mouse">The mouse event containing the screen position.</param>
    /// <returns>The resulting <see cref="CursorPosition"/> after positioning.</returns>
    private int PositionCursor (Mouse mouse) { return PositionCursor (TextModel.GetColFromX (_text, ScrollOffset, mouse.Position!.Value.X), false); }

    private bool _isButtonPressed;
    private bool _isButtonReleased;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse ev)
    {
        if (ev is { IsPressed: false, IsReleased: false }
            && !ev.Flags.HasFlag (MouseFlags.PositionReport)
            && !ev.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked)
            && !ev.Flags.HasFlag (MouseFlags.LeftButtonTripleClicked)
            && !ev.Flags.HasFlag (ContextMenu!.MouseFlags))
        {
            return false;
        }

        if (!CanFocus)
        {
            return true;
        }

        if (!HasFocus && ev.Flags != MouseFlags.PositionReport)
        {
            SetFocus ();
        }

        // Give autocomplete first opportunity to respond to mouse clicks
        if (SelectedLength == 0 && Autocomplete.OnMouseEvent (ev, true))
        {
            return true;
        }

        if (ev.Flags == MouseFlags.LeftButtonPressed)
        {
            EnsureHasFocus ();
            PositionCursor (ev);

            if (_isButtonReleased)
            {
                ClearAllSelection ();
            }

            _isButtonReleased = true;
            _isButtonPressed = true;
        }
        else if (ev.Flags == (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport) && _isButtonPressed)
        {
            int x = PositionCursor (ev);
            _isButtonReleased = false;
            PrepareSelection (x);

            if (App?.Mouse.MouseGrabView is null)
            {
                App?.Mouse.GrabMouse (this);
            }
        }
        else if (ev.Flags == MouseFlags.LeftButtonReleased)
        {
            _isButtonReleased = true;
            _isButtonPressed = false;
            App?.Mouse.UngrabMouse ();
        }
        else if (ev.Flags == MouseFlags.LeftButtonDoubleClicked)
        {
            EnsureHasFocus ();
            int x = PositionCursor (ev);
            (int startCol, int col, int row)? newPos = GetModel ().ProcessDoubleClickSelection (x, x, 0, UseSameRuneTypeForWords, SelectWordOnlyOnDoubleClick);

            if (newPos is null)
            {
                return true;
            }

            SelectedStart = newPos.Value.startCol;
            CursorPosition = newPos.Value.col;
        }
        else if (ev.Flags == MouseFlags.LeftButtonTripleClicked)
        {
            EnsureHasFocus ();
            PositionCursor (0);
            ClearAllSelection ();
            PrepareSelection (0, _text.Count);
        }
        else if (ev.Flags == ContextMenu!.MouseFlags)
        {
            PositionCursor (ev);
            ShowContextMenu (false);
        }

        return true;

        void EnsureHasFocus ()
        {
            if (!HasFocus)
            {
                SetFocus ();
            }
        }
    }
}
