using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>Fully featured multi-line text editor</summary>
/// <remarks>
///     <list type="table">
///         <listheader>
///             <term>Shortcut</term> <description>Action performed</description>
///         </listheader>
///         <item>
///             <term>Left cursor, Control-b</term> <description>Moves the editing point left.</description>
///         </item>
///         <item>
///             <term>Right cursor, Control-f</term> <description>Moves the editing point right.</description>
///         </item>
///         <item>
///             <term>Alt-b</term> <description>Moves one word back.</description>
///         </item>
///         <item>
///             <term>Alt-f</term> <description>Moves one word forward.</description>
///         </item>
///         <item>
///             <term>Up cursor, Control-p</term> <description>Moves the editing point one line up.</description>
///         </item>
///         <item>
///             <term>Down cursor, Control-n</term> <description>Moves the editing point one line down</description>
///         </item>
///         <item>
///             <term>Home key, Control-a</term> <description>Moves the cursor to the beginning of the line.</description>
///         </item>
///         <item>
///             <term>End key, Control-e</term> <description>Moves the cursor to the end of the line.</description>
///         </item>
///         <item>
///             <term>Control-Home</term> <description>Scrolls to the first line and moves the cursor there.</description>
///         </item>
///         <item>
///             <term>Control-End</term> <description>Scrolls to the last line and moves the cursor there.</description>
///         </item>
///         <item>
///             <term>Delete, Control-d</term> <description>Deletes the character in front of the cursor.</description>
///         </item>
///         <item>
///             <term>Backspace</term> <description>Deletes the character behind the cursor.</description>
///         </item>
///         <item>
///             <term>Control-k</term>
///             <description>
///                 Deletes the text until the end of the line and replaces the kill buffer with the deleted text.
///                 You can paste this text in a different place by using Control-y.
///             </description>
///         </item>
///         <item>
///             <term>Control-y</term>
///             <description>Pastes the content of the kill ring into the current position.</description>
///         </item>
///         <item>
///             <term>Alt-d</term>
///             <description>
///                 Deletes the word above the cursor and adds it to the kill ring. You can paste the contents of
///                 the kill ring with Control-y.
///             </description>
///         </item>
///         <item>
///             <term>Control-q</term>
///             <description>
///                 Quotes the next input character, to prevent the normal processing of key handling to take
///                 place.
///             </description>
///         </item>
///     </list>
/// </remarks>
public partial class TextView : View, IDesignable
{
    // The column we are tracking, or -1 if we are not tracking any column
    private string? _currentCaller;
    private CultureInfo? _currentCulture;
    private Dim? _savedHeight;

    /// <summary>
    ///     Initializes a <see cref="TextView"/> on the specified area, with dimensions controlled with the X, Y, Width
    ///     and Height properties.
    /// </summary>
    public TextView ()
    {
        CanFocus = true;
        Used = true;

        // By default, disable hotkeys (in case someone sets Title)
        base.HotKeySpecifier = new ('\xffff');

        _model.LinesLoaded += Model_LinesLoaded!;
        _historyText.ChangeText += HistoryText_ChangeText;

        Initialized += TextView_Initialized!;

        SubViewsLaidOut += TextView_LayoutComplete;

        CreateCommandsAndBindings ();

        _currentCulture = Thread.CurrentThread.CurrentUICulture;
    }

    private void TextView_Initialized (object sender, EventArgs e)
    {
        Autocomplete.HostControl ??= this;

        ContextMenu = CreateContextMenu ();
        App?.Popover?.Register (ContextMenu);
        KeyBindings.Add (ContextMenu.Key, Command.Context);

        OnContentsChanged ();
    }

    private void TextView_LayoutComplete (object? sender, LayoutEventArgs e)
    {
        WrapTextModel ();
        Adjust ();
    }

