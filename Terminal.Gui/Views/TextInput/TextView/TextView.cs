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
    /// <summary>
    ///     Gets or sets the default cursor style.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static CursorStyle DefaultCursorStyle { get; set; } = CursorStyle.BlinkingBar;

    private CultureInfo? _currentCulture;

    //private Dim? _savedHeight;

    /// <summary>
    ///     Initializes a <see cref="TextView"/> on the specified area, with dimensions controlled with the X, Y, Width
    ///     and Height properties.
    /// </summary>
    public TextView ()
    {
        CanFocus = true;
        Used = true;

        ViewportSettings |= ViewportSettingsFlags.AllowLocationPlusSizeGreaterThanContentSize;

        // By default, disable hotkeys (in case someone sets Title)
        base.HotKeySpecifier = new Rune ('\xffff');

        _model.LinesLoaded += Model_LinesLoaded!;
        _historyText.ChangeText += HistoryText_ChangeText;

        CreateCommandsAndBindings ();

        _currentCulture = Thread.CurrentThread.CurrentUICulture;

        Cursor = new Cursor { Style = DefaultCursorStyle };
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        Autocomplete.HostControl ??= this;

        ContextMenu = CreateContextMenu ();
        App?.Popover?.Register (ContextMenu);
        KeyBindings.Add (ContextMenu.Key, Command.Context);

        UpdateScrollBars ();
        UpdateContentSize ();
        PositionCursor ();
        base.EndInit ();
    }

    /// <inheritdoc/>
    protected override void OnSubViewsLaidOut (LayoutEventArgs args)
    {
        base.OnSubViewsLaidOut (args);
        WrapTextModel ();
        // Don't call AdjustViewport() here - it resets viewport to cursor position,
        // undoing any user scrolling via scrollbar. AdjustViewport() is called when
        // cursor actually moves (InsertionPoint setter, movement commands, etc.)
        UpdateContentSize ();
    }

    // TODO: Upgrade TextView events to use CWP
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
        ContentsChanged?.Invoke (this, new ContentsChangedEventArgs (CurrentRow, CurrentColumn));

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

        if (newHasFocus)
        {
            PositionCursor ();
        }
    }

    /// <summary>Positions the cursor on the current row and column</summary>
    public void PositionCursor ()
    {
        if (!CanFocus || !Enabled || ReadOnly || Driver is null)
        {
            Cursor = Cursor with { Position = null };

            return;
        }

        List<Cell> line = _model.GetLine (CurrentRow);
        var col = 0;

        if (line.Count > 0)
        {
            for (int idx = Viewport.X; idx < line.Count; idx++)
            {
                if (idx >= CurrentColumn)
                {
                    break;
                }

                int cols = line [idx].Grapheme.GetColumns ();

                if (line [idx].Grapheme == "\t")
                {
                    if (TabWidth > 0)
                    {
                        // Calculate columns to next tab stop
                        // Tab stops are at multiples of TabWidth (0, 4, 8, 12, ...)
                        // If we're at visual column col, advance to next tab stop
                        cols = TabWidth - col % TabWidth;
                    }
                    else
                    {
                        // When TabWidth is 0, tabs are invisible (0 columns)
                        cols = 0;
                    }
                }
                else
                {
                    // Ensures that cols less than 0 to be 1 because it will be converted to a printable rune
                    cols = Math.Max (cols, 1);
                }

                if (TextModel.SetCol (ref col, Viewport.Width, cols))
                {
                    continue;
                }
                col = CurrentColumn;

                break;
            }
        }

        int posX = CurrentColumn - Viewport.X;
        int posY = CurrentRow - Viewport.Y;

        if (posX > -1 && col >= posX && posX < Viewport.Width && Viewport.Y <= CurrentRow && posY < Viewport.Height)
        {
            Cursor = Cursor with { Position = ViewportToScreen (new Point (col, CurrentRow - Viewport.Y)) };
        }
        else
        {
            Cursor = Cursor with { Position = null };
        }
    }

    private PopoverMenu CreateContextMenu ()
    {
        PopoverMenu menu = new (new List<View>
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

    private void ContextMenu_KeyChanged (object? sender, KeyChangedEventArgs e) => KeyBindings.Replace (e.OldKey, e.NewKey);

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
        TabKeyAddsTab = false;

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
