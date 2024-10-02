#nullable enable
namespace Terminal.Gui;

/// <summary>Displays a group of labels each with a selected indicator. Only one of those can be selected at a given time.</summary>
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

        // Things this view knows how to do
        AddCommand (
                    Command.Up,
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
                    Command.Down,
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
                    Command.Start,
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
                    Command.End,
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
                    Command.Select,
                    () =>
                    {
                        if (SelectedItem == Cursor)
                        {
                            if (!MoveDownRight ())
                            {
                                MoveHome ();
                            }
                        }

                        return ChangeSelectedItem (Cursor) is false or null;
                    });

        // Accept (Enter key) - Raise Accept event - DO NOT advance state
        AddCommand (Command.Accept, RaiseAcceptEvent);

        AddCommand (
                    Command.HotKey,
                    ctx =>
                    {
                        var item = ctx.KeyBinding?.Context as int?;

                        if (HasFocus)
                        {
                            if (ctx is { KeyBinding: { } } && (ctx.KeyBinding.Value.BoundView != this || HotKey == ctx.Key?.NoAlt.NoCtrl.NoShift))
                            {
                                // It's this.HotKey OR Another View (Label?) forwarded the hotkey command to us - Act just like `Space` (Select)
                                return InvokeCommand (Command.Select, ctx.Key, ctx.KeyBinding);
                            }
                        }

                        if (item is { } && item < _radioLabels.Count)
                        {
                            if (item.Value == SelectedItem)
                            {
                                return true;
                            }

                            // If a RadioItem.HotKey is pressed we always set the selected item - never SetFocus
                            if (ChangeSelectedItem (item.Value) is null or false)
                            {
                                return true;
                            }

                            return false;
                        }

                        if (SelectedItem == -1 && ChangeSelectedItem (0) == true)
                        {
                            return true;
                        }

                        SetFocus ();

                        return true;
                    });

        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Vertical;
        _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);

        SetupKeyBindings ();

        LayoutStarted += RadioGroup_LayoutStarted;

        HighlightStyle = HighlightStyle.PressedOutside | HighlightStyle.Pressed;

        MouseClick += RadioGroup_MouseClick;
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
    ///     Gets or sets whether double clicking on a Radio Item will cause the <see cref="View.Accept"/> event to be raised.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see langword="false"/> and Accept is not handled, the Accept event on the <see cref="View.SuperView"/> will be raised. The default is
    ///         <see langword="true"/>.
    ///     </para>
    /// </remarks>
    public bool DoubleClickAccepts { get; set; } = true;

    private void RadioGroup_MouseClick (object sender, MouseEventEventArgs e)
    {
        if (e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
        {
            int viewportX = e.MouseEvent.Position.X;
            int viewportY = e.MouseEvent.Position.Y;

            int pos = Orientation == Orientation.Horizontal ? viewportX : viewportY;

            int rCount = Orientation == Orientation.Horizontal
                             ? _horizontal.Last ().pos + _horizontal.Last ().length
                             : _radioLabels.Count;

            if (pos < rCount)
            {
                int c = Orientation == Orientation.Horizontal
                            ? _horizontal.FindIndex (x => x.pos <= viewportX && x.pos + x.length - 2 >= viewportX)
                            : viewportY;

                if (c > -1)
                {
                    if (ChangeSelectedItem (c) == false)
                    {
                        Cursor = c;
                        e.Handled = true;
                    }
                }
            }
        }

        if (DoubleClickAccepts && e.MouseEvent.Flags.HasFlag (MouseFlags.Button1DoubleClicked))
        {
            int savedSelectedItem = SelectedItem;

            if (RaiseAcceptEvent () == true)
            {
                e.Handled = false;
                _selected = savedSelectedItem;
            }

            if (SuperView?.InvokeCommand (Command.Accept) is false or null)
            {
                e.Handled = true;
            }
        }
    }


    private List<(int pos, int length)> _horizontal;
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

    private List<string> _radioLabels = [];

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

    private int _selected;

    /// <summary>The currently selected item from the list of radio labels</summary>
    /// <value>The selected.</value>
    public int SelectedItem
    {
        get => _selected;
        set => ChangeSelectedItem (value);
    }

    /// <summary>
    ///     INTERNAL Sets the selected item.
    /// </summary>
    /// <param name="value"></param>
    /// <returns><see langword="true"/> if state change was canceled, <see langword="false"/> if the state changed, and <see langword="null"/> if the state was not changed for some other reason.</returns>
    private bool? ChangeSelectedItem (int value)
    {
        if (_selected == value || value > _radioLabels.Count - 1)
        {
            return null;
        }

        if (RaiseSelectEvent () == true)
        {
            return true;
        }

        int savedSelected = _selected;
        _selected = value;
        Cursor = Math.Max (_selected, 0);

        OnSelectedItemChanged (value, SelectedItem);
        SelectedItemChanged?.Invoke (this, new (SelectedItem, savedSelected));

        SetNeedsDisplay ();

        return false;
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

                    if (j == hotPos && i == Cursor)
                    {
                        Application.Driver?.SetAttribute (
                                                          HasFocus
                                                              ? ColorScheme.HotFocus
                                                              : GetHotNormalColor ()
                                                         );
                    }
                    else if (j == hotPos && i != Cursor)
                    {
                        Application.Driver?.SetAttribute (GetHotNormalColor ());
                    }
                    else if (HasFocus && i == Cursor)
                    {
                        Application.Driver?.SetAttribute (GetFocusColor ());
                    }

                    if (rune == HotKeySpecifier && j + 1 < rlRunes.Length)
                    {
                        j++;
                        rune = rlRunes [j];

                        if (i == Cursor)
                        {
                            Application.Driver?.SetAttribute (
                                                              HasFocus
                                                                  ? ColorScheme.HotFocus
                                                                  : GetHotNormalColor ()
                                                             );
                        }
                        else if (i != Cursor)
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
                DrawHotString (rl, HasFocus && i == Cursor);
            }
        }
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

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>> OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>> OrientationChanged;

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        SetupKeyBindings ();
        SetContentSize ();
    }

    #endregion IOrientation

    // TODO: This should be cancelable
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
                if (_horizontal.Count > 0)
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

    /// <summary>Allow to invoke the <see cref="SelectedItemChanged"/> after their creation.</summary>
    public void Refresh () { OnSelectedItemChanged (_selected, -1); }

    // TODO: This should use StateEventArgs<int> and should be cancelable.
    /// <summary>Invoked when the selected radio label has changed.</summary>
    public event EventHandler<SelectedItemChangedArgs> SelectedItemChanged;

    private bool MoveDownRight ()
    {
        if (Cursor + 1 < _radioLabels.Count)
        {
            Cursor++;
            SetNeedsDisplay ();

            return true;
        }

        // Moving past should move focus to next view, not wrap
        return false;
    }

    private void MoveEnd () { Cursor = Math.Max (_radioLabels.Count - 1, 0); }
    private void MoveHome () { Cursor = 0; }

    private bool MoveUpLeft ()
    {
        if (Cursor > 0)
        {
            Cursor--;
            SetNeedsDisplay ();

            return true;
        }

        // Moving past should move focus to next view, not wrap
        return false;
    }

    private void RadioGroup_LayoutStarted (object sender, EventArgs e) { SetContentSize (); }

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
