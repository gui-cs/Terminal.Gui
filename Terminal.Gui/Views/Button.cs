namespace Terminal.Gui.Views;

/// <summary>
///     Raises the <see cref="View.Accepting"/> and <see cref="View.Accepted"/> events when the user presses <see cref="View.HotKey"/>,
///         <c>Enter</c>, or <c>Space</c> or clicks with the mouse.
/// </summary>
/// <remarks>
///     <para>Use <see cref="View.HotKeySpecifier"/> to change the hot key specifier from the default of ('_').</para>
///     <para>
///         Button can act as the default <see cref="Command.Accept"/> handler for all peer-Views. See
///         <see cref="IsDefault"/>.
///     </para>
///     <para>
///         Set <see cref="View.MouseHoldRepeat"/> to <see langword="true"/> to have the
///         <see cref="View.Accepting"/> event invoked repeatedly while the button is pressed.
///     </para>
///     <para>
///         Button does not raise <see cref="View.Activating"/> events.
///     </para>
///     <para>Default key bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Key</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Space</term> <description>Accepts the button (<see cref="Command.Accept"/>).</description>
///         </item>
///         <item>
///             <term>Enter</term> <description>Accepts the button (<see cref="Command.Accept"/>).</description>
///         </item>
///     </list>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click / Double-Click / Triple-Click</term>
///             <description>Accepts the button (<see cref="Command.Accept"/>).</description>
///         </item>
///     </list>
/// </remarks>
public class Button : View, IDesignable, IAcceptTarget
{
    private readonly Rune _leftBracket;
    private readonly Rune _leftDefault;
    private readonly Rune _rightBracket;
    private readonly Rune _rightDefault;

    /// <summary>
    ///     Gets or sets whether <see cref="Button"/>s are shown with a shadow effect by default.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyles DefaultShadow { get; set; } = ShadowStyles.Opaque;

    /// <summary>
    ///     Gets or sets the default Highlight Style.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static MouseState DefaultMouseHighlightStates { get; set; } = MouseState.In | MouseState.Pressed | MouseState.PressedOutside;

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

        KeyBindings.ReplaceCommands (Key.Space, Command.Accept);
        KeyBindings.ReplaceCommands (Key.Enter, Command.Accept);

        // Replace default Accept binding with HotKey for mouse clicks
        // These are managed dynamically when MouseHoldRepeat changes
        SetMouseBindings (MouseHoldRepeat);

        TitleChanged += Button_TitleChanged;

        // Apply the initial shadow via a virtual method so that subclasses (e.g. ScrollButton)
        // can override GetDefaultShadowStyle() to return null, completely avoiding the
        // create-then-destroy allocation cycle that would otherwise occur.
        base.ShadowStyle = GetDefaultShadowStyle ();
        MouseHighlightStates = DefaultMouseHighlightStates;
    }

    /// <summary>
    ///     Returns the shadow style that should be applied during construction. Subclasses that
    ///     never show a shadow (e.g. <see cref="ScrollButton"/> or internal buttons used by arrangement UI)
    ///     should override this to return <see langword="null"/> to avoid the create-then-destroy
    ///     allocation pattern.
    /// </summary>
    /// <returns>
    ///     <see cref="DefaultShadow"/> by default. Return <see langword="null"/> to construct without
    ///     any shadow infrastructure.
    /// </returns>
    protected virtual ShadowStyles? GetDefaultShadowStyle () => DefaultShadow;

    /// <inheritdoc/>
    protected override void OnMouseHoldRepeatChanged (ValueChangedEventArgs<MouseFlags?> args) => SetMouseBindings (args.NewValue);

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
            MouseBindings.Add (mouseHoldRepeat.Value, Command.Accept);
        }
        else
        {
            // MouseHoldRepeat disabled: Remove ALL Click/Release/Press bindings, add only Clicked→HotKey
            MouseBindings.Remove (MouseFlags.LeftButtonClicked);
            MouseBindings.Remove (MouseFlags.LeftButtonReleased);

            // Add Clicked→HotKey bindings (default behavior)
            MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept);
            MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
            MouseBindings.Add (MouseFlags.LeftButtonTripleClicked, Command.Accept);
        }
    }

    /// <inheritdoc />
    protected override void OnHotKeyCommand (ICommandContext? commandContext)
    {
        InvokeCommand (Command.Accept);
    }

    private void Button_TitleChanged (object? sender, EventArgs<string> e)
    {
        base.Text = e.Value;
        TextFormatter.HotKeySpecifier = HotKeySpecifier;
    }

    /// <inheritdoc/>
    public override string Text { get => Title; set => base.Text = Title = value; }

    /// <inheritdoc/>
    public override Rune HotKeySpecifier { get => base.HotKeySpecifier; set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value; }

    /// <inheritdoc />
    public bool IsDefault
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

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
