using System;

namespace Terminal.Gui;

/// <summary>
/// Defines the event arguments for <see cref="KeyEvent"/>
/// </summary>
public class KeyEventArgs : EventArgs {
	/// <summary>
	/// Constructs.
	/// </summary>
	/// <param name="ke"></param>
	public KeyEventArgs (KeyEvent ke) => KeyEvent = ke;
	/// <summary>
	/// The <see cref="KeyEvent"/> for the event.
	/// </summary>
	public KeyEvent KeyEvent { get; set; }
	/// <summary>
	/// Indicates if the current Key event has already been processed and the driver should stop notifying any other event subscriber.
	/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
	/// </summary>
	public bool Handled { get; set; } = false;
}

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
/// <para>
///   Numerics keys are the values between 48 and 57 corresponding to 0 to 9
/// </para>
/// </para>
/// <para>
///   Upper alpha keys are the values between 65 and 90 corresponding to A to Z
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
	/// The key code representing null or empty
	/// </summary>
	Null = '\0',

	/// <summary>
	/// Backspace key.
	/// </summary>
	Backspace = 8,

	/// <summary>
	/// The key code for the user pressing the tab key (forwards tab key).
	/// </summary>
	Tab = 9,

	/// <summary>
	/// The key code for the user pressing the return key.
	/// </summary>
	Enter = '\n',

	/// <summary>
	/// The key code for the user pressing the clear key.
	/// </summary>
	Clear = 12,

	/// <summary>
	/// The key code for the user pressing the escape key
	/// </summary>
	Esc = 27,

	/// <summary>
	/// The key code for the user pressing the space bar
	/// </summary>
	Space = 32,

	/// <summary>
	/// Digit 0.
	/// </summary>
	D0 = 48,
	/// <summary>
	/// Digit 1.
	/// </summary>
	D1,
	/// <summary>
	/// Digit 2.
	/// </summary>
	D2,
	/// <summary>
	/// Digit 3.
	/// </summary>
	D3,
	/// <summary>
	/// Digit 4.
	/// </summary>
	D4,
	/// <summary>
	/// Digit 5.
	/// </summary>
	D5,
	/// <summary>
	/// Digit 6.
	/// </summary>
	D6,
	/// <summary>
	/// Digit 7.
	/// </summary>
	D7,
	/// <summary>
	/// Digit 8.
	/// </summary>
	D8,
	/// <summary>
	/// Digit 9.
	/// </summary>
	D9,

	/// <summary>
	/// The key code for the user pressing Shift-A
	/// </summary>
	A = 65,
	/// <summary>
	/// The key code for the user pressing Shift-B
	/// </summary>
	B,
	/// <summary>
	/// The key code for the user pressing Shift-C
	/// </summary>
	C,
	/// <summary>
	/// The key code for the user pressing Shift-D
	/// </summary>
	D,
	/// <summary>
	/// The key code for the user pressing Shift-E
	/// </summary>
	E,
	/// <summary>
	/// The key code for the user pressing Shift-F
	/// </summary>
	F,
	/// <summary>
	/// The key code for the user pressing Shift-G
	/// </summary>
	G,
	/// <summary>
	/// The key code for the user pressing Shift-H
	/// </summary>
	H,
	/// <summary>
	/// The key code for the user pressing Shift-I
	/// </summary>
	I,
	/// <summary>
	/// The key code for the user pressing Shift-J
	/// </summary>
	J,
	/// <summary>
	/// The key code for the user pressing Shift-K
	/// </summary>
	K,
	/// <summary>
	/// The key code for the user pressing Shift-L
	/// </summary>
	L,
	/// <summary>
	/// The key code for the user pressing Shift-M
	/// </summary>
	M,
	/// <summary>
	/// The key code for the user pressing Shift-N
	/// </summary>
	N,
	/// <summary>
	/// The key code for the user pressing Shift-O
	/// </summary>
	O,
	/// <summary>
	/// The key code for the user pressing Shift-P
	/// </summary>
	P,
	/// <summary>
	/// The key code for the user pressing Shift-Q
	/// </summary>
	Q,
	/// <summary>
	/// The key code for the user pressing Shift-R
	/// </summary>
	R,
	/// <summary>
	/// The key code for the user pressing Shift-S
	/// </summary>
	S,
	/// <summary>
	/// The key code for the user pressing Shift-T
	/// </summary>
	T,
	/// <summary>
	/// The key code for the user pressing Shift-U
	/// </summary>
	U,
	/// <summary>
	/// The key code for the user pressing Shift-V
	/// </summary>
	V,
	/// <summary>
	/// The key code for the user pressing Shift-W
	/// </summary>
	W,
	/// <summary>
	/// The key code for the user pressing Shift-X
	/// </summary>
	X,
	/// <summary>
	/// The key code for the user pressing Shift-Y
	/// </summary>
	Y,
	/// <summary>
	/// The key code for the user pressing Shift-Z
	/// </summary>
	Z,
	/// <summary>
	/// The key code for the user pressing A
	/// </summary>
	a = 97,
	/// <summary>
	/// The key code for the user pressing B
	/// </summary>
	b,
	/// <summary>
	/// The key code for the user pressing C
	/// </summary>
	c,
	/// <summary>
	/// The key code for the user pressing D
	/// </summary>
	d,
	/// <summary>
	/// The key code for the user pressing E
	/// </summary>
	e,
	/// <summary>
	/// The key code for the user pressing F
	/// </summary>
	f,
	/// <summary>
	/// The key code for the user pressing G
	/// </summary>
	g,
	/// <summary>
	/// The key code for the user pressing H
	/// </summary>
	h,
	/// <summary>
	/// The key code for the user pressing I
	/// </summary>
	i,
	/// <summary>
	/// The key code for the user pressing J
	/// </summary>
	j,
	/// <summary>
	/// The key code for the user pressing K
	/// </summary>
	k,
	/// <summary>
	/// The key code for the user pressing L
	/// </summary>
	l,
	/// <summary>
	/// The key code for the user pressing M
	/// </summary>
	m,
	/// <summary>
	/// The key code for the user pressing N
	/// </summary>
	n,
	/// <summary>
	/// The key code for the user pressing O
	/// </summary>
	o,
	/// <summary>
	/// The key code for the user pressing P
	/// </summary>
	p,
	/// <summary>
	/// The key code for the user pressing Q
	/// </summary>
	q,
	/// <summary>
	/// The key code for the user pressing R
	/// </summary>
	r,
	/// <summary>
	/// The key code for the user pressing S
	/// </summary>
	s,
	/// <summary>
	/// The key code for the user pressing T
	/// </summary>
	t,
	/// <summary>
	/// The key code for the user pressing U
	/// </summary>
	u,
	/// <summary>
	/// The key code for the user pressing V
	/// </summary>
	v,
	/// <summary>
	/// The key code for the user pressing W
	/// </summary>
	w,
	/// <summary>
	/// The key code for the user pressing X
	/// </summary>
	x,
	/// <summary>
	/// The key code for the user pressing Y
	/// </summary>
	y,
	/// <summary>
	/// The key code for the user pressing Z
	/// </summary>
	z,
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
	/// Cursor up key
	/// </summary>
	CursorUp = 0x100000,
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
	/// Home key.
	/// </summary>
	Home,
	/// <summary>
	/// End key.
	/// </summary>
	End,

	/// <summary>
	/// Insert character key.
	/// </summary>
	InsertChar,

	/// <summary>
	/// Delete character key.
	/// </summary>
	DeleteChar,

	/// <summary>
	/// Shift-tab key (backwards tab key).
	/// </summary>
	BackTab,

	/// <summary>
	/// Print screen character key.
	/// </summary>
	PrintScreen,

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
	/// F13 key.
	/// </summary>
	F13,
	/// <summary>
	/// F14 key.
	/// </summary>
	F14,
	/// <summary>
	/// F15 key.
	/// </summary>
	F15,
	/// <summary>
	/// F16 key.
	/// </summary>
	F16,
	/// <summary>
	/// F17 key.
	/// </summary>
	F17,
	/// <summary>
	/// F18 key.
	/// </summary>
	F18,
	/// <summary>
	/// F19 key.
	/// </summary>
	F19,
	/// <summary>
	/// F20 key.
	/// </summary>
	F20,
	/// <summary>
	/// F21 key.
	/// </summary>
	F21,
	/// <summary>
	/// F22 key.
	/// </summary>
	F22,
	/// <summary>
	/// F23 key.
	/// </summary>
	F23,
	/// <summary>
	/// F24 key.
	/// </summary>
	F24,

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
	/// Symbolic definition for the key.
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
	public bool IsShift => keyModifiers.Shift || Key == Key.BackTab;

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

	/// <summary>
	/// Pretty prints the KeyEvent
	/// </summary>
	/// <returns></returns>
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

		msg += $"{((Key)KeyValue != Key.Unknown && ((uint)this.KeyValue & (uint)Key.CharMask) > 27 ? $"{(char)this.KeyValue}" : $"{key}")}";

		return msg;
	}
}
