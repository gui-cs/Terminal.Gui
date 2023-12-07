using System;
using System.Text;

namespace Terminal.Gui;

/// <summary>
/// Defines the event arguments for keyboard events.
/// </summary>
public class KeyEventArgs : EventArgs {

	/// <summary>
	/// Constructs a new <see cref="KeyEventArgs"/>
	/// </summary>
	public KeyEventArgs () : this (Key.Unknown) { }

	/// <summary>
	///   Constructs a new <see cref="KeyEventArgs"/> from the provided Key value - can be a rune cast into a Key value
	/// </summary>
	/// <param name="k">The key</param>
	public KeyEventArgs (Key k)
	{
		Key = k;
	}

	/// <summary>
	/// Indicates if the current Key event has already been processed and the driver should stop notifying any other event subscriber.
	/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
	/// </summary>
	public bool Handled { get; set; } = false;

	/// <summary>
	/// Symbolic definition for the key.
	/// </summary>
	public Key Key { get; set; }

	/// <summary>
	/// The key value as a Rune. This is the actual value of the key pressed, and is independent of the modifiers.
	/// </summary>
	/// <remarks>
	/// If the key pressed is a letter, this will be the upper or lower case letter depending on whether the shift key is pressed.
	/// If the key is outside of the <see cref="Key.CharMask"/> range, this will be <see langword="null"/>.
	/// </remarks>
	public Rune AsRune => GetKeyAsRune (Key);

	/// <summary>
	/// Gets a value indicating whether the Shift key was pressed.
	/// </summary>
	/// <value><see langword="true"/> if is shift; otherwise, <see langword="false"/>.</value>
	public bool IsShift => (Key & Key.ShiftMask) != 0;

	/// <summary>
	/// Gets a value indicating whether the Alt key was pressed (real or synthesized)
	/// </summary>
	/// <value><see langword="true"/> if is alternate; otherwise, <see langword="false"/>.</value>
	public bool IsAlt => (Key & Key.AltMask) != 0;

	/// <summary>
	/// Gets a value indicating whether the Ctrl key was pressed.
	/// </summary>
	/// <value><see langword="true"/> if is ctrl; otherwise, <see langword="false"/>.</value>
	public bool IsCtrl => (Key & Key.CtrlMask) != 0;

	/// <summary>
	/// Pretty prints the KeyEvent
	/// </summary>
	/// <returns></returns>
	public override string ToString ()
	{
		return ToString (Key, (Rune)'+');
	}

	private static Rune GetKeyAsRune (Key key)
	{
		if (key is Key.Null or Key.SpecialMask) {
			return default;
		}

		// Extract the base key (removing modifier flags)
		Key baseKey = key & ~Key.CtrlMask & ~Key.AltMask & ~Key.ShiftMask;

		switch (baseKey) {
		case >= Key.A and <= Key.Z when !key.HasFlag (Key.ShiftMask):
			return new Rune ((char)(baseKey + 32));
		case >= Key.A and <= Key.Z:
			return new Rune ((char)baseKey);
		case >= Key.Space and < Key.A:
			return new Rune ((char)baseKey);
		}

		if (Enum.IsDefined(typeof(Key), baseKey)) {
			return default;
		}

		return new Rune ((char)baseKey);
	}

	private static string GetKeyString (Key key)
	{
		if (key is Key.Null or Key.SpecialMask) {
			return string.Empty;
		}
		// Extract the base key (removing modifier flags)
		Key baseKey = key & ~Key.CtrlMask & ~Key.AltMask & ~Key.ShiftMask;

		if (!key.HasFlag (Key.ShiftMask) && baseKey is >= Key.A and <= Key.Z) {
			return ((char)(key + 32)).ToString ();
		}

		if (key is >= Key.Space and < Key.A) {
			return ((char)key).ToString ();
		}

		string keyName = Enum.GetName (typeof (Key), key);
		return !string.IsNullOrEmpty (keyName) ? keyName : ((char)key).ToString ();
	}


	/// <summary>
	/// Formats a <see cref="Key"/> as a string using the default separator of '+'
	/// </summary>
	/// <param name="key">The key to format.</param>
	/// <returns>The formatted string. If the key is a printable character, it will be returned as a string. Otherwise, the key name will be returned.</returns>
	public static string ToString (Key key)
	{
		return ToString (key, (Rune)'+');
	}

