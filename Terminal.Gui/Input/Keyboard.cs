using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Terminal.Gui;

/// <summary>
/// Provides an abstraction for common keyboard operations and state. Used for processing keyboard input and raising keyboard events.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a high-level abstraction with helper methods and properties for common keyboard operations. Use this class
/// instead of the <see cref="Terminal.Gui.KeyCode"/> enumeration for keyboard input whenever possible.
/// </para>
/// <para>
/// IMPORTANT: Lowercase alpha keys are encoded (in <see cref="Key.KeyCode"/>) as values between 65 and 90 corresponding to
/// the un-shifted A to Z keys on a keyboard. Enum values are provided for these (e.g. <see cref="KeyCode.A"/>, <see cref="KeyCode.B"/>, etc.).
/// Even though the values are the same as the ASCII values for uppercase characters, these enum values represent *lowercase*, un-shifted characters.
/// </para>
/// <para>
/// The default value for <see cref="Key.KeyCode"/> is <see cref="KeyCode.Null"/>. This is used to indicate that the key has not been
/// set. 
/// </para>
/// </remarks>
public class Key : EventArgs, IEquatable<Key> {

	/// <summary>
	/// Constructs a new <see cref="Key"/>
	/// </summary>
	public Key () : this (KeyCode.Null) { }

	/// <summary>
	///   Constructs a new <see cref="Key"/> from the provided Key value
	/// </summary>
	/// <param name="k">The key</param>
	public Key (KeyCode k)
	{
		KeyCode = k;
	}

	public Key (KeyCode k, params string [] modifiers)
	{
		KeyCode = k;
		foreach (var mod in modifiers) {
			if (_modifierDict.TryGetValue (mod.ToLowerInvariant (), out var m)) {
				KeyCode |= m;
			} else {
				throw new ArgumentException ($"Invalid modifier: {mod}");
			}
		}
	}

	/// <summary>
	/// Indicates if the current Key event has already been processed and the driver should stop notifying any other event subscriber.
	/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
	/// </summary>
	public bool Handled { get; set; } = false;

	/// <summary>
	/// The encoded key value.
	/// </summary>
	/// <para>
	/// IMPORTANT: Lowercase alpha keys are encoded (in <see cref="Gui.KeyCode"/>) as values between 65 and 90 corresponding to the un-shifted A to Z keys on a keyboard. Enum values
	/// are provided for these (e.g. <see cref="KeyCode.A"/>, <see cref="KeyCode.B"/>, etc.). Even though the values are the same as the ASCII
	/// values for uppercase characters, these enum values represent *lowercase*, un-shifted characters.
	/// </para>
	[JsonInclude]
	[JsonConverter (typeof (KeyCodeJsonConverter))]
	public KeyCode KeyCode { get; set; }

	/// <summary>
	/// Enables passing the key binding scope with the event. Default is <see cref="KeyBindingScope.Focused"/>.
	/// </summary>
	public KeyBindingScope Scope { get; set; } = KeyBindingScope.Focused;

	/// <summary>
	/// The key value as a Rune. This is the actual value of the key pressed, and is independent of the modifiers.
	/// </summary>
	/// <remarks>
	/// If the key pressed is a letter (a-z or A-Z), this will be the upper or lower case letter depending on whether the shift key is pressed.
	/// If the key is outside of the <see cref="KeyCode.CharMask"/> range, this will be <see langword="default"/>.
	/// </remarks>
	public Rune AsRune => ToRune (KeyCode);

