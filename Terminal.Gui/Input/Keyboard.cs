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
	public KeyEventArgs () : this (ConsoleDriverKey.Unknown) { }

	/// <summary>
	///   Constructs a new <see cref="KeyEventArgs"/> from the provided Key value - can be a rune cast into a Key value
	/// </summary>
	/// <param name="k">The key</param>
	public KeyEventArgs (ConsoleDriverKey k)
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
	public ConsoleDriverKey Key { get; set; }

	/// <summary>
	/// Enables passing the key binding scope with the event. Default is <see cref="KeyBindingScope.Focused"/>.
	/// </summary>
	public KeyBindingScope Scope { get; set; } = KeyBindingScope.Focused;

	/// <summary>
	/// The key value as a Rune. This is the actual value of the key pressed, and is independent of the modifiers.
	/// </summary>
	/// <remarks>
	/// If the key pressed is a letter (a-z or A-Z), this will be the upper or lower case letter depending on whether the shift key is pressed.
	/// If the key is outside of the <see cref="ConsoleDriverKey.CharMask"/> range, this will be <see langword="default"/>.
	/// </remarks>
	public Rune AsRune => ToRune (Key);

	/// <summary>
	/// Converts a <see cref="Key"/> to a <see cref="Rune"/>.
	/// </summary>
	/// <remarks>
	/// If the key is a letter (a-z or A-Z), this will be the upper or lower case letter depending on whether the shift key is pressed.
	/// If the key is outside of the <see cref="ConsoleDriverKey.CharMask"/> range, this will be <see langword="default"/>.
	/// </remarks>
	/// <param name="key"></param>
	/// <returns>The key converted to a rune. <see langword="default"/> if conversion is not possible.</returns>
	public static Rune ToRune (ConsoleDriverKey key)
	{
		if (key is ConsoleDriverKey.Null or ConsoleDriverKey.SpecialMask || key.HasFlag (ConsoleDriverKey.CtrlMask) || key.HasFlag (ConsoleDriverKey.AltMask)) {
			return default;
		}

		// Extract the base key (removing modifier flags)
		ConsoleDriverKey baseKey = key & ~ConsoleDriverKey.CtrlMask & ~ConsoleDriverKey.AltMask & ~ConsoleDriverKey.ShiftMask;

		switch (baseKey) {
		case >= ConsoleDriverKey.A and <= ConsoleDriverKey.Z when !key.HasFlag (ConsoleDriverKey.ShiftMask):
			return new Rune ((char)(baseKey + 32));
		case >= ConsoleDriverKey.A and <= ConsoleDriverKey.Z:
			return new Rune ((char)baseKey);
		case >= ConsoleDriverKey.Null and < ConsoleDriverKey.A:
			return new Rune ((char)baseKey);
		}

		if (Enum.IsDefined (typeof (ConsoleDriverKey), baseKey)) {
			return default;
		}

		return new Rune ((char)baseKey);
	}

	/// <summary>
	/// Gets a value indicating whether the Shift key was pressed.
	/// </summary>
	/// <value><see langword="true"/> if is shift; otherwise, <see langword="false"/>.</value>
	public bool IsShift => (Key & ConsoleDriverKey.ShiftMask) != 0;

	/// <summary>
	/// Gets a value indicating whether the Alt key was pressed (real or synthesized)
	/// </summary>
	/// <value><see langword="true"/> if is alternate; otherwise, <see langword="false"/>.</value>
	public bool IsAlt => (Key & ConsoleDriverKey.AltMask) != 0;

	/// <summary>
	/// Gets a value indicating whether the Ctrl key was pressed.
	/// </summary>
	/// <value><see langword="true"/> if is ctrl; otherwise, <see langword="false"/>.</value>
	public bool IsCtrl => (Key & ConsoleDriverKey.CtrlMask) != 0;

	/// <summary>
	/// Gets a value indicating whether the key is a letter (a-z or A-Z). This is independent of the shift key.
	/// </summary>	
	public bool IsAlpha {
		get {
			if (IsAlt || IsCtrl) {
				return false;
			}
			return (Key & ConsoleDriverKey.CharMask) is >= ConsoleDriverKey.A and <= ConsoleDriverKey.Z;
		}
	}

	/// <summary>
	/// Gets the key without any shift modifiers. 
	/// </summary>
	public ConsoleDriverKey BareKey => Key & ~ConsoleDriverKey.CtrlMask & ~ConsoleDriverKey.AltMask & ~ConsoleDriverKey.ShiftMask;

	#region Standard Key Definitions

	/// <summary>
	/// The key code for the Backspace key.
	/// </summary>
	public const uint Backspace = (uint)ConsoleDriverKey.Backspace;

	/// <summary>
	/// The key code for the user pressing the tab key (forwards tab key).
	/// </summary>
	public const uint Tab = (uint)ConsoleDriverKey.Tab;

	/// <summary>
	/// The key code for the user pressing the return key.
	/// </summary>
	public const uint Enter = (uint)ConsoleDriverKey.Enter;

	/// <summary>
	/// The key code for the user pressing the clear key.
	/// </summary>
	public const uint Clear = (uint)ConsoleDriverKey.Clear;

	/// <summary>
	/// The key code for the Shift key
	/// </summary>
	public const uint Shift = (uint)ConsoleDriverKey.Shift;

	/// <summary>
	/// The key code for the Ctrl key
	/// </summary>
	public const uint Ctrl = (uint)ConsoleDriverKey.Ctrl;

	/// <summary>
	/// The key code for the Alt key
	/// </summary>
	public const uint Alt = (uint)ConsoleDriverKey.Alt;

	/// <summary>
	/// The key code for the user pressing the escape key
	/// </summary>
	public const uint Esc = (uint)ConsoleDriverKey.Esc;

	/// <summary>
	/// The key code for the user pressing the space bar
	/// </summary>
	public const uint Space = (uint)ConsoleDriverKey.Space;

	/// <summary>
	/// The key code for the user pressing DEL
	/// </summary>
	public const uint Delete = (uint)ConsoleDriverKey.Delete;

	/// <summary>
	/// Cursor up key
	/// </summary>
	public const uint CursorUp = (uint)ConsoleDriverKey.CursorUp;
	/// <summary>
	/// Cursor down key.
	/// </summary>
	public const uint CursorDown = (uint)ConsoleDriverKey.CursorDown;
	/// <summary>
	/// Cursor left key.
	/// </summary>
	public const uint CursorLeft = (uint)ConsoleDriverKey.CursorLeft;
	/// <summary>
	/// Cursor right key.
	/// </summary>
	public const uint CursorRight = (uint)ConsoleDriverKey.CursorRight;
	/// <summary>
	/// Page Up key.
	/// </summary>
	public const uint PageUp = (uint)ConsoleDriverKey.PageUp;
	/// <summary>
	/// Page Down key.
	/// </summary>
	public const uint PageDown = (uint)ConsoleDriverKey.PageDown;
	/// <summary>
	/// Home key.
	/// </summary>
	public const uint Home = (uint)ConsoleDriverKey.Home;
	/// <summary>
	/// End key.
	/// </summary>
	public const uint End = (uint)ConsoleDriverKey.End;

	/// <summary>
	/// Insert character key.
	/// </summary>
	public const uint InsertChar = (uint)ConsoleDriverKey.InsertChar;

	/// <summary>
	/// Delete character key.
	/// </summary>
	public const uint DeleteChar = (uint)ConsoleDriverKey.DeleteChar;

	/// <summary>
	/// Print screen character key.
	/// </summary>
	public const uint PrintScreen = (uint)ConsoleDriverKey.PrintScreen;

	/// <summary>
	/// F1 key.
	/// </summary>
	public const uint F1 = (uint)ConsoleDriverKey.F1;
	/// <summary>
	/// F2 key.
	/// </summary>
	public const uint F2 = (uint)ConsoleDriverKey.F2;
	/// <summary>
	/// F3 key.
	/// </summary>
	public const uint F3 = (uint)ConsoleDriverKey.F3;
	/// <summary>
	/// F4 key.
	/// </summary>
	public const uint F4 = (uint)ConsoleDriverKey.F4;
	/// <summary>
	/// F5 key.
	/// </summary>
	public const uint F5 = (uint)ConsoleDriverKey.F5;
	/// <summary>
	/// F6 key.
	/// </summary>
	public const uint F6 = (uint)ConsoleDriverKey.F6;
	/// <summary>
	/// F7 key.
	/// </summary>
	public const uint F7 = (uint)ConsoleDriverKey.F7;
	/// <summary>
	/// F8 key.
	/// </summary>
	public const uint F8 = (uint)ConsoleDriverKey.F8;
	/// <summary>
	/// F9 key.
	/// </summary>
	public const uint F9 = (uint)ConsoleDriverKey.F9;
	/// <summary>
	/// F10 key.
	/// </summary>
	public const uint F10 = (uint)ConsoleDriverKey.F10;
	/// <summary>
	/// F11 key.
	/// </summary>
	public const uint F11 = (uint)ConsoleDriverKey.F11;
	/// <summary>
	/// F12 key.
	/// </summary>
	public const uint F12 = (uint)ConsoleDriverKey.F12;
	/// <summary>
	/// F13 key.
	/// </summary>
	public const uint F13 = (uint)ConsoleDriverKey.F13;
	/// <summary>
	/// F14 key.
	/// </summary>
	public const uint F14 = (uint)ConsoleDriverKey.F14;
	/// <summary>
	/// F15 key.
	/// </summary>
	public const uint F15 = (uint)ConsoleDriverKey.F15;
	/// <summary>
	/// F16 key.
	/// </summary>
	public const uint F16 = (uint)ConsoleDriverKey.F16;
	/// <summary>
	/// F17 key.
	/// </summary>
	public const uint F17 = (uint)ConsoleDriverKey.F17;
	/// <summary>
	/// F18 key.
	/// </summary>
	public const uint F18 = (uint)ConsoleDriverKey.F18;
	/// <summary>
	/// F19 key.
	/// </summary>
	public const uint F19 = (uint)ConsoleDriverKey.F19;
	/// <summary>
	/// F20 key.
	/// </summary>
	public const uint F20 = (uint)ConsoleDriverKey.F20;
	/// <summary>
	/// F21 key.
	/// </summary>
	public const uint F21 = (uint)ConsoleDriverKey.F21;
	/// <summary>
	/// F22 key.
	/// </summary>
	public const uint F22 = (uint)ConsoleDriverKey.F22;
	/// <summary>
	/// F23 key.
	/// </summary>
	public const uint F23 = (uint)ConsoleDriverKey.F23;
	/// <summary>
	/// F24 key.
	/// </summary>
	public const uint F24 = (uint)ConsoleDriverKey.F24;

	#endregion

	#region Operators
	/// <summary>
	/// Cast to a Rune. This is the actual value of the key pressed, and is independent of the modifiers.
	/// </summary>
	/// <remarks>
	/// Uses <see cref="AsRune"/>.
	/// </remarks>
	/// <param name="kea"></param>
	public static implicit operator Rune (KeyEventArgs kea) => kea.AsRune;

	/// <summary>
	/// Cast to a char.
	/// </summary>
	/// <param name="kea"></param>
	public static explicit operator char (KeyEventArgs kea) => (char)kea.AsRune.Value;
	#endregion Operators

	#region String conversion
	/// <summary>
	/// Pretty prints the KeyEvent
	/// </summary>
	/// <returns></returns>
	public override string ToString ()
	{
		return ToString (Key, (Rune)'+');
	}

	private static string GetKeyString (ConsoleDriverKey key)
	{
		if (key is ConsoleDriverKey.Null or ConsoleDriverKey.SpecialMask) {
			return string.Empty;
		}
		// Extract the base key (removing modifier flags)
		ConsoleDriverKey baseKey = key & ~ConsoleDriverKey.CtrlMask & ~ConsoleDriverKey.AltMask & ~ConsoleDriverKey.ShiftMask;

		if (!key.HasFlag (ConsoleDriverKey.ShiftMask) && baseKey is >= ConsoleDriverKey.A and <= ConsoleDriverKey.Z) {
			return ((char)(key + 32)).ToString ();
		}

		if (key is >= ConsoleDriverKey.Space and < ConsoleDriverKey.A) {
			return ((char)key).ToString ();
		}

		string keyName = Enum.GetName (typeof (ConsoleDriverKey), key);
		return !string.IsNullOrEmpty (keyName) ? keyName : ((char)key).ToString ();
	}


	/// <summary>
	/// Formats a <see cref="Key"/> as a string using the default separator of '+'
	/// </summary>
	/// <param name="key">The key to format.</param>
	/// <returns>The formatted string. If the key is a printable character, it will be returned as a string. Otherwise, the key name will be returned.</returns>
	public static string ToString (ConsoleDriverKey key)
	{
		return ToString (key, (Rune)'+');
	}

	/// <summary>
	/// Formats a <see cref="Key"/> as a string.
	/// </summary>
	/// <param name="key">The key to format.</param>
	/// <param name="separator">The character to use as a separator between modifier keys and and the key itself.</param>
	/// <returns>The formatted string. If the key is a printable character, it will be returned as a string. Otherwise, the key name will be returned.</returns>
	public static string ToString (ConsoleDriverKey key, Rune separator)
	{
		if (key == ConsoleDriverKey.Null) {
			return string.Empty;
		}

		StringBuilder sb = new StringBuilder ();

		// Extract and handle modifiers
		bool hasModifiers = false;
		if ((key & ConsoleDriverKey.CtrlMask) != 0) {
			sb.Append ($"Ctrl{separator}");
			hasModifiers = true;
		}
		if ((key & ConsoleDriverKey.AltMask) != 0) {
			sb.Append ($"Alt{separator}");
			hasModifiers = true;
		}
		if ((key & ConsoleDriverKey.ShiftMask) != 0) {
			sb.Append ($"Shift{separator}");
			hasModifiers = true;
		}

		// Extract the base key (removing modifier flags)
		ConsoleDriverKey baseKey = key & ~ConsoleDriverKey.CtrlMask & ~ConsoleDriverKey.AltMask & ~ConsoleDriverKey.ShiftMask;

		// Handle special cases and modifiers on their own
		if ((key != ConsoleDriverKey.SpecialMask) && (baseKey != ConsoleDriverKey.Null || hasModifiers)) {
			if ((key & ConsoleDriverKey.SpecialMask) != 0 && baseKey >= ConsoleDriverKey.A && baseKey <= ConsoleDriverKey.Z) {
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
	#endregion
}
