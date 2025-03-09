#nullable enable
// 
// WindowsDriver.cs: Windows specific driver
//

// HACK:
// WindowsConsole/Terminal has two issues:
// 1) Tearing can occur when the console is resized.
// 2) The values provided during Init (and the first WindowsConsole.EventType.WindowBufferSize) are not correct.
//
// If HACK_CHECK_WINCHANGED is defined then we ignore WindowsConsole.EventType.WindowBufferSize events
// and instead check the console size every 500ms in a thread in WidowsMainLoop.
// As of Windows 11 23H2 25947.1000 and/or WT 1.19.2682 tearing no longer occurs when using
// the WindowsConsole.EventType.WindowBufferSize event. However, on Init the window size is
// still incorrect so we still need this hack.

//#define HACK_CHECK_WINCHANGED

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Terminal.Gui.ConsoleDrivers.ConsoleKeyMapping;
using static Terminal.Gui.SpinnerStyle;

namespace Terminal.Gui;

internal class WindowsDriver : ConsoleDriver
{
    private readonly bool _isWindowsTerminal;

    private WindowsConsole.SmallRect _damageRegion;
    private bool _isButtonDoubleClicked;
    private bool _isButtonPressed;
    private bool _isButtonReleased;
    private bool _isOneFingerDoubleClicked;

    private WindowsConsole.ButtonState? _lastMouseButtonPressed;
    private WindowsMainLoop? _mainLoopDriver;
    private WindowsConsole.ExtendedCharInfo [] _outputBuffer = new WindowsConsole.ExtendedCharInfo [0 * 0];
    private Point? _point;
    private Point _pointMove;
    private bool _processButtonClick;

    public WindowsDriver ()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            WinConsole = new ();

