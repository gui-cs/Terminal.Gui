#nullable enable
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

internal partial class WindowsOutput : OutputBase, IConsoleOutput
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

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    private static partial nint GetStdHandle (int nStdHandle);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool CloseHandle (nint handle);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    private static partial nint CreateConsoleScreenBuffer (
        DesiredAccess dwDesiredAccess,
        ShareMode dwShareMode,
        nint secutiryAttributes,
        uint flags,
        nint screenBufferData
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
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

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool SetConsoleActiveScreenBuffer (nint handle);

    [LibraryImport ("kernel32.dll")]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool SetConsoleCursorPosition (nint hConsoleOutput, WindowsConsole.Coord dwCursorPosition);

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool SetConsoleCursorInfo (nint hConsoleOutput, [In] ref WindowsConsole.ConsoleCursorInfo lpConsoleCursorInfo);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    public static partial bool SetConsoleTextAttribute (nint hConsoleOutput, ushort wAttributes);

    [LibraryImport ("kernel32.dll")]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [LibraryImport ("kernel32.dll")]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static partial bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    [LibraryImport ("kernel32.dll", SetLastError = true)]
    private static partial WindowsConsole.Coord GetLargestConsoleWindowSize (
        nint hConsoleOutput
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool SetConsoleScreenBufferInfoEx (nint hConsoleOutput, ref WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX consoleScreenBufferInfo);

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool SetConsoleWindowInfo (
        nint hConsoleOutput,
        bool bAbsolute,
        [In] ref WindowsConsole.SmallRect lpConsoleWindow
    );

    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private readonly nint _outputHandle;
    private nint _screenBuffer;
    private readonly bool _isVirtualTerminal;

    public WindowsOutput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (WindowsOutput)}");

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        // Get the standard output handle which is the current screen buffer.
        _outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);
        GetConsoleMode (_outputHandle, out uint mode);
        _isVirtualTerminal = (mode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) != 0;

        if (_isVirtualTerminal)
        {
            //Enable alternative screen buffer.
            Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
        }
        else
        {
            CreateScreenBuffer ();

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

            // Force 16 colors if not in virtual terminal mode.
            Application.Force16Colors = true;
        }
    }

    private void CreateScreenBuffer ()
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
    }

    public void Write (ReadOnlySpan<char> str)
    {
        if (!WriteConsole (_isVirtualTerminal ? _outputHandle : _screenBuffer, str, (uint)str.Length, out uint _, nint.Zero))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error (), "Failed to write to console screen buffer.");
        }
    }

    public Size ResizeBuffer (Size size)
    {
        Size newSize = SetConsoleWindow (
                                 (short)Math.Max (size.Width, 0),
                                 (short)Math.Max (size.Height, 0));

        return newSize;
    }

    internal Size SetConsoleWindow (short cols, short rows)
    {
        var csbi = new WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (_isVirtualTerminal ? _outputHandle : _screenBuffer, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }

        WindowsConsole.Coord maxWinSize = GetLargestConsoleWindowSize (_isVirtualTerminal ? _outputHandle : _screenBuffer);
        short newCols = Math.Min (cols, maxWinSize.X);
        short newRows = Math.Min (rows, maxWinSize.Y);
        csbi.dwSize = new (newCols, Math.Max (newRows, (short)1));
        csbi.srWindow = new (0, 0, newCols, newRows);
        csbi.dwMaximumWindowSize = new (newCols, newRows);

        if (!SetConsoleScreenBufferInfoEx (_isVirtualTerminal ? _outputHandle : _screenBuffer, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }

        var winRect = new WindowsConsole.SmallRect (0, 0, (short)(newCols - 1), (short)Math.Max (newRows - 1, 0));

        if (!SetConsoleWindowInfo (_outputHandle, true, ref winRect))
        {
            //throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
            return new (cols, rows);
        }

        SetConsoleOutputWindow (csbi);

        return new (winRect.Right + 1, newRows - 1 < 0 ? 0 : winRect.Bottom + 1);
    }

    private void SetConsoleOutputWindow (WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX csbi)
    {
        if ((_isVirtualTerminal
                 ? _outputHandle
                 : _screenBuffer) != nint.Zero && !SetConsoleScreenBufferInfoEx (_isVirtualTerminal ? _outputHandle : _screenBuffer, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }
    }

    public override void Write (IOutputBuffer outputBuffer)
    {
        _force16Colors = Application.Driver!.Force16Colors;
        _everythingStringBuilder = new StringBuilder ();

        // for 16 color mode we will write to a backing buffer then flip it to the active one at the end to avoid jitter.
        _consoleBuffer = 0;
        if (_force16Colors)
        {
            if (_isVirtualTerminal)
            {
                _consoleBuffer = _outputHandle;
            }
            else
            {
                _consoleBuffer = _screenBuffer;
            }
        }
        else
        {
            _consoleBuffer = _outputHandle;
        }

        base.Write (outputBuffer);

        try
        {
            if (_force16Colors && !_isVirtualTerminal)
            {
                SetConsoleActiveScreenBuffer (_consoleBuffer);
            }
            else
            {
                var span = _everythingStringBuilder.ToString ().AsSpan (); // still allocates the string

                var result = WriteConsole (_consoleBuffer, span, (uint)span.Length, out _, nint.Zero);
                if (!result)
                {
                    int err = Marshal.GetLastWin32Error ();

                    if (err != 0)
                    {
                        throw new Win32Exception (err);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Logging.Logger.LogError ($"Error: {e.Message} in {nameof (WindowsOutput)}");

            if (!ConsoleDriver.RunningUnitTests)
            {
                throw;
            }
        }
    }
    /// <inheritdoc />
    protected override void Write (StringBuilder output)
    {
        if (output.Length == 0)
        {
            return;
        }

        var str = output.ToString ();

        if (_force16Colors && !_isVirtualTerminal)
        {
            var a = str.ToCharArray ();
            WriteConsole (_screenBuffer,a ,(uint)a.Length, out _, nint.Zero);
        }
        else
        {
            _everythingStringBuilder.Append (str);
        }
    }

    /// <inheritdoc />
    protected override void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
    {
        var force16Colors = Application.Force16Colors;

        if (force16Colors)
        {
            if (_isVirtualTerminal)
            {
                output.Append (EscSeqUtils.CSI_SetForegroundColor (attr.Foreground.GetAnsiColorCode ()));
                output.Append (EscSeqUtils.CSI_SetBackgroundColor (attr.Background.GetAnsiColorCode ()));
                EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
            }
            else
            {
                var as16ColorInt = (ushort)((int)attr.Foreground.GetClosestNamedColor16 () | ((int)attr.Background.GetClosestNamedColor16 () << 4));
                SetConsoleTextAttribute (_screenBuffer, as16ColorInt);
            }
        }
        else
        {
            EscSeqUtils.CSI_AppendForegroundColorRGB (output, attr.Foreground.R, attr.Foreground.G, attr.Foreground.B);
            EscSeqUtils.CSI_AppendBackgroundColorRGB (output, attr.Background.R, attr.Background.G, attr.Background.B);
            EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
        }
    }


    private Size? _lastSize;
    private Size? _lastWindowSizeBeforeMaximized;
    private bool _lockResize;

    public Size GetWindowSize ()
    {
        if (_lockResize)
        {
            return _lastSize!.Value;
        }

        var newSize = GetWindowSize (out _);
        Size largestWindowSize = GetLargestConsoleWindowSize ();

        if (_lastWindowSizeBeforeMaximized is null && newSize == largestWindowSize)
        {
            _lastWindowSizeBeforeMaximized = _lastSize;
        }
        else if (_lastWindowSizeBeforeMaximized is { } && newSize != largestWindowSize)
        {
            if (newSize != _lastWindowSizeBeforeMaximized)
            {
                newSize = _lastWindowSizeBeforeMaximized.Value;
            }

            _lastWindowSizeBeforeMaximized = null;
        }

        if (_lastSize == null || _lastSize != newSize)
        {
            // User is resizing the screen, they can only ever resize the active
            // buffer since. We now however have issue because background offscreen
            // buffer will be wrong size, recreate it to ensure it doesn't result in
            // differing active and back buffer sizes (which causes flickering of window size)
            Size? bufSize = null;
            while (bufSize != newSize)
            {
                _lockResize = true;
                bufSize = ResizeBuffer (newSize);
            }

            _lockResize = false;
            _lastSize = newSize;
        }

        return newSize;
    }

    public Size GetWindowSize (out WindowsConsole.Coord cursorPosition)
    {
        var csbi = new WindowsConsole.CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (_isVirtualTerminal ? _outputHandle : _screenBuffer, ref csbi))
        {
            //throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
            cursorPosition = default;
            return Size.Empty;
        }

        Size sz = new (
                       csbi.srWindow.Right - csbi.srWindow.Left + 1,
                       csbi.srWindow.Bottom - csbi.srWindow.Top + 1);

        cursorPosition = csbi.dwCursorPosition;
        return sz;
    }

    private Size GetLargestConsoleWindowSize ()
    {
        WindowsConsole.Coord maxWinSize = GetLargestConsoleWindowSize (_isVirtualTerminal ? _outputHandle : _screenBuffer);

        return new (maxWinSize.X, maxWinSize.Y);
    }

    /// <inheritdoc />
    protected override bool SetCursorPositionImpl (int screenPositionX, int screenPositionY)
    {
        if (_force16Colors && !_isVirtualTerminal)
        {
            SetConsoleCursorPosition (_screenBuffer, new ((short)screenPositionX, (short)screenPositionY));
        }
        else
        {
            // CSI codes are 1 indexed
            _everythingStringBuilder.Append (EscSeqUtils.CSI_SaveCursorPosition);
            EscSeqUtils.CSI_AppendCursorPosition (_everythingStringBuilder, screenPositionY + 1, screenPositionX + 1);
        }

        _lastCursorPosition = new (screenPositionX, screenPositionY);

        return true;
    }

    /// <inheritdoc/>
    public override void SetCursorVisibility (CursorVisibility visibility)
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        if (!_isVirtualTerminal)
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

    private Point? _lastCursorPosition;

    /// <inheritdoc/>
    public void SetCursorPosition (int col, int row)
    {
        if (_lastCursorPosition is { } && _lastCursorPosition.Value.X == col && _lastCursorPosition.Value.Y == row)
        {
            return;
        }

        _lastCursorPosition = new (col, row);

        if (_isVirtualTerminal)
        {
            var sb = new StringBuilder ();
            EscSeqUtils.CSI_AppendCursorPosition (sb, row + 1, col + 1);
            Write (sb.ToString ());
        }
        else
        {
            SetConsoleCursorPosition (_screenBuffer, new ((short)col, (short)row));
        }
    }

    private bool _isDisposed;
    private bool _force16Colors;
    private nint _consoleBuffer;
    private StringBuilder _everythingStringBuilder;

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_isVirtualTerminal)
        {
            //Disable alternative screen buffer.
            Console.Out.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
        }
        else
        {
            if (_screenBuffer != nint.Zero)
            {
                CloseHandle (_screenBuffer);
            }

            _screenBuffer = nint.Zero;
        }

        _isDisposed = true;
    }
}
