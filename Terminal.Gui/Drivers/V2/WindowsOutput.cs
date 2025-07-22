#nullable enable
using System.Buffers;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using static Terminal.Gui.Drivers.WindowsConsole;

namespace Terminal.Gui.Drivers;

internal partial class WindowsOutput : IConsoleOutput
{
    [LibraryImport ("kernel32.dll", EntryPoint = "WriteConsoleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool WriteConsole (
        nint hConsoleOutput,
        ReadOnlySpan<char> lpbufer,
        uint numberOfCharsToWriten,
        out uint lpNumberOfCharsWritten,
        nint lpReserved
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle (nint handle);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint CreateConsoleScreenBuffer (
        DesiredAccess dwDesiredAccess,
        ShareMode dwShareMode,
        nint secutiryAttributes,
        uint flags,
        nint screenBufferData
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleScreenBufferInfoEx (nint hConsoleOutput, ref WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX csbi);

    [Flags]
    private enum ShareMode : uint
    {
        FileShareRead = 1,
        FileShareWrite = 2
    }

    [Flags]
    private enum DesiredAccess : uint
    {
        GenericRead = 2147483648,
        GenericWrite = 1073741824
    }

    internal static nint INVALID_HANDLE_VALUE = new (-1);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleActiveScreenBuffer (nint handle);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleCursorPosition (nint hConsoleOutput, WindowsConsole.Coord dwCursorPosition);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCursorInfo (nint hConsoleOutput, [In] ref WindowsConsole.ConsoleCursorInfo lpConsoleCursorInfo);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleTextAttribute (
        nint hConsoleOutput,
        ushort wAttributes
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern Coord GetLargestConsoleWindowSize (
        nint hConsoleOutput
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleScreenBufferInfoEx (nint hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX consoleScreenBufferInfo);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleWindowInfo (
        nint hConsoleOutput,
        bool bAbsolute,
        [In] ref SmallRect lpConsoleWindow
    );

    private nint _screenBuffer;
    private nint _outputHandle;

    // Last text style used, for updating style with EscSeqUtils.CSI_AppendTextStyleChange().
    private TextStyle _redrawTextStyle = TextStyle.None;

    public WindowsOutput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (WindowsOutput)}");

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        _outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);

        if (!GetConsoleMode (_outputHandle, out uint mode))
        {
            throw new ApplicationException ($"Failed to get _outputHandle console mode, error code: {Marshal.GetLastWin32Error ()}.");
        }

        IsVirtualTerminal = (mode & (uint)ConsoleModes.EnableVirtualTerminalProcessing) != 0;

        if (!IsVirtualTerminal)
        {
            _screenBuffer = CreateConsoleScreenBuffer (
                                                       DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
                                                       ShareMode.FileShareRead | ShareMode.FileShareWrite,
                                                       nint.Zero,
                                                       1,
                                                       nint.Zero
                                                      );

            if (_screenBuffer == INVALID_HANDLE_VALUE)
            {
                int err = Marshal.GetLastWin32Error ();

                if (err != 0)
                {
                    throw new Win32Exception (err);
                }
            }

            if (!SetConsoleActiveScreenBuffer (_screenBuffer))
            {
                throw new Win32Exception (Marshal.GetLastWin32Error ());
            }

            if (!GetConsoleMode (_screenBuffer, out mode))
            {
                throw new ApplicationException ($"Failed to get screenBuffer console mode, error code: {Marshal.GetLastWin32Error ()}.");
            }

            const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;

            mode &= ~ENABLE_WRAP_AT_EOL_OUTPUT; // Disable wrap

            if (!SetConsoleMode (_screenBuffer, mode))
            {
                throw new ApplicationException ($"Failed to set screenBuffer console mode, error code: {Marshal.GetLastWin32Error ()}.");
            }
        }
    }

    internal bool IsVirtualTerminal { get; init; }

    public void Write (ReadOnlySpan<char> str)
    {
        if (!WriteConsole (IsVirtualTerminal ? _outputHandle : _screenBuffer, str, (uint)str.Length, out uint _, nint.Zero))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error (), "Failed to write to console screen buffer.");
        }
    }

