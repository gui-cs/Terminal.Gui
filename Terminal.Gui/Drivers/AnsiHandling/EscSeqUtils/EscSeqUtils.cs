#nullable enable
using System.Diagnostics;
using System.Globalization;

// ReSharper disable InconsistentNaming

namespace Terminal.Gui.Drivers;

/// <summary>
///     Provides a platform-independent API for managing ANSI escape sequences.
/// </summary>
/// <remarks>
///     Useful resources:
///     * https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences
///     * https://invisible-island.net/xterm/ctlseqs/ctlseqs.html
///     * https://vt100.net/
/// </remarks>
public static class EscSeqUtils
{
    /// <summary>
    ///     Escape key code (ASCII 27/0x1B).
    /// </summary>
    public const char KeyEsc = (char)KeyCode.Esc;

    /// <summary>
    ///     ESC [ - The CSI (Control Sequence Introducer).
    /// </summary>
    public const string CSI = "\u001B[";

    #region Screen Window Buffer

    /// <summary>
    ///     Options for ANSI ESC "[xJ" - Clears part of the screen.
    /// </summary>
    public enum ClearScreenOptions
    {
        /// <summary>
        ///     If n is 0 (or missing), clear from cursor to end of screen.
        /// </summary>
        CursorToEndOfScreen = 0,

        /// <summary>
        ///     If n is 1, clear from cursor to beginning of the screen.
        /// </summary>
        CursorToBeginningOfScreen = 1,

        /// <summary>
        ///     If n is 2, clear entire screen (and moves cursor to upper left on DOS ANSI.SYS).
        /// </summary>
        EntireScreen = 2,

        /// <summary>
        ///     If n is 3, clear entire screen and delete all lines saved in the scrollback buffer
        /// </summary>
        EntireScreenAndScrollbackBuffer = 3
    }

    /// <summary>
    ///     ESC [ ? 1047 h - Activate xterm alternative buffer (no backscroll)
    /// </summary>
    /// <remarks>
    ///     From
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s_
    ///     Use Alternate Screen Buffer, xterm.
    /// </remarks>
    public static readonly string CSI_ActivateAltBufferNoBackscroll = CSI + "?1047h";

    /// <summary>
    ///     ESC [ ? 1047 l - Restore xterm working buffer (with backscroll)
    /// </summary>
    /// <remarks>
    ///     From
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s_
    ///     Use Normal Screen Buffer, xterm.  Clear the screen first if in the Alternate Screen Buffer.
    /// </remarks>
    public static readonly string CSI_RestoreAltBufferWithBackscroll = CSI + "?1047l";

    /// <summary>
    ///     ESC [ ? 1049 l - Restore cursor position and restore xterm working buffer (with backscroll)
    /// </summary>
    /// <remarks>
    ///     From
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s_
    ///     Use Normal Screen Buffer and restore cursor as in DECRC, xterm.
    ///     resource.This combines the effects of the 1047 and 1048  modes.
    /// </remarks>
    public static readonly string CSI_RestoreCursorAndRestoreAltBufferWithBackscroll = CSI + "?1049l";

    /// <summary>
    ///     ESC [ ? 1049 h - Save cursor position and activate xterm alternative buffer (no backscroll)
    /// </summary>
    /// <remarks>
    ///     From
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s_
    ///     Save cursor as in DECSC, xterm. After saving the cursor, switch to the Alternate Screen Buffer,
    ///     clearing it first.
    ///     This control combines the effects of the 1047 and 1048 modes.
    ///     Use this with terminfo-based applications rather than the 47 mode.
    /// </remarks>
    public static readonly string CSI_SaveCursorAndActivateAltBufferNoBackscroll = CSI + "?1049h";

    /// <summary>
    ///     ESC [ x J - Clears part of the screen. See <see cref="ClearScreenOptions"/>.
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    public static string CSI_ClearScreen (ClearScreenOptions option) { return $"{CSI}{(int)option}J"; }

    /// <summary>
    ///     ESC [ 8 ; height ; width t - Set Terminal Window Size
    ///     https://terminalguide.namepad.de/seq/csi_st-8/
    /// </summary>
    public static string CSI_SetTerminalWindowSize (int height, int width) { return $"{CSI}8;{height};{width}t"; }

    #endregion Screen Window Buffer

    #region Mouse

    /// <summary>
    ///     ESC [ ? 1003 l - Disable any mouse event tracking.
    /// </summary>
    public static readonly string CSI_DisableAnyEventMouse = CSI + "?1003l";