	/// <summary>
	/// Converts a <see cref="KeyCode"/> to a <see cref="Rune"/>.
	/// </summary>
	/// <remarks>
	/// If the key is a letter (a-z or A-Z), this will be the upper or lower case letter depending on whether the shift key is pressed.
	/// If the key is outside of the <see cref="KeyCode.CharMask"/> range, this will be <see langword="default"/>.
	/// </remarks>
	/// <param name="key"></param>
	/// <returns>The key converted to a rune. <see langword="default"/> if conversion is not possible.</returns>
	public static Rune ToRune (KeyCode key)
	{
		if (key is KeyCode.Null or KeyCode.Unknown or KeyCode.SpecialMask || key.HasFlag (KeyCode.CtrlMask) || key.HasFlag (KeyCode.AltMask)) {
			return default;
		}

		// Extract the base key (removing modifier flags)
		KeyCode baseKey = key & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask;

		switch (baseKey) {
		case >= KeyCode.A and <= KeyCode.Z when !key.HasFlag (KeyCode.ShiftMask):
			return new Rune ((char)(baseKey + 32));
		case >= KeyCode.A and <= KeyCode.Z:
			return new Rune ((char)baseKey);
		case > KeyCode.Null and < KeyCode.A:
			return new Rune ((char)baseKey);
		}

		if (Enum.IsDefined (typeof (KeyCode), baseKey)) {
			return default;
		}

		return new Rune ((char)baseKey);
	}

	/// <summary>
	/// Gets a value indicating whether the Shift key was pressed.
	/// </summary>
	/// <value><see langword="true"/> if is shift; otherwise, <see langword="false"/>.</value>
	public bool IsShift => (KeyCode & KeyCode.ShiftMask) != 0;

	/// <summary>
	/// Gets a value indicating whether the Alt key was pressed (real or synthesized)
	/// </summary>
	/// <value><see langword="true"/> if is alternate; otherwise, <see langword="false"/>.</value>
	public bool IsAlt => (KeyCode & KeyCode.AltMask) != 0;

	/// <summary>
	/// Gets a value indicating whether the Ctrl key was pressed.
	/// </summary>
	/// <value><see langword="true"/> if is ctrl; otherwise, <see langword="false"/>.</value>
	public bool IsCtrl => (KeyCode & KeyCode.CtrlMask) != 0;

	/// <summary>
	/// Gets a value indicating whether the KeyCode is a lower case letter from 'a' to 'z', independent of the shift key.
	/// </summary>
	/// <remarks>
	/// IMPORTANT: Lowercase alpha keys are encoded in <see cref="Key.KeyCode"/> as values between 65 and 90 corresponding to
	/// the un-shifted A to Z keys on a keyboard. Enum values are provided for these (e.g. <see cref="KeyCode.A"/>, <see cref="KeyCode.B"/>, etc.).
	/// Even though the values are the same as the ASCII values for uppercase characters, these enum values represent *lowercase*, un-shifted characters.
	/// </remarks>
	public bool IsLowerCaseAtoZ => Key.TestLowerCaseAtoZ (KeyCode);

	/// <summary>
	/// Tests if a KeyCode is composed of a lower case letter from 'a' to 'z', independent of the shift key.
	/// </summary>
	/// <remarks>
	/// IMPORTANT: Lowercase alpha keys are encoded in <see cref="Key.KeyCode"/> as values between 65 and 90 corresponding to
	/// the un-shifted A to Z keys on a keyboard. Enum values are provided for these (e.g. <see cref="KeyCode.A"/>, <see cref="KeyCode.B"/>, etc.).
	/// Even though the values are the same as the ASCII values for uppercase characters, these enum values represent *lowercase*, un-shifted characters.
	/// </remarks>
	public static bool TestLowerCaseAtoZ (KeyCode keyCode)
	{
		if (((keyCode & KeyCode.AltMask) != 0) || (keyCode & KeyCode.CtrlMask) != 0) {
			return false;
		}

		if ((keyCode & ~KeyCode.Space & ~KeyCode.ShiftMask) is >= KeyCode.A and <= KeyCode.Z) {
			return true;
		}

		return (keyCode & KeyCode.CharMask) is >= KeyCode.A and <= KeyCode.Z;
	}

	/// <summary>
	/// Gets the key without any shift modifiers. 
	/// </summary>
	public KeyCode BareKey => KeyCode & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask;

	public bool IsValid => !(KeyCode == KeyCode.Null || KeyCode == KeyCode.Unknown);

	#region Standard Key Definitions


