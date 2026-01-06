namespace Terminal.Gui.Views;

/// <summary>Single-line text editor.</summary>
/// <remarks>The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.</remarks>
public partial class TextField : View, IDesignable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TextField"/> class.
    /// </summary>
    public TextField ()
    {
        _historyText = new ();
        _isButtonReleased = true;
        _selectedStart = -1;
        _text = [];

        ReadOnly = false;
        Autocomplete = new TextFieldAutocomplete ();
        Height = Dim.Auto (DimAutoStyle.Text, 1);

        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;
        Used = true;
        MousePositionTracking = true;

        _historyText.ChangeText += HistoryText_ChangeText;

        Initialized += TextField_Initialized;

        CreateCommandsAndBindings ();

        _currentCulture = Thread.CurrentThread.CurrentUICulture;
    }

    private void TextField_Initialized (object? sender, EventArgs e)
    {
        _cursorPosition = Text.GetRuneCount ();

        if (Viewport.Width > 0)
        {
            ScrollOffset = _cursorPosition > Viewport.Width + 1 ? _cursorPosition - Viewport.Width + 1 : 0;
        }

        if (Autocomplete.HostControl is null)
        {
            Autocomplete.HostControl = this;
            Autocomplete.PopupInsideContainer = false;
        }

        CreateContextMenu ();

        if (ContextMenu?.Key is { })
        {
            KeyBindings.Add (ContextMenu.Key, Command.Context);
        }
    }

    /// <summary>Gets or sets whether the text field is read-only.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     Gets or sets whether the text entered is treated as secret (e.g., for passwords).
    ///     <remarks>The displayed text is masked (e.g., with asterisks) when this is set to true.</remarks>
    /// </summary>
    public bool Secret { get; set; }

    /// <summary>
    ///     Converts the logical <see cref="CursorPosition"/> to screen coordinates and positions the terminal cursor.
    /// </summary>
    /// <returns>
    ///     A <see cref="Point"/> representing the cursor's screen position within the viewport, where X is the column
    ///     and Y is always 0 (since TextField is single-line).
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method performs the critical translation between logical text position and physical screen position:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Starts from <see cref="ScrollOffset"/> (first visible character)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Iterates through visible text elements up to <see cref="CursorPosition"/></description>
    ///             </item>
    ///             <item>
    ///                 <description>Accumulates screen column widths (accounting for wide characters)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Calls <see cref="View.Move"/> to position the terminal cursor</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Coordinate spaces:</b>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><see cref="CursorPosition"/>: Logical position (0 to text length)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Returned Point: Screen position within viewport (0 to viewport width)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> For text "Hi世界" with ScrollOffset=0:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>CursorPosition=0 → Screen column 0 (before 'H')</description>
    ///             </item>
    ///             <item>
    ///                 <description>CursorPosition=2 → Screen column 2 (before '世')</description>
    ///             </item>
    ///             <item>
    ///                 <description>CursorPosition=3 → Screen column 4 (before '界', because '世' is 2 columns wide)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This method also triggers <see cref="ProcessAutocomplete"/> to update autocomplete suggestions.
    ///     </para>
    /// </remarks>
    /// <seealso cref="CursorPosition"/>
    /// <seealso cref="ScrollOffset"/>
    public override Point? PositionCursor ()
    {
        ProcessAutocomplete ();

        var col = 0;

        for (int idx = ScrollOffset < 0 ? 0 : ScrollOffset; idx < _text.Count; idx++)
        {
            if (idx == _cursorPosition)
            {
                break;
            }

            int cols = Math.Max (_text [idx].GetColumns (), 1);

            TextModel.SetCol (ref col, Viewport.Width - 1, cols);
        }

        int pos = col + Math.Min (Viewport.X, 0);
        Move (pos, 0);

        return new Point (pos, 0);
    }


    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (App?.Mouse.MouseGrabView is { } && App?.Mouse.MouseGrabView == this)
        {
            App?.Mouse.UngrabMouse ();
        }
    }


    /// <summary>Get the Context Menu for this view.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    private void ContextMenu_KeyChanged (object? sender, KeyChangedEventArgs e) { KeyBindings.Replace (e.OldKey.KeyCode, e.NewKey.KeyCode); }

    private void CreateContextMenu ()
    {
        DisposeContextMenu ();

        PopoverMenu menu = new (
                                new List<MenuItem>
                                {
                                    new (this, Command.SelectAll, Strings.ctxSelectAll),
                                    new (this, Command.DeleteAll, Strings.ctxDeleteAll),
                                    new (this, Command.Copy, Strings.ctxCopy),
                                    new (this, Command.Cut, Strings.ctxCut),
                                    new (this, Command.Paste, Strings.ctxPaste),
                                    new (this, Command.Undo, Strings.ctxUndo),
                                    new (this, Command.Redo, Strings.ctxRedo)
                                });

        HotKeyBindings.Remove (menu.Key);
        HotKeyBindings.Add (menu.Key, Command.Context);
        menu.KeyChanged += ContextMenu_KeyChanged;

        ContextMenu = menu;
        App?.Popover?.Register (ContextMenu);
    }

    private void DisposeContextMenu ()
    {
        if (ContextMenu is { })
        {
            ContextMenu.Visible = false;
            ContextMenu.KeyChanged -= ContextMenu_KeyChanged;
            ContextMenu.Dispose ();
            ContextMenu = null;
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Text = "This is a test.";
        Title = "Caption";

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            DisposeContextMenu ();
        }

        base.Dispose (disposing);
    }

}
