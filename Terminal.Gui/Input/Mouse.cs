namespace Terminal.Gui;

/// <summary>Mouse flags reported in <see cref="MouseEvent"/>.</summary>
/// <remarks>They just happen to map to the ncurses ones.</remarks>
[Flags]
public enum MouseFlags
{
    /// <summary>
    ///    No mouse event. This is the default value for <see cref="MouseEvent.Flags"/> when no mouse event is being reported.
    /// </summary>
    None = 0,

    /// <summary>The first mouse button was pressed.</summary>
    Button1Pressed = 0x2,

    /// <summary>The first mouse button was released.</summary>
    Button1Released = 0x1,

    /// <summary>The first mouse button was clicked (press+release).</summary>
    Button1Clicked = 0x4,

    /// <summary>The first mouse button was double-clicked.</summary>
    Button1DoubleClicked = 0x8,

    /// <summary>The first mouse button was triple-clicked.</summary>
    Button1TripleClicked = 0x10,

    /// <summary>The second mouse button was pressed.</summary>
    Button2Pressed = 0x80,

    /// <summary>The second mouse button was released.</summary>
    Button2Released = 0x40,

    /// <summary>The second mouse button was clicked (press+release).</summary>
    Button2Clicked = 0x100,

    /// <summary>The second mouse button was double-clicked.</summary>
    Button2DoubleClicked = 0x200,

    /// <summary>The second mouse button was triple-clicked.</summary>
    Button2TripleClicked = 0x400,

    /// <summary>The third mouse button was pressed.</summary>
    Button3Pressed = 0x2000,

    /// <summary>The third mouse button was released.</summary>
    Button3Released = 0x1000,

    /// <summary>The third mouse button was clicked (press+release).</summary>
    Button3Clicked = 0x4000,

    /// <summary>The third mouse button was double-clicked.</summary>
    Button3DoubleClicked = 0x8000,

    /// <summary>The third mouse button was triple-clicked.</summary>
    Button3TripleClicked = 0x10000,

    /// <summary>The fourth mouse button was pressed.</summary>
    Button4Pressed = 0x80000,

    /// <summary>The fourth mouse button was released.</summary>
    Button4Released = 0x40000,

    /// <summary>The fourth button was clicked (press+release).</summary>
    Button4Clicked = 0x100000,

    /// <summary>The fourth button was double-clicked.</summary>
    Button4DoubleClicked = 0x200000,

    /// <summary>The fourth button was triple-clicked.</summary>
    Button4TripleClicked = 0x400000,

    /// <summary>Flag: the shift key was pressed when the mouse button took place.</summary>
    ButtonShift = 0x2000000,

    /// <summary>Flag: the ctrl key was pressed when the mouse button took place.</summary>
    ButtonCtrl = 0x1000000,

    /// <summary>Flag: the alt key was pressed when the mouse button took place.</summary>
    ButtonAlt = 0x4000000,

    /// <summary>The mouse position is being reported in this event.</summary>
    ReportMousePosition = 0x8000000,

    /// <summary>Vertical button wheeled up.</summary>
    WheeledUp = 0x10000000,

    /// <summary>Vertical button wheeled down.</summary>
    WheeledDown = 0x20000000,

    /// <summary>Vertical button wheeled up while pressing ButtonCtrl.</summary>
    WheeledLeft = ButtonCtrl | WheeledUp,

    /// <summary>Vertical button wheeled down while pressing ButtonCtrl.</summary>
    WheeledRight = ButtonCtrl | WheeledDown,

    /// <summary>Mask that captures all the events.</summary>
    AllEvents = 0x7ffffff
}

// TODO: Merge MouseEvent and MouseEventEventArgs into a single class.

/// <summary>
///     Conveys the details of mouse events, such as coordinates and button state, from
///     ConsoleDrivers up to <see cref="Application"/> and Views.
/// </summary>
/// <remarks>
///     The <see cref="Application"/> class includes the <see cref="Application.MouseEvent"/> event which takes a
///     MouseEvent argument.
/// </remarks>
public class MouseEvent
{
    /// <summary>Flags indicating the kind of mouse event that is being posted.</summary>
    public MouseFlags Flags { get; set; }

    /// <summary>The View at the location for the mouse event.</summary>
    public View View { get; set; }

    /// <summary>The position of the mouse in <see cref="Gui.View.Viewport"/>-relative coordinates.</summary>
    public Point Position { get; set; }

    /// <summary>
    ///     The screen-relative mouse position.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Position"/> is <see cref="Gui.View.Viewport"/>-relative. When the mouse is grabbed by a view,
    ///         <see cref="ScreenPosition"/> provides the mouse position screen-relative coordinates, enabling the grabbed view to know how much the
    ///         mouse has moved.
    ///     </para>
    ///     <para>
    ///         Calculated and processed in <see cref="Application.OnMouseEvent(MouseEvent)"/>.
    ///     </para>
    /// </remarks>
    public Point ScreenPosition { get; set; }

    /// <summary>
    ///     Indicates if the current mouse event has first pressed <see langword="true"/>, latest released <see langword="false"/> or none <see langword="null"/>.
    /// </summary>
    public bool? IsMouseDown { get; set; }

    /// <summary>
    ///     Indicates if the current mouse event has been processed. Set this value to <see langword="true"/> to indicate the mouse
    ///     event was handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>Returns a <see cref="T:System.String"/> that represents the current <see cref="MouseEvent"/>.</summary>
    /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="MouseEvent"/>.</returns>
    public override string ToString () { return $"({Position}):{Flags}"; }
}
