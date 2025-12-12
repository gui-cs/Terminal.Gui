namespace Terminal.Gui.Drivers;

/// <summary>
///     Encodes <see cref="Key"/> objects into ANSI escape sequences.
/// </summary>
/// <remarks>
///     This is the inverse operation of <see cref="AnsiKeyboardParser"/>. It converts Terminal.Gui
///     <see cref="Key"/> objects back into the ANSI escape sequences that would produce them.
///     Used primarily for test input injection in drivers that consume character streams (e.g., UnixDriver).
/// </remarks>
public static class AnsiKeyboardEncoder
{
    /// <summary>
    ///     Converts a <see cref="Key"/> to its ANSI escape sequence representation or character.
    /// </summary>
    /// <param name="key">The key to encode.</param>
    /// <returns>
    ///     An ANSI escape sequence string for special keys (arrows, function keys, etc.),
    ///     or a character string for regular characters.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         For special keys (arrows, function keys, etc.), this returns the appropriate
    ///         ANSI escape sequence. For regular characters, it returns the character itself.
    ///     </para>
    ///     <para>
    ///         Alt combinations are represented by prefixing the character/sequence with ESC.
    ///         Ctrl combinations for letters A-Z are represented as ASCII control codes (0x01-0x1A).
    ///         Shift affects letter case but not control key behavior.
    ///     </para>
    ///     <para>
    ///         Note: Certain modifier combinations cannot be represented in ANSI (e.g., Ctrl+Shift
    ///         combinations produce the same control code as Ctrl alone).
    ///     </para>
    /// </remarks>
    public static string Encode (Key key)
    {
        // Strip modifiers to get base key
        KeyCode baseKeyCode = key.KeyCode & ~(KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask);

        // Check if it's a special key that needs an ANSI sequence
        string? ansiSeq = GetSpecialKeySequence (baseKeyCode);

        if (ansiSeq != null)
        {
            // For special keys with Alt, prefix the sequence with ESC
            if (key.IsAlt)
            {
                return $"{EscSeqUtils.KeyEsc}{ansiSeq}";
            }

            return ansiSeq;
        }

        // Handle Ctrl combinations for letters (Ctrl takes precedence over Alt)
        if (key.IsCtrl && baseKeyCode >= KeyCode.A && baseKeyCode <= KeyCode.Z)
        {
            // Ctrl+A = 0x01, Ctrl+B = 0x02, etc.
            var ctrlChar = (char)(baseKeyCode - KeyCode.A + 1);

            // If Alt is also pressed, prefix with ESC
            if (key.IsAlt)
            {
                return $"{EscSeqUtils.KeyEsc}{ctrlChar}";
            }

            return ctrlChar.ToString ();
        }

        // For regular characters, use the character value
        if (baseKeyCode < (KeyCode)128)
        {
            var ch = (char)baseKeyCode;

            // KeyCode.A through KeyCode.Z are uppercase by definition
            // If shift is NOT pressed, convert to lowercase
            if (ch >= 'A' && ch <= 'Z' && !key.IsShift)
            {
                ch = char.ToLower (ch);
            }

            // Handle Alt combinations by prefixing with ESC
            if (key.IsAlt)
            {
                return $"{EscSeqUtils.KeyEsc}{ch}";
            }

            return ch.ToString ();
        }

        // Fallback: use the ConsoleKeyMapping for complex cases
        ConsoleKeyInfo consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key.KeyCode);

        return consoleKeyInfo.KeyChar.ToString ();
    }

    /// <summary>
    ///     Gets the ANSI escape sequence for special keys (non-character keys).
    /// </summary>
    /// <param name="keyCode">The base key code (without modifiers).</param>
    /// <returns>The ANSI sequence string, or null if the key is not a special key.</returns>
    private static string? GetSpecialKeySequence (KeyCode keyCode)
    {
        return keyCode switch
               {
                   // Cursor movement keys - CSI sequences
                   KeyCode.CursorUp => $"{EscSeqUtils.CSI}A",
                   KeyCode.CursorDown => $"{EscSeqUtils.CSI}B",
                   KeyCode.CursorRight => $"{EscSeqUtils.CSI}C",
                   KeyCode.CursorLeft => $"{EscSeqUtils.CSI}D",
                   KeyCode.Home => $"{EscSeqUtils.CSI}H",
                   KeyCode.End => $"{EscSeqUtils.CSI}F",

                   // Function keys F1-F4 use SS3 format (ESC O)
                   KeyCode.F1 => $"{EscSeqUtils.KeyEsc}OP",
                   KeyCode.F2 => $"{EscSeqUtils.KeyEsc}OQ",
                   KeyCode.F3 => $"{EscSeqUtils.KeyEsc}OR",
                   KeyCode.F4 => $"{EscSeqUtils.KeyEsc}OS",

                   // Function keys F5-F12 use CSI format with tilde terminator
                   KeyCode.F5 => $"{EscSeqUtils.CSI}15~",
                   KeyCode.F6 => $"{EscSeqUtils.CSI}17~",
                   KeyCode.F7 => $"{EscSeqUtils.CSI}18~",
                   KeyCode.F8 => $"{EscSeqUtils.CSI}19~",
                   KeyCode.F9 => $"{EscSeqUtils.CSI}20~",
                   KeyCode.F10 => $"{EscSeqUtils.CSI}21~",
                   KeyCode.F11 => $"{EscSeqUtils.CSI}23~",
                   KeyCode.F12 => $"{EscSeqUtils.CSI}24~",

                   // Editing keys - CSI format with tilde terminator
                   KeyCode.Insert => $"{EscSeqUtils.CSI}2~",
                   KeyCode.Delete => $"{EscSeqUtils.CSI}3~",
                   KeyCode.PageUp => $"{EscSeqUtils.CSI}5~",
                   KeyCode.PageDown => $"{EscSeqUtils.CSI}6~",

                   // Special characters
                   KeyCode.Tab => "\t",
                   KeyCode.Enter => "\r",
                   KeyCode.Backspace => "\x7F", // DEL (127)
                   KeyCode.Esc => $"{EscSeqUtils.KeyEsc}",

                   _ => null
               };
    }
}
