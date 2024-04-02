//
// Driver.cs: Curses-based Driver
//

using System.Diagnostics;
using System.Runtime.InteropServices;
using Terminal.Gui.ConsoleDrivers;
using Unix.Terminal;

namespace Terminal.Gui;

/// <summary>This is the Curses driver for the gui.cs/Terminal framework.</summary>
internal class CursesDriver : ConsoleDriver
{
    public Curses.Window _window;
    private CursorVisibility? _currentCursorVisibility;
    private CursorVisibility? _initialCursorVisibility;
    private MouseFlags _lastMouseFlags;
    private UnixMainLoop _mainLoopDriver;
    private object _processInputToken;

    public override int Cols
    {
        get => Curses.Cols;
        internal set
        {
            Curses.Cols = value;
            ClearContents ();
        }
    }

    public override int Rows
    {
        get => Curses.Lines;
        internal set
        {
            Curses.Lines = value;
            ClearContents ();
        }
    }

    public override bool SupportsTrueColor => false;

    /// <inheritdoc/>
    public override bool EnsureCursorVisibility () { return false; }

    /// <inheritdoc/>
    public override bool GetCursorVisibility (out CursorVisibility visibility)
    {
        visibility = CursorVisibility.Invisible;

        if (!_currentCursorVisibility.HasValue)
        {
            return false;
        }

        visibility = _currentCursorVisibility.Value;

        return true;
    }

    public override string GetVersionInfo () { return $"{Curses.curses_version ()}"; }

    public static bool Is_WSL_Platform ()
    {
        // xclip does not work on WSL, so we need to use the Windows clipboard vis Powershell
        //if (new CursesClipboard ().IsSupported) {
        //	// If xclip is installed on Linux under WSL, this will return true.
        //	return false;
        //}
        (int exitCode, string result) = ClipboardProcessRunner.Bash ("uname -a", waitForOutput: true);

        if (exitCode == 0 && result.Contains ("microsoft") && result.Contains ("WSL"))
        {
            return true;
        }

        return false;
    }

    public override bool IsRuneSupported (Rune rune)
    {
        // See Issue #2615 - CursesDriver is broken with non-BMP characters
        return base.IsRuneSupported (rune) && rune.IsBmp;
    }

    public override void Move (int col, int row)
    {
        base.Move (col, row);

        if (RunningUnitTests)
        {
            return;
        }

        if (IsValidLocation (col, row))
        {
            Curses.move (row, col);
        }
        else
        {
            // Not a valid location (outside screen or clip region)
            // Move within the clip region, then AddRune will actually move to Col, Row
            Curses.move (Clip.Y, Clip.X);
        }
    }

    public override void Refresh ()
    {
        UpdateScreen ();
        UpdateCursor ();
    }

    public override void SendKeys (char keyChar, ConsoleKey consoleKey, bool shift, bool alt, bool control)
    {
        KeyCode key;

        if (consoleKey == ConsoleKey.Packet)
        {
            var mod = new ConsoleModifiers ();

            if (shift)
            {
                mod |= ConsoleModifiers.Shift;
            }

            if (alt)
            {
                mod |= ConsoleModifiers.Alt;
            }

            if (control)
            {
                mod |= ConsoleModifiers.Control;
            }

            var cKeyInfo = new ConsoleKeyInfo (keyChar, consoleKey, shift, alt, control);
            cKeyInfo = ConsoleKeyMapping.DecodeVKPacketToKConsoleKeyInfo (cKeyInfo);
            key = ConsoleKeyMapping.MapConsoleKeyInfoToKeyCode (cKeyInfo);
        }
        else
        {
            key = (KeyCode)keyChar;
        }

        OnKeyDown (new Key (key));
        OnKeyUp (new Key (key));

        //OnKeyPressed (new KeyEventArgsEventArgs (key));
    }