    /// <summary>
    ///     ESC [ ? 1006 l - Disable SGR (Select Graphic Rendition).
    /// </summary>
    public static readonly string CSI_DisableSgrExtModeMouse = CSI + "?1006l";

    /// <summary>
    ///     ESC [ ? 1015 l - Disable URXVT (Unicode Extended Virtual Terminal).
    /// </summary>
    public static readonly string CSI_DisableUrxvtExtModeMouse = CSI + "?1015l";

    /// <summary>
    ///     ESC [ ? 1003 h - Enable  mouse event tracking.
    /// </summary>
    public static readonly string CSI_EnableAnyEventMouse = CSI + "?1003h";

    /// <summary>
    ///     ESC [ ? 1006 h - Enable SGR (Select Graphic Rendition).
    /// </summary>
    public static readonly string CSI_EnableSgrExtModeMouse = CSI + "?1006h";

    /// <summary>
    ///     ESC [ ? 1015 h - Enable URXVT (Unicode Extended Virtual Terminal).
    /// </summary>
    public static readonly string CSI_EnableUrxvtExtModeMouse = CSI + "?1015h";

    /// <summary>
    ///     Control sequence for disabling mouse events.
    /// </summary>
    public static readonly string CSI_DisableMouseEvents =
        CSI_DisableAnyEventMouse + CSI_DisableUrxvtExtModeMouse + CSI_DisableSgrExtModeMouse;

    /// <summary>
    ///     Control sequence for enabling mouse events.
    /// </summary>
    public static readonly string CSI_EnableMouseEvents =
        CSI_EnableAnyEventMouse + CSI_EnableUrxvtExtModeMouse + CSI_EnableSgrExtModeMouse;

    #endregion Mouse

    #region Keyboard

    /// <summary>
    ///     Helper to set the Control key states based on the char.
    /// </summary>
    /// <param name="ch">The char value.</param>
    /// <returns></returns>
    public static ConsoleKeyInfo MapChar (char ch) { return MapConsoleKeyInfo (new (ch, ConsoleKey.None, false, false, false)); }