	/// <summary>
	/// Formats a <see cref="Key"/> as a string.
	/// </summary>
	/// <param name="key">The key to format.</param>
	/// <param name="separator">The character to use as a separator between modifier keys and and the key itself.</param>
	/// <returns>The formatted string. If the key is a printable character, it will be returned as a string. Otherwise, the key name will be returned.</returns>
	public static string ToString (Key key, Rune separator)
	{
		if (key == Key.Null) {
			return string.Empty;
		}

		StringBuilder sb = new StringBuilder ();

		// Extract and handle modifiers
		bool hasModifiers = false;
		if ((key & Key.CtrlMask) != 0) {
			sb.Append ($"Ctrl{separator}");
			hasModifiers = true;
		}
		if ((key & Key.AltMask) != 0) {
			sb.Append ($"Alt{separator}");
			hasModifiers = true;
		}
		if ((key & Key.ShiftMask) != 0) {
			sb.Append ($"Shift{separator}");
			hasModifiers = true;
		}

		// Extract the base key (removing modifier flags)
		Key baseKey = key & ~Key.CtrlMask & ~Key.AltMask & ~Key.ShiftMask;

		// Handle special cases and modifiers on their own
		if ((key != Key.SpecialMask) && (baseKey != Key.Null || hasModifiers)) {
			if ((key & Key.SpecialMask) != 0 && baseKey >= Key.A && baseKey <= Key.Z) {
				sb.Append (baseKey);
			} else {
				// Append the actual key name
				sb.Append (GetKeyString (baseKey));
			}
		}

		var result = sb.ToString ();
		result = TrimEndRune (result, separator);
		return result;
	}

	static string TrimEndRune (string input, Rune runeToTrim)
	{
		// Convert the Rune to a string (which may be one or two chars)
		string runeString = runeToTrim.ToString ();

		if (input.EndsWith (runeString)) {
			// Remove the rune from the end of the string
			return input.Substring (0, input.Length - runeString.Length);
		}

		return input;
	}
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
	//a = 97,
	///// <summary>
	///// The key code for the user pressing B
	///// </summary>
	//b,
	///// <summary>
	///// The key code for the user pressing C
	///// </summary>
	//c,
	///// <summary>
	///// The key code for the user pressing D
	///// </summary>
	//d,
	///// <summary>
	///// The key code for the user pressing E
	///// </summary>
	//e,
	///// <summary>
	///// The key code for the user pressing F
	///// </summary>
	//f,
	///// <summary>
	///// The key code for the user pressing G
	///// </summary>
	//g,
	///// <summary>
	///// The key code for the user pressing H
	///// </summary>
	//h,
	///// <summary>
	///// The key code for the user pressing I
	///// </summary>
	//i,
	///// <summary>
	///// The key code for the user pressing J
	///// </summary>
	//j,
	///// <summary>
	///// The key code for the user pressing K
	///// </summary>
	//k,
	///// <summary>
	///// The key code for the user pressing L
	///// </summary>
	//l,
	///// <summary>
	///// The key code for the user pressing M
	///// </summary>
	//m,
	///// <summary>
	///// The key code for the user pressing N
	///// </summary>
	//n,
	///// <summary>
	///// The key code for the user pressing O
	///// </summary>
	//o,
	///// <summary>
	///// The key code for the user pressing P
	///// </summary>
	//p,
	///// <summary>
	///// The key code for the user pressing Q
	///// </summary>
	//q,
	///// <summary>
	///// The key code for the user pressing R
	///// </summary>
	//r,
	///// <summary>
	///// The key code for the user pressing S
	///// </summary>
	//s,
	///// <summary>
	///// The key code for the user pressing T
	///// </summary>
	//t,
	///// <summary>
	///// The key code for the user pressing U
	///// </summary>
	//u,
	///// <summary>
	///// The key code for the user pressing V
	///// </summary>
	//v,
	///// <summary>
	///// The key code for the user pressing W
	///// </summary>
	//w,
	///// <summary>
	///// The key code for the user pressing X
	///// </summary>
	//x,
	///// <summary>
	///// The key code for the user pressing Y
	///// </summary>
	//y,
	///// <summary>
	///// The key code for the user pressing Z
	///// </summary>
	//z,
	///// <summary>
	///// The key code for the user pressing the delete key.
	///// </summary>
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

