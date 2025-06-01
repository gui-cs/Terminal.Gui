using System.Globalization;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

// QUESTION: This class combines Windows specific code with cross-platform code. Should this be split into two classes?
/// <summary>Helper class to handle the scan code and virtual key from a <see cref="ConsoleKey"/>.</summary>
public static class ConsoleKeyMapping
{
#if !WT_ISSUE_8871_FIXED // https://github.com/microsoft/terminal/issues/8871
    /// <summary>
    ///     Translates (maps) a virtual-key code into a scan code or character value, or translates a scan code into a
    ///     virtual-key code.
    /// </summary>
    /// <param name="vk"></param>
    /// <param name="uMapType">
    ///     If MAPVK_VK_TO_CHAR (2) - The uCode parameter is a virtual-key code and is translated into an
    ///     un-shifted character value in the low order word of the return value.
    /// </param>
    /// <param name="dwhkl"></param>
    /// <returns>
    ///     An un-shifted character value in the low order word of the return value. Dead keys (diacritics) are indicated
    ///     by setting the top bit of the return value. If there is no translation, the function returns 0. See Remarks.
    /// </returns>
    [DllImport ("user32.dll", EntryPoint = "MapVirtualKeyExW", CharSet = CharSet.Unicode)]
    private static extern uint MapVirtualKeyEx (VK vk, uint uMapType, nint dwhkl);

    /// <summary>Retrieves the active input locale identifier (formerly called the keyboard layout).</summary>
    /// <param name="idThread">0 for current thread</param>
    /// <returns>
    ///     The return value is the input locale identifier for the thread. The low word contains a Language Identifier
    ///     for the input language and the high word contains a device handle to the physical layout of the keyboard.
    /// </returns>
    [DllImport ("user32.dll", EntryPoint = "GetKeyboardLayout", CharSet = CharSet.Unicode)]
    private static extern nint GetKeyboardLayout (nint idThread);

    //[DllImport ("user32.dll", EntryPoint = "GetKeyboardLayoutNameW", CharSet = CharSet.Unicode)]
    //extern static uint GetKeyboardLayoutName (uint idThread);
    [DllImport ("user32.dll")]
    private static extern nint GetForegroundWindow ();

    [DllImport ("user32.dll")]
    private static extern nint GetWindowThreadProcessId (nint hWnd, nint ProcessId);

    /// <summary>
    ///     Translates the specified virtual-key code and keyboard state to the corresponding Unicode character or
    ///     characters using the Win32 API MapVirtualKey.
    /// </summary>
    /// <param name="vk"></param>
    /// <returns>
    ///     An un-shifted character value in the low order word of the return value. Dead keys (diacritics) are indicated
    ///     by setting the top bit of the return value. If there is no translation, the function returns 0.
    /// </returns>
    public static uint MapVKtoChar (VK vk)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            return 0;
        }

        nint tid = GetWindowThreadProcessId (GetForegroundWindow (), 0);
        nint hkl = GetKeyboardLayout (tid);

        return MapVirtualKeyEx (vk, 2, hkl);
    }
#else
    /// <summary>
    /// Translates (maps) a virtual-key code into a scan code or character value, or translates a scan code into a virtual-key code.
    /// </summary>
    /// <param name="vk"></param>
    /// <param name="uMapType">
    /// If MAPVK_VK_TO_CHAR (2) - The uCode parameter is a virtual-key code and is translated into an unshifted
    /// character value in the low order word of the return value. 
    /// </param>
    /// <returns>An unshifted character value in the low order word of the return value. Dead keys (diacritics)
    /// are indicated by setting the top bit of the return value. If there is no translation,
    /// the function returns 0. See Remarks.</returns>
    [DllImport ("user32.dll", EntryPoint = "MapVirtualKeyW", CharSet = CharSet.Unicode)]
    extern static uint MapVirtualKey (VK vk, uint uMapType = 2);

    uint MapVKtoChar (VK vk) => MapVirtualKeyToCharEx (vk);
