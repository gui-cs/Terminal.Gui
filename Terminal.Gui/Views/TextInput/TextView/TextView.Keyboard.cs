namespace Terminal.Gui.Views;

public partial class TextView
{
    private bool _enterKeyAddsLine = true;

    /// <summary>
    ///     Gets or sets whether pressing ENTER in a <see cref="TextView"/> adds a new line of text
    ///     invokes the <see cref="View.Accepting"/> event.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property alters <see cref="Multiline"/>.
    ///         If <see cref="EnterKeyAddsLine"/> is set to <see langword="true"/>, then <see cref="Multiline"/> is also set to
    ///         `true` and vice versa.
    ///     </para>
    ///     <para>
    ///         If <see cref="EnterKeyAddsLine"/> is set to <see langword="false"/>, then <see cref="TabKeyAddsTab"/> gets set
    ///         to
    ///         <see langword="false"/>.
    ///     </para>
    /// </remarks>
    public bool EnterKeyAddsLine
    {
        get => _enterKeyAddsLine;
        set
        {
            _enterKeyAddsLine = value;

            if (_enterKeyAddsLine && !_multiline)
            {
                // BUGBUG: Setting properties should not have side effects like this. Multiline and AllowsReturn should be independent.
                Multiline = true;
            }

            if (!_enterKeyAddsLine && _multiline)
            {
                Multiline = false;

                // BUGBUG: Setting properties should not have side effects like this. Multiline and AllowsTab should be independent.
                TabKeyAddsTab = false;
            }

            SetNeedsDraw ();
        }
    }

    private bool _tabKeyAddsTab = true;

    /// <summary>
    ///     Gets or sets whether the <see cref="TextView"/> inserts a tab character (<c>\t</c>) into the text or ignores tab
    ///     input. If
    ///     set to <see langword="false"/> and the user presses the <see cref="Key.Tab"/> the focus will move to the next
    ///     view.
    ///     The default is <see langword="true"/> ; if the user presses <see cref="Key.Tab"/>, a tab character will be inserted
    ///     into the
    ///     text.
    /// </summary>
    /// <remarks>
    ///     This setting has no effect on shift-<see cref="Key.Tab"/> which always moves the focus to the previous view.
    /// </remarks>
    public bool TabKeyAddsTab
    {
        get => _tabKeyAddsTab;
        set
        {
            _tabKeyAddsTab = value;

            if (_tabKeyAddsTab && _tabWidth == 0)
            {
                _tabWidth = 4;
            }

            if (_tabKeyAddsTab && !_multiline)
            {
                Multiline = true;
            }

            if (!_tabKeyAddsTab && _tabWidth > 0)
            {
                _tabWidth = 0;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Provides autocomplete context menu based on suggestions at the current cursor position. Configure
    ///     <see cref="IAutocomplete.SuggestionGenerator"/> to enable this feature
    /// </summary>
    public IAutocomplete Autocomplete { get; protected set; } = new TextViewAutocomplete ();

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        if (!key.IsValid)
        {
            return false;
        }

        // Give autocomplete first opportunity to respond to key presses
        if (SelectedLength == 0 && Autocomplete.Suggestions.Count > 0 && Autocomplete.ProcessKey (key))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key a)
    {
        if (!CanFocus)
        {
            return true;
        }

        ResetColumnTrack ();

        if (a == Autocomplete.Reopen)
        {
            GenerateSuggestions ();

            return Autocomplete.ProcessKey (a);
        }

        // Ignore control characters and other special keys
        if (!a.IsKeyCodeAtoZ && (a.KeyCode < KeyCode.Space || a.KeyCode > KeyCode.CharMask))
        {
            return false;
        }

        InsertText (a);
        DoNeededAction ();

        return true;
    }

    /// <inheritdoc/>
    protected override void OnSuperViewChanged (ValueChangedEventArgs<View?> args)
    {
        base.OnSuperViewChanged (args);

        if (SuperView is { })
        {
            if (Autocomplete.HostControl is null)
            {
                Autocomplete.HostControl = this;
            }
        }
        else
        {
            Autocomplete.HostControl = null;
        }
    }

    private void ProcessAutocomplete ()
    {
        if (_isDrawing)
        {
            return;
        }

        if (_clickWithSelecting)
        {
            _clickWithSelecting = false;

            return;
        }

        if (SelectedLength > 0)
        {
            return;
        }

        // draw autocomplete
        GenerateSuggestions ();

        var renderAt = new Point (Autocomplete.Context.CursorPosition, Autocomplete.PopupInsideContainer ? InsertionPoint.Y + 1 - TopRow : 0);

        Autocomplete.RenderOverlay (renderAt);
    }

    private void GenerateSuggestions ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        int cursorPosition = Math.Min (CurrentColumn, currentLine.Count);

        Autocomplete.Context = new AutocompleteContext (currentLine, cursorPosition, Autocomplete.Context != null ? Autocomplete.Context.Canceled : false);

        Autocomplete.GenerateSuggestions (Autocomplete.Context);
    }
}