    /// <summary>
    ///     Ensures a console key is mapped to one that works correctly with ANSI escape sequences.
    /// </summary>
    /// <param name="consoleKeyInfo">The <see cref="ConsoleKeyInfo"/>.</param>
    /// <returns>The <see cref="ConsoleKeyInfo"/> modified.</returns>
    public static ConsoleKeyInfo MapConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
    {
        ConsoleKeyInfo newConsoleKeyInfo = consoleKeyInfo;
        var key = ConsoleKey.None;
        char keyChar = consoleKeyInfo.KeyChar;

        switch ((uint)keyChar)
        {
            case 0:
                if (consoleKeyInfo.Key == (ConsoleKey)64)
                { // Ctrl+Space in Windows.
                    newConsoleKeyInfo = new (
                                             consoleKeyInfo.KeyChar,
                                             ConsoleKey.Spacebar,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }
                else if (consoleKeyInfo.Key == ConsoleKey.None)
                {
                    newConsoleKeyInfo = new (
                                             consoleKeyInfo.KeyChar,
                                             ConsoleKey.Spacebar,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                             true);
                }

                break;
            case uint n when n is > 0 and <= KeyEsc:
                if (consoleKeyInfo is { Key: 0, KeyChar: '\u001B' })
                {
                    key = ConsoleKey.Escape;

                    newConsoleKeyInfo = new (
                                             consoleKeyInfo.KeyChar,
                                             key,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }
                else if (consoleKeyInfo is { Key: 0, KeyChar: '\t' })
                {
                    key = ConsoleKey.Tab;

                    newConsoleKeyInfo = new (
                                             consoleKeyInfo.KeyChar,
                                             key,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }
                else if (consoleKeyInfo is { Key: 0, KeyChar: '\r' })
                {
                    key = ConsoleKey.Enter;

                    newConsoleKeyInfo = new (
                                             consoleKeyInfo.KeyChar,
                                             key,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }
                else if (consoleKeyInfo is { Key: 0, KeyChar: '\n' })
                {
                    key = ConsoleKey.Enter;

                    newConsoleKeyInfo = new (
                                             consoleKeyInfo.KeyChar,
                                             key,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                             true);
                }
                else if (consoleKeyInfo is { Key: 0, KeyChar: '\b' })
                {
                    key = ConsoleKey.Backspace;

                    newConsoleKeyInfo = new (
                                             consoleKeyInfo.KeyChar,
                                             key,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                             true);
                }
                else if (consoleKeyInfo.Key == 0)
                {
                    key = (ConsoleKey)(char)(consoleKeyInfo.KeyChar + (uint)ConsoleKey.A - 1);

                    newConsoleKeyInfo = new (
                                             consoleKeyInfo.KeyChar,
                                             key,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                             (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                             true);
                }

                break;
            case uint n when n is >= '\u001c' and <= '\u001f':
                key = (ConsoleKey)(char)(consoleKeyInfo.KeyChar + 24);

                newConsoleKeyInfo = new (
                                         (char)key,
                                         key,
                                         (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                         (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                         true);

                break;
            case 127: // DEL
                key = ConsoleKey.Backspace;

                newConsoleKeyInfo = new (
                                         consoleKeyInfo.KeyChar,
                                         key,
                                         (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                         (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                         (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);

                break;
            default:
                uint ck = ConsoleKeyMapping.MapKeyCodeToConsoleKey ((KeyCode)consoleKeyInfo.KeyChar, out bool isConsoleKey);

                if (isConsoleKey)
                {
                    key = (ConsoleKey)ck;
                }

                newConsoleKeyInfo = new (
                                         keyChar,
                                         key,
                                         GetShiftMod (consoleKeyInfo.Modifiers),
                                         (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                         (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);

                break;
        }

        return newConsoleKeyInfo;

        bool GetShiftMod (ConsoleModifiers modifiers)
        {
            if (consoleKeyInfo.KeyChar is >= (char)ConsoleKey.A and <= (char)ConsoleKey.Z && modifiers == ConsoleModifiers.None)
            {
                return true;
            }

            return (modifiers & ConsoleModifiers.Shift) != 0;
        }
    }

    internal static KeyCode MapKey (ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Multiply:
            case ConsoleKey.Add:
            case ConsoleKey.Separator:
            case ConsoleKey.Subtract:
            case ConsoleKey.Decimal:
            case ConsoleKey.Divide:
            case ConsoleKey.OemPeriod:
            case ConsoleKey.OemComma:
            case ConsoleKey.OemPlus:
            case ConsoleKey.OemMinus:
            case ConsoleKey.Packet:
            case ConsoleKey.Oem1:
            case ConsoleKey.Oem2:
            case ConsoleKey.Oem3:
            case ConsoleKey.Oem4:
            case ConsoleKey.Oem5:
            case ConsoleKey.Oem6:
            case ConsoleKey.Oem7:
            case ConsoleKey.Oem8:
            case ConsoleKey.Oem102:
                if (keyInfo.KeyChar == 0)
                {
                    // All Oem* produce a valid KeyChar and is not guaranteed to be printable ASCII, but it’s never just '\0' (null).
                    // If that happens it's because Console.ReadKey is misreporting for AltGr + non-character keys
                    // or if it's a combine key waiting for the next input which will determine the respective KeyChar.
                    // This behavior only happens on Windows and not on Unix-like systems.
                    if (keyInfo.Key != ConsoleKey.Multiply
                        && keyInfo.Key != ConsoleKey.Add
                        && keyInfo.Key != ConsoleKey.Decimal
                        && keyInfo.Key != ConsoleKey.Subtract
                        && keyInfo.Key != ConsoleKey.Divide
                        && keyInfo.Key != ConsoleKey.OemPeriod
                        && keyInfo.Key != ConsoleKey.OemComma
                        && keyInfo.Key != ConsoleKey.OemPlus
                        && keyInfo.Key != ConsoleKey.OemMinus
                        && keyInfo.Key != ConsoleKey.Oem1
                        && keyInfo.Key != ConsoleKey.Oem2
                        && keyInfo.Key != ConsoleKey.Oem3
                        && keyInfo.Key != ConsoleKey.Oem4
                        && keyInfo.Key != ConsoleKey.Oem5
                        && keyInfo.Key != ConsoleKey.Oem6
                        && keyInfo.Key != ConsoleKey.Oem7
                        && keyInfo.Key != ConsoleKey.Oem102)
                    {
                        // If the keyChar is 0, keyInfo.Key value is not a printable character.
                        Debug.Assert (keyInfo.Key == 0);
                    }

                    return KeyCode.Null; // MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode)keyInfo.Key);
                }

                if (keyInfo.Modifiers != ConsoleModifiers.Shift)
                {
                    // If Shift wasn't down we don't need to do anything but return the keyInfo.KeyChar
                    return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
                }

                // Strip off Shift - We got here because they KeyChar from Windows is the shifted char (e.g. "Ç")
                // and passing on Shift would be redundant.
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.KeyChar);
        }

        // Handle control keys whose VK codes match the related ASCII value (those below ASCII 33) like ESC
        if (keyInfo.Key != ConsoleKey.None && Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key))
        {
            if (keyInfo is { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.I })
            {
                return KeyCode.Tab;
            }

            return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)(uint)keyInfo.Key);
        }

        // Handle control keys (e.g. CursorUp)
        if (keyInfo.Key != ConsoleKey.None
            && Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint))
        {
            return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)((uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint));
        }

        if ((ConsoleKey)keyInfo.KeyChar is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            // Shifted
            keyInfo = new (
                           keyInfo.KeyChar,
                           (ConsoleKey)keyInfo.KeyChar,
                           true,
                           keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt),
                           keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control));
        }

        if ((ConsoleKey)keyInfo.KeyChar - 32 is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            // Unshifted
            keyInfo = new (
                           keyInfo.KeyChar,
                           (ConsoleKey)(keyInfo.KeyChar - 32),
                           false,
                           keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt),
                           keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control));
        }

        if (keyInfo.Key is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt)
                || keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
            {
                // DotNetDriver doesn't support Shift-Ctrl/Shift-Alt combos
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.Key);
            }

            if (keyInfo.Modifiers == ConsoleModifiers.Shift)
            {
                // If ShiftMask is on  add the ShiftMask
                if (char.IsUpper (keyInfo.KeyChar))
                {
                    return (KeyCode)keyInfo.Key | KeyCode.ShiftMask;
                }
            }

            return (KeyCode)keyInfo.Key;
        }

