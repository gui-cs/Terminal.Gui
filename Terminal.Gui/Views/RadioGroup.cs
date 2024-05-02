namespace Terminal.Gui;

/// <summary>Displays a group of labels each with a selected indicator. Only one of those can be selected at a given time.</summary>
public class RadioGroup : View
{
    private int _cursor;
    private List<(int pos, int length)> _horizontal;
    private int _horizontalSpace = 2;
    private Orientation _orientation = Orientation.Vertical;
    private List<string> _radioLabels = [];
    private int _selected;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Computed"/>
    ///     layout.
    /// </summary>
    public RadioGroup ()
    {
        CanFocus = true;

        // Things this view knows how to do
        AddCommand (
                    Command.LineUp,
                    () =>
                    {
                        MoveUp ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.LineDown,
                    () =>
                    {
                        MoveDown ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.TopHome,
                    () =>
                    {
                        MoveHome ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.BottomEnd,
                    () =>
                    {
                        MoveEnd ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Accept,
                    () =>
                    {
                        SelectItem ();
                        return !OnAccept ();
                    }
                   );

        // Default keybindings for this view
        KeyBindings.Add (Key.CursorUp, Command.LineUp);
        KeyBindings.Add (Key.CursorDown, Command.LineDown);
        KeyBindings.Add (Key.Home, Command.TopHome);
        KeyBindings.Add (Key.End, Command.BottomEnd);
        KeyBindings.Add (Key.Space, Command.Accept);

        LayoutStarted += RadioGroup_LayoutStarted;

        HighlightStyle = Gui.HighlightStyle.PressedOutside | Gui.HighlightStyle.Pressed;

        MouseClick += RadioGroup_MouseClick;
    }

    // TODO: Fix InvertColorsOnPress - only highlight the selected item

    private void RadioGroup_MouseClick (object sender, MouseEventEventArgs e)
    {
        SetFocus ();

        int boundsX = e.MouseEvent.X;
        int boundsY = e.MouseEvent.Y;

        int pos = _orientation == Orientation.Horizontal ? boundsX : boundsY;

        int rCount = _orientation == Orientation.Horizontal
                         ? _horizontal.Last ().pos + _horizontal.Last ().length
                         : _radioLabels.Count;

        if (pos < rCount)
        {
            int c = _orientation == Orientation.Horizontal
                        ? _horizontal.FindIndex (x => x.pos <= boundsX && x.pos + x.length - 2 >= boundsX)
                        : boundsY;

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
                SetWidthHeight (_radioLabels);
                UpdateTextFormatterText ();
                SetNeedsDisplay ();
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
    ///     The radio labels to display. A key binding will be added for each radio radio enabling the user to select
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

            foreach (string label in _radioLabels)
            {
                if (TextFormatter.FindHotKey (label, HotKeySpecifier, out _, out Key hotKey))
                {
                    AddKeyBindingsForHotKey (Key.Empty, hotKey);
                }
            }

            if (IsInitialized && prevCount != _radioLabels.Count)
            {
                SetWidthHeight (_radioLabels);
            }

            SelectedItem = 0;
            SetNeedsDisplay ();
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
                        Application.Driver.SetAttribute (
                                                         HasFocus
                                                             ? ColorScheme.HotFocus
                                                             : GetHotNormalColor ()
                                                        );
                    }
                    else if (j == hotPos && i != _cursor)
                    {
                        Application.Driver.SetAttribute (GetHotNormalColor ());
                    }
                    else if (HasFocus && i == _cursor)
                    {
                        Application.Driver.SetAttribute (ColorScheme.Focus);
                    }

                    if (rune == HotKeySpecifier && j + 1 < rlRunes.Length)
                    {
                        j++;
                        rune = rlRunes [j];

                        if (i == _cursor)
                        {
                            Application.Driver.SetAttribute (
                                                             HasFocus
                                                                 ? ColorScheme.HotFocus
                                                                 : GetHotNormalColor ()
                                                            );
                        }
                        else if (i != _cursor)
                        {
                            Application.Driver.SetAttribute (GetHotNormalColor ());
                        }
                    }

                    Application.Driver.AddRune (rune);
                    Driver.SetAttribute (GetNormalColor ());
                }
            }
            else
            {
                DrawHotString (rl, HasFocus && i == _cursor, ColorScheme);
            }
        }
    }

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnEnter (view);
    }

