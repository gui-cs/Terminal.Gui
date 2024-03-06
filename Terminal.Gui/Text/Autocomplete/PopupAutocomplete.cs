namespace Terminal.Gui;

/// <summary>
///     Renders an overlay on another view at a given point that allows selecting from a range of 'autocomplete'
///     options.
/// </summary>
public abstract partial class PopupAutocomplete : AutocompleteBase
{
    private bool closed;
    private ColorScheme colorScheme;
    private View hostControl;
    private View top, popup;
    private int toRenderLength;

    /// <summary>Creates a new instance of the <see cref="PopupAutocomplete"/> class.</summary>
    public PopupAutocomplete () { PopupInsideContainer = true; }

    /// <summary>
    ///     The colors to use to render the overlay. Accessing this property before the Application has been initialized
    ///     will cause an error
    /// </summary>
    public override ColorScheme ColorScheme
    {
        get
        {
            if (colorScheme is null)
            {
                colorScheme = Colors.ColorSchemes ["Menu"];
            }

            return colorScheme;
        }
        set => colorScheme = value;
    }

    /// <summary>The host control to handle.</summary>
    public override View HostControl
    {
        get => hostControl;
        set
        {
            hostControl = value;
            top = hostControl.SuperView;

            if (top is { })
            {
                top.DrawContent += Top_DrawContent;
                top.DrawContentComplete += Top_DrawContentComplete;
                top.Removed += Top_Removed;
            }
        }
    }

    /// <summary>
    ///     When more suggestions are available than can be rendered the user can scroll down the dropdown list. This
    ///     indicates how far down they have gone
    /// </summary>
    public virtual int ScrollOffset { get; set; }

#nullable enable
    private Point? LastPopupPos { get; set; }
#nullable restore

    /// <inheritdoc/>
    public override void EnsureSelectedIdxIsValid ()
    {
        base.EnsureSelectedIdxIsValid ();

        // if user moved selection up off top of current scroll window
        if (SelectedIdx < ScrollOffset)
        {
            ScrollOffset = SelectedIdx;
        }

        // if user moved selection down past bottom of current scroll window
        while (toRenderLength > 0 && SelectedIdx >= ScrollOffset + toRenderLength)
        {
            ScrollOffset++;
        }
    }

