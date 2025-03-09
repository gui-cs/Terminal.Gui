//
// FakeDriver.cs: A fake IConsoleDriver for unit tests. 
//

using System.Diagnostics;
using System.Runtime.InteropServices;
using Terminal.Gui.ConsoleDrivers;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui;

/// <summary>Implements a mock IConsoleDriver for unit testing</summary>
public class FakeDriver : ConsoleDriver
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class Behaviors
    {
        public Behaviors (
            bool useFakeClipboard = false,
            bool fakeClipboardAlwaysThrowsNotSupportedException = false,
            bool fakeClipboardIsSupportedAlwaysTrue = false
        )
        {
            UseFakeClipboard = useFakeClipboard;
            FakeClipboardAlwaysThrowsNotSupportedException = fakeClipboardAlwaysThrowsNotSupportedException;
            FakeClipboardIsSupportedAlwaysFalse = fakeClipboardIsSupportedAlwaysTrue;

            // double check usage is correct
            Debug.Assert (useFakeClipboard == false && fakeClipboardAlwaysThrowsNotSupportedException == false);
            Debug.Assert (useFakeClipboard == false && fakeClipboardIsSupportedAlwaysTrue == false);
        }

        public bool FakeClipboardAlwaysThrowsNotSupportedException { get; internal set; }
        public bool FakeClipboardIsSupportedAlwaysFalse { get; internal set; }
        public bool UseFakeClipboard { get; internal set; }
    }

    public static Behaviors FakeBehaviors = new ();
    public override bool SupportsTrueColor => false;

    /// <inheritdoc />
    public override void WriteRaw (string ansi)
    {

    }

    public FakeDriver ()
    {
        Cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = FakeConsole.WIDTH;
        Rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = FakeConsole.HEIGHT;

        if (FakeBehaviors.UseFakeClipboard)
        {
            Clipboard = new FakeClipboard (
                                           FakeBehaviors.FakeClipboardAlwaysThrowsNotSupportedException,
                                           FakeBehaviors.FakeClipboardIsSupportedAlwaysFalse
                                          );
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
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
        }
    }

    public override void End ()
    {
        FakeConsole.ResetColor ();
        FakeConsole.Clear ();
    }

    private FakeMainLoop _mainLoopDriver;

    public override MainLoop Init ()
    {
        FakeConsole.MockKeyPresses.Clear ();

        Cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = FakeConsole.WIDTH;
        Rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = FakeConsole.HEIGHT;
        FakeConsole.Clear ();
        ResizeScreen ();
        CurrentAttribute = new Attribute (Color.White, Color.Black);
        //ClearContents ();

        _mainLoopDriver = new FakeMainLoop (this);
        _mainLoopDriver.MockKeyPressed = MockKeyPressedHandler;

        return new MainLoop (_mainLoopDriver);
    }

    public override bool UpdateScreen ()
    {
        bool updated = false;

        int savedRow = FakeConsole.CursorTop;
        int savedCol = FakeConsole.CursorLeft;
        bool savedCursorVisible = FakeConsole.CursorVisible;

        var top = 0;
        var left = 0;
        int rows = Rows;
        int cols = Cols;
        var output = new StringBuilder ();
        var redrawAttr = new Attribute ();
        int lastCol = -1;

        for (int row = top; row < rows; row++)
        {
            if (!_dirtyLines [row])
            {
                continue;
            }

            updated = true;

            FakeConsole.CursorTop = row;
            FakeConsole.CursorLeft = 0;

            _dirtyLines [row] = false;
            output.Clear ();

            for (int col = left; col < cols; col++)
            {
                lastCol = -1;
                var outputWidth = 0;

                for (; col < cols; col++)
                {
                    if (!Contents [row, col].IsDirty)
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

                    Attribute attr = Contents [row, col].Attribute.Value;

                    // Performance: Only send the escape sequence if the attribute has changed.
                    if (attr != redrawAttr)
                    {
                        redrawAttr = attr;
                        FakeConsole.ForegroundColor = (ConsoleColor)attr.Foreground.GetClosestNamedColor16 ();
                        FakeConsole.BackgroundColor = (ConsoleColor)attr.Background.GetClosestNamedColor16 ();
                    }

                    outputWidth++;
                    Rune rune = Contents [row, col].Rune;
                    output.Append (rune.ToString ());

                    if (rune.IsSurrogatePair () && rune.GetColumns () < 2)
                    {
                        WriteToConsole (output, ref lastCol, row, ref outputWidth);
                        FakeConsole.CursorLeft--;
                    }

                    Contents [row, col].IsDirty = false;
                }
            }

            if (output.Length > 0)
            {
                FakeConsole.CursorTop = row;
                FakeConsole.CursorLeft = lastCol;

                foreach (char c in output.ToString ())
                {
                    FakeConsole.Write (c);
                }
            }
        }

        FakeConsole.CursorTop = 0;
        FakeConsole.CursorLeft = 0;

        //SetCursorVisibility (savedVisibility);

        void WriteToConsole (StringBuilder output, ref int lastCol, int row, ref int outputWidth)
        {
            FakeConsole.CursorTop = row;
            FakeConsole.CursorLeft = lastCol;

            foreach (char c in output.ToString ())
            {
                FakeConsole.Write (c);
            }

            output.Clear ();
            lastCol += outputWidth;
            outputWidth = 0;
        }

        FakeConsole.CursorTop = savedRow;
        FakeConsole.CursorLeft = savedCol;
        FakeConsole.CursorVisible = savedCursorVisible;
        return updated;
    }


    #region Color Handling

    ///// <remarks>
    ///// In the FakeDriver, colors are encoded as an int; same as NetDriver
    ///// However, the foreground color is stored in the most significant 16 bits, 
    ///// and the background color is stored in the least significant 16 bits.
    ///// </remarks>
    //public override Attribute MakeColor (Color foreground, Color background)
    //{
    //	// Encode the colors into the int value.
    //	return new Attribute (
    //		platformColor: 0,//((((int)foreground.ColorName) & 0xffff) << 16) | (((int)background.ColorName) & 0xffff),
    //		foreground: foreground,
    //		background: background
    //	);
    //}

    #endregion

    private KeyCode MapKey (ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.Esc);
            case ConsoleKey.Tab:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.Tab);
            case ConsoleKey.Clear:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.Clear);
            case ConsoleKey.Home:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.Home);
            case ConsoleKey.End:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.End);
            case ConsoleKey.LeftArrow:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.CursorLeft);
            case ConsoleKey.RightArrow:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.CursorRight);
            case ConsoleKey.UpArrow:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.CursorUp);
            case ConsoleKey.DownArrow:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.CursorDown);
            case ConsoleKey.PageUp:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.PageUp);
            case ConsoleKey.PageDown:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.PageDown);
            case ConsoleKey.Enter:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.Enter);
            case ConsoleKey.Spacebar:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (
                                                                keyInfo.Modifiers,
                                                                keyInfo.KeyChar == 0
                                                                    ? KeyCode.Space
                                                                    : (KeyCode)keyInfo.KeyChar
                                                               );
            case ConsoleKey.Backspace:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.Backspace);
            case ConsoleKey.Delete:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.Delete);
            case ConsoleKey.Insert:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.Insert);
            case ConsoleKey.PrintScreen:
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, KeyCode.PrintScreen);

            case ConsoleKey.Oem1:
            case ConsoleKey.Oem2:
            case ConsoleKey.Oem3:
            case ConsoleKey.Oem4:
            case ConsoleKey.Oem5:
            case ConsoleKey.Oem6:
            case ConsoleKey.Oem7:
            case ConsoleKey.Oem8:
            case ConsoleKey.Oem102:
            case ConsoleKey.OemPeriod:
            case ConsoleKey.OemComma:
            case ConsoleKey.OemPlus:
            case ConsoleKey.OemMinus:
                if (keyInfo.KeyChar == 0)
                {
                    return KeyCode.Null;
                }

                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
        }

        ConsoleKey key = keyInfo.Key;

        if (key >= ConsoleKey.A && key <= ConsoleKey.Z)
        {
            int delta = key - ConsoleKey.A;

            if (keyInfo.KeyChar != (uint)key)
            {
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.Key);
            }

            if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control)
                || keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt)
                || keyInfo.Modifiers.HasFlag (ConsoleModifiers.Shift))
            {
                return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)((uint)KeyCode.A + delta));
            }

            char alphaBase = keyInfo.Modifiers != ConsoleModifiers.Shift ? 'A' : 'a';

            return (KeyCode)((uint)alphaBase + delta);
        }

        return ConsoleKeyMapping.MapToKeyCodeModifiers (keyInfo.Modifiers, (KeyCode)keyInfo.KeyChar);
    }

    private CursorVisibility _savedCursorVisibility;

    private void MockKeyPressedHandler (ConsoleKeyInfo consoleKeyInfo)
    {
        if (consoleKeyInfo.Key == ConsoleKey.Packet)
        {
            consoleKeyInfo = ConsoleKeyMapping.DecodeVKPacketToKConsoleKeyInfo (consoleKeyInfo);
        }

        KeyCode map = MapKey (consoleKeyInfo);
        OnKeyDown (new Key (map));
        OnKeyUp (new Key (map));

        //OnKeyPressed (new KeyEventArgs (map));
    }

    /// <inheritdoc/>
    public override bool GetCursorVisibility (out CursorVisibility visibility)
    {
        visibility = FakeConsole.CursorVisible
                         ? CursorVisibility.Default
                         : CursorVisibility.Invisible;

        return FakeConsole.CursorVisible;
    }

    /// <inheritdoc/>
    public override bool SetCursorVisibility (CursorVisibility visibility)
    {
        _savedCursorVisibility = visibility;

        return FakeConsole.CursorVisible = visibility == CursorVisibility.Default;
    }

    /// <inheritdoc/>
    private bool EnsureCursorVisibility ()
    {
        if (!(Col >= 0 && Row >= 0 && Col < Cols && Row < Rows))
        {
            GetCursorVisibility (out CursorVisibility cursorVisibility);
            _savedCursorVisibility = cursorVisibility;
            SetCursorVisibility (CursorVisibility.Invisible);

            return false;
        }

        SetCursorVisibility (_savedCursorVisibility);

        return FakeConsole.CursorVisible;
    }

    public override void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
    {
        MockKeyPressedHandler (new ConsoleKeyInfo (keyChar, key, shift, alt, control));
    }

    private AnsiResponseParser _parser = new ();

    /// <inheritdoc />
    internal override IAnsiResponseParser GetParser () => _parser;

    public void SetBufferSize (int width, int height)
    {
        FakeConsole.SetBufferSize (width, height);
        Cols = width;
        Rows = height;
        SetWindowSize (width, height);
        ProcessResize ();
    }

    public void SetWindowSize (int width, int height)
    {
        FakeConsole.SetWindowSize (width, height);

        if (width != Cols || height != Rows)
        {
            SetBufferSize (width, height);
            Cols = width;
            Rows = height;
        }

        ProcessResize ();
    }

    public void SetWindowPosition (int left, int top)
    {
        if (Left > 0 || Top > 0)
        {
            Left = 0;
            Top = 0;
        }

        FakeConsole.SetWindowPosition (Left, Top);
    }

    private void ProcessResize ()
    {
        ResizeScreen ();
        ClearContents ();
        OnSizeChanged (new SizeChangedEventArgs (new (Cols, Rows)));
    }

    public virtual void ResizeScreen ()
    {
        if (FakeConsole.WindowHeight > 0)
        {
            // Can raise an exception while it is still resizing.
            try
            {
                FakeConsole.CursorTop = 0;
                FakeConsole.CursorLeft = 0;
                FakeConsole.WindowTop = 0;
                FakeConsole.WindowLeft = 0;
            }
            catch (IOException)
            {
                return;
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }
        }

        // CONCURRENCY: Unsynchronized access to Clip is not safe.
        Clip = new (Screen);
    }

    public override void UpdateCursor ()
    {
        if (!EnsureCursorVisibility ())
        {
            return;
        }

        // Prevents the exception of size changing during resizing.
        try
        {
            // BUGBUG: Why is this using BufferWidth/Height and now Cols/Rows?
            if (Col >= 0 && Col < FakeConsole.BufferWidth && Row >= 0 && Row < FakeConsole.BufferHeight)
            {
                FakeConsole.SetCursorPosition (Col, Row);
            }
        }
        catch (IOException)
        { }
        catch (ArgumentOutOfRangeException)
        { }
    }

    #region Not Implemented

    public override void Suspend ()
    {
        //throw new NotImplementedException ();
    }

    #endregion

    public class FakeClipboard : ClipboardBase
    {
        public Exception FakeException;

        private readonly bool _isSupportedAlwaysFalse;
        private string _contents = string.Empty;

        public FakeClipboard (
            bool fakeClipboardThrowsNotSupportedException = false,
            bool isSupportedAlwaysFalse = false
        )
        {
            _isSupportedAlwaysFalse = isSupportedAlwaysFalse;

            if (fakeClipboardThrowsNotSupportedException)
            {
                FakeException = new NotSupportedException ("Fake clipboard exception");
            }
        }

        public override bool IsSupported => !_isSupportedAlwaysFalse;

        protected override string GetClipboardDataImpl ()
        {
            if (FakeException is { })
            {
                throw FakeException;
            }

            return _contents;
        }

        protected override void SetClipboardDataImpl (string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException (nameof (text));
            }

            if (FakeException is { })
            {
                throw FakeException;
            }

            _contents = text;
        }
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
