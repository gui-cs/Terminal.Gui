#nullable enable
//
// FakeDriver.cs: A fake IConsoleDriver for unit tests. 
//

using System.Diagnostics;
using System.Runtime.InteropServices;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.Drivers;

/// <summary>
///     Implements a mock <see cref="IConsoleDriver"/> for unit testing. This driver simulates console behavior
///     without requiring a real terminal, allowing for deterministic testing of Terminal.Gui applications.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="FakeDriver"/> extends the legacy <see cref="ConsoleDriver"/> base class and is designed
///         for backward compatibility with existing tests. It provides programmatic control over console state,
///         including screen size, keyboard input, and output verification.
///     </para>
///     <para>
///         <strong>Key Features:</strong>
///     </para>
///     <list type="bullet">
///         <item>Programmatic screen resizing via <see cref="SetBufferSize"/> and <see cref="SetWindowSize"/></item>
///         <item>Keyboard input simulation via <see cref="FakeConsole.PushMockKeyPress"/></item>
///         <item>Mouse input simulation via <see cref="FakeConsole"/> methods</item>
///         <item>Output verification via <see cref="ConsoleDriver.Contents"/> buffer inspection</item>
///         <item>Event firing for resize, keyboard, and mouse events</item>
///     </list>
///     <para>
///         <strong>Usage:</strong> Most tests should use <see cref="AutoInitShutdownAttribute"/> which automatically
///         initializes Application with FakeDriver. For more control, create and configure FakeDriver instances directly.
///     </para>
///     <para>
///         <strong>Thread Safety:</strong> FakeDriver is not thread-safe. Tests using this driver should not run
///         in parallel with other tests that access driver state.
///     </para>
///     <para>
///         For detailed usage examples and patterns, see the README.md file in this directory.
///     </para>
/// </remarks>
/// <seealso cref="AutoInitShutdownAttribute"/>
/// <seealso cref="FakeConsole"/>
/// <seealso cref="FakeComponentFactory"/>
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

    public static Behaviors FakeBehaviors { get; } = new ();
    public override bool SupportsTrueColor => false;

    /// <inheritdoc />
    public override void WriteRaw (string ansi)
    {

    }

    public FakeDriver ()
    {
        // FakeDriver implies UnitTests
        RunningUnitTests = true;

        base.Cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = FakeConsole.WIDTH;
        base.Rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = FakeConsole.HEIGHT;

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
                if (PlatformDetection.IsWSLPlatform ())
                {
                    Clipboard = new WSLClipboard ();
                }
                else
                {
                    Clipboard = new UnixClipboard ();
                }
            }
        }
    }

    public override void End ()
    {
        FakeConsole.ResetColor ();
        FakeConsole.Clear ();
    }

    public override void Init ()
    {
        FakeConsole.MockKeyPresses.Clear ();

        Cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = FakeConsole.WIDTH;
        Rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = FakeConsole.HEIGHT;
        FakeConsole.Clear ();
        ResizeScreen ();
        CurrentAttribute = new Attribute (Color.White, Color.Black);
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
            if (!_dirtyLines! [row])
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
                    if (!Contents! [row, col].IsDirty)
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

                    Attribute attr = Contents [row, col].Attribute!.Value;

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

        void WriteToConsole (StringBuilder outputSb, ref int lastColumn, int row, ref int outputWidth)
        {
            FakeConsole.CursorTop = row;
            FakeConsole.CursorLeft = lastColumn;

            foreach (char c in outputSb.ToString ())
            {
                FakeConsole.Write (c);
            }

            outputSb.Clear ();
            lastColumn += outputWidth;
            outputWidth = 0;
        }

        FakeConsole.CursorTop = savedRow;
        FakeConsole.CursorLeft = savedCol;
        FakeConsole.CursorVisible = savedCursorVisible;
        return updated;
    }


    #region Color Handling

    ///// <remarks>
    ///// In the FakeDriver, colors are encoded as an int; same as DotNetDriver
    ///// However, the foreground color is stored in the most significant 16 bits, 
    ///// and the background color is stored in the least significant 16 bits.
    ///// </remarks>
    //public override Attribute MakeColor (Color foreground, Color background)
    //{
    //	// Encode the colors into the int value.
    //	return new Attribute (
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

    private AnsiResponseParser _parser = new ();

    /// <inheritdoc />
    internal override IAnsiResponseParser GetParser () => _parser;

    /// <summary>
    ///     Sets the size of the fake console screen/buffer for testing purposes. This method updates
    ///     the driver's dimensions (<see cref="ConsoleDriver.Cols"/> and <see cref="ConsoleDriver.Rows"/>),
    ///     clears the contents, and fires the <see cref="ConsoleDriver.SizeChanged"/> event.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is intended for use in unit tests to simulate terminal resize events.
    ///         For FakeDriver, the buffer size and window size are always the same (there is no scrollback).
    ///     </para>
    ///     <para>
    ///         When called, this method:
    ///         <list type="number">
    ///             <item>Updates the <see cref="FakeConsole"/> buffer size</item>
    ///             <item>Sets <see cref="ConsoleDriver.Cols"/> and <see cref="ConsoleDriver.Rows"/> to the new dimensions</item>
    ///             <item>Updates the window size to match</item>
    ///             <item>Clears the screen contents</item>
    ///             <item>Fires the <see cref="ConsoleDriver.SizeChanged"/> event</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong> This method is not thread-safe. Tests using this method
    ///         should ensure they are not accessing the driver concurrently.
    ///     </para>
    ///     <para>
    ///         <strong>Relationship to Screen property:</strong> After calling this method, 
    ///         <see cref="ConsoleDriver.Screen"/> will return a rectangle with origin (0,0) and size (width, height).
    ///     </para>
    /// </remarks>
    /// <param name="width">The new width in columns.</param>
    /// <param name="height">The new height in rows.</param>
    /// <example>
    ///     <code>
    ///     // Simulate a terminal resize to 120x30
    ///     var driver = new FakeDriver();
    ///     driver.SetBufferSize(120, 30);
    ///     Assert.Equal(120, driver.Cols);
    ///     Assert.Equal(30, driver.Rows);
    ///     </code>
    /// </example>
    public void SetBufferSize (int width, int height)
    {
        FakeConsole.SetBufferSize (width, height);
        Cols = width;
        Rows = height;
        SetWindowSize (width, height);
        ProcessResize ();
    }

    /// <summary>
    ///     Sets the window size of the fake console. For FakeDriver, this is functionally equivalent to
    ///     <see cref="SetBufferSize"/> as the fake console does not support scrollback (window size == buffer size).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method exists for API compatibility with real console drivers, but in FakeDriver,
    ///         the window size and buffer size are always kept in sync. Calling this method will update
    ///         both the window and buffer to the specified size.
    ///     </para>
    ///     <para>
    ///         Prefer using <see cref="SetBufferSize"/> for clarity in test code, as it more accurately
    ///         describes what's happening (setting the entire screen size for the fake driver).
    ///     </para>
    /// </remarks>
    /// <param name="width">The new width in columns.</param>
    /// <param name="height">The new height in rows.</param>
    /// <seealso cref="SetBufferSize"/>
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
        public Exception? FakeException { get; set; }

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

        protected override void SetClipboardDataImpl (string? text)
        {
            if (FakeException is { })
            {
                throw FakeException;
            }

            _contents = text ?? throw new ArgumentNullException (nameof (text));
        }
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
