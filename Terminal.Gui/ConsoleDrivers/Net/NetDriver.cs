//
// NetDriver.cs: The System.Console-based .NET driver, works on Windows and Unix, but is not particularly efficient.
//

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using static Terminal.Gui.ConsoleDrivers.ConsoleKeyMapping;
using static Terminal.Gui.ConsoleDrivers.Net.NetEvents;
using static Terminal.Gui.EscSeqUtils;

namespace Terminal.Gui.ConsoleDrivers.Net;


[SupportedOSPlatform("WINDOWS")]
[SupportedOSPlatform("OSX")]
[SupportedOSPlatform("LINUX")]
internal sealed class NetDriver : ConsoleDriver
{
    private const int COLOR_BLACK = 30;
    private const int COLOR_BLUE = 34;
    private const int COLOR_BRIGHT_BLACK = 90;
    private const int COLOR_BRIGHT_BLUE = 94;
    private const int COLOR_BRIGHT_CYAN = 96;
    private const int COLOR_BRIGHT_GREEN = 92;
    private const int COLOR_BRIGHT_MAGENTA = 95;
    private const int COLOR_BRIGHT_RED = 91;
    private const int COLOR_BRIGHT_WHITE = 97;
    private const int COLOR_BRIGHT_YELLOW = 93;
    private const int COLOR_CYAN = 36;
    private const int COLOR_GREEN = 32;
    private const int COLOR_MAGENTA = 35;
    private const int COLOR_RED = 31;
    private const int COLOR_WHITE = 37;
    private const int COLOR_YELLOW = 33;
    private NetMainLoop _mainLoopDriver = null!;
    public bool IsWinPlatform { get; private set; }
    public NetWinVTConsole? NetWinConsole { get; private set; }

    public override void Refresh ()
    {
        UpdateScreen ();
        UpdateCursor ();
    }

    public override void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
    {
        InputResult input = new ()
        {
            EventType = EventType.Key, ConsoleKeyInfo = new (keyChar, key, shift, alt, control)
        };

        try
        {
            ProcessInput (input);
        }
        catch (OverflowException)
        {
            // BUG: This is almost certainly a bug.
            // When and why does this exception happen?
        }
    }

    public override bool SupportsTrueColor => Environment.OSVersion.Platform == PlatformID.Unix
                                           || (IsWinPlatform && Environment.OSVersion.Version.Build >= 14931);