    public void Write (IOutputBuffer buffer)
    {
        WindowsConsole.ExtendedCharInfo [] outputBuffer = new WindowsConsole.ExtendedCharInfo [buffer.Rows * buffer.Cols];

        // TODO: probably do need this right?
        /*
        if (!windowSize.IsEmpty && (windowSize.Width != buffer.Cols || windowSize.Height != buffer.Rows))
        {
            return;
        }*/

        var bufferCoords = new WindowsConsole.Coord
        {
            X = (short)buffer.Cols, //Clip.Width,
            Y = (short)buffer.Rows //Clip.Height
        };

        for (var row = 0; row < buffer.Rows; row++)
        {
            if (!buffer.DirtyLines [row])
            {
                continue;
            }

            buffer.DirtyLines [row] = false;

            for (var col = 0; col < buffer.Cols; col++)
            {
                int position = row * buffer.Cols + col;
                outputBuffer [position].Attribute = buffer.Contents [row, col].Attribute.GetValueOrDefault ();

                if (buffer.Contents [row, col].IsDirty == false)
                {
                    outputBuffer [position].Empty = true;
                    outputBuffer [position].Char = [(char)buffer.Contents [row, col].Rune.Value];

                    continue;
                }

                outputBuffer [position].Empty = false;

                if (buffer.Contents [row, col].Rune.IsBmp)
                {
                    outputBuffer [position].Char = [(char)buffer.Contents [row, col].Rune.Value];
                }
                else
                {
                    outputBuffer [position].Char = [(char)buffer.Contents [row, col].Rune.ToString () [0],
                                                       (char)buffer.Contents [row, col].Rune.ToString () [1]];

                    if (buffer.Contents [row, col].Rune.GetColumns () > 1 && col + 1 < buffer.Cols)
                    {
                        // TODO: This is a hack to deal with non-BMP and wide characters.
                        col++;
                        position = row * buffer.Cols + col;
                        outputBuffer [position].Empty = false;
                        outputBuffer [position].Char = ['\0'];
                    }
                }
            }
        }

        var damageRegion = new WindowsConsole.SmallRect
        {
            Top = 0,
            Left = 0,
            Bottom = (short)buffer.Rows,
            Right = (short)buffer.Cols
        };

        //size, ExtendedCharInfo [] charInfoBuffer, Coord , SmallRect window,
        if (!ConsoleDriver.RunningUnitTests
            && !WriteToConsole (
                                new (buffer.Cols, buffer.Rows),
                                outputBuffer,
                                bufferCoords,
                                damageRegion,
                                Application.Driver!.Force16Colors))
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        WindowsConsole.SmallRect.MakeEmpty (ref damageRegion);
    }

    private struct Run
    {
        public ushort attr;
        public string text;

        public Run (ushort attr, string text)
        {
            this.attr = attr;
            this.text = text;
        }
    }