	/// <summary>
	/// The Shift modifier flag. Combine with a key code property to indicate the shift modifier. E.g. <c>key.Enter | key.Shift</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="IsShift"/> provides a helper to determine if the <see cref="Key"/> has the Shift modifier applied.
	/// </remarks>
	public const uint Shift = (uint)KeyCode.ShiftMask;

	/// <summary>
	/// The Ctrl modifier flag. Combine with a key code property to indicate the Ctrl modifier. E.g. <c>key.Enter | key.Ctrl</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="IsCtrl"/> provides a helper to determine if the <see cref="Key"/> has the Ctrl modifier applied.
	/// </remarks>
	public const uint Ctrl = (uint)KeyCode.CtrlMask;

	/// <summary>
	/// The Alt modifier flag. Combine with a key code property to indicate the Ctrl modifier. E.g. <c>key.Enter | key.Ctrl</c>.
	/// </summary>
	/// <remarks>
	/// <see cref="IsAlt"/> provides a helper to determine if the <see cref="Key"/> has the Alt modifier applied.
	/// </remarks>
	public const uint Alt = (uint)KeyCode.AltMask;

	/// <summary>
	/// The key code for the Backspace key.
	/// </summary>
	public const uint Backspace = (uint)KeyCode.Backspace;

	/// <summary>
	/// The key code for the user pressing the tab key (forwards tab key).
	/// </summary>
	public const uint Tab = (uint)KeyCode.Tab;

	/// <summary>
	/// The key code for the user pressing the return key.
	/// </summary>
	public const uint Enter = (uint)KeyCode.Enter;

	/// <summary>
	/// The key code for the user pressing the clear key.
	/// </summary>
	public const uint Clear = (uint)KeyCode.Clear;

	/// <summary>
	/// The key code for the Shift key
	/// </summary>
	public const uint ShiftKey = (uint)KeyCode.ShiftKey;

	/// <summary>
	/// The key code for the Ctrl key
	/// </summary>
	public const uint CtrlKey = (uint)KeyCode.CtrlKey;

	/// <summary>
	/// The key code for the Alt key
	/// </summary>
	public const uint AltKey = (uint)KeyCode.AltKey;

	/// <summary>
	/// The key code for the CapsLock key
	/// </summary>
	public const uint CapsLock = (uint)KeyCode.CapsLock;

	/// <summary>
	/// The key code for the user pressing the escape key
	/// </summary>
	public const uint Esc = (uint)KeyCode.Esc;

	/// <summary>
	/// The key code for the user pressing the space bar
	/// </summary>
	public const uint Space = (uint)KeyCode.Space;

	/// <summary>
	/// The key code for the user pressing DEL
	/// </summary>
	public const uint Delete = (uint)KeyCode.Delete;

	/// <summary>
	/// Cursor up key
	/// </summary>
	public const uint CursorUp = (uint)KeyCode.CursorUp;
	/// <summary>
	/// Cursor down key.
	/// </summary>
	public const uint CursorDown = (uint)KeyCode.CursorDown;
	/// <summary>
	/// Cursor left key.
	/// </summary>
	public const uint CursorLeft = (uint)KeyCode.CursorLeft;
	/// <summary>
	/// Cursor right key.
	/// </summary>
	public const uint CursorRight = (uint)KeyCode.CursorRight;
	/// <summary>
	/// Page Up key.
	/// </summary>
	public const uint PageUp = (uint)KeyCode.PageUp;
	/// <summary>
	/// Page Down key.
	/// </summary>
	public const uint PageDown = (uint)KeyCode.PageDown;
	/// <summary>
	/// Home key.
	/// </summary>
	public const uint Home = (uint)KeyCode.Home;
	/// <summary>
	/// End key.
	/// </summary>
	public const uint End = (uint)KeyCode.End;

	/// <summary>
	/// Insert character key.
	/// </summary>
	public const uint InsertChar = (uint)KeyCode.InsertChar;

	/// <summary>
	/// Delete character key.
	/// </summary>
	public const uint DeleteChar = (uint)KeyCode.DeleteChar;

	/// <summary>
	/// Print screen character key.
	/// </summary>
	public const uint PrintScreen = (uint)KeyCode.PrintScreen;

