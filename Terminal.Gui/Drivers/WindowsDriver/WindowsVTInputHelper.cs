using System.Runtime.InteropServices;
using System.Text.Json;
using static Terminal.Gui.Drivers.WindowsConsole;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for getting input from the Windows Console in Virtual Terminal Sequence (VTS) mode using only
///     low-level Windows APIs.
/// </summary>
/// <remarks>
///     <para>
///         When Virtual Terminal Sequences (VTS) Input mode is enabled via <c>ENABLE_VIRTUAL_TERMINAL_INPUT</c>,
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

    [DllImport ("kernel32.dll", EntryPoint = "PeekConsoleInputW", CharSet = CharSet.Unicode)]
    public static extern bool PeekConsoleInput (
        nint hConsoleInput,
        nint lpBuffer,
        uint nLength,
        out uint lpNumberOfEventsRead
    );

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

    [DllImport ("kernel32.dll")]
    internal static extern uint GetConsoleCP ();

    // Console mode flags
    private const int STD_INPUT_HANDLE = -10;
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
    private const uint ENABLE_PROCESSED_INPUT = 0x0001;
    private const uint ENABLE_LINE_INPUT = 0x0002;
    private const uint ENABLE_ECHO_INPUT = 0x0004;
    private const uint ENABLE_MOUSE_INPUT = 0x0010;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

    #endregion

    private uint _originalConsoleMode;
    private bool _disposed;

    /// <summary>
    ///     Gets whether VTS input mode was successfully enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => field;
        private set
        {
            field = value;
            Logging.Trace ($"{value}");
        }
    }

    /// <summary>
    ///     Gets the Windows console input handle.
    /// </summary>
    public nint InputHandle { get; private set; }

    /// <summary>
    ///     Attempts to enable Windows Virtual Terminal Input mode.
    /// </summary>
    /// <returns>True if VTS mode was enabled successfully; false otherwise.</returns>
    public bool TryEnable ()
    {
        if (IsEnabled)
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

            if (!GetConsoleMode (InputHandle, out _originalConsoleMode))
            {
                Logging.Warning ("Failed to get Windows console mode.");

                return false;
            }

            // Configure VTS input mode:
            // - Enable: VTS input, mouse input, extended flags
            // - Disable: processed input, line input, echo, quick edit
            // This allows raw ANSI sequence reading
            uint newMode = _originalConsoleMode;
            newMode |= ENABLE_VIRTUAL_TERMINAL_INPUT | ENABLE_MOUSE_INPUT | ENABLE_EXTENDED_FLAGS;
            newMode &= ~(ENABLE_PROCESSED_INPUT | ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT | ENABLE_QUICK_EDIT_MODE);

            if (!SetConsoleMode (InputHandle, newMode))
            {
                Logging.Warning ("Failed to set Windows VTS console mode.");

                return false;
            }

            Encoding.RegisterProvider (CodePagesEncodingProvider.Instance);

            IsEnabled = true;
            Logging.Information ($"Windows VTS input mode enabled successfully. Mode: 0x{newMode:X} (was 0x{_originalConsoleMode:X})");

            return true;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to enable Windows VTS mode: {ex.Message}");

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

        if (!IsEnabled || InputHandle == nint.Zero)
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

        if (!IsEnabled || InputHandle == nint.Zero || !Console.KeyAvailable)
        {
            return false;
        }

        try
        {
            //Logging.Trace ("ReadFile...");
            bool success = ReadFile (InputHandle, buffer, (uint)buffer.Length, out uint numBytesRead, nint.Zero);
#pragma warning disable IL3050
            //Logging.Trace ($"...{JsonSerializer.Serialize (Encoding.UTF8.GetString (buffer, 0, (int)numBytesRead))}");
#pragma warning restore IL3050

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
        if (!IsEnabled || _disposed || InputHandle == nint.Zero)
        {
            return;
        }

        try
        {
            // Flush the input buffer to clear any pending INPUT_RECORD structures
            // This prevents residual ANSI responses from lingering in the OS buffer
            if (!FlushConsoleInputBuffer (InputHandle))
            {
                int error = Marshal.GetLastWin32Error ();
                Logging.Warning ($"FlushConsoleInputBuffer failed with error: {error}");
            }

            SetConsoleMode (InputHandle, _originalConsoleMode);
            IsEnabled = false;
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

    public bool Peek ()
    {
        const int BUFFER_SIZE = 1; // We only need to check if there's at least one event
        nint pRecord = Marshal.AllocHGlobal (Marshal.SizeOf<InputRecord> () * BUFFER_SIZE);

        try
        {
            // Use PeekConsoleInput to inspect the input buffer without removing events
            if (PeekConsoleInput (InputHandle, pRecord, BUFFER_SIZE, out uint numberOfEventsRead))
            {
                // Return true if there's at least one event in the buffer
                return numberOfEventsRead > 0;
            }
            else
            {
                // Handle the failure of PeekConsoleInput
                throw new InvalidOperationException ("Failed to peek console input.");
            }
        }
        catch (Exception ex)
        {
            // Optionally log the exception
            Logging.Error (@$"Error in Peek: {ex.Message}");

            return false;
        }
        finally
        {
            // Free the allocated memory
            Marshal.FreeHGlobal (pRecord);
        }
    }
}
