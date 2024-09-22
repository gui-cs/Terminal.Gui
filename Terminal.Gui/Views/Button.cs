//
// Button.cs: Button control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

namespace Terminal.Gui;

/// <summary>Button is a <see cref="View"/> that provides an item that invokes raises the <see cref="View.Accept"/> event.</summary>
/// <remarks>
///     <para>
///         Provides a button showing text that raises the <see cref="View.Accept"/> event when clicked on with a mouse or
///         when the user presses SPACE, ENTER, or the <see cref="View.HotKey"/>. The hot key is the first letter or digit
///         following the first underscore ('_') in the button text.
///     </para>
///     <para>Use <see cref="View.HotKeySpecifier"/> to change the hot key specifier from the default of ('_').</para>
///     <para>
///         When the button is configured as the default (<see cref="IsDefault"/>) and the user presses the ENTER key, if
///         no other <see cref="View"/> processes the key, the <see cref="Button"/>'s <see cref="View.Accept"/> event will
///         be fired.
///     </para>
///     <para>
///         Set <see cref="View.WantContinuousButtonPressed"/> to <see langword="true"/> to have the <see cref="View.Accept"/> event
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
    /// Gets or sets whether <see cref="Button"/>s are shown with a shadow effect by default.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.None;

    /// <summary>
    /// Gets or sets the default Highlight Style.
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
        AddCommand (Command.HotKey, () =>
        {
            SetFocus ();
            return !OnAccept ();
        });

        KeyBindings.Add (Key.Space, Command.HotKey);
        KeyBindings.Add (Key.Enter, Command.HotKey);

        TitleChanged += Button_TitleChanged;
        MouseClick += Button_MouseClick;

        ShadowStyle = DefaultShadow;
        HighlightStyle = DefaultHighlightStyle;
    }

    private bool _wantContinuousButtonPressed;

    /// <inheritdoc />
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
        e.Handled = InvokeCommand (Command.HotKey) == true;
    }

    private void Button_TitleChanged (object sender, EventArgs<string> e)
    {
        base.Text = e.CurrentValue;
        TextFormatter.HotKeySpecifier = HotKeySpecifier;
    }

    /// <inheritdoc />
    public override string Text
    {
        get => base.Title;
        set => base.Text = base.Title = value;
    }

    /// <inheritdoc />
    public override Rune HotKeySpecifier
    {
        get => base.HotKeySpecifier;
        set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value;
    }

    /// <summary>Gets or sets whether the <see cref="Button"/> is the default action to activate in a dialog.</summary>
    /// <value><c>true</c> if is default; otherwise, <c>false</c>.</value>
    public bool IsDefault
    {
        get => _isDefault;
        set
        {
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

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        Title = "_Button";

        return true;
    }
}