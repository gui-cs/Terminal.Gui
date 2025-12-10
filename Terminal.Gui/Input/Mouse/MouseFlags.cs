namespace Terminal.Gui.Input;

/// <summary>Mouse flags reported in <see cref="MouseEventArgs"/>.</summary>
/// <remarks>
///     <para>
///         The definition of these flags is based on the ncurses mouse event reporting.
///     </para>
///     <para>
///         This enum provides both numbered button flags (Button1, Button2, Button3, Button4) and semantic aliases
///         (LeftButton, MiddleButton, RightButton) for improved code readability. The numbered flags follow the
///         ncurses convention, while the semantic aliases map to the standard mouse button layout:
///         Button1 = Left, Button2 = Middle, Button3 = Right, Button4 = Extra/XButton1.
///     </para>
///     <para>
///         Each button supports multiple event types: Pressed, Released, Clicked, DoubleClicked, and TripleClicked.
///         Additionally, modifier flags (ButtonShift, ButtonCtrl, ButtonAlt) can be combined with button events using
///         bitwise OR operations.
///     </para>
/// </remarks>
[Flags]
public enum MouseFlags
{
    /// <summary>
    ///     No mouse event. This is the default value for <see cref="MouseEventArgs.Flags"/> when no mouse event is being
    ///     reported.
    /// </summary>
    None = 0,

    /// <summary>The first mouse button was pressed.</summary>
    Button1Pressed = 0x2,

    /// <summary>
    ///     Indicates that the left mouse button has been pressed.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button1Pressed"/> and can be used interchangeably to
    ///     refer to a left mouse button pressed event.
    /// </remarks>
    LeftButtonPressed = Button1Pressed,

    /// <summary>The first mouse button was released.</summary>
    Button1Released = 0x1,

    /// <summary>
    ///     Indicates that the left mouse button has been released.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button1Released"/> and can be used interchangeably to
    ///     refer to a left mouse button released event.
    /// </remarks>
    LeftButtonReleased = Button1Released,

    /// <summary>The first mouse button was clicked (press+release).</summary>
    Button1Clicked = 0x4,

    /// <summary>
    ///     Indicates that the left mouse button was clicked.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button1Clicked"/> and can be used interchangeably to
    ///     refer to a left mouse button click event.
    /// </remarks>
    LeftButtonClicked = Button1Clicked,

    /// <summary>The first mouse button was double-clicked.</summary>
    Button1DoubleClicked = 0x8,

    /// <summary>
    ///     Indicates that the left mouse button was double-clicked.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button1DoubleClicked"/> and can be used interchangeably to
    ///     refer to a left mouse button double-click event.
    /// </remarks>
    LeftButtonDoubleClicked = Button1DoubleClicked,

    /// <summary>The first mouse button was triple-clicked.</summary>
    Button1TripleClicked = 0x10,

    /// <summary>
    ///     Indicates that the left mouse button was triple-clicked.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button1TripleClicked"/> and can be used interchangeably to
    ///     refer to a left mouse button triple-click event.
    /// </remarks>
    LeftButtonTripleClicked = Button1TripleClicked,

    /// <summary>The second mouse button was pressed.</summary>
    Button2Pressed = 0x80,

    /// <summary>
    ///     Indicates that the middle mouse button has been pressed.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button2Pressed"/> and can be used interchangeably to
    ///     refer to a middle mouse button pressed event.
    /// </remarks>
    MiddleButtonPressed = Button2Pressed,

    /// <summary>The second mouse button was released.</summary>
    Button2Released = 0x40,

    /// <summary>
    ///     Indicates that the middle mouse button has been released.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button2Released"/> and can be used interchangeably to
    ///     refer to a middle mouse button released event.
    /// </remarks>
    MiddleButtonReleased = Button2Released,

    /// <summary>The second mouse button was clicked (press+release).</summary>
    Button2Clicked = 0x100,

    /// <summary>
    ///     Indicates that the middle mouse button was clicked.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button2Clicked"/> and can be used interchangeably to
    ///     refer to a middle mouse button click event.
    /// </remarks>
    MiddleButtonClicked = Button2Clicked,

    /// <summary>The second mouse button was double-clicked.</summary>
    Button2DoubleClicked = 0x200,

    /// <summary>
    ///     Indicates that the middle mouse button was double-clicked.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button2DoubleClicked"/> and can be used interchangeably to
    ///     refer to a middle mouse button double-click event.
    /// </remarks>
    MiddleButtonDoubleClicked = Button2DoubleClicked,

    /// <summary>The second mouse button was triple-clicked.</summary>
    Button2TripleClicked = 0x400,

    /// <summary>
    ///     Indicates that the middle mouse button was triple-clicked.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button2TripleClicked"/> and can be used interchangeably to
    ///     refer to a middle mouse button triple-click event.
    /// </remarks>
    MiddleButtonTripleClicked = Button2TripleClicked,

    /// <summary>The third mouse button was pressed.</summary>
    Button3Pressed = 0x2000,

    /// <summary>
    ///     Indicates that the right mouse button has been pressed.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button3Pressed"/> and can be used interchangeably to
    ///     refer to a right mouse button pressed event.
    /// </remarks>
    RightButtonPressed = Button3Pressed,

    /// <summary>The third mouse button was released.</summary>
    Button3Released = 0x1000,

    /// <summary>
    ///     Indicates that the right mouse button has been released.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button3Released"/> and can be used interchangeably to
    ///     refer to a right mouse button released event.
    /// </remarks>
    RightButtonReleased = Button3Released,

    /// <summary>The third mouse button was clicked (press+release).</summary>
    Button3Clicked = 0x4000,

    /// <summary>
    ///     Indicates that the right mouse button was clicked.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button3Clicked"/> and can be used interchangeably to
    ///     refer to a right mouse button click event.
    /// </remarks>
    RightButtonClicked = Button3Clicked,

    /// <summary>The third mouse button was double-clicked.</summary>
    Button3DoubleClicked = 0x8000,

    /// <summary>
    ///     Indicates that the right mouse button was double-clicked.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button3DoubleClicked"/> and can be used interchangeably to
    ///     refer to a right mouse button double-click event.
    /// </remarks>
    RightButtonDoubleClicked = Button3DoubleClicked,

    /// <summary>The third mouse button was triple-clicked.</summary>
    Button3TripleClicked = 0x10000,

    /// <summary>
    ///     Indicates that the right mouse button was triple-clicked.
    /// </summary>
    /// <remarks>
    ///     This value is an alias for <see cref="Button3TripleClicked"/> and can be used interchangeably to
    ///     refer to a right mouse button triple-click event.
    /// </remarks>
    RightButtonTripleClicked = Button3TripleClicked,

    /// <summary>The fourth mouse button was pressed.</summary>
    Button4Pressed = 0x80000,

    /// <summary>The fourth mouse button was released.</summary>
    Button4Released = 0x40000,

    /// <summary>The fourth mouse button was clicked.</summary>
    Button4Clicked = 0x100000,

    /// <summary>The fourth mouse button was double-clicked.</summary>
    Button4DoubleClicked = 0x200000,

    /// <summary>The fourth mouse button was triple-clicked.</summary>
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