    /// <inheritdoc/>
    public override bool SetCursorVisibility (CursorVisibility visibility)
    {
        if (_initialCursorVisibility.HasValue == false)
        {
            return false;
        }

        if (!RunningUnitTests)
        {
            Curses.curs_set (((int)visibility >> 16) & 0x000000FF);
        }

        if (visibility != CursorVisibility.Invisible)
        {
            Console.Out.Write (
                               EscSeqUtils.CSI_SetCursorStyle (
                                                               (EscSeqUtils.DECSCUSR_Style)(((int)visibility >> 24)
                                                                                            & 0xFF)
                                                              )
                              );
        }

        _currentCursorVisibility = visibility;

        return true;
    }

    public void StartReportingMouseMoves ()
    {
        if (!RunningUnitTests)
        {
            Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
        }
    }

    public void StopReportingMouseMoves ()
    {
        if (!RunningUnitTests)
        {
            Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);
        }
    }

    public override void Suspend ()
    {
        StopReportingMouseMoves ();

        if (!RunningUnitTests)
        {
            Platform.Suspend ();
            Curses.Window.Standard.redrawwin ();
            Curses.refresh ();
        }

        StartReportingMouseMoves ();
    }

    public override void UpdateCursor ()
    {
        EnsureCursorVisibility ();

        if (!RunningUnitTests && Col >= 0 && Col < Cols && Row >= 0 && Row < Rows)
        {
            Curses.move (Row, Col);
        }
    }

    public override void UpdateScreen ()
    {
        for (var row = 0; row < Rows; row++)
        {
            if (!_dirtyLines [row])
            {
                continue;
            }

            _dirtyLines [row] = false;

            for (var col = 0; col < Cols; col++)
            {
                if (Contents [row, col].IsDirty == false)
                {
                    continue;
                }

                if (RunningUnitTests)
                {
                    // In unit tests, we don't want to actually write to the screen.
                    continue;
                }

                Curses.attrset (Contents [row, col].Attribute.GetValueOrDefault ().PlatformColor);

                Rune rune = Contents [row, col].Rune;

                if (rune.IsBmp)
                {
                    // BUGBUG: CursesDriver doesn't render CharMap correctly for wide chars (and other Unicode) - Curses is doing something funky with glyphs that report GetColums() of 1 yet are rendered wide. E.g. 0x2064 (invisible times) is reported as 1 column but is rendered as 2. WindowsDriver & NetDriver correctly render this as 1 column, overlapping the next cell.
                    if (rune.GetColumns () < 2)
                    {
                        Curses.mvaddch (row, col, rune.Value);
                    }
                    else /*if (col + 1 < Cols)*/
                    {
                        Curses.mvaddwstr (row, col, rune.ToString ());
                    }
                }
                else
                {
                    Curses.mvaddwstr (row, col, rune.ToString ());

                    if (rune.GetColumns () > 1 && col + 1 < Cols)
                    {
                        // TODO: This is a hack to deal with non-BMP and wide characters.
                        //col++;
                        Curses.mvaddch (row, ++col, '*');
                    }
                }
            }
        }

        if (!RunningUnitTests)
        {
            Curses.move (Row, Col);
            _window.wrefresh ();
        }
    }

    internal override void End ()
    {
        StopReportingMouseMoves ();
        SetCursorVisibility (CursorVisibility.Default);

        if (_mainLoopDriver is { })
        {
            _mainLoopDriver.RemoveWatch (_processInputToken);
        }

        if (RunningUnitTests)
        {
            return;
        }

        // throws away any typeahead that has been typed by
        // the user and has not yet been read by the program.
        Curses.flushinp ();

        Curses.endwin ();
    }

    internal override MainLoop Init ()
    {
        _mainLoopDriver = new UnixMainLoop (this);

        if (!RunningUnitTests)
        {
            _window = Curses.initscr ();
            Curses.set_escdelay (10);

            // Ensures that all procedures are performed at some previous closing.
            Curses.doupdate ();

            // 
            // We are setting Invisible as default so we could ignore XTerm DECSUSR setting
            //
            switch (Curses.curs_set (0))
            {
                case 0:
                    _currentCursorVisibility = _initialCursorVisibility = CursorVisibility.Invisible;

                    break;

                case 1:
                    _currentCursorVisibility = _initialCursorVisibility = CursorVisibility.Underline;
                    Curses.curs_set (1);

                    break;

                case 2:
                    _currentCursorVisibility = _initialCursorVisibility = CursorVisibility.Box;
                    Curses.curs_set (2);

                    break;

                default:
                    _currentCursorVisibility = _initialCursorVisibility = null;

                    break;
            }

            if (!Curses.HasColors)
            {
                throw new InvalidOperationException ("V2 - This should never happen. File an Issue if it does.");
            }

            Curses.raw ();
            Curses.noecho ();

            Curses.Window.Standard.keypad (true);

            Curses.StartColor ();
            Curses.UseDefaultColors ();

            if (!RunningUnitTests)
            {
                Curses.timeout (0);
            }

            _processInputToken = _mainLoopDriver?.AddWatch (
                                                            0,
                                                            UnixMainLoop.Condition.PollIn,
                                                            x =>
                                                            {
                                                                ProcessInput ();

                                                                return true;
                                                            }
                                                           );
        }

        CurrentAttribute = new Attribute (ColorName.White, ColorName.Black);

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            Clipboard = new FakeDriver.FakeClipboard ();
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
            {
                Clipboard = new MacOSXClipboard ();
            }
            else
            {
                if (Is_WSL_Platform ())
                {
                    Clipboard = new WSLClipboard ();
                }
                else
                {
                    Clipboard = new CursesClipboard ();
                }
            }
        }

        ClearContents ();
        StartReportingMouseMoves ();

        if (!RunningUnitTests)
        {
            Curses.CheckWinChange ();
            Curses.refresh ();
        }

        return new MainLoop (_mainLoopDriver);
    }

    internal void ProcessInput ()
    {
        int wch;
        int code = Curses.get_wch (out wch);

        //System.Diagnostics.Debug.WriteLine ($"code: {code}; wch: {wch}");
        if (code == Curses.ERR)
        {
            return;
        }

        var k = KeyCode.Null;

        if (code == Curses.KEY_CODE_YES)
        {
            while (code == Curses.KEY_CODE_YES && wch == Curses.KeyResize)
            {
                ProcessWinChange ();
                code = Curses.get_wch (out wch);
            }

            if (wch == 0)
            {
                return;
            }

            if (wch == Curses.KeyMouse)
            {
                int wch2 = wch;

                while (wch2 == Curses.KeyMouse)
                {
                    Key kea = null;

                    ConsoleKeyInfo [] cki =
                    {
                        new ((char)KeyCode.Esc, 0, false, false, false),
                        new ('[', 0, false, false, false),
                        new ('<', 0, false, false, false)
                    };
                    code = 0;
                    HandleEscSeqResponse (ref code, ref k, ref wch2, ref kea, ref cki);
                }

                return;
            }

            k = MapCursesKey (wch);

            if (wch >= 277 && wch <= 288)
            {
                // Shift+(F1 - F12)
                wch -= 12;
                k = KeyCode.ShiftMask | MapCursesKey (wch);
            }
            else if (wch >= 289 && wch <= 300)
            {
                // Ctrl+(F1 - F12)
                wch -= 24;
                k = KeyCode.CtrlMask | MapCursesKey (wch);
            }
            else if (wch >= 301 && wch <= 312)
            {
                // Ctrl+Shift+(F1 - F12)
                wch -= 36;
                k = KeyCode.CtrlMask | KeyCode.ShiftMask | MapCursesKey (wch);
            }
            else if (wch >= 313 && wch <= 324)
            {
                // Alt+(F1 - F12)
                wch -= 48;
                k = KeyCode.AltMask | MapCursesKey (wch);
            }
            else if (wch >= 325 && wch <= 327)
            {
                // Shift+Alt+(F1 - F3)
                wch -= 60;
                k = KeyCode.ShiftMask | KeyCode.AltMask | MapCursesKey (wch);
            }

            OnKeyDown (new Key (k));
            OnKeyUp (new Key (k));

            return;
        }

        // Special handling for ESC, we want to try to catch ESC+letter to simulate alt-letter as well as Alt-Fkey
        if (wch == 27)
        {
            Curses.timeout (10);

            code = Curses.get_wch (out int wch2);

            if (code == Curses.KEY_CODE_YES)
            {
                k = KeyCode.AltMask | MapCursesKey (wch);
            }

            Key key = null;

            if (code == 0)
            {
                // The ESC-number handling, debatable.
                // Simulates the AltMask itself by pressing Alt + Space.
                if (wch2 == (int)KeyCode.Space)
                {
                    k = KeyCode.AltMask;
                }
                else if (wch2 - (int)KeyCode.Space >= (uint)KeyCode.A
                         && wch2 - (int)KeyCode.Space <= (uint)KeyCode.Z)
                {
                    k = (KeyCode)((uint)KeyCode.AltMask + (wch2 - (int)KeyCode.Space));
                }
                else if (wch2 >= (uint)KeyCode.A - 64 && wch2 <= (uint)KeyCode.Z - 64)
                {
                    k = (KeyCode)((uint)(KeyCode.AltMask | KeyCode.CtrlMask) + (wch2 + 64));
                }
                else if (wch2 >= (uint)KeyCode.D0 && wch2 <= (uint)KeyCode.D9)
                {
                    k = (KeyCode)((uint)KeyCode.AltMask + (uint)KeyCode.D0 + (wch2 - (uint)KeyCode.D0));
                }
                else if (wch2 == Curses.KeyCSI)
                {
                    ConsoleKeyInfo [] cki =
                    {
                        new ((char)KeyCode.Esc, 0, false, false, false), new ('[', 0, false, false, false)
                    };
                    HandleEscSeqResponse (ref code, ref k, ref wch2, ref key, ref cki);

                    return;
                }
                else
                {
                    // Unfortunately there are no way to differentiate Ctrl+Alt+alfa and Ctrl+Shift+Alt+alfa.
                    if (((KeyCode)wch2 & KeyCode.CtrlMask) != 0)
                    {
                        k = (KeyCode)((uint)KeyCode.CtrlMask + (wch2 & ~(int)KeyCode.CtrlMask));
                    }

                    if (wch2 == 0)
                    {
                        k = KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Space;
                    }
                    else if (wch >= (uint)KeyCode.A && wch <= (uint)KeyCode.Z)
                    {
                        k = KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Space;
                    }
                    else if (wch2 < 256)
                    {
                        k = (KeyCode)wch2; // | KeyCode.AltMask;
                    }
                    else
                    {
                        k = (KeyCode)((uint)(KeyCode.AltMask | KeyCode.CtrlMask) + wch2);
                    }
                }

                key = new Key (k);
            }
            else
            {
                key = Key.Esc;
            }

            OnKeyDown (key);
            OnKeyUp (key);
        }
        else if (wch == Curses.KeyTab)
        {
            k = MapCursesKey (wch);
            OnKeyDown (new Key (k));
            OnKeyUp (new Key (k));
        }
        else
        {
            // Unfortunately there are no way to differentiate Ctrl+alfa and Ctrl+Shift+alfa.
            k = (KeyCode)wch;

            if (wch == 0)
            {
                k = KeyCode.CtrlMask | KeyCode.Space;
            }
            else if (wch >= (uint)KeyCode.A - 64 && wch <= (uint)KeyCode.Z - 64)
            {
                if ((KeyCode)(wch + 64) != KeyCode.J)
                {
                    k = KeyCode.CtrlMask | (KeyCode)(wch + 64);
                }
            }
            else if (wch >= (uint)KeyCode.A && wch <= (uint)KeyCode.Z)
            {
                k = (KeyCode)wch | KeyCode.ShiftMask;
            }

            if (wch == '\n' || wch == '\r')
            {
                k = KeyCode.Enter;
            }

            OnKeyDown (new Key (k));
            OnKeyUp (new Key (k));
        }
    }

    internal void ProcessWinChange ()
    {
        if (!RunningUnitTests && Curses.CheckWinChange ())
        {
            ClearContents ();
            OnSizeChanged (new SizeChangedEventArgs (new (Cols, Rows)));
        }
    }

    private void HandleEscSeqResponse (
        ref int code,
        ref KeyCode k,
        ref int wch2,
        ref Key keyEventArgs,
        ref ConsoleKeyInfo [] cki
    )
    {
        ConsoleKey ck = 0;
        ConsoleModifiers mod = 0;

        while (code == 0)
        {
            code = Curses.get_wch (out wch2);
            var consoleKeyInfo = new ConsoleKeyInfo ((char)wch2, 0, false, false, false);

            if (wch2 == 0 || wch2 == 27 || wch2 == Curses.KeyMouse)
            {
                EscSeqUtils.DecodeEscSeq (
                                          null,
                                          ref consoleKeyInfo,
                                          ref ck,
                                          cki,
                                          ref mod,
                                          out _,
                                          out _,
                                          out _,
                                          out _,
                                          out bool isKeyMouse,
                                          out List<MouseFlags> mouseFlags,
                                          out Point pos,
                                          out _,
                                          ProcessMouseEvent
                                         );

                if (isKeyMouse)
                {
                    foreach (MouseFlags mf in mouseFlags)
                    {
                        ProcessMouseEvent (mf, pos);
                    }

                    cki = null;

                    if (wch2 == 27)
                    {
                        cki = EscSeqUtils.ResizeArray (
                                                       new ConsoleKeyInfo (
                                                                           (char)KeyCode.Esc,
                                                                           0,
                                                                           false,
                                                                           false,
                                                                           false
                                                                          ),
                                                       cki
                                                      );
                    }
                }
                else
                {
                    k = ConsoleKeyMapping.MapConsoleKeyInfoToKeyCode (consoleKeyInfo);
                    keyEventArgs = new Key (k);
                    OnKeyDown (keyEventArgs);
                }
            }
            else
            {
                cki = EscSeqUtils.ResizeArray (consoleKeyInfo, cki);
            }
        }
    }

    private static KeyCode MapCursesKey (int cursesKey)
    {
        switch (cursesKey)
        {
            case Curses.KeyF1: return KeyCode.F1;
            case Curses.KeyF2: return KeyCode.F2;
            case Curses.KeyF3: return KeyCode.F3;
            case Curses.KeyF4: return KeyCode.F4;
            case Curses.KeyF5: return KeyCode.F5;
            case Curses.KeyF6: return KeyCode.F6;
            case Curses.KeyF7: return KeyCode.F7;
            case Curses.KeyF8: return KeyCode.F8;
            case Curses.KeyF9: return KeyCode.F9;
            case Curses.KeyF10: return KeyCode.F10;
            case Curses.KeyF11: return KeyCode.F11;
            case Curses.KeyF12: return KeyCode.F12;
            case Curses.KeyUp: return KeyCode.CursorUp;
            case Curses.KeyDown: return KeyCode.CursorDown;
            case Curses.KeyLeft: return KeyCode.CursorLeft;
            case Curses.KeyRight: return KeyCode.CursorRight;
            case Curses.KeyHome: return KeyCode.Home;
            case Curses.KeyEnd: return KeyCode.End;
            case Curses.KeyNPage: return KeyCode.PageDown;
            case Curses.KeyPPage: return KeyCode.PageUp;
            case Curses.KeyDeleteChar: return KeyCode.Delete;
            case Curses.KeyInsertChar: return KeyCode.Insert;
            case Curses.KeyTab: return KeyCode.Tab;
            case Curses.KeyBackTab: return KeyCode.Tab | KeyCode.ShiftMask;
            case Curses.KeyBackspace: return KeyCode.Backspace;
            case Curses.ShiftKeyUp: return KeyCode.CursorUp | KeyCode.ShiftMask;
            case Curses.ShiftKeyDown: return KeyCode.CursorDown | KeyCode.ShiftMask;
            case Curses.ShiftKeyLeft: return KeyCode.CursorLeft | KeyCode.ShiftMask;
            case Curses.ShiftKeyRight: return KeyCode.CursorRight | KeyCode.ShiftMask;
            case Curses.ShiftKeyHome: return KeyCode.Home | KeyCode.ShiftMask;
            case Curses.ShiftKeyEnd: return KeyCode.End | KeyCode.ShiftMask;
            case Curses.ShiftKeyNPage: return KeyCode.PageDown | KeyCode.ShiftMask;
            case Curses.ShiftKeyPPage: return KeyCode.PageUp | KeyCode.ShiftMask;
            case Curses.AltKeyUp: return KeyCode.CursorUp | KeyCode.AltMask;
            case Curses.AltKeyDown: return KeyCode.CursorDown | KeyCode.AltMask;
            case Curses.AltKeyLeft: return KeyCode.CursorLeft | KeyCode.AltMask;
            case Curses.AltKeyRight: return KeyCode.CursorRight | KeyCode.AltMask;
            case Curses.AltKeyHome: return KeyCode.Home | KeyCode.AltMask;
            case Curses.AltKeyEnd: return KeyCode.End | KeyCode.AltMask;
            case Curses.AltKeyNPage: return KeyCode.PageDown | KeyCode.AltMask;
            case Curses.AltKeyPPage: return KeyCode.PageUp | KeyCode.AltMask;
            case Curses.CtrlKeyUp: return KeyCode.CursorUp | KeyCode.CtrlMask;
            case Curses.CtrlKeyDown: return KeyCode.CursorDown | KeyCode.CtrlMask;
            case Curses.CtrlKeyLeft: return KeyCode.CursorLeft | KeyCode.CtrlMask;
            case Curses.CtrlKeyRight: return KeyCode.CursorRight | KeyCode.CtrlMask;
            case Curses.CtrlKeyHome: return KeyCode.Home | KeyCode.CtrlMask;
            case Curses.CtrlKeyEnd: return KeyCode.End | KeyCode.CtrlMask;
            case Curses.CtrlKeyNPage: return KeyCode.PageDown | KeyCode.CtrlMask;
            case Curses.CtrlKeyPPage: return KeyCode.PageUp | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyUp: return KeyCode.CursorUp | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyDown: return KeyCode.CursorDown | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyLeft: return KeyCode.CursorLeft | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyRight: return KeyCode.CursorRight | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyHome: return KeyCode.Home | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyEnd: return KeyCode.End | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyNPage: return KeyCode.PageDown | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyPPage: return KeyCode.PageUp | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftAltKeyUp: return KeyCode.CursorUp | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyDown: return KeyCode.CursorDown | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyLeft: return KeyCode.CursorLeft | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyRight: return KeyCode.CursorRight | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyNPage: return KeyCode.PageDown | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyPPage: return KeyCode.PageUp | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyHome: return KeyCode.Home | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyEnd: return KeyCode.End | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.AltCtrlKeyNPage: return KeyCode.PageDown | KeyCode.AltMask | KeyCode.CtrlMask;
            case Curses.AltCtrlKeyPPage: return KeyCode.PageUp | KeyCode.AltMask | KeyCode.CtrlMask;
            case Curses.AltCtrlKeyHome: return KeyCode.Home | KeyCode.AltMask | KeyCode.CtrlMask;
            case Curses.AltCtrlKeyEnd: return KeyCode.End | KeyCode.AltMask | KeyCode.CtrlMask;
            default: return KeyCode.Null;
        }
    }

    private void ProcessMouseEvent (MouseFlags mouseFlag, Point pos)
    {
        bool WasButtonReleased (MouseFlags flag)
        {
            return flag.HasFlag (MouseFlags.Button1Released)
                   || flag.HasFlag (MouseFlags.Button2Released)
                   || flag.HasFlag (MouseFlags.Button3Released)
                   || flag.HasFlag (MouseFlags.Button4Released);
        }

        bool IsButtonNotPressed (MouseFlags flag)
        {
            return !flag.HasFlag (MouseFlags.Button1Pressed)
                   && !flag.HasFlag (MouseFlags.Button2Pressed)
                   && !flag.HasFlag (MouseFlags.Button3Pressed)
                   && !flag.HasFlag (MouseFlags.Button4Pressed);
        }

        bool IsButtonClickedOrDoubleClicked (MouseFlags flag)
        {
            return flag.HasFlag (MouseFlags.Button1Clicked)
                   || flag.HasFlag (MouseFlags.Button2Clicked)
                   || flag.HasFlag (MouseFlags.Button3Clicked)
                   || flag.HasFlag (MouseFlags.Button4Clicked)
                   || flag.HasFlag (MouseFlags.Button1DoubleClicked)
                   || flag.HasFlag (MouseFlags.Button2DoubleClicked)
                   || flag.HasFlag (MouseFlags.Button3DoubleClicked)
                   || flag.HasFlag (MouseFlags.Button4DoubleClicked);
        }

        Debug.WriteLine ($"CursesDriver: ({pos.X},{pos.Y}) - {mouseFlag}");


        if ((WasButtonReleased (mouseFlag) && IsButtonNotPressed (_lastMouseFlags)) || (IsButtonClickedOrDoubleClicked (mouseFlag) && _lastMouseFlags == 0))
        {
            return;
        }

        _lastMouseFlags = mouseFlag;

        var me = new MouseEvent { Flags = mouseFlag, X = pos.X, Y = pos.Y };
        Debug.WriteLine ($"CursesDriver: ({me.X},{me.Y}) - {me.Flags}");

        OnMouseEvent (new MouseEventEventArgs (me));
    }

    #region Color Handling

    /// <summary>Creates an Attribute from the provided curses-based foreground and background color numbers</summary>
    /// <param name="foreground">Contains the curses color number for the foreground (color, plus any attributes)</param>
    /// <param name="background">Contains the curses color number for the background (color, plus any attributes)</param>
    /// <returns></returns>
    private static Attribute MakeColor (short foreground, short background)
    {
        var v = (short)(foreground | (background << 4));

        // TODO: for TrueColor - Use InitExtendedPair
        Curses.InitColorPair (v, foreground, background);

        return new Attribute (
                              Curses.ColorPair (v),
                              CursesColorNumberToColorName (foreground),
                              CursesColorNumberToColorName (background)
                             );
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     In the CursesDriver, colors are encoded as an int. The foreground color is stored in the most significant 4
    ///     bits, and the background color is stored in the least significant 4 bits. The Terminal.GUi Color values are
    ///     converted to curses color encoding before being encoded.
    /// </remarks>
    public override Attribute MakeColor (in Color foreground, in Color background)
    {
        if (!RunningUnitTests)
        {
            return MakeColor (
                              ColorNameToCursesColorNumber (foreground.GetClosestNamedColor ()),
                              ColorNameToCursesColorNumber (background.GetClosestNamedColor ())
                             );
        }

        return new Attribute (
                              0,
                              foreground,
                              background
                             );
    }

    private static short ColorNameToCursesColorNumber (ColorName color)
    {
        switch (color)
        {
            case ColorName.Black:
                return Curses.COLOR_BLACK;
            case ColorName.Blue:
                return Curses.COLOR_BLUE;
            case ColorName.Green:
                return Curses.COLOR_GREEN;
            case ColorName.Cyan:
                return Curses.COLOR_CYAN;
            case ColorName.Red:
                return Curses.COLOR_RED;
            case ColorName.Magenta:
                return Curses.COLOR_MAGENTA;
            case ColorName.Yellow:
                return Curses.COLOR_YELLOW;
            case ColorName.Gray:
                return Curses.COLOR_WHITE;
            case ColorName.DarkGray:
                return Curses.COLOR_GRAY;
            case ColorName.BrightBlue:
                return Curses.COLOR_BLUE | Curses.COLOR_GRAY;
            case ColorName.BrightGreen:
                return Curses.COLOR_GREEN | Curses.COLOR_GRAY;
            case ColorName.BrightCyan:
                return Curses.COLOR_CYAN | Curses.COLOR_GRAY;
            case ColorName.BrightRed:
                return Curses.COLOR_RED | Curses.COLOR_GRAY;
            case ColorName.BrightMagenta:
                return Curses.COLOR_MAGENTA | Curses.COLOR_GRAY;
            case ColorName.BrightYellow:
                return Curses.COLOR_YELLOW | Curses.COLOR_GRAY;
            case ColorName.White:
                return Curses.COLOR_WHITE | Curses.COLOR_GRAY;
        }

        throw new ArgumentException ("Invalid color code");
    }

    private static ColorName CursesColorNumberToColorName (short color)
    {
        switch (color)
        {
            case Curses.COLOR_BLACK:
                return ColorName.Black;
            case Curses.COLOR_BLUE:
                return ColorName.Blue;
            case Curses.COLOR_GREEN:
                return ColorName.Green;
            case Curses.COLOR_CYAN:
                return ColorName.Cyan;
            case Curses.COLOR_RED:
                return ColorName.Red;
            case Curses.COLOR_MAGENTA:
                return ColorName.Magenta;
            case Curses.COLOR_YELLOW:
                return ColorName.Yellow;
            case Curses.COLOR_WHITE:
                return ColorName.Gray;
            case Curses.COLOR_GRAY:
                return ColorName.DarkGray;
            case Curses.COLOR_BLUE | Curses.COLOR_GRAY:
                return ColorName.BrightBlue;
            case Curses.COLOR_GREEN | Curses.COLOR_GRAY:
                return ColorName.BrightGreen;
            case Curses.COLOR_CYAN | Curses.COLOR_GRAY:
                return ColorName.BrightCyan;
            case Curses.COLOR_RED | Curses.COLOR_GRAY:
                return ColorName.BrightRed;
            case Curses.COLOR_MAGENTA | Curses.COLOR_GRAY:
                return ColorName.BrightMagenta;
            case Curses.COLOR_YELLOW | Curses.COLOR_GRAY:
                return ColorName.BrightYellow;
            case Curses.COLOR_WHITE | Curses.COLOR_GRAY:
                return ColorName.White;
        }

        throw new ArgumentException ("Invalid curses color code");
    }

    #endregion
}

