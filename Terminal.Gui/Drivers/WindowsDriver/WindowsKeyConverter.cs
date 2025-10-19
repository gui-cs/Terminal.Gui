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
}
