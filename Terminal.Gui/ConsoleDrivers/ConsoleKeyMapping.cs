using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Terminal.Gui.ConsoleDrivers;
/// <summary>
/// Helper class to handle the scan code and virtual key from a <see cref="ConsoleKey"/>.
/// </summary>
public static class ConsoleKeyMapping {
	class ScanCodeMapping : IEquatable<ScanCodeMapping> {
		public uint ScanCode;
		public VK VirtualKey;
		public ConsoleModifiers Modifiers;
		public uint UnicodeChar;

		public ScanCodeMapping (uint scanCode, VK virtualKey, ConsoleModifiers modifiers, uint unicodeChar)
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
			sCode = _scanCodes.FirstOrDefault ((e) => e.VirtualKey == (VK)keyValue && e.Modifiers == modifiers);
			if (sCode == null && modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control)) {
				return _scanCodes.FirstOrDefault ((e) => e.VirtualKey == (VK)keyValue && e.Modifiers == 0);
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
				sCode = _scanCodes.FirstOrDefault ((e) => e.VirtualKey == (VK)consoleKey);
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
			consoleKey = (uint)scode.VirtualKey;
			keyChar = scode.UnicodeChar;
			scanCode = scode.ScanCode;
		}
		if (scode == null) {
			scode = unicodeChar != 0 ? GetScanCode ("UnicodeChar", decodedChar, mod) : null;
			if (scode != null) {
				consoleKey = (uint)scode.VirtualKey;
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
				consoleKey = (uint)scode.VirtualKey;
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

	/// <summary>
	/// Generated from winuser.h. See https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
	/// </summary>
	internal enum VK : ushort {
		LBUTTON = 0x01,
		RBUTTON = 0x02,
		CANCEL = 0x03,
		MBUTTON = 0x04,
		XBUTTON1 = 0x05,
		XBUTTON2 = 0x06,
		BACK = 0x08,
		TAB = 0x09,
		CLEAR = 0x0C,
		RETURN = 0x0D,
		SHIFT = 0x10,
		CONTROL = 0x11,
		MENU = 0x12,
		PAUSE = 0x13,
		CAPITAL = 0x14,
		KANA = 0x15,
		HANGUL = 0x15,
		JUNJA = 0x17,
		FINAL = 0x18,
		HANJA = 0x19,
		KANJI = 0x19,
		ESCAPE = 0x1B,
		CONVERT = 0x1C,
		NONCONVERT = 0x1D,
		ACCEPT = 0x1E,
		MODECHANGE = 0x1F,
		SPACE = 0x20,
		PRIOR = 0x21,
		NEXT = 0x22,
		END = 0x23,
		HOME = 0x24,
		LEFT = 0x25,
		UP = 0x26,
		RIGHT = 0x27,
		DOWN = 0x28,
		SELECT = 0x29,
		PRINT = 0x2A,
		EXECUTE = 0x2B,
		SNAPSHOT = 0x2C,
		INSERT = 0x2D,
		DELETE = 0x2E,
		HELP = 0x2F,
		LWIN = 0x5B,
		RWIN = 0x5C,
		APPS = 0x5D,
		SLEEP = 0x5F,
		NUMPAD0 = 0x60,
		NUMPAD1 = 0x61,
		NUMPAD2 = 0x62,
		NUMPAD3 = 0x63,
		NUMPAD4 = 0x64,
		NUMPAD5 = 0x65,
		NUMPAD6 = 0x66,
		NUMPAD7 = 0x67,
		NUMPAD8 = 0x68,
		NUMPAD9 = 0x69,
		MULTIPLY = 0x6A,
		ADD = 0x6B,
		SEPARATOR = 0x6C,
		SUBTRACT = 0x6D,
		DECIMAL = 0x6E,
		DIVIDE = 0x6F,
		F1 = 0x70,
		F2 = 0x71,
		F3 = 0x72,
		F4 = 0x73,
		F5 = 0x74,
		F6 = 0x75,
		F7 = 0x76,
		F8 = 0x77,
		F9 = 0x78,
		F10 = 0x79,
		F11 = 0x7A,
		F12 = 0x7B,
		F13 = 0x7C,
		F14 = 0x7D,
		F15 = 0x7E,
		F16 = 0x7F,
		F17 = 0x80,
		F18 = 0x81,
		F19 = 0x82,
		F20 = 0x83,
		F21 = 0x84,
		F22 = 0x85,
		F23 = 0x86,
		F24 = 0x87,
		NUMLOCK = 0x90,
		SCROLL = 0x91,
		OEM_NEC_EQUAL = 0x92,
		OEM_FJ_JISHO = 0x92,
		OEM_FJ_MASSHOU = 0x93,
		OEM_FJ_TOUROKU = 0x94,
		OEM_FJ_LOYA = 0x95,
		OEM_FJ_ROYA = 0x96,
		LSHIFT = 0xA0,
		RSHIFT = 0xA1,
		LCONTROL = 0xA2,
		RCONTROL = 0xA3,
		LMENU = 0xA4,  // L Alt
		RMENU = 0xA5,  // R Alt
		BROWSER_BACK = 0xA6,
		BROWSER_FORWARD = 0xA7,
		BROWSER_REFRESH = 0xA8,
		BROWSER_STOP = 0xA9,
		BROWSER_SEARCH = 0xAA,
		BROWSER_FAVORITES = 0xAB,
		BROWSER_HOME = 0xAC,
		VOLUME_MUTE = 0xAD,
		VOLUME_DOWN = 0xAE,
		VOLUME_UP = 0xAF,
		MEDIA_NEXT_TRACK = 0xB0,
		MEDIA_PREV_TRACK = 0xB1,
		MEDIA_STOP = 0xB2,
		MEDIA_PLAY_PAUSE = 0xB3,
		LAUNCH_MAIL = 0xB4,
		LAUNCH_MEDIA_SELECT = 0xB5,
		LAUNCH_APP1 = 0xB6,
		LAUNCH_APP2 = 0xB7,
		OEM_1 = 0xBA,
		OEM_PLUS = 0xBB,
		OEM_COMMA = 0xBC,
		OEM_MINUS = 0xBD,
		OEM_PERIOD = 0xBE,
		OEM_2 = 0xBF,
		OEM_3 = 0xC0,
		OEM_4 = 0xDB,
		OEM_5 = 0xDC,
		OEM_6 = 0xDD,
		OEM_7 = 0xDE,
		OEM_8 = 0xDF,
		OEM_AX = 0xE1,
		OEM_102 = 0xE2,
		ICO_HELP = 0xE3,
		ICO_00 = 0xE4,
		PROCESSKEY = 0xE5,
		ICO_CLEAR = 0xE6,
		PACKET = 0xE7,
		OEM_RESET = 0xE9,
		OEM_JUMP = 0xEA,
		OEM_PA1 = 0xEB,
		OEM_PA2 = 0xEC,
		OEM_PA3 = 0xED,
		OEM_WSCTRL = 0xEE,
		OEM_CUSEL = 0xEF,
		OEM_ATTN = 0xF0,
		OEM_FINISH = 0xF1,
		OEM_COPY = 0xF2,
		OEM_AUTO = 0xF3,
		OEM_ENLW = 0xF4,
		OEM_BACKTAB = 0xF5,
		ATTN = 0xF6,
		CRSEL = 0xF7,
		EXSEL = 0xF8,
		EREOF = 0xF9,
		PLAY = 0xFA,
		ZOOM = 0xFB,
		NONAME = 0xFC,
		PA1 = 0xFD,
		OEM_CLEAR = 0xFE
	}


	static HashSet<ScanCodeMapping> _scanCodes = new HashSet<ScanCodeMapping> {
		new (1, VK.ESCAPE, 0, '\u001B'), // Escape
		new (1, VK.ESCAPE, ConsoleModifiers.Shift, '\u001B'),
		new (2, (VK)'1', 0, '1'), // D1
		new (2, (VK)'1', ConsoleModifiers.Shift, '!'),
		new (3, (VK)'2', 0, '2'), // D2
		new (3, (VK)'2', ConsoleModifiers.Shift, '\"'), // BUGBUG: This is true for Portugese keyboard, but not ENG (@) or DEU (")
		new (3, (VK)'2', ConsoleModifiers.Alt | ConsoleModifiers.Control, '@'),
		new (4, (VK)'3', 0, '3'), // D3
		new (4, (VK)'3', ConsoleModifiers.Shift, '#'),
		new (4, (VK)'3', ConsoleModifiers.Alt | ConsoleModifiers.Control, '£'),
		new (5, (VK)'4', 0, '4'), // D4
		new (5, (VK)'4', ConsoleModifiers.Shift, '$'),
		new (5, (VK)'4', ConsoleModifiers.Alt | ConsoleModifiers.Control, '§'),
		new (6, (VK)'5', 0, '5'), // D5
		new (6, (VK)'5', ConsoleModifiers.Shift, '%'),
		new (6, (VK)'5', ConsoleModifiers.Alt | ConsoleModifiers.Control, '€'),
		new (7, (VK)'6', 0, '6'), // D6
		new (7, (VK)'6', ConsoleModifiers.Shift, '&'),
		new (8, (VK)'7', 0, '7'), // D7
		new (8, (VK)'7', ConsoleModifiers.Shift, '/'),
		new (8, (VK)'7', ConsoleModifiers.Alt | ConsoleModifiers.Control, '{'),
		new (9, (VK)'8', 0, '8'), // D8
		new (9, (VK)'8', ConsoleModifiers.Shift, '('),
		new (9, (VK)'8', ConsoleModifiers.Alt | ConsoleModifiers.Control, '['),
		new (10, (VK)'9', 0, '9'), // D9
		new (10, (VK)'9', ConsoleModifiers.Shift, ')'),
		new (10, (VK)'9', ConsoleModifiers.Alt | ConsoleModifiers.Control, ']'),
		new (11, (VK)'0', 0, '0'), // D0
		new (11, (VK)'0', ConsoleModifiers.Shift, '='),
		new (11, (VK)'0', ConsoleModifiers.Alt | ConsoleModifiers.Control, '}'),
		new (12, VK.OEM_4, 0, '\''), // Oem4
		new (12, VK.OEM_4, ConsoleModifiers.Shift, '?'),
		new (13, VK.OEM_6, 0, '+'), // Oem6
		new (13, VK.OEM_6, ConsoleModifiers.Shift, '*'),
		new (14, VK.BACK, 0, '\u0008'), // Backspace
		new (14, VK.BACK, ConsoleModifiers.Shift, '\u0008'),
		new (15, VK.TAB, 0, '\u0009'), // Tab
		new (15, VK.TAB, ConsoleModifiers.Shift, '\u000F'),
		new (16, (VK)'Q', 0, 'q'), // Q
		new (16, (VK)'Q', ConsoleModifiers.Shift, 'Q'),
		new (17, (VK)'W', 0, 'w'), // W
		new (17, (VK)'W', ConsoleModifiers.Shift, 'W'),
		new (18, (VK)'E', 0, 'e'), // E
		new (18, (VK)'E', ConsoleModifiers.Shift, 'E'),
		new (19, (VK)'R', 0, 'r'), // R
		new (19, (VK)'R', ConsoleModifiers.Shift, 'R'),
		new (20, (VK)'T', 0, 't'), // T
		new (20, (VK)'T', ConsoleModifiers.Shift, 'T'),
		new (21, (VK)'Y', 0, 'y'), // Y
		new (21, (VK)'Y', ConsoleModifiers.Shift, 'Y'),
		new (22, (VK)'U', 0, 'u'), // U
		new (22, (VK)'U', ConsoleModifiers.Shift, 'U'),
		new (23, (VK)'I', 0, 'i'), // I
		new (23, (VK)'I', ConsoleModifiers.Shift, 'I'),
		new (24, (VK)'O', 0, 'o'), // O
		new (24, (VK)'O', ConsoleModifiers.Shift, 'O'),
		new (25, (VK)'P', 0, 'p'), // P
		new (25, (VK)'P', ConsoleModifiers.Shift, 'P'),
		new (26, VK.OEM_PLUS, 0, '+'), // OemPlus
		new (26, VK.OEM_PLUS, ConsoleModifiers.Shift, '*'),
		new (26, VK.OEM_PLUS, ConsoleModifiers.Alt | ConsoleModifiers.Control, '¨'),
		new (27, VK.OEM_1, 0, '´'), // Oem1
		new (27, VK.OEM_1, ConsoleModifiers.Shift, '`'),
		new (28, VK.RETURN, 0, '\u000D'), // Enter
		new (28, VK.RETURN, ConsoleModifiers.Shift, '\u000D'),
		new (29, VK.CONTROL, 0, '\0'), // Control
		new (29, VK.CONTROL, ConsoleModifiers.Shift, '\0'),
		new (30, (VK)'A', 0, 'a'), // A
		new (30, (VK)'A', ConsoleModifiers.Shift, 'A'),
		new (31, (VK)'S', 0, 's'), // S
		new (31, (VK)'S', ConsoleModifiers.Shift, 'S'),
		new (32, (VK)'D', 0, 'd'), // D
		new (32, (VK)'D', ConsoleModifiers.Shift, 'D'),
		new (33, (VK)'F', 0, 'f'), // F
		new (33, (VK)'F', ConsoleModifiers.Shift, 'F'),
		new (34, (VK)'G', 0, 'g'), // G
		new (34, (VK)'G', ConsoleModifiers.Shift, 'G'),
		new (35, (VK)'H', 0, 'h'), // H
		new (35, (VK)'H', ConsoleModifiers.Shift, 'H'),
		new (36, (VK)'J', 0, 'j'), // J
		new (36, (VK)'J', ConsoleModifiers.Shift, 'J'),
		new (37, (VK)'K', 0, 'k'), // K
		new (37, (VK)'K', ConsoleModifiers.Shift, 'K'),
		new (38, (VK)'L', 0, 'l'), // L
		new (38, (VK)'L', ConsoleModifiers.Shift, 'L'),
		new (39, VK.OEM_3, 0, '`'), // Oem3 (Backtick/Grave)
		new (39, VK.OEM_3, ConsoleModifiers.Shift, '~'),
		new (40, VK.OEM_7, 0, '\''), // Oem7 (Single Quote)
		new (40, VK.OEM_7, ConsoleModifiers.Shift, '\"'),
		new (41, VK.OEM_5, 0, '\\'), // Oem5 (Backslash)
		new (41, VK.OEM_5, ConsoleModifiers.Shift, '|'),
		new (42, VK.LSHIFT, 0, '\0'), // Left Shift
		new (42, VK.LSHIFT, ConsoleModifiers.Shift, '\0'),
		new (43, VK.OEM_2, 0, '/'), // Oem2 (Forward Slash)
		new (43, VK.OEM_2, ConsoleModifiers.Shift, '?'),
		new (44, (VK)'Z', 0, 'z'), // Z
		new (44, (VK)'Z', ConsoleModifiers.Shift, 'Z'),
		new (45, (VK)'X', 0, 'x'), // X
		new (45, (VK)'X', ConsoleModifiers.Shift, 'X'),
		new (46, (VK)'C', 0, 'c'), // C
		new (46, (VK)'C', ConsoleModifiers.Shift, 'C'),
		new (47, (VK)'V', 0, 'v'), // V
		new (47, (VK)'V', ConsoleModifiers.Shift, 'V'),
		new (48, (VK)'B', 0, 'b'), // B
		new (48, (VK)'B', ConsoleModifiers.Shift, 'B'),
		new (49, (VK)'N', 0, 'n'), // N
		new (49, (VK)'N', ConsoleModifiers.Shift, 'N'),
		new (50, (VK)'M', 0, 'm'), // M
		new (50, (VK)'M', ConsoleModifiers.Shift, 'M'),
		new (51, VK.OEM_COMMA, 0, ','), // OemComma
		new (51, VK.OEM_COMMA, ConsoleModifiers.Shift, '<'),
		new (52, VK.OEM_PERIOD, 0, '.'), // OemPeriod
		new (52, VK.OEM_PERIOD, ConsoleModifiers.Shift, '>'),
		new (53, VK.OEM_MINUS, 0, '-'), // OemMinus
		new (53, VK.OEM_MINUS, ConsoleModifiers.Shift, '_'),
		new (54, VK.RSHIFT, 0, '\0'), // Right Shift
		new (54, VK.RSHIFT, ConsoleModifiers.Shift, '\0'),
		new (55, VK.PRINT, 0, '\0'), // Print Screen
		new (55, VK.PRINT, ConsoleModifiers.Shift, '\0'),
		new (56, VK.LMENU, 0, '\0'), // Alt
		new (56, VK.LMENU, ConsoleModifiers.Shift, '\0'),
		new (57, VK.SPACE, 0, ' '), // Spacebar
		new (57, VK.SPACE, ConsoleModifiers.Shift, ' '),
		new (58, VK.CAPITAL, 0, '\0'), // Caps Lock
		new (58, VK.CAPITAL, ConsoleModifiers.Shift, '\0'),
		new (59, VK.F1, 0, '\0'), // F1
		new (59, VK.F1, ConsoleModifiers.Shift, '\0'),
		new (60, VK.F2, 0, '\0'), // F2
		new (60, VK.F2, ConsoleModifiers.Shift, '\0'),
		new (61, VK.F3, 0, '\0'), // F3
		new (61, VK.F3, ConsoleModifiers.Shift, '\0'),
		new (62, VK.F4, 0, '\0'), // F4
		new (62, VK.F4, ConsoleModifiers.Shift, '\0'),
		new (63, VK.F5, 0, '\0'), // F5
		new (63, VK.F5, ConsoleModifiers.Shift, '\0'),
		new (64, VK.F6, 0, '\0'), // F6
		new (64, VK.F6, ConsoleModifiers.Shift, '\0'),
		new (65, VK.F7, 0, '\0'), // F7
		new (65, VK.F7, ConsoleModifiers.Shift, '\0'),
		new (66, VK.F8, 0, '\0'), // F8
		new (66, VK.F8, ConsoleModifiers.Shift, '\0'),
		new (67, VK.F9, 0, '\0'), // F9
		new (67, VK.F9, ConsoleModifiers.Shift, '\0'),
		new (68, VK.F10, 0, '\0'), // F10
		new (68, VK.F10, ConsoleModifiers.Shift, '\0'),
		new (69, VK.NUMLOCK, 0, '\0'), // Num Lock
		new (69, VK.NUMLOCK, ConsoleModifiers.Shift, '\0'),
		new (70, VK.SCROLL, 0, '\0'), // Scroll Lock
		new (70, VK.SCROLL, ConsoleModifiers.Shift, '\0'),
		new (71, VK.HOME, 0, '\0'), // Home
		new (71, VK.HOME, ConsoleModifiers.Shift, '\0'),
		new (72, VK.UP, 0, '\0'), // Up Arrow
		new (72, VK.UP, ConsoleModifiers.Shift, '\0'),
		new (73, VK.PRIOR, 0, '\0'), // Page Up
		new (73, VK.PRIOR, ConsoleModifiers.Shift, '\0'),
		new (74, VK.SUBTRACT, 0, '-'), // Subtract (Num Pad '-')
		new (74, VK.SUBTRACT, ConsoleModifiers.Shift, '-'),
		new (75, VK.LEFT, 0, '\0'), // Left Arrow
		new (75, VK.LEFT, ConsoleModifiers.Shift, '\0'),
		new (76, VK.CLEAR, 0, '\0'), // Center key (Num Pad 5 with Num Lock off)
		new (76, VK.CLEAR, ConsoleModifiers.Shift, '\0'),
		new (77, VK.RIGHT, 0, '\0'), // Right Arrow
		new (77, VK.RIGHT, ConsoleModifiers.Shift, '\0'),
		new (78, VK.ADD, 0, '+'), // Add (Num Pad '+')
		new (78, VK.ADD, ConsoleModifiers.Shift, '+'),
		new (79, VK.END, 0, '\0'), // End
		new (79, VK.END, ConsoleModifiers.Shift, '\0'),
		new (80, VK.DOWN, 0, '\0'), // Down Arrow
		new (80, VK.DOWN, ConsoleModifiers.Shift, '\0'),
		new (81, VK.NEXT, 0, '\0'), // Page Down
		new (81, VK.NEXT, ConsoleModifiers.Shift, '\0'),
		new (82, VK.INSERT, 0, '\0'), // Insert
		new (82, VK.INSERT, ConsoleModifiers.Shift, '\0'),
		new (83, VK.DELETE, 0, '\0'), // Delete
		new (83, VK.DELETE, ConsoleModifiers.Shift, '\0'),
		new (86, VK.OEM_102, 0, '<'), // OEM 102 (Typically '<' or '|' key next to Left Shift)
		new (86, VK.OEM_102, ConsoleModifiers.Shift, '>'),
		new (87, VK.F11, 0, '\0'), // F11
		new (87, VK.F11, ConsoleModifiers.Shift, '\0'),
		new (88, VK.F12, 0, '\0'), // F12
		new (88, VK.F12, ConsoleModifiers.Shift, '\0')
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
