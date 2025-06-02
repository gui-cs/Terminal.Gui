#nullable enable

namespace Terminal.Gui.Views;

/// <summary>Displays a list of mutually-exclusive items. Each items can have its own hotkey.</summary>
public class RadioGroup : View, IDesignable, IOrientation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RadioGroup"/> class.
    /// </summary>
    public RadioGroup ()
    {
        CanFocus = true;

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        // Select (Space key or mouse click) - The default implementation sets focus. RadioGroup does not.
        AddCommand (Command.Select, HandleSelectCommand);

        // Accept (Enter key or DoubleClick) - Raise Accept event - DO NOT advance state
        AddCommand (Command.Accept, HandleAcceptCommand);

        // Hotkey - ctx may indicate a radio item hotkey was pressed. Behavior depends on HasFocus
        //          If HasFocus and it's this.HotKey invoke Select command - DO NOT raise Accept
        //          If it's a radio item HotKey select that item and raise Selected event - DO NOT raise Accept
        //          If nothing is selected, select first and raise Selected event - DO NOT raise Accept
        AddCommand (Command.HotKey, HandleHotKeyCommand);

        AddCommand (Command.Up, () => HasFocus && MoveUpLeft ());
        AddCommand (Command.Down, () => HasFocus && MoveDownRight ());
        AddCommand (Command.Start, () => HasFocus && MoveHome ());
        AddCommand (Command.End, () => HasFocus && MoveEnd ());

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Vertical;

        SetupKeyBindings ();

        // By default, single click is already bound to Command.Select
        MouseBindings.Add (MouseFlags.Button1DoubleClicked, Command.Accept);

        SubViewLayout += RadioGroup_LayoutStarted;
    }

    private bool? HandleHotKeyCommand (ICommandContext? ctx)
    {
        // If the command did not come from a keyboard event, ignore it
        if (ctx is not CommandContext<KeyBinding> keyCommandContext)
        {
            return false;
        }

        var item = keyCommandContext.Binding.Data as int?;

        if (HasFocus)
        {
            if (item is null || HotKey == keyCommandContext.Binding.Key?.NoAlt.NoCtrl.NoShift!)
            {
                // It's this.HotKey OR Another View (Label?) forwarded the hotkey command to us - Act just like `Space` (Select)
                return InvokeCommand (Command.Select);
            }
        }

        if (item is { } && item < _radioLabels.Count)
        {
            if (item.Value == SelectedItem)
            {
                return true;
            }

            // If a RadioItem.HotKey is pressed we always set the selected item - never SetFocus
            bool selectedItemChanged = ChangeSelectedItem (item.Value);

            if (selectedItemChanged)
            {
                // Doesn't matter if it's handled
                RaiseSelecting (ctx);

                return true;
            }

            return false;
        }

        if (SelectedItem == -1 && ChangeSelectedItem (0))
        {
            if (RaiseSelecting (ctx) == true)
            {
                return true;
            }

            return false;
        }

        if (RaiseHandlingHotKey () == true)
        {
            return true;
        }

        ;

        // Default Command.Hotkey sets focus
        SetFocus ();

        return true;
    }

    private bool? HandleAcceptCommand (ICommandContext? ctx)
    {
        if (!DoubleClickAccepts
            && ctx is CommandContext<MouseBinding> mouseCommandContext
            && mouseCommandContext.Binding.MouseEventArgs!.Flags.HasFlag (MouseFlags.Button1DoubleClicked))
        {
            return false;
        }

        return RaiseAccepting (ctx);
    }

    private bool? HandleSelectCommand (ICommandContext? ctx)
    {
        if (ctx is CommandContext<MouseBinding> mouseCommandContext
            && mouseCommandContext.Binding.MouseEventArgs!.Flags.HasFlag (MouseFlags.Button1Clicked))
        {
            int viewportX = mouseCommandContext.Binding.MouseEventArgs.Position.X;
            int viewportY = mouseCommandContext.Binding.MouseEventArgs.Position.Y;

            int pos = Orientation == Orientation.Horizontal ? viewportX : viewportY;

            int rCount = Orientation == Orientation.Horizontal
                             ? _horizontal!.Last ().pos + _horizontal!.Last ().length
                             : _radioLabels.Count;

            if (pos < rCount)
            {
                int c = Orientation == Orientation.Horizontal
                            ? _horizontal!.FindIndex (x => x.pos <= viewportX && x.pos + x.length - 2 >= viewportX)
                            : viewportY;

                if (c > -1)
                {
                    // Just like the user pressing the items' hotkey
                    return InvokeCommand (Command.HotKey, new KeyBinding ([Command.HotKey], this, c)) == true;
                }
            }

            return false;
        }

        var cursorChanged = false;

        if (SelectedItem == Cursor)
        {
            cursorChanged = MoveDownRight ();

            if (!cursorChanged)
            {
                cursorChanged = MoveHome ();
            }
        }

        var selectedItemChanged = false;

        if (SelectedItem != Cursor)
        {
            selectedItemChanged = ChangeSelectedItem (Cursor);
        }

        if (cursorChanged || selectedItemChanged)
        {
            if (RaiseSelecting (ctx) == true)
            {
                return true;
            }
        }

        return cursorChanged || selectedItemChanged;
    }

    // TODO: Fix InvertColorsOnPress - only highlight the selected item

    private void SetupKeyBindings ()
    {
        // Default keybindings for this view
        if (Orientation == Orientation.Vertical)
        {
            KeyBindings.Remove (Key.CursorUp);
            KeyBindings.Add (Key.CursorUp, Command.Up);
            KeyBindings.Remove (Key.CursorDown);
            KeyBindings.Add (Key.CursorDown, Command.Down);
        }
        else
        {
            KeyBindings.Remove (Key.CursorLeft);
            KeyBindings.Add (Key.CursorLeft, Command.Up);
            KeyBindings.Remove (Key.CursorRight);
            KeyBindings.Add (Key.CursorRight, Command.Down);
        }

        KeyBindings.Remove (Key.Home);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Remove (Key.End);
        KeyBindings.Add (Key.End, Command.End);
    }

    /// <summary>
    ///     Gets or sets whether double-clicking on a Radio Item will cause the <see cref="View.Accepting"/> event to be
    ///     raised.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see langword="false"/> and Accept is not handled, the Accept event on the <see cref="View.SuperView"/> will
    ///         be raised. The default is
    ///         <see langword="true"/>.
    ///     </para>
    /// </remarks>
    public bool DoubleClickAccepts { get; set; } = true;

    private List<(int pos, int length)>? _horizontal;
    private int _horizontalSpace = 2;

    /// <summary>
    ///     Gets or sets the horizontal space for this <see cref="RadioGroup"/> if the <see cref="Orientation"/> is
    ///     <see cref="Orientation.Horizontal"/>
    /// </summary>
    public int HorizontalSpace
    {
        get => _horizontalSpace;
        set
        {
            if (_horizontalSpace != value && Orientation == Orientation.Horizontal)
            {
                _horizontalSpace = value;
                UpdateTextFormatterText ();
                SetContentSize ();
            }
        }
    }

    /// <summary>
    ///     If <see langword="true"/> the <see cref="RadioLabels"/> will each be automatically assigned a hotkey.
    ///     <see cref="UsedHotKeys"/> will be used to ensure unique keys are assigned. Set <see cref="UsedHotKeys"/>
    ///     before setting <see cref="RadioLabels"/> with any hotkeys that may conflict with other Views.
    /// </summary>
    public bool AssignHotKeysToRadioLabels { get; set; }

    /// <summary>
    ///     Gets the list of hotkeys already used by <see cref="RadioLabels"/> or that should not be used if
    ///     <see cref="AssignHotKeysToRadioLabels"/>
    ///     is enabled.
    /// </summary>
    public List<Key> UsedHotKeys { get; } = [];

    private readonly List<string> _radioLabels = [];

    /// <summary>
    ///     The radio labels to display. A <see cref="Command.HotKey"/> key binding will be added for each label enabling the
    ///     user to select
    ///     and/or focus the radio label using the keyboard. See <see cref="View.HotKey"/> for details on how HotKeys work.
    /// </summary>
    /// <value>The radio labels.</value>
    public string [] RadioLabels
    {
        get => _radioLabels.ToArray ();
        set
        {
            // Remove old hot key bindings
            foreach (string label in _radioLabels)
            {
                if (TextFormatter.FindHotKey (label, HotKeySpecifier, out _, out Key hotKey))
                {
                    AddKeyBindingsForHotKey (hotKey, Key.Empty);
                }
            }

            _radioLabels.Clear ();

            // Pick a unique hotkey for each radio label
            for (var labelIndex = 0; labelIndex < value.Length; labelIndex++)
            {
                string name = value [labelIndex];
                string? nameWithHotKey = name;

                if (AssignHotKeysToRadioLabels)
                {
                    // Find the first char in label that is [a-z], [A-Z], or [0-9]
                    for (var i = 0; i < name.Length; i++)
                    {
                        char c = char.ToLowerInvariant (name [i]);
                        if (UsedHotKeys.Contains (new (c)) || !char.IsAsciiLetterOrDigit (c))
                        {
                            continue;
                        }

                        if (char.IsAsciiLetterOrDigit (c))
                        {
                            char? hotChar = c;
                            nameWithHotKey = name.Insert (i, HotKeySpecifier.ToString ());
                            UsedHotKeys.Add (new (hotChar));

                            break;
                        }
                    }
                }

                _radioLabels.Add (nameWithHotKey);

                if (TextFormatter.FindHotKey (nameWithHotKey, HotKeySpecifier, out _, out Key hotKey))
                {
                    AddKeyBindingsForHotKey (Key.Empty, hotKey, labelIndex);
                }
            }

            SelectedItem = 0;
            SetContentSize ();
        }
    }

    private int _selected;

    /// <summary>Gets or sets the selected radio label index.</summary>
    /// <value>The index. -1 if no item is selected.</value>
    public int SelectedItem
    {
        get => _selected;
        set => ChangeSelectedItem (value);
    }

    /// <summary>
    ///     INTERNAL Sets the selected item.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>
    ///     <see langword="true"/> if the selected item changed.
    /// </returns>
    private bool ChangeSelectedItem (int value)
    {
        if (_selected == value || value > _radioLabels.Count - 1)
        {
            return false;
        }

        int savedSelected = _selected;
        _selected = value;
        Cursor = Math.Max (_selected, 0);

        OnSelectedItemChanged (value, SelectedItem);
        SelectedItemChanged?.Invoke (this, new (SelectedItem, savedSelected));

        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        SetAttribute (GetAttributeForRole (VisualRole.Normal));

        for (var i = 0; i < _radioLabels.Count; i++)
        {
            switch (Orientation)
            {
                case Orientation.Vertical:
                    Move (0, i);

                    break;
                case Orientation.Horizontal:
                    Move (_horizontal! [i].pos, 0);

                    break;
            }

            string rl = _radioLabels [i];
            SetAttribute (GetAttributeForRole (VisualRole.Normal));
            Driver?.AddStr ($"{(i == _selected ? Glyphs.Selected : Glyphs.UnSelected)} ");
            TextFormatter.FindHotKey (rl, HotKeySpecifier, out int hotPos, out Key hotKey);

            if (hotPos != -1 && hotKey != Key.Empty)
            {
                Rune [] rlRunes = rl.ToRunes ();

                for (var j = 0; j < rlRunes.Length; j++)
                {
                    Rune rune = rlRunes [j];

                    if (j == hotPos && i == Cursor)
                    {
                        SetAttribute (HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal));
                    }
                    else if (j == hotPos && i != Cursor)
                    {
                        SetAttribute (GetAttributeForRole (VisualRole.HotNormal));
                    }
                    else if (HasFocus && i == Cursor)
                    {
                        SetAttribute (GetAttributeForRole (VisualRole.Focus));
                    }

                    if (rune == HotKeySpecifier && j + 1 < rlRunes.Length)
                    {
                        j++;
                        rune = rlRunes [j];

                        if (i == Cursor)
                        {
                            SetAttribute (HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal));
                        }
                        else if (i != Cursor)
                        {
                            SetAttribute (GetAttributeForRole (VisualRole.HotNormal));
                        }
                    }

                    Application.Driver?.AddRune (rune);
                    SetAttribute (GetAttributeForRole (VisualRole.Normal));
                }
            }
            else
            {
                DrawHotString (rl, HasFocus && i == Cursor);
            }
        }

        return true;
    }

    #region IOrientation

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="RadioGroup"/>. The default is
    ///     <see cref="Orientation.Vertical"/>.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

    private readonly OrientationHelper _orientationHelper;

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

