namespace Terminal.Gui.ViewBase;

/// <summary>
///     A lightweight <see cref="View"/> that renders title text with focus-appropriate attributes
///     and raises directional <see cref="Command"/>s based on its <see cref="Orientation"/> and
///     <see cref="Direction"/>.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="TitleView"/> implements <see cref="IOrientation"/>. When <see cref="Orientation"/> is
///         <see cref="Orientation.Horizontal"/>, <see cref="TextFormatter.Direction"/> is set to
///         <see cref="TextDirection.LeftRight_TopBottom"/> and Left/Right arrow keys are bound to
///         <see cref="Command.Left"/>/<see cref="Command.Right"/>.
///         When <see cref="Orientation.Vertical"/>, text renders top-to-bottom and Up/Down arrow keys
///         are bound to <see cref="Command.Up"/>/<see cref="Command.Down"/>.
///     </para>
///     <para>
///         The owning view subscribes to <see cref="TitleView"/>'s directional commands (via
///         <see cref="View.CommandsToBubbleUp"/>) to handle navigation.
///     </para>
/// </remarks>
public sealed class TitleView : View, IOrientation
{
    private readonly OrientationHelper _orientationHelper;

    public TitleView ()
    {
        CanFocus = true;

        // Do not participate in tab navigation — focus is set by click or by the owning view.
        TabStop = TabBehavior.NoStop;
        Border.Settings = BorderSettings.None;
        SuperViewRendersLineCanvas = true;

        //AddCommand (Command.Up, () => false);
        //AddCommand (Command.Down, () => false);
        //AddCommand (Command.Left, () => false);
        //AddCommand (Command.Right, () => false);

        AddCommand (Command.Activate,
                    ctx =>
                    {
                        SetFocus ();

                        return true;
                    });

        AddCommand (Command.HotKey,
                    ctx =>
                    {
                        SetFocus ();

                        return true;
                    });

        // Remove Enter — title views should not respond to Enter
        KeyBindings.Remove (Key.Enter);

        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);

        MouseBindings.Clear ();
        MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.HotKey);

        _orientationHelper = new OrientationHelper (this);

        // Default to horizontal — call SetupKeyBindings directly because OrientationHelper's
        // default is also Horizontal, so setting it won't trigger OnOrientationChanged.
        SetupKeyBindings ();
    }

    /// <summary>
    ///     Gets or sets the navigation direction for this title view.
    /// </summary>
    /// <value>
    ///     <see cref="NavigationDirection.Backward"/> for titles on the top or left edge.
    ///     <see cref="NavigationDirection.Forward"/> for titles on the bottom or right edge.
    /// </value>
    public NavigationDirection Direction { get; set; }

    /// <inheritdoc/>
    public override string Text { get => base.Text; set => base.Text = Title = value; }

    /// <summary>
    ///     Binds the appropriate arrow keys to their directional <see cref="Command"/> based on
    ///     the current <see cref="Orientation"/>.
    /// </summary>
    private void SetupKeyBindings ()
    {
        KeyBindings.Remove (Key.CursorUp);
        KeyBindings.Remove (Key.CursorDown);
        KeyBindings.Remove (Key.CursorLeft);
        KeyBindings.Remove (Key.CursorRight);

        if (Orientation == Orientation.Horizontal)
        {
            KeyBindings.Add (Key.CursorLeft, Command.Left);
            KeyBindings.Add (Key.CursorRight, Command.Right);
        }
        else
        {
            KeyBindings.Add (Key.CursorUp, Command.Up);
            KeyBindings.Add (Key.CursorDown, Command.Down);
        }
    }

    #region IOrientation members

    /// <inheritdoc/>
    public Orientation Orientation { get => _orientationHelper.Orientation; set => _orientationHelper.Orientation = value; }

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        // Update TextFormatter direction to match the new orientation
        TextFormatter.Direction = newOrientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;

        SetupKeyBindings ();
    }

    #endregion
}
