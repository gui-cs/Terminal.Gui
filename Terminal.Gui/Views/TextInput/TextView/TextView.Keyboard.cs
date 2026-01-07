using Terminal.Gui.Input;

namespace Terminal.Gui.Views;

public partial class TextView
{
    // BUGBUG: AllowsReturn is mis-named. It should be EnterKeyAccepts.
    private bool _allowsReturn = true;

    /// <summary>
    ///     Gets or sets whether pressing ENTER in a <see cref="TextView"/> creates a new line of text
    ///     in the view or invokes the <see cref="View.Accepting"/> event.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property alters <see cref="Multiline"/>.
    ///         If <see cref="AllowsReturn"/> is set to <see langword="true"/>, then <see cref="Multiline"/> is also set to
    ///         `true` and vice versa.
    ///     </para>
    ///     <para>
    ///         If <see cref="AllowsReturn"/> is set to <see langword="false"/>, then <see cref="AllowsTab"/> gets set to
    ///         <see langword="false"/>.
    ///     </para>
    /// </remarks>
    public bool AllowsReturn
    {
        get => _allowsReturn;
        set
        {
            _allowsReturn = value;

            if (_allowsReturn && !_multiline)
            {
                // BUGBUG: Setting properties should not have side-effects like this. Multiline and AllowsReturn should be independent.
                Multiline = true;
            }

            if (!_allowsReturn && _multiline)
            {
                Multiline = false;

                // BUGBUG: Setting properties should not have side-effects like this. Multiline and AllowsTab should be independent.
                AllowsTab = false;
            }

            SetNeedsDraw ();
        }
    }

    private bool _allowsTab = true;

    /// <summary>
    ///     Gets or sets whether the <see cref="TextView"/> inserts a tab character into the text or ignores tab input. If
    ///     set to `false` and the user presses the tab key (or shift-tab) the focus will move to the next view (or previous
    ///     with shift-tab). The default is `true`; if the user presses the tab key, a tab character will be inserted into the
    ///     text.
    /// </summary>
    public bool AllowsTab
    {
        get => _allowsTab;
        set
        {
            _allowsTab = value;

            if (_allowsTab && _tabWidth == 0)
            {
                _tabWidth = 4;
            }

            if (_allowsTab && !_multiline)
            {
                Multiline = true;
            }

            if (!_allowsTab && _tabWidth > 0)
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

        var renderAt = new Point (
                                  Autocomplete.Context.CursorPosition,
                                  Autocomplete.PopupInsideContainer
                                      ? InsertionPoint.Y + 1 - TopRow
                                      : 0
                                 );

        Autocomplete.RenderOverlay (renderAt);
    }

    private void GenerateSuggestions ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        int cursorPosition = Math.Min (CurrentColumn, currentLine.Count);

        Autocomplete.Context = new (
                                    currentLine,
                                    cursorPosition,
                                    Autocomplete.Context != null
                                        ? Autocomplete.Context.Canceled
                                        : false
                                   );

        Autocomplete.GenerateSuggestions (
                                          Autocomplete.Context
                                         );
    }
}
