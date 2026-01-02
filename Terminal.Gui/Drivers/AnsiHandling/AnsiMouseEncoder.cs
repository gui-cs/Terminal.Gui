namespace Terminal.Gui.Drivers;

/// <summary>
///     Encodes <see cref="Mouse"/> events into ANSI SGR (1006) extended mouse format escape sequences.
/// </summary>
/// <remarks>
///     <para>
///         This is the inverse operation of <see cref="AnsiMouseParser"/>. It converts Terminal.Gui
///         <see cref="Mouse"/> events back into the ANSI escape sequences that would produce them.
///         Used primarily for test input injection in drivers that consume character streams (e.g., UnixDriver).
///     </para>
///     <para>
///         The SGR format uses decimal text encoding: <c>ESC[&lt;button;x;yM</c> (press) or <c>ESC[&lt;button;x;ym</c>
///         (release).
///     </para>
/// </remarks>
public static class AnsiMouseEncoder
{
    /// <summary>
    ///     Converts a <see cref="Mouse"/> event to an ANSI SGR (1006) extended mouse format escape sequence.
    /// </summary>
    /// <param name="mouse">The mouse event to convert.</param>
    /// <returns>ANSI escape sequence string in format: <c>ESC[&lt;button;x;yM</c> or <c>ESC[&lt;button;x;ym</c></returns>
    /// <remarks>
    ///     <para>
    ///         SGR format: <c>ESC[&lt;button;x;y{M|m}</c>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>M = press, m = release</description>
    ///         </item>
    ///         <item>
    ///             <description>Coordinates are 1-based in ANSI (Terminal.Gui uses 0-based)</description>
    ///         </item>
    ///         <item>
    ///             <description>Button codes encode both the button and modifier keys</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static string Encode (Mouse mouse)
    {
        // SGR format: ESC[<button;x;y{M|m}
        // M = press, m = release
        // Coordinates are 1-based in ANSI

        int buttonCode = GetButtonCode (mouse.Flags);
        int x = mouse.ScreenPosition.X + 1; // Convert to 1-based
        int y = mouse.ScreenPosition.Y + 1; // Convert to 1-based
        char terminator = GetTerminator (mouse.Flags);

        return $"{EscSeqUtils.CSI}<{buttonCode};{x};{y}{terminator}";
    }

    /// <summary>
    ///     Gets the ANSI button code from <see cref="MouseFlags"/>.
    /// </summary>
    /// <param name="flags">The mouse flags to encode.</param>
    /// <returns>The ANSI SGR button code.</returns>
    /// <remarks>
    ///     <para>
    ///         This is the inverse of <see cref="AnsiMouseParser.GetFlags"/> - it converts Terminal.Gui
    ///         <see cref="MouseFlags"/> back to the ANSI SGR button code that would produce those flags.
    ///     </para>
    ///     <para>
    ///         The ANSI button code encoding is:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Base button: 0=left, 1=middle, 2=right</description>
    ///         </item>
    ///         <item>
    ///             <description>Add 32 for drag (PositionReport with button)</description>
    ///         </item>
    ///         <item>
    ///             <description>Add 64 for wheel (64=up, 65=down, 68=left, 69=right)</description>
    ///         </item>
    ///         <item>
    ///             <description>Modifiers: +8 for Alt, +16 for Ctrl, special handling for Shift</description>
    ///         </item>
    ///     </list>
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
    ///     Gets the terminator character for the ANSI mouse sequence.
    /// </summary>
    /// <param name="flags">The mouse flags.</param>
    /// <returns>M for press/wheel/motion events, m for release events.</returns>
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
