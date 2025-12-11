namespace Terminal.Gui.Input;

/// <summary>
///     Provides an abstraction for common mouse operations and state.
///     Represents a mouse event, including position, button state, and other flags.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="View.MouseEvent"/> event uses this class.
///     </para>
/// </remarks>
public class Mouse : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Mouse"/> class.
    /// </summary>
    public Mouse () { }

    /// <summary>
    ///     Gets or sets a value indicating whether the mouse event was handled.
    /// </summary>
    /// <remarks>
    ///     Set this to <see langword="true"/> to prevent the event from being processed by other views.
    /// </remarks>
    public bool Handled { get; set; }

    /// <summary>
    ///     The timestamp when this mouse event was created. Used for multi-click detection timing.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    ///     Flags indicating the state of the mouse buttons and the type of event that occurred.
    /// </summary>
    public MouseFlags Flags { get; set; }

    /// <summary>
    ///     The screen-relative mouse position, in columns and rows.
    /// </summary>
    public Point ScreenPosition { get; set; }

    /// <summary>
    ///     The view that is the target of the mouse event.
    /// </summary>
    /// <remarks>
    ///     This is the deepest view in the view hierarchy that contains the mouse position.
    /// </remarks>
    public View? View { get; set; }

    /// <summary>
    ///     The position of the mouse in <see cref="View"/>'s viewport-relative coordinates.
    /// </summary>
    /// <remarks>
    ///     This property is only valid if <see cref="View"/> is not <see langword="null"/>.
    /// </remarks>
    public Point? Position { get; set; }

    /// <summary>
    ///     Gets a value indicating whether a mouse button was pressed.
    /// </summary>
    public bool IsPressed => Flags.HasFlag (MouseFlags.Button1Pressed)
                             || Flags.HasFlag (MouseFlags.Button2Pressed)
                             || Flags.HasFlag (MouseFlags.Button3Pressed)
                             || Flags.HasFlag (MouseFlags.Button4Pressed);

    /// <summary>
    ///     Gets a value indicating whether a mouse button was released.
    /// </summary>
    public bool IsReleased => Flags.HasFlag (MouseFlags.Button1Released)
                              || Flags.HasFlag (MouseFlags.Button2Released)
                              || Flags.HasFlag (MouseFlags.Button3Released)
                              || Flags.HasFlag (MouseFlags.Button4Released);

    /// <summary>
    ///     Gets a value indicating whether a single-click mouse event occurred.
    /// </summary>
    public bool IsSingleClicked => Flags.HasFlag (MouseFlags.Button1Clicked)
                                   || Flags.HasFlag (MouseFlags.Button2Clicked)
                                   || Flags.HasFlag (MouseFlags.Button3Clicked)
                                   || Flags.HasFlag (MouseFlags.Button4Clicked);

    /// <summary>
    ///     Gets a value indicating whether a double-click mouse event occurred.
    /// </summary>
    public bool IsDoubleClicked => Flags.HasFlag (MouseFlags.Button1DoubleClicked)
                                   || Flags.HasFlag (MouseFlags.Button2DoubleClicked)
                                   || Flags.HasFlag (MouseFlags.Button3DoubleClicked)
                                   || Flags.HasFlag (MouseFlags.Button4DoubleClicked);

    /// <summary>
    ///     Gets a value indicating whether a triple-click mouse event occurred.
    /// </summary>
    public bool IsTripleClicked => Flags.HasFlag (MouseFlags.Button1TripleClicked)
                                   || Flags.HasFlag (MouseFlags.Button2TripleClicked)
                                   || Flags.HasFlag (MouseFlags.Button3TripleClicked)
                                   || Flags.HasFlag (MouseFlags.Button4TripleClicked);

    /// <summary>
    ///     Gets a value indicating whether a single, double, or triple-click mouse event occurred.
    /// </summary>
    public bool IsSingleDoubleOrTripleClicked =>
        Flags.HasFlag (MouseFlags.Button1Clicked)
        || Flags.HasFlag (MouseFlags.Button2Clicked)
        || Flags.HasFlag (MouseFlags.Button3Clicked)
        || Flags.HasFlag (MouseFlags.Button4Clicked)
        || Flags.HasFlag (MouseFlags.Button1DoubleClicked)
        || Flags.HasFlag (MouseFlags.Button2DoubleClicked)
        || Flags.HasFlag (MouseFlags.Button3DoubleClicked)
        || Flags.HasFlag (MouseFlags.Button4DoubleClicked)
        || Flags.HasFlag (MouseFlags.Button1TripleClicked)
        || Flags.HasFlag (MouseFlags.Button2TripleClicked)
        || Flags.HasFlag (MouseFlags.Button3TripleClicked)
        || Flags.HasFlag (MouseFlags.Button4TripleClicked);

    /// <summary>
    ///     Gets a value indicating whether a mouse wheel event occurred.
    /// </summary>
    public bool IsWheel => Flags.HasFlag (MouseFlags.WheeledDown)
                           || Flags.HasFlag (MouseFlags.WheeledUp)
                           || Flags.HasFlag (MouseFlags.WheeledLeft)
                           || Flags.HasFlag (MouseFlags.WheeledRight);

    /// <summary>Returns a string that represents the current mouse event.</summary>
    /// <returns>A string that represents the current mouse event.</returns>
    public override string ToString () { return $"{Timestamp:ss.fff}:{ScreenPosition}:{Flags}:{View?.Id}:{Position}"; }
}
