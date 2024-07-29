namespace Terminal.Gui;

/// <summary>Displays a group of labels each with a selected indicator. Only one of those can be selected at a given time.</summary>
public class RadioGroup : View, IDesignable
{
    private int _cursor;
    private List<(int pos, int length)> _horizontal;
    private int _horizontalSpace = 2;
    private Orientation _orientation = Orientation.Vertical;
    private List<string> _radioLabels = [];
    private int _selected;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RadioGroup"/> class.
    /// </summary>
    public RadioGroup ()
    {
        CanFocus = true;

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        // Things this view knows how to do
        AddCommand (
                    Command.LineUp,
                    () =>
                    {
                        if (!HasFocus)
                        {
                            return false;
                        }

                        return MoveUpLeft ();
                    }
                   );

        AddCommand (
                    Command.LineDown,
                    () =>
                    {
                        if (!HasFocus)
                        {
                            return false;
                        }
                        return MoveDownRight ();
                    }
                   );

        AddCommand (
                    Command.TopHome,
                    () =>
                    {
                        if (!HasFocus)
                        {
                            return false;
                        }
                        MoveHome ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.BottomEnd,
                    () =>
                    {
                        if (!HasFocus)
                        {
                            return false;
                        }
                        MoveEnd ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Accept,
                    () =>
                    {
                        SelectedItem = _cursor;

                        return OnAccept () is true or null;
                    }
                   );

        AddCommand (
                    Command.HotKey,
                    ctx =>
                    {
                        SetFocus ();
                        if (ctx.KeyBinding?.Context is { } && (int)ctx.KeyBinding?.Context! < _radioLabels.Count)
                        {
                            SelectedItem = (int)ctx.KeyBinding?.Context!;

                            return OnAccept () is true or null;
                        }

                        return true;
                    });

        SetupKeyBindings ();

        LayoutStarted += RadioGroup_LayoutStarted;

        HighlightStyle = HighlightStyle.PressedOutside | HighlightStyle.Pressed;

        MouseClick += RadioGroup_MouseClick;
    }

    // TODO: Fix InvertColorsOnPress - only highlight the selected item

    private void SetupKeyBindings ()
    {
        KeyBindings.Clear ();

        // Default keybindings for this view
        if (Orientation == Orientation.Vertical)
        {
            KeyBindings.Add (Key.CursorUp, Command.LineUp);
            KeyBindings.Add (Key.CursorDown, Command.LineDown);
        }
        else
        {
            KeyBindings.Add (Key.CursorLeft, Command.LineUp);
            KeyBindings.Add (Key.CursorRight, Command.LineDown);
        }

        KeyBindings.Add (Key.Home, Command.TopHome);
        KeyBindings.Add (Key.End, Command.BottomEnd);
        KeyBindings.Add (Key.Space, Command.Accept);
    }

    private void RadioGroup_MouseClick (object sender, MouseEventEventArgs e)
    {
        SetFocus ();

        int viewportX = e.MouseEvent.Position.X;
        int viewportY = e.MouseEvent.Position.Y;

        int pos = _orientation == Orientation.Horizontal ? viewportX : viewportY;

        int rCount = _orientation == Orientation.Horizontal
                         ? _horizontal.Last ().pos + _horizontal.Last ().length
                         : _radioLabels.Count;

        if (pos < rCount)
        {
            int c = _orientation == Orientation.Horizontal
                        ? _horizontal.FindIndex (x => x.pos <= viewportX && x.pos + x.length - 2 >= viewportX)
                        : viewportY;

            if (c > -1)
            {
                _cursor = SelectedItem = c;
                SetNeedsDisplay ();
            }
        }

        e.Handled = true;
    }

    /// <summary>
    ///     Gets or sets the horizontal space for this <see cref="RadioGroup"/> if the <see cref="Orientation"/> is
    ///     <see cref="Orientation.Horizontal"/>
    /// </summary>
    public int HorizontalSpace
    {
        get => _horizontalSpace;
        set
        {
            if (_horizontalSpace != value && _orientation == Orientation.Horizontal)
            {
                _horizontalSpace = value;
                UpdateTextFormatterText ();
                SetContentSize ();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="RadioGroup"/>. The default is
    ///     <see cref="Orientation.Vertical"/>.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientation;
        set => OnOrientationChanged (value);
    }

    /// <summary>
    ///     The radio labels to display. A key binding will be added for each radio enabling the user to select
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

            int prevCount = _radioLabels.Count;
            _radioLabels = value.ToList ();

            for (var index = 0; index < _radioLabels.Count; index++)
            {
                string label = _radioLabels [index];

                if (TextFormatter.FindHotKey (label, HotKeySpecifier, out _, out Key hotKey))
                {
                    AddKeyBindingsForHotKey (Key.Empty, hotKey, index);
                }
            }

            SelectedItem = 0;
            SetContentSize ();
        }
    }

    /// <summary>The currently selected item from the list of radio labels</summary>
    /// <value>The selected.</value>
    public int SelectedItem
    {
        get => _selected;
        set
        {
            OnSelectedItemChanged (value, SelectedItem);
            _cursor = Math.Max (_selected, 0);
            SetNeedsDisplay ();
        }
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        Driver.SetAttribute (GetNormalColor ());

        for (var i = 0; i < _radioLabels.Count; i++)
        {
            switch (Orientation)
            {
                case Orientation.Vertical:
                    Move (0, i);

                    break;
                case Orientation.Horizontal:
                    Move (_horizontal [i].pos, 0);

                    break;
            }

            string rl = _radioLabels [i];
            Driver.SetAttribute (GetNormalColor ());
            Driver.AddStr ($"{(i == _selected ? Glyphs.Selected : Glyphs.UnSelected)} ");
            TextFormatter.FindHotKey (rl, HotKeySpecifier, out int hotPos, out Key hotKey);

            if (hotPos != -1 && hotKey != Key.Empty)
            {
                Rune [] rlRunes = rl.ToRunes ();

                for (var j = 0; j < rlRunes.Length; j++)
                {
                    Rune rune = rlRunes [j];

                    if (j == hotPos && i == _cursor)
                    {
                        Application.Driver?.SetAttribute (
                                                         HasFocus
                                                             ? ColorScheme.HotFocus
                                                             : GetHotNormalColor ()
                                                        );
                    }
                    else if (j == hotPos && i != _cursor)
                    {
                        Application.Driver?.SetAttribute (GetHotNormalColor ());
                    }
                    else if (HasFocus && i == _cursor)
                    {
                        Application.Driver?.SetAttribute (ColorScheme.Focus);
                    }

                    if (rune == HotKeySpecifier && j + 1 < rlRunes.Length)
                    {
                        j++;
                        rune = rlRunes [j];

                        if (i == _cursor)
                        {
                            Application.Driver?.SetAttribute (
                                                             HasFocus
                                                                 ? ColorScheme.HotFocus
                                                                 : GetHotNormalColor ()
                                                            );
                        }
                        else if (i != _cursor)
                        {
                            Application.Driver?.SetAttribute (GetHotNormalColor ());
                        }
                    }

                    Application.Driver?.AddRune (rune);
                    Driver.SetAttribute (GetNormalColor ());
                }
            }
            else
            {
                DrawHotString (rl, HasFocus && i == _cursor, ColorScheme);
            }
        }
    }

    /// <summary>Called when the view orientation has changed. Invokes the <see cref="OrientationChanged"/> event.</summary>
    /// <param name="newOrientation"></param>
    /// <returns>True of the event was cancelled.</returns>
    public virtual bool OnOrientationChanged (Orientation newOrientation)
    {
        var args = new OrientationEventArgs (newOrientation);
        OrientationChanged?.Invoke (this, args);

        if (!args.Cancel)
        {
            _orientation = newOrientation;
            SetupKeyBindings ();
            SetContentSize ();
        }

        return args.Cancel;
    }

    // TODO: This should be cancelable
    /// <summary>Called whenever the current selected item changes. Invokes the <see cref="SelectedItemChanged"/> event.</summary>
    /// <param name="selectedItem"></param>
    /// <param name="previousSelectedItem"></param>
    public virtual void OnSelectedItemChanged (int selectedItem, int previousSelectedItem)
    { 
        if (_selected == selectedItem)
        {
            return;
        }
        _selected = selectedItem;
        SelectedItemChanged?.Invoke (this, new (selectedItem, previousSelectedItem));
    }

    /// <summary>
    ///     Fired when the view orientation has changed. Can be cancelled by setting
    ///     <see cref="OrientationEventArgs.Cancel"/> to true.
    /// </summary>
    public event EventHandler<OrientationEventArgs> OrientationChanged;

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        var x = 0;
        var y = 0;

        switch (Orientation)
        {
            case Orientation.Vertical:
                y = _cursor;

                break;
            case Orientation.Horizontal:
                x = _horizontal [_cursor].pos;

                break;

            default:
                return null;
        }

        Move (x, y);

        return null; // Don't show the cursor
    }

    /// <summary>Allow to invoke the <see cref="SelectedItemChanged"/> after their creation.</summary>
    public void Refresh () { OnSelectedItemChanged (_selected, -1); }

    // TODO: This should use StateEventArgs<int> and should be cancelable.
    /// <summary>Invoked when the selected radio label has changed.</summary>
    public event EventHandler<SelectedItemChangedArgs> SelectedItemChanged;

    private bool MoveDownRight ()
    {
        if (_cursor + 1 < _radioLabels.Count)
        {
            _cursor++;
            SetNeedsDisplay ();

            return true;
        }

        // Moving past should move focus to next view, not wrap
        return false;
    }

    private void MoveEnd () { _cursor = Math.Max (_radioLabels.Count - 1, 0); }
    private void MoveHome () { _cursor = 0; }

    private bool MoveUpLeft ()
    {
        if (_cursor > 0)
        {
            _cursor--;
            SetNeedsDisplay ();

            return true;
        }
        // Moving past should move focus to next view, not wrap
        return false;
    }

    private void RadioGroup_LayoutStarted (object sender, EventArgs e) { SetContentSize (); }

    private void SetContentSize ()
    {
        switch (_orientation)
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

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        RadioLabels = new [] { "Option _1", "Option _2", "Option _3" };
        return true;
    }
}