            // otherwise we're probably running in unit tests
            Clipboard = new WindowsClipboard ();
        }
        else
        {
            Clipboard = new FakeDriver.FakeClipboard ();
        }

        // TODO: if some other Windows-based terminal supports true color, update this logic to not
        // force 16color mode (.e.g ConEmu which really doesn't work well at all).
        _isWindowsTerminal = _isWindowsTerminal =
                                 Environment.GetEnvironmentVariable ("WT_SESSION") is { } || Environment.GetEnvironmentVariable ("VSAPPIDNAME") != null;

        if (!_isWindowsTerminal)
        {
            Force16Colors = true;
        }
    }

    public override bool SupportsTrueColor => RunningUnitTests || (Environment.OSVersion.Version.Build >= 14931 && _isWindowsTerminal);

    public WindowsConsole? WinConsole { get; private set; }

    public static WindowsConsole.KeyEventRecord FromVKPacketToKeyEventRecord (WindowsConsole.KeyEventRecord keyEvent)
    {
        if (keyEvent.wVirtualKeyCode != (VK)ConsoleKey.Packet)
        {
            return keyEvent;
        }

        var mod = new ConsoleModifiers ();

        if (keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.ShiftPressed))
        {
            mod |= ConsoleModifiers.Shift;
        }

        if (keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightAltPressed)
            || keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftAltPressed))
        {
            mod |= ConsoleModifiers.Alt;
        }

        if (keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftControlPressed)
            || keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightControlPressed))
        {
            mod |= ConsoleModifiers.Control;
        }

        var cKeyInfo = new ConsoleKeyInfo (
                                           keyEvent.UnicodeChar,
                                           (ConsoleKey)keyEvent.wVirtualKeyCode,
                                           mod.HasFlag (ConsoleModifiers.Shift),
                                           mod.HasFlag (ConsoleModifiers.Alt),
                                           mod.HasFlag (ConsoleModifiers.Control));
        cKeyInfo = DecodeVKPacketToKConsoleKeyInfo (cKeyInfo);
        uint scanCode = GetScanCodeFromConsoleKeyInfo (cKeyInfo);

        return new WindowsConsole.KeyEventRecord
        {
            UnicodeChar = cKeyInfo.KeyChar,
            bKeyDown = keyEvent.bKeyDown,
            dwControlKeyState = keyEvent.dwControlKeyState,
            wRepeatCount = keyEvent.wRepeatCount,
            wVirtualKeyCode = (VK)cKeyInfo.Key,
            wVirtualScanCode = (ushort)scanCode
        };
    }

    public override bool IsRuneSupported (Rune rune) { return base.IsRuneSupported (rune) && rune.IsBmp; }

    public override void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
    {
        var input = new WindowsConsole.InputRecord
        {
            EventType = WindowsConsole.EventType.Key
        };

        var keyEvent = new WindowsConsole.KeyEventRecord
        {
            bKeyDown = true
        };
        var controlKey = new WindowsConsole.ControlKeyState ();

        if (shift)
        {
            controlKey |= WindowsConsole.ControlKeyState.ShiftPressed;
            keyEvent.UnicodeChar = '\0';
            keyEvent.wVirtualKeyCode = VK.SHIFT;
        }

        if (alt)
        {
            controlKey |= WindowsConsole.ControlKeyState.LeftAltPressed;
            controlKey |= WindowsConsole.ControlKeyState.RightAltPressed;
            keyEvent.UnicodeChar = '\0';
            keyEvent.wVirtualKeyCode = VK.MENU;
        }

        if (control)
        {
            controlKey |= WindowsConsole.ControlKeyState.LeftControlPressed;
            controlKey |= WindowsConsole.ControlKeyState.RightControlPressed;
            keyEvent.UnicodeChar = '\0';
            keyEvent.wVirtualKeyCode = VK.CONTROL;
        }

        keyEvent.dwControlKeyState = controlKey;

        input.KeyEvent = keyEvent;

        if (shift || alt || control)
        {
            ProcessInput (input);
        }

        keyEvent.UnicodeChar = keyChar;

        //if ((uint)key < 255) {
        //	keyEvent.wVirtualKeyCode = (ushort)key;
        //} else {
        //	keyEvent.wVirtualKeyCode = '\0';
        //}
        keyEvent.wVirtualKeyCode = (VK)key;

        input.KeyEvent = keyEvent;

        try
        {
            ProcessInput (input);
        }
        catch (OverflowException)
        { }
        finally
        {
            keyEvent.bKeyDown = false;
            input.KeyEvent = keyEvent;
            ProcessInput (input);
        }
    }

    /// <inheritdoc />
    internal override IAnsiResponseParser GetParser () => _parser;


    public override void WriteRaw (string str)
    {
        WinConsole?.WriteANSI (str);
    }

    #region Not Implemented

    public override void Suspend () { throw new NotImplementedException (); }

    #endregion

    public static WindowsConsole.ConsoleKeyInfoEx ToConsoleKeyInfoEx (WindowsConsole.KeyEventRecord keyEvent)
    {
        WindowsConsole.ControlKeyState state = keyEvent.dwControlKeyState;

        bool shift = (state & WindowsConsole.ControlKeyState.ShiftPressed) != 0;
        bool alt = (state & (WindowsConsole.ControlKeyState.LeftAltPressed | WindowsConsole.ControlKeyState.RightAltPressed)) != 0;
        bool control = (state & (WindowsConsole.ControlKeyState.LeftControlPressed | WindowsConsole.ControlKeyState.RightControlPressed)) != 0;
        bool capslock = (state & WindowsConsole.ControlKeyState.CapslockOn) != 0;
        bool numlock = (state & WindowsConsole.ControlKeyState.NumlockOn) != 0;
        bool scrolllock = (state & WindowsConsole.ControlKeyState.ScrolllockOn) != 0;

        var cki = new ConsoleKeyInfo (keyEvent.UnicodeChar, (ConsoleKey)keyEvent.wVirtualKeyCode, shift, alt, control);

        return new WindowsConsole.ConsoleKeyInfoEx (cki, capslock, numlock, scrolllock);
    }

    #region Cursor Handling

    private CursorVisibility? _cachedCursorVisibility;

    public override void UpdateCursor ()
    {
        if (RunningUnitTests)
        {
            return;
        }

        if (Col < 0 || Row < 0 || Col >= Cols || Row >= Rows)
        {
            GetCursorVisibility (out CursorVisibility cursorVisibility);
            _cachedCursorVisibility = cursorVisibility;
            SetCursorVisibility (CursorVisibility.Invisible);

            return;
        }

        var position = new WindowsConsole.Coord
        {
            X = (short)Col,
            Y = (short)Row
        };

        if (Force16Colors)
        {
            WinConsole?.SetCursorPosition (position);
        }
        else
        {
            var sb = new StringBuilder ();
            EscSeqUtils.CSI_AppendCursorPosition (sb, position.Y + 1, position.X + 1);
            WinConsole?.WriteANSI (sb.ToString ());
        }

        if (_cachedCursorVisibility is { })
        {
            SetCursorVisibility (_cachedCursorVisibility.Value);
        }
        //EnsureCursorVisibility ();
    }

    /// <inheritdoc/>
    public override bool GetCursorVisibility (out CursorVisibility visibility)
    {
        if (WinConsole is { })
        {
            bool result = WinConsole.GetCursorVisibility (out visibility);

            if (_cachedCursorVisibility is { } && visibility != _cachedCursorVisibility)
            {
                _cachedCursorVisibility = visibility;
            }

            return result;
        }

        visibility = _cachedCursorVisibility ?? CursorVisibility.Default;

        return visibility != CursorVisibility.Invisible;
    }

    /// <inheritdoc/>
    public override bool SetCursorVisibility (CursorVisibility visibility)
    {
        _cachedCursorVisibility = visibility;

        if (Force16Colors)
        {
            return WinConsole is null || WinConsole.SetCursorVisibility (visibility);
        }
        else
        {
            var sb = new StringBuilder ();
            sb.Append (visibility != CursorVisibility.Invisible ? EscSeqUtils.CSI_ShowCursor : EscSeqUtils.CSI_HideCursor);
            return WinConsole?.WriteANSI (sb.ToString ()) ?? false;
        }
    }
    #endregion Cursor Handling

    public override bool UpdateScreen ()
    {
        bool updated = false;
        Size windowSize = WinConsole?.GetConsoleBufferWindow (out Point _) ?? new Size (Cols, Rows);

        if (!windowSize.IsEmpty && (windowSize.Width != Cols || windowSize.Height != Rows))
        {
            return updated;
        }

        var bufferCoords = new WindowsConsole.Coord
        {
            X = (short)Cols, //Clip.Width,
            Y = (short)Rows, //Clip.Height
        };

        for (var row = 0; row < Rows; row++)
        {
            if (!_dirtyLines! [row])
            {
                continue;
            }

            _dirtyLines [row] = false;
            updated = true;

            for (var col = 0; col < Cols; col++)
            {
                int position = row * Cols + col;
                _outputBuffer [position].Attribute = Contents! [row, col].Attribute.GetValueOrDefault ();

                if (Contents [row, col].IsDirty == false)
                {
                    _outputBuffer [position].Empty = true;
                    _outputBuffer [position].Char = (char)Rune.ReplacementChar.Value;

                    continue;
                }

                _outputBuffer [position].Empty = false;

                if (Contents [row, col].Rune.IsBmp)
                {
                    _outputBuffer [position].Char = (char)Contents [row, col].Rune.Value;
                }
                else
                {
                    //_outputBuffer [position].Empty = true;
                    _outputBuffer [position].Char = (char)Rune.ReplacementChar.Value;

                    if (Contents [row, col].Rune.GetColumns () > 1 && col + 1 < Cols)
                    {
                        // TODO: This is a hack to deal with non-BMP and wide characters.
                        col++;
                        position = row * Cols + col;
                        _outputBuffer [position].Empty = false;
                        _outputBuffer [position].Char = ' ';
                    }
                }
            }
        }

        _damageRegion = new WindowsConsole.SmallRect
        {
            Top = 0,
            Left = 0,
            Bottom = (short)Rows,
            Right = (short)Cols
        };

        if (!RunningUnitTests
            && WinConsole != null
            && !WinConsole.WriteToConsole (new (Cols, Rows), _outputBuffer, bufferCoords, _damageRegion, Force16Colors))
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        WindowsConsole.SmallRect.MakeEmpty (ref _damageRegion);

        return updated;
    }

    public override void End ()
    {
        if (_mainLoopDriver is { })
        {
#if HACK_CHECK_WINCHANGED

            _mainLoopDriver.WinChanged -= ChangeWin;
#endif
        }

        _mainLoopDriver = null;

        WinConsole?.Cleanup ();
        WinConsole = null;

        if (!RunningUnitTests && _isWindowsTerminal)
        {
            // Disable alternative screen buffer.
            Console.Out.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
        }
    }

    public override MainLoop Init ()
    {
        _mainLoopDriver = new WindowsMainLoop (this);

        if (!RunningUnitTests)
        {
            try
            {
                if (WinConsole is { })
                {
                    // BUGBUG: The results from GetConsoleOutputWindow are incorrect when called from Init.
                    // Our thread in WindowsMainLoop.CheckWin will get the correct results. See #if HACK_CHECK_WINCHANGED
                    Size winSize = WinConsole.GetConsoleOutputWindow (out Point _);
                    Cols = winSize.Width;
                    Rows = winSize.Height;
                    OnSizeChanged (new SizeChangedEventArgs (new (Cols, Rows)));
                }

                WindowsConsole.SmallRect.MakeEmpty (ref _damageRegion);

                if (_isWindowsTerminal)
                {
                    Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
                }
            }
            catch (Win32Exception e)
            {
                // We are being run in an environment that does not support a console
                // such as a unit test, or a pipe.
                Debug.WriteLine ($"Likely running unit tests. Setting WinConsole to null so we can test it elsewhere. Exception: {e}");
                WinConsole = null;
            }
        }

        CurrentAttribute = new Attribute (Color.White, Color.Black);

        _outputBuffer = new WindowsConsole.ExtendedCharInfo [Rows * Cols];
        // CONCURRENCY: Unsynchronized access to Clip is not safe.
        Clip = new (Screen);

        _damageRegion = new WindowsConsole.SmallRect
        {
            Top = 0,
            Left = 0,
            Bottom = (short)Rows,
            Right = (short)Cols
        };

        ClearContents ();

#if HACK_CHECK_WINCHANGED
        _mainLoopDriver.WinChanged = ChangeWin;
#endif

        if (!RunningUnitTests)
        {
        WinConsole?.SetInitialCursorVisibility ();
        }

        return new MainLoop (_mainLoopDriver);
    }

    private AnsiResponseParser<WindowsConsole.InputRecord> _parser = new ();

    internal void ProcessInput (WindowsConsole.InputRecord inputEvent)
    {
        foreach (var e in Parse (inputEvent))
        {
            ProcessInputAfterParsing (e);
        }
    }

    internal void ProcessInputAfterParsing (WindowsConsole.InputRecord inputEvent)
    {

        switch (inputEvent.EventType)
        {
            case WindowsConsole.EventType.Key:
                if (inputEvent.KeyEvent.wVirtualKeyCode == (VK)ConsoleKey.Packet)
                {
                    // Used to pass Unicode characters as if they were keystrokes.
                    // The VK_PACKET key is the low word of a 32-bit
                    // Virtual Key value used for non-keyboard input methods.
                    inputEvent.KeyEvent = FromVKPacketToKeyEventRecord (inputEvent.KeyEvent);
                }

                WindowsConsole.ConsoleKeyInfoEx keyInfo = ToConsoleKeyInfoEx (inputEvent.KeyEvent);

                //Debug.WriteLine ($"event: KBD: {GetKeyboardLayoutName()} {inputEvent.ToString ()} {keyInfo.ToString (keyInfo)}");

                KeyCode map = MapKey (keyInfo);

                if (map == KeyCode.Null)
                {
                    break;
                }

                // This follows convention in NetDriver
                OnKeyDown (new Key (map));
                OnKeyUp (new Key (map));

                break;

            case WindowsConsole.EventType.Mouse:
                MouseEventArgs me = ToDriverMouse (inputEvent.MouseEvent);

                if (/*me is null ||*/ me.Flags == MouseFlags.None)
                {
                    break;
                }

                OnMouseEvent (me);

                if (_processButtonClick)
                {
                    OnMouseEvent (new ()
                    {
                        Position = me.Position,
                        Flags = ProcessButtonClick (inputEvent.MouseEvent)
                    });
                }

                break;

            case WindowsConsole.EventType.Focus:
                break;

#if !HACK_CHECK_WINCHANGED
            case WindowsConsole.EventType.WindowBufferSize:

                Cols = inputEvent.WindowBufferSizeEvent._size.X;
                Rows = inputEvent.WindowBufferSizeEvent._size.Y;
                Application.Screen = new (0, 0, Cols, Rows);

                ResizeScreen ();
                ClearContents ();
                Application.Top?.SetNeedsLayout ();
                Application.LayoutAndDraw ();

                break;
#endif
        }
    }

    private IEnumerable<WindowsConsole.InputRecord> Parse (WindowsConsole.InputRecord inputEvent)
    {
        if (inputEvent.EventType != WindowsConsole.EventType.Key)
        {
            yield return inputEvent;
            yield break;
        }

        // Swallow key up events - they are unreliable
        if (!inputEvent.KeyEvent.bKeyDown)
        {
            yield break;
        }

        foreach (var i in ShouldReleaseParserHeldKeys ())
        {
            yield return i;
        }

        foreach (Tuple<char, WindowsConsole.InputRecord> output in
                 _parser.ProcessInput (Tuple.Create (inputEvent.KeyEvent.UnicodeChar, inputEvent)))
        {
            yield return output.Item2;
        }
    }

    public IEnumerable<WindowsConsole.InputRecord> ShouldReleaseParserHeldKeys ()
    {
        if (_parser.State == AnsiResponseParserState.ExpectingEscapeSequence &&
            DateTime.Now - _parser.StateChangedAt > EscTimeout)
        {
            return _parser.Release ().Select (o => o.Item2);
        }

        return [];
    }

