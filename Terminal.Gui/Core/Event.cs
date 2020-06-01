//
// Evemts.cs: Events, Key mappings
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;

namespace Terminal.Gui {

	/// <summary>
	/// Identifies the state of the "shift"-keys within a event.
	/// </summary>
	public class KeyModifiers {
		/// <summary>
		/// Check if the Shift key was pressed or not.
		/// </summary>
		public bool Shift;
		/// <summary>
		/// Check if the Alt key was pressed or not.
		/// </summary>
		public bool Alt;
		/// <summary>
		/// Check if the Ctrl key was pressed or not.
		/// </summary>
		public bool Ctrl;
		/// <summary>
		/// Check if the Caps lock key was pressed or not.
		/// </summary>
		public bool Capslock;
		/// <summary>
		/// Check if the Num lock key was pressed or not.
		/// </summary>
		public bool Numlock;
		/// <summary>
		/// Check if the Scroll lock key was pressed or not.
		/// </summary>
		public bool Scrolllock;
	}

	/// <summary>
	/// The <see cref="Key"/> enumeration contains special encoding for some keys, but can also
	/// encode all the unicode values that can be passed.   
	/// </summary>
	/// <remarks>
	/// <para>
	///   If the <see cref="SpecialMask"/> is set, then the value is that of the special mask,
	///   otherwise, the value is the one of the lower bits (as extracted by <see cref="CharMask"/>)
	/// </para>
	/// <para>
	///   Control keys are the values between 1 and 26 corresponding to Control-A to Control-Z
	/// </para>
	/// <para>
	///   Unicode runes are also stored here, the letter 'A" for example is encoded as a value 65 (not surfaced in the enum).
	/// </para>
	/// </remarks>
	[Flags]
	public enum Key : uint {
		/// <summary>
		/// Mask that indicates that this is a character value, values outside this range
		/// indicate special characters like Alt-key combinations or special keys on the
		/// keyboard like function keys, arrows keys and so on.
		/// </summary>
		CharMask = 0xfffff,

		/// <summary>
		/// If the <see cref="SpecialMask"/> is set, then the value is that of the special mask,
		/// otherwise, the value is the one of the lower bits (as extracted by <see cref="CharMask"/>).
		/// </summary>
		SpecialMask = 0xfff00000,

		/// <summary>
		/// The key code for the user pressing Control-spacebar
		/// </summary>
		ControlSpace = 0,

		/// <summary>
		/// The key code for the user pressing Control-A
		/// </summary>
		ControlA = 1,
		/// <summary>
		/// The key code for the user pressing Control-B
		/// </summary>
		ControlB,
		/// <summary>
		/// The key code for the user pressing Control-C
		/// </summary>
		ControlC,
		/// <summary>
		/// The key code for the user pressing Control-D
		/// </summary>
		ControlD,
		/// <summary>
		/// The key code for the user pressing Control-E
		/// </summary>
		ControlE,
		/// <summary>
		/// The key code for the user pressing Control-F
		/// </summary>
		ControlF,
		/// <summary>
		/// The key code for the user pressing Control-G
		/// </summary>
		ControlG,
		/// <summary>
		/// The key code for the user pressing Control-H
		/// </summary>
		ControlH,
		/// <summary>
		/// The key code for the user pressing Control-I (same as the tab key).
		/// </summary>
		ControlI,
		/// <summary>
		/// The key code for the user pressing Control-J
		/// </summary>
		ControlJ,
		/// <summary>
		/// The key code for the user pressing Control-K
		/// </summary>
		ControlK,
		/// <summary>
		/// The key code for the user pressing Control-L
		/// </summary>
		ControlL,
		/// <summary>
		/// The key code for the user pressing Control-M
		/// </summary>
		ControlM,
		/// <summary>
		/// The key code for the user pressing Control-N (same as the return key).
		/// </summary>
		ControlN,
		/// <summary>
		/// The key code for the user pressing Control-O
		/// </summary>
		ControlO,
		/// <summary>
		/// The key code for the user pressing Control-P
		/// </summary>
		ControlP,
		/// <summary>
		/// The key code for the user pressing Control-Q
		/// </summary>
		ControlQ,
		/// <summary>
		/// The key code for the user pressing Control-R
		/// </summary>
		ControlR,
		/// <summary>
		/// The key code for the user pressing Control-S
		/// </summary>
		ControlS,
		/// <summary>
		/// The key code for the user pressing Control-T
		/// </summary>
		ControlT,
		/// <summary>
		/// The key code for the user pressing Control-U
		/// </summary>
		ControlU,
		/// <summary>
		/// The key code for the user pressing Control-V
		/// </summary>
		ControlV,
		/// <summary>
		/// The key code for the user pressing Control-W
		/// </summary>
		ControlW,
		/// <summary>
		/// The key code for the user pressing Control-X
		/// </summary>
		ControlX,
		/// <summary>
		/// The key code for the user pressing Control-Y
		/// </summary>
		ControlY,
		/// <summary>
		/// The key code for the user pressing Control-Z
		/// </summary>
		ControlZ,

		/// <summary>
		/// The key code for the user pressing the escape key
		/// </summary>
		Esc = 27,

		/// <summary>
		/// The key code for the user pressing the return key.
		/// </summary>
		Enter = '\n',

		/// <summary>
		/// The key code for the user pressing the space bar
		/// </summary>
		Space = 32,

		/// <summary>
		/// The key code for the user pressing the delete key.
		/// </summary>
		Delete = 127,

		/// <summary>
		/// When this value is set, the Key encodes the sequence Shift-KeyValue.
		/// </summary>
		ShiftMask = 0x10000000,

		/// <summary>
		///   When this value is set, the Key encodes the sequence Alt-KeyValue.
		///   And the actual value must be extracted by removing the AltMask.
		/// </summary>
		AltMask = 0x80000000,

		/// <summary>
		///   When this value is set, the Key encodes the sequence Ctrl-KeyValue.
		///   And the actual value must be extracted by removing the CtrlMask.
		/// </summary>
		CtrlMask = 0x40000000,

		/// <summary>
		/// Backspace key.
		/// </summary>
		Backspace = 0x100000,

		/// <summary>
		/// Cursor up key
		/// </summary>
		CursorUp,
		/// <summary>
		/// Cursor down key.
		/// </summary>
		CursorDown,
		/// <summary>
		/// Cursor left key.
		/// </summary>
		CursorLeft,
		/// <summary>
		/// Cursor right key.
		/// </summary>
		CursorRight,
		/// <summary>
		/// Page Up key.
		/// </summary>
		PageUp,
		/// <summary>
		/// Page Down key.
		/// </summary>
		PageDown,
		/// <summary>
		/// Home key
		/// </summary>
		Home,
		/// <summary>
		/// End key
		/// </summary>
		End,
		/// <summary>
		/// Delete character key
		/// </summary>
		DeleteChar,
		/// <summary>
		/// Insert character key
		/// </summary>
		InsertChar,
		/// <summary>
		/// F1 key.
		/// </summary>
		F1,
		/// <summary>
		/// F2 key.
		/// </summary>
		F2,
		/// <summary>
		/// F3 key.
		/// </summary>
		F3,
		/// <summary>
		/// F4 key.
		/// </summary>
		F4,
		/// <summary>
		/// F5 key.
		/// </summary>
		F5,
		/// <summary>
		/// F6 key.
		/// </summary>
		F6,
		/// <summary>
		/// F7 key.
		/// </summary>
		F7,
		/// <summary>
		/// F8 key.
		/// </summary>
		F8,
		/// <summary>
		/// F9 key.
		/// </summary>
		F9,
		/// <summary>
		/// F10 key.
		/// </summary>
		F10,
		/// <summary>
		/// F11 key.
		/// </summary>
		F11,
		/// <summary>
		/// F12 key.
		/// </summary>
		F12,
		/// <summary>
		/// The key code for the user pressing the tab key (forwards tab key).
		/// </summary>
		Tab,
		/// <summary>
		/// Shift-tab key (backwards tab key).
		/// </summary>
		BackTab,
		/// <summary>
		/// A key with an unknown mapping was raised.
		/// </summary>
		Unknown
	}

	/// <summary>
	/// Describes a keyboard event.
	/// </summary>
	public class KeyEvent {
		KeyModifiers keyModifiers;

