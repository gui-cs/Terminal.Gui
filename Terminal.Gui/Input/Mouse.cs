using System;

namespace Terminal.Gui;

/// <summary>
/// Mouse flags reported in <see cref="MouseEvent"/>.
/// </summary>
/// <remarks>
/// They just happen to map to the ncurses ones.
/// </remarks>
[Flags]
public enum MouseFlags {
	/// <summary>
	/// The first mouse button was pressed.
	/// </summary>
	Button1Pressed = unchecked((int)0x2),
	/// <summary>
	/// The first mouse button was released.
	/// </summary>
	Button1Released = unchecked((int)0x1),
	/// <summary>
	/// The first mouse button was clicked (press+release).
	/// </summary>
	Button1Clicked = unchecked((int)0x4),
	/// <summary>
	/// The first mouse button was double-clicked.
	/// </summary>
	Button1DoubleClicked = unchecked((int)0x8),
	/// <summary>
	/// The first mouse button was triple-clicked.
	/// </summary>
	Button1TripleClicked = unchecked((int)0x10),
	/// <summary>
	/// The second mouse button was pressed.
	/// </summary>
	Button2Pressed = unchecked((int)0x80),
	/// <summary>
	/// The second mouse button was released.
	/// </summary>
	Button2Released = unchecked((int)0x40),
	/// <summary>
	/// The second mouse button was clicked (press+release).
	/// </summary>
	Button2Clicked = unchecked((int)0x100),
	/// <summary>
	/// The second mouse button was double-clicked.
	/// </summary>
	Button2DoubleClicked = unchecked((int)0x200),
	/// <summary>
	/// The second mouse button was triple-clicked.
	/// </summary>
	Button2TripleClicked = unchecked((int)0x400),
	/// <summary>
	/// The third mouse button was pressed.
	/// </summary>
	Button3Pressed = unchecked((int)0x2000),
	/// <summary>
	/// The third mouse button was released.
	/// </summary>
	Button3Released = unchecked((int)0x1000),
	/// <summary>
	/// The third mouse button was clicked (press+release).
	/// </summary>
	Button3Clicked = unchecked((int)0x4000),
	/// <summary>
	/// The third mouse button was double-clicked.
	/// </summary>
	Button3DoubleClicked = unchecked((int)0x8000),
	/// <summary>
	/// The third mouse button was triple-clicked.
	/// </summary>
	Button3TripleClicked = unchecked((int)0x10000),
	/// <summary>
	/// The fourth mouse button was pressed.
	/// </summary>
	Button4Pressed = unchecked((int)0x80000),
	/// <summary>
	/// The fourth mouse button was released.
	/// </summary>
	Button4Released = unchecked((int)0x40000),
	/// <summary>
	/// The fourth button was clicked (press+release).
	/// </summary>
	Button4Clicked = unchecked((int)0x100000),
	/// <summary>
	/// The fourth button was double-clicked.
	/// </summary>
	Button4DoubleClicked = unchecked((int)0x200000),
	/// <summary>
	/// The fourth button was triple-clicked.
	/// </summary>
	Button4TripleClicked = unchecked((int)0x400000),
	/// <summary>
	/// Flag: the shift key was pressed when the mouse button took place.
	/// </summary>
	ButtonShift = unchecked((int)0x2000000),
	/// <summary>
	/// Flag: the ctrl key was pressed when the mouse button took place.
	/// </summary>
	ButtonCtrl = unchecked((int)0x1000000),
	/// <summary>
	/// Flag: the alt key was pressed when the mouse button took place.
	/// </summary>
	ButtonAlt = unchecked((int)0x4000000),
	/// <summary>
	/// The mouse position is being reported in this event.
	/// </summary>
	ReportMousePosition = unchecked((int)0x8000000),
	/// <summary>
	/// Vertical button wheeled up.
	/// </summary>
	WheeledUp = unchecked((int)0x10000000),
	/// <summary>
	/// Vertical button wheeled down.
	/// </summary>
	WheeledDown = unchecked((int)0x20000000),
	/// <summary>
	/// Vertical button wheeled up while pressing ButtonShift.
	/// </summary>
	WheeledLeft = ButtonShift | WheeledUp,
	/// <summary>
	/// Vertical button wheeled down while pressing ButtonShift.
	/// </summary>
	WheeledRight = ButtonShift | WheeledDown,
	/// <summary>
	/// Mask that captures all the events.
	/// </summary>
	AllEvents = unchecked((int)0x7ffffff),
}

// TODO: Merge MouseEvent and MouseEventEventArgs into a single class.

/// <summary>
/// Low-level construct that conveys the details of mouse events, such
/// as coordinates and button state, from ConsoleDrivers up to <see cref="Application"/> and
/// Views.
/// </summary>
/// <remarks>The <see cref="Application"/> class includes the <see cref="Application.MouseEvent"/>
/// Action which takes a MouseEvent argument.</remarks>
public class MouseEvent {
	/// <summary>
	/// The X (column) location for the mouse event.
	/// </summary>
	public int X { get; set; }

	/// <summary>
	/// The Y (column) location for the mouse event.
	/// </summary>
	public int Y { get; set; }

	/// <summary>
	/// Flags indicating the kind of mouse event that is being posted.
	/// </summary>
	public MouseFlags Flags { get; set; }

	/// <summary>
	/// The offset X (column) location for the mouse event.
	/// </summary>
	public int OfX { get; set; }

	/// <summary>
	/// The offset Y (column) location for the mouse event.
	/// </summary>
	public int OfY { get; set; }

	/// <summary>
	/// The current view at the location for the mouse event.
	/// </summary>
	public View View { get; set; }

	/// <summary>
	/// Indicates if the current mouse event has already been processed and the driver should stop notifying any other event subscriber.
	/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Returns a <see cref="T:System.String"/> that represents the current <see cref="MouseEvent"/>.
	/// </summary>
	/// <returns>A <see cref="T:System.String"/> that represents the current <see cref="MouseEvent"/>.</returns>
	public override string ToString ()
	{
		return $"({X},{Y}:{Flags}";
	}
}