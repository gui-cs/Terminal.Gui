using System.Runtime.InteropServices;
using Terminal.Gui.Tracing;

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
///             <item>All input can be parsed uniformly with <see cref="AnsiResponseParser"/></item>
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
    private static extern bool ReadFile (nint hFile, byte [] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, nint lpOverlapped);

    // Equivalent of poll() on Unix — needed because ReadFile blocks and the input loop
    // requires a non-blocking availability check for throttling and cancellation.
    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool GetNumberOfConsoleInputEvents (nint hConsoleInput, out uint lpcNumberOfEvents);

#if ANSI_DRIVER_SUPPORT_CTRLZ_ON_WINDOWS
    #region Ctrl+Z workaround for Windows bug (https://github.com/microsoft/terminal/issues/4958)

    [DllImport ("kernel32.dll", EntryPoint = "PeekConsoleInputW", CharSet = CharSet.Unicode)]
    private static extern bool PeekConsoleInput (nint hConsoleInput, [Out] InputRecord [] lpBuffer, uint nLength, out uint lpNumberOfEventsRead);

    [DllImport ("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ReadConsoleInput (nint hConsoleInput, [Out] InputRecord [] lpBuffer, uint nLength, out uint lpNumberOfEventsRead);

    #endregion

#endif

    #endregion

    // Console mode flags
    private const int STD_INPUT_HANDLE = -10;
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
    private const uint ENABLE_PROCESSED_INPUT = 0x0001;
    private const uint ENABLE_LINE_INPUT = 0x0002;
    private const uint ENABLE_ECHO_INPUT = 0x0004;
    private const uint ENABLE_MOUSE_INPUT = 0x0010;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

    private uint _originalConsoleMode;
    private bool _disposed;

    /// <summary>
    ///     Gets whether VTS input mode was successfully enabled.
    /// </summary>
    public bool IsEnabled { get; private set; }

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
                Trace.Lifecycle (nameof (WindowsVTInputHelper), "Init", "Failed to get Windows console input handle.");

                return false;
            }

            if (!GetConsoleMode (InputHandle, out _originalConsoleMode))
            {
                Trace.Lifecycle (nameof (WindowsVTInputHelper), "Init", "Failed to get Windows console mode.");

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
                Trace.Lifecycle (nameof (WindowsVTInputHelper), "Init", "Failed to set Windows VTS console mode.");

                return false;
            }

            Encoding.RegisterProvider (CodePagesEncodingProvider.Instance);

            IsEnabled = true;

            Trace.Lifecycle (nameof (WindowsVTInputHelper),
                             "Init",
                             $"Windows VTS input mode enabled successfully. Mode: 0x{newMode:X} (was 0x{_originalConsoleMode:X})");

            return true;
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (WindowsVTInputHelper), "Init", $"Failed to enable Windows VTS mode: {ex.Message}");

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

        if (!IsEnabled || InputHandle == nint.Zero)
        {
            return false;
        }

        try
        {
#if ANSI_DRIVER_SUPPORT_CTRLZ_ON_WINDOWS
            // Workaround for Windows bug (since Win8): ReadFile unconditionally treats
            // Ctrl+Z as EOF (returns 0 bytes) even when ENABLE_PROCESSED_INPUT is disabled.
            // See https://github.com/microsoft/terminal/issues/4958
            // We peek the input buffer and, if the front event is a Ctrl+Z key-down,
            // consume it via ReadConsoleInput and synthesize the 0x1A byte ourselves.
            // This requires GetNumberOfConsoleInputEvents + PeekConsoleInput + ReadConsoleInput.
            if (TryHandleCtrlZ (buffer, out bytesRead))
            {
                return bytesRead > 0;
            }
#endif

            // Read the VT byte stream via ReadFile. With ENABLE_VIRTUAL_TERMINAL_INPUT
            // enabled, the Windows console converts all input (keyboard, mouse, etc.)
            // into ANSI escape sequences in this stream.
            bool success = ReadFile (InputHandle, buffer, (uint)buffer.Length, out uint numBytesRead, nint.Zero);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error ();
                Trace.Lifecycle (nameof (WindowsVTInputHelper), "Read", $"ReadFile failed with error code: {error}");

                return false;
            }

            bytesRead = (int)numBytesRead;

            return bytesRead > 0;
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (WindowsVTInputHelper), "Read", $"Error reading Windows console input: {ex.Message}");

            return false;
        }
    }

#if ANSI_DRIVER_SUPPORT_CTRLZ_ON_WINDOWS
    /// <summary>
    ///     Checks whether the front event in the console input buffer is a Ctrl+Z key-down.
    ///     If so, consumes it via <c>ReadConsoleInput</c> and writes <c>0x1A</c> (SUB) into the buffer.
    /// </summary>
    /// <remarks>
    ///     This works around a Windows bug (since Windows 8) where <c>ReadFile</c> unconditionally
    ///     treats Ctrl+Z as EOF and returns 0 bytes, even when <c>ENABLE_PROCESSED_INPUT</c> is disabled.
    ///     See <see href="https://github.com/microsoft/terminal/issues/4958"/>.
    /// </remarks>
    /// <param name="buffer">Buffer to write the synthesized byte into.</param>
    /// <param name="bytesRead">Set to 1 if Ctrl+Z was handled; 0 otherwise.</param>
    /// <returns><c>true</c> if the front event was Ctrl+Z (handled or discarded); <c>false</c> if it was not Ctrl+Z.</returns>
    private bool TryHandleCtrlZ (byte [] buffer, out int bytesRead)
    {
        bytesRead = 0;
        InputRecord [] peekBuf = new InputRecord [1];

        if (!PeekConsoleInput (InputHandle, peekBuf, 1, out uint peeked) || peeked == 0)
        {
            return false;
        }

        InputRecord rec = peekBuf [0];

        if (rec.EventType != EventType.Key || rec.KeyEvent.UnicodeChar != '\x1A')
        {
            return false;
        }

        // It's a Ctrl+Z event. Consume it from the input buffer.
        InputRecord [] consumeBuf = new InputRecord [1];
        ReadConsoleInput (InputHandle, consumeBuf, 1, out _);

        // Only synthesize the byte for key-down events.
        if (rec.KeyEvent.bKeyDown)
        {
            buffer [0] = 0x1A;
            bytesRead = 1;
        }

        return true;
    }
#endif

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
            SetConsoleMode (InputHandle, _originalConsoleMode);
            IsEnabled = false;
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (WindowsVTInputHelper), "Restore", $"Failed to restore Windows console mode: {ex.Message}");
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

    /// <summary>
    ///     Checks whether input is available without consuming it.
    /// </summary>
    /// <returns><c>true</c> if there is at least one input event available.</returns>
    public bool Peek () => GetNumberOfConsoleInputEvents (InputHandle, out uint count) && count > 0;
}
