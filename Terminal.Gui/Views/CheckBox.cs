namespace Terminal.Gui;

/// <summary>The <see cref="CheckBox"/> <see cref="View"/> shows an on/off toggle that the user can set</summary>
public class CheckBox : View {
    private bool _allowNullChecked;
    private Rune _charChecked;
    private Rune _charNullChecked;
    private Rune _charUnChecked;
    private bool? _checked;

    /// <summary>
    ///     Initializes a new instance of <see cref="CheckBox"/> based on the given text, using
    ///     <see cref="LayoutStyle.Computed"/> layout.
    /// </summary>
    public CheckBox () : this (string.Empty) { }

    /// <summary>
    ///     Initializes a new instance of <see cref="CheckBox"/> based on the given text, using
    ///     <see cref="LayoutStyle.Computed"/> layout.
    /// </summary>
    /// <param name="s">S.</param>
    /// <param name="is_checked">If set to <c>true</c> is checked.</param>
    public CheckBox (string s, bool is_checked = false) { SetInitialProperties (s, is_checked); }

    /// <summary>Initializes a new instance of <see cref="CheckBox"/> using <see cref="LayoutStyle.Absolute"/> layout.</summary>
    /// <remarks>
    ///     The size of <see cref="CheckBox"/> is computed based on the text length. This <see cref="CheckBox"/> is not
    ///     toggled.
    /// </remarks>
    public CheckBox (int x, int y, string s) : this (x, y, s, false) { }

    /// <summary>Initializes a new instance of <see cref="CheckBox"/> using <see cref="LayoutStyle.Absolute"/> layout.</summary>
    /// <remarks>The size of <see cref="CheckBox"/> is computed based on the text length.</remarks>
    public CheckBox (int x, int y, string s, bool is_checked) : base (new Rect (x, y, s.Length, 1)) {
        SetInitialProperties (s, is_checked);
    }

    /// <summary>
    ///     If <see langword="true"/> allows <see cref="Checked"/> to be null, true or false. If <see langword="false"/>
    ///     only allows <see cref="Checked"/> to be true or false.
    /// </summary>
    public bool AllowNullChecked {
        get => _allowNullChecked;
        set {
            _allowNullChecked = value;
            Checked ??= false;
        }
    }

    /// <summary>The state of the <see cref="CheckBox"/></summary>
    public bool? Checked {
        get => _checked;
        set {
            if (value == null && !AllowNullChecked) {
                return;
            }

            _checked = value;
            UpdateTextFormatterText ();
            OnResizeNeeded ();
        }
    }

    /// <inheritdoc/>
    public override Key HotKey {
        get => base.HotKey;
        set {
            if (value is null) {
                throw new ArgumentException (nameof (value));
            }

            Key prev = base.HotKey;
            if (prev != value) {
                base.HotKey = TextFormatter.HotKey = value;

                // Also add Alt+HotKey
                if (prev != Key.Empty && KeyBindings.TryGet (prev.WithAlt, out _)) {
                    if (value.KeyCode == KeyCode.Null) {
                        KeyBindings.Remove (prev.WithAlt);
                    } else {
                        KeyBindings.Replace (prev.WithAlt, value.WithAlt);
                    }
                } else if (value != Key.Empty) {
                    KeyBindings.Add (value.WithAlt, Command.Accept);
                }
            }
        }
    }

    ///<inheritdoc/>
    public override bool MouseEvent (MouseEvent me) {
        if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) || !CanFocus) {
            return false;
        }

        ToggleChecked ();

        return true;
    }

    ///<inheritdoc/>
    public override bool OnEnter (View view) {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnEnter (view);
    }

    /// <summary>Called when the <see cref="Checked"/> property changes. Invokes the <see cref="Toggled"/> event.</summary>
    public virtual void OnToggled (ToggleEventArgs e) { Toggled?.Invoke (this, e); }

    ///<inheritdoc/>
    public override void PositionCursor () { Move (0, 0); }

    /// <summary>Toggled event, raised when the <see cref="CheckBox"/>  is toggled.</summary>
    /// <remarks>
    ///     Client code can hook up to this event, it is raised when the <see cref="CheckBox"/> is activated either with
    ///     the mouse or the keyboard. The passed <c>bool</c> contains the previous state.
    /// </remarks>
    public event EventHandler<ToggleEventArgs> Toggled;

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText () {
        switch (TextAlignment) {
            case TextAlignment.Left:
            case TextAlignment.Centered:
            case TextAlignment.Justified:
                TextFormatter.Text = $"{GetCheckedState ()} {GetFormatterText ()}";

                break;
            case TextAlignment.Right:
                TextFormatter.Text = $"{GetFormatterText ()} {GetCheckedState ()}";

                break;
        }
    }

    private Rune GetCheckedState () {
        return Checked switch {
                   true => _charChecked,
                   false => _charUnChecked,
                   var _ => _charNullChecked
               };
    }

    private string GetFormatterText () {
        if (AutoSize || string.IsNullOrEmpty (Text) || (Frame.Width <= 2)) {
            return Text;
        }

        return Text[..Math.Min (Frame.Width - 2, Text.GetRuneCount ())];
    }

    // TODO: v2 - Remove constructors with parameters
    /// <summary>Private helper to set the initial properties of the View that were provided via constructors.</summary>
    /// <param name="s"></param>
    /// <param name="is_checked"></param>
    private void SetInitialProperties (string s, bool is_checked) {
        _charNullChecked = Glyphs.NullChecked;
        _charChecked = Glyphs.Checked;
        _charUnChecked = Glyphs.UnChecked;
        Checked = is_checked;
        HotKeySpecifier = (Rune)'_';
        CanFocus = true;
        AutoSize = true;
        Text = s;

        // Things this view knows how to do
        AddCommand (Command.ToggleChecked, () => ToggleChecked ());
        AddCommand (
                    Command.Accept,
                    () => {
                        if (!HasFocus) {
                            SetFocus ();
                        }

                        ToggleChecked ();

                        return true;
                    });

        // Default keybindings for this view
        KeyBindings.Add (Key.Space, Command.ToggleChecked);
    }

    private bool ToggleChecked () {
        if (!HasFocus) {
            SetFocus ();
        }

        bool? previousChecked = Checked;
        if (AllowNullChecked) {
            switch (previousChecked) {
                case null:
                    Checked = true;

                    break;
                case true:
                    Checked = false;

                    break;
                case false:
                    Checked = null;

                    break;
            }
        } else {
            Checked = !Checked;
        }

        OnToggled (new ToggleEventArgs (previousChecked, Checked));
        SetNeedsDisplay ();

        return true;
    }
}
