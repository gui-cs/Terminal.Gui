using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Terminal.Gui.ConsoleDrivers {
	/// <summary>
	/// Helper class to handle the scan code and virtual key from a <see cref="ConsoleKey"/>.
	/// </summary>
	public static class ConsoleKeyMapping {
		class ScanCodeMapping : IEquatable<ScanCodeMapping> {
			public uint ScanCode;
			public uint VirtualKey;
			public ConsoleModifiers Modifiers;
			public uint UnicodeChar;

			public ScanCodeMapping (uint scanCode, uint virtualKey, ConsoleModifiers modifiers, uint unicodeChar)
			{
				ScanCode = scanCode;
				VirtualKey = virtualKey;
				Modifiers = modifiers;
				UnicodeChar = unicodeChar;
			}

			public bool Equals (ScanCodeMapping other)
			{
				return ScanCode.Equals (other.ScanCode) &&
					VirtualKey.Equals (other.VirtualKey) &&
					Modifiers.Equals (other.Modifiers) &&
					UnicodeChar.Equals (other.UnicodeChar);
			}
		}

		static ConsoleModifiers GetModifiers (ConsoleModifiers modifiers)
		{
			if (modifiers.HasFlag (ConsoleModifiers.Shift)
			&& !modifiers.HasFlag (ConsoleModifiers.Alt)
			&& !modifiers.HasFlag (ConsoleModifiers.Control)) {
				return ConsoleModifiers.Shift;
			} else if (modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control)) {
				return modifiers;
			}

			return 0;
		}

		static ScanCodeMapping GetScanCode (string propName, uint keyValue, ConsoleModifiers modifiers)
		{
			switch (propName) {
			case "UnicodeChar":
				var sCode = _scanCodes.FirstOrDefault ((e) => e.UnicodeChar == keyValue && e.Modifiers == modifiers);
				if (sCode == null && modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control)) {
					return _scanCodes.FirstOrDefault ((e) => e.UnicodeChar == keyValue && e.Modifiers == 0);
				}
				return sCode;
			case "VirtualKey":
				sCode = _scanCodes.FirstOrDefault ((e) => e.VirtualKey == keyValue && e.Modifiers == modifiers);
				if (sCode == null && modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control)) {
					return _scanCodes.FirstOrDefault ((e) => e.VirtualKey == keyValue && e.Modifiers == 0);
				}
				return sCode;
			}

			return null;
		}

		/// <summary>
		/// Get the scan code from a <see cref="ConsoleKeyInfo"/>.
		/// </summary>
		/// <param name="consoleKeyInfo">The console key info.</param>
		/// <returns>The value if apply.</returns>
		public static uint GetScanCodeFromConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
		{
			var mod = GetModifiers (consoleKeyInfo.Modifiers);
			ScanCodeMapping scode = GetScanCode ("VirtualKey", (uint)consoleKeyInfo.Key, mod);
			if (scode != null) {
				return scode.ScanCode;
			}

			return 0;
		}

		/// <summary>
		/// Gets the <see cref="ConsoleKeyInfo"/> from the provided <see cref="KeyCode"/>.
		/// </summary>
		/// <param name="key">The key code.</param>
		/// <returns>The console key info.</returns>
		public static ConsoleKeyInfo GetConsoleKeyInfoFromKeyCode (KeyCode key)
		{
			var modifiers = MapToConsoleModifiers (key);
			var keyValue = MapKeyCodeToConsoleKey (key, out bool isConsoleKey);
			if (isConsoleKey) {
				var mod = GetModifiers (modifiers);
				var scode = GetScanCode ("VirtualKey", (uint)keyValue, mod);
				if (scode != null) {
					return new ConsoleKeyInfo ((char)scode.UnicodeChar, (ConsoleKey)scode.VirtualKey, modifiers.HasFlag (ConsoleModifiers.Shift),
						modifiers.HasFlag (ConsoleModifiers.Alt), modifiers.HasFlag (ConsoleModifiers.Control));
				}
			} else {
				var keyChar = GetKeyCharFromUnicodeChar ((uint)keyValue, modifiers, out uint consoleKey, out _, isConsoleKey);
				if (consoleKey != 0) {
					return new ConsoleKeyInfo ((char)keyChar, (ConsoleKey)consoleKey, modifiers.HasFlag (ConsoleModifiers.Shift),
						modifiers.HasFlag (ConsoleModifiers.Alt), modifiers.HasFlag (ConsoleModifiers.Control));
				}
			}

			return new ConsoleKeyInfo ((char)keyValue, ConsoleKey.None, modifiers.HasFlag (ConsoleModifiers.Shift),
				modifiers.HasFlag (ConsoleModifiers.Alt), modifiers.HasFlag (ConsoleModifiers.Control));
		}

		/// <summary>
		/// Map existing <see cref="KeyCode"/> modifiers to <see cref="ConsoleModifiers"/>.
		/// </summary>
		/// <param name="key">The key code.</param>
		/// <returns>The console modifiers.</returns>
		public static ConsoleModifiers MapToConsoleModifiers (KeyCode key)
		{
			var modifiers = new ConsoleModifiers ();
			if (key.HasFlag (KeyCode.ShiftMask)) {
				modifiers |= ConsoleModifiers.Shift;
			}
			if (key.HasFlag (KeyCode.AltMask)) {
				modifiers |= ConsoleModifiers.Alt;
			}
			if (key.HasFlag (KeyCode.CtrlMask)) {
				modifiers |= ConsoleModifiers.Control;
			}

			return modifiers;
		}

		/// <summary>
		/// Gets <see cref="ConsoleModifiers"/> from <see cref="bool"/> modifiers.
		/// </summary>
		/// <param name="shift">The shift key.</param>
		/// <param name="alt">The alt key.</param>
		/// <param name="control">The control key.</param>
		/// <returns>The console modifiers.</returns>
		public static ConsoleModifiers GetModifiers (bool shift, bool alt, bool control)
		{
			var modifiers = new ConsoleModifiers ();
			if (shift) {
				modifiers |= ConsoleModifiers.Shift;
			}
			if (alt) {
				modifiers |= ConsoleModifiers.Alt;
			}
			if (control) {
				modifiers |= ConsoleModifiers.Control;
			}

			return modifiers;
		}

		/// <summary>
		/// Get the <see cref="ConsoleKeyInfo"/> from a unicode character and modifiers (e.g. (Key)'a' and (Key)Key.CtrlMask).
		/// </summary>
		/// <param name="keyValue">The key as a unicode codepoint.</param>
		/// <param name="modifiers">The modifier keys.</param>
		/// <param name="scanCode">The resulting scan code.</param>
		/// <returns>The <see cref="ConsoleKeyInfo"/>.</returns>
		static ConsoleKeyInfo GetConsoleKeyInfoFromKeyChar (uint keyValue, ConsoleModifiers modifiers, out uint scanCode)
		{
			scanCode = 0;
			if (keyValue == 0) {
				return new ConsoleKeyInfo ((char)keyValue, ConsoleKey.None, modifiers.HasFlag (ConsoleModifiers.Shift),
					modifiers.HasFlag (ConsoleModifiers.Alt), modifiers.HasFlag (ConsoleModifiers.Control));
			}

			uint outputChar = keyValue;
			uint consoleKey;
			if (keyValue > byte.MaxValue) {
				var sCode = _scanCodes.FirstOrDefault ((e) => e.UnicodeChar == keyValue);
				if (sCode == null) {
					consoleKey = (byte)(keyValue & byte.MaxValue);
					sCode = _scanCodes.FirstOrDefault ((e) => e.VirtualKey == consoleKey);
					if (sCode == null) {
						consoleKey = 0;
						outputChar = keyValue;
					} else {
						outputChar = (char)(keyValue >> 8);
					}
				} else {
					consoleKey = (byte)sCode.VirtualKey;
					outputChar = keyValue;
				}
			} else {
				consoleKey = (byte)keyValue;
				outputChar = '\0';
			}

			return new ConsoleKeyInfo ((char)outputChar, (ConsoleKey)consoleKey, modifiers.HasFlag (ConsoleModifiers.Shift),
				modifiers.HasFlag (ConsoleModifiers.Alt), modifiers.HasFlag (ConsoleModifiers.Control));
		}

		internal static uint GetKeyChar (uint keyValue, ConsoleModifiers modifiers)
		{
			if (modifiers == ConsoleModifiers.Shift && keyValue - 32 is >= 'A' and <= 'Z') {
				return keyValue - 32;
			} else if (modifiers == ConsoleModifiers.None && keyValue is >= 'A' and <= 'Z') {
				return keyValue + 32;
			}

			if (modifiers == ConsoleModifiers.Shift && keyValue - 32 is >= 'À' and <= 'Ý') {
				return keyValue - 32;
			} else if (modifiers == ConsoleModifiers.None && keyValue is >= 'À' and <= 'Ý') {
				return keyValue + 32;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '0') {
				return keyValue + 13;
			} else if (modifiers == ConsoleModifiers.None && keyValue - 13 is '0') {
				return keyValue - 13;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is >= '1' and <= '9' and not '7') {
				return keyValue - 16;
			} else if (modifiers == ConsoleModifiers.None && keyValue + 16 is >= '1' and <= '9' and not '7') {
				return keyValue + 16;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '7') {
				return keyValue - 8;
			} else if (modifiers == ConsoleModifiers.None && keyValue + 8 is '7') {
				return keyValue + 8;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '\'') {
				return keyValue + 24;
			} else if (modifiers == ConsoleModifiers.None && keyValue - 24 is '\'') {
				return keyValue - 24;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '«') {
				return keyValue + 16;
			} else if (modifiers == ConsoleModifiers.None && keyValue - 16 is '«') {
				return keyValue - 16;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '\\') {
				return keyValue + 32;
			} else if (modifiers == ConsoleModifiers.None && keyValue - 32 is '\\') {
				return keyValue - 32;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '+') {
				return keyValue - 1;
			} else if (modifiers == ConsoleModifiers.None && keyValue + 1 is '+') {
				return keyValue + 1;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '´') {
				return keyValue - 84;
			} else if (modifiers == ConsoleModifiers.None && keyValue + 84 is '´') {
				return keyValue + 84;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is 'º') {
				return keyValue - 16;
			} else if (modifiers == ConsoleModifiers.None && keyValue + 16 is 'º') {
				return keyValue + 16;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '~') {
				return keyValue - 32;
			} else if (modifiers == ConsoleModifiers.None && keyValue + 32 is '~') {
				return keyValue + 32;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '<') {
				return keyValue + 2;
			} else if (modifiers == ConsoleModifiers.None && keyValue - 2 is '<') {
				return keyValue - 2;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is ',') {
				return keyValue + 15;
			} else if (modifiers == ConsoleModifiers.None && keyValue - 15 is ',') {
				return keyValue - 15;
			}
			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '.') {
				return keyValue + 12;
			} else if (modifiers == ConsoleModifiers.None && keyValue - 12 is '.') {
				return keyValue - 12;
			}

			if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '-') {
				return keyValue + 50;
			} else if (modifiers == ConsoleModifiers.None && keyValue - 50 is '-') {
				return keyValue - 50;
			}

			return keyValue;
		}

		/// <summary>
		/// Get the output character from the <see cref="GetConsoleKeyInfoFromKeyCode"/>, with the correct <see cref="ConsoleKey"/>
		/// and the scan code used on <see cref="WindowsDriver"/>.
		/// </summary>
		/// <param name="unicodeChar">The unicode character.</param>
		/// <param name="modifiers">The modifiers keys.</param>
		/// <param name="consoleKey">The resulting console key.</param>
		/// <param name="scanCode">The resulting scan code.</param>
		/// <param name="isConsoleKey">Indicates if the <paramref name="unicodeChar"/> is a <see cref="ConsoleKey"/>.</param>
		/// <returns>The output character or the <paramref name="consoleKey"/>.</returns>
		/// <remarks>This is only used by the <see cref="GetConsoleKeyInfoFromKeyCode"/> and by unit tests.</remarks>
		internal static uint GetKeyCharFromUnicodeChar (uint unicodeChar, ConsoleModifiers modifiers, out uint consoleKey, out uint scanCode, bool isConsoleKey = false)
		{
			uint decodedChar = unicodeChar >> 8 == 0xff ? unicodeChar & 0xff : unicodeChar;
			uint keyChar = decodedChar;
			consoleKey = 0;
			var mod = GetModifiers (modifiers);
			scanCode = 0;
			ScanCodeMapping scode = null;
			if (unicodeChar != 0 && unicodeChar >> 8 != 0xff && isConsoleKey) {
				scode = GetScanCode ("VirtualKey", decodedChar, mod);
			}
			if (isConsoleKey && scode != null) {
				consoleKey = scode.VirtualKey;
				keyChar = scode.UnicodeChar;
				scanCode = scode.ScanCode;
			}
			if (scode == null) {
				scode = unicodeChar != 0 ? GetScanCode ("UnicodeChar", decodedChar, mod) : null;
				if (scode != null) {
					consoleKey = scode.VirtualKey;
					keyChar = scode.UnicodeChar;
					scanCode = scode.ScanCode;
				}
			}
			if (decodedChar != 0 && scanCode == 0 && char.IsLetter ((char)decodedChar)) {
				string stFormD = ((char)decodedChar).ToString ().Normalize (System.Text.NormalizationForm.FormD);
				for (int i = 0; i < stFormD.Length; i++) {
					var uc = CharUnicodeInfo.GetUnicodeCategory (stFormD [i]);
					if (uc != UnicodeCategory.NonSpacingMark && uc != UnicodeCategory.OtherLetter) {
						consoleKey = char.ToUpper (stFormD [i]);
						scode = GetScanCode ("VirtualKey", char.ToUpper (stFormD [i]), 0);
						if (scode != null) {
							scanCode = scode.ScanCode;
						}
					}
				}
			}
			if (keyChar < 255 && consoleKey == 0 && scanCode == 0) {
				scode = GetScanCode ("VirtualKey", keyChar, mod);
				if (scode != null) {
					consoleKey = scode.VirtualKey;
					keyChar = scode.UnicodeChar;
					scanCode = scode.ScanCode;
				}
			}

			return keyChar;
		}

		/// <summary>
		/// Maps a unicode character (e.g. (Key)'a') to a uint representing a <see cref="ConsoleKey"/>.
		/// </summary>
		/// <param name="keyValue">The key value.</param>
		/// <param name="isConsoleKey">Indicates if the <paramref name="keyValue"/> is a <see cref="ConsoleKey"/>.
		/// <see langword="true"/> means the return value is in the ConsoleKey enum.
		/// <see langword="false"/> means the return value can be mapped to a valid unicode character.
		/// </param>
		/// <returns>The <see cref="ConsoleKey"/> or the <paramref name="keyValue"/>.</returns>
		/// <remarks>This is only used by the <see cref="GetConsoleKeyInfoFromKeyCode"/> and by unit tests.</remarks>
		internal static uint MapKeyCodeToConsoleKey (KeyCode keyValue, out bool isConsoleKey)
		{
			isConsoleKey = true;
			keyValue = keyValue & ~KeyCode.CtrlMask & ~KeyCode.ShiftMask & ~KeyCode.AltMask;

			switch (keyValue) {
			case KeyCode.Enter:
				return (uint)ConsoleKey.Enter;
			case KeyCode.Delete:
				return (uint)ConsoleKey.Delete;
			case KeyCode.CursorUp:
				return (uint)ConsoleKey.UpArrow;
			case KeyCode.CursorDown:
				return (uint)ConsoleKey.DownArrow;
			case KeyCode.CursorLeft:
				return (uint)ConsoleKey.LeftArrow;
			case KeyCode.CursorRight:
				return (uint)ConsoleKey.RightArrow;
			case KeyCode.PageUp:
				return (uint)ConsoleKey.PageUp;
			case KeyCode.PageDown:
				return (uint)ConsoleKey.PageDown;
			case KeyCode.Home:
				return (uint)ConsoleKey.Home;
			case KeyCode.End:
				return (uint)ConsoleKey.End;
			case KeyCode.InsertChar:
				return (uint)ConsoleKey.Insert;
			case KeyCode.DeleteChar:
				return (uint)ConsoleKey.Delete;
			case KeyCode.F1:
				return (uint)ConsoleKey.F1;
			case KeyCode.F2:
				return (uint)ConsoleKey.F2;
			case KeyCode.F3:
				return (uint)ConsoleKey.F3;
			case KeyCode.F4:
				return (uint)ConsoleKey.F4;
			case KeyCode.F5:
				return (uint)ConsoleKey.F5;
			case KeyCode.F6:
				return (uint)ConsoleKey.F6;
			case KeyCode.F7:
				return (uint)ConsoleKey.F7;
			case KeyCode.F8:
				return (uint)ConsoleKey.F8;
			case KeyCode.F9:
				return (uint)ConsoleKey.F9;
			case KeyCode.F10:
				return (uint)ConsoleKey.F10;
			case KeyCode.F11:
				return (uint)ConsoleKey.F11;
			case KeyCode.F12:
				return (uint)ConsoleKey.F12;
			case KeyCode.F13:
				return (uint)ConsoleKey.F13;
			case KeyCode.F14:
				return (uint)ConsoleKey.F14;
			case KeyCode.F15:
				return (uint)ConsoleKey.F15;
			case KeyCode.F16:
				return (uint)ConsoleKey.F16;
			case KeyCode.F17:
				return (uint)ConsoleKey.F17;
			case KeyCode.F18:
				return (uint)ConsoleKey.F18;
			case KeyCode.F19:
				return (uint)ConsoleKey.F19;
			case KeyCode.F20:
				return (uint)ConsoleKey.F20;
			case KeyCode.F21:
				return (uint)ConsoleKey.F21;
			case KeyCode.F22:
				return (uint)ConsoleKey.F22;
			case KeyCode.F23:
				return (uint)ConsoleKey.F23;
			case KeyCode.F24:
				return (uint)ConsoleKey.F24;
			case KeyCode.Tab | KeyCode.ShiftMask:
				return (uint)ConsoleKey.Tab;
			}

			isConsoleKey = false;
			return (uint)keyValue;
		}

		/// <summary>
		/// Maps a <see cref="ConsoleKeyInfo"/> to a <see cref="KeyCode"/>.
		/// </summary>
		/// <param name="consoleKeyInfo">The console key.</param>
		/// <returns>The <see cref="KeyCode"/> or the <paramref name="consoleKeyInfo"/>.</returns>
		public static KeyCode MapConsoleKeyInfoToKeyCode (ConsoleKeyInfo consoleKeyInfo)
		{
			KeyCode keyCode;

			switch (consoleKeyInfo.Key) {
			case ConsoleKey.Enter:
				keyCode = KeyCode.Enter;
				break;
			case ConsoleKey.Delete:
				keyCode = KeyCode.Delete;
				break;
			case ConsoleKey.UpArrow:
				keyCode = KeyCode.CursorUp;
				break;
			case ConsoleKey.DownArrow:
				keyCode = KeyCode.CursorDown;
				break;
			case ConsoleKey.LeftArrow:
				keyCode = KeyCode.CursorLeft;
				break;
			case ConsoleKey.RightArrow:
				keyCode = KeyCode.CursorRight;
				break;
			case ConsoleKey.PageUp:
				keyCode = KeyCode.PageUp;
				break;
			case ConsoleKey.PageDown:
				keyCode = KeyCode.PageDown;
				break;
			case ConsoleKey.Home:
				keyCode = KeyCode.Home;
				break;
			case ConsoleKey.End:
				keyCode = KeyCode.End;
				break;
			case ConsoleKey.Insert:
				keyCode = KeyCode.InsertChar;
				break;
			case ConsoleKey.F1:
				keyCode = KeyCode.F1;
				break;
			case ConsoleKey.F2:
				keyCode = KeyCode.F2;
				break;
			case ConsoleKey.F3:
				keyCode = KeyCode.F3;
				break;
			case ConsoleKey.F4:
				keyCode = KeyCode.F4;
				break;
			case ConsoleKey.F5:
				keyCode = KeyCode.F5;
				break;
			case ConsoleKey.F6:
				keyCode = KeyCode.F6;
				break;
			case ConsoleKey.F7:
				keyCode = KeyCode.F7;
				break;
			case ConsoleKey.F8:
				keyCode = KeyCode.F8;
				break;
			case ConsoleKey.F9:
				keyCode = KeyCode.F9;
				break;
			case ConsoleKey.F10:
				keyCode = KeyCode.F10;
				break;
			case ConsoleKey.F11:
				keyCode = KeyCode.F11;
				break;
			case ConsoleKey.F12:
				keyCode = KeyCode.F12;
				break;
			case ConsoleKey.F13:
				keyCode = KeyCode.F13;
				break;
			case ConsoleKey.F14:
				keyCode = KeyCode.F14;
				break;
			case ConsoleKey.F15:
				keyCode = KeyCode.F15;
				break;
			case ConsoleKey.F16:
				keyCode = KeyCode.F16;
				break;
			case ConsoleKey.F17:
				keyCode = KeyCode.F17;
				break;
			case ConsoleKey.F18:
				keyCode = KeyCode.F18;
				break;
			case ConsoleKey.F19:
				keyCode = KeyCode.F19;
				break;
			case ConsoleKey.F20:
				keyCode = KeyCode.F20;
				break;
			case ConsoleKey.F21:
				keyCode = KeyCode.F21;
				break;
			case ConsoleKey.F22:
				keyCode = KeyCode.F22;
				break;
			case ConsoleKey.F23:
				keyCode = KeyCode.F23;
				break;
			case ConsoleKey.F24:
				keyCode = KeyCode.F24;
				break;
			case ConsoleKey.Tab:
				keyCode = KeyCode.Tab;
				break;
			default:
				keyCode = (KeyCode)consoleKeyInfo.KeyChar;
				break;
			}
			keyCode |= MapToKeyCodeModifiers (consoleKeyInfo.Modifiers, keyCode);

			return keyCode;
		}

		/// <summary>
		/// Maps a <see cref="ConsoleKeyInfo"/> to a <see cref="KeyCode"/>.
		/// </summary>
		/// <param name="modifiers">The console modifiers.</param>
		/// <param name="key">The key code.</param>
		/// <returns>The <see cref="KeyCode"/> with <see cref="ConsoleModifiers"/> or the <paramref name="key"/></returns>
		public static KeyCode MapToKeyCodeModifiers (ConsoleModifiers modifiers, KeyCode key)
		{
			var keyMod = new KeyCode ();
			if ((modifiers & ConsoleModifiers.Shift) != 0) {
				keyMod = KeyCode.ShiftMask;
			}
			if ((modifiers & ConsoleModifiers.Control) != 0) {
				keyMod |= KeyCode.CtrlMask;
			}
			if ((modifiers & ConsoleModifiers.Alt) != 0) {
				keyMod |= KeyCode.AltMask;
			}

			return keyMod != KeyCode.Null ? keyMod | key : key;
		}

		static HashSet<ScanCodeMapping> _scanCodes = new HashSet<ScanCodeMapping> {
			new ScanCodeMapping (1, 27, 0, 27), // Escape
			new ScanCodeMapping (1, 27, ConsoleModifiers.Shift, 27),
			new ScanCodeMapping (2, 49, 0, 49), // D1
			new ScanCodeMapping (2, 49, ConsoleModifiers.Shift, 33),
			new ScanCodeMapping (3, 50, 0, 50), // D2
			new ScanCodeMapping (3, 50, ConsoleModifiers.Shift, 34),
			new ScanCodeMapping (3, 50, ConsoleModifiers.Alt | ConsoleModifiers.Control, 64),
			new ScanCodeMapping (4, 51, 0, 51), // D3
			new ScanCodeMapping (4, 51, ConsoleModifiers.Shift, 35),
			new ScanCodeMapping (4, 51, ConsoleModifiers.Alt | ConsoleModifiers.Control, 163),
			new ScanCodeMapping (5, 52, 0, 52), // D4
			new ScanCodeMapping (5, 52, ConsoleModifiers.Shift, 36),
			new ScanCodeMapping (5, 52, ConsoleModifiers.Alt | ConsoleModifiers.Control, 167),
			new ScanCodeMapping (6, 53, 0, 53), // D5
			new ScanCodeMapping (6, 53, ConsoleModifiers.Shift, 37),
			new ScanCodeMapping (6, 53, ConsoleModifiers.Alt | ConsoleModifiers.Control, 8364),
			new ScanCodeMapping (7, 54, 0, 54), // D6
			new ScanCodeMapping (7, 54, ConsoleModifiers.Shift, 38),
			new ScanCodeMapping (8, 55, 0, 55), // D7
			new ScanCodeMapping (8, 55, ConsoleModifiers.Shift, 47),
			new ScanCodeMapping (8, 55, ConsoleModifiers.Alt | ConsoleModifiers.Control, 123),
			new ScanCodeMapping (9, 56, 0, 56), // D8
			new ScanCodeMapping (9, 56, ConsoleModifiers.Shift, 40),
			new ScanCodeMapping (9, 56, ConsoleModifiers.Alt | ConsoleModifiers.Control, 91),
			new ScanCodeMapping (10, 57, 0, 57), // D9
			new ScanCodeMapping (10, 57, ConsoleModifiers.Shift, 41),
			new ScanCodeMapping (10, 57, ConsoleModifiers.Alt | ConsoleModifiers.Control, 93),
			new ScanCodeMapping (11, 48, 0, 48), // D0
			new ScanCodeMapping (11, 48, ConsoleModifiers.Shift, 61),
			new ScanCodeMapping (11, 48, ConsoleModifiers.Alt | ConsoleModifiers.Control, 125),
			new ScanCodeMapping (12, 219, 0, 39), // Oem4
			new ScanCodeMapping (12, 219, ConsoleModifiers.Shift, 63),
			new ScanCodeMapping (13, 221, 0, 171), // Oem6
			new ScanCodeMapping (13, 221, ConsoleModifiers.Shift, 187),
			new ScanCodeMapping (14, 8, 0, 8), // Backspace
			new ScanCodeMapping (14, 8, ConsoleModifiers.Shift, 8),
			new ScanCodeMapping (15, 9, 0, 9), // Tab
			new ScanCodeMapping (15, 9, ConsoleModifiers.Shift, 15),
			new ScanCodeMapping (16, 81, 0, 113), // Q
			new ScanCodeMapping (16, 81, ConsoleModifiers.Shift, 81),
			new ScanCodeMapping (17, 87, 0, 119), // W
			new ScanCodeMapping (17, 87, ConsoleModifiers.Shift, 87),
			new ScanCodeMapping (18, 69, 0, 101), // E
			new ScanCodeMapping (18, 69, ConsoleModifiers.Shift, 69),
			new ScanCodeMapping (19, 82, 0, 114), // R
			new ScanCodeMapping (19, 82, ConsoleModifiers.Shift, 82),
			new ScanCodeMapping (20, 84, 0, 116), // T
			new ScanCodeMapping (20, 84, ConsoleModifiers.Shift, 84),
			new ScanCodeMapping (21, 89, 0, 121), // Y
			new ScanCodeMapping (21, 89, ConsoleModifiers.Shift, 89),
			new ScanCodeMapping (22, 85, 0, 117), // U
			new ScanCodeMapping (22, 85, ConsoleModifiers.Shift, 85),
			new ScanCodeMapping (23, 73, 0, 105), // I
			new ScanCodeMapping (23, 73, ConsoleModifiers.Shift, 73),
			new ScanCodeMapping (24, 79, 0, 111), // O
			new ScanCodeMapping (24, 79, ConsoleModifiers.Shift, 79),
			new ScanCodeMapping (25, 80, 0, 112), // P
			new ScanCodeMapping (25, 80, ConsoleModifiers.Shift, 80),
			new ScanCodeMapping (26, 187, 0, 43), // OemPlus
			new ScanCodeMapping (26, 187, ConsoleModifiers.Shift, 42),
			new ScanCodeMapping (26, 187, ConsoleModifiers.Alt | ConsoleModifiers.Control, 168),
			new ScanCodeMapping (27, 186, 0, 180), // Oem1
			new ScanCodeMapping (27, 186, ConsoleModifiers.Shift, 96),
			new ScanCodeMapping (28, 13, 0, 13), // Enter
			new ScanCodeMapping (28, 13, ConsoleModifiers.Shift, 13),
			new ScanCodeMapping (29, 17, 0, 0), // Control
			new ScanCodeMapping (29, 17, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (scanCode: 30, virtualKey: 65, modifiers: 0, unicodeChar: 97), // VK = A, UC = 'a'
			new ScanCodeMapping (30, 65, ConsoleModifiers.Shift, 65),  // VK = A | Shift, UC = 'A'
			new ScanCodeMapping (31, 83, 0, 115), // S
			new ScanCodeMapping (31, 83, ConsoleModifiers.Shift, 83),
			new ScanCodeMapping (32, 68, 0, 100), // D
			new ScanCodeMapping (32, 68, ConsoleModifiers.Shift, 68),
			new ScanCodeMapping (33, 70, 0, 102), // F
			new ScanCodeMapping (33, 70, ConsoleModifiers.Shift, 70),
			new ScanCodeMapping (34, 71, 0, 103), // G
			new ScanCodeMapping (34, 71, ConsoleModifiers.Shift, 71),
			new ScanCodeMapping (35, 72, 0, 104), // H
			new ScanCodeMapping (35, 72, ConsoleModifiers.Shift, 72),
			new ScanCodeMapping (36, 74, 0, 106), // J
			new ScanCodeMapping (36, 74, ConsoleModifiers.Shift, 74),
			new ScanCodeMapping (37, 75, 0, 107), // K
			new ScanCodeMapping (37, 75, ConsoleModifiers.Shift, 75),
			new ScanCodeMapping (38, 76, 0, 108), // L
			new ScanCodeMapping (38, 76, ConsoleModifiers.Shift, 76),
			new ScanCodeMapping (39, 192, 0, 231), // Oem3
			new ScanCodeMapping (39, 192, ConsoleModifiers.Shift, 199),
			new ScanCodeMapping (40, 222, 0, 186), // Oem7
			new ScanCodeMapping (40, 222, ConsoleModifiers.Shift, 170),
			new ScanCodeMapping (41, 220, 0, 92), // Oem5
			new ScanCodeMapping (41, 220, ConsoleModifiers.Shift, 124),
			new ScanCodeMapping (42, 16, 0, 0), // LShift
			new ScanCodeMapping (42, 16, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (43, 191, 0, 126), // Oem2
			new ScanCodeMapping (43, 191, ConsoleModifiers.Shift, 94),
			new ScanCodeMapping (44, 90, 0, 122), // Z
			new ScanCodeMapping (44, 90, ConsoleModifiers.Shift, 90),
			new ScanCodeMapping (45, 88, 0, 120), // X
			new ScanCodeMapping (45, 88, ConsoleModifiers.Shift, 88),
			new ScanCodeMapping (46, 67, 0, 99), // C
			new ScanCodeMapping (46, 67, ConsoleModifiers.Shift, 67),
			new ScanCodeMapping (47, 86, 0, 118), // V
			new ScanCodeMapping (47, 86, ConsoleModifiers.Shift, 86),
			new ScanCodeMapping (48, 66, 0, 98), // B
			new ScanCodeMapping (48, 66, ConsoleModifiers.Shift, 66),
			new ScanCodeMapping (49, 78, 0, 110), // N
			new ScanCodeMapping (49, 78, ConsoleModifiers.Shift, 78),
			new ScanCodeMapping (50, 77, 0, 109), // M
			new ScanCodeMapping (50, 77, ConsoleModifiers.Shift, 77),
			new ScanCodeMapping (51, 188, 0, 44), // OemComma
			new ScanCodeMapping (51, 188, ConsoleModifiers.Shift, 59),
			new ScanCodeMapping (52, 190, 0, 46), // OemPeriod
			new ScanCodeMapping (52, 190, ConsoleModifiers.Shift, 58),
			new ScanCodeMapping (53, 189, 0, 45), // OemMinus
			new ScanCodeMapping (53, 189, ConsoleModifiers.Shift, 95),
			new ScanCodeMapping (54, 16, 0, 0), // RShift
			new ScanCodeMapping (54, 16, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (55, 44, 0, 0), // PrintScreen
			new ScanCodeMapping (55, 44, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (56, 18, 0, 0), // Alt
			new ScanCodeMapping (56, 18, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (57, 32, 0, 32), // Spacebar
			new ScanCodeMapping (57, 32, ConsoleModifiers.Shift, 32),
			new ScanCodeMapping (58, 20, 0, 0), // Caps
			new ScanCodeMapping (58, 20, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (59, 112, 0, 0), // F1
			new ScanCodeMapping (59, 112, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (60, 113, 0, 0), // F2
			new ScanCodeMapping (60, 113, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (61, 114, 0, 0), // F3
			new ScanCodeMapping (61, 114, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (62, 115, 0, 0), // F4
			new ScanCodeMapping (62, 115, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (63, 116, 0, 0), // F5
			new ScanCodeMapping (63, 116, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (64, 117, 0, 0), // F6
			new ScanCodeMapping (64, 117, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (65, 118, 0, 0), // F7
			new ScanCodeMapping (65, 118, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (66, 119, 0, 0), // F8
			new ScanCodeMapping (66, 119, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (67, 120, 0, 0), // F9
			new ScanCodeMapping (67, 120, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (68, 121, 0, 0), // F10
			new ScanCodeMapping (68, 121, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (69, 144, 0, 0), // Num
			new ScanCodeMapping (69, 144, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (70, 145, 0, 0), // Scroll
			new ScanCodeMapping (70, 145, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (71, 36, 0, 0), // Home
			new ScanCodeMapping (71, 36, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (72, 38, 0, 0), // UpArrow
			new ScanCodeMapping (72, 38, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (73, 33, 0, 0), // PageUp
			new ScanCodeMapping (73, 33, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (74, 109, 0, 45), // Subtract
			new ScanCodeMapping (74, 109, ConsoleModifiers.Shift, 45),
			new ScanCodeMapping (75, 37, 0, 0), // LeftArrow
			new ScanCodeMapping (75, 37, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (76, 12, 0, 0), // Center
			new ScanCodeMapping (76, 12, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (77, 39, 0, 0), // RightArrow
			new ScanCodeMapping (77, 39, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (78, 107, 0, 43), // Add
			new ScanCodeMapping (78, 107, ConsoleModifiers.Shift, 43),
			new ScanCodeMapping (79, 35, 0, 0), // End
			new ScanCodeMapping (79, 35, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (80, 40, 0, 0), // DownArrow
			new ScanCodeMapping (80, 40, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (81, 34, 0, 0), // PageDown
			new ScanCodeMapping (81, 34, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (82, 45, 0, 0), // Insert
			new ScanCodeMapping (82, 45, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (83, 46, 0, 0), // Delete
			new ScanCodeMapping (83, 46, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (86, 226, 0, 60), // OEM 102
			new ScanCodeMapping (86, 226, ConsoleModifiers.Shift, 62),
			new ScanCodeMapping (87, 122, 0, 0), // F11
			new ScanCodeMapping (87, 122, ConsoleModifiers.Shift, 0),
			new ScanCodeMapping (88, 123, 0, 0), // F12
			new ScanCodeMapping (88, 123, ConsoleModifiers.Shift, 0)
		};

		/// <summary>
		/// Decode a <see cref="ConsoleKeyInfo"/> that is using <see cref="ConsoleKey.Packet"/>.
		/// </summary>
		/// <param name="consoleKeyInfo">The console key info.</param>
		/// <returns>The decoded <see cref="ConsoleKeyInfo"/> or the <paramref name="consoleKeyInfo"/>.</returns>
		/// <remarks>If it's a <see cref="ConsoleKey.Packet"/> the <see cref="ConsoleKeyInfo.KeyChar"/> may be
		/// a <see cref="ConsoleKeyInfo.Key"/> or a <see cref="ConsoleKeyInfo.KeyChar"/> value.
		/// </remarks>
		public static ConsoleKeyInfo DecodeVKPacketToKConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
		{
			if (consoleKeyInfo.Key != ConsoleKey.Packet) {
				return consoleKeyInfo;
			}

			return GetConsoleKeyInfoFromKeyChar (consoleKeyInfo.KeyChar, consoleKeyInfo.Modifiers, out _);
		}

		/// <summary>
		/// Encode the <see cref="ConsoleKeyInfo.KeyChar"/> with the <see cref="ConsoleKeyInfo.Key"/>
		/// if the first a byte length, otherwise only the KeyChar is considered and searched on the database.
		/// </summary>
		/// <param name="consoleKeyInfo">The console key info.</param>
		/// <returns>The encoded KeyChar with the Key if both can be shifted, otherwise only the KeyChar.</returns>
		/// <remarks>This is useful to use with the <see cref="ConsoleKey.Packet"/>.</remarks>
		public static char EncodeKeyCharForVKPacket (ConsoleKeyInfo consoleKeyInfo)
		{
			char keyChar = consoleKeyInfo.KeyChar;
			ConsoleKey consoleKey = consoleKeyInfo.Key;
			if (keyChar != 0 && consoleKeyInfo.KeyChar < byte.MaxValue && consoleKey == ConsoleKey.None) {
				// try to get the ConsoleKey
				var scode = _scanCodes.FirstOrDefault ((e) => e.UnicodeChar == keyChar);
				if (scode != null) {
					consoleKey = (ConsoleKey)scode.VirtualKey;
				}
			}
			if (keyChar < byte.MaxValue && consoleKey != ConsoleKey.None) {
				keyChar = (char)(consoleKeyInfo.KeyChar << 8 | (byte)consoleKey);
			}

			return keyChar;
		}
	}
}