    /// <summary>
    ///     Handle mouse events before <see cref="HostControl"/> e.g. to make mouse events like report/click apply to the
    ///     autocomplete control instead of changing the cursor position in the underlying text view.
    /// </summary>
    /// <param name="me">The mouse event.</param>
    /// <param name="fromHost">If was called from the popup or from the host.</param>
    /// <returns><c>true</c>if the mouse can be handled <c>false</c>otherwise.</returns>
    public override bool OnMouseEvent (MouseEvent me, bool fromHost = false)
    {
        if (fromHost)
        {
            if (!Visible)
            {
                return false;
            }

            // TODO: Revisit this
            //GenerateSuggestions ();

            if (Visible && Suggestions.Count == 0)
            {
                Visible = false;
                HostControl?.SetNeedsDisplay ();

                return true;
            }

            if (!Visible && Suggestions.Count > 0)
            {
                Visible = true;
                HostControl?.SetNeedsDisplay ();
                Application.UngrabMouse ();

                return false;
            }

            // not in the popup
            if (Visible && HostControl is { })
            {
                Visible = false;
                closed = false;
            }

            HostControl?.SetNeedsDisplay ();

            return false;
        }

        if (popup is null || Suggestions.Count == 0)
        {
            ManipulatePopup ();

            return false;
        }

        if (me.Flags == MouseFlags.ReportMousePosition)
        {
            RenderSelectedIdxByMouse (me);

            return true;
        }

        if (me.Flags == MouseFlags.Button1Clicked)
        {
            SelectedIdx = me.Y - ScrollOffset;

            return Select ();
        }

        if (me.Flags == MouseFlags.WheeledDown)
        {
            MoveDown ();

            return true;
        }

        if (me.Flags == MouseFlags.WheeledUp)
        {
            MoveUp ();

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Handle key events before <see cref="HostControl"/> e.g. to make key events like up/down apply to the
    ///     autocomplete control instead of changing the cursor position in the underlying text view.
    /// </summary>
    /// <param name="key">The key event.</param>
    /// <returns><c>true</c>if the key can be handled <c>false</c>otherwise.</returns>
    public override bool ProcessKey (Key key)
    {
        if (SuggestionGenerator.IsWordChar ((Rune)key))
        {
            Visible = true;
            ManipulatePopup ();
            closed = false;

            return false;
        }

        if (key == Reopen)
        {
            Context.Canceled = false;

            return ReopenSuggestions ();
        }

        if (closed || Suggestions.Count == 0)
        {
            Visible = false;

            if (!closed)
            {
                Close ();
            }

            return false;
        }

        if (key == Key.CursorDown)
        {
            MoveDown ();

            return true;
        }

        if (key == Key.CursorUp)
        {
            MoveUp ();

            return true;
        }

        // TODO : Revisit this
        /*if (a.ConsoleDriverKey == Key.CursorLeft || a.ConsoleDriverKey == Key.CursorRight) {
            GenerateSuggestions (a.ConsoleDriverKey == Key.CursorLeft ? -1 : 1);
            if (Suggestions.Count == 0) {
                Visible = false;
                if (!closed) {
                    Close ();
                }
            }
            return false;
        }*/

        if (key == SelectionKey)
        {
            return Select ();
        }

        if (key == CloseKey)
        {
            Close ();
            Context.Canceled = true;

            return true;
        }

        return false;
    }

    /// <summary>Renders the autocomplete dialog inside or outside the given <see cref="HostControl"/> at the given point.</summary>
    /// <param name="renderAt"></param>
    public override void RenderOverlay (Point renderAt)
    {
        if (!Context.Canceled && Suggestions.Count > 0 && !Visible && HostControl?.HasFocus == true)
        {
            ProcessKey (new Key (Suggestions [0].Title [0]));
        }
        else if (!Visible || HostControl?.HasFocus == false || Suggestions.Count == 0)
        {
            LastPopupPos = null;
            Visible = false;

            if (Suggestions.Count == 0)
            {
                Context.Canceled = false;
            }

            return;
        }

        LastPopupPos = renderAt;

        int height, width;

        if (PopupInsideContainer)
        {
            // don't overspill vertically
            height = Math.Min (HostControl.ContentArea.Height - renderAt.Y, MaxHeight);

            // There is no space below, lets see if can popup on top
            if (height < Suggestions.Count && HostControl.ContentArea.Height - renderAt.Y >= height)
            {
                // Verifies that the upper limit available is greater than the lower limit
                if (renderAt.Y > HostControl.ContentArea.Height - renderAt.Y)
                {
                    renderAt.Y = Math.Max (renderAt.Y - Math.Min (Suggestions.Count + 1, MaxHeight + 1), 0);
                    height = Math.Min (Math.Min (Suggestions.Count, MaxHeight), LastPopupPos.Value.Y - 1);
                }
            }
        }
        else
        {
            // don't overspill vertically
            height = Math.Min (Math.Min (top.ContentArea.Height - HostControl.Frame.Bottom, MaxHeight), Suggestions.Count);

            // There is no space below, lets see if can popup on top
            if (height < Suggestions.Count && HostControl.Frame.Y - top.Frame.Y >= height)
            {
                // Verifies that the upper limit available is greater than the lower limit
                if (HostControl.Frame.Y > top.ContentArea.Height - HostControl.Frame.Y)
                {
                    renderAt.Y = Math.Max (HostControl.Frame.Y - Math.Min (Suggestions.Count, MaxHeight), 0);
                    height = Math.Min (Math.Min (Suggestions.Count, MaxHeight), HostControl.Frame.Y);
                }
            }
            else
            {
                renderAt.Y = HostControl.Frame.Bottom;
            }
        }

        if (ScrollOffset > Suggestions.Count - height)
        {
            ScrollOffset = 0;
        }

        Suggestion [] toRender = Suggestions.Skip (ScrollOffset).Take (height).ToArray ();
        toRenderLength = toRender.Length;

        if (toRender.Length == 0)
        {
            return;
        }

        width = Math.Min (MaxWidth, toRender.Max (s => s.Title.Length));

        if (PopupInsideContainer)
        {
            // don't overspill horizontally, let's see if can be displayed on the left
            if (width > HostControl.ContentArea.Width - renderAt.X)
            {
                // Verifies that the left limit available is greater than the right limit
                if (renderAt.X > HostControl.ContentArea.Width - renderAt.X)
                {
                    renderAt.X -= Math.Min (width, LastPopupPos.Value.X);
                    width = Math.Min (width, LastPopupPos.Value.X);
                }
                else
                {
                    width = Math.Min (width, HostControl.ContentArea.Width - renderAt.X);
                }
            }
        }
        else
        {
            // don't overspill horizontally, let's see if can be displayed on the left
            if (width > top.ContentArea.Width - (renderAt.X + HostControl.Frame.X))
            {
                // Verifies that the left limit available is greater than the right limit
                if (renderAt.X + HostControl.Frame.X > top.ContentArea.Width - (renderAt.X + HostControl.Frame.X))
                {
                    renderAt.X -= Math.Min (width, LastPopupPos.Value.X);
                    width = Math.Min (width, LastPopupPos.Value.X);
                }
                else
                {
                    width = Math.Min (width, top.ContentArea.Width - renderAt.X);
                }
            }
        }

        if (PopupInsideContainer)
        {
            popup.Frame = new (
                               new (HostControl.Frame.X + renderAt.X, HostControl.Frame.Y + renderAt.Y),
                               new (width, height)
                              );
        }
        else
        {
            popup.Frame = new (
                               renderAt with { X = HostControl.Frame.X + renderAt.X },
                               new (width, height)
                              );
        }

        popup.Move (0, 0);

        for (var i = 0; i < toRender.Length; i++)
        {
            if (i == SelectedIdx - ScrollOffset)
            {
                Application.Driver.SetAttribute (ColorScheme.Focus);
            }
            else
            {
                Application.Driver.SetAttribute (ColorScheme.Normal);
            }

            popup.Move (0, i);

            string text = TextFormatter.ClipOrPad (toRender [i].Title, width);

            Application.Driver.AddStr (text);
        }
    }

    /// <summary>
    ///     Closes the Autocomplete context menu if it is showing and <see cref="IAutocomplete.ClearSuggestions"/>
    /// </summary>
    protected void Close ()
    {
        ClearSuggestions ();
        Visible = false;
        closed = true;
        HostControl?.SetNeedsDisplay ();
        ManipulatePopup ();
    }

    /// <summary>Deletes the text backwards before insert the selected text in the <see cref="HostControl"/>.</summary>
    protected abstract void DeleteTextBackwards ();

    /// <summary>
    ///     Called when the user confirms a selection at the current cursor location in the <see cref="HostControl"/>. The
    ///     <paramref name="accepted"/> string is the full autocomplete word to be inserted. Typically a host will have to
    ///     remove some characters such that the <paramref name="accepted"/> string completes the word instead of simply being
    ///     appended.
    /// </summary>
    /// <param name="accepted"></param>
    /// <returns>True if the insertion was possible otherwise false</returns>
    protected virtual bool InsertSelection (Suggestion accepted)
    {
        SetCursorPosition (Context.CursorPosition + accepted.Remove);

        // delete the text
        for (var i = 0; i < accepted.Remove; i++)
        {
            DeleteTextBackwards ();
        }

        InsertText (accepted.Replacement);

        return true;
    }

    /// <summary>Insert the selected text in the <see cref="HostControl"/>.</summary>
    /// <param name="accepted"></param>
    protected abstract void InsertText (string accepted);

    /// <summary>Moves the selection in the Autocomplete context menu down one</summary>
    protected void MoveDown ()
    {
        SelectedIdx++;

        if (SelectedIdx > Suggestions.Count - 1)
        {
            SelectedIdx = 0;
        }

        EnsureSelectedIdxIsValid ();
        HostControl?.SetNeedsDisplay ();
    }

    /// <summary>Moves the selection in the Autocomplete context menu up one</summary>
    protected void MoveUp ()
    {
        SelectedIdx--;

        if (SelectedIdx < 0)
        {
            SelectedIdx = Suggestions.Count - 1;
        }

        EnsureSelectedIdxIsValid ();
        HostControl?.SetNeedsDisplay ();
    }

    /// <summary>Render the current selection in the Autocomplete context menu by the mouse reporting.</summary>
    /// <param name="me"></param>
    protected void RenderSelectedIdxByMouse (MouseEvent me)
    {
        if (SelectedIdx != me.Y - ScrollOffset)
        {
            SelectedIdx = me.Y - ScrollOffset;

            if (LastPopupPos is { })
            {
                RenderOverlay ((Point)LastPopupPos);
            }
        }
    }

    /// <summary>Reopen the popup after it has been closed.</summary>
    /// <returns></returns>
    protected bool ReopenSuggestions ()
    {
        // TODO: Revisit
        //GenerateSuggestions ();

        if (Suggestions.Count > 0)
        {
            Visible = true;
            closed = false;
            HostControl?.SetNeedsDisplay ();

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Completes the autocomplete selection process. Called when user hits the
    ///     <see cref="IAutocomplete.SelectionKey"/>.
    /// </summary>
    /// <returns></returns>
    protected bool Select ()
    {
        if (SelectedIdx >= 0 && SelectedIdx < Suggestions.Count)
        {
            Suggestion accepted = Suggestions [SelectedIdx];

            return InsertSelection (accepted);
        }

        return false;
    }

    /// <summary>Set the cursor position in the <see cref="HostControl"/>.</summary>
    /// <param name="column"></param>
    protected abstract void SetCursorPosition (int column);

    private void ManipulatePopup ()
    {
        if (Visible && popup is null)
        {
            popup = new Popup (this) { Frame = Rectangle.Empty };
            top?.Add (popup);
        }

        if (!Visible && popup is { })
        {
            top?.Remove (popup);
            popup.Dispose ();
            popup = null;
        }
    }

    private void Top_DrawContent (object sender, DrawEventArgs e)
    {
        if (!closed)
        {
            ReopenSuggestions ();
        }

        ManipulatePopup ();

        if (Visible)
        {
            top.BringSubviewToFront (popup);
        }
    }

    private void Top_DrawContentComplete (object sender, DrawEventArgs e) { ManipulatePopup (); }

    private void Top_Removed (object sender, SuperViewChangedEventArgs e)
    {
        Visible = false;
        ManipulatePopup ();
    }
}