    public override void Suspend ()
    {
        if (Environment.OSVersion.Platform != PlatformID.Unix)
        {
            return;
        }

        StopReportingMouseMoves ();

        if (!RunningUnitTests)
        {
            Console.ResetColor ();
            Console.Clear ();

            //Disable alternative screen buffer.
            Console.Out.Write (CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

            //Set cursor key to cursor.
            Console.Out.Write (CSI_ShowCursor);

            Platform.Suspend ();

            //Enable alternative screen buffer.
            Console.Out.Write (CSI_SaveCursorAndActivateAltBufferNoBackscroll);

            SetContentsAsDirty ();
            Refresh ();
        }

        StartReportingMouseMoves ();
    }

    public override void UpdateScreen ()
    {
        if (RunningUnitTests
         || _winSizeChanging
         || Console.WindowHeight < 1
         || (Contents is { } c && c.Length != Rows * Cols)
         || Rows != Console.WindowHeight)
        {
            return;
        }

        var top = 0;
        var left = 0;
        int rows = Rows;
        int cols = Cols;
        StringBuilder output = new ();
        Attribute redrawAttr = default;
        Attribute attr = default;
        ref Attribute redrawAttrRef = ref redrawAttr;
        int lastCol = -1;

        CursorVisibility? savedVisibility = _cachedCursorVisibility;
        SetCursorVisibility (CursorVisibility.Invisible);

        for (int row = top; row < rows; row++)
        {
            if (Console.WindowHeight < 1)
            {
                return;
            }

            if (_dirtyLines? [row] is false)
            {
                continue;
            }

            if (!SetCursorPosition (0, row))
            {
                return;
            }

            _dirtyLines! [row] = false;
            output.Clear ();

            for (int col = left; col < cols; col++)
            {
                lastCol = -1;
                var outputWidth = 0;

                for (; col < cols; col++)
                {
                    if (Contents? [row, col].IsDirty is false)
                    {
                        if (output.Length > 0)
                        {
                            WriteToConsole (output, ref lastCol, row, ref outputWidth);
                        }
                        else if (lastCol == -1)
                        {
                            lastCol = col;
                        }

                        if (lastCol + 1 < cols)
                        {
                            lastCol++;
                        }

                        continue;
                    }

                    if (lastCol == -1)
                    {
                        lastCol = col;
                    }

                    // These two checks likely can be made conditional for debug builds, once this
                    // is mad thread-safe.
                    // They shouldn't be possible at that point but would be good to have debug capability.
                    if (Contents is null)
                    {
                        throw new InvalidOperationException ("Contents null on attempt to update screen.");
                    }

                    if (!Contents [row, col].Attribute.HasValue)
                    {
                        throw new InvalidOperationException ($"Null contents at ({row},{col}).");
                    }

                    attr = Contents [row, col].Attribute!.Value;

                    // Performance: Only send the escape sequence if the attribute has changed.
                    if (attr != redrawAttrRef)
                    {
                        redrawAttrRef = ref attr;

                        if (Force16Colors)
                        {
                            output.Append (
                                           CSI_SetGraphicsRendition (
                                                                     MapColors (
                                                                                (ConsoleColor)attr.Background.GetClosestNamedColor (),
                                                                                false
                                                                               ),
                                                                     MapColors ((ConsoleColor)attr.Foreground.GetClosestNamedColor ())
                                                                    )
                                          );
                        }
                        else
                        {
                            output.Append (
                                           CSI_SetForegroundColorRGB (
                                                                      attr.Foreground.R,
                                                                      attr.Foreground.G,
                                                                      attr.Foreground.B
                                                                     )
                                          );

                            output.Append (
                                           CSI_SetBackgroundColorRGB (
                                                                      attr.Background.R,
                                                                      attr.Background.G,
                                                                      attr.Background.B
                                                                     )
                                          );
                        }
                    }

                    outputWidth++;
                    Rune rune = Contents [row, col].Rune;
                    output.Append (rune);

                    if (Contents [row, col].CombiningMarks.Count > 0)
                    {
                        // AtlasEngine does not support NON-NORMALIZED combining marks in a way
                        // compatible with the driver architecture. Any CMs (except in the first col)
                        // are correctly combined with the base char, but are ALSO treated as 1 column
                        // width codepoints E.g. `echo "[e`u{0301}`u{0301}]"` will output `[é  ]`.
                        // 
                        // For now, we just ignore the list of CMs.
                        //foreach (var combMark in Contents [row, col].CombiningMarks) {
                        //    output.Append (combMark);
                        //}
                        //WriteToConsole (output, ref lastCol, row, ref outputWidth);
                    }
                    else if (rune.IsSurrogatePair () && rune.GetColumns () < 2)
                    {
                        WriteToConsole (output, ref lastCol, row, ref outputWidth);
                        SetCursorPosition (col - 1, row);
                    }

                    Contents [row, col].IsDirty = false;
                }
            }

            if (output.Length > 0)
            {
                SetCursorPosition (lastCol, row);
                Console.Write (output);
            }
        }

        SetCursorPosition (0, 0);

        _cachedCursorVisibility = savedVisibility;

        // TODO: Eliminate StringBuilder uses where possible.
        static void WriteToConsole (StringBuilder outputLocal, ref int lastColumn, int row, ref int outputWidth)
        {
            SetCursorPosition (lastCol, row);
            Console.Write (output);
            output.Clear ();
            lastCol += outputWidth;
            outputWidth = 0;
        }
    }

    internal override void End ()
    {
        if (IsWinPlatform)
        {
            NetWinConsole?.Cleanup ();
        }

        StopReportingMouseMoves ();

        if (!RunningUnitTests)
        {
            Console.ResetColor ();

            //Disable alternative screen buffer.
            Console.Out.Write (CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

            //Set cursor key to cursor.
            Console.Out.Write (CSI_ShowCursor);
            Console.Out.Close ();
        }
    }

    internal override MainLoop Init ()
    {
        if (Environment.OSVersion.Platform is PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows)
        {
            IsWinPlatform = true;

            try
            {
                NetWinConsole = new ();
            }
            catch (IOException ioe) when (Console.IsOutputRedirected || Console.IsInputRedirected)
            {
            // TODO: Deal with this more formally
                // Likely running as a unit test, or in a non-interactive session.
            }
        }

        if (IsWinPlatform)
        {
            Clipboard = new WindowsClipboard ();
        }
        else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            Clipboard = new MacOSXClipboard ();
        }
        else
        {
            if (CursesDriver.Is_WSL_Platform ())
            {
                Clipboard = new WSLClipboard ();
            }
            else
            {
                Clipboard = new CursesClipboard ();
            }
        }

        if (!RunningUnitTests)
        {
            Console.TreatControlCAsInput = true;

            Cols = Console.WindowWidth;
            Rows = Console.WindowHeight;

            //Enable alternative screen buffer.
            Console.Out.Write (CSI_SaveCursorAndActivateAltBufferNoBackscroll);

            //Set cursor key to application.
            Console.Out.Write (CSI_HideCursor);
        }
        else
        {
            // We are being run in an environment that does not support a console
            // such as a unit test, or a pipe.

            // TODO: Consider using a stream of some sort, like FileStream or MemoryStream,
            // to eliminate that problem.
            Cols = 80;
            Rows = 24;
        }

        ResizeScreen ();
        ClearContents ();
        CurrentAttribute = new (Color.White, Color.Black);

        StartReportingMouseMoves ();

        _mainLoopDriver = new (this);

        // TODO: This should be an event.
        // Might want to explore alternatives, regardless.
        _mainLoopDriver.ProcessInput = ProcessInput;

        return new (_mainLoopDriver);
    }

    private void ProcessInput (InputResult inputEvent)
    {
        switch (inputEvent.EventType)
        {
            case EventType.Key:
                ConsoleKeyInfo consoleKeyInfo = inputEvent.ConsoleKeyInfo;

                //if (consoleKeyInfo.Key == ConsoleKey.Packet) {
                //    consoleKeyInfo = FromVKPacketToKConsoleKeyInfo (consoleKeyInfo);
                //}

                //Debug.WriteLine ($"event: {inputEvent}");

                KeyCode map = MapKey (consoleKeyInfo);

                if (map == KeyCode.Null)
                {
                    break;
                }

                OnKeyDown (new (map));
                OnKeyUp (new (map));

                break;
            case EventType.Mouse:
                MouseEvent me = ToDriverMouse (inputEvent.MouseEvent);

                //Debug.WriteLine ($"NetDriver: ({me.X},{me.Y}) - {me.Flags}");
                OnMouseEvent (me);

                break;
            case EventType.WindowSize:
                _winSizeChanging = true;
                Top = 0;
                Left = 0;
                Cols = inputEvent.WindowSizeEvent.Size.Width;
                Rows = Math.Max (inputEvent.WindowSizeEvent.Size.Height, 0);

                ResizeScreen ();
                ClearContents ();
                _winSizeChanging = false;
                OnSizeChanged (new (new (Cols, Rows)));

                break;
            case EventType.RequestResponse:
                break;
            case EventType.WindowPosition:
                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }
    }

    #region Size and Position Handling

    private volatile bool _winSizeChanging;

    public override void SetWindowPosition (int col, int row)
    {
        if (!RunningUnitTests)
        {
            Top = Console.WindowTop;
            Left = Console.WindowLeft;
        }
        else
        {
            Top = row;
            Left = col;
        }
    }

    public void ResizeScreen ()
    {
        // TODO: Break the windows-specific part out into a method attributed as such.
        // Not supported on Unix.
        if (IsWinPlatform)
        {
            // Can raise an exception while still resizing.
            try
            {
#pragma warning disable CA1416
                if (Console.WindowHeight > 0)
                {
                    Console.CursorTop = 0;
                    Console.CursorLeft = 0;
                    Console.WindowTop = 0;
                    Console.WindowLeft = 0;

                    if (Console.WindowHeight > Rows)
                    {
                        Console.SetWindowSize (Cols, Rows);
                    }

                    Console.SetBufferSize (Cols, Rows);
                }
#pragma warning restore CA1416
            }

            // INTENT: Why are these eating the exceptions?
            // Comments would be good here.
            catch (IOException)
            {
                // CONCURRENCY: Unsynchronized access to Clip is not safe.
                Clip = new (0, 0, Cols, Rows);
            }
            catch (ArgumentOutOfRangeException)
            {
                // CONCURRENCY: Unsynchronized access to Clip is not safe.
                Clip = new (0, 0, Cols, Rows);
            }
        }
        else
        {
            Console.Out.Write (CSI_SetTerminalWindowSize (Rows, Cols));
        }

        // CONCURRENCY: Unsynchronized access to Clip is not safe.
        Clip = new (0, 0, Cols, Rows);
    }

    #endregion

    #region Color Handling

    // Cache the list of ConsoleColor values.
    [UnconditionalSuppressMessage (
                                      "AOT",
                                      "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                      Justification = "<Pending>")]
    private static readonly HashSet<int> ConsoleColorValues = new (
                                                                   Enum.GetValues (typeof (ConsoleColor))
                                                                       .OfType<ConsoleColor> ()
                                                                       .Select (c => (int)c)
                                                                  );

    // Dictionary for mapping ConsoleColor values to the values used by System.Net.Console.
    private static readonly Dictionary<ConsoleColor, int> colorMap = new ()
    {
        { ConsoleColor.Black, COLOR_BLACK },
        { ConsoleColor.DarkBlue, COLOR_BLUE },
        { ConsoleColor.DarkGreen, COLOR_GREEN },
        { ConsoleColor.DarkCyan, COLOR_CYAN },
        { ConsoleColor.DarkRed, COLOR_RED },
        { ConsoleColor.DarkMagenta, COLOR_MAGENTA },
        { ConsoleColor.DarkYellow, COLOR_YELLOW },
        { ConsoleColor.Gray, COLOR_WHITE },
        { ConsoleColor.DarkGray, COLOR_BRIGHT_BLACK },
        { ConsoleColor.Blue, COLOR_BRIGHT_BLUE },
        { ConsoleColor.Green, COLOR_BRIGHT_GREEN },
        { ConsoleColor.Cyan, COLOR_BRIGHT_CYAN },
        { ConsoleColor.Red, COLOR_BRIGHT_RED },
        { ConsoleColor.Magenta, COLOR_BRIGHT_MAGENTA },
        { ConsoleColor.Yellow, COLOR_BRIGHT_YELLOW },
        { ConsoleColor.White, COLOR_BRIGHT_WHITE }
    };

    // Map a ConsoleColor to a platform dependent value.
    private static int MapColors (ConsoleColor color, bool isForeground = true) =>
        colorMap.TryGetValue (color, out int colorValue) ? colorValue + (isForeground ? 0 : 10) : 0;

    ///// <remarks>
    ///// In the NetDriver, colors are encoded as an int. 
    ///// However, the foreground color is stored in the most significant 16 bits, 
    ///// and the background color is stored in the least significant 16 bits.
    ///// </remarks>
    //public override Attribute MakeColor (Color foreground, Color background)
    //{
    //  // Encode the colors into the int value.
    //  return new Attribute (
    //    platformColor: ((((int)foreground.ColorName) & 0xffff) << 16) | (((int)background.ColorName) & 0xffff),
    //    foreground: foreground,
    //    background: background
    //  );
    //}

    #endregion

    #region Cursor Handling

    private static bool SetCursorPosition (int col, int row)
    {
        if (IsWinPlatform)
        {
            // Could happen that the windows is still resizing and the col is bigger than Console.WindowWidth.
            try
            {
                Console.SetCursorPosition (col, row);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // + 1 is needed because non-Windows is based on 1 instead of 0 and
        // Console.CursorTop/CursorLeft isn't reliable.
        Console.Out.Write (CSI_SetCursorPosition (row + 1, col + 1));

        return true;
    }

    private CursorVisibility? _cachedCursorVisibility;

    public override void UpdateCursor ()
    {
        EnsureCursorVisibility ();

        if (Col >= 0 && Col < Cols && Row >= 0 && Row <= Rows)
        {
            SetCursorPosition (Col, Row);
            SetWindowPosition (0, Row);
        }
    }

    public override bool GetCursorVisibility (out CursorVisibility visibility)
    {
        visibility = _cachedCursorVisibility ?? CursorVisibility.Default;

        return visibility == CursorVisibility.Default;
    }

    /// <inheritdoc />
    public override string GetVersionInfo () => nameof (NetDriver);

    public override bool SetCursorVisibility (CursorVisibility visibility)
    {
        _cachedCursorVisibility = visibility;

        Console.Out.Write (visibility == CursorVisibility.Default ? CSI_ShowCursor : CSI_HideCursor);

        return visibility == CursorVisibility.Default;
    }

    public override bool EnsureCursorVisibility ()
    {
        if (!(Col >= 0 && Row >= 0 && Col < Cols && Row < Rows))
        {
            GetCursorVisibility (out CursorVisibility cursorVisibility);
            _cachedCursorVisibility = cursorVisibility;
            SetCursorVisibility (CursorVisibility.Invisible);

            return false;
        }

        SetCursorVisibility (_cachedCursorVisibility ?? CursorVisibility.Default);

        return _cachedCursorVisibility == CursorVisibility.Default;
    }

    #endregion

    #region Mouse Handling

    public static void StartReportingMouseMoves ()
    {
        if (!RunningUnitTests)
        {
            Console.Out.Write (CSI_EnableMouseEvents);
        }
    }

    public static void StopReportingMouseMoves ()
    {
        if (!RunningUnitTests)
        {
            Console.Out.Write (CSI_DisableMouseEvents);
        }
    }

    private static MouseEvent ToDriverMouse (NetEvents.MouseEvent me)
    {
        //System.Diagnostics.Debug.WriteLine ($"X: {me.Position.X}; Y: {me.Position.Y}; ButtonState: {me.ButtonState}");

        MouseFlags mouseFlag = 0;

        if ((me.ButtonState & MouseButtonState.Button1Pressed) != 0)
        {
            mouseFlag |= MouseFlags.Button1Pressed;
        }

        if ((me.ButtonState & MouseButtonState.Button1Released) != 0)
        {
            mouseFlag |= MouseFlags.Button1Released;
        }

        if ((me.ButtonState & MouseButtonState.Button1Clicked) != 0)
        {
            mouseFlag |= MouseFlags.Button1Clicked;
        }

        if ((me.ButtonState & MouseButtonState.Button1DoubleClicked) != 0)
        {
            mouseFlag |= MouseFlags.Button1DoubleClicked;
        }

        if ((me.ButtonState & MouseButtonState.Button1TripleClicked) != 0)
        {
            mouseFlag |= MouseFlags.Button1TripleClicked;
        }

        if ((me.ButtonState & MouseButtonState.Button2Pressed) != 0)
        {
            mouseFlag |= MouseFlags.Button2Pressed;
        }

        if ((me.ButtonState & MouseButtonState.Button2Released) != 0)
        {
            mouseFlag |= MouseFlags.Button2Released;
        }

        if ((me.ButtonState & MouseButtonState.Button2Clicked) != 0)
        {
            mouseFlag |= MouseFlags.Button2Clicked;
        }

        if ((me.ButtonState & MouseButtonState.Button2DoubleClicked) != 0)
        {
            mouseFlag |= MouseFlags.Button2DoubleClicked;
        }

        if ((me.ButtonState & MouseButtonState.Button2TripleClicked) != 0)
        {
            mouseFlag |= MouseFlags.Button2TripleClicked;
        }

        if ((me.ButtonState & MouseButtonState.Button3Pressed) != 0)
        {
            mouseFlag |= MouseFlags.Button3Pressed;
        }

        if ((me.ButtonState & MouseButtonState.Button3Released) != 0)
        {
            mouseFlag |= MouseFlags.Button3Released;
        }

        if ((me.ButtonState & MouseButtonState.Button3Clicked) != 0)
        {
            mouseFlag |= MouseFlags.Button3Clicked;
        }

        if ((me.ButtonState & MouseButtonState.Button3DoubleClicked) != 0)
        {
            mouseFlag |= MouseFlags.Button3DoubleClicked;
        }

        if ((me.ButtonState & MouseButtonState.Button3TripleClicked) != 0)
        {
            mouseFlag |= MouseFlags.Button3TripleClicked;
        }

        if ((me.ButtonState & MouseButtonState.ButtonWheeledUp) != 0)
        {
            mouseFlag |= MouseFlags.WheeledUp;
        }

        if ((me.ButtonState & MouseButtonState.ButtonWheeledDown) != 0)
        {
            mouseFlag |= MouseFlags.WheeledDown;
        }

        if ((me.ButtonState & MouseButtonState.ButtonWheeledLeft) != 0)
        {
            mouseFlag |= MouseFlags.WheeledLeft;
        }

        if ((me.ButtonState & MouseButtonState.ButtonWheeledRight) != 0)
        {
            mouseFlag |= MouseFlags.WheeledRight;
        }

        if ((me.ButtonState & MouseButtonState.Button4Pressed) != 0)
        {
            mouseFlag |= MouseFlags.Button4Pressed;
        }

        if ((me.ButtonState & MouseButtonState.Button4Released) != 0)
        {
            mouseFlag |= MouseFlags.Button4Released;
        }

        if ((me.ButtonState & MouseButtonState.Button4Clicked) != 0)
        {
            mouseFlag |= MouseFlags.Button4Clicked;
        }

        if ((me.ButtonState & MouseButtonState.Button4DoubleClicked) != 0)
        {
            mouseFlag |= MouseFlags.Button4DoubleClicked;
        }

        if ((me.ButtonState & MouseButtonState.Button4TripleClicked) != 0)
        {
            mouseFlag |= MouseFlags.Button4TripleClicked;
        }

        if ((me.ButtonState & MouseButtonState.ReportMousePosition) != 0)
        {
            mouseFlag |= MouseFlags.ReportMousePosition;
        }

        if ((me.ButtonState & MouseButtonState.ButtonShift) != 0)
        {
            mouseFlag |= MouseFlags.ButtonShift;
        }

        if ((me.ButtonState & MouseButtonState.ButtonCtrl) != 0)
        {
            mouseFlag |= MouseFlags.ButtonCtrl;
        }

        if ((me.ButtonState & MouseButtonState.ButtonAlt) != 0)
        {
            mouseFlag |= MouseFlags.ButtonAlt;
        }

        return new () { Position = me.Position, Flags = mouseFlag };
    }

    #endregion Mouse Handling

    #region Keyboard Handling

    private static ConsoleKeyInfo FromVKPacketToKConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
    {
        if (consoleKeyInfo.Key != ConsoleKey.Packet)
        {
            return consoleKeyInfo;
        }

        ConsoleModifiers mod = consoleKeyInfo.Modifiers;
        bool shift = (mod & ConsoleModifiers.Shift) != 0;
        bool alt = (mod & ConsoleModifiers.Alt) != 0;
        bool control = (mod & ConsoleModifiers.Control) != 0;

        ConsoleKeyInfo cKeyInfo = DecodeVKPacketToKConsoleKeyInfo (consoleKeyInfo);

        return new (cKeyInfo.KeyChar, cKeyInfo.Key, shift, alt, control);
    }

    private static KeyCode MapKey (ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
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
                    // If the keyChar is 0, keyInfo.Key value is not a printable character. 

                    return KeyCode.Null; // MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode)keyInfo.Key);
                }

                if (keyInfo.Modifiers != ConsoleModifiers.Shift)
                {
                    // If Shift wasn't down we don't need to do anything but return the keyInfo.KeyChar
                    return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
                }

                // Strip off Shift - We got here because they KeyChar from Windows is the shifted char (e.g. "Ç")
                // and passing on Shift would be redundant.
                return MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.KeyChar);
        }

        // Handle control keys whose VK codes match the related ASCII value (those below ASCII 33) like ESC
        if (keyInfo.Key != ConsoleKey.None && Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key))
        {
            if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control) && keyInfo.Key == ConsoleKey.I)
            {
                return KeyCode.Tab;
            }

            return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)(uint)keyInfo.Key);
        }

        // Handle control keys (e.g. CursorUp)
        if (keyInfo.Key != ConsoleKey.None
         && Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint))
        {
            return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)((uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint));
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
                // NetDriver doesn't support Shift-Ctrl/Shift-Alt combos
                return MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.Key);
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

        return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
    }

    #endregion Keyboard Handling
}
