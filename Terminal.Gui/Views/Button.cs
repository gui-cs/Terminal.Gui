
using static Terminal.Gui.ViewBase.View;

namespace Terminal.Gui.Views;

/// <summary>
///     A button View that can be pressed with the mouse or keyboard.
/// </summary>
/// <remarks>
///     <para>
///         The Button will raise the <see cref="View.Accepting"/> event when the user presses <see cref="View.HotKey"/>,
///         <c>Enter</c>, or <c>Space</c>
///         or clicks on the button with the mouse.
///     </para>
///     <para>Use <see cref="View.HotKeySpecifier"/> to change the hot key specifier from the default of ('_').</para>
///     <para>
///         Button can act as the default <see cref="Command.Accept"/> handler for all peer-Views. See
///         <see cref="IsDefaultAcceptView"/>.
///     </para>
///     <para>
///         Set <see cref="View.WantContinuousButtonPressed"/> to <see langword="true"/> to have the
///         <see cref="View.Accepting"/> event
///         invoked repeatedly while the button is pressed.
///     </para>
/// </remarks>
public class Button : View, IDesignable, IDefaultAcceptView
{
    private readonly Rune _leftBracket;
    private readonly Rune _leftDefault;
    private readonly Rune _rightBracket;
    private readonly Rune _rightDefault;
    private bool _isDefaultAcceptView;

    /// <summary>
    ///     Gets or sets whether <see cref="Button"/>s are shown with a shadow effect by default.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.Opaque;

    /// <summary>
    ///     Gets or sets the default Highlight Style.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static MouseState DefaultHighlightStates { get; set; } = MouseState.In | MouseState.Pressed | MouseState.PressedOutside;

    /// <summary>Initializes a new instance of <see cref="Button"/>.</summary>
    public Button ()
    {
        base.TextAlignment = Alignment.Center;
        base.VerticalTextAlignment = Alignment.Center;

        _leftBracket = Glyphs.LeftBracket;
        _rightBracket = Glyphs.RightBracket;
        _leftDefault = Glyphs.LeftDefaultIndicator;
        _rightDefault = Glyphs.RightDefaultIndicator;

        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);

        CanFocus = true;

        AddCommand (Command.HotKey, HandleHotKeyCommand);

        KeyBindings.Remove (Key.Space);
        KeyBindings.Add (Key.Space, Command.HotKey);
        KeyBindings.Remove (Key.Enter);
        KeyBindings.Add (Key.Enter, Command.HotKey);

        MouseBindings.ReplaceCommands (MouseFlags.Button1Clicked, Command.HotKey);
        MouseBindings.ReplaceCommands (MouseFlags.Button2Clicked, Command.HotKey);
        MouseBindings.ReplaceCommands (MouseFlags.Button3Clicked, Command.HotKey);
        MouseBindings.ReplaceCommands (MouseFlags.Button4Clicked, Command.HotKey);

        TitleChanged += Button_TitleChanged;
        //MouseClick += Button_MouseClick;

        base.ShadowStyle = DefaultShadow;
        HighlightStates = DefaultHighlightStates;

        if (MouseHeldDown != null)
        {
            MouseHeldDown.MouseIsHeldDownTick += (_,_) => RaiseAccepting (null);
        }
    }

    private bool? HandleHotKeyCommand (ICommandContext commandContext)
    {
        bool cachedIsDefault = IsDefaultAcceptView; // Supports "Swap Default" in Buttons scenario where IsDefaultAcceptView changes

        if (RaiseActivating (commandContext) is true)
        {
            return true;
        }

        bool? handled = RaiseAccepting (commandContext);

        if (handled == true)
        {
            return true;
        }

        SetFocus ();

        // TODO: If `IsDefaultAcceptView` were a property on `View` *any* View could work this way. That's theoretical as
        // TODO: no use-case has been identified for any View other than Button to act like this.
        // If Accept was not handled...
        if (cachedIsDefault && SuperView is { })
        {
            return SuperView.InvokeCommand (Command.Accept);
        }

        return false;
    }
    private void Button_MouseClick (object sender, MouseEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        // TODO: With https://github.com/gui-cs/Terminal.Gui/issues/3778 we won't have to pass data:
        e.Handled = InvokeCommand<KeyBinding> (Command.HotKey, new KeyBinding ([Command.HotKey], this, data: null)) == true;
    }

    private void Button_TitleChanged (object sender, EventArgs<string> e)
    {
        base.Text = e.Value;
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
    ///     Helper for <see cref="GetIsDefaultAcceptView"/> and <see cref="SetIsDefaultAcceptView"/>.
    /// </summary>
    public bool IsDefaultAcceptView
    {
        get => GetIsDefaultAcceptView ();
        set => SetIsDefaultAcceptView (value);
    }


    /// <inheritdoc />
    public bool GetIsDefaultAcceptView () { return _isDefaultAcceptView; }

    /// <inheritdoc />
    public void SetIsDefaultAcceptView (bool value)
    {
        if (_isDefaultAcceptView == value)
        {
            return;
        }

        _isDefaultAcceptView = value;

        UpdateTextFormatterText ();
        SetNeedsLayout ();
    }

    /// <summary>
    ///     Gets or sets whether the Button will show decorations or not. If <see langword="true"/> the glyphs that normally
    ///     bracket the Button Title and the <see cref="IsDefaultAcceptView"/> indicator will not be shown.
    /// </summary>
    public bool NoDecorations { get; set; }

    /// <summary>
    ///     Gets or sets whether the Button will include padding on each side of the Title.
    /// </summary>
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
        else if (IsDefaultAcceptView)
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