	/// <summary>
	/// F1 key.
	/// </summary>
	public const uint F1 = (uint)KeyCode.F1;
	/// <summary>
	/// F2 key.
	/// </summary>
	public const uint F2 = (uint)KeyCode.F2;
	/// <summary>
	/// F3 key.
	/// </summary>
	public const uint F3 = (uint)KeyCode.F3;
	/// <summary>
	/// F4 key.
	/// </summary>
	public const uint F4 = (uint)KeyCode.F4;
	/// <summary>
	/// F5 key.
	/// </summary>
	public const uint F5 = (uint)KeyCode.F5;
	/// <summary>
	/// F6 key.
	/// </summary>
	public const uint F6 = (uint)KeyCode.F6;
	/// <summary>
	/// F7 key.
	/// </summary>
	public const uint F7 = (uint)KeyCode.F7;
	/// <summary>
	/// F8 key.
	/// </summary>
	public const uint F8 = (uint)KeyCode.F8;
	/// <summary>
	/// F9 key.
	/// </summary>
	public const uint F9 = (uint)KeyCode.F9;
	/// <summary>
	/// F10 key.
	/// </summary>
	public const uint F10 = (uint)KeyCode.F10;
	/// <summary>
	/// F11 key.
	/// </summary>
	public const uint F11 = (uint)KeyCode.F11;
	/// <summary>
	/// F12 key.
	/// </summary>
	public const uint F12 = (uint)KeyCode.F12;
	/// <summary>
	/// F13 key.
	/// </summary>
	public const uint F13 = (uint)KeyCode.F13;
	/// <summary>
	/// F14 key.
	/// </summary>
	public const uint F14 = (uint)KeyCode.F14;
	/// <summary>
	/// F15 key.
	/// </summary>
	public const uint F15 = (uint)KeyCode.F15;
	/// <summary>
	/// F16 key.
	/// </summary>
	public const uint F16 = (uint)KeyCode.F16;
	/// <summary>
	/// F17 key.
	/// </summary>
	public const uint F17 = (uint)KeyCode.F17;
	/// <summary>
	/// F18 key.
	/// </summary>
	public const uint F18 = (uint)KeyCode.F18;
	/// <summary>
	/// F19 key.
	/// </summary>
	public const uint F19 = (uint)KeyCode.F19;
	/// <summary>
	/// F20 key.
	/// </summary>
	public const uint F20 = (uint)KeyCode.F20;
	/// <summary>
	/// F21 key.
	/// </summary>
	public const uint F21 = (uint)KeyCode.F21;
	/// <summary>
	/// F22 key.
	/// </summary>
	public const uint F22 = (uint)KeyCode.F22;
	/// <summary>
	/// F23 key.
	/// </summary>
	public const uint F23 = (uint)KeyCode.F23;
	/// <summary>
	/// F24 key.
	/// </summary>
	public const uint F24 = (uint)KeyCode.F24;

	#endregion

	#region Operators
	/// <summary>
	/// Explicitly cast a <see cref="Key"/> to a <see cref="Rune"/>. The conversion is lossy. 
	/// </summary>
	/// <remarks>
	/// Uses <see cref="AsRune"/>.
	/// </remarks>
	/// <param name="kea"></param>
	public static explicit operator Rune (Key kea) => kea.AsRune;

	/// <summary>
	/// Explicitly cast <see cref="Key"/> to a <see langword="char"/>. The conversion is lossy. 
	/// </summary>
	/// <param name="kea"></param>
	public static explicit operator char (Key kea) => (char)kea.AsRune.Value;

	/// <summary>
	/// Explicitly cast <see cref="Key"/> to a <see cref="KeyCode"/>. The conversion is lossy. 
	/// </summary>
	/// <param name="key"></param>
	public static explicit operator KeyCode (Key key) => key.KeyCode;

	/// <summary>
	/// Cast <see cref="KeyCode"/> to a <see cref="Key"/>. 
	/// </summary>
	/// <param name="keyCode"></param>
	public static implicit operator Key (KeyCode keyCode) => new Key (keyCode);
	