#endif
    /// <summary>
    ///     Retrieves the name of the active input locale identifier (formerly called the keyboard layout) for the calling
    ///     thread.
    /// </summary>
    /// <param name="pwszKLID"></param>
    /// <returns></returns>
    [DllImport ("user32.dll")]
    private static extern bool GetKeyboardLayoutName ([Out] StringBuilder pwszKLID);

    /// <summary>
    ///     Retrieves the name of the active input locale identifier (formerly called the keyboard layout) for the calling
    ///     thread.
    /// </summary>
    /// <returns></returns>
    public static string GetKeyboardLayoutName ()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            return "none";
        }

        var klidSB = new StringBuilder ();
        GetKeyboardLayoutName (klidSB);

        return klidSB.ToString ();
    }

    private class ScanCodeMapping : IEquatable<ScanCodeMapping>
    {
        public readonly ConsoleModifiers Modifiers;
        public readonly uint ScanCode;
        public readonly uint UnicodeChar;
        public readonly VK VirtualKey;

        public ScanCodeMapping (uint scanCode, VK virtualKey, ConsoleModifiers modifiers, uint unicodeChar)
        {
            ScanCode = scanCode;
            VirtualKey = virtualKey;
            Modifiers = modifiers;
            UnicodeChar = unicodeChar;
        }

        public bool Equals (ScanCodeMapping other)
        {
            return ScanCode.Equals (other.ScanCode)
                   && VirtualKey.Equals (other.VirtualKey)
                   && Modifiers.Equals (other.Modifiers)
                   && UnicodeChar.Equals (other.UnicodeChar);
        }
    }

    private static ConsoleModifiers GetModifiers (ConsoleModifiers modifiers)
    {
        if (modifiers.HasFlag (ConsoleModifiers.Shift)
            && !modifiers.HasFlag (ConsoleModifiers.Alt)
            && !modifiers.HasFlag (ConsoleModifiers.Control))
        {
            return ConsoleModifiers.Shift;
        }

        if (modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control))
        {
            return modifiers;
        }

        return 0;
    }

    private static ScanCodeMapping GetScanCode (string propName, uint keyValue, ConsoleModifiers modifiers)
    {
        switch (propName)
        {
            case "UnicodeChar":
                ScanCodeMapping sCode =
                    _scanCodes.FirstOrDefault (e => e.UnicodeChar == keyValue && e.Modifiers == modifiers);

                if (sCode is null && modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control))
                {
                    return _scanCodes.FirstOrDefault (e => e.UnicodeChar == keyValue && e.Modifiers == 0);
                }

                return sCode;
            case "VirtualKey":
                sCode = _scanCodes.FirstOrDefault (e => e.VirtualKey == (VK)keyValue && e.Modifiers == modifiers);

                if (sCode is null && modifiers == (ConsoleModifiers.Alt | ConsoleModifiers.Control))
                {
                    return _scanCodes.FirstOrDefault (e => e.VirtualKey == (VK)keyValue && e.Modifiers == 0);
                }

                return sCode;
        }

        return null;
    }

    // BUGBUG: This API is not correct. It is only used by WindowsDriver in VKPacket scenarios
    /// <summary>Get the scan code from a <see cref="ConsoleKeyInfo"/>.</summary>
    /// <param name="consoleKeyInfo">The console key info.</param>
    /// <returns>The value if apply.</returns>
    public static uint GetScanCodeFromConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
    {
        ConsoleModifiers mod = GetModifiers (consoleKeyInfo.Modifiers);
        ScanCodeMapping scode = GetScanCode ("VirtualKey", (uint)consoleKeyInfo.Key, mod);

        if (scode is { })
        {
            return scode.ScanCode;
        }

        return 0;
    }

    // BUGBUG: This API is not correct. It is only used by FakeDriver and VkeyPacketSimulator
    /// <summary>Gets the <see cref="ConsoleKeyInfo"/> from the provided <see cref="KeyCode"/>.</summary>
    /// <param name="key">The key code.</param>
    /// <returns>The console key info.</returns>
    public static ConsoleKeyInfo GetConsoleKeyInfoFromKeyCode (KeyCode key)
    {
        ConsoleModifiers modifiers = MapToConsoleModifiers (key);
        uint keyValue = MapKeyCodeToConsoleKey (key, out bool isConsoleKey);

        if (isConsoleKey)
        {
            ConsoleModifiers mod = GetModifiers (modifiers);
            ScanCodeMapping scode = GetScanCode ("VirtualKey", keyValue, mod);

            if (scode is { })
            {
                return new ConsoleKeyInfo (
                                           (char)scode.UnicodeChar,
                                           (ConsoleKey)scode.VirtualKey,
                                           modifiers.HasFlag (ConsoleModifiers.Shift),
                                           modifiers.HasFlag (ConsoleModifiers.Alt),
                                           modifiers.HasFlag (ConsoleModifiers.Control)
                                          );
            }
        }
        else
        {
            uint keyChar = GetKeyCharFromUnicodeChar (keyValue, modifiers, out uint consoleKey, out _, isConsoleKey);

            if (consoleKey != 0)
            {
                return new ConsoleKeyInfo (
                                           (char)keyChar,
                                           (ConsoleKey)consoleKey,
                                           modifiers.HasFlag (ConsoleModifiers.Shift),
                                           modifiers.HasFlag (ConsoleModifiers.Alt),
                                           modifiers.HasFlag (ConsoleModifiers.Control)
                                          );
            }
        }

        return new ConsoleKeyInfo (
                                   (char)keyValue,
                                   ConsoleKey.None,
                                   modifiers.HasFlag (ConsoleModifiers.Shift),
                                   modifiers.HasFlag (ConsoleModifiers.Alt),
                                   modifiers.HasFlag (ConsoleModifiers.Control)
                                  );
    }

    /// <summary>Map existing <see cref="KeyCode"/> modifiers to <see cref="ConsoleModifiers"/>.</summary>
    /// <param name="key">The key code.</param>
    /// <returns>The console modifiers.</returns>
    public static ConsoleModifiers MapToConsoleModifiers (KeyCode key)
    {
        var modifiers = new ConsoleModifiers ();

        if (key.HasFlag (KeyCode.ShiftMask) || char.IsUpper ((char)key))
        {
            modifiers |= ConsoleModifiers.Shift;
        }

        if (key.HasFlag (KeyCode.AltMask))
        {
            modifiers |= ConsoleModifiers.Alt;
        }

        if (key.HasFlag (KeyCode.CtrlMask))
        {
            modifiers |= ConsoleModifiers.Control;
        }

        return modifiers;
    }

    /// <summary>Gets <see cref="ConsoleModifiers"/> from <see cref="bool"/> modifiers.</summary>
    /// <param name="shift">The shift key.</param>
    /// <param name="alt">The alt key.</param>
    /// <param name="control">The control key.</param>
    /// <returns>The console modifiers.</returns>
    public static ConsoleModifiers GetModifiers (bool shift, bool alt, bool control)
    {
        var modifiers = new ConsoleModifiers ();

        if (shift)
        {
            modifiers |= ConsoleModifiers.Shift;
        }

        if (alt)
        {
            modifiers |= ConsoleModifiers.Alt;
        }

        if (control)
        {
            modifiers |= ConsoleModifiers.Control;
        }

        return modifiers;
    }

    /// <summary>
    ///     Get the <see cref="ConsoleKeyInfo"/> from a unicode character and modifiers (e.g. (Key)'a' and
    ///     (Key)Key.CtrlMask).
    /// </summary>
    /// <param name="keyValue">The key as a unicode codepoint.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <param name="scanCode">The resulting scan code.</param>
    /// <returns>The <see cref="ConsoleKeyInfo"/>.</returns>
    private static ConsoleKeyInfo GetConsoleKeyInfoFromKeyChar (
        uint keyValue,
        ConsoleModifiers modifiers,
        out uint scanCode
    )
    {
        scanCode = 0;

        if (keyValue == 0)
        {
            return new ConsoleKeyInfo (
                                       (char)keyValue,
                                       ConsoleKey.None,
                                       modifiers.HasFlag (ConsoleModifiers.Shift),
                                       modifiers.HasFlag (ConsoleModifiers.Alt),
                                       modifiers.HasFlag (ConsoleModifiers.Control)
                                      );
        }

        uint outputChar = keyValue;
        uint consoleKey;

        if (keyValue > byte.MaxValue)
        {
            ScanCodeMapping sCode = _scanCodes.FirstOrDefault (e => e.UnicodeChar == keyValue);

            if (sCode is null)
            {
                consoleKey = (byte)(keyValue & byte.MaxValue);
                sCode = _scanCodes.FirstOrDefault (e => e.VirtualKey == (VK)consoleKey);

                if (sCode is null)
                {
                    consoleKey = 0;
                    outputChar = keyValue;
                }
                else
                {
                    outputChar = (char)(keyValue >> 8);
                }
            }
            else
            {
                consoleKey = (byte)sCode.VirtualKey;
                outputChar = keyValue;
            }
        }
        else
        {
            consoleKey = (byte)keyValue;
            outputChar = '\0';
        }

        return new ConsoleKeyInfo (
                                   (char)outputChar,
                                   (ConsoleKey)consoleKey,
                                   modifiers.HasFlag (ConsoleModifiers.Shift),
                                   modifiers.HasFlag (ConsoleModifiers.Alt),
                                   modifiers.HasFlag (ConsoleModifiers.Control)
                                  );
    }

    // Used only by unit tests
    internal static uint GetKeyChar (uint keyValue, ConsoleModifiers modifiers)
    {
        if (modifiers == ConsoleModifiers.Shift && keyValue - 32 is >= 'A' and <= 'Z')
        {
            return keyValue - 32;
        }

        if (modifiers == ConsoleModifiers.None && keyValue is >= 'A' and <= 'Z')
        {
            return keyValue + 32;
        }

        if (modifiers == ConsoleModifiers.Shift && keyValue - 32 is >= 'À' and <= 'Ý')
        {
            return keyValue - 32;
        }

        if (modifiers == ConsoleModifiers.None && keyValue is >= 'À' and <= 'Ý')
        {
            return keyValue + 32;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '0')
        {
            return keyValue + 13;
        }

        if (modifiers == ConsoleModifiers.None && keyValue - 13 is '0')
        {
            return keyValue - 13;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is >= '1' and <= '9' and not '7')
        {
            return keyValue - 16;
        }

        if (modifiers == ConsoleModifiers.None && keyValue + 16 is >= '1' and <= '9' and not '7')
        {
            return keyValue + 16;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '7')
        {
            return keyValue - 8;
        }

        if (modifiers == ConsoleModifiers.None && keyValue + 8 is '7')
        {
            return keyValue + 8;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '\'')
        {
            return keyValue + 24;
        }

        if (modifiers == ConsoleModifiers.None && keyValue - 24 is '\'')
        {
            return keyValue - 24;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '«')
        {
            return keyValue + 16;
        }

        if (modifiers == ConsoleModifiers.None && keyValue - 16 is '«')
        {
            return keyValue - 16;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '\\')
        {
            return keyValue + 32;
        }

        if (modifiers == ConsoleModifiers.None && keyValue - 32 is '\\')
        {
            return keyValue - 32;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '+')
        {
            return keyValue - 1;
        }

        if (modifiers == ConsoleModifiers.None && keyValue + 1 is '+')
        {
            return keyValue + 1;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '´')
        {
            return keyValue - 84;
        }

        if (modifiers == ConsoleModifiers.None && keyValue + 84 is '´')
        {
            return keyValue + 84;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is 'º')
        {
            return keyValue - 16;
        }

        if (modifiers == ConsoleModifiers.None && keyValue + 16 is 'º')
        {
            return keyValue + 16;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '~')
        {
            return keyValue - 32;
        }

        if (modifiers == ConsoleModifiers.None && keyValue + 32 is '~')
        {
            return keyValue + 32;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '<')
        {
            return keyValue + 2;
        }

        if (modifiers == ConsoleModifiers.None && keyValue - 2 is '<')
        {
            return keyValue - 2;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is ',')
        {
            return keyValue + 15;
        }

        if (modifiers == ConsoleModifiers.None && keyValue - 15 is ',')
        {
            return keyValue - 15;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '.')
        {
            return keyValue + 12;
        }

        if (modifiers == ConsoleModifiers.None && keyValue - 12 is '.')
        {
            return keyValue - 12;
        }

        if (modifiers.HasFlag (ConsoleModifiers.Shift) && keyValue is '-')
        {
            return keyValue + 50;
        }

        if (modifiers == ConsoleModifiers.None && keyValue - 50 is '-')
        {
            return keyValue - 50;
        }

        return keyValue;
    }

    /// <summary>
    ///     Get the output character from the <see cref="GetConsoleKeyInfoFromKeyCode"/>, with the correct
    ///     <see cref="ConsoleKey"/> and the scan code used on <see cref="WindowsDriver"/>.
    /// </summary>
    /// <param name="unicodeChar">The unicode character.</param>
    /// <param name="modifiers">The modifiers keys.</param>
    /// <param name="consoleKey">The resulting console key.</param>
    /// <param name="scanCode">The resulting scan code.</param>
    /// <param name="isConsoleKey">Indicates if the <paramref name="unicodeChar"/> is a <see cref="ConsoleKey"/>.</param>
    /// <returns>The output character or the <paramref name="consoleKey"/>.</returns>
    /// <remarks>This is only used by the <see cref="GetConsoleKeyInfoFromKeyCode"/> and by unit tests.</remarks>
    internal static uint GetKeyCharFromUnicodeChar (
        uint unicodeChar,
        ConsoleModifiers modifiers,
        out uint consoleKey,
        out uint scanCode,
        bool isConsoleKey = false
    )
    {
        uint decodedChar = unicodeChar >> 8 == 0xff ? unicodeChar & 0xff : unicodeChar;
        uint keyChar = decodedChar;
        consoleKey = 0;
        ConsoleModifiers mod = GetModifiers (modifiers);
        scanCode = 0;
        ScanCodeMapping scode = null;

        if (unicodeChar != 0 && unicodeChar >> 8 != 0xff && isConsoleKey)
        {
            scode = GetScanCode ("VirtualKey", decodedChar, mod);
        }

        if (isConsoleKey && scode is { })
        {
            consoleKey = (uint)scode.VirtualKey;
            keyChar = scode.UnicodeChar;
            scanCode = scode.ScanCode;
        }

        if (scode is null)
        {
            scode = unicodeChar != 0 ? GetScanCode ("UnicodeChar", decodedChar, mod) : null;

            if (scode is { })
            {
                consoleKey = (uint)scode.VirtualKey;
                keyChar = scode.UnicodeChar;
                scanCode = scode.ScanCode;
            }
        }

        if (decodedChar != 0 && scanCode == 0 && char.IsLetter ((char)decodedChar))
        {
            string stFormD = ((char)decodedChar).ToString ().Normalize (NormalizationForm.FormD);

            for (var i = 0; i < stFormD.Length; i++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory (stFormD [i]);

                if (uc != UnicodeCategory.NonSpacingMark && uc != UnicodeCategory.OtherLetter)
                {
                    char ck = char.ToUpper (stFormD [i]);
                    consoleKey = (uint)(ck > 0 && ck <= 255 ? char.ToUpper (stFormD [i]) : 0);
                    scode = GetScanCode ("VirtualKey", char.ToUpper (stFormD [i]), 0);

                    if (scode is { })
                    {
                        scanCode = scode.ScanCode;
                    }
                }
            }
        }

        if (keyChar < 255 && consoleKey == 0 && scanCode == 0)
        {
            scode = GetScanCode ("VirtualKey", keyChar, mod);

            if (scode is { })
            {
                consoleKey = (uint)scode.VirtualKey;
                keyChar = scode.UnicodeChar;
                scanCode = scode.ScanCode;
            }
        }

        return keyChar;
    }

    /// <summary>Maps a unicode character (e.g. (Key)'a') to a uint representing a <see cref="ConsoleKey"/>.</summary>
    /// <param name="keyValue">The key value.</param>
    /// <param name="isConsoleKey">
    ///     Indicates if the <paramref name="keyValue"/> is a <see cref="ConsoleKey"/>.
    ///     <see langword="true"/> means the return value is in the ConsoleKey enum. <see langword="false"/> means the return
    ///     value can be mapped to a valid unicode character.
    /// </param>
    /// <returns>The <see cref="ConsoleKey"/> or the <paramref name="keyValue"/>.</returns>
    /// <remarks>This is only used by the <see cref="GetConsoleKeyInfoFromKeyCode"/> and by unit tests.</remarks>
    internal static uint MapKeyCodeToConsoleKey (KeyCode keyValue, out bool isConsoleKey)
    {
        isConsoleKey = true;
        keyValue = keyValue & ~KeyCode.CtrlMask & ~KeyCode.ShiftMask & ~KeyCode.AltMask;

        switch (keyValue)
        {
            case KeyCode.Enter:
                return (uint)ConsoleKey.Enter;
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
            case KeyCode.Insert:
                return (uint)ConsoleKey.Insert;
            case KeyCode.Delete:
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
            case KeyCode.Space:
                return (uint)ConsoleKey.Spacebar;
            default:
                uint c = (char)keyValue;

                if (c is >= (char)ConsoleKey.A and <= (char)ConsoleKey.Z)
                {
                    return c;
                }

                if ((c - 32) is >= (char)ConsoleKey.A and <= (char)ConsoleKey.Z)
                {
                    return (c - 32);
                }

                if (Enum.IsDefined (typeof (ConsoleKey), keyValue.ToString ()))
                {
                    return (uint)keyValue;
                }

                // DEL
                if ((uint)keyValue == 127)
                {
                    return (uint)ConsoleKey.Backspace;
                }
                break;
        }

        isConsoleKey = false;

        return (uint)keyValue;
    }

    /// <summary>Maps a <see cref="ConsoleKeyInfo"/> to a <see cref="KeyCode"/>.</summary>
    /// <param name="consoleKeyInfo">The console key.</param>
    /// <returns>The <see cref="KeyCode"/> or the <paramref name="consoleKeyInfo"/>.</returns>
    public static KeyCode MapConsoleKeyInfoToKeyCode (ConsoleKeyInfo consoleKeyInfo)
    {
        KeyCode keyCode;

        switch (consoleKeyInfo.Key)
        {
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
                keyCode = KeyCode.Insert;

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
            case ConsoleKey.Clear:
                keyCode = KeyCode.Clear;

                break;
            case ConsoleKey.Tab:
                keyCode = KeyCode.Tab;

                break;
            case ConsoleKey.Spacebar:
                keyCode = KeyCode.Space;

                break;
            case ConsoleKey.Backspace:
                keyCode = KeyCode.Backspace;

                break;
            default:
                if ((int)consoleKeyInfo.KeyChar is >= 1 and <= 26)
                {
                    keyCode = (KeyCode)(consoleKeyInfo.KeyChar + 64);
                }
                else
                {
                    keyCode = (KeyCode)consoleKeyInfo.KeyChar;
                }

                break;
        }

        keyCode |= MapToKeyCodeModifiers (consoleKeyInfo.Modifiers, keyCode);

        return keyCode;
    }

    /// <summary>Maps a <see cref="ConsoleKeyInfo"/> to a <see cref="KeyCode"/>.</summary>
    /// <param name="modifiers">The console modifiers.</param>
    /// <param name="key">The key code.</param>
    /// <returns>The <see cref="KeyCode"/> with <see cref="ConsoleModifiers"/> or the <paramref name="key"/></returns>
    public static KeyCode MapToKeyCodeModifiers (ConsoleModifiers modifiers, KeyCode key)
    {
        var keyMod = new KeyCode ();

        if ((modifiers & ConsoleModifiers.Shift) != 0)
        {
            keyMod = KeyCode.ShiftMask;
        }

        if ((modifiers & ConsoleModifiers.Control) != 0)
        {
            keyMod |= KeyCode.CtrlMask;
        }

        if ((modifiers & ConsoleModifiers.Alt) != 0)
        {
            keyMod |= KeyCode.AltMask;
        }

        return keyMod != KeyCode.Null ? keyMod | key : key;
    }

    /// <summary>Generated from winuser.h. See https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes</summary>
    public enum VK : ushort
    {
        /// <summary>Left mouse button.</summary>
        LBUTTON = 0x01,

        /// <summary>Right mouse button.</summary>
        RBUTTON = 0x02,

        /// <summary>Control-break processing.</summary>
        CANCEL = 0x03,

        /// <summary>Middle mouse button (three-button mouse).</summary>
        MBUTTON = 0x04,

        /// <summary>X1 mouse button.</summary>
        XBUTTON1 = 0x05,

        /// <summary>X2 mouse button.</summary>
        XBUTTON2 = 0x06,

        /// <summary>BACKSPACE key.</summary>
        BACK = 0x08,

        /// <summary>TAB key.</summary>
        TAB = 0x09,

        /// <summary>CLEAR key.</summary>
        CLEAR = 0x0C,

        /// <summary>ENTER key.</summary>
        RETURN = 0x0D,

        /// <summary>SHIFT key.</summary>
        SHIFT = 0x10,

        /// <summary>CTRL key.</summary>
        CONTROL = 0x11,

        /// <summary>ALT key.</summary>
        MENU = 0x12,

        /// <summary>PAUSE key.</summary>
        PAUSE = 0x13,

        /// <summary>CAPS LOCK key.</summary>
        CAPITAL = 0x14,

        /// <summary>IME Kana mode.</summary>
        KANA = 0x15,

        /// <summary>IME Hangul mode.</summary>
        HANGUL = 0x15,

        /// <summary>IME Junja mode.</summary>
        JUNJA = 0x17,

        /// <summary>IME final mode.</summary>
        FINAL = 0x18,

        /// <summary>IME Hanja mode.</summary>
        HANJA = 0x19,

        /// <summary>IME Kanji mode.</summary>
        KANJI = 0x19,

        /// <summary>ESC key.</summary>
        ESCAPE = 0x1B,

        /// <summary>IME convert.</summary>
        CONVERT = 0x1C,

        /// <summary>IME nonconvert.</summary>
        NONCONVERT = 0x1D,

        /// <summary>IME accept.</summary>
        ACCEPT = 0x1E,

        /// <summary>IME mode change request.</summary>
        MODECHANGE = 0x1F,

        /// <summary>SPACEBAR.</summary>
        SPACE = 0x20,

        /// <summary>PAGE UP key.</summary>
        PRIOR = 0x21,

        /// <summary>PAGE DOWN key.</summary>
        NEXT = 0x22,

        /// <summary>END key.</summary>
        END = 0x23,

        /// <summary>HOME key.</summary>
        HOME = 0x24,

        /// <summary>LEFT ARROW key.</summary>
        LEFT = 0x25,

        /// <summary>UP ARROW key.</summary>
        UP = 0x26,

        /// <summary>RIGHT ARROW key.</summary>
        RIGHT = 0x27,

        /// <summary>DOWN ARROW key.</summary>
        DOWN = 0x28,

        /// <summary>SELECT key.</summary>
        SELECT = 0x29,

        /// <summary>PRINT key.</summary>
        PRINT = 0x2A,

        /// <summary>EXECUTE key</summary>
        EXECUTE = 0x2B,

        /// <summary>PRINT SCREEN key</summary>
        SNAPSHOT = 0x2C,

        /// <summary>INS key</summary>
        INSERT = 0x2D,

        /// <summary>DEL key</summary>
        DELETE = 0x2E,

        /// <summary>HELP key</summary>
        HELP = 0x2F,

        /// <summary>Left Windows key (Natural keyboard)</summary>
        LWIN = 0x5B,

        /// <summary>Right Windows key (Natural keyboard)</summary>
        RWIN = 0x5C,

        /// <summary>Applications key (Natural keyboard)</summary>
        APPS = 0x5D,

        /// <summary>Computer Sleep key</summary>
        SLEEP = 0x5F,

        /// <summary>Numeric keypad 0 key</summary>
        NUMPAD0 = 0x60,

        /// <summary>Numeric keypad 1 key</summary>
        NUMPAD1 = 0x61,

        /// <summary>Numeric keypad 2 key</summary>
        NUMPAD2 = 0x62,

        /// <summary>Numeric keypad 3 key</summary>
        NUMPAD3 = 0x63,

        /// <summary>Numeric keypad 4 key</summary>
        NUMPAD4 = 0x64,

        /// <summary>Numeric keypad 5 key</summary>
        NUMPAD5 = 0x65,

        /// <summary>Numeric keypad 6 key</summary>
        NUMPAD6 = 0x66,

        /// <summary>Numeric keypad 7 key</summary>
        NUMPAD7 = 0x67,

        /// <summary>Numeric keypad 8 key</summary>
        NUMPAD8 = 0x68,

        /// <summary>Numeric keypad 9 key</summary>
        NUMPAD9 = 0x69,

        /// <summary>Multiply key</summary>
        MULTIPLY = 0x6A,

        /// <summary>Add key</summary>
        ADD = 0x6B,

        /// <summary>Separator key</summary>
        SEPARATOR = 0x6C,

        /// <summary>Subtract key</summary>
        SUBTRACT = 0x6D,

        /// <summary>Decimal key</summary>
        DECIMAL = 0x6E,

        /// <summary>Divide key</summary>
        DIVIDE = 0x6F,

        /// <summary>F1 key</summary>
        F1 = 0x70,

        /// <summary>F2 key</summary>
        F2 = 0x71,

        /// <summary>F3 key</summary>
        F3 = 0x72,

        /// <summary>F4 key</summary>
        F4 = 0x73,

        /// <summary>F5 key</summary>
        F5 = 0x74,

        /// <summary>F6 key</summary>
        F6 = 0x75,

        /// <summary>F7 key</summary>
        F7 = 0x76,

        /// <summary>F8 key</summary>
        F8 = 0x77,

        /// <summary>F9 key</summary>
        F9 = 0x78,

        /// <summary>F10 key</summary>
        F10 = 0x79,

        /// <summary>F11 key</summary>
        F11 = 0x7A,

        /// <summary>F12 key</summary>
        F12 = 0x7B,

        /// <summary>F13 key</summary>
        F13 = 0x7C,

        /// <summary>F14 key</summary>
        F14 = 0x7D,

        /// <summary>F15 key</summary>
        F15 = 0x7E,

        /// <summary>F16 key</summary>
        F16 = 0x7F,

        /// <summary>F17 key</summary>
        F17 = 0x80,

        /// <summary>F18 key</summary>
        F18 = 0x81,

        /// <summary>F19 key</summary>
        F19 = 0x82,

        /// <summary>F20 key</summary>
        F20 = 0x83,

        /// <summary>F21 key</summary>
        F21 = 0x84,

        /// <summary>F22 key</summary>
        F22 = 0x85,

        /// <summary>F23 key</summary>
        F23 = 0x86,

        /// <summary>F24 key</summary>
        F24 = 0x87,

        /// <summary>NUM LOCK key</summary>
        NUMLOCK = 0x90,

        /// <summary>SCROLL LOCK key</summary>
        SCROLL = 0x91,

        /// <summary>NEC PC-9800 kbd definition: '=' key on numpad</summary>
        OEM_NEC_EQUAL = 0x92,

        /// <summary>Fujitsu/OASYS kbd definition: 'Dictionary' key</summary>
        OEM_FJ_JISHO = 0x92,

        /// <summary>Fujitsu/OASYS kbd definition: 'Unregister word' key</summary>
        OEM_FJ_MASSHOU = 0x93,

        /// <summary>Fujitsu/OASYS kbd definition: 'Register word' key</summary>
        OEM_FJ_TOUROKU = 0x94,

        /// <summary>Fujitsu/OASYS kbd definition: 'Left OYAYUBI' key</summary>
        OEM_FJ_LOYA = 0x95,

        /// <summary>Fujitsu/OASYS kbd definition: 'Right OYAYUBI' key</summary>
        OEM_FJ_ROYA = 0x96,

        /// <summary>Left SHIFT key</summary>
        LSHIFT = 0xA0,

        /// <summary>Right SHIFT key</summary>
        RSHIFT = 0xA1,

        /// <summary>Left CONTROL key</summary>
        LCONTROL = 0xA2,

        /// <summary>Right CONTROL key</summary>
        RCONTROL = 0xA3,

        /// <summary>Left MENU key (Left Alt key)</summary>
        LMENU = 0xA4,

        /// <summary>Right MENU key (Right Alt key)</summary>
        RMENU = 0xA5,

        /// <summary>Browser Back key</summary>
        BROWSER_BACK = 0xA6,

        /// <summary>Browser Forward key</summary>
        BROWSER_FORWARD = 0xA7,

        /// <summary>Browser Refresh key</summary>
        BROWSER_REFRESH = 0xA8,

        /// <summary>Browser Stop key</summary>
        BROWSER_STOP = 0xA9,

        /// <summary>Browser Search key</summary>
        BROWSER_SEARCH = 0xAA,

        /// <summary>Browser Favorites key</summary>
        BROWSER_FAVORITES = 0xAB,

        /// <summary>Browser Home key</summary>
        BROWSER_HOME = 0xAC,

        /// <summary>Volume Mute key</summary>
        VOLUME_MUTE = 0xAD,

        /// <summary>Volume Down key</summary>
        VOLUME_DOWN = 0xAE,

        /// <summary>Volume Up key</summary>
        VOLUME_UP = 0xAF,

        /// <summary>Next Track key</summary>
        MEDIA_NEXT_TRACK = 0xB0,

        /// <summary>Previous Track key</summary>
        MEDIA_PREV_TRACK = 0xB1,

        /// <summary>Stop Media key</summary>
        MEDIA_STOP = 0xB2,

        /// <summary>Play/Pause Media key</summary>
        MEDIA_PLAY_PAUSE = 0xB3,

        /// <summary>Start Mail key</summary>
        LAUNCH_MAIL = 0xB4,

        /// <summary>Select Media key</summary>
        LAUNCH_MEDIA_SELECT = 0xB5,

        /// <summary>Start Application 1 key</summary>
        LAUNCH_APP1 = 0xB6,

        /// <summary>Start Application 2 key</summary>
        LAUNCH_APP2 = 0xB7,

        /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ';:' key</summary>
        OEM_1 = 0xBA,

        /// <summary>For any country/region, the '+' key</summary>
        OEM_PLUS = 0xBB,

        /// <summary>For any country/region, the ',' key</summary>
        OEM_COMMA = 0xBC,

        /// <summary>For any country/region, the '-' key</summary>
        OEM_MINUS = 0xBD,

        /// <summary>For any country/region, the '.' key</summary>
        OEM_PERIOD = 0xBE,

        /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '/?' key</summary>
        OEM_2 = 0xBF,

        /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '`~' key</summary>
        OEM_3 = 0xC0,

        /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '[{' key</summary>
        OEM_4 = 0xDB,

        /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the '\|' key</summary>
        OEM_5 = 0xDC,

        /// <summary>Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the ']}' key</summary>
        OEM_6 = 0xDD,

        /// <summary>
        ///     Used for miscellaneous characters; it can vary by keyboard. For the US standard keyboard, the
        ///     'single-quote/double-quote' key
        /// </summary>
        OEM_7 = 0xDE,

        /// <summary>Used for miscellaneous characters; it can vary by keyboard.</summary>
        OEM_8 = 0xDF,

        /// <summary>'AX' key on Japanese AX kbd</summary>
        OEM_AX = 0xE1,

        /// <summary>Either the angle bracket key or the backslash key on the RT 102-key keyboard</summary>
        OEM_102 = 0xE2,

        /// <summary>Help key on ICO</summary>
        ICO_HELP = 0xE3,

        /// <summary>00 key on ICO</summary>
        ICO_00 = 0xE4,

        /// <summary>Process key</summary>
        PROCESSKEY = 0xE5,

        /// <summary>Clear key on ICO</summary>
        ICO_CLEAR = 0xE6,

        /// <summary>Packet key to be used to pass Unicode characters as if they were keystrokes</summary>
        PACKET = 0xE7,

        /// <summary>Reset key</summary>
        OEM_RESET = 0xE9,

        /// <summary>Jump key</summary>
        OEM_JUMP = 0xEA,

        /// <summary>PA1 key</summary>
        OEM_PA1 = 0xEB,

        /// <summary>PA2 key</summary>
        OEM_PA2 = 0xEC,

        /// <summary>PA3 key</summary>
        OEM_PA3 = 0xED,

        /// <summary>WsCtrl key</summary>
        OEM_WSCTRL = 0xEE,

        /// <summary>CuSel key</summary>
        OEM_CUSEL = 0xEF,

        /// <summary>Attn key</summary>
        OEM_ATTN = 0xF0,

        /// <summary>Finish key</summary>
        OEM_FINISH = 0xF1,

        /// <summary>Copy key</summary>
        OEM_COPY = 0xF2,

        /// <summary>Auto key</summary>
        OEM_AUTO = 0xF3,

        /// <summary>Enlw key</summary>
        OEM_ENLW = 0xF4,

        /// <summary>BackTab key</summary>
        OEM_BACKTAB = 0xF5,

        /// <summary>Attn key</summary>
        ATTN = 0xF6,

        /// <summary>CrSel key</summary>
        CRSEL = 0xF7,

        /// <summary>ExSel key</summary>
        EXSEL = 0xF8,

        /// <summary>Erase EOF key</summary>
        EREOF = 0xF9,

        /// <summary>Play key</summary>
        PLAY = 0xFA,

        /// <summary>Zoom key</summary>
        ZOOM = 0xFB,

        /// <summary>Reserved</summary>
        NONAME = 0xFC,

        /// <summary>PA1 key</summary>
        PA1 = 0xFD,

        /// <summary>Clear key</summary>
        OEM_CLEAR = 0xFE
    }

    // BUGBUG: This database makes no sense. It is not possible to map a VK code to a character without knowing the keyboard layout
    //         It should be deleted.
    private static readonly HashSet<ScanCodeMapping> _scanCodes = new ()
    {
        new ScanCodeMapping (
                             1,
                             VK.ESCAPE,
                             0,
                             '\u001B'
                            ), // Escape
        new ScanCodeMapping (
                             1,
                             VK.ESCAPE,
                             ConsoleModifiers.Shift,
                             '\u001B'
                            ),
        new ScanCodeMapping (
                             2,
                             (VK)'1',
                             0,
                             '1'
                            ), // D1
        new ScanCodeMapping (
                             2,
                             (VK)'1',
                             ConsoleModifiers.Shift,
                             '!'
                            ),
        new ScanCodeMapping (
                             3,
                             (VK)'2',
                             0,
                             '2'
                            ), // D2
        new ScanCodeMapping (
                             3,
                             (VK)'2',
                             ConsoleModifiers.Shift,
                             '\"'
                            ), // BUGBUG: This is true for Portuguese keyboard, but not ENG (@) or DEU (")
        new ScanCodeMapping (
                             3,
                             (VK)'2',
                             ConsoleModifiers.Alt
                             | ConsoleModifiers.Control,
                             '@'
                            ),
        new ScanCodeMapping (
                             4,
                             (VK)'3',
                             0,
                             '3'
                            ), // D3
        new ScanCodeMapping (
                             4,
                             (VK)'3',
                             ConsoleModifiers.Shift,
                             '#'
                            ),
        new ScanCodeMapping (
                             4,
                             (VK)'3',
                             ConsoleModifiers.Alt
                             | ConsoleModifiers.Control,
                             '£'
                            ),
        new ScanCodeMapping (
                             5,
                             (VK)'4',
                             0,
                             '4'
                            ), // D4
        new ScanCodeMapping (
                             5,
                             (VK)'4',
                             ConsoleModifiers.Shift,
                             '$'
                            ),
        new ScanCodeMapping (
                             5,
                             (VK)'4',
                             ConsoleModifiers.Alt
                             | ConsoleModifiers.Control,
                             '§'
                            ),
        new ScanCodeMapping (
                             6,
                             (VK)'5',
                             0,
                             '5'
                            ), // D5
        new ScanCodeMapping (
                             6,
                             (VK)'5',
                             ConsoleModifiers.Shift,
                             '%'
                            ),
        new ScanCodeMapping (
                             6,
                             (VK)'5',
                             ConsoleModifiers.Alt
                             | ConsoleModifiers.Control,
                             '€'
                            ),
        new ScanCodeMapping (
                             7,
                             (VK)'6',
                             0,
                             '6'
                            ), // D6
        new ScanCodeMapping (
                             7,
                             (VK)'6',
                             ConsoleModifiers.Shift,
                             '&'
                            ),
        new ScanCodeMapping (
                             8,
                             (VK)'7',
                             0,
                             '7'
                            ), // D7
        new ScanCodeMapping (
                             8,
                             (VK)'7',
                             ConsoleModifiers.Shift,
                             '/'
                            ),
        new ScanCodeMapping (
                             8,
                             (VK)'7',
                             ConsoleModifiers.Alt
                             | ConsoleModifiers.Control,
                             '{'
                            ),
        new ScanCodeMapping (
                             9,
                             (VK)'8',
                             0,
                             '8'
                            ), // D8
        new ScanCodeMapping (
                             9,
                             (VK)'8',
                             ConsoleModifiers.Shift,
                             '('
                            ),
        new ScanCodeMapping (
                             9,
                             (VK)'8',
                             ConsoleModifiers.Alt
                             | ConsoleModifiers.Control,
                             '['
                            ),
        new ScanCodeMapping (
                             10,
                             (VK)'9',
                             0,
                             '9'
                            ), // D9
        new ScanCodeMapping (
                             10,
                             (VK)'9',
                             ConsoleModifiers.Shift,
                             ')'
                            ),
        new ScanCodeMapping (
                             10,
                             (VK)'9',
                             ConsoleModifiers.Alt
                             | ConsoleModifiers.Control,
                             ']'
                            ),
        new ScanCodeMapping (
                             11,
                             (VK)'0',
                             0,
                             '0'
                            ), // D0
        new ScanCodeMapping (
                             11,
                             (VK)'0',
                             ConsoleModifiers.Shift,
                             '='
                            ),
        new ScanCodeMapping (
                             11,
                             (VK)'0',
                             ConsoleModifiers.Alt
                             | ConsoleModifiers.Control,
                             '}'
                            ),
        new ScanCodeMapping (
                             12,
                             VK.OEM_4,
                             0,
                             '\''
                            ), // Oem4
        new ScanCodeMapping (
                             12,
                             VK.OEM_4,
                             ConsoleModifiers.Shift,
                             '?'
                            ),
        new ScanCodeMapping (
                             13,
                             VK.OEM_6,
                             0,
                             '+'
                            ), // Oem6
        new ScanCodeMapping (
                             13,
                             VK.OEM_6,
                             ConsoleModifiers.Shift,
                             '*'
                            ),
        new ScanCodeMapping (
                             14,
                             VK.BACK,
                             0,
                             '\u0008'
                            ), // Backspace
        new ScanCodeMapping (
                             14,
                             VK.BACK,
                             ConsoleModifiers.Shift,
                             '\u0008'
                            ),
        new ScanCodeMapping (
                             15,
                             VK.TAB,
                             0,
                             '\u0009'
                            ), // Tab
        new ScanCodeMapping (
                             15,
                             VK.TAB,
                             ConsoleModifiers.Shift,
                             '\u000F'
                            ),
        new ScanCodeMapping (
                             16,
                             (VK)'Q',
                             0,
                             'q'
                            ), // Q
        new ScanCodeMapping (
                             16,
                             (VK)'Q',
                             ConsoleModifiers.Shift,
                             'Q'
                            ),
        new ScanCodeMapping (
                             17,
                             (VK)'W',
                             0,
                             'w'
                            ), // W
        new ScanCodeMapping (
                             17,
                             (VK)'W',
                             ConsoleModifiers.Shift,
                             'W'
                            ),
        new ScanCodeMapping (
                             18,
                             (VK)'E',
                             0,
                             'e'
                            ), // E
        new ScanCodeMapping (
                             18,
                             (VK)'E',
                             ConsoleModifiers.Shift,
                             'E'
                            ),
        new ScanCodeMapping (
                             19,
                             (VK)'R',
                             0,
                             'r'
                            ), // R
        new ScanCodeMapping (
                             19,
                             (VK)'R',
                             ConsoleModifiers.Shift,
                             'R'
                            ),
        new ScanCodeMapping (
                             20,
                             (VK)'T',
                             0,
                             't'
                            ), // T
        new ScanCodeMapping (
                             20,
                             (VK)'T',
                             ConsoleModifiers.Shift,
                             'T'
                            ),
        new ScanCodeMapping (
                             21,
                             (VK)'Y',
                             0,
                             'y'
                            ), // Y
        new ScanCodeMapping (
                             21,
                             (VK)'Y',
                             ConsoleModifiers.Shift,
                             'Y'
                            ),
        new ScanCodeMapping (
                             22,
                             (VK)'U',
                             0,
                             'u'
                            ), // U
        new ScanCodeMapping (
                             22,
                             (VK)'U',
                             ConsoleModifiers.Shift,
                             'U'
                            ),
        new ScanCodeMapping (
                             23,
                             (VK)'I',
                             0,
                             'i'
                            ), // I
        new ScanCodeMapping (
                             23,
                             (VK)'I',
                             ConsoleModifiers.Shift,
                             'I'
                            ),
        new ScanCodeMapping (
                             24,
                             (VK)'O',
                             0,
                             'o'
                            ), // O
        new ScanCodeMapping (
                             24,
                             (VK)'O',
                             ConsoleModifiers.Shift,
                             'O'
                            ),
        new ScanCodeMapping (
                             25,
                             (VK)'P',
                             0,
                             'p'
                            ), // P
        new ScanCodeMapping (
                             25,
                             (VK)'P',
                             ConsoleModifiers.Shift,
                             'P'
                            ),
        new ScanCodeMapping (
                             26,
                             VK.OEM_PLUS,
                             0,
                             '+'
                            ), // OemPlus
        new ScanCodeMapping (
                             26,
                             VK.OEM_PLUS,
                             ConsoleModifiers.Shift,
                             '*'
                            ),
        new ScanCodeMapping (
                             26,
                             VK.OEM_PLUS,
                             ConsoleModifiers.Alt
                             | ConsoleModifiers.Control,
                             '¨'
                            ),
        new ScanCodeMapping (
                             27,
                             VK.OEM_1,
                             0,
                             '´'
                            ), // Oem1
        new ScanCodeMapping (
                             27,
                             VK.OEM_1,
                             ConsoleModifiers.Shift,
                             '`'
                            ),
        new ScanCodeMapping (
                             28,
                             VK.RETURN,
                             0,
                             '\u000D'
                            ), // Enter
        new ScanCodeMapping (
                             28,
                             VK.RETURN,
                             ConsoleModifiers.Shift,
                             '\u000D'
                            ),
        new ScanCodeMapping (
                             29,
                             VK.CONTROL,
                             0,
                             '\0'
                            ), // Control
        new ScanCodeMapping (
                             29,
                             VK.CONTROL,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             30,
                             (VK)'A',
                             0,
                             'a'
                            ), // A
        new ScanCodeMapping (
                             30,
                             (VK)'A',
                             ConsoleModifiers.Shift,
                             'A'
                            ),
        new ScanCodeMapping (
                             31,
                             (VK)'S',
                             0,
                             's'
                            ), // S
        new ScanCodeMapping (
                             31,
                             (VK)'S',
                             ConsoleModifiers.Shift,
                             'S'
                            ),
        new ScanCodeMapping (
                             32,
                             (VK)'D',
                             0,
                             'd'
                            ), // D
        new ScanCodeMapping (
                             32,
                             (VK)'D',
                             ConsoleModifiers.Shift,
                             'D'
                            ),
        new ScanCodeMapping (
                             33,
                             (VK)'F',
                             0,
                             'f'
                            ), // F
        new ScanCodeMapping (
                             33,
                             (VK)'F',
                             ConsoleModifiers.Shift,
                             'F'
                            ),
        new ScanCodeMapping (
                             34,
                             (VK)'G',
                             0,
                             'g'
                            ), // G
        new ScanCodeMapping (
                             34,
                             (VK)'G',
                             ConsoleModifiers.Shift,
                             'G'
                            ),
        new ScanCodeMapping (
                             35,
                             (VK)'H',
                             0,
                             'h'
                            ), // H
        new ScanCodeMapping (
                             35,
                             (VK)'H',
                             ConsoleModifiers.Shift,
                             'H'
                            ),
        new ScanCodeMapping (
                             36,
                             (VK)'J',
                             0,
                             'j'
                            ), // J
        new ScanCodeMapping (
                             36,
                             (VK)'J',
                             ConsoleModifiers.Shift,
                             'J'
                            ),
        new ScanCodeMapping (
                             37,
                             (VK)'K',
                             0,
                             'k'
                            ), // K
        new ScanCodeMapping (
                             37,
                             (VK)'K',
                             ConsoleModifiers.Shift,
                             'K'
                            ),
        new ScanCodeMapping (
                             38,
                             (VK)'L',
                             0,
                             'l'
                            ), // L
        new ScanCodeMapping (
                             38,
                             (VK)'L',
                             ConsoleModifiers.Shift,
                             'L'
                            ),
        new ScanCodeMapping (
                             39,
                             VK.OEM_3,
                             0,
                             '`'
                            ), // Oem3 (Backtick/Grave)
        new ScanCodeMapping (
                             39,
                             VK.OEM_3,
                             ConsoleModifiers.Shift,
                             '~'
                            ),
        new ScanCodeMapping (
                             40,
                             VK.OEM_7,
                             0,
                             '\''
                            ), // Oem7 (Single Quote)
        new ScanCodeMapping (
                             40,
                             VK.OEM_7,
                             ConsoleModifiers.Shift,
                             '\"'
                            ),
        new ScanCodeMapping (
                             41,
                             VK.OEM_5,
                             0,
                             '\\'
                            ), // Oem5 (Backslash)
        new ScanCodeMapping (
                             41,
                             VK.OEM_5,
                             ConsoleModifiers.Shift,
                             '|'
                            ),
        new ScanCodeMapping (
                             42,
                             VK.LSHIFT,
                             0,
                             '\0'
                            ), // Left Shift
        new ScanCodeMapping (
                             42,
                             VK.LSHIFT,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             43,
                             VK.OEM_2,
                             0,
                             '/'
                            ), // Oem2 (Forward Slash)
        new ScanCodeMapping (
                             43,
                             VK.OEM_2,
                             ConsoleModifiers.Shift,
                             '?'
                            ),
        new ScanCodeMapping (
                             44,
                             (VK)'Z',
                             0,
                             'z'
                            ), // Z
        new ScanCodeMapping (
                             44,
                             (VK)'Z',
                             ConsoleModifiers.Shift,
                             'Z'
                            ),
        new ScanCodeMapping (
                             45,
                             (VK)'X',
                             0,
                             'x'
                            ), // X
        new ScanCodeMapping (
                             45,
                             (VK)'X',
                             ConsoleModifiers.Shift,
                             'X'
                            ),
        new ScanCodeMapping (
                             46,
                             (VK)'C',
                             0,
                             'c'
                            ), // C
        new ScanCodeMapping (
                             46,
                             (VK)'C',
                             ConsoleModifiers.Shift,
                             'C'
                            ),
        new ScanCodeMapping (
                             47,
                             (VK)'V',
                             0,
                             'v'
                            ), // V
        new ScanCodeMapping (
                             47,
                             (VK)'V',
                             ConsoleModifiers.Shift,
                             'V'
                            ),
        new ScanCodeMapping (
                             48,
                             (VK)'B',
                             0,
                             'b'
                            ), // B
        new ScanCodeMapping (
                             48,
                             (VK)'B',
                             ConsoleModifiers.Shift,
                             'B'
                            ),
        new ScanCodeMapping (
                             49,
                             (VK)'N',
                             0,
                             'n'
                            ), // N
        new ScanCodeMapping (
                             49,
                             (VK)'N',
                             ConsoleModifiers.Shift,
                             'N'
                            ),
        new ScanCodeMapping (
                             50,
                             (VK)'M',
                             0,
                             'm'
                            ), // M
        new ScanCodeMapping (
                             50,
                             (VK)'M',
                             ConsoleModifiers.Shift,
                             'M'
                            ),
        new ScanCodeMapping (
                             51,
                             VK.OEM_COMMA,
                             0,
                             ','
                            ), // OemComma
        new ScanCodeMapping (
                             51,
                             VK.OEM_COMMA,
                             ConsoleModifiers.Shift,
                             '<'
                            ),
        new ScanCodeMapping (
                             52,
                             VK.OEM_PERIOD,
                             0,
                             '.'
                            ), // OemPeriod
        new ScanCodeMapping (
                             52,
                             VK.OEM_PERIOD,
                             ConsoleModifiers.Shift,
                             '>'
                            ),
        new ScanCodeMapping (
                             53,
                             VK.OEM_MINUS,
                             0,
                             '-'
                            ), // OemMinus
        new ScanCodeMapping (
                             53,
                             VK.OEM_MINUS,
                             ConsoleModifiers.Shift,
                             '_'
                            ),
        new ScanCodeMapping (
                             54,
                             VK.RSHIFT,
                             0,
                             '\0'
                            ), // Right Shift
        new ScanCodeMapping (
                             54,
                             VK.RSHIFT,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             55,
                             VK.PRINT,
                             0,
                             '\0'
                            ), // Print Screen
        new ScanCodeMapping (
                             55,
                             VK.PRINT,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             56,
                             VK.LMENU,
                             0,
                             '\0'
                            ), // Alt
        new ScanCodeMapping (
                             56,
                             VK.LMENU,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             57,
                             VK.SPACE,
                             0,
                             ' '
                            ), // Spacebar
        new ScanCodeMapping (
                             57,
                             VK.SPACE,
                             ConsoleModifiers.Shift,
                             ' '
                            ),
        new ScanCodeMapping (
                             58,
                             VK.CAPITAL,
                             0,
                             '\0'
                            ), // Caps Lock
        new ScanCodeMapping (
                             58,
                             VK.CAPITAL,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             59,
                             VK.F1,
                             0,
                             '\0'
                            ), // F1
        new ScanCodeMapping (
                             59,
                             VK.F1,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             60,
                             VK.F2,
                             0,
                             '\0'
                            ), // F2
        new ScanCodeMapping (
                             60,
                             VK.F2,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             61,
                             VK.F3,
                             0,
                             '\0'
                            ), // F3
        new ScanCodeMapping (
                             61,
                             VK.F3,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             62,
                             VK.F4,
                             0,
                             '\0'
                            ), // F4
        new ScanCodeMapping (
                             62,
                             VK.F4,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             63,
                             VK.F5,
                             0,
                             '\0'
                            ), // F5
        new ScanCodeMapping (
                             63,
                             VK.F5,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             64,
                             VK.F6,
                             0,
                             '\0'
                            ), // F6
        new ScanCodeMapping (
                             64,
                             VK.F6,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             65,
                             VK.F7,
                             0,
                             '\0'
                            ), // F7
        new ScanCodeMapping (
                             65,
                             VK.F7,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             66,
                             VK.F8,
                             0,
                             '\0'
                            ), // F8
        new ScanCodeMapping (
                             66,
                             VK.F8,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             67,
                             VK.F9,
                             0,
                             '\0'
                            ), // F9
        new ScanCodeMapping (
                             67,
                             VK.F9,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             68,
                             VK.F10,
                             0,
                             '\0'
                            ), // F10
        new ScanCodeMapping (
                             68,
                             VK.F10,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             69,
                             VK.NUMLOCK,
                             0,
                             '\0'
                            ), // Num Lock
        new ScanCodeMapping (
                             69,
                             VK.NUMLOCK,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             70,
                             VK.SCROLL,
                             0,
                             '\0'
                            ), // Scroll Lock
        new ScanCodeMapping (
                             70,
                             VK.SCROLL,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             71,
                             VK.HOME,
                             0,
                             '\0'
                            ), // Home
        new ScanCodeMapping (
                             71,
                             VK.HOME,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             72,
                             VK.UP,
                             0,
                             '\0'
                            ), // Up Arrow
        new ScanCodeMapping (
                             72,
                             VK.UP,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             73,
                             VK.PRIOR,
                             0,
                             '\0'
                            ), // Page Up
        new ScanCodeMapping (
                             73,
                             VK.PRIOR,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             74,
                             VK.SUBTRACT,
                             0,
                             '-'
                            ), // Subtract (Num Pad '-')
        new ScanCodeMapping (
                             74,
                             VK.SUBTRACT,
                             ConsoleModifiers.Shift,
                             '-'
                            ),
        new ScanCodeMapping (
                             75,
                             VK.LEFT,
                             0,
                             '\0'
                            ), // Left Arrow
        new ScanCodeMapping (
                             75,
                             VK.LEFT,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             76,
                             VK.CLEAR,
                             0,
                             '\0'
                            ), // Center key (Num Pad 5 with Num Lock off)
        new ScanCodeMapping (
                             76,
                             VK.CLEAR,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             77,
                             VK.RIGHT,
                             0,
                             '\0'
                            ), // Right Arrow
        new ScanCodeMapping (
                             77,
                             VK.RIGHT,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             78,
                             VK.ADD,
                             0,
                             '+'
                            ), // Add (Num Pad '+')
        new ScanCodeMapping (
                             78,
                             VK.ADD,
                             ConsoleModifiers.Shift,
                             '+'
                            ),
        new ScanCodeMapping (
                             79,
                             VK.END,
                             0,
                             '\0'
                            ), // End
        new ScanCodeMapping (
                             79,
                             VK.END,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             80,
                             VK.DOWN,
                             0,
                             '\0'
                            ), // Down Arrow
        new ScanCodeMapping (
                             80,
                             VK.DOWN,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             81,
                             VK.NEXT,
                             0,
                             '\0'
                            ), // Page Down
        new ScanCodeMapping (
                             81,
                             VK.NEXT,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             82,
                             VK.INSERT,
                             0,
                             '\0'
                            ), // Insert
        new ScanCodeMapping (
                             82,
                             VK.INSERT,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             83,
                             VK.DELETE,
                             0,
                             '\0'
                            ), // Delete
        new ScanCodeMapping (
                             83,
                             VK.DELETE,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             86,
                             VK.OEM_102,
                             0,
                             '<'
                            ), // OEM 102 (Typically '<' or '|' key next to Left Shift)
        new ScanCodeMapping (
                             86,
                             VK.OEM_102,
                             ConsoleModifiers.Shift,
                             '>'
                            ),
        new ScanCodeMapping (
                             87,
                             VK.F11,
                             0,
                             '\0'
                            ), // F11
        new ScanCodeMapping (
                             87,
                             VK.F11,
                             ConsoleModifiers.Shift,
                             '\0'
                            ),
        new ScanCodeMapping (
                             88,
                             VK.F12,
                             0,
                             '\0'
                            ), // F12
        new ScanCodeMapping (
                             88,
                             VK.F12,
                             ConsoleModifiers.Shift,
                             '\0'
                            )
    };

    /// <summary>Decode a <see cref="ConsoleKeyInfo"/> that is using <see cref="ConsoleKey.Packet"/>.</summary>
    /// <param name="consoleKeyInfo">The console key info.</param>
    /// <returns>The decoded <see cref="ConsoleKeyInfo"/> or the <paramref name="consoleKeyInfo"/>.</returns>
    /// <remarks>
    ///     If it's a <see cref="ConsoleKey.Packet"/> the <see cref="ConsoleKeyInfo.KeyChar"/> may be a
    ///     <see cref="ConsoleKeyInfo.Key"/> or a <see cref="ConsoleKeyInfo.KeyChar"/> value.
    /// </remarks>
    public static ConsoleKeyInfo DecodeVKPacketToKConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
    {
        if (consoleKeyInfo.Key != ConsoleKey.Packet)
        {
            return consoleKeyInfo;
        }

        return GetConsoleKeyInfoFromKeyChar (consoleKeyInfo.KeyChar, consoleKeyInfo.Modifiers, out _);
    }

    /// <summary>
    ///     Encode the <see cref="ConsoleKeyInfo.KeyChar"/> with the <see cref="ConsoleKeyInfo.Key"/> if the first a byte
    ///     length, otherwise only the KeyChar is considered and searched on the database.
    /// </summary>
    /// <param name="consoleKeyInfo">The console key info.</param>
    /// <returns>The encoded KeyChar with the Key if both can be shifted, otherwise only the KeyChar.</returns>
    /// <remarks>This is useful to use with the <see cref="ConsoleKey.Packet"/>.</remarks>
    public static char EncodeKeyCharForVKPacket (ConsoleKeyInfo consoleKeyInfo)
    {
        char keyChar = consoleKeyInfo.KeyChar;
        ConsoleKey consoleKey = consoleKeyInfo.Key;

        if (keyChar != 0 && consoleKeyInfo.KeyChar < byte.MaxValue && consoleKey == ConsoleKey.None)
        {
            // try to get the ConsoleKey
            ScanCodeMapping scode = _scanCodes.FirstOrDefault (e => e.UnicodeChar == keyChar);

            if (scode is { })
            {
                consoleKey = (ConsoleKey)scode.VirtualKey;
            }
        }

        if (keyChar < byte.MaxValue && consoleKey != ConsoleKey.None)
        {
            keyChar = (char)((consoleKeyInfo.KeyChar << 8) | (byte)consoleKey);
        }

        return keyChar;
    }
}
