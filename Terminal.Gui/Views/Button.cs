//
// Button.cs: Button control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

namespace Terminal.Gui;

/// <summary>
///     A View that raises the <see cref="View.Accepted"/> event when clicked with the mouse or when the
///     <see cref="View.HotKey"/>, <c>Enter</c>, or <c>Space</c> key is pressed.
/// </summary>
/// <remarks>
///     <para>
///         Provides a button showing text that raises the <see cref="View.Accepted"/> event when clicked on with a mouse or
///         when the user presses <c>Enter</c>, <c>Space</c> or the <see cref="View.HotKey"/>. The hot key is the first
///         letter or digit
///         following the first underscore ('_') in the button text.
///     </para>
///     <para>Use <see cref="View.HotKeySpecifier"/> to change the hot key specifier from the default of ('_').</para>
///     <para>
///         When the button is configured as the default (<see cref="IsDefault"/>) and the user causes the button to be
///         accepted the <see cref="Button"/>'s <see cref="View.Accepted"/> event will be raised. If the Accept event is not
///         handled, the Accept event on the <see cref="View.SuperView"/>. will be raised. This enables default Accept
///         behavior.
///     </para>
///     <para>
///         Set <see cref="View.WantContinuousButtonPressed"/> to <see langword="true"/> to have the
///         <see cref="View.Accepted"/> event
///         invoked repeatedly while the button is pressed.
///     </para>
/// </remarks>
public class Button : View, IDesignable
{
    private readonly Rune _leftBracket;
    private readonly Rune _leftDefault;
    private readonly Rune _rightBracket;
    private readonly Rune _rightDefault;
    private bool _isDefault;

    /// <summary>
    ///     Gets or sets whether <see cref="Button"/>s are shown with a shadow effect by default.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.None;

    /// <summary>
    ///     Gets or sets the default Highlight Style.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static HighlightStyle DefaultHighlightStyle { get; set; } = HighlightStyle.Pressed | HighlightStyle.Hover;

    /// <summary>Initializes a new instance of <see cref="Button"/>.</summary>
    public Button ()
    {
        TextAlignment = Alignment.Center;
        VerticalTextAlignment = Alignment.Center;

        _leftBracket = Glyphs.LeftBracket;
        _rightBracket = Glyphs.RightBracket;
        _leftDefault = Glyphs.LeftDefaultIndicator;
        _rightDefault = Glyphs.RightDefaultIndicator;

        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);

        CanFocus = true;

        // Override default behavior of View
        AddCommand (
                    Command.HotKey,
                    (ctx) =>
                    {
                        bool cachedIsDefault = IsDefault; // Supports "Swap Default" in Buttons scenario where IsDefault changes

                        if (RaiseSelected (ctx) is true)
                        {
                            return true;
                        }
                        bool? handled = RaiseAccepted ();

                        if (handled == true)
                        {
                            return true;
                        }

                        SetFocus ();

                        // TODO: If `IsDefault` were a property on `View` *any* View could work this way. That's theoretical as
                        // TODO: no use-case has been identified for any View other than Button to act like this.
                        // If Accept was not handled...
                        if (cachedIsDefault && SuperView is { })
                        {
                            return SuperView.InvokeCommand (Command.Accept);
                        }

                        return false;
                    });

        KeyBindings.Remove (Key.Space);
        KeyBindings.Add (Key.Space, Command.HotKey);
        KeyBindings.Remove (Key.Enter);
        KeyBindings.Add (Key.Enter, Command.HotKey);

        TitleChanged += Button_TitleChanged;
        MouseClick += Button_MouseClick;

        ShadowStyle = DefaultShadow;
        HighlightStyle = DefaultHighlightStyle;
    }

    private bool _wantContinuousButtonPressed;

    /// <inheritdoc/>
    public override bool WantContinuousButtonPressed
    {
        get => _wantContinuousButtonPressed;
        set
        {
            if (value == _wantContinuousButtonPressed)
            {
                return;
            }

            _wantContinuousButtonPressed = value;

            if (_wantContinuousButtonPressed)
            {
                HighlightStyle |= HighlightStyle.PressedOutside;
            }
            else
            {
                HighlightStyle &= ~HighlightStyle.PressedOutside;
            }
        }
    }

    private void Button_MouseClick (object sender, MouseEventEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }
        e.Handled = InvokeCommand (Command.HotKey, ctx: new (Command.HotKey, key: null, data: this)) == true;
    }

    private void Button_TitleChanged (object sender, EventArgs<string> e)
    {
        base.Text = e.CurrentValue;
        TextFormatter.HotKeySpecifier = HotKeySpecifier;
    }

    /// <inheritdoc/>
    public override string Text
    {
        get => Title;
        set => base.Text = Title = value;
    }

    /// <inheritdoc/>
    public override Rune HotKeySpecifier
    {
        get => base.HotKeySpecifier;
        set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value;
    }

    /// <summary>
    ///     Gets or sets whether the <see cref="Button"/> will show an indicator indicating it is the default Button. If
    ///     <see langword="true"/>
    ///     <see cref="Command.Accept"/> will be invoked when the user presses <c>Enter</c> and no other peer-
    ///     <see cref="View"/> processes the key.
    ///     If <see cref="View.Accepted"/> is not handled, the Gets or sets whether the <see cref="Button"/> will show an
    ///     indicator indicating it is the default Button. If <see langword="true"/>
    ///     <see cref="Command.Accept"/> command on the <see cref="View.SuperView"/> will be invoked.
    /// </summary>
    public bool IsDefault
    {
        get => _isDefault;
        set
        {
            if (_isDefault == value)
            {
                return;
            }

            _isDefault = value;

            UpdateTextFormatterText ();
            OnResizeNeeded ();
        }
    }

    /// <summary></summary>
    public bool NoDecorations { get; set; }

    /// <summary></summary>
    public bool NoPadding { get; set; }

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        if (HotKey.IsValid && Text != "")
        {
            for (var i = 0; i < TextFormatter.Text.GetRuneCount (); i++)
            {
                if (TextFormatter.Text [i] == Text [0])
                {
                    Move (i, 0);

                    return null; // Don't show the cursor
                }
            }
        }

        return base.PositionCursor ();
    }

    /// <inheritdoc/>
    protected override void UpdateTextFormatterText ()
    {
        base.UpdateTextFormatterText ();

        if (NoDecorations)
        {
            TextFormatter.Text = Text;
        }
        else if (IsDefault)
        {
            TextFormatter.Text = $"{_leftBracket}{_leftDefault} {Text} {_rightDefault}{_rightBracket}";
        }
        else
        {
            if (NoPadding)
            {
                TextFormatter.Text = $"{_leftBracket}{Text}{_rightBracket}";
            }
            else
            {
                TextFormatter.Text = $"{_leftBracket} {Text} {_rightBracket}";
            }
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Title = "_Button";

        return true;
    }
}
