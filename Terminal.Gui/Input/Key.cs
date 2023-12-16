using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

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
[JsonConverter (typeof (KeyJsonConverter))]
public class Key : EventArgs, IEquatable<Key> {
	/// <summary>
	/// Constructs a new <see cref="Key"/>
	/// </summary>
	public Key () : this (KeyCode.Null) { }

	/// <summary>
	///   Constructs a new <see cref="Key"/> from the provided Key value
	/// </summary>
	/// <param name="k">The key</param>
	public Key (KeyCode k) => KeyCode = k;

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
	[JsonInclude] [JsonConverter (typeof (KeyCodeJsonConverter))]
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
		if (key is KeyCode.Null or KeyCode.SpecialMask || key.HasFlag (KeyCode.CtrlMask) || key.HasFlag (KeyCode.AltMask)) {
			return default;
		}

		// Extract the base key (removing modifier flags)
		var baseKey = key & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask;

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
	/// Gets a value indicating whether the KeyCode is composed of a lower case letter from 'a' to 'z', independent of the shift key.
	/// </summary>
	/// <remarks>
	/// IMPORTANT: Lowercase alpha keys are encoded in <see cref="Key.KeyCode"/> as values between 65 and 90 corresponding to
	/// the un-shifted A to Z keys on a keyboard. Enum values are provided for these (e.g. <see cref="KeyCode.A"/>, <see cref="KeyCode.B"/>, etc.).
	/// Even though the values are the same as the ASCII values for uppercase characters, these enum values represent *lowercase*, un-shifted characters.
	/// </remarks>
	public bool IsKeyCodeAtoZ => GetIsKeyCodeAtoZ (KeyCode);

