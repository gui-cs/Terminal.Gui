namespace Terminal.Gui.ConsoleDrivers.Net;

using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using Microsoft.Win32.SafeHandles;

[MustDisposeResource]
internal sealed partial class NetWinVTConsole : IDisposable
{
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 8;
    private const uint ENABLE_ECHO_INPUT = 4;
    private const uint ENABLE_EXTENDED_FLAGS = 128;
    private const uint ENABLE_INSERT_MODE = 32;
    private const uint ENABLE_LINE_INPUT = 2;
    private const uint ENABLE_LVB_GRID_WORLDWIDE = 10;
    private const uint ENABLE_MOUSE_INPUT = 16;

    // Input modes.
    private const uint ENABLE_PROCESSED_INPUT = 1;

    // Output modes.
    private const uint ENABLE_PROCESSED_OUTPUT = 1;
    private const uint ENABLE_QUICK_EDIT_MODE = 64;
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 512;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;
    private const uint ENABLE_WINDOW_INPUT = 8;
    private const uint ENABLE_WRAP_AT_EOL_OUTPUT = 2;
    private const int STD_ERROR_HANDLE = -12;
    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;

    private readonly SafeHandleMinusOneIsInvalid _errorHandle;
    private readonly SafeHandleMinusOneIsInvalid _inputHandle;
    private readonly SafeHandleMinusOneIsInvalid _outputHandle;
    private readonly uint _originalErrorConsoleMode;
    private readonly uint _originalInputConsoleMode;
    private readonly uint _originalOutputConsoleMode;

    public NetWinVTConsole ()
    {
        _inputHandle = GetStdHandle (STD_INPUT_HANDLE);

        if (!GetConsoleMode (_inputHandle, out uint mode))
        {
            throw new IOException ($"Failed to get input console mode, error code: {GetLastError ()}.");
        }

        _originalInputConsoleMode = mode;

        if ((mode & ENABLE_VIRTUAL_TERMINAL_INPUT) < ENABLE_VIRTUAL_TERMINAL_INPUT)
        {
            mode |= ENABLE_VIRTUAL_TERMINAL_INPUT;

            if (!SetConsoleMode (_inputHandle, mode))
            {
                throw new IOException ($"Failed to set input console mode, error code: {GetLastError ()}.");
            }
        }

        _outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);

        if (!GetConsoleMode (_outputHandle, out mode))
        {
            throw new IOException ($"Failed to get output console mode, error code: {GetLastError ()}.");
        }

        _originalOutputConsoleMode = mode;

        if ((mode & (ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN)) < DISABLE_NEWLINE_AUTO_RETURN)
        {
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;

            if (!SetConsoleMode (_outputHandle, mode))
            {
                throw new IOException ($"Failed to set output console mode, error code: {GetLastError ()}.");
            }
        }

        _errorHandle = GetStdHandle (STD_ERROR_HANDLE);

        if (!GetConsoleMode (_errorHandle, out mode))
        {
            throw new IOException ($"Failed to get error console mode, error code: {GetLastError ()}.");
        }

        _originalErrorConsoleMode = mode;

        if ((mode & DISABLE_NEWLINE_AUTO_RETURN) < DISABLE_NEWLINE_AUTO_RETURN)
        {
            mode |= DISABLE_NEWLINE_AUTO_RETURN;

            if (!SetConsoleMode (_errorHandle, mode))
            {
                throw new IOException ($"Failed to set error console mode, error code: {GetLastError ()}.");
            }
        }
    }

    public void Cleanup ()
    {
        if (!SetConsoleMode (_inputHandle, _originalInputConsoleMode))
        {
            throw new IOException ($"Failed to restore input console mode, error code: {GetLastError ()}.");
        }

        if (!SetConsoleMode (_outputHandle, _originalOutputConsoleMode))
        {
            throw new IOException ($"Failed to restore output console mode, error code: {GetLastError ()}.");
        }

        if (!SetConsoleMode (_errorHandle, _originalErrorConsoleMode))
        {
            throw new IOException ($"Failed to restore error console mode, error code: {GetLastError ()}.");
        }
    }

    [LibraryImport ("kernel32")]
    private static partial bool GetConsoleMode (SafeHandle hConsoleHandle, out uint lpMode);

    [LibraryImport ("kernel32")]
    private static partial uint GetLastError ();

    [MustDisposeResource (false)]
    [LibraryImport ("kernel32", SetLastError = true)]
    private static partial SafeHandleMinusOneIsInvalid GetStdHandle (int nStdHandle);

    [LibraryImport ("kernel32")]
    private static partial bool SetConsoleMode (SafeHandle hConsoleHandle, uint dwMode);

    private volatile bool _disposed;

    private void Dispose (bool disposing)
    {
        if (disposing)
        {
            _errorHandle.Dispose ();
            _inputHandle.Dispose ();
            _outputHandle.Dispose ();
        }
    }

    /// <inheritdoc />
    public void Dispose ()
    {
        if (!_disposed)
        {
            Dispose (true);
            _disposed = true;
            GC.SuppressFinalize (this);
        }
    }

    /// <inheritdoc />
    ~NetWinVTConsole () { Dispose (false); }
}