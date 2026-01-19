using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for enabling Windows Virtual Terminal Input mode.
/// </summary>
/// <remarks>
///     <para>
///         When Virtual Terminal (VT) Input mode is enabled via <c>ENABLE_VIRTUAL_TERMINAL_INPUT</c>,
///         the Windows Console converts user input (keyboard, mouse) into ANSI escape sequences that
///         can be read via standard input APIs like <c>ReadFile</c> or <c>Console.OpenStandardInput()</c>.
///     </para>
///     <para>
///         This provides a unified, cross-platform ANSI input mechanism where:
///         <list type="bullet">
///             <item>Keyboard input becomes ANSI sequences (e.g., Arrow Up = ESC[A)</item>
///             <item>Mouse input becomes SGR format sequences (e.g., ESC[&lt;0;10;5M)</item>
///             <item>All input can be parsed uniformly with <see cref="AnsiResponseParser{TInputRecord}"/></item>
///         </list>
///     </para>
/// </remarks>
internal sealed class WindowsVTInputHelper : IDisposable
{
    #region P/Invoke Declarations

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile (
        nint hFile,
        byte [] lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        nint lpOverlapped
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool GetNumberOfConsoleInputEvents (
        nint hConsoleInput,
        out uint lpcNumberOfEvents
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool FlushConsoleInputBuffer (nint hConsoleInput);

    // Standard handles.
    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;
    private const int STD_ERROR_HANDLE = -12;

    // Input console modes flags.
    private const uint ENABLE_PROCESSED_INPUT = 1;
    private const uint ENABLE_LINE_INPUT = 2;
    private const uint ENABLE_ECHO_INPUT = 4;
    private const uint ENABLE_WINDOW_INPUT = 8;
    private const uint ENABLE_MOUSE_INPUT = 16;
    private const uint ENABLE_INSERT_MODE = 32;
    private const uint ENABLE_QUICK_EDIT_MODE = 64;
    private const uint ENABLE_EXTENDED_FLAGS = 128;
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 512;

    // Output console modes flags.
    private const uint ENABLE_PROCESSED_OUTPUT = 1;
    private const uint ENABLE_WRAP_AT_EOL_OUTPUT = 2;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 8;
    private const uint ENABLE_LVB_GRID_WORLDWIDE = 10;

    #endregion

    private uint _originalInputConsoleMode;
    private bool _disposed;
    private nint _outputHandle;
    private uint _originalOutputConsoleMode;

    /// <summary>
    ///     Gets whether VT input mode was successfully enabled.
    /// </summary>
    public bool IsVTModeEnabled { get; private set; }

    /// <summary>
    ///     Gets the Windows console input handle.
    /// </summary>
    public nint InputHandle { get; private set; }

    /// <summary>
    ///     Attempts to enable Windows Virtual Terminal Input mode.
    /// </summary>
    /// <returns>True if VT mode was enabled successfully; false otherwise.</returns>
    public bool TryEnable ()
    {
        if (IsVTModeEnabled)
        {
            return true;
        }

        // Only attempt on Windows
        if (!RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
        {
            return false;
        }

        try
        {
            InputHandle = GetStdHandle (STD_INPUT_HANDLE);

            if (InputHandle == nint.Zero || InputHandle == new nint (-1))
            {
                Logging.Warning ("Failed to get Windows console input handle.");

                return false;
            }

            if (!GetConsoleMode (InputHandle, out _originalInputConsoleMode))
            {
                Logging.Warning ("Failed to get Windows input console mode.");

                return false;
            }

            // Configure VT input mode:
            // - Enable: VT input, mouse input, extended flags
            // - Disable: processed input, line input, echo, quick edit
            // This allows raw ANSI sequence reading
            uint newMode = _originalInputConsoleMode;
            newMode |= ENABLE_VIRTUAL_TERMINAL_INPUT | ENABLE_MOUSE_INPUT | ENABLE_EXTENDED_FLAGS;
            newMode &= ~(ENABLE_PROCESSED_INPUT | ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT | ENABLE_QUICK_EDIT_MODE);

            if (!SetConsoleMode (InputHandle, newMode))
            {
                Logging.Warning ("Failed to set Windows VT input console mode.");

                return false;
            }

            _outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);

            if (_outputHandle == nint.Zero || _outputHandle == new nint (-1))
            {
                Logging.Warning ("Failed to get Windows output console handle.");

                return false;
            }

            if (!GetConsoleMode (_outputHandle, out _originalOutputConsoleMode))
            {
                Logging.Warning ("Failed to get Windows output console mode.");

                return false;
            }

            newMode = _originalOutputConsoleMode;
            newMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;

            if (!SetConsoleMode (_outputHandle, newMode))
            {
                Logging.Warning ("Failed to set Windows VT output console mode.");

                return false;
            }

            IsVTModeEnabled = true;
            Logging.Information ($"Windows VT input mode enabled successfully. Mode: 0x{newMode:X} (was 0x{_originalInputConsoleMode:X})");

            return true;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to enable Windows VT mode: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Checks if console input events are available.
    /// </summary>
    /// <param name="eventCount">Number of events available, if successful.</param>
    /// <returns>True if check succeeded; false otherwise.</returns>
    public bool TryGetInputEventCount (out uint eventCount)
    {
        eventCount = 0;

        if (!IsVTModeEnabled || InputHandle == nint.Zero)
        {
            return false;
        }

        try
        {
            if (GetNumberOfConsoleInputEvents (InputHandle, out eventCount))
            {
                return true;
            }

            int error = Marshal.GetLastWin32Error ();
            Logging.Warning ($"GetNumberOfConsoleInputEvents failed with error: {error}");

            return false;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Exception checking console input events: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Reads ANSI input sequences from the console.
    /// </summary>
    /// <param name="buffer">Buffer to read into.</param>
    /// <param name="bytesRead">Number of bytes actually read.</param>
    /// <returns>True if read succeeded; false otherwise.</returns>
    public bool TryRead (byte [] buffer, out int bytesRead)
    {
        bytesRead = 0;

        if (!IsVTModeEnabled || InputHandle == nint.Zero)
        {
            return false;
        }

        try
        {
            if (!Console.KeyAvailable)
            {
                return false;
            }

            bool success = ReadFile (InputHandle, buffer, (uint)buffer.Length, out uint numBytesRead, nint.Zero);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error ();
                Logging.Warning ($"ReadFile failed with error code: {error}");

                return false;
            }

            bytesRead = (int)numBytesRead;

            return true;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Error reading Windows console input: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Restores the console to its original mode.
    /// </summary>
    public void Restore ()
    {
        if (!IsVTModeEnabled || _disposed || InputHandle == nint.Zero)
        {
            return;
        }

        IsVTModeEnabled = false;

        try
        {
            // Flush the input buffer to clear any pending INPUT_RECORD structures
            // This prevents residual ANSI responses from lingering in the OS buffer
            if (!FlushConsoleInputBuffer (InputHandle))
            {
                int error = Marshal.GetLastWin32Error ();
                Logging.Warning ($"FlushConsoleInputBuffer failed with error: {error}");
            }

            if (!SetConsoleMode (InputHandle, _originalInputConsoleMode))
            {
                throw new ApplicationException ($"Failed to restore input console mode, error code: {Marshal.GetLastPInvokeError ()}.");
            }

            if (!SetConsoleMode (_outputHandle, _originalOutputConsoleMode))
            {
                throw new ApplicationException ($"Failed to restore output console mode, error code: {Marshal.GetLastPInvokeError ()}.");
            }

            IsVTModeEnabled = false;
            Logging.Information ("Windows console mode restored.");
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to restore Windows console mode: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        Restore ();
        _disposed = true;
    }
}