internal static class Platform
{
    private static int _suspendSignal;

    /// <summary>Suspends the process by sending SIGTSTP to itself</summary>
    /// <returns>The suspend.</returns>
    public static bool Suspend ()
    {
        int signal = GetSuspendSignal ();

        if (signal == -1)
        {
            return false;
        }

        killpg (0, signal);

        return true;
    }

    private static int GetSuspendSignal ()
    {
        if (_suspendSignal != 0)
        {
            return _suspendSignal;
        }

        nint buf = Marshal.AllocHGlobal (8192);

        if (uname (buf) != 0)
        {
            Marshal.FreeHGlobal (buf);
            _suspendSignal = -1;

            return _suspendSignal;
        }

        try
        {
            switch (Marshal.PtrToStringAnsi (buf))
            {
                case "Darwin":
                case "DragonFly":
                case "FreeBSD":
                case "NetBSD":
                case "OpenBSD":
                    _suspendSignal = 18;

                    break;
                case "Linux":
                    // TODO: should fetch the machine name and
                    // if it is MIPS return 24
                    _suspendSignal = 20;

                    break;
                case "Solaris":
                    _suspendSignal = 24;

                    break;
                default:
                    _suspendSignal = -1;

                    break;
            }

            return _suspendSignal;
        }
        finally
        {
            Marshal.FreeHGlobal (buf);
        }
    }

    [DllImport ("libc")]
    private static extern int killpg (int pgrp, int pid);

    [DllImport ("libc")]
    private static extern int uname (nint buf);
}