    /// <summary>Raised when the contents of the <see cref="TextView"/> are changed.</summary>
    /// <remarks>
    ///     Unlike the <see cref="View.TextChanged"/> event, this event is raised whenever the user types or otherwise changes
    ///     the contents of the <see cref="TextView"/>.
    /// </remarks>
    public event EventHandler<ContentsChangedEventArgs>? ContentsChanged;

    /// <summary>
    ///     Called when the contents of the TextView change. E.g. when the user types text or deletes text. Raises the
    ///     <see cref="ContentsChanged"/> event.
    /// </summary>
    public virtual void OnContentsChanged ()
    {
        ContentsChanged?.Invoke (this, new (CurrentRow, CurrentColumn));

        ProcessInheritsPreviousScheme (CurrentRow, CurrentColumn);
        ProcessAutocomplete ();
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (App?.Mouse.MouseGrabView is { } && App?.Mouse.MouseGrabView == this)
        {
            App?.Mouse.UngrabMouse ();
        }
    }

    /// <summary>Positions the cursor on the current row and column</summary>
    public void PositionCursor ()
    {
        ProcessAutocomplete ();

        if (!CanFocus || !Enabled || Driver is null)
        {
            return;
        }

        if (App?.Mouse.MouseGrabView == this && IsSelecting)
        {
            SetNeedsDraw ();
        }

        List<Cell> line = _model.GetLine (CurrentRow);
        var col = 0;

        if (line.Count > 0)
        {
            for (int idx = _leftColumn; idx < line.Count; idx++)
            {
                if (idx >= CurrentColumn)
                {
                    break;
                }

                int cols = line [idx].Grapheme.GetColumns ();

                if (line [idx].Grapheme == "\t")
                {
                    cols += TabWidth + 1;
                }
                else
                {
                    // Ensures that cols less than 0 to be 1 because it will be converted to a printable rune
                    cols = Math.Max (cols, 1);
                }

                if (!TextModel.SetCol (ref col, Viewport.Width, cols))
                {
                    col = CurrentColumn;

                    break;
                }
            }
        }

        int posX = CurrentColumn - _leftColumn;
        int posY = CurrentRow - _topRow;

        if (posX > -1 && col >= posX && posX < Viewport.Width && _topRow <= CurrentRow && posY < Viewport.Height)
        {
            SetCursor (Cursor with
            {
                Position = ViewportToScreen (new Point (col, CurrentRow - _topRow)),
                Shape = CursorShape.Default
            });
        }
        else
        {
            SetCursor (Cursor with { Position = null, Shape = Cursor.Shape });
        }
    }

    private PopoverMenu CreateContextMenu ()
    {
        PopoverMenu menu = new (
                                new List<View>
                                {
                                    new MenuItem (this, Command.SelectAll, Strings.ctxSelectAll),
                                    new MenuItem (this, Command.DeleteAll, Strings.ctxDeleteAll),
                                    new MenuItem (this, Command.Copy, Strings.ctxCopy),
                                    new MenuItem (this, Command.Cut, Strings.ctxCut),
                                    new MenuItem (this, Command.Paste, Strings.ctxPaste),
                                    new MenuItem (this, Command.Undo, Strings.ctxUndo),
                                    new MenuItem (this, Command.Redo, Strings.ctxRedo)
                                });

        menu.KeyChanged += ContextMenu_KeyChanged;

        return menu;
    }

    //
    // Clears the contents of the selected region
    //

    private void ContextMenu_KeyChanged (object? sender, KeyChangedEventArgs e) { KeyBindings.Replace (e.OldKey, e.NewKey); }

    /// <summary>Get the Context Menu.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Text = """
               TextView provides a fully featured multi-line text editor.
               It supports word wrap and history for undo.
               """;

        // This enables AllViews_HasFocus_Changed_Event to pass since it requires
        // tab navigation to work
        AllowsTab = false;

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing && ContextMenu is { })
        {
            ContextMenu.Visible = false;
            ContextMenu.Dispose ();
            ContextMenu = null;
        }

        base.Dispose (disposing);
    }
}