	/// <inheritdoc/>
	public override bool Equals (object obj) => obj is Key k && k.KeyCode == KeyCode;

	/// <inheritdoc/>
	public override int GetHashCode () => (int)KeyCode;

	/// <summary>
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static bool operator == (Key a, Key b) => a?.KeyCode == b?.KeyCode;

	/// <summary>
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static bool operator != (Key a, Key b) => a?.KeyCode != b?.KeyCode;

	bool IEquatable<Key>.Equals (Key other) => Equals ((object)other);
	#endregion Operators

	#region String conversion
	/// <summary>
	/// Pretty prints the KeyEvent
	/// </summary>
	/// <returns></returns>
	public override string ToString ()
	{
		return ToString (KeyCode, (Rune)'+');
	}

	private static string GetKeyString (KeyCode key)
	{
		if (key is KeyCode.Null or KeyCode.Unknown or KeyCode.SpecialMask) {
			return string.Empty;
		}
		// Extract the base key (removing modifier flags)
		KeyCode baseKey = key & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask;

		if (!key.HasFlag (KeyCode.ShiftMask) && baseKey is >= KeyCode.A and <= KeyCode.Z) {
			return ((char)(key + 32)).ToString ();
		}

		if (key is >= KeyCode.Space and < KeyCode.A) {
			return ((char)key).ToString ();
		}

		string keyName = Enum.GetName (typeof (KeyCode), key);
		return !string.IsNullOrEmpty (keyName) ? keyName : ((char)key).ToString ();
	}


	/// <summary>
	/// Formats a <see cref="KeyCode"/> as a string using the default separator of '+'
	/// </summary>
	/// <param name="key">The key to format.</param>
	/// <returns>The formatted string. If the key is a printable character, it will be returned as a string. Otherwise, the key name will be returned.</returns>
	public static string ToString (KeyCode key)
	{
		return ToString (key, (Rune)'+');
	}

