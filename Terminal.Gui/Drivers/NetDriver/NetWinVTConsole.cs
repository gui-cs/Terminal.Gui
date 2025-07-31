#nullable enable
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

internal class NetWinVTConsole
{
    // Input modes.
    private const uint ENABLE_PROCESSED_INPUT = 1;
    private const uint ENABLE_LINE_INPUT = 2;
    private const uint ENABLE_ECHO_INPUT = 4;
    private const uint ENABLE_WINDOW_INPUT = 8;
    private const uint ENABLE_MOUSE_INPUT = 16;
    private const uint ENABLE_INSERT_MODE = 32;
    private const uint ENABLE_QUICK_EDIT_MODE = 64;
    private const uint ENABLE_EXTENDED_FLAGS = 128;
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 512;

    // Output modes.
    private const uint ENABLE_PROCESSED_OUTPUT = 1;
    private const uint ENABLE_WRAP_AT_EOL_OUTPUT = 2;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 8;
    private const uint ENABLE_LVB_GRID_WORLDWIDE = 10;

    // Standard handles.
    private const int STD_ERROR_HANDLE = -12;
    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;

    // Handles and original console modes.
    private readonly nint _errorHandle;
    private readonly nint _inputHandle;
    private readonly uint _originalErrorConsoleMode;
    private readonly uint _originalInputConsoleMode;
    private readonly uint _originalOutputConsoleMode;
    private readonly nint _outputHandle;

    public NetWinVTConsole ()
    {
        _inputHandle = GetStdHandle (STD_INPUT_HANDLE);

        if (!GetConsoleMode (_inputHandle, out uint mode))
        {
            throw new ApplicationException ($"Failed to get input console mode, error code: {GetLastError ()}.");
        }

        _originalInputConsoleMode = mode;

        if ((mode & ENABLE_VIRTUAL_TERMINAL_INPUT) < ENABLE_VIRTUAL_TERMINAL_INPUT)
        {
            mode |= ENABLE_VIRTUAL_TERMINAL_INPUT;

            if (!SetConsoleMode (_inputHandle, mode))
            {
                throw new ApplicationException ($"Failed to set input console mode, error code: {GetLastError ()}.");
            }
        }

        _outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);

        if (!GetConsoleMode (_outputHandle, out mode))
        {
            throw new ApplicationException ($"Failed to get output console mode, error code: {GetLastError ()}.");
        }

        _originalOutputConsoleMode = mode;

        if ((mode & (ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN)) < DISABLE_NEWLINE_AUTO_RETURN)
        {
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;

            if (!SetConsoleMode (_outputHandle, mode))
            {
                throw new ApplicationException ($"Failed to set output console mode, error code: {GetLastError ()}.");
            }
        }

        _errorHandle = GetStdHandle (STD_ERROR_HANDLE);

        if (!GetConsoleMode (_errorHandle, out mode))
        {
            throw new ApplicationException ($"Failed to get error console mode, error code: {GetLastError ()}.");
        }

        _originalErrorConsoleMode = mode;

        if ((mode & DISABLE_NEWLINE_AUTO_RETURN) < DISABLE_NEWLINE_AUTO_RETURN)
        {
            mode |= DISABLE_NEWLINE_AUTO_RETURN;

            if (!SetConsoleMode (_errorHandle, mode))
            {
                throw new ApplicationException ($"Failed to set error console mode, error code: {GetLastError ()}.");
            }
        }
    }

    public void Cleanup ()
    {
        if (!SetConsoleMode (_inputHandle, _originalInputConsoleMode))
        {
            throw new ApplicationException ($"Failed to restore input console mode, error code: {GetLastError ()}.");
        }

        if (!SetConsoleMode (_outputHandle, _originalOutputConsoleMode))
        {
            throw new ApplicationException ($"Failed to restore output console mode, error code: {GetLastError ()}.");
        }

        if (!SetConsoleMode (_errorHandle, _originalErrorConsoleMode))
        {
            throw new ApplicationException ($"Failed to restore error console mode, error code: {GetLastError ()}.");
        }
    }

    internal Size GetConsoleBufferWindow (out Point position)
    {
        if (_outputHandle == nint.Zero)
        {
            position = Point.Empty;

            return Size.Empty;
        }

        var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (_outputHandle, ref csbi))
        {
            //throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
            position = Point.Empty;

            return Size.Empty;
        }

        Size sz = new (
                       csbi.srWindow.Right - csbi.srWindow.Left + 1,
                       csbi.srWindow.Bottom - csbi.srWindow.Top + 1);
        position = new (csbi.srWindow.Left, csbi.srWindow.Top);

        return sz;
    }

    internal Size SetConsoleWindow (short cols, short rows)
    {
        var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (_outputHandle, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }

        Coord maxWinSize = GetLargestConsoleWindowSize (_outputHandle);
        short newCols = Math.Min (cols, maxWinSize.X);
        short newRows = Math.Min (rows, maxWinSize.Y);
        csbi.dwSize = new Coord (newCols, Math.Max (newRows, (short)1));
        csbi.srWindow = new SmallRect (0, 0, newCols, newRows);
        csbi.dwMaximumWindowSize = new Coord (newCols, newRows);

        if (!SetConsoleScreenBufferInfoEx (_outputHandle, ref csbi))
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
        if (_outputHandle != nint.Zero && !SetConsoleScreenBufferInfoEx (_outputHandle, ref csbi))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }
    }

    internal Size GetLargestConsoleWindowSize ()
    {
        Coord maxWinSize = GetLargestConsoleWindowSize (_outputHandle);

        return new (maxWinSize.X, maxWinSize.Y);
    }

    // --------------Imports-----------------
    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll")]
    private static extern uint GetLastError ();

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleScreenBufferInfoEx (nint hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX csbi);

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

    // -----------structs-----------------
    [StructLayout (LayoutKind.Sequential)]
    public struct CONSOLE_SCREEN_BUFFER_INFOEX
    {
        public uint cbSize;
        public Coord dwSize;
        public Coord dwCursorPosition;
        public ushort wAttributes;
        public SmallRect srWindow;
        public Coord dwMaximumWindowSize;
        public ushort wPopupAttributes;
        public bool bFullscreenSupported;

        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 16)]
        public COLORREF [] ColorTable;
    }

    [StructLayout (LayoutKind.Sequential)]
    public struct Coord
    {
        public short X;
        public short Y;

        public Coord (short x, short y)
        {
            X = x;
            Y = y;
        }

        public readonly override string ToString () { return $"({X},{Y})"; }
    }

    [StructLayout (LayoutKind.Sequential)]
    public struct SmallRect
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;

        public SmallRect (short left, short top, short right, short bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    [StructLayout (LayoutKind.Explicit, Size = 4)]
    public struct COLORREF
    {
        public COLORREF (byte r, byte g, byte b)
        {
            Value = 0;
            R = r;
            G = g;
            B = b;
        }

        public COLORREF (uint value)
        {
            R = 0;
            G = 0;
            B = 0;
            Value = value & 0x00FFFFFF;
        }

        [FieldOffset (0)]
        public byte R;

        [FieldOffset (1)]
        public byte G;

        [FieldOffset (2)]
        public byte B;

        [FieldOffset (0)]
        public uint Value;
    }
}
