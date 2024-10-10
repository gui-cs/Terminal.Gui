namespace Terminal.Gui;

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
    ///     Escape key code (ASCII 27/0x1B).
    /// </summary>
    public const char KeyEsc = (char)KeyCode.Esc;

    /// <summary>
    ///     ESC [ - The CSI (Control Sequence Introducer).
    /// </summary>
    public const string CSI = "\u001B[";

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

    //private static bool isButtonReleased;
    private static bool isButtonClicked;

    private static bool isButtonDoubleClicked;

    //private static MouseFlags? lastMouseButtonReleased;
    // QUESTION: What's the difference between isButtonClicked and isButtonPressed?
    // Some clarity or comments would be handy, here.
    // It also seems like some enforcement of valid states might be a good idea.
    private static bool isButtonPressed;
    private static bool isButtonTripleClicked;

    private static MouseFlags? lastMouseButtonPressed;
    private static Point? point;

    /// <summary>
    ///     Control sequence for disabling mouse events.
    /// </summary>
    public static string CSI_DisableMouseEvents { get; set; } =
        CSI_DisableAnyEventMouse + CSI_DisableUrxvtExtModeMouse + CSI_DisableSgrExtModeMouse;

    /// <summary>
    ///     Control sequence for enabling mouse events.
    /// </summary>
    public static string CSI_EnableMouseEvents { get; set; } =
        CSI_EnableAnyEventMouse + CSI_EnableUrxvtExtModeMouse + CSI_EnableSgrExtModeMouse;

    /// <summary>
    ///     ESC [ x J - Clears part of the screen. See <see cref="ClearScreenOptions"/>.
    /// </summary>
    /// <param name="option"></param>
    /// <returns></returns>
    public static string CSI_ClearScreen (ClearScreenOptions option) { return $"{CSI}{(int)option}J"; }

    /// <summary>
    ///     Decodes an ANSI escape sequence.
    /// </summary>
    /// <param name="escSeqRequests">The <see cref="EscSeqRequests"/> which may contain a request.</param>
    /// <param name="newConsoleKeyInfo">The <see cref="ConsoleKeyInfo"/> which may change.</param>
    /// <param name="key">The <see cref="ConsoleKey"/> which may change.</param>
    /// <param name="cki">The <see cref="ConsoleKeyInfo"/> array.</param>
    /// <param name="mod">The <see cref="ConsoleModifiers"/> which may change.</param>
    /// <param name="c1Control">The control returned by the <see cref="GetC1ControlChar"/> method.</param>
    /// <param name="code">The code returned by the <see cref="GetEscapeResult(char[])"/> method.</param>
    /// <param name="values">The values returned by the <see cref="GetEscapeResult(char[])"/> method.</param>
    /// <param name="terminator">The terminator returned by the <see cref="GetEscapeResult(char[])"/> method.</param>
    /// <param name="isMouse">Indicates if the escape sequence is a mouse event.</param>
    /// <param name="buttonState">The <see cref="MouseFlags"/> button state.</param>
    /// <param name="pos">The <see cref="MouseFlags"/> position.</param>
    /// <param name="isResponse">Indicates if the escape sequence is a response to a request.</param>
    /// <param name="continuousButtonPressedHandler">The handler that will process the event.</param>
    public static void DecodeEscSeq (
        EscSeqRequests escSeqRequests,
        ref ConsoleKeyInfo newConsoleKeyInfo,
        ref ConsoleKey key,
        ConsoleKeyInfo [] cki,
        ref ConsoleModifiers mod,
        out string c1Control,
        out string code,
        out string [] values,
        out string terminator,
        out bool isMouse,
        out List<MouseFlags> buttonState,
        out Point pos,
        out bool isResponse,
        Action<MouseFlags, Point> continuousButtonPressedHandler
    )
    {
        char [] kChars = GetKeyCharArray (cki);
        (c1Control, code, values, terminator) = GetEscapeResult (kChars);
        isMouse = false;
        buttonState = new List<MouseFlags> { 0 };
        pos = default (Point);
        isResponse = false;
        char keyChar = '\0';

        switch (c1Control)
        {
            case "ESC":
                if (values is null && string.IsNullOrEmpty (terminator))
                {
                    key = ConsoleKey.Escape;

                    newConsoleKeyInfo = new ConsoleKeyInfo (
                                                            cki [0].KeyChar,
                                                            key,
                                                            (mod & ConsoleModifiers.Shift) != 0,
                                                            (mod & ConsoleModifiers.Alt) != 0,
                                                            (mod & ConsoleModifiers.Control) != 0);
                }
                else if ((uint)cki [1].KeyChar >= 1 && (uint)cki [1].KeyChar <= 26)
                {
                    key = (ConsoleKey)(char)(cki [1].KeyChar + (uint)ConsoleKey.A - 1);

                    newConsoleKeyInfo = new ConsoleKeyInfo (
                                                            cki [1].KeyChar,
                                                            key,
                                                            false,
                                                            true,
                                                            true);
                }
                else
                {
                    if (cki [1].KeyChar >= 97 && cki [1].KeyChar <= 122)
                    {
                        key = (ConsoleKey)cki [1].KeyChar.ToString ().ToUpper () [0];
                    }
                    else
                    {
                        key = (ConsoleKey)cki [1].KeyChar;
                    }

                    newConsoleKeyInfo = new ConsoleKeyInfo (
                                                            (char)key,
                                                            (ConsoleKey)Math.Min ((uint)key, 255),
                                                            false,
                                                            true,
                                                            false);
                }

                break;
            case "SS3":
                key = GetConsoleKey (terminator [0], values [0], ref mod, ref keyChar);

                newConsoleKeyInfo = new ConsoleKeyInfo (
                                                        keyChar,
                                                        key,
                                                        (mod & ConsoleModifiers.Shift) != 0,
                                                        (mod & ConsoleModifiers.Alt) != 0,
                                                        (mod & ConsoleModifiers.Control) != 0);

                break;
            case "CSI":
                if (!string.IsNullOrEmpty (code) && code == "<")
                {
                    GetMouse (cki, out buttonState, out pos, continuousButtonPressedHandler);
                    isMouse = true;

                    return;
                }

                if (escSeqRequests is { } && escSeqRequests.HasResponse (terminator))
                {
                    isResponse = true;
                    escSeqRequests.Remove (terminator);

                    return;
                }

                if (!string.IsNullOrEmpty (terminator))
                {
                    key = GetConsoleKey (terminator [0], values [0], ref mod, ref keyChar);

                    if (key != 0 && values.Length > 1)
                    {
                        mod |= GetConsoleModifiers (values [1]);
                    }

                    newConsoleKeyInfo = new ConsoleKeyInfo (
                                                            keyChar,
                                                            key,
                                                            (mod & ConsoleModifiers.Shift) != 0,
                                                            (mod & ConsoleModifiers.Alt) != 0,
                                                            (mod & ConsoleModifiers.Control) != 0);
                }
                else
                {
                    // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/2803
                    // This is caused by NetDriver depending on Console.KeyAvailable?
                    throw new InvalidOperationException ("CSI response, but there's no terminator");

                    //newConsoleKeyInfo = new ConsoleKeyInfo ('\0',
                    //	key,
                    //	(mod & ConsoleModifiers.Shift) != 0,
                    //	(mod & ConsoleModifiers.Alt) != 0,
                    //	(mod & ConsoleModifiers.Control) != 0);
                }

                break;
        }
    }

    #nullable enable
    /// <summary>
    ///     Gets the c1Control used in the called escape sequence.
    /// </summary>
    /// <param name="c">The char used.</param>
    /// <returns>The c1Control.</returns>
    [Pure]
    public static string GetC1ControlChar (in char c)
    {
        // These control characters are used in the vtXXX emulation.
        return c switch
               {
                   'D' => "IND", // Index
                   'E' => "NEL", // Next Line
                   'H' => "HTS", // Tab Set
                   'M' => "RI", // Reverse Index
                   'N' => "SS2", // Single Shift Select of G2 Character Set: affects next character only
                   'O' => "SS3", // Single Shift Select of G3 Character Set: affects next character only
                   'P' => "DCS", // Device Control String
                   'V' => "SPA", // Start of Guarded Area
                   'W' => "EPA", // End of Guarded Area
                   'X' => "SOS", // Start of String
                   'Z' => "DECID", // Return Terminal ID Obsolete form of CSI c (DA)
                   '[' => "CSI", // Control Sequence Introducer
                   '\\' => "ST", // String Terminator
                   ']' => "OSC", // Operating System Command
                   '^' => "PM", // Privacy Message
                   '_' => "APC", // Application Program Command
                   _ => string.Empty
               };
    }

    /// <summary>
    ///     Gets the <see cref="ConsoleKey"/> depending on terminating and value.
    /// </summary>
    /// <param name="terminator">
    ///     The terminator indicating a reply to <see cref="CSI_SendDeviceAttributes"/> or
    ///     <see cref="CSI_SendDeviceAttributes2"/>.
    /// </param>
    /// <param name="value">The value.</param>
    /// <param name="mod">The <see cref="ConsoleModifiers"/> which may change.</param>
    /// <param name="keyChar">Normally is '\0' but on some cases may need other value.</param>
    /// <returns>The <see cref="ConsoleKey"/> and probably the <see cref="ConsoleModifiers"/>.</returns>
    public static ConsoleKey GetConsoleKey (char terminator, string? value, ref ConsoleModifiers mod, ref char keyChar)
    {
        if (terminator == 'Z')
        {
            mod |= ConsoleModifiers.Shift;
        }

        if (terminator == 'l')
        {
            keyChar = '+';
        }

        if (terminator == 'm')
        {
            keyChar = '-';
        }

        return (terminator, value) switch
               {
                   ('A', _) => ConsoleKey.UpArrow,
                   ('B', _) => ConsoleKey.DownArrow,
                   ('C', _) => ConsoleKey.RightArrow,
                   ('D', _) => ConsoleKey.LeftArrow,
                   ('F', _) => ConsoleKey.End,
                   ('H', _) => ConsoleKey.Home,
                   ('P', _) => ConsoleKey.F1,
                   ('Q', _) => ConsoleKey.F2,
                   ('R', _) => ConsoleKey.F3,
                   ('S', _) => ConsoleKey.F4,
                   ('Z', _) => ConsoleKey.Tab,
                   ('~', "2") => ConsoleKey.Insert,
                   ('~', "3") => ConsoleKey.Delete,
                   ('~', "5") => ConsoleKey.PageUp,
                   ('~', "6") => ConsoleKey.PageDown,
                   ('~', "15") => ConsoleKey.F5,
                   ('~', "17") => ConsoleKey.F6,
                   ('~', "18") => ConsoleKey.F7,
                   ('~', "19") => ConsoleKey.F8,
                   ('~', "20") => ConsoleKey.F9,
                   ('~', "21") => ConsoleKey.F10,
                   ('~', "23") => ConsoleKey.F11,
                   ('~', "24") => ConsoleKey.F12,
                   ('l', _) => ConsoleKey.Add,
                   ('m', _) => ConsoleKey.Subtract,
                   ('p', _) => ConsoleKey.Insert,
                   ('q', _) => ConsoleKey.End,
                   ('r', _) => ConsoleKey.DownArrow,
                   ('s', _) => ConsoleKey.PageDown,
                   ('t', _) => ConsoleKey.LeftArrow,
                   ('u', _) => ConsoleKey.Clear,
                   ('v', _) => ConsoleKey.RightArrow,
                   ('w', _) => ConsoleKey.Home,
                   ('x', _) => ConsoleKey.UpArrow,
                   ('y', _) => ConsoleKey.PageUp,
                   (_, _) => 0
               };
    }

    /// <summary>
    ///     Gets the <see cref="ConsoleModifiers"/> from the value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The <see cref="ConsoleModifiers"/> or zero.</returns>
    public static ConsoleModifiers GetConsoleModifiers (string? value)
    {
        return value switch
               {
                   "2" => ConsoleModifiers.Shift,
                   "3" => ConsoleModifiers.Alt,
                   "4" => ConsoleModifiers.Shift | ConsoleModifiers.Alt,
                   "5" => ConsoleModifiers.Control,
                   "6" => ConsoleModifiers.Shift | ConsoleModifiers.Control,
                   "7" => ConsoleModifiers.Alt | ConsoleModifiers.Control,
                   "8" => ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control,
                   _ => 0
               };
    }
    #nullable restore

    /// <summary>
    ///     Gets all the needed information about an escape sequence.
    /// </summary>
    /// <param name="kChar">The array with all chars.</param>
    /// <returns>
    ///     The c1Control returned by <see cref="GetC1ControlChar"/>, code, values and terminating.
    /// </returns>
    public static (string c1Control, string code, string [] values, string terminating) GetEscapeResult (char [] kChar)
    {
        if (kChar is null || kChar.Length == 0)
        {
            return (null, null, null, null);
        }

        if (kChar [0] != KeyEsc)
        {
            throw new InvalidOperationException ("Invalid escape character!");
        }

        if (kChar.Length == 1)
        {
            return ("ESC", null, null, null);
        }

        if (kChar.Length == 2)
        {
            return ("ESC", null, null, kChar [1].ToString ());
        }

        string c1Control = GetC1ControlChar (kChar [1]);
        string code = null;
        int nSep = kChar.Count (static x => x == ';') + 1;
        var values = new string [nSep];
        var valueIdx = 0;
        var terminating = string.Empty;

        for (var i = 2; i < kChar.Length; i++)
        {
            char c = kChar [i];

            if (char.IsDigit (c))
            {
                // PERF: Ouch
                values [valueIdx] += c.ToString ();
            }
            else if (c == ';')
            {
                valueIdx++;
            }
            else if (valueIdx == nSep - 1 || i == kChar.Length - 1)
            {
                // PERF: Ouch
                terminating += c.ToString ();
            }
            else
            {
                // PERF: Ouch
                code += c.ToString ();
            }
        }

        return (c1Control, code, values, terminating);
    }

    /// <summary>
    ///     A helper to get only the <see cref="ConsoleKeyInfo.KeyChar"/> from the <see cref="ConsoleKeyInfo"/> array.
    /// </summary>
    /// <param name="cki"></param>
    /// <returns>The char array of the escape sequence.</returns>
    // PERF: This is expensive
    public static char [] GetKeyCharArray (ConsoleKeyInfo [] cki)
    {
        char [] kChar = { };
        var length = 0;

        foreach (ConsoleKeyInfo kc in cki)
        {
            length++;
            Array.Resize (ref kChar, length);
            kChar [length - 1] = kc.KeyChar;
        }

        return kChar;
    }

    /// <summary>
    ///     Gets the <see cref="MouseFlags"/> mouse button flags and the position.
    /// </summary>
    /// <param name="cki">The <see cref="ConsoleKeyInfo"/> array.</param>
    /// <param name="mouseFlags">The mouse button flags.</param>
    /// <param name="pos">The mouse position.</param>
    /// <param name="continuousButtonPressedHandler">The handler that will process the event.</param>
    public static void GetMouse (
        ConsoleKeyInfo [] cki,
        out List<MouseFlags> mouseFlags,
        out Point pos,
        Action<MouseFlags, Point> continuousButtonPressedHandler
    )
    {
        MouseFlags buttonState = 0;
        pos = Point.Empty;
        var buttonCode = 0;
        var foundButtonCode = false;
        var foundPoint = 0;
        string value = string.Empty;
        char [] kChar = GetKeyCharArray (cki);

        // PERF: This loop could benefit from use of Spans and other strategies to avoid copies.
        //System.Diagnostics.Debug.WriteLine ($"kChar: {new string (kChar)}");
        for (var i = 0; i < kChar.Length; i++)
        {
            // PERF: Copy
            char c = kChar [i];

            if (c == '<')
            {
                foundButtonCode = true;
            }
            else if (foundButtonCode && c != ';')
            {
                // PERF: Ouch
                value += c.ToString ();
            }
            else if (c == ';')
            {
                if (foundButtonCode)
                {
                    foundButtonCode = false;
                    buttonCode = int.Parse (value);
                }

                if (foundPoint == 1)
                {
                    pos.X = int.Parse (value) - 1;
                }

                value = string.Empty;
                foundPoint++;
            }
            else if (foundPoint > 0 && c != 'm' && c != 'M')
            {
                value += c.ToString ();
            }
            else if (c == 'm' || c == 'M')
            {
                //pos.Y = int.Parse (value) + Console.WindowTop - 1;
                pos.Y = int.Parse (value) - 1;

                switch (buttonCode)
                {
                    case 0:
                    case 8:
                    case 16:
                    case 24:
                    case 32:
                    case 36:
                    case 40:
                    case 48:
                    case 56:
                        buttonState = c == 'M'
                                          ? MouseFlags.Button1Pressed
                                          : MouseFlags.Button1Released;

                        break;
                    case 1:
                    case 9:
                    case 17:
                    case 25:
                    case 33:
                    case 37:
                    case 41:
                    case 45:
                    case 49:
                    case 53:
                    case 57:
                    case 61:
                        buttonState = c == 'M'
                                          ? MouseFlags.Button2Pressed
                                          : MouseFlags.Button2Released;

                        break;
                    case 2:
                    case 10:
                    case 14:
                    case 18:
                    case 22:
                    case 26:
                    case 30:
                    case 34:
                    case 42:
                    case 46:
                    case 50:
                    case 54:
                    case 58:
                    case 62:
                        buttonState = c == 'M'
                                          ? MouseFlags.Button3Pressed
                                          : MouseFlags.Button3Released;

                        break;
                    case 35:
                    //// Needed for Windows OS
                    //if (isButtonPressed && c == 'm'
                    //	&& (lastMouseEvent.ButtonState == MouseFlags.Button1Pressed
                    //	|| lastMouseEvent.ButtonState == MouseFlags.Button2Pressed
                    //	|| lastMouseEvent.ButtonState == MouseFlags.Button3Pressed)) {

                    //	switch (lastMouseEvent.ButtonState) {
                    //	case MouseFlags.Button1Pressed:
                    //		buttonState = MouseFlags.Button1Released;
                    //		break;
                    //	case MouseFlags.Button2Pressed:
                    //		buttonState = MouseFlags.Button2Released;
                    //		break;
                    //	case MouseFlags.Button3Pressed:
                    //		buttonState = MouseFlags.Button3Released;
                    //		break;
                    //	}
                    //} else {
                    //	buttonState = MouseFlags.ReportMousePosition;
                    //}
                    //break;
                    case 39:
                    case 43:
                    case 47:
                    case 51:
                    case 55:
                    case 59:
                    case 63:
                        buttonState = MouseFlags.ReportMousePosition;

                        break;
                    case 64:
                        buttonState = MouseFlags.WheeledUp;

                        break;
                    case 65:
                        buttonState = MouseFlags.WheeledDown;

                        break;
                    case 68:
                    case 72:
                    case 80:
                        buttonState = MouseFlags.WheeledLeft; // Shift/Ctrl+WheeledUp

                        break;
                    case 69:
                    case 73:
                    case 81:
                        buttonState = MouseFlags.WheeledRight; // Shift/Ctrl+WheeledDown

                        break;
                }

                // Modifiers.
                switch (buttonCode)
                {
                    case 8:
                    case 9:
                    case 10:
                    case 43:
                        buttonState |= MouseFlags.ButtonAlt;

                        break;
                    case 14:
                    case 47:
                        buttonState |= MouseFlags.ButtonAlt | MouseFlags.ButtonShift;

                        break;
                    case 16:
                    case 17:
                    case 18:
                    case 51:
                        buttonState |= MouseFlags.ButtonCtrl;

                        break;
                    case 22:
                    case 55:
                        buttonState |= MouseFlags.ButtonCtrl | MouseFlags.ButtonShift;

                        break;
                    case 24:
                    case 25:
                    case 26:
                    case 59:
                        buttonState |= MouseFlags.ButtonCtrl | MouseFlags.ButtonAlt;

                        break;
                    case 30:
                    case 63:
                        buttonState |= MouseFlags.ButtonCtrl | MouseFlags.ButtonShift | MouseFlags.ButtonAlt;

                        break;
                    case 32:
                    case 33:
                    case 34:
                        buttonState |= MouseFlags.ReportMousePosition;

                        break;
                    case 36:
                    case 37:
                        buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonShift;

                        break;
                    case 39:
                    case 68:
                    case 69:
                        buttonState |= MouseFlags.ButtonShift;

                        break;
                    case 40:
                    case 41:
                    case 42:
                        buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonAlt;

                        break;
                    case 45:
                    case 46:
                        buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonAlt | MouseFlags.ButtonShift;

                        break;
                    case 48:
                    case 49:
                    case 50:
                        buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl;

                        break;
                    case 53:
                    case 54:
                        buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl | MouseFlags.ButtonShift;

                        break;
                    case 56:
                    case 57:
                    case 58:
                        buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl | MouseFlags.ButtonAlt;

                        break;
                    case 61:
                    case 62:
                        buttonState |= MouseFlags.ReportMousePosition | MouseFlags.ButtonCtrl | MouseFlags.ButtonShift | MouseFlags.ButtonAlt;

                        break;
                }
            }
        }

        mouseFlags = [MouseFlags.AllEvents];

        if (lastMouseButtonPressed != null
            && !isButtonPressed
            && !buttonState.HasFlag (MouseFlags.ReportMousePosition)
            && !buttonState.HasFlag (MouseFlags.Button1Released)
            && !buttonState.HasFlag (MouseFlags.Button2Released)
            && !buttonState.HasFlag (MouseFlags.Button3Released)
            && !buttonState.HasFlag (MouseFlags.Button4Released))
        {
            lastMouseButtonPressed = null;
            isButtonPressed = false;
        }

        if ((!isButtonClicked
             && !isButtonDoubleClicked
             && (buttonState == MouseFlags.Button1Pressed
                 || buttonState == MouseFlags.Button2Pressed
                 || buttonState == MouseFlags.Button3Pressed
                 || buttonState == MouseFlags.Button4Pressed)
             && lastMouseButtonPressed is null)
            || (isButtonPressed && lastMouseButtonPressed is { } && buttonState.HasFlag (MouseFlags.ReportMousePosition)))
        {
            mouseFlags [0] = buttonState;
            lastMouseButtonPressed = buttonState;
            isButtonPressed = true;

            point = pos;

            if ((mouseFlags [0] & MouseFlags.ReportMousePosition) == 0)
            {
                Application.MainLoop.AddIdle (
                                              () =>
                                              {
                                                  // INTENT: What's this trying to do?
                                                  // The task itself is not awaited.
                                                  Task.Run (
                                                            async () => await ProcessContinuousButtonPressedAsync (
                                                                         buttonState,
                                                                         continuousButtonPressedHandler));

                                                  return false;
                                              });
            }
            else if (mouseFlags [0].HasFlag (MouseFlags.ReportMousePosition))
            {
                point = pos;

                // The isButtonPressed must always be true, otherwise we can lose the feature
                // If mouse flags has ReportMousePosition this feature won't run
                // but is always prepared with the new location
                //isButtonPressed = false;
            }
        }
        else if (isButtonDoubleClicked
                 && (buttonState == MouseFlags.Button1Pressed
                     || buttonState == MouseFlags.Button2Pressed
                     || buttonState == MouseFlags.Button3Pressed
                     || buttonState == MouseFlags.Button4Pressed))
        {
            mouseFlags [0] = GetButtonTripleClicked (buttonState);
            isButtonDoubleClicked = false;
            isButtonTripleClicked = true;
        }
        else if (isButtonClicked
                 && (buttonState == MouseFlags.Button1Pressed
                     || buttonState == MouseFlags.Button2Pressed
                     || buttonState == MouseFlags.Button3Pressed
                     || buttonState == MouseFlags.Button4Pressed))
        {
            mouseFlags [0] = GetButtonDoubleClicked (buttonState);
            isButtonClicked = false;
            isButtonDoubleClicked = true;

            Application.MainLoop.AddIdle (
                                          () =>
                                          {
                                              Task.Run (async () => await ProcessButtonDoubleClickedAsync ());

                                              return false;
                                          });
        }

        //else if (isButtonReleased && !isButtonClicked && buttonState == MouseFlags.ReportMousePosition) {
        //	mouseFlag [0] = GetButtonClicked ((MouseFlags)lastMouseButtonReleased);
        //	lastMouseButtonReleased = null;
        //	isButtonReleased = false;
        //	isButtonClicked = true;
        //	Application.MainLoop.AddIdle (() => {
        //		Task.Run (async () => await ProcessButtonClickedAsync ());
        //		return false;
        //	});

        //} 
        else if (!isButtonClicked
                 && !isButtonDoubleClicked
                 && (buttonState == MouseFlags.Button1Released
                     || buttonState == MouseFlags.Button2Released
                     || buttonState == MouseFlags.Button3Released
                     || buttonState == MouseFlags.Button4Released))
        {
            mouseFlags [0] = buttonState;
            isButtonPressed = false;

            if (isButtonTripleClicked)
            {
                isButtonTripleClicked = false;
            }
            else if (pos.X == point?.X && pos.Y == point?.Y)
            {
                mouseFlags.Add (GetButtonClicked (buttonState));
                isButtonClicked = true;

                Application.MainLoop.AddIdle (
                                              () =>
                                              {
                                                  Task.Run (async () => await ProcessButtonClickedAsync ());

                                                  return false;
                                              });
            }

            point = pos;

            //if ((lastMouseButtonPressed & MouseFlags.ReportMousePosition) == 0) {
            //	lastMouseButtonReleased = buttonState;
            //	isButtonPressed = false;
            //	isButtonReleased = true;
            //} else {
            //	lastMouseButtonPressed = null;
            //	isButtonPressed = false;
            //}
        }
        else if (buttonState == MouseFlags.WheeledUp)
        {
            mouseFlags [0] = MouseFlags.WheeledUp;
        }
        else if (buttonState == MouseFlags.WheeledDown)
        {
            mouseFlags [0] = MouseFlags.WheeledDown;
        }
        else if (buttonState == MouseFlags.WheeledLeft)
        {
            mouseFlags [0] = MouseFlags.WheeledLeft;
        }
        else if (buttonState == MouseFlags.WheeledRight)
        {
            mouseFlags [0] = MouseFlags.WheeledRight;
        }
        else if (buttonState == MouseFlags.ReportMousePosition)
        {
            mouseFlags [0] = MouseFlags.ReportMousePosition;
        }
        else
        {
            mouseFlags [0] = buttonState;

            //foreach (var flag in buttonState.GetUniqueFlags()) {
            //	mouseFlag [0] |= flag;
            //}
        }

        mouseFlags [0] = SetControlKeyStates (buttonState, mouseFlags [0]);

        //buttonState = mouseFlags;

        //System.Diagnostics.Debug.WriteLine ($"buttonState: {buttonState} X: {pos.X} Y: {pos.Y}");
        //foreach (var mf in mouseFlags) {
        //	System.Diagnostics.Debug.WriteLine ($"mouseFlags: {mf} X: {pos.X} Y: {pos.Y}");
        //}
    }

    /// <summary>
    ///     Ensures a console key is mapped to one that works correctly with ANSI escape sequences.
    /// </summary>
    /// <param name="consoleKeyInfo">The <see cref="ConsoleKeyInfo"/>.</param>
    /// <returns>The <see cref="ConsoleKeyInfo"/> modified.</returns>
    public static ConsoleKeyInfo MapConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
    {
        ConsoleKeyInfo newConsoleKeyInfo = consoleKeyInfo;
        ConsoleKey key;
        char keyChar = consoleKeyInfo.KeyChar;

        switch ((uint)keyChar)
        {
            case 0:
                if (consoleKeyInfo.Key == (ConsoleKey)64)
                { // Ctrl+Space in Windows.
                    newConsoleKeyInfo = new ConsoleKeyInfo (
                                                            ' ',
                                                            ConsoleKey.Spacebar,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }

                break;
            case uint n when n > 0 && n <= KeyEsc:
                if (consoleKeyInfo.Key == 0 && consoleKeyInfo.KeyChar == '\r')
                {
                    key = ConsoleKey.Enter;

                    newConsoleKeyInfo = new ConsoleKeyInfo (
                                                            consoleKeyInfo.KeyChar,
                                                            key,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);
                }
                else if (consoleKeyInfo.Key == 0)
                {
                    key = (ConsoleKey)(char)(consoleKeyInfo.KeyChar + (uint)ConsoleKey.A - 1);

                    newConsoleKeyInfo = new ConsoleKeyInfo (
                                                            (char)key,
                                                            key,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                            (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                            true);
                }

                break;
            case 127: // DEL
                newConsoleKeyInfo = new ConsoleKeyInfo (
                                                        consoleKeyInfo.KeyChar,
                                                        ConsoleKey.Backspace,
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0,
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0,
                                                        (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0);

                break;
            default:
                newConsoleKeyInfo = consoleKeyInfo;

                break;
        }

        return newConsoleKeyInfo;
    }

    /// <summary>
    ///     A helper to resize the <see cref="ConsoleKeyInfo"/> as needed.
    /// </summary>
    /// <param name="consoleKeyInfo">The <see cref="ConsoleKeyInfo"/>.</param>
    /// <param name="cki">The <see cref="ConsoleKeyInfo"/> array to resize.</param>
    /// <returns>The <see cref="ConsoleKeyInfo"/> resized.</returns>
    public static ConsoleKeyInfo [] ResizeArray (ConsoleKeyInfo consoleKeyInfo, ConsoleKeyInfo [] cki)
    {
        Array.Resize (ref cki, cki is null ? 1 : cki.Length + 1);
        cki [cki.Length - 1] = consoleKeyInfo;

        return cki;
    }

    private static MouseFlags GetButtonClicked (MouseFlags mouseFlag)
    {
        MouseFlags mf = default;

        switch (mouseFlag)
        {
            case MouseFlags.Button1Released:
                mf = MouseFlags.Button1Clicked;

                break;

            case MouseFlags.Button2Released:
                mf = MouseFlags.Button2Clicked;

                break;

            case MouseFlags.Button3Released:
                mf = MouseFlags.Button3Clicked;

                break;
        }

        return mf;
    }

    private static MouseFlags GetButtonDoubleClicked (MouseFlags mouseFlag)
    {
        MouseFlags mf = default;

        switch (mouseFlag)
        {
            case MouseFlags.Button1Pressed:
                mf = MouseFlags.Button1DoubleClicked;

                break;

            case MouseFlags.Button2Pressed:
                mf = MouseFlags.Button2DoubleClicked;

                break;

            case MouseFlags.Button3Pressed:
                mf = MouseFlags.Button3DoubleClicked;

                break;
        }

        return mf;
    }

    private static MouseFlags GetButtonTripleClicked (MouseFlags mouseFlag)
    {
        MouseFlags mf = default;

        switch (mouseFlag)
        {
            case MouseFlags.Button1Pressed:
                mf = MouseFlags.Button1TripleClicked;

                break;

            case MouseFlags.Button2Pressed:
                mf = MouseFlags.Button2TripleClicked;

                break;

            case MouseFlags.Button3Pressed:
                mf = MouseFlags.Button3TripleClicked;

                break;
        }

        return mf;
    }

    private static async Task ProcessButtonClickedAsync ()
    {
        await Task.Delay (300);
        isButtonClicked = false;
    }

    private static async Task ProcessButtonDoubleClickedAsync ()
    {
        await Task.Delay (300);
        isButtonDoubleClicked = false;
    }

    private static async Task ProcessContinuousButtonPressedAsync (MouseFlags mouseFlag, Action<MouseFlags, Point> continuousButtonPressedHandler)
    {
        // PERF: Pause and poll in a hot loop.
        // This should be replaced with event dispatch and a synchronization primitive such as AutoResetEvent.
        // Will make a massive difference in responsiveness.
        while (isButtonPressed)
        {
            await Task.Delay (100);

            View view = Application.WantContinuousButtonPressedView;

            if (view is null)
            {
                break;
            }

            if (isButtonPressed && lastMouseButtonPressed is { } && (mouseFlag & MouseFlags.ReportMousePosition) == 0)
            {
                Application.Invoke (() => continuousButtonPressedHandler (mouseFlag, point ?? Point.Empty));
            }
        }
    }

    private static MouseFlags SetControlKeyStates (MouseFlags buttonState, MouseFlags mouseFlag)
    {
        if ((buttonState & MouseFlags.ButtonCtrl) != 0 && (mouseFlag & MouseFlags.ButtonCtrl) == 0)
        {
            mouseFlag |= MouseFlags.ButtonCtrl;
        }

        if ((buttonState & MouseFlags.ButtonShift) != 0 && (mouseFlag & MouseFlags.ButtonShift) == 0)
        {
            mouseFlag |= MouseFlags.ButtonShift;
        }

        if ((buttonState & MouseFlags.ButtonAlt) != 0 && (mouseFlag & MouseFlags.ButtonAlt) == 0)
        {
            mouseFlag |= MouseFlags.ButtonAlt;
        }

        return mouseFlag;
    }

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

    /// <summary>
    ///     ESC [ 8 ; height ; width t - Set Terminal Window Size
    ///     https://terminalguide.namepad.de/seq/csi_st-8/
    /// </summary>
    public static string CSI_SetTerminalWindowSize (int height, int width) { return $"{CSI}8;{height};{width}t"; }

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

    #endregion

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
    ///     ESC[48;2;{r};{g};{b}m	Set background color as RGB.
    /// </summary>
    public static string CSI_SetBackgroundColorRGB (int r, int g, int b) { return $"{CSI}48;2;{r};{g};{b}m"; }

    #endregion

    #region Requests

    /// <summary>
    ///     ESC [ ? 6 n - Request Cursor Position Report (?) (DECXCPR)
    ///     https://terminalguide.namepad.de/seq/csi_sn__p-6/
    /// </summary>
    public static readonly string CSI_RequestCursorPositionReport = CSI + "?6n";

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
    /// </summary>
    public static readonly string CSI_SendDeviceAttributes = CSI + "0c";

    /// <summary>
    ///     ESC [ > 0 c - Send Device Attributes (Secondary DA)
    ///     Windows Terminal v1.18+ emits: "\x1b[>0;10;1c" (vt100, firmware version 1.0, vt220)
    /// </summary>
    public static readonly string CSI_SendDeviceAttributes2 = CSI + ">0c";

    /// <summary>
    ///     The terminator indicating a reply to <see cref="CSI_SendDeviceAttributes"/> or
    ///     <see cref="CSI_SendDeviceAttributes2"/>
    /// </summary>
    public const string CSI_ReportDeviceAttributes_Terminator = "c";

    /*
     TODO: depends on https://github.com/gui-cs/Terminal.Gui/pull/3768
    /// <summary>
    ///     CSI 16 t - Request sixel resolution (width and height in pixels)
    /// </summary>
    public static readonly AnsiEscapeSequenceRequest CSI_RequestSixelResolution = new () { Request = CSI + "16t", Terminator = "t" };

    /// <summary>
    ///     CSI 14 t - Request window size in pixels (width x height)
    /// </summary>
    public static readonly AnsiEscapeSequenceRequest CSI_RequestWindowSizeInPixels = new () { Request = CSI + "14t", Terminator = "t" };
    */

    /// <summary>
    ///     CSI 1 8 t  | yes | yes |  yes  | report window size in chars
    ///     https://terminalguide.namepad.de/seq/csi_st-18/
    /// </summary>
    public static readonly string CSI_ReportTerminalSizeInChars = CSI + "18t";

    /// <summary>
    ///     The terminator indicating a reply to <see cref="CSI_ReportTerminalSizeInChars"/> : ESC [ 8 ; height ; width t
    /// </summary>
    public const string CSI_ReportTerminalSizeInChars_Terminator = "t";

    /// <summary>
    ///     The value of the response to <see cref="CSI_ReportTerminalSizeInChars"/> indicating value 1 and 2 are the terminal
    ///     size in chars.
    /// </summary>
    public const string CSI_ReportTerminalSizeInChars_ResponseValue = "8";

    #endregion
}