	/// <summary>
	/// Formats a <see cref="KeyCode"/> as a string.
	/// </summary>
	/// <param name="key">The key to format.</param>
	/// <param name="separator">The character to use as a separator between modifier keys and and the key itself.</param>
	/// <returns>The formatted string. If the key is a printable character, it will be returned as a string. Otherwise, the key name will be returned.</returns>
	public static string ToString (KeyCode key, Rune separator)
	{
		if (key is KeyCode.Null or KeyCode.Unknown) {
			return string.Empty;
		}

		StringBuilder sb = new StringBuilder ();

		// Extract the base key (removing modifier flags)
		KeyCode baseKey = key & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask;

		// Extract and handle modifiers
		bool hasModifiers = false;
		if ((key & KeyCode.CtrlMask) != 0) {
			sb.Append ($"Ctrl{separator}");
			hasModifiers = true;
		}
		if ((key & KeyCode.AltMask) != 0) {
			sb.Append ($"Alt{separator}");
			hasModifiers = true;
		}
		if ((key & KeyCode.ShiftMask) != 0 && !Key.TestLowerCaseAtoZ (key)) {
			sb.Append ($"Shift{separator}");
			hasModifiers = true;
		}

		// Handle special cases and modifiers on their own
		if ((key != KeyCode.SpecialMask) && (baseKey != KeyCode.Null || hasModifiers)) {
			if ((key & KeyCode.SpecialMask) != 0 && (baseKey & ~KeyCode.Space) is >= KeyCode.A and <= KeyCode.Z) {
				sb.Append (baseKey & ~KeyCode.Space);
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

	private static readonly Dictionary<string, KeyCode> _modifierDict = new Dictionary<string, KeyCode> (comparer: StringComparer.InvariantCultureIgnoreCase) {
			{ "Shift", KeyCode.ShiftMask },
			{ "Ctrl", KeyCode.CtrlMask },
			{ "Alt", KeyCode.AltMask }
		};

	/// <summary>
	/// Converts the provided string to a new <see cref="Key"/> instance.
	/// </summary>
	/// <param name="text">The text to analyze. Formats supported are
	/// "Ctrl+X", "Alt+X", "Shift+X", "Ctrl+Alt+X", "Ctrl+Shift+X", "Alt+Shift+X", "Ctrl+Alt+Shift+X", and "X".
	/// </param>
	/// <param name="key">The parsed value.</param>
	/// <returns>A boolean value indicating whether parsing was successful.</returns>
	/// <remarks>
	/// </remarks>
	public static bool TryParse (string text, [NotNullWhen (true)] out Key key)
	{
		if (string.IsNullOrEmpty (text)) {
			key = new Key (KeyCode.Null);
			return true;
		}

		key = null;

		// Split the string into parts
		string [] parts = text.Split ('+', '-');

		if (parts.Length is 0 or > 4 || parts.Any (string.IsNullOrEmpty)) {
			return false;
		}

		// if it's just a shift key
		if (parts.Length == 1) {
			switch (parts [0]) {
			case "Ctrl":
				key = new Key (KeyCode.CtrlKey);
				return true;
			case "Alt":
				key = new Key (KeyCode.AltKey);
				return true;
			case "Shift":
				key = new Key (KeyCode.ShiftKey);
				return true;
			}
		}

		KeyCode modifiers = KeyCode.Null;
		for (int index = 0; index < parts.Length; index++) {
			if (_modifierDict.TryGetValue (parts [index].ToLowerInvariant (), out KeyCode modifier)) {
				modifiers |= modifier;
				parts [index] = string.Empty; // eat it
			}
		}

		// we now have the modifiers

		var partNotFound = parts.FirstOrDefault (p => !string.IsNullOrEmpty (p), string.Empty);
		var parsedKeyCode = KeyCode.Null;
		var parsedInt = 0;
		if (partNotFound.Length == 1) {
			var keyCode = (KeyCode)partNotFound [0];
			// if it's a single digit int, treat it as such
			if (int.TryParse (partNotFound,
				System.Globalization.NumberStyles.Integer,
				System.Globalization.CultureInfo.InvariantCulture,
				out parsedInt)) {
				keyCode = (KeyCode)((int)KeyCode.D0 + parsedInt);
			} else if (Enum.TryParse (partNotFound, false, out parsedKeyCode)) {
				if ((KeyCode)parsedKeyCode != KeyCode.Null) {
					if (parsedKeyCode is >= KeyCode.A and <= KeyCode.Z && modifiers == 0) {
						key = new Key (((KeyCode)parsedKeyCode) | KeyCode.ShiftMask);
						return true;
					}
					key = new Key ((KeyCode)parsedKeyCode | modifiers);
					return true;
				}
			}
			key = new Key (keyCode | modifiers);
			return true;
		}

		if (Enum.TryParse (partNotFound, true, out parsedKeyCode)) {
			if ((KeyCode)parsedKeyCode != KeyCode.Null) {
				if ((KeyCode)parsedKeyCode is >= KeyCode.A and <= KeyCode.Z && modifiers == 0) {
					key = new Key (((KeyCode)parsedKeyCode) | KeyCode.ShiftMask);
					return true;
				}
				key = new Key ((KeyCode)parsedKeyCode | modifiers);
				return true;
			}
		}

		// if it's a number int, treat it as a unicode value
		if (int.TryParse (partNotFound,
			System.Globalization.NumberStyles.Number,
			System.Globalization.CultureInfo.InvariantCulture, out parsedInt)) {
			if (!Rune.IsValid (parsedInt)) {
				return false;
			}
			if ((KeyCode)parsedInt is >= KeyCode.A and <= KeyCode.Z && modifiers == 0) {
				key = new Key (((KeyCode)parsedInt) | KeyCode.ShiftMask);
				return true;
			}
			key = new Key (((KeyCode)parsedInt));
			return true;
		}

		if (Enum.TryParse (partNotFound, true, out parsedKeyCode)) {
			if (Key.TestLowerCaseAtoZ ((KeyCode)parsedKeyCode)) {
				key = new Key (((KeyCode)parsedKeyCode) | modifiers & ~KeyCode.Space);
				return true;
			}
		}

		return false;
	}
	#endregion
}