#if HACK_CHECK_WINCHANGED
    private void ChangeWin (object s, SizeChangedEventArgs e)
    {
        if (e.Size is null)
        {
            return;
        }

        int w = e.Size.Value.Width;

        if (w == Cols - 3 && e.Size.Value.Height < Rows)
        {
            w += 3;
        }

        Left = 0;
        Top = 0;
        Cols = e.Size.Value.Width;
        Rows = e.Size.Value.Height;

        if (!RunningUnitTests)
        {
            Size newSize = WinConsole.SetConsoleWindow (
                                                        (short)Math.Max (w, 16),
                                                        (short)Math.Max (e.Size.Value.Height, 0));

            Cols = newSize.Width;
            Rows = newSize.Height;
        }

        ResizeScreen ();
        ClearContents ();
        OnSizeChanged (new SizeChangedEventArgs (new (Cols, Rows)));
    }
#endif

    public static KeyCode MapKey (WindowsConsole.ConsoleKeyInfoEx keyInfoEx)
    {
        ConsoleKeyInfo keyInfo = keyInfoEx.ConsoleKeyInfo;

        switch (keyInfo.Key)
        {
            case ConsoleKey.D0:
            case ConsoleKey.D1:
            case ConsoleKey.D2:
            case ConsoleKey.D3:
            case ConsoleKey.D4:
            case ConsoleKey.D5:
            case ConsoleKey.D6:
            case ConsoleKey.D7:
            case ConsoleKey.D8:
            case ConsoleKey.D9:
            case ConsoleKey.NumPad0:
            case ConsoleKey.NumPad1:
            case ConsoleKey.NumPad2:
            case ConsoleKey.NumPad3:
            case ConsoleKey.NumPad4:
            case ConsoleKey.NumPad5:
            case ConsoleKey.NumPad6:
            case ConsoleKey.NumPad7:
            case ConsoleKey.NumPad8:
            case ConsoleKey.NumPad9:
            case ConsoleKey.Oem1:
            case ConsoleKey.Oem2:
            case ConsoleKey.Oem3:
            case ConsoleKey.Oem4:
            case ConsoleKey.Oem5:
            case ConsoleKey.Oem6:
            case ConsoleKey.Oem7:
            case ConsoleKey.Oem8:
            case ConsoleKey.Oem102:
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
                // These virtual key codes are mapped differently depending on the keyboard layout in use.
                // We use the Win32 API to map them to the correct character.
                uint mapResult = MapVKtoChar ((VK)keyInfo.Key);

                if (mapResult == 0)
                {
                    // There is no mapping - this should not happen
                    Debug.Assert (true, $@"Unable to map the virtual key code {keyInfo.Key}.");

                    return KeyCode.Null;
                }

                // An un-shifted character value is in the low order word of the return value.
                var mappedChar = (char)(mapResult & 0x0000FFFF);

                if (keyInfo.KeyChar == 0)
                {
                    // If the keyChar is 0, keyInfo.Key value is not a printable character. 

                    // Dead keys (diacritics) are indicated by setting the top bit of the return value. 
                    if ((mapResult & 0x80000000) != 0)
                    {
                        // Dead key (e.g. Oem2 '~'/'^' on POR keyboard)
                        // Option 1: Throw it out. 
                        //    - Apps will never see the dead keys
                        //    - If user presses a key that can be combined with the dead key ('a'), the right thing happens (app will see '�').
                        //      - NOTE: With Dead Keys, KeyDown != KeyUp. The KeyUp event will have just the base char ('a').
                        //    - If user presses dead key again, the right thing happens (app will see `~~`)
                        //    - This is what Notepad etc... appear to do
                        // Option 2: Expand the API to indicate the KeyCode is a dead key
                        //    - Enables apps to do their own dead key processing
                        //    - Adds complexity; no dev has asked for this (yet).
                        // We choose Option 1 for now.
                        return KeyCode.Null;

                        // Note: Ctrl-Deadkey (like Oem3 '`'/'~` on ENG) can't be supported.
                        // Sadly, the charVal is just the deadkey and subsequent key events do not contain
                        // any info that the previous event was a deadkey.
                        // Note WT does not support Ctrl-Deadkey either.
                    }

                    if (keyInfo.Modifiers != 0)
                    {
                        // These Oem keys have well-defined chars. We ensure the representative char is used.
                        // If we don't do this, then on some keyboard layouts the wrong char is 
                        // returned (e.g. on ENG OemPlus un-shifted is =, not +). This is important
                        // for key persistence ("Ctrl++" vs. "Ctrl+=").
                        mappedChar = keyInfo.Key switch
                        {
                            ConsoleKey.OemPeriod => '.',
                            ConsoleKey.OemComma => ',',
                            ConsoleKey.OemPlus => '+',
                            ConsoleKey.OemMinus => '-',
                            _ => mappedChar
                        };
                    }

                    // Return the mappedChar with modifiers. Because mappedChar is un-shifted, if Shift was down
                    // we should keep it
                    return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)mappedChar);
                }

                // KeyChar is printable
                if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt) && keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
                {
                    // AltGr support - AltGr is equivalent to Ctrl+Alt - the correct char is in KeyChar
                    return (KeyCode)keyInfo.KeyChar;
                }

                if (keyInfo.Modifiers != ConsoleModifiers.Shift)
                {
                    // If Shift wasn't down we don't need to do anything but return the mappedChar
                    return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)mappedChar);
                }

                // Strip off Shift - We got here because they KeyChar from Windows is the shifted char (e.g. "�")
                // and passing on Shift would be redundant.
                return MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.KeyChar);
        }

        // A..Z are special cased:
        // - Alone, they represent lowercase a...z
        // - With ShiftMask they are A..Z
        // - If CapsLock is on the above is reversed.
        // - If Alt and/or Ctrl are present, treat as upper case
        if (keyInfo.Key is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            if (keyInfo.KeyChar == 0)
            {
                // KeyChar is not printable - possibly an AltGr key?
                // AltGr support - AltGr is equivalent to Ctrl+Alt
                if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt) && keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
                {
                    return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)(uint)keyInfo.Key);
                }
            }

            if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt) || keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control))
            {
                return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)(uint)keyInfo.Key);
            }

            if ((keyInfo.Modifiers == ConsoleModifiers.Shift) ^ keyInfoEx.CapsLock)
            {
                // If (ShiftMask is on and CapsLock is off) or (ShiftMask is off and CapsLock is on) add the ShiftMask
                if (char.IsUpper (keyInfo.KeyChar))
                {
                    if (keyInfo.KeyChar <= 'Z')
                    {
                        return (KeyCode)keyInfo.Key | KeyCode.ShiftMask;
                }

                    // Always return the KeyChar because it may be an Á, À with Oem1, etc
                    return (KeyCode)keyInfo.KeyChar;
            }
            }

            if (keyInfo.KeyChar <= 'z')
            {
                return (KeyCode)keyInfo.Key;
            }

            // Always return the KeyChar because it may be an á, à with Oem1, etc
            return (KeyCode)keyInfo.KeyChar;
        }

        // Handle control keys whose VK codes match the related ASCII value (those below ASCII 33) like ESC
        if (Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key))
        {
            // If the key is JUST a modifier, return it as just that key
            if (keyInfo.Key == (ConsoleKey)VK.SHIFT)
            { // Shift 16
                return KeyCode.ShiftMask;
            }

            if (keyInfo.Key == (ConsoleKey)VK.CONTROL)
            { // Ctrl 17
                return KeyCode.CtrlMask;
            }

            if (keyInfo.Key == (ConsoleKey)VK.MENU)
            { // Alt 18
                return KeyCode.AltMask;
            }

            if (keyInfo.KeyChar == 0)
            {
                return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
            }

            if (keyInfo.Key != ConsoleKey.None)
            {
                return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
            }

            return MapToKeyCodeModifiers (keyInfo.Modifiers & ~ConsoleModifiers.Shift, (KeyCode)keyInfo.KeyChar);
        }

        // Handle control keys (e.g. CursorUp)
        if (Enum.IsDefined (typeof (KeyCode), (uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint))
        {
            return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)((uint)keyInfo.Key + (uint)KeyCode.MaxCodePoint));
        }

        return MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
    }

    private MouseFlags ProcessButtonClick (WindowsConsole.MouseEventRecord mouseEvent)
    {
        MouseFlags mouseFlag = 0;

        switch (_lastMouseButtonPressed)
        {
            case WindowsConsole.ButtonState.Button1Pressed:
                mouseFlag = MouseFlags.Button1Clicked;

                break;

            case WindowsConsole.ButtonState.Button2Pressed:
                mouseFlag = MouseFlags.Button2Clicked;

                break;

            case WindowsConsole.ButtonState.RightmostButtonPressed:
                mouseFlag = MouseFlags.Button3Clicked;

                break;
        }

        _point = new Point
        {
            X = mouseEvent.MousePosition.X,
            Y = mouseEvent.MousePosition.Y
        };
        _lastMouseButtonPressed = null;
        _isButtonReleased = false;
        _processButtonClick = false;
        _point = null;

        return mouseFlag;
    }

    private async Task ProcessButtonDoubleClickedAsync ()
    {
        await Task.Delay (200);
        _isButtonDoubleClicked = false;
        _isOneFingerDoubleClicked = false;

        //buttonPressedCount = 0;
    }

    private async Task ProcessContinuousButtonPressedAsync (MouseFlags mouseFlag)
    {
        // When a user presses-and-holds, start generating pressed events every `startDelay`
        // After `iterationsUntilFast` iterations, speed them up to `fastDelay` ms
        const int START_DELAY = 500;
        const int ITERATIONS_UNTIL_FAST = 4;
        const int FAST_DELAY = 50;

        int iterations = 0;
        int delay = START_DELAY;
        while (_isButtonPressed)
        {
            // TODO: This makes IConsoleDriver dependent on Application, which is not ideal. This should be moved to Application.
            View? view = Application.WantContinuousButtonPressedView;

            if (view is null)
            {
                break;
            }

            if (iterations++ >= ITERATIONS_UNTIL_FAST)
            {
                delay = FAST_DELAY;
            }
            await Task.Delay (delay);

            var me = new MouseEventArgs
            {
                ScreenPosition = _pointMove,
                Flags = mouseFlag
            };

            //Debug.WriteLine($"ProcessContinuousButtonPressedAsync: {view}");
            if (_isButtonPressed && (mouseFlag & MouseFlags.ReportMousePosition) == 0)
            {
                // TODO: This makes IConsoleDriver dependent on Application, which is not ideal. This should be moved to Application.
                Application.Invoke (() => OnMouseEvent (me));
            }
        }
    }

    private void ResizeScreen ()
    {
        _outputBuffer = new WindowsConsole.ExtendedCharInfo [Rows * Cols];
        // CONCURRENCY: Unsynchronized access to Clip is not safe.
        Clip = new (Screen);

        _damageRegion = new WindowsConsole.SmallRect
        {
            Top = 0,
            Left = 0,
            Bottom = (short)Rows,
            Right = (short)Cols
        };
        _dirtyLines = new bool [Rows];

        WinConsole?.ForceRefreshCursorVisibility ();
    }

    private static MouseFlags SetControlKeyStates (WindowsConsole.MouseEventRecord mouseEvent, MouseFlags mouseFlag)
    {
        if (mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightControlPressed)
            || mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftControlPressed))
        {
            mouseFlag |= MouseFlags.ButtonCtrl;
        }

        if (mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.ShiftPressed))
        {
            mouseFlag |= MouseFlags.ButtonShift;
        }

        if (mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightAltPressed)
            || mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftAltPressed))
        {
            mouseFlag |= MouseFlags.ButtonAlt;
        }

        return mouseFlag;
    }

    [CanBeNull]
    private MouseEventArgs ToDriverMouse (WindowsConsole.MouseEventRecord mouseEvent)
    {
        var mouseFlag = MouseFlags.AllEvents;

        //Debug.WriteLine ($"ToDriverMouse: {mouseEvent}");

        if (_isButtonDoubleClicked || _isOneFingerDoubleClicked)
        {
            // TODO: This makes IConsoleDriver dependent on Application, which is not ideal. This should be moved to Application.
            Application.MainLoop!.AddIdle (
                                          () =>
                                          {
                                              Task.Run (async () => await ProcessButtonDoubleClickedAsync ());

                                              return false;
                                          });
        }

        // The ButtonState member of the MouseEvent structure has bit corresponding to each mouse button.
        // This will tell when a mouse button is pressed. When the button is released this event will
        // be fired with its bit set to 0. So when the button is up ButtonState will be 0.
        // To map to the correct driver events we save the last pressed mouse button, so we can
        // map to the correct clicked event.
        if ((_lastMouseButtonPressed is { } || _isButtonReleased) && mouseEvent.ButtonState != 0)
        {
            _lastMouseButtonPressed = null;

            //isButtonPressed = false;
            _isButtonReleased = false;
        }

        var p = new Point
        {
            X = mouseEvent.MousePosition.X,
            Y = mouseEvent.MousePosition.Y
        };

        if ((mouseEvent.ButtonState != 0 && mouseEvent.EventFlags == 0 && _lastMouseButtonPressed is null && !_isButtonDoubleClicked)
            || (_lastMouseButtonPressed == null
                && mouseEvent.EventFlags.HasFlag (WindowsConsole.EventFlags.MouseMoved)
                && mouseEvent.ButtonState != 0
                && !_isButtonReleased
                && !_isButtonDoubleClicked))
        {
            switch (mouseEvent.ButtonState)
            {
                case WindowsConsole.ButtonState.Button1Pressed:
                    mouseFlag = MouseFlags.Button1Pressed;

                    break;

                case WindowsConsole.ButtonState.Button2Pressed:
                    mouseFlag = MouseFlags.Button2Pressed;

                    break;

                case WindowsConsole.ButtonState.RightmostButtonPressed:
                    mouseFlag = MouseFlags.Button3Pressed;

                    break;
            }

            if (_point is null)
            {
                _point = p;
            }

            if (mouseEvent.EventFlags.HasFlag (WindowsConsole.EventFlags.MouseMoved))
            {
                _pointMove = p;
                mouseFlag |= MouseFlags.ReportMousePosition;
                _isButtonReleased = false;
                _processButtonClick = false;
            }

            _lastMouseButtonPressed = mouseEvent.ButtonState;
            _isButtonPressed = true;

            if ((mouseFlag & MouseFlags.ReportMousePosition) == 0)
            {
                // TODO: This makes IConsoleDriver dependent on Application, which is not ideal. This should be moved to Application.
                Application.MainLoop!.AddIdle (
                                              () =>
                                              {
                                                  Task.Run (async () => await ProcessContinuousButtonPressedAsync (mouseFlag));

                                                  return false;
                                              });
            }
        }
        else if (_lastMouseButtonPressed != null
                 && mouseEvent.EventFlags == 0
                 && !_isButtonReleased
                 && !_isButtonDoubleClicked
                 && !_isOneFingerDoubleClicked)
        {
            switch (_lastMouseButtonPressed)
            {
                case WindowsConsole.ButtonState.Button1Pressed:
                    mouseFlag = MouseFlags.Button1Released;

                    break;

                case WindowsConsole.ButtonState.Button2Pressed:
                    mouseFlag = MouseFlags.Button2Released;

                    break;

                case WindowsConsole.ButtonState.RightmostButtonPressed:
                    mouseFlag = MouseFlags.Button3Released;

                    break;
            }

            _isButtonPressed = false;
            _isButtonReleased = true;

            if (_point is { } && ((Point)_point).X == mouseEvent.MousePosition.X && ((Point)_point).Y == mouseEvent.MousePosition.Y)
            {
                _processButtonClick = true;
            }
            else
            {
                _point = null;
            }
            _processButtonClick = true;

        }
        else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved
                 && !_isOneFingerDoubleClicked
                 && _isButtonReleased
                 && p == _point)
        {
            mouseFlag = ProcessButtonClick (mouseEvent);
        }
        else if (mouseEvent.EventFlags.HasFlag (WindowsConsole.EventFlags.DoubleClick))
        {
            switch (mouseEvent.ButtonState)
            {
                case WindowsConsole.ButtonState.Button1Pressed:
                    mouseFlag = MouseFlags.Button1DoubleClicked;

                    break;

                case WindowsConsole.ButtonState.Button2Pressed:
                    mouseFlag = MouseFlags.Button2DoubleClicked;

                    break;

                case WindowsConsole.ButtonState.RightmostButtonPressed:
                    mouseFlag = MouseFlags.Button3DoubleClicked;

                    break;
            }

            _isButtonDoubleClicked = true;
        }
        else if (mouseEvent.EventFlags == 0 && mouseEvent.ButtonState != 0 && _isButtonDoubleClicked)
        {
            switch (mouseEvent.ButtonState)
            {
                case WindowsConsole.ButtonState.Button1Pressed:
                    mouseFlag = MouseFlags.Button1TripleClicked;

                    break;

                case WindowsConsole.ButtonState.Button2Pressed:
                    mouseFlag = MouseFlags.Button2TripleClicked;

                    break;

                case WindowsConsole.ButtonState.RightmostButtonPressed:
                    mouseFlag = MouseFlags.Button3TripleClicked;

                    break;
            }

            _isButtonDoubleClicked = false;
        }
        else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseWheeled)
        {
            switch ((int)mouseEvent.ButtonState)
            {
                case int v when v > 0:
                    mouseFlag = MouseFlags.WheeledUp;

                    break;

                case int v when v < 0:
                    mouseFlag = MouseFlags.WheeledDown;

                    break;
            }
        }
        else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseWheeled && mouseEvent.ControlKeyState == WindowsConsole.ControlKeyState.ShiftPressed)
        {
            switch ((int)mouseEvent.ButtonState)
            {
                case int v when v > 0:
                    mouseFlag = MouseFlags.WheeledLeft;

                    break;

                case int v when v < 0:
                    mouseFlag = MouseFlags.WheeledRight;

                    break;
            }
        }
        else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseHorizontalWheeled)
        {
            switch ((int)mouseEvent.ButtonState)
            {
                case int v when v < 0:
                    mouseFlag = MouseFlags.WheeledLeft;

                    break;

                case int v when v > 0:
                    mouseFlag = MouseFlags.WheeledRight;

                    break;
            }
        }
        else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved)
        {
            mouseFlag = MouseFlags.ReportMousePosition;

            if (mouseEvent.MousePosition.X != _pointMove.X || mouseEvent.MousePosition.Y != _pointMove.Y)
            {
                _pointMove = new Point (mouseEvent.MousePosition.X, mouseEvent.MousePosition.Y);
            }
        }
        else if (mouseEvent is { ButtonState: 0, EventFlags: 0 })
        {
            // This happens on a double or triple click event.
            mouseFlag = MouseFlags.None;
        }

        mouseFlag = SetControlKeyStates (mouseEvent, mouseFlag);

        //System.Diagnostics.Debug.WriteLine (
        //	$"point.X:{(point is { } ? ((Point)point).X : -1)};point.Y:{(point is { } ? ((Point)point).Y : -1)}");

        return new MouseEventArgs
        {
            Position = new (mouseEvent.MousePosition.X, mouseEvent.MousePosition.Y),
            Flags = mouseFlag
        };
    }
}