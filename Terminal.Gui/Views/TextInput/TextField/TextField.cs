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
        _selectionAnchor = -1;
        _text = [];

        ReadOnly = false;
        Autocomplete = new TextFieldAutocomplete ();
        Height = Dim.Auto (DimAutoStyle.Text, 1);

        CanFocus = true;
        Used = true;
        MousePositionTracking = true;

        _historyText.ChangeText += HistoryText_ChangeText;

        Initialized += TextField_Initialized;

        CreateCommandsAndBindings ();

        _currentCulture = Thread.CurrentThread.CurrentUICulture;
    }

    private void TextField_Initialized (object? sender, EventArgs e)
    {
        _insertionPoint = Text.GetRuneCount ();

        if (Viewport.Width > 0)
        {
            ScrollOffset = _insertionPoint > Viewport.Width + 1 ? _insertionPoint - Viewport.Width + 1 : 0;
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

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (App?.Mouse.MouseGrabView is { } && App?.Mouse.MouseGrabView == this)
        {
            App?.Mouse.UngrabMouse ();
        }
        UpdateCursor ();
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
