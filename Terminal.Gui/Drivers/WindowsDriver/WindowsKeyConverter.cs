#nullable enable


namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IKeyConverter{T}"/> capable of converting the
///     windows native <see cref="WindowsConsole.InputRecord"/> class
///     into Terminal.Gui shared <see cref="Key"/> representation
///     (used by <see cref="View"/> etc).
/// </summary>
internal class WindowsKeyConverter : IKeyConverter<WindowsConsole.InputRecord>
{
    /// <inheritdoc/>
    public Key ToKey (WindowsConsole.InputRecord inputEvent)
    {
        if (inputEvent.KeyEvent.wVirtualKeyCode == (ConsoleKeyMapping.VK)ConsoleKey.Packet)
        {
            // Used to pass Unicode characters as if they were keystrokes.
            // The VK_PACKET key is the low word of a 32-bit
            // Virtual Key value used for non-keyboard input methods.
            inputEvent.KeyEvent = WindowsKeyHelper.FromVKPacketToKeyEventRecord (inputEvent.KeyEvent);
        }

        var keyInfo = WindowsKeyHelper.ToConsoleKeyInfoEx (inputEvent.KeyEvent);

        //Debug.WriteLine ($"event: KBD: {GetKeyboardLayoutName()} {inputEvent.ToString ()} {keyInfo.ToString (keyInfo)}");

        KeyCode map = WindowsKeyHelper.MapKey (keyInfo);

        if (map == KeyCode.Null)
        {
            return (Key)0;
        }

        return new (map);
    }

    /// <inheritdoc />
    public WindowsConsole.InputRecord ToKeyInfo (Key key)
    {
        // Convert Key to ConsoleKeyInfo using the cross-platform mapping utility
        ConsoleKeyInfo consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key.KeyCode);

        // Build the ControlKeyState from the ConsoleKeyInfo modifiers
        var controlKeyState = WindowsConsole.ControlKeyState.NoControlKeyPressed;

        if (consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Shift))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.ShiftPressed;
        }

        if (consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftAltPressed;
        }

        if (consoleKeyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
        {
            controlKeyState |= WindowsConsole.ControlKeyState.LeftControlPressed;
        }

        // Get the scan code for this key
        uint scanCode = ConsoleKeyMapping.GetScanCodeFromConsoleKeyInfo (consoleKeyInfo);

        // Create a KeyEventRecord with the converted values
        var keyEvent = new WindowsConsole.KeyEventRecord
        {
            bKeyDown = true, // Assume key down for conversion
            wRepeatCount = 1,
            wVirtualKeyCode = (ConsoleKeyMapping.VK)consoleKeyInfo.Key,
            wVirtualScanCode = (ushort)scanCode,
            UnicodeChar = consoleKeyInfo.KeyChar,
            dwControlKeyState = controlKeyState
        };

        // Create and return an InputRecord with the keyboard event
        return new WindowsConsole.InputRecord
        {
            EventType = WindowsConsole.EventType.Key,
            KeyEvent = keyEvent
        };
    }
}
