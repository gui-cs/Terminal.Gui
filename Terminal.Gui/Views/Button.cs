#nullable disable
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
///         <see cref="IsDefault"/>.
///     </para>
///     <para>
///         Set <see cref="View.MouseHoldRepeat"/> to <see langword="true"/> to have the
///         <see cref="View.Accepting"/> event
///         invoked repeatedly while the button is pressed.
///     </para>
/// </remarks>
public class Button : View, IDesignable
{
    private static ShadowStyle _defaultShadow = ShadowStyle.Opaque; // Resources/config.json overrides
    private static MouseState _defaultMouseHighlightStates = MouseState.In | MouseState.Pressed | MouseState.PressedOutside; // Resources/config.json overrides

    private readonly Rune _leftBracket;
    private readonly Rune _leftDefault;
    private readonly Rune _rightBracket;
    private readonly Rune _rightDefault;
    private bool _isDefault;

    /// <summary>
    ///     Gets or sets whether <see cref="Button"/>s are shown with a shadow effect by default.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyle DefaultShadow
    {
        get => _defaultShadow;
        set => _defaultShadow = value;
    }

    /// <summary>
    ///     Gets or sets the default Highlight Style.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static MouseState DefaultMouseHighlightStates
    {
        get => _defaultMouseHighlightStates;
        set => _defaultMouseHighlightStates = value;
    }

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

        KeyBindings.ReplaceCommands (Key.Space, Command.HotKey);
        KeyBindings.ReplaceCommands (Key.Enter, Command.HotKey);

        // Replace default Accept binding with HotKey for mouse clicks
        // These are managed dynamically when MouseHoldRepeat changes
        SetMouseBindings (MouseHoldRepeat);

        TitleChanged += Button_TitleChanged;

        base.ShadowStyle = DefaultShadow;
        MouseHighlightStates = DefaultMouseHighlightStates;
    }

    /// <inheritdoc/>
    protected override void OnMouseHoldRepeatChanged (ValueChangedEventArgs<MouseFlags?> args)
    {
        SetMouseBindings (args.NewValue);
    }

    private void SetMouseBindings (MouseFlags? mouseHoldRepeat)
    {
        if (mouseHoldRepeat.HasValue)
        {
            // MouseHoldRepeat enabled: Remove ALL Click/Release/Press bindings, add only configured event→HotKey
            MouseBindings.Remove (MouseFlags.LeftButtonPressed);
            MouseBindings.Remove (MouseFlags.LeftButtonClicked);
            MouseBindings.Remove (MouseFlags.LeftButtonDoubleClicked);
            MouseBindings.Remove (MouseFlags.LeftButtonTripleClicked);
            MouseBindings.Remove (MouseFlags.LeftButtonReleased);

            // Add configured mouse event→HotKey binding
            MouseBindings.Add (mouseHoldRepeat.Value, Command.HotKey);
        }
        else
        {
            // MouseHoldRepeat disabled: Remove ALL Click/Release/Press bindings, add only Clicked→HotKey
            MouseBindings.Remove (MouseFlags.LeftButtonClicked);
            MouseBindings.Remove (MouseFlags.LeftButtonReleased);

            // Add Clicked→HotKey bindings (default behavior)
            MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);
            MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.HotKey);
            MouseBindings.Add (MouseFlags.LeftButtonTripleClicked, Command.HotKey);
        }
    }

    private bool? HandleHotKeyCommand (ICommandContext commandContext)
    {
        bool cachedIsDefault = IsDefault; // Supports "Swap Default" in Buttons scenario where IsDefault changes

        bool? handled = RaiseActivating (commandContext);

        if (handled == true)
        {
            return true;
        }
        SetFocus ();

        handled = RaiseAccepting (commandContext);

        if (handled == true)
        {
            return true;
        }

        // TODO: If `IsDefault` were a property on `View` *any* View could work this way. That's theoretical as
        // TODO: no use-case has been identified for any View other than Button to act like this.
        // If Accept was not handled...
        if (cachedIsDefault && SuperView is { })
        {
            return SuperView.InvokeCommand (Command.Accept);
        }

        return false;
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
    ///     Gets or sets whether the <see cref="Button"/> will act as the default handler for <see cref="Command.Accept"/>
    ///     commands on the <see cref="View.SuperView"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see langword="true"/>:
    ///     </para>
    ///     <para>
    ///         - The Button will display an indicator that it is the default Button.
    ///     </para>
    ///     <para>
    ///         - When clicked, if the Accepting event is not handled, <see cref="Command.Accept"/> will be
    ///         invoked on the SuperView.
    ///     </para>
    ///     <para>
    ///         - If a peer-View receives <see cref="Command.Accept"/> and does not handle it, the command will be passed to
    ///         the
    ///         first Button in the SuperView that has <see cref="IsDefault"/> set to <see langword="true"/>. See
    ///         <see cref="View.RaiseAccepting"/> for more information.
    ///     </para>
    /// </remarks>
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
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Gets or sets whether the Button will show decorations or not. If <see langword="true"/> the glyphs that normally
    ///     bracket the Button Title and the <see cref="IsDefault"/> indicator will not be shown.
    /// </summary>
    public bool NoDecorations { get; set; }

    /// <summary>
    ///     Gets or sets whether the Button will include padding on each side of the Title.
    /// </summary>
    public bool NoPadding { get; set; }

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