		/// <summary>
		/// Symb olid definition for the key.
		/// </summary>
		public Key Key;

		/// <summary>
		///   The key value cast to an integer, you will typical use this for
		///   extracting the Unicode rune value out of a key, when none of the
		///   symbolic options are in use.
		/// </summary>
		public int KeyValue => (int)Key;

		/// <summary>
		/// Gets a value indicating whether the Shift key was pressed.
		/// </summary>
		/// <value><c>true</c> if is shift; otherwise, <c>false</c>.</value>
		public bool IsShift => keyModifiers.Shift;

		/// <summary>
		/// Gets a value indicating whether the Alt key was pressed (real or synthesized)
		/// </summary>
		/// <value><c>true</c> if is alternate; otherwise, <c>false</c>.</value>
		public bool IsAlt => keyModifiers.Alt;

		/// <summary>
		/// Determines whether the value is a control key (and NOT just the ctrl key)
		/// </summary>
		/// <value><c>true</c> if is ctrl; otherwise, <c>false</c>.</value>
		//public bool IsCtrl => ((uint)Key >= 1) && ((uint)Key <= 26);
		public bool IsCtrl => keyModifiers.Ctrl;

		/// <summary>
		/// Gets a value indicating whether the Caps lock key was pressed (real or synthesized)
		/// </summary>
		/// <value><c>true</c> if is alternate; otherwise, <c>false</c>.</value>
		public bool IsCapslock => keyModifiers.Capslock;

		/// <summary>
		/// Gets a value indicating whether the Num lock key was pressed (real or synthesized)
		/// </summary>
		/// <value><c>true</c> if is alternate; otherwise, <c>false</c>.</value>
		public bool IsNumlock => keyModifiers.Numlock;

		/// <summary>
		/// Gets a value indicating whether the Scroll lock key was pressed (real or synthesized)
		/// </summary>
		/// <value><c>true</c> if is alternate; otherwise, <c>false</c>.</value>
		public bool IsScrolllock => keyModifiers.Scrolllock;

		/// <summary>
		/// Constructs a new <see cref="KeyEvent"/>
		/// </summary>
		public KeyEvent ()
		{
			Key = Key.Unknown;
			keyModifiers = new KeyModifiers ();
		}

		/// <summary>
		///   Constructs a new <see cref="KeyEvent"/> from the provided Key value - can be a rune cast into a Key value
		/// </summary>
		public KeyEvent (Key k, KeyModifiers km)
		{
			Key = k;
			keyModifiers = km;
		}

		///<inheritdoc cref="ToString"/>
		public override string ToString ()
		{
			string msg = "";
			var key = this.Key;
			if (keyModifiers.Shift) {
				msg += "Shift-";
			}
			if (keyModifiers.Alt) {
				msg += "Alt-";
			}
			if (keyModifiers.Ctrl) {
				msg += "Ctrl-";
			}
			if (keyModifiers.Capslock) {
				msg += "Capslock-";
			}
			if (keyModifiers.Numlock) {
				msg += "Numlock-";
			}
			if (keyModifiers.Scrolllock) {
				msg += "Scrolllock-";
			}

			msg += $"{(((uint)this.KeyValue & (uint)Key.CharMask) > 27 ? $"{(char)this.KeyValue}" : $"{key}")}";

			return msg;
		}
	}

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
		/// Vertical button wheeled up.
		/// </summary>
		WheeledDown = unchecked((int)0x20000000),
		/// <summary>
		/// Mask that captures all the events.
		/// </summary>
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

		/// <summary>
		/// The offset X (column) location for the mouse event.
		/// </summary>
		public int OfX;

		/// <summary>
		/// The offset Y (column) location for the mouse event.
		/// </summary>
		public int OfY;

		/// <summary>
		/// The current view at the location for the mouse event.
		/// </summary>
		public View View;

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="MouseEvent"/>.
		/// </summary>
		/// <returns>A <see cref="T:System.String"/> that represents the current <see cref="MouseEvent"/>.</returns>
		public override string ToString ()
		{
			return $"({X},{Y}:{Flags}";
		}
	}
}