	/// <summary>
	/// Tests if a KeyCode is composed of a lower case letter from 'a' to 'z', independent of the shift key.
	/// </summary>
	/// <remarks>
	/// IMPORTANT: Lowercase alpha keys are encoded in <see cref="Key.KeyCode"/> as values between 65 and 90 corresponding to
	/// the un-shifted A to Z keys on a keyboard. Enum values are provided for these (e.g. <see cref="KeyCode.A"/>, <see cref="KeyCode.B"/>, etc.).
	/// Even though the values are the same as the ASCII values for uppercase characters, these enum values represent *lowercase*, un-shifted characters.
	/// </remarks>
	public static bool GetIsKeyCodeAtoZ (KeyCode keyCode)
	{
		if ((keyCode & KeyCode.AltMask) != 0 || (keyCode & KeyCode.CtrlMask) != 0) {
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

	/// <summary>
	/// Indicates whether the <see cref="Key"/> is valid or not. 
	/// </summary>
	public bool IsValid => !(KeyCode == KeyCode.Null || KeyCode == KeyCode.Unknown);

	/// <summary>
	/// Helper for specifying a shifted <see cref="Key"/>.
	/// <code>
	/// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
	/// </code>
	/// </summary>
	public Key WithShift => new (KeyCode | KeyCode.ShiftMask);

	/// <summary>
	/// Helper for removing a shift modifier from a <see cref="Key"/>.
	/// <code>
	/// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
	/// var AltDelete = ControlAltDelete.NoCtrl;
	/// </code>
	/// </summary>
	public Key NoShift => new (KeyCode & ~KeyCode.ShiftMask);

	/// <summary>
	/// Helper for specifying a shifted <see cref="Key"/>.
	/// <code>
	/// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
	/// </code>
	/// </summary>
	public Key WithCtrl => new (KeyCode | KeyCode.CtrlMask);

	/// <summary>
	/// Helper for removing a shift modifier from a <see cref="Key"/>.
	/// <code>
	/// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
	/// var AltDelete = ControlAltDelete.NoCtrl;
	/// </code>
	/// </summary>
	public Key NoCtrl => new (KeyCode & ~KeyCode.CtrlMask);

	/// <summary>
	/// Helper for specifying a shifted <see cref="Key"/>.
	/// <code>
	/// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
	/// </code>
	/// </summary>
	public Key WithAlt => new (KeyCode | KeyCode.AltMask);

	/// <summary>
	/// Helper for removing a shift modifier from a <see cref="Key"/>.
	/// <code>
	/// var ControlAltDelete = new Key(Key.Delete).WithAlt.WithDel;
	/// var AltDelete = ControlAltDelete.NoCtrl;
	/// </code>
	/// </summary>
	public Key NoAlt => new (KeyCode & ~KeyCode.AltMask);

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
	public static implicit operator Key (KeyCode keyCode) => new (keyCode);


	/// <summary>
	/// Cast <see langword="char"/> to a <see cref="Key"/>. 
	/// </summary>
	/// <param name="ch"></param>
	public static implicit operator Key (char ch) => new ((KeyCode)ch);

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
	public override string ToString () => ToString (KeyCode, (Rune)'+');

	static string GetKeyString (KeyCode key)
	{
		if (key is KeyCode.Null or KeyCode.SpecialMask) {
			return string.Empty;
		}
		// Extract the base key (removing modifier flags)
		var baseKey = key & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask;

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
	public static string ToString (KeyCode key) => ToString (key, (Rune)'+');

	/// <summary>
	/// Formats a <see cref="KeyCode"/> as a string.
	/// </summary>
	/// <param name="key">The key to format.</param>
	/// <param name="separator">The character to use as a separator between modifier keys and and the key itself.</param>
	/// <returns>The formatted string. If the key is a printable character, it will be returned as a string. Otherwise, the key name will be returned.</returns>
	public static string ToString (KeyCode key, Rune separator)
	{
		if (key is KeyCode.Null) {
			return string.Empty;
		}

		var sb = new StringBuilder ();

		// Extract the base key (removing modifier flags)
		var baseKey = key & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask;

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
		if ((key & KeyCode.ShiftMask) != 0 && !GetIsKeyCodeAtoZ (key)) {
			sb.Append ($"Shift{separator}");
			hasModifiers = true;
		}

		// Handle special cases and modifiers on their own
		if (key != KeyCode.SpecialMask && (baseKey != KeyCode.Null || hasModifiers)) {
			if ((key & KeyCode.SpecialMask) != 0 && (baseKey & ~KeyCode.Space) is >= KeyCode.A and <= KeyCode.Z) {
				sb.Append (baseKey & ~KeyCode.Space);
			} else {
				// Append the actual key name
				sb.Append (GetKeyString (baseKey));
			}
		}

		string result = sb.ToString ();
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

	static readonly Dictionary<string, KeyCode> _modifierDict = new (comparer: StringComparer.InvariantCultureIgnoreCase) {
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

		var modifiers = KeyCode.Null;
		for (int index = 0; index < parts.Length; index++) {
			if (_modifierDict.TryGetValue (parts [index].ToLowerInvariant (), out var modifier)) {
				modifiers |= modifier;
				parts [index] = string.Empty; // eat it
			}
		}

		// we now have the modifiers

		string partNotFound = parts.FirstOrDefault (p => !string.IsNullOrEmpty (p), string.Empty);
		var parsedKeyCode = KeyCode.Null;
		int parsedInt = 0;
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
						key = new Key ((KeyCode)parsedKeyCode | KeyCode.ShiftMask);
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
					key = new Key ((KeyCode)parsedKeyCode | KeyCode.ShiftMask);
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
				key = new Key ((KeyCode)parsedInt | KeyCode.ShiftMask);
				return true;
			}
			key = new Key ((KeyCode)parsedInt);
			return true;
		}

		if (Enum.TryParse (partNotFound, true, out parsedKeyCode)) {
			if (GetIsKeyCodeAtoZ ((KeyCode)parsedKeyCode)) {
				key = new Key ((KeyCode)parsedKeyCode | modifiers & ~KeyCode.Space);
				return true;
			}
		}

		return false;
	}
	#endregion


	#region Standard Key Definitions
	/// <summary>
	/// An uninitialized The <see cref="Key"/> object.
	/// </summary>
	public static readonly Key Empty = new ();

	/// <summary>
	/// The <see cref="Key"/> object for the Backspace key.
	/// </summary>
	public static readonly Key Backspace = new (KeyCode.Backspace);

	/// <summary>
	/// The <see cref="Key"/> object for the tab key (forwards tab key).
	/// </summary>
	public static readonly Key Tab = new (KeyCode.Tab);

	/// <summary>
	/// The <see cref="Key"/> object for the return key.
	/// </summary>
	public static readonly Key Enter = new (KeyCode.Enter);

	/// <summary>
	/// The <see cref="Key"/> object for the clear key.
	/// </summary>
	public static readonly Key Clear = new (KeyCode.Clear);

	/// <summary>
	/// The <see cref="Key"/> object for the Shift key.
	/// </summary>
	public static readonly Key Shift = new (KeyCode.ShiftKey);

	/// <summary>
	/// The <see cref="Key"/> object for the Ctrl key.
	/// </summary>
	public static readonly Key Ctrl = new (KeyCode.CtrlKey);

	/// <summary>
	/// The <see cref="Key"/> object for the Alt key.
	/// </summary>
	public static readonly Key Alt = new (KeyCode.AltKey);

	/// <summary>
	/// The <see cref="Key"/> object for the CapsLock key.
	/// </summary>
	public static readonly Key CapsLock = new (KeyCode.CapsLock);

	/// <summary>
	/// The <see cref="Key"/> object for the Escape key.
	/// </summary>
	public static readonly Key Esc = new (KeyCode.Esc);

	/// <summary>
	/// The <see cref="Key"/> object for the Space bar key.
	/// </summary>
	public static readonly Key Space = new (KeyCode.Space);

	/// <summary>
	/// The <see cref="Key"/> object for 0 key.
	/// </summary>
	public static readonly Key D0 = new (KeyCode.D0);

	/// <summary>
	/// The <see cref="Key"/> object for 1 key.
	/// </summary>
	public static readonly Key D1 = new (KeyCode.D1);

	/// <summary>
	/// The <see cref="Key"/> object for 2 key.
	/// </summary>
	public static readonly Key D2 = new (KeyCode.D2);

	/// <summary>
	/// The <see cref="Key"/> object for 3 key.
	/// </summary>
	public static readonly Key D3 = new (KeyCode.D3);

	/// <summary>
	/// The <see cref="Key"/> object for 4 key.
	/// </summary>
	public static readonly Key D4 = new (KeyCode.D4);

	/// <summary>
	/// The <see cref="Key"/> object for 5 key.
	/// </summary>
	public static readonly Key D5 = new (KeyCode.D5);

	/// <summary>
	/// The <see cref="Key"/> object for 6 key.
	/// </summary>
	public static readonly Key D6 = new (KeyCode.D6);

	/// <summary>
	/// The <see cref="Key"/> object for 7 key.
	/// </summary>
	public static readonly Key D7 = new (KeyCode.D7);

	/// <summary>
	/// The <see cref="Key"/> object for 8 key.
	/// </summary>
	public static readonly Key D8 = new (KeyCode.D8);

	/// <summary>
	/// The <see cref="Key"/> object for 9 key.
	/// </summary>
	public static readonly Key D9 = new (KeyCode.D9);

	/// <summary>
	/// The <see cref="Key"/> object for the A key (un-shifted). Use <c>Key.A.WithShift</c> for uppercase 'A'.
	/// </summary>
	public static readonly Key A = new (KeyCode.A);

	/// <summary>
	/// The <see cref="Key"/> object for the B key (un-shifted). Use <c>Key.B.WithShift</c> for uppercase 'B'.
	/// </summary>
	public static readonly Key B = new (KeyCode.B);

	/// <summary>
	/// The <see cref="Key"/> object for the C key (un-shifted). Use <c>Key.C.WithShift</c> for uppercase 'C'.
	/// </summary>
	public static readonly Key C = new (KeyCode.C);

	/// <summary>
	/// The <see cref="Key"/> object for the D key (un-shifted). Use <c>Key.D.WithShift</c> for uppercase 'D'.
	/// </summary>
	public static readonly Key D = new (KeyCode.D);

	/// <summary>
	/// The <see cref="Key"/> object for the E key (un-shifted). Use <c>Key.E.WithShift</c> for uppercase 'E'.
	/// </summary>
	public static readonly Key E = new (KeyCode.E);

	/// <summary>
	/// The <see cref="Key"/> object for the F key (un-shifted). Use <c>Key.F.WithShift</c> for uppercase 'F'.
	/// </summary>
	public static readonly Key F = new (KeyCode.F);

	/// <summary>
	/// The <see cref="Key"/> object for the G key (un-shifted). Use <c>Key.G.WithShift</c> for uppercase 'G'.
	/// </summary>
	public static readonly Key G = new (KeyCode.G);

	/// <summary>
	/// The <see cref="Key"/> object for the H key (un-shifted). Use <c>Key.H.WithShift</c> for uppercase 'H'.
	/// </summary>
	public static readonly Key H = new (KeyCode.H);

	/// <summary>
	/// The <see cref="Key"/> object for the I key (un-shifted). Use <c>Key.I.WithShift</c> for uppercase 'I'.
	/// </summary>
	public static readonly Key I = new (KeyCode.I);

	/// <summary>
	/// The <see cref="Key"/> object for the J key (un-shifted). Use <c>Key.J.WithShift</c> for uppercase 'J'.
	/// </summary>
	public static readonly Key J = new (KeyCode.J);

	/// <summary>
	/// The <see cref="Key"/> object for the K key (un-shifted). Use <c>Key.K.WithShift</c> for uppercase 'K'.
	/// </summary>
	public static readonly Key K = new (KeyCode.K);

	/// <summary>
	/// The <see cref="Key"/> object for the L key (un-shifted). Use <c>Key.L.WithShift</c> for uppercase 'L'.
	/// </summary>
	public static readonly Key L = new (KeyCode.L);

	/// <summary>
	/// The <see cref="Key"/> object for the M key (un-shifted). Use <c>Key.M.WithShift</c> for uppercase 'M'.
	/// </summary>
	public static readonly Key M = new (KeyCode.M);

	/// <summary>
	/// The <see cref="Key"/> object for the N key (un-shifted). Use <c>Key.N.WithShift</c> for uppercase 'N'.
	/// </summary>
	public static readonly Key N = new (KeyCode.N);

	/// <summary>
	/// The <see cref="Key"/> object for the O key (un-shifted). Use <c>Key.O.WithShift</c> for uppercase 'O'.
	/// </summary>
	public static readonly Key O = new (KeyCode.O);

	/// <summary>
	/// The <see cref="Key"/> object for the P key (un-shifted). Use <c>Key.P.WithShift</c> for uppercase 'P'.
	/// </summary>
	public static readonly Key P = new (KeyCode.P);

	/// <summary>
	/// The <see cref="Key"/> object for the Q key (un-shifted). Use <c>Key.Q.WithShift</c> for uppercase 'Q'.
	/// </summary>
	public static readonly Key Q = new (KeyCode.Q);

	/// <summary>
	/// The <see cref="Key"/> object for the R key (un-shifted). Use <c>Key.R.WithShift</c> for uppercase 'R'.
	/// </summary>
	public static readonly Key R = new (KeyCode.R);

	/// <summary>
	/// The <see cref="Key"/> object for the S key (un-shifted). Use <c>Key.S.WithShift</c> for uppercase 'S'.
	/// </summary>
	public static readonly Key S = new (KeyCode.S);

	/// <summary>
	/// The <see cref="Key"/> object for the T key (un-shifted). Use <c>Key.T.WithShift</c> for uppercase 'T'.
	/// </summary>
	public static readonly Key T = new (KeyCode.T);

	/// <summary>
	/// The <see cref="Key"/> object for the U key (un-shifted). Use <c>Key.U.WithShift</c> for uppercase 'U'.
	/// </summary>
	public static readonly Key U = new (KeyCode.U);

	/// <summary>
	/// The <see cref="Key"/> object for the V key (un-shifted). Use <c>Key.V.WithShift</c> for uppercase 'V'.
	/// </summary>
	public static readonly Key V = new (KeyCode.V);

	/// <summary>
	/// The <see cref="Key"/> object for the W key (un-shifted). Use <c>Key.W.WithShift</c> for uppercase 'W'.
	/// </summary>
	public static readonly Key W = new (KeyCode.W);

	/// <summary>
	/// The <see cref="Key"/> object for the X key (un-shifted). Use <c>Key.X.WithShift</c> for uppercase 'X'.
	/// </summary>
	public static readonly Key X = new (KeyCode.X);

	/// <summary>
	/// The <see cref="Key"/> object for the Y key (un-shifted). Use <c>Key.Y.WithShift</c> for uppercase 'Y'.
	/// </summary>
	public static readonly Key Y = new (KeyCode.Y);

	/// <summary>
	/// The <see cref="Key"/> object for the Z key (un-shifted). Use <c>Key.Z.WithShift</c> for uppercase 'Z'.
	/// </summary>
	public static readonly Key Z = new (KeyCode.Z);

	/// <summary>
	/// The <see cref="Key"/> object for the Delete key.
	/// </summary>
	public static readonly Key Delete = new (KeyCode.Delete);

	/// <summary>
	/// The <see cref="Key"/> object for the Cursor up key.
	/// </summary>
	public static readonly Key CursorUp = new (KeyCode.CursorUp);

	/// <summary>
	/// The <see cref="Key"/> object for Cursor down key.
	/// </summary>
	public static readonly Key CursorDown = new (KeyCode.CursorDown);

	/// <summary>
	/// The <see cref="Key"/> object for Cursor left key.
	/// </summary>
	public static readonly Key CursorLeft = new (KeyCode.CursorLeft);

	/// <summary>
	/// The <see cref="Key"/> object for Cursor right key.
	/// </summary>
	public static readonly Key CursorRight = new (KeyCode.CursorRight);

	/// <summary>
	/// The <see cref="Key"/> object for Page Up key.
	/// </summary>
	public static readonly Key PageUp = new (KeyCode.PageUp);

	/// <summary>
	/// The <see cref="Key"/> object for Page Down key.
	/// </summary>
	public static readonly Key PageDown = new (KeyCode.PageDown);

	/// <summary>
	/// The <see cref="Key"/> object for Home key.
	/// </summary>
	public static readonly Key Home = new (KeyCode.Home);

	/// <summary>
	/// The <see cref="Key"/> object for End key.
	/// </summary>
	public static readonly Key End = new (KeyCode.End);

	/// <summary>
	/// The <see cref="Key"/> object for Insert Character key.
	/// </summary>
	public static readonly Key InsertChar = new (KeyCode.InsertChar);

	/// <summary>
	/// The <see cref="Key"/> object for Delete Character key.
	/// </summary>
	public static readonly Key DeleteChar = new (KeyCode.DeleteChar);

	/// <summary>
	/// The <see cref="Key"/> object for Print Screen key.
	/// </summary>
	public static readonly Key PrintScreen = new (KeyCode.PrintScreen);

	/// <summary>
	/// The <see cref="Key"/> object for F1 key.
	/// </summary>
	public static readonly Key F1 = new (KeyCode.F1);

	/// <summary>
	/// The <see cref="Key"/> object for F2 key.
	/// </summary>
	public static readonly Key F2 = new (KeyCode.F2);

	/// <summary>
	/// The <see cref="Key"/> object for F3 key.
	/// </summary>
	public static readonly Key F3 = new (KeyCode.F3);

	/// <summary>
	/// The <see cref="Key"/> object for F4 key.
	/// </summary>
	public static readonly Key F4 = new (KeyCode.F4);

	/// <summary>
	/// The <see cref="Key"/> object for F5 key.
	/// </summary>
	public static readonly Key F5 = new (KeyCode.F5);

	/// <summary>
	/// The <see cref="Key"/> object for F6 key.
	/// </summary>
	public static readonly Key F6 = new (KeyCode.F6);

	/// <summary>
	/// The <see cref="Key"/> object for F7 key.
	/// </summary>
	public static readonly Key F7 = new (KeyCode.F7);

	/// <summary>
	/// The <see cref="Key"/> object for F8 key.
	/// </summary>
	public static readonly Key F8 = new (KeyCode.F8);

	/// <summary>
	/// The <see cref="Key"/> object for F9 key.
	/// </summary>
	public static readonly Key F9 = new (KeyCode.F9);

	/// <summary>
	/// The <see cref="Key"/> object for F10 key.
	/// </summary>
	public static readonly Key F10 = new (KeyCode.F10);

	/// <summary>
	/// The <see cref="Key"/> object for F11 key.
	/// </summary>
	public static readonly Key F11 = new (KeyCode.F11);

	/// <summary>
	/// The <see cref="Key"/> object for F12 key.
	/// </summary>
	public static readonly Key F12 = new (KeyCode.F12);

	/// <summary>
	/// The <see cref="Key"/> object for F13 key.
	/// </summary>
	public static readonly Key F13 = new (KeyCode.F13);

	/// <summary>
	/// The <see cref="Key"/> object for F14 key.
	/// </summary>
	public static readonly Key F14 = new (KeyCode.F14);

	/// <summary>
	/// The <see cref="Key"/> object for F15 key.
	/// </summary>
	public static readonly Key F15 = new (KeyCode.F15);

	/// <summary>
	/// The <see cref="Key"/> object for F16 key.
	/// </summary>
	public static readonly Key F16 = new (KeyCode.F16);

	/// <summary>
	/// The <see cref="Key"/> object for F17 key.
	/// </summary>
	public static readonly Key F17 = new (KeyCode.F17);

	/// <summary>
	/// The <see cref="Key"/> object for F18 key.
	/// </summary>
	public static readonly Key F18 = new (KeyCode.F18);

	/// <summary>
	/// The <see cref="Key"/> object for F19 key.
	/// </summary>
	public static readonly Key F19 = new (KeyCode.F19);

	/// <summary>
	/// The <see cref="Key"/> object for F20 key.
	/// </summary>
	public static readonly Key F20 = new (KeyCode.F20);

	/// <summary>
	/// The <see cref="Key"/> object for F21 key.
	/// </summary>
	public static readonly Key F21 = new (KeyCode.F21);

	/// <summary>
	/// The <see cref="Key"/> object for F22 key.
	/// </summary>
	public static readonly Key F22 = new (KeyCode.F22);

	/// <summary>
	/// The <see cref="Key"/> object for F23 key.
	/// </summary>
	public static readonly Key F23 = new (KeyCode.F23);

	/// <summary>
	/// The <see cref="Key"/> object for F24 key.
	/// </summary>
	public static readonly Key F24 = new (KeyCode.F24);
	#endregion
}