    /// <inheritdoc/>
    public override bool? OnInvokingKeyBindings (Key keyEvent)
    {
        // This is a bit of a hack. We want to handle the key bindings for the radio group but
        // InvokeKeyBindings doesn't pass any context so we can't tell if the key binding is for
        // the radio group or for one of the radio buttons. So before we call the base class
        // we set SelectedItem appropriately.

        Key key = keyEvent;

        if (KeyBindings.TryGet (key, out _))
        {
            // Search RadioLabels 
            for (var i = 0; i < _radioLabels.Count; i++)
            {
                if (TextFormatter.FindHotKey (
                                              _radioLabels [i],
                                              HotKeySpecifier,
                                              out _,
                                              out Key hotKey,
                                              true
                                             )
                    && key.NoAlt.NoCtrl.NoShift == hotKey)
                {
                    SelectedItem = i;
                    break;
                }
            }
        }

        return base.OnInvokingKeyBindings (keyEvent);
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
            SetNeedsLayout ();
        }

        return args.Cancel;
    }

    // TODO: This should be cancelable
    /// <summary>Called whenever the current selected item changes. Invokes the <see cref="SelectedItemChanged"/> event.</summary>
    /// <param name="selectedItem"></param>
    /// <param name="previousSelectedItem"></param>
    public virtual void OnSelectedItemChanged (int selectedItem, int previousSelectedItem)
    {
        _selected = selectedItem;
        SelectedItemChanged?.Invoke (this, new SelectedItemChangedArgs (selectedItem, previousSelectedItem));
    }

    /// <summary>
    ///     Fired when the view orientation has changed. Can be cancelled by setting
    ///     <see cref="OrientationEventArgs.Cancel"/> to true.
    /// </summary>
    public event EventHandler<OrientationEventArgs> OrientationChanged;

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        int x = 0;
        int y = 0;
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
        return new Point (x, y);
    }

    /// <summary>Allow to invoke the <see cref="SelectedItemChanged"/> after their creation.</summary>
    public void Refresh () { OnSelectedItemChanged (_selected, -1); }

    // TODO: This should use StateEventArgs<int> and should be cancelable.
    /// <summary>Invoked when the selected radio label has changed.</summary>
    public event EventHandler<SelectedItemChangedArgs> SelectedItemChanged;

    private void CalculateHorizontalPositions ()
    {
        if (_orientation == Orientation.Horizontal)
        {
            _horizontal = new List<(int pos, int length)> ();
            var start = 0;
            var length = 0;

            for (var i = 0; i < _radioLabels.Count; i++)
            {
                start += length;

                length = _radioLabels [i].GetColumns () + 2 + (i < _radioLabels.Count - 1 ? _horizontalSpace : 0);
                _horizontal.Add ((start, length));
            }
        }
    }

    private static Rectangle MakeRect (int x, int y, List<string> radioLabels)
    {
        if (radioLabels is null)
        {
            return new (x, y, 0, 0);
        }

        var width = 0;

        foreach (string s in radioLabels)
        {
            width = Math.Max (s.GetColumns () + 2, width);
        }

        return new (x, y, width, radioLabels.Count);
    }

    private void MoveDown ()
    {
        if (_cursor + 1 < _radioLabels.Count)
        {
            _cursor++;
            SetNeedsDisplay ();
        }
        else if (_cursor > 0)
        {
            _cursor = 0;
            SetNeedsDisplay ();
        }
    }

    private void MoveEnd () { _cursor = Math.Max (_radioLabels.Count - 1, 0); }
    private void MoveHome () { _cursor = 0; }

    private void MoveUp ()
    {
        if (_cursor > 0)
        {
            _cursor--;
            SetNeedsDisplay ();
        }
        else if (_radioLabels.Count - 1 > 0)
        {
            _cursor = _radioLabels.Count - 1;
            SetNeedsDisplay ();
        }
    }

    private void RadioGroup_LayoutStarted (object sender, EventArgs e) { SetWidthHeight (_radioLabels); }
    private void SelectItem () { SelectedItem = _cursor; }

    private void SetWidthHeight (List<string> radioLabels)
    {
        switch (_orientation)
        {
            case Orientation.Vertical:
                Rectangle r = MakeRect (0, 0, radioLabels);

                if (IsInitialized)
                {
                    Width = r.Width + GetAdornmentsThickness ().Horizontal;
                    Height = radioLabels.Count + GetAdornmentsThickness ().Vertical;
                }

                break;

            case Orientation.Horizontal:
                CalculateHorizontalPositions ();
                var length = 0;

                foreach ((int pos, int length) item in _horizontal)
                {
                    length += item.length;
                }

                if (IsInitialized)
                {
                    Width = length + GetAdornmentsThickness ().Vertical;
                    Height = 1 + GetAdornmentsThickness ().Horizontal;
                }

                break;
        }
    }
}
