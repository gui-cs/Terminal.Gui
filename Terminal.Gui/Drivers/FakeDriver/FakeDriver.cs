#nullable enable

//
// FakeDriver.cs: A fake IConsoleDriver for unit tests. 
//

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

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
            Debug.Assert (!useFakeClipboard && !fakeClipboardAlwaysThrowsNotSupportedException);
            Debug.Assert (!useFakeClipboard && !fakeClipboardIsSupportedAlwaysTrue);
        }

        public bool FakeClipboardAlwaysThrowsNotSupportedException { get; internal set; }
        public bool FakeClipboardIsSupportedAlwaysFalse { get; internal set; }
        public bool UseFakeClipboard { get; internal set; }
    }

    public static Behaviors FakeBehaviors { get; } = new ();
    public override bool SupportsTrueColor => false;

    /// <inheritdoc/>
    public override void WriteRaw (string ansi) { }

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
        ClearContents ();
        CurrentAttribute = new (Color.White, Color.Black);
    }

    public override bool UpdateScreen ()
    {
        var updated = false;

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

    private readonly AnsiResponseParser _parser = new ();

    /// <inheritdoc/>
    internal override IAnsiResponseParser GetParser () { return _parser; }

    /// <summary>
    ///     Sets the screen size for testing purposes. Only available in FakeDriver.
    ///     This method updates the driver's dimensions and triggers the ScreenChanged event.
    /// </summary>
    /// <param name="width">The new width in columns.</param>
    /// <param name="height">The new height in rows.</param>
    public override void SetScreenSize (int width, int height) { SetBufferSize (width, height); }

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
        OnSizeChanged (new (new (Cols, Rows)));
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

        // Prevents the exception to size changing during resizing.
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


#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}