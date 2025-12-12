using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Input processor for <see cref="UnixInput"/>, deals in <see cref="char"/> stream.
/// </summary>
internal class UnixInputProcessor : InputProcessorImpl<char>
{
    /// <inheritdoc />
    public UnixInputProcessor (ConcurrentQueue<char> inputBuffer) : base (inputBuffer, new UnixKeyConverter ())
    {
        DriverName = "unix";
    }

    /// <inheritdoc />
    public override void EnqueueKeyDownEvent (Key key)
    {
        // Convert Key → ANSI sequence (if needed) or char
        string sequence = KeyToAnsiSequence (key);

        // If input supports testing, use it
        if (InputImpl is ITestableInput<char> testableInput)
        {
            foreach (char ch in sequence)
            {
                testableInput.AddInput (ch);
            }
        }
    }

    /// <summary>
    ///     Converts a Key to its ANSI escape sequence representation or character.
    /// </summary>
    /// <remarks>
    ///     For special keys (arrows, function keys, etc.), this returns the appropriate
    ///     ANSI escape sequence. For regular characters, it returns the character itself.
    /// </remarks>
    internal static string KeyToAnsiSequence (Key key)
    {
        // Strip modifiers to get base key
        KeyCode baseKeyCode = key.KeyCode & ~(KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask);

        // Check if it's a special key that needs an ANSI sequence
        string? ansiSeq = baseKeyCode switch
        {
            // Cursor movement
            KeyCode.CursorUp => "\u001B[A",
            KeyCode.CursorDown => "\u001B[B",
            KeyCode.CursorRight => "\u001B[C",
            KeyCode.CursorLeft => "\u001B[D",
            KeyCode.Home => "\u001B[H",
            KeyCode.End => "\u001B[F",

            // Function keys (F1-F12)
            KeyCode.F1 => "\u001BOP",
            KeyCode.F2 => "\u001BOQ",
            KeyCode.F3 => "\u001BOR",
            KeyCode.F4 => "\u001BOS",
            KeyCode.F5 => "\u001B[15~",
            KeyCode.F6 => "\u001B[17~",
            KeyCode.F7 => "\u001B[18~",
            KeyCode.F8 => "\u001B[19~",
            KeyCode.F9 => "\u001B[20~",
            KeyCode.F10 => "\u001B[21~",
            KeyCode.F11 => "\u001B[23~",
            KeyCode.F12 => "\u001B[24~",

            // Editing keys
            KeyCode.Insert => "\u001B[2~",
            KeyCode.Delete => "\u001B[3~",
            KeyCode.PageUp => "\u001B[5~",
            KeyCode.PageDown => "\u001B[6~",

            // Special characters
            KeyCode.Tab => "\t",
            KeyCode.Enter => "\r",
            KeyCode.Backspace => "\x7F", // DEL (127)
            KeyCode.Esc => "\u001B",

            _ => null
        };

        if (ansiSeq != null)
        {
            // TODO: Handle modifiers (Ctrl, Shift, Alt) by modifying the ANSI sequence
            // For now, just return the base sequence
            return ansiSeq;
        }

        // For regular characters, convert Key to char
        // Handle Ctrl combinations (Ctrl takes precedence over Alt)
        if (key.IsCtrl && baseKeyCode >= KeyCode.A && baseKeyCode <= KeyCode.Z)
        {
            // Ctrl+A = 0x01, Ctrl+B = 0x02, etc.
            char ctrlChar = (char)(baseKeyCode - KeyCode.A + 1);

            // If Alt is also pressed, prefix with ESC
            if (key.IsAlt)
            {
                return $"\u001B{ctrlChar}";
            }

            return ctrlChar.ToString ();
        }

        // For regular characters, just use the character value
        if (baseKeyCode < (KeyCode)128)
        {
            char ch = (char)baseKeyCode;

            // KeyCode.A through KeyCode.Z are uppercase by definition
            // If shift is NOT pressed, convert to lowercase
            if (ch >= 'A' && ch <= 'Z' && !key.IsShift)
            {
                ch = char.ToLower (ch);
            }

            // Handle Alt combinations by prefixing with ESC
            if (key.IsAlt)
            {
                return $"\u001B{ch}";
            }

            return ch.ToString ();
        }

        // Fallback: use the ConsoleKeyMapping
        ConsoleKeyInfo consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyInfoFromKeyCode (key.KeyCode);

        return consoleKeyInfo.KeyChar.ToString ();
    }

    /// <inheritdoc />
    protected override void Process (char input)
    {
        foreach (Tuple<char, char> released in Parser.ProcessInput (Tuple.Create (input, input)))
        {
            ProcessAfterParsing (released.Item2);
        }
    }

    /// <inheritdoc />
    public override void EnqueueMouseEvent (IApplication? app, Mouse mouse)
    {
        // Convert Mouse to ANSI SGR format escape sequence
        string ansiSequence = MouseToAnsiSequence (mouse);

        // Enqueue each character of the ANSI sequence
        if (InputImpl is ITestableInput<char> testableInput)
        {
            foreach (char ch in ansiSequence)
            {
                testableInput.AddInput (ch);
            }
        }
    }

    /// <summary>
    ///     Converts a Mouse event to an ANSI SGR (1006) extended mouse format escape sequence.
    /// </summary>
    /// <param name="mouse">The mouse event to convert.</param>
    /// <returns>ANSI escape sequence string in format: ESC[&lt;button;x;yM or ESC[&lt;button;x;ym</returns>
    private static string MouseToAnsiSequence (Mouse mouse)
    {
        // SGR format: ESC[<button;x;y{M|m}
        // M = press, m = release
        // Coordinates are 1-based in ANSI

        int buttonCode = GetButtonCode (mouse.Flags);
        int x = mouse.ScreenPosition.X + 1; // Convert to 1-based
        int y = mouse.ScreenPosition.Y + 1; // Convert to 1-based
        char terminator = GetTerminator (mouse.Flags);

        return $"\u001B[<{buttonCode};{x};{y}{terminator}";
    }

    /// <summary>
    ///     Gets the ANSI button code from MouseFlags.
    ///     This is the inverse of AnsiMouseParser.GetFlags() - it converts Terminal.Gui
    ///     MouseFlags back to the ANSI SGR button code that would produce those flags.
    /// </summary>
    /// <remarks>
    ///     The ANSI button code encoding is:
    ///     - Base button: 0=left, 1=middle, 2=right
    ///     - Add 32 for drag (PositionReport with button)
    ///     - Add 64 for wheel (64=up, 65=down, 68=left, 69=right)
    ///     - Modifiers: +8 for Alt, +16 for Ctrl, special handling for Shift
    /// </remarks>
    private static int GetButtonCode (MouseFlags flags)
    {
        // Special cases: wheel events
        // Note: WheeledLeft = Ctrl | WheeledUp, WheeledRight = Ctrl | WheeledDown
        // So we need to check for these combinations first before checking individual flags

        if (flags.HasFlag (MouseFlags.WheeledLeft))
        {
            // WheeledLeft is defined as Ctrl | WheeledUp, which maps to button code 68
            // The ANSI parser also adds Shift flag for code 68, but we'll let that happen naturally
            return 68;
        }

        if (flags.HasFlag (MouseFlags.WheeledRight))
        {
            // WheeledRight is defined as Ctrl | WheeledDown, which maps to button code 69
            // The ANSI parser also adds Shift flag for code 69, but we'll let that happen naturally
            return 69;
        }

        if (flags.HasFlag (MouseFlags.WheeledUp))
        {
            return 64;
        }

        if (flags.HasFlag (MouseFlags.WheeledDown))
        {
            return 65;
        }

        // Determine base button (0, 1, 2)
        int baseButton;

        if (flags.HasFlag (MouseFlags.LeftButtonPressed) || flags.HasFlag (MouseFlags.LeftButtonReleased))
        {
            baseButton = 0;
        }
        else if (flags.HasFlag (MouseFlags.MiddleButtonPressed) || flags.HasFlag (MouseFlags.MiddleButtonReleased))
        {
            baseButton = 1;
        }
        else if (flags.HasFlag (MouseFlags.RightButtonPressed) || flags.HasFlag (MouseFlags.RightButtonReleased))
        {
            baseButton = 2;
        }
        else if (flags.HasFlag (MouseFlags.PositionReport))
        {
            // Motion without button
            baseButton = 35;
        }
        else
        {
            baseButton = 0; // Default to left
        }

        // Start with base button
        int buttonCode = baseButton;

        // Check if it's a drag event (position report with button pressed)
        bool isDrag = flags.HasFlag (MouseFlags.PositionReport)
                      && (flags.HasFlag (MouseFlags.LeftButtonPressed)
                          || flags.HasFlag (MouseFlags.MiddleButtonPressed)
                          || flags.HasFlag (MouseFlags.RightButtonPressed));

        if (isDrag)
        {
            // Drag events use codes 32-34
            return 32 + baseButton;
        }

        // Add modifiers
        bool hasAlt = flags.HasFlag (MouseFlags.Alt);
        bool hasCtrl = flags.HasFlag (MouseFlags.Ctrl);
        bool hasShift = flags.HasFlag (MouseFlags.Shift);

        // Standard modifier encoding: Alt=+8, Ctrl=+16
        if (hasAlt)
        {
            buttonCode += 8;
        }

        if (hasCtrl)
        {
            buttonCode += 16;
        }

        // Shift is trickier - looking at AnsiMouseParser switch statement:
        // - Codes 14, 22, 30, 36-37, 45-46, 53-54, 61-62 include Shift
        // - Pattern is not simply +4, it depends on other modifiers
        // For Ctrl+Shift: codes are 22 (right), 53-54 (motion+ctrl+shift)
        // For Alt+Shift: codes are 14 (right), 45-46 (motion+alt+shift), 47 (motion)
        // For position reports with shift: 36-37, 45-46, 53-54, 61-62

        if (hasShift)
        {
            if (flags.HasFlag (MouseFlags.PositionReport))
            {
                // Position report with shift
                buttonCode = 36 + baseButton; // Base for motion+shift
            }
            else if (hasCtrl && hasAlt)
            {
                // Ctrl+Alt+Shift: code 30 (for right) + offset
                buttonCode += 6; //  Makes 24+6=30
            }
            else if (hasCtrl)
            {
                // Ctrl+Shift: code 22 for right button
                // 16+2+4 = 22 for right, but we want pattern
                buttonCode += 6; // Makes 16+0+6=22 for left+ctrl+shift
            }
            else if (hasAlt)
            {
                // Alt+Shift: code 14 for right
                buttonCode += 6; // Makes 8+0+6=14 for left+alt+shift
            }
            else
            {
                // Just shift (for motion events this is handled above)
                // For button events, shift isn't typically sent alone
                buttonCode += 4; // Approximation
            }
        }

        return buttonCode;
    }

    /// <summary>
    ///     Gets the terminator character (M for press, m for release).
    /// </summary>
    private static char GetTerminator (MouseFlags flags)
    {
        // Release events use 'm', press/wheel/motion use 'M'
        if (flags.HasFlag (MouseFlags.LeftButtonReleased)
            || flags.HasFlag (MouseFlags.MiddleButtonReleased)
            || flags.HasFlag (MouseFlags.RightButtonReleased)
            || flags.HasFlag (MouseFlags.Button4Released))
        {
            return 'm';
        }

        return 'M';
    }
}