    public bool WriteToConsole (Size size, WindowsConsole.ExtendedCharInfo [] charInfoBuffer, WindowsConsole.Coord bufferSize, WindowsConsole.SmallRect window, bool force16Colors)
    {

        //Debug.WriteLine ("WriteToConsole");

        //if (_screenBuffer == nint.Zero)
        //{
        //    ReadFromConsoleOutput (size, bufferSize, ref window);
        //}

        Attribute? prev = null;
        var result = false;

        if (force16Colors)
        {
            StringBuilder stringBuilder = new ();
            var i = 0;
            List<Run> runs = [];
            Run? current = null;
            SetCursorPosition (0, 0);

            foreach (ExtendedCharInfo info in charInfoBuffer)
            {
                if (IsVirtualTerminal)
                {
                    Attribute attr = info.Attribute;
                    AnsiColorCode fgColor = info.Attribute.Foreground.GetAnsiColorCode ();
                    AnsiColorCode bgColor = info.Attribute.Background.GetAnsiColorCode ();

                    if (attr != prev)
                    {
                        prev = attr;
                        stringBuilder.Append (EscSeqUtils.CSI_SetForegroundColor (fgColor));
                        stringBuilder.Append (EscSeqUtils.CSI_SetBackgroundColor (bgColor));

                        EscSeqUtils.CSI_AppendTextStyleChange (stringBuilder, _redrawTextStyle, attr.Style);
                        _redrawTextStyle = attr.Style;
                    }

                    if (info.Char [0] != '\x1b')
                    {
                        if (!info.Empty)
                        {
                            stringBuilder.Append (info.Char);
                        }
                    }
                    else
                    {
                        stringBuilder.Append (' ');
                    }
                }
                else
                {
                    if (info.Empty)
                    {
                        i++;
                        continue;
                    }

                    if (!info.Empty)
                    {
                        var attr = (ushort)((int)info.Attribute.Foreground.GetClosestNamedColor16 ()
                                            | ((int)info.Attribute.Background.GetClosestNamedColor16 () << 4));

                        // Start new run if needed
                        if (current == null || attr != current.Value.attr)
                        {
                            if (current != null)
                            {
                                runs.Add (new (current.Value.attr, stringBuilder.ToString ()));
                            }

                            stringBuilder.Clear ();
                            current = new Run (attr, "");
                        }

                        stringBuilder!.Append (info.Char);
                    }

                    i++;

                    if (i > 0 && i <= charInfoBuffer.Length && i % bufferSize.X == 0)
                    {
                        if (i < charInfoBuffer.Length)
                        {
                            stringBuilder.AppendLine ();
                        }

                        runs.Add (new (current!.Value.attr, stringBuilder.ToString ()));
                        stringBuilder.Clear ();
                    }
                }
            }

            if (IsVirtualTerminal)
            {
                stringBuilder.Append (EscSeqUtils.CSI_RestoreCursorPosition);
                stringBuilder.Append (EscSeqUtils.CSI_HideCursor);

                // TODO: Potentially could stackalloc whenever reasonably small (<= 8 kB?) write buffer is needed.
                char [] rentedWriteArray = ArrayPool<char>.Shared.Rent (minimumLength: stringBuilder.Length);
                try
                {
                    Span<char> writeBuffer = rentedWriteArray.AsSpan (0, stringBuilder.Length);
                    stringBuilder.CopyTo (0, writeBuffer, stringBuilder.Length);

                    // Supply console with the new content.
                    result = WriteConsole (_outputHandle, writeBuffer, (uint)writeBuffer.Length, out uint _, nint.Zero);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return (rentedWriteArray);
                }

                foreach (SixelToRender sixel in Application.Sixel)
                {
                    SetCursorPosition ((short)sixel.ScreenPosition.X, (short)sixel.ScreenPosition.Y);
                    WriteConsole (_outputHandle, sixel.SixelData, (uint)sixel.SixelData.Length, out uint _, nint.Zero);
                }
            }
            else
            {
                foreach (var run in runs)
                {
                    SetConsoleTextAttribute (_screenBuffer, run.attr);
                    result = WriteConsole (_screenBuffer, run.text, (uint)run.text.Length, out _, nint.Zero);
                }
            }
        }
        else
        {
            StringBuilder stringBuilder = new();

            stringBuilder.Append (EscSeqUtils.CSI_SaveCursorPosition);
            EscSeqUtils.CSI_AppendCursorPosition (stringBuilder, 0, 0);

            foreach (WindowsConsole.ExtendedCharInfo info in charInfoBuffer)
            {
                Attribute attr = info.Attribute;

                if (attr != prev)
                {
                    prev = attr;
                    EscSeqUtils.CSI_AppendForegroundColorRGB (stringBuilder, attr.Foreground.R, attr.Foreground.G, attr.Foreground.B);
                    EscSeqUtils.CSI_AppendBackgroundColorRGB (stringBuilder, attr.Background.R, attr.Background.G, attr.Background.B);
                    EscSeqUtils.CSI_AppendTextStyleChange (stringBuilder, _redrawTextStyle, attr.Style);
                    _redrawTextStyle = attr.Style;
                }

                if (info.Char [0] != '\x1b')
                {
                    if (!info.Empty)
                    {
                        stringBuilder.Append (info.Char);
                    }
                }
                else
                {
                    stringBuilder.Append (' ');
                }
            }

            stringBuilder.Append (EscSeqUtils.CSI_RestoreCursorPosition);
            stringBuilder.Append (EscSeqUtils.CSI_HideCursor);

            // TODO: Potentially could stackalloc whenever reasonably small (<= 8 kB?) write buffer is needed.
            char [] rentedWriteArray = ArrayPool<char>.Shared.Rent (minimumLength: stringBuilder.Length);
            try
            {
                Span<char> writeBuffer = rentedWriteArray.AsSpan(0, stringBuilder.Length);
                stringBuilder.CopyTo (0, writeBuffer, stringBuilder.Length);

                // Supply console with the new content.
                result = WriteConsole (IsVirtualTerminal ? _outputHandle : _screenBuffer, writeBuffer, (uint)writeBuffer.Length, out uint _, nint.Zero);
            }
            finally
            {
                ArrayPool<char>.Shared.Return (rentedWriteArray);
            }

            foreach (SixelToRender sixel in Application.Sixel)
            {
                SetCursorPosition ((short)sixel.ScreenPosition.X, (short)sixel.ScreenPosition.Y);
                WriteConsole (IsVirtualTerminal ? _outputHandle : _screenBuffer, sixel.SixelData, (uint)sixel.SixelData.Length, out uint _, nint.Zero);
            }
        }

        if (!result)
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        return result;
    }

