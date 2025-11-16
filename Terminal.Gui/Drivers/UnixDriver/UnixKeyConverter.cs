namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IKeyConverter{T}"/> capable of converting the
///     unix native <see cref="char"/> class
///     into Terminal.Gui shared <see cref="Key"/> representation
///     (used by <see cref="View"/> etc).
/// </summary>
internal class UnixKeyConverter : IKeyConverter<char>
{
    /// <inheritdoc/>
    public Key ToKey (char value)
    {
        ConsoleKeyInfo adjustedInput = EscSeqUtils.MapChar (value);

        return EscSeqUtils.MapKey (adjustedInput);
    }

    /// <inheritdoc/>
    public char ToKeyInfo (Key key)
    {
        // Convert Key to ConsoleKeyInfo using the cross-platform mapping utility
        ConsoleKeyInfo consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key.KeyCode);

        // Return the character representation
        // For Unix, we primarily care about the KeyChar as Unix deals with character input
        return consoleKeyInfo.KeyChar;
    }
}