        return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
    }

    #endregion Keyboard

    #region Cursor

    //ESC [ M - RI Reverse Index – Performs the reverse operation of \n, moves cursor up one line, maintains horizontal position, scrolls buffer if necessary*

    /// <summary>
    ///     ESC [ 7 - Save Cursor Position in Memory**
    /// </summary>
    public static readonly string CSI_SaveCursorPosition = CSI + "7";

    /// <summary>
    ///     ESC [ 8 - DECSR Restore Cursor Position from Memory**
    /// </summary>
    public static readonly string CSI_RestoreCursorPosition = CSI + "8";

    //ESC [ < n > A - CUU - Cursor Up       Cursor up by < n >
    //ESC [ < n > B - CUD - Cursor Down     Cursor down by < n >
    //ESC [ < n > C - CUF - Cursor Forward  Cursor forward (Right) by < n >
    //ESC [ < n > D - CUB - Cursor Backward Cursor backward (Left) by < n >
    //ESC [ < n > E - CNL - Cursor Next Line - Cursor down < n > lines from current position
    //ESC [ < n > F - CPL - Cursor Previous Line    Cursor up < n > lines from current position
    //ESC [ < n > G - CHA - Cursor Horizontal Absolute      Cursor moves to < n > th position horizontally in the current line
    //ESC [ < n > d - VPA - Vertical Line Position Absolute Cursor moves to the < n > th position vertically in the current column

    /// <summary>
    ///     ESC [ y ; x H - CUP Cursor Position - Cursor moves to x ; y coordinate within the viewport, where x is the column
    ///     of the y line
    /// </summary>
    /// <param name="row">Origin is (1,1).</param>
    /// <param name="col">Origin is (1,1).</param>
    /// <returns></returns>
    public static string CSI_SetCursorPosition (int row, int col) { return $"{CSI}{row};{col}H"; }

    /// <summary>
    ///     ESC [ y ; x H - CUP Cursor Position - Cursor moves to x ; y coordinate within the viewport, where x is the column
    ///     of the y line
    /// </summary>
    /// <param name="builder">StringBuilder where to append the cursor position sequence.</param>
    /// <param name="row">Origin is (1,1).</param>
    /// <param name="col">Origin is (1,1).</param>
    public static void CSI_AppendCursorPosition (StringBuilder builder, int row, int col)
    {
        // InterpolatedStringHandler is composed in stack, skipping the string allocation.
        builder.Append ($"{CSI}{row};{col}H");
    }

    /// <summary>
    ///     ESC [ y ; x H - CUP Cursor Position - Cursor moves to x ; y coordinate within the viewport, where x is the column
    ///     of the y line
    /// </summary>
    /// <param name="writer">TextWriter where to write the cursor position sequence.</param>
    /// <param name="row">Origin is (1,1).</param>
    /// <param name="col">Origin is (1,1).</param>
    public static void CSI_WriteCursorPosition (TextWriter writer, int row, int col)
    {
        const int maxInputBufferSize =

            // CSI (2) + ';' + 'H'
            4
            +

            // row + col (2x int sign + int max value)
            2
            + 20;
        Span<char> buffer = stackalloc char [maxInputBufferSize];

        if (!buffer.TryWrite (CultureInfo.InvariantCulture, $"{CSI}{row};{col}H", out int charsWritten))
        {
            var tooLongCursorPositionSequence = $"{CSI}{row};{col}H";

            throw new InvalidOperationException (
                                                 $"{nameof (CSI_WriteCursorPosition)} buffer (len: {buffer.Length}) is too short for cursor position sequence '{tooLongCursorPositionSequence}' (len: {tooLongCursorPositionSequence.Length}).");
        }

        ReadOnlySpan<char> cursorPositionSequence = buffer [..charsWritten];
        writer.Write (cursorPositionSequence);
    }

    //ESC [ <y> ; <x> f - HVP     Horizontal Vertical Position* Cursor moves to<x>; <y> coordinate within the viewport, where <x> is the column of the<y> line
    //ESC [ s - ANSISYSSC       Save Cursor – Ansi.sys emulation	**With no parameters, performs a save cursor operation like DECSC
    //ESC [ u - ANSISYSRC       Restore Cursor – Ansi.sys emulation	**With no parameters, performs a restore cursor operation like DECRC
    //ESC [ ? 12 h - ATT160  Text Cursor Enable Blinking     Start the cursor blinking
    //ESC [ ? 12 l - ATT160  Text Cursor Disable Blinking    Stop blinking the cursor
    /// <summary>
    ///     ESC [ ? 25 h - DECTCEM Text Cursor Enable Mode Show    Show the cursor
    /// </summary>
    public static readonly string CSI_ShowCursor = CSI + "?25h";

    /// <summary>
    ///     ESC [ ? 25 l - DECTCEM Text Cursor Enable Mode Hide    Hide the cursor
    /// </summary>
    public static readonly string CSI_HideCursor = CSI + "?25l";

    //ESC [ ? 12 h - ATT160  Text Cursor Enable Blinking     Start the cursor blinking
    //ESC [ ? 12 l - ATT160  Text Cursor Disable Blinking    Stop blinking the cursor
    //ESC [ ? 25 h - DECTCEM Text Cursor Enable Mode Show    Show the cursor
    //ESC [ ? 25 l - DECTCEM Text Cursor Enable Mode Hide    Hide the cursor

    /// <summary>
    ///     Styles for ANSI ESC "[x q" - Set Cursor Style
    /// </summary>
    public enum DECSCUSR_Style
    {
        /// <summary>
        ///     DECSCUSR - User Shape - Default cursor shape configured by the user
        /// </summary>
        UserShape = 0,

        /// <summary>
        ///     DECSCUSR - Blinking Block - Blinking block cursor shape
        /// </summary>
        BlinkingBlock = 1,

        /// <summary>
        ///     DECSCUSR - Steady Block - Steady block cursor shape
        /// </summary>
        SteadyBlock = 2,

        /// <summary>
        ///     DECSCUSR - Blinking Underline - Blinking underline cursor shape
        /// </summary>
        BlinkingUnderline = 3,

        /// <summary>
        ///     DECSCUSR - Steady Underline - Steady underline cursor shape
        /// </summary>
        SteadyUnderline = 4,

        /// <summary>
        ///     DECSCUSR - Blinking Bar - Blinking bar cursor shape
        /// </summary>
        BlinkingBar = 5,

        /// <summary>
        ///     DECSCUSR - Steady Bar - Steady bar cursor shape
        /// </summary>
        SteadyBar = 6
    }

    /// <summary>
    ///     ESC [ n SP q - Select Cursor Style (DECSCUSR)
    ///     https://terminalguide.namepad.de/seq/csi_sq_t_space/
    /// </summary>
    /// <param name="style"></param>
    /// <returns></returns>
    public static string CSI_SetCursorStyle (DECSCUSR_Style style) { return $"{CSI}{(int)style} q"; }

    #endregion Cursor

    #region Colors

    /// <summary>
    ///     ESC [ (n) m - SGR - Set Graphics Rendition - Set the format of the screen and text as specified by (n)
    ///     This command is special in that the (n) position can accept between 0 and 16 parameters separated by semicolons.
    ///     When no parameters are specified, it is treated the same as a single 0 parameter.
    ///     https://terminalguide.namepad.de/seq/csi_sm/
    /// </summary>
    public static string CSI_SetGraphicsRendition (params int [] parameters) { return $"{CSI}{string.Join (";", parameters)}m"; }

    /// <summary>
    ///     ESC [ (n) m - Uses <see cref="CSI_SetGraphicsRendition(int[])"/> to set the foreground color.
    /// </summary>
    /// <param name="code">One of the 16 color codes.</param>
    /// <returns></returns>
    public static string CSI_SetForegroundColor (AnsiColorCode code) { return CSI_SetGraphicsRendition ((int)code); }

    /// <summary>
    ///     ESC [ (n) m - Uses <see cref="CSI_SetGraphicsRendition(int[])"/> to set the background color.
    /// </summary>
    /// <param name="code">One of the 16 color codes.</param>
    /// <returns></returns>
    public static string CSI_SetBackgroundColor (AnsiColorCode code) { return CSI_SetGraphicsRendition ((int)code + 10); }

    /// <summary>
    ///     ESC[38;5;{id}m - Set foreground color (256 colors)
    /// </summary>
    public static string CSI_SetForegroundColor256 (int color) { return $"{CSI}38;5;{color}m"; }

    /// <summary>
    ///     ESC[48;5;{id}m - Set background color (256 colors)
    /// </summary>
    public static string CSI_SetBackgroundColor256 (int color) { return $"{CSI}48;5;{color}m"; }

    /// <summary>
    ///     ESC[38;2;{r};{g};{b}m	Set foreground color as RGB.
    /// </summary>
    public static string CSI_SetForegroundColorRGB (int r, int g, int b) { return $"{CSI}38;2;{r};{g};{b}m"; }

    /// <summary>
    ///     ESC[38;2;{r};{g};{b}m	Append foreground color as RGB to StringBuilder.
    /// </summary>
    public static void CSI_AppendForegroundColorRGB (StringBuilder builder, int r, int g, int b)
    {
        // InterpolatedStringHandler is composed in stack, skipping the string allocation.
        builder.Append ($"{CSI}38;2;{r};{g};{b}m");
    }

    /// <summary>
    ///     ESC[48;2;{r};{g};{b}m	Set background color as RGB.
    /// </summary>
    public static string CSI_SetBackgroundColorRGB (int r, int g, int b) { return $"{CSI}48;2;{r};{g};{b}m"; }

    /// <summary>
    ///     ESC[48;2;{r};{g};{b}m	Append background color as RGB to StringBuilder.
    /// </summary>
    public static void CSI_AppendBackgroundColorRGB (StringBuilder builder, int r, int g, int b)
    {
        // InterpolatedStringHandler is composed in stack, skipping the string allocation.
        builder.Append ($"{CSI}48;2;{r};{g};{b}m");
    }

    #endregion Colors

    #region Text Styles

    /// <summary>
    ///     Appends an ANSI SGR (Select Graphic Rendition) escape sequence to switch printed text from one
    ///     <see cref="TextStyle"/> to another.
    /// </summary>
    /// <param name="output"><see cref="StringBuilder"/> to add escape sequence to.</param>
    /// <param name="prev">Previous <see cref="TextStyle"/> to change away from.</param>
    /// <param name="next">Next <see cref="TextStyle"/> to change to.</param>
    /// <remarks>
    ///     <para>
    ///         Unlike colors, most text styling options are not mutually exclusive with each other, and can be applied
    ///         independently. This creates a problem when
    ///         switching from one style to another: For instance, if your previous style is just bold, and your next style is
    ///         just italic, then simply adding the
    ///         sequence to enable italic text would cause the text to remain bold. This method automatically handles this
    ///         problem, enabling and disabling styles as
    ///         necessary to apply exactly the next style.
    ///     </para>
    /// </remarks>
    internal static void CSI_AppendTextStyleChange (StringBuilder output, TextStyle prev, TextStyle next)
    {
        // Do nothing if styles are the same, as no changes are necessary.
        if (prev == next)
        {
            return;
        }

        // Bitwise operations to determine flag changes. A ^ B are the flags different between two flag sets. These different flags that exist in the next flag
        // set (diff & next) are the ones that were enabled in the switch, those that exist in the previous flag set (diff & prev) are the ones that were
        // disabled.
        TextStyle diff = prev ^ next;
        TextStyle enabled = diff & next;
        TextStyle disabled = diff & prev;

        // List of escape codes to apply.
        List<int> sgr = new ();

        if (disabled != TextStyle.None)
        {
            // Special case: Both bold and faint have the same disabling code. While unusual, it can be valid to have both enabled at the same time, so when
            // one and only one of them is being disabled, we need to re-enable the other afterward. We can check what flags remain enabled by taking
            // prev & next, as this is the set of flags both have.
            if (disabled.HasFlag (TextStyle.Bold))
            {
                sgr.Add (22);

                if ((prev & next).HasFlag (TextStyle.Faint))
                {
                    sgr.Add (2);
                }
            }

            if (disabled.HasFlag (TextStyle.Faint))
            {
                sgr.Add (22);

                if ((prev & next).HasFlag (TextStyle.Bold))
                {
                    sgr.Add (1);
                }
            }

            if (disabled.HasFlag (TextStyle.Italic))
            {
                sgr.Add (23);
            }

            if (disabled.HasFlag (TextStyle.Underline))
            {
                sgr.Add (24);
            }

            if (disabled.HasFlag (TextStyle.Blink))
            {
                sgr.Add (25);
            }

            if (disabled.HasFlag (TextStyle.Reverse))
            {
                sgr.Add (27);
            }

            if (disabled.HasFlag (TextStyle.Strikethrough))
            {
                sgr.Add (29);
            }
        }

        if (enabled != TextStyle.None)
        {
            if (enabled.HasFlag (TextStyle.Bold))
            {
                sgr.Add (1);
            }

            if (enabled.HasFlag (TextStyle.Faint))
            {
                sgr.Add (2);
            }

            if (enabled.HasFlag (TextStyle.Italic))
            {
                sgr.Add (3);
            }

            if (enabled.HasFlag (TextStyle.Underline))
            {
                sgr.Add (4);
            }

            if (enabled.HasFlag (TextStyle.Blink))
            {
                sgr.Add (5);
            }

            if (enabled.HasFlag (TextStyle.Reverse))
            {
                sgr.Add (7);
            }

            if (enabled.HasFlag (TextStyle.Strikethrough))
            {
                sgr.Add (9);
            }
        }

        output.Append ("\x1b[");
        output.Append (string.Join (';', sgr));
        output.Append ('m');
    }

    #endregion Text Styles

    #region Requests

    /// <summary>
    ///     ESC [ ? 6 n - Request Cursor Position Report (?) (DECXCPR)
    ///     https://terminalguide.namepad.de/seq/csi_sn__p-6/
    ///     The terminal reply to <see cref="CSI_RequestCursorPositionReport"/>. ESC [ ? (y) ; (x) ; 1 R
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_RequestCursorPositionReport = new () { Request = CSI + "?6n", Terminator = "R" };

    /// <summary>
    ///     The terminal reply to <see cref="CSI_RequestCursorPositionReport"/>. ESC [ ? (y) ; (x) R
    /// </summary>
    public const string CSI_RequestCursorPositionReport_Terminator = "R";

    /// <summary>
    ///     ESC [ 0 c - Send Device Attributes (Primary DA)
    ///     https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Application-Program-Command-functions
    ///     https://www.xfree86.org/current/ctlseqs.html
    ///     Windows Terminal v1.17 and below emits “\x1b[?1;0c”, indicating "VT101 with No Options".
    ///     Windows Terminal v1.18+ emits: \x1b[?61;6;7;22;23;24;28;32;42c"
    ///     See https://github.com/microsoft/terminal/pull/14906
    ///     61 - The device conforms to level 1 of the character cell display architecture
    ///     (See https://github.com/microsoft/terminal/issues/15693#issuecomment-1633304497)
    ///     6 = Selective erase
    ///     7 = Soft fonts
    ///     22 = Color text
    ///     23 = Greek character sets
    ///     24 = Turkish character sets
    ///     28 = Rectangular area operations
    ///     32 = Text macros
    ///     42 = ISO Latin-2 character set
    ///     The terminator indicating a reply to <see cref="CSI_SendDeviceAttributes"/> or
    ///     <see cref="CSI_SendDeviceAttributes2"/>
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_SendDeviceAttributes = new () { Request = CSI + "0c", Terminator = "c" };

    /// <summary>
    ///     ESC [ > 0 c - Send Device Attributes (Secondary DA)
    ///     Windows Terminal v1.18+ emits: "\x1b[>0;10;1c" (vt100, firmware version 1.0, vt220)
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_SendDeviceAttributes2 = new () { Request = CSI + ">0c", Terminator = "c" };

    /// <summary>
    ///     CSI 16 t - Request sixel resolution (width and height in pixels)
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_RequestSixelResolution = new () { Request = CSI + "16t", Terminator = "t" };

    /// <summary>
    ///     CSI 14 t - Request window size in pixels (width x height)
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_RequestWindowSizeInPixels = new () { Request = CSI + "14t", Terminator = "t" };

    /// <summary>
    ///     CSI 1 8 t  | yes | yes |  yes  | report window size in chars
    ///     https://terminalguide.namepad.de/seq/csi_st-18/
    ///     The terminator indicating a reply to <see cref="CSI_ReportWindowSizeInChars"/> : ESC [ 8 ; height ; width t
    /// </summary>
    public static readonly AnsiEscapeSequence CSI_ReportWindowSizeInChars = new () { Request = CSI + "18t", Terminator = "t", Value = "8" };

    /// <summary>
    ///     The terminator indicating a reply to <see cref="CSI_ReportWindowSizeInChars"/> : ESC [ 8 ; height ; width t
    /// </summary>
    public const string CSI_ReportWindowSizeInChars_Terminator = "t";

    /// <summary>
    ///     The value of the response to <see cref="CSI_ReportWindowSizeInChars"/> indicating value 1 and 2 are the terminal
    ///     size in chars.
    /// </summary>
    public const string CSI_ReportWindowSizeInChars_ResponseValue = "8";

    #endregion Requests

    #region OSC

    /// <summary>
    ///     OSC (Operating System Command) escape sequence prefix.
    /// </summary>
    /// <remarks>
    ///     OSC sequences are used for operating system-specific commands like setting window title,
    ///     hyperlinks (OSC 8), and other terminal emulator features.
    /// </remarks>
    public const string OSC = "\u001B]";

    /// <summary>
    ///     String Terminator (ST) - terminates OSC sequences.
    /// </summary>
    /// <remarks>
    ///     Can also be represented as BEL (0x07) in some terminals, but ST is more modern.
    /// </remarks>
    public const string ST = "\u001B\\";

    /// <summary>
    ///     Starts a hyperlink using OSC 8 escape sequence.
    /// </summary>
    /// <param name="url">The URL to link to (e.g., "https://github.com").</param>
    /// <param name="id">Optional hyperlink ID for matching start/end pairs. Use null for automatic matching.</param>
    /// <returns>The OSC 8 start sequence.</returns>
    /// <remarks>
    ///     OSC 8 format: ESC ] 8 ; params ; URL ST
    ///     Supported in Windows Terminal, iTerm2, and other modern terminals.
    ///     Must be followed by visible text, then terminated with <see cref="OSC_EndHyperlink"/>.
    /// </remarks>
    public static string OSC_StartHyperlink (string url, string? id = null)
    {
        // Format: ESC ] 8 ; params ; URL ST
        // params can include "id=value" for matching start/end
        string parameters = string.IsNullOrEmpty (id) ? "" : $"id={id}";

        return $"{OSC}8;{parameters};{url}{ST}";
    }

    /// <summary>
    ///     Ends a hyperlink using OSC 8 escape sequence.
    /// </summary>
    /// <returns>The OSC 8 end sequence.</returns>
    /// <remarks>
    ///     This terminates the hyperlink started by <see cref="OSC_StartHyperlink"/>.
    ///     Format: ESC ] 8 ; ; ST (empty URL ends the hyperlink).
    /// </remarks>
    public static string OSC_EndHyperlink ()
    {
        // Format: ESC ] 8 ; ; ST (empty URL ends hyperlink)
        return $"{OSC}8;;{ST}";
    }

    #endregion OSC

    /// <summary>
    ///     Convert a <see cref="ConsoleKeyInfo"/> array to string.
    /// </summary>
    /// <param name="consoleKeyInfos"></param>
    /// <returns>The string representing the array.</returns>
    public static string ToString (ConsoleKeyInfo [] consoleKeyInfos)
    {
        StringBuilder sb = new ();

        foreach (ConsoleKeyInfo keyChar in consoleKeyInfos)
        {
            sb.Append (keyChar.KeyChar);
        }

        return sb.ToString ();
    }
}