    public Size GetWindowSize (Size? lastSize = null)
    {
        var csbi = new WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (IsVirtualTerminal ? _outputHandle : _screenBuffer, ref csbi))
        {
            //throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
            return Size.Empty;
        }

        Size sz = new (
                       csbi.srWindow.Right - csbi.srWindow.Left + 1,
                       csbi.srWindow.Bottom - csbi.srWindow.Top + 1);

        if (lastSize is { } && sz != lastSize)
        {
            Size newSize = SetConsoleWindow ((short)sz.Width, (short)sz.Height);

            if (sz != newSize)
            {
                return newSize;
            }
        }

        return sz;
    }

    private Size SetConsoleWindow (short cols, short rows)
    {
        var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (IsVirtualTerminal ? _outputHandle : _screenBuffer, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }

        Coord maxWinSize = GetLargestConsoleWindowSize (IsVirtualTerminal ? _outputHandle : _screenBuffer);
        short newCols = Math.Min (cols, maxWinSize.X);
        short newRows = Math.Min (rows, maxWinSize.Y);
        csbi.dwSize = new Coord (newCols, Math.Max (newRows, (short)1));
        csbi.srWindow = new SmallRect (0, 0, newCols, newRows);
        csbi.dwMaximumWindowSize = new Coord (newCols, newRows);

        if (!SetConsoleScreenBufferInfoEx (IsVirtualTerminal ? _outputHandle : _screenBuffer, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }

        var winRect = new SmallRect (0, 0, (short)(newCols - 1), (short)Math.Max (newRows - 1, 0));

        if (!SetConsoleWindowInfo (_outputHandle, true, ref winRect))
        {
            //throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
            return new (cols, rows);
        }

        SetConsoleOutputWindow (csbi);

        return new (winRect.Right + 1, newRows - 1 < 0 ? 0 : winRect.Bottom + 1);
    }

    private void SetConsoleOutputWindow (CONSOLE_SCREEN_BUFFER_INFOEX csbi)
    {
        if ((IsVirtualTerminal
                 ? _outputHandle
                 : _screenBuffer) != nint.Zero && !SetConsoleScreenBufferInfoEx (IsVirtualTerminal ? _outputHandle : _screenBuffer, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }
    }

    /// <inheritdoc/>
    public void SetCursorVisibility (CursorVisibility visibility)
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        if (Application.Driver!.Force16Colors)
        {
            var info = new WindowsConsole.ConsoleCursorInfo
            {
                dwSize = (uint)visibility & 0x00FF,
                bVisible = ((uint)visibility & 0xFF00) != 0
            };

            SetConsoleCursorInfo (_screenBuffer, ref info);
        }
        else
        {
            string cursorVisibilitySequence = visibility != CursorVisibility.Invisible
                                                  ? EscSeqUtils.CSI_ShowCursor
                                                  : EscSeqUtils.CSI_HideCursor;
            Write (cursorVisibilitySequence);
        }
    }

    private Point _lastCursorPosition;

    /// <inheritdoc/>
    public void SetCursorPosition (int col, int row)
    {
        if (_lastCursorPosition.X == col && _lastCursorPosition.Y == row)
        {
            return;
        }

        _lastCursorPosition = new (col, row);

        SetConsoleCursorPosition (_screenBuffer, new ((short)col, (short)row));
    }

    private bool _isDisposed;

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_screenBuffer != nint.Zero)
        {
            try
            {
                CloseHandle (_screenBuffer);
            }
            catch (Exception e)
            {
                Logging.Logger.LogError (e, "Error trying to close screen buffer handle in WindowsOutput via interop method");
            }
        }

        _isDisposed = true;
    }
}
