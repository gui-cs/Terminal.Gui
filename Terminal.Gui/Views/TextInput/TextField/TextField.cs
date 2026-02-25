namespace Terminal.Gui.Views;

/// <summary>Single-line text editor.</summary>
/// <remarks>The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.</remarks>
public partial class TextField : View, IDesignable, IValue<string>
{
    /// <summary>
    ///     Gets or sets the default cursor style.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static CursorStyle DefaultCursorStyle { get; set; } = CursorStyle.BlinkingBar;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TextField"/> class.
    /// </summary>
    public TextField ()
    {
        _historyText = new HistoryText ();
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

        Cursor = new Cursor { Style = DefaultCursorStyle };
    }

    private void TextField_Initialized (object? sender, EventArgs e)
    {
        _insertionPoint = Text.GetRuneCount ();

        if (Viewport.Width > 0)
        {
            ScrollOffset = _insertionPoint > Viewport.Width + 1 ? _insertionPoint - Viewport.Width + 1 : 0;
        }

        if (Autocomplete.HostControl is { })
        {
            return;
        }
        Autocomplete.HostControl = this;
        Autocomplete.PopupInsideContainer = false;
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
        if (App is { } && App.Mouse.IsGrabbed (this))
        {
            App.Mouse.UngrabMouse ();
        }

        // If gaining focus via keyboard (not mouse), select all text
        if (newHasFocus && !_focusSetByMouse && _text.Count > 0)
        {
            SelectAll ();
        }

        // Reset the flag after handling focus change
        _focusSetByMouse = false;

        UpdateCursor ();

        if (newHasFocus)
        {
            CreateContextMenu ();

            if (ContextMenu?.Key is { })
            {
                KeyBindings.Add (ContextMenu.Key, Command.Context);
            }
        }
        else
        {
            if (ContextMenu?.Key is { })
            {
                KeyBindings.Remove (ContextMenu.Key);
            }
            DisposeContextMenu ();
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewsLaidOut (LayoutEventArgs args)
    {
        base.OnSubViewsLaidOut (args);
        UpdateCursor ();
    }

    /// <summary>Get the Context Menu for this view.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    private void ContextMenu_KeyChanged (object? sender, KeyChangedEventArgs e) => KeyBindings.Replace (e.OldKey.KeyCode, e.NewKey.KeyCode);

    private void CreateContextMenu ()
    {
        DisposeContextMenu ();

        PopoverMenu menu = new (new List<MenuItem>
        {
            new (this, Command.SelectAll),
            new (this, Command.DeleteAll),
            new (this, Command.Copy),
            new (this, Command.Cut),
            new (this, Command.Paste),
            new (this, Command.Undo),
            new (this, Command.Redo)
        })
        {
#if DEBUG
            Id = "textFieldContextMenu"
#endif
        };

        HotKeyBindings.Remove (menu.Key);
        HotKeyBindings.Add (menu.Key, Command.Context);
        menu.KeyChanged += ContextMenu_KeyChanged;

        ContextMenu = menu;
        App?.Popovers?.Register (ContextMenu);
    }

    private void DisposeContextMenu ()
    {
        if (ContextMenu is null)
        {
            return;
        }
        ContextMenu.Visible = false;
        App?.Popovers?.DeRegister (ContextMenu);
        ContextMenu.KeyChanged -= ContextMenu_KeyChanged;
        ContextMenu.Dispose ();
        ContextMenu = null;
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Text = "This is a test.";
        Title = "Caption";

        return true;
    }

    #region IValue<string> Implementation

    /// <summary>
    ///     Gets or sets the value of the <see cref="TextField"/>. This is an alias for <see cref="Text"/>.
    /// </summary>
    /// <remarks>
    ///     This property enables <see cref="TextField"/> to be used with the <see cref="IValue{TValue}"/> pattern
    ///     for generic value access and command propagation.
    /// </remarks>
    public string? Value { get => Text; set => Text = value ?? string.Empty; }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<string?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<string?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Raises the <see cref="ValueChanging"/> event.
    /// </summary>
    private bool RaiseValueChanging (string? currentValue, string? newValue)
    {
        ValueChangingEventArgs<string?> args = new (currentValue, newValue);
        ValueChanging?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>
    ///     Raises the <see cref="ValueChanged"/> event.
    /// </summary>
    private void RaiseValueChanged (string? oldValue, string? newValue)
    {
        ValueChangedEventArgs<string?> args = new (oldValue, newValue);
        ValueChanged?.Invoke (this, args);
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, newValue));
    }

    #endregion

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