#pragma warning restore CS0067

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        SetupKeyBindings ();
        SetContentSize ();
    }

    #endregion IOrientation

    // TODO: Add a SelectedItemChanging event like CheckBox has.
    /// <summary>Called whenever the current selected item changes. Invokes the <see cref="SelectedItemChanged"/> event.</summary>
    /// <param name="selectedItem"></param>
    /// <param name="previousSelectedItem"></param>
    protected virtual void OnSelectedItemChanged (int selectedItem, int previousSelectedItem) { }

    /// <summary>
    ///     Gets or sets the <see cref="RadioLabels"/> index for the cursor. The cursor may or may not be the selected
    ///     RadioItem.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Maps to either the X or Y position within <see cref="View.Viewport"/> depending on <see cref="Orientation"/>.
    ///     </para>
    /// </remarks>
    public int Cursor { get; set; }

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        var x = 0;
        var y = 0;

        switch (Orientation)
        {
            case Orientation.Vertical:
                y = Cursor;

                break;
            case Orientation.Horizontal:
                if (_horizontal!.Count > 0)
                {
                    x = _horizontal [Cursor].pos;
                }

                break;

            default:
                return null;
        }

        Move (x, y);

        return null; // Don't show the cursor
    }

    /// <summary>Raised when the selected radio label has changed.</summary>
    public event EventHandler<SelectedItemChangedArgs>? SelectedItemChanged;

    private bool MoveDownRight ()
    {
        if (Cursor + 1 < _radioLabels.Count)
        {
            Cursor++;
            SetNeedsDraw ();

            return true;
        }

        // Moving past should move focus to next view, not wrap
        return false;
    }

    private bool MoveEnd ()
    {
        Cursor = Math.Max (_radioLabels.Count - 1, 0);

        return true;
    }

    private bool MoveHome ()
    {
        if (Cursor != 0)
        {
            Cursor = 0;

            return true;
        }

        return false;
    }

    private bool MoveUpLeft ()
    {
        if (Cursor > 0)
        {
            Cursor--;
            SetNeedsDraw ();

            return true;
        }

        // Moving past should move focus to next view, not wrap
        return false;
    }

    private void RadioGroup_LayoutStarted (object? sender, EventArgs e) { SetContentSize (); }

    private void SetContentSize ()
    {
        switch (Orientation)
        {
            case Orientation.Vertical:
                var width = 0;

                foreach (string s in _radioLabels)
                {
                    width = Math.Max (s.GetColumns () + 2, width);
                }

                SetContentSize (new (width, _radioLabels.Count));

                break;

            case Orientation.Horizontal:
                _horizontal = new ();
                var start = 0;
                var length = 0;

                for (var i = 0; i < _radioLabels.Count; i++)
                {
                    start += length;

                    length = _radioLabels [i].GetColumns () + 2 + (i < _radioLabels.Count - 1 ? _horizontalSpace : 0);
                    _horizontal.Add ((start, length));
                }

                SetContentSize (new (_horizontal.Sum (item => item.length), 1));

                break;
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        RadioLabels = new [] { "Option _1", "Option _2", "Option _3" };

        return true;
    }
}
