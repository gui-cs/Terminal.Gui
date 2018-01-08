//
// Evemts.cs: Events, Key mappings
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;

namespace Terminal {

	/// <summary>
	/// The Key enumeration contains special encoding for some keys, but can also
	/// encode all the unicode values that can be passed.   
	/// </summary>
	/// <remarks>
	/// <para>
	///   If the SpecialMask is set, then the value is that of the special mask,
	///   otherwise, the value is the one of the lower bits (as extracted by CharMask)
	/// </para>
	/// <para>
	///   Control keys are the values between 1 and 26 corresponding to Control-A to Control-Z
	/// </para>
	/// </remarks>
	public enum Key : uint {
		CharMask = 0xfffff,
		SpecialMask = 0xfff00000,
		ControlA = 1,
		ControlB,
		ControlC,
		ControlD,
		ControlE,
		ControlF,
		ControlG,
		ControlH,
		ControlI,
		Tab = ControlI,
		ControlJ,
		ControlK,
		ControlL,
		ControlM,
		ControlN,
		ControlO,
		ControlP,
		ControlQ,
		ControlR,
		ControlS,
		ControlT,
		ControlU,
		ControlV,
		ControlW,
		ControlX,
		ControlY,
		ControlZ,
		Esc = 27,
		Enter = '\n',
		Space = 32,
		Delete = 127,

		AltMask = 0x80000000,

		Backspace = 0x100000,
		CursorUp,
		CursorDown,
		CursorLeft,
		CursorRight,
		PageUp,
		PageDown,
		Home,
		End,
		DeleteChar,
		InsertChar,
		F1,
		F2,
		F3,
		F4,
		F5,
		F6,
		F7,
		F8,
		F9,
		F10,
		BackTab,
		Unknown
	}

	/// <summary>
	/// Describes a keyboard event
	/// </summary>
	public struct KeyEvent {
		public Key Key;
		public int KeyValue => (int)Key;

		/// <summary>
		/// Gets a value indicating whether the Alt key was pressed (real or synthesized)
		/// </summary>
		/// <value><c>true</c> if is alternate; otherwise, <c>false</c>.</value>
		public bool IsAlt => (Key & Key.AltMask) != 0;

		/// <summary>
		/// Determines whether the value is a control key
		/// </summary>
		/// <value><c>true</c> if is ctrl; otherwise, <c>false</c>.</value>
		public bool IsCtrl => ((uint)Key >= 1) && ((uint)Key <= 26);

		public KeyEvent (Key k)
		{
			Key = k;
		}
	}

	/// <summary>
	/// Mouse flags reported in MouseEvent.
	/// </summary>
	/// <remarks>
	/// They just happen to map to the ncurses ones.
	/// </remarks>
	[Flags]
	public enum MouseFlags {
		Button1Pressed = unchecked((int)0x2),
		Button1Released = unchecked((int)0x1),
		Button1Clicked = unchecked((int)0x4),
		Button1DoubleClicked = unchecked((int)0x8),
		Button1TripleClicked = unchecked((int)0x10),
		Button2Pressed = unchecked((int)0x80),
		Button2Released = unchecked((int)0x40),
		Button2Clicked = unchecked((int)0x100),
		Button2DoubleClicked = unchecked((int)0x200),
		Button2TrippleClicked = unchecked((int)0x400),
		Button3Pressed = unchecked((int)0x2000),
		Button3Released = unchecked((int)0x1000),
		Button3Clicked = unchecked((int)0x4000),
		Button3DoubleClicked = unchecked((int)0x8000),
		Button3TripleClicked = unchecked((int)0x10000),
		Button4Pressed = unchecked((int)0x80000),
		Button4Released = unchecked((int)0x40000),
		Button4Clicked = unchecked((int)0x100000),
		Button4DoubleClicked = unchecked((int)0x200000),
		Button4TripleClicked = unchecked((int)0x400000),
		ButtonShift = unchecked((int)0x2000000),
		ButtonCtrl = unchecked((int)0x1000000),
		ButtonAlt = unchecked((int)0x4000000),
		ReportMousePosition = unchecked((int)0x8000000),
		AllEvents = unchecked((int)0x7ffffff),
	}

	/// <summary>
	/// Describes a mouse event
	/// </summary>
	public struct MouseEvent {
		/// <summary>
		/// The X (column) location for the mouse event.
		/// </summary>
		public int X;

		/// <summary>
		/// The Y (column) location for the mouse event.
		/// </summary>
		public int Y;

		/// <summary>
		/// Flags indicating the kind of mouse event that is being posted.
		/// </summary>
		public MouseFlags Flags;
	}

	public class Event {
		public class Key : Event {
			public int Code { get; private set; }
			public bool Alt { get; private set; }
			public Key (int code)
			{
				Code = code;
			}
		}

		public class Mouse : Event {
		}

		public static Event CreateMouseEvent ()
		{
			return new Mouse ();
		}

		public static Event CreateKeyEvent (int code)
		{
			return new Key (code);
		}

	}

}