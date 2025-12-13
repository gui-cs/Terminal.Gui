using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <para>
///         <see cref="IInput{TInputRecord}"/> implementation that uses a character stream for pure ANSI input.
///         Supports both test injection via <see cref="ITestableInput{TInputRecord}"/> and real console reading.
///     </para>
///     <para>
///         <b>Pure ANSI Driver with Optional Raw Mode</b>
///     </para>
///     <para>
///         This driver reads raw bytes from <see cref="Console.OpenStandardInput()"/> and processes them as
///         ANSI escape sequences. It attempts to configure the terminal for proper ANSI input:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Unix/Mac</b> - Uses termios P/Invoke to disable echo and line buffering (raw mode).
///             This works reliably on all Unix-like systems.
///         </item>
///         <item>
///             <b>Windows</b> - Enables Virtual Terminal Input mode via Windows Console API.
///             <b>Important</b>: On Windows, even with VT mode, <see cref="Console.OpenStandardInput()"/>
///             may not provide character-by-character input. Windows Console input is message-based.
///             For reliable Windows input, use <c>WindowsInput</c> which uses <c>ReadConsoleInput</c> API.
///         </item>
///     </list>
///     <para>
///         <b>Limitations:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Windows</b> - May not work properly due to Console input architecture differences.
///             `Console.OpenStandardInput().Read()` can block even when `Console.KeyAvailable` is true.
///         </item>
///         <item>
///             <b>Recommended for Unit Tests Only</b> - For production, use platform-specific drivers
///             (UnixInput, WindowsInput) which use native APIs.
///         </item>
///     </list>
///     <para>
///         <b>Platform Support:</b>
///     /// </para>
///     <list type="bullet">
///         <item><b>Unix/Mac</b> - Attempts to use termios for raw mode (like UnixInput)</item>
///         <item><b>Windows</b> - Works with Virtual Terminal mode enabled</item>
///         <item><b>Unit Tests</b> - Always works via <see cref="ITestableInput{TInputRecord}"/></item>
///     /// </para>
///     <para>
///         <b>Architecture:</b>
///     /// </para>
///     <para>
///         Reads raw bytes from <see cref="Console.OpenStandardInput()"/>, converts them to UTF-8 characters,
///         and feeds them to <see cref="AnsiResponseParser{TInputRecord}"/> which extracts keyboard events,
///         mouse events (SGR format), and terminal responses.
///     /// </para>
/// </summary>
public class FakeInput : InputImpl<char>, ITestableInput<char>
{
    #region Platform-Specific Raw Mode Support (Unix/Mac)

    // Termios structures and constants for Unix raw mode
    [StructLayout (LayoutKind.Sequential)]
    private struct Termios
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;

        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 32)]
        public byte [] c_cc;

        public uint c_ispeed;
        public uint c_ospeed;
    }

    // Unix termios constants
    private const int STDIN_FILENO = 0;
    private const int TCSANOW = 0;
    private const ulong BRKINT = 0x00000002;
    private const ulong ICRNL = 0x00000100;
    private const ulong INPCK = 0x00000010;
    private const ulong ISTRIP = 0x00000020;
    private const ulong IXON = 0x00000400;
    private const ulong OPOST = 0x00000001;
    private const ulong CS8 = 0x00000030;
    private const ulong ECHO = 0x00000008;
    private const ulong ICANON = 0x00000100;
    private const ulong IEXTEN = 0x00008000;
    private const ulong ISIG = 0x00000001;

    // P/Invoke declarations (only available on Unix)
    [DllImport ("libc", SetLastError = true)]
    private static extern int tcgetattr (int fd, out Termios termios);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcsetattr (int fd, int optional_actions, ref Termios termios);

    [DllImport ("libc", EntryPoint = "cfmakeraw", SetLastError = false)]
    private static extern void cfmakeraw_ref (ref Termios termios);

    private Termios _originalTermios;
    private bool _rawModeEnabled;

    #endregion

    #region Platform-Specific Input (Windows)

    // Windows Console API for VT input
    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    private const int STD_INPUT_HANDLE = -10;
    private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
    private const uint ENABLE_PROCESSED_INPUT = 0x0001;
    private const uint ENABLE_MOUSE_INPUT = 0x0010;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

    private nint _windowsInputHandle;
    private uint _originalWindowsConsoleMode;
    private bool _windowsVtModeEnabled;

    #endregion

    // Queue for storing injected input that will be returned by Peek/Read
    private readonly ConcurrentQueue<char> _testInput = new ();

    private int _peekCallCount;

    /// <summary>
    ///     Gets the number of times <see cref="Peek"/> has been called.
    ///     This is useful for verifying that the input loop throttling is working correctly.
    /// </summary>
    internal int PeekCallCount => _peekCallCount;

    private bool _terminalInitialized;
    private Stream? _inputStream;

    /// <summary>
    ///     Creates a new FakeInput.
    /// </summary>
    public FakeInput ()
    {
        Logging.Information ($"Creating {nameof (FakeInput)}");

        try
        {
            // Check if we have a real console first
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                Logging.Warning ("Console is redirected. Running in degraded mode.");
                _terminalInitialized = false;
                return;
            }

            // Try to enable raw mode on Unix/Mac platforms
            TryEnableRawMode ();

            // Get the raw input stream for ANSI sequence reading
            _inputStream = Console.OpenStandardInput ();

            if (!_inputStream.CanRead)
            {
                Logging.Warning ("Console input stream is not readable. Running in degraded mode.");
                _terminalInitialized = false;
                return;
            }

            // Try to disable Ctrl+C handling to allow raw input
            try
            {
                Console.TreatControlCAsInput = true;
            }
            catch
            {
                // Not supported in all environments
            }

            Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
            Console.Out.Write (EscSeqUtils.CSI_HideCursor);
            Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);

            _terminalInitialized = true;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to initialize terminal: {ex.Message}. Running in degraded mode.");
            _terminalInitialized = false;
        }
    }

    private void TryEnableRawMode ()
    {
        // Windows: Enable Virtual Terminal Input mode
        if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
        {
            TryEnableWindowsVTMode ();
            return;
        }

        // Unix/Mac: Use termios for raw mode
        if (!RuntimeInformation.IsOSPlatform (OSPlatform.Linux) && 
            !RuntimeInformation.IsOSPlatform (OSPlatform.OSX) &&
            !RuntimeInformation.IsOSPlatform (OSPlatform.FreeBSD))
        {
            return;
        }

        try
        {
            // Get current terminal attributes
            int result = tcgetattr (STDIN_FILENO, out _originalTermios);

            if (result != 0)
            {
                int errno = Marshal.GetLastWin32Error ();
                Logging.Warning ($"tcgetattr failed (errno={errno}). Cannot enable raw mode.");
                return;
            }

            // Create modified attributes for raw mode
            Termios raw = _originalTermios;

            try
            {
                // Try using cfmakeraw if available
                cfmakeraw_ref (ref raw);
            }
            catch (EntryPointNotFoundException)
            {
                // Manually configure raw mode if cfmakeraw not available
                raw.c_iflag &= ~((uint)BRKINT | (uint)ICRNL | (uint)INPCK | (uint)ISTRIP | (uint)IXON);
                raw.c_oflag &= ~(uint)OPOST;
                raw.c_cflag |= (uint)CS8;
                raw.c_lflag &= ~((uint)ECHO | (uint)ICANON | (uint)IEXTEN | (uint)ISIG);
            }

            // Apply raw mode settings
            result = tcsetattr (STDIN_FILENO, TCSANOW, ref raw);

            if (result != 0)
            {
                int errno = Marshal.GetLastWin32Error ();
                Logging.Warning ($"tcsetattr failed (errno={errno}). Cannot enable raw mode.");
                return;
            }

            _rawModeEnabled = true;
            Logging.Information ("Raw mode enabled successfully.");
        }
        catch (DllNotFoundException)
        {
            // libc not available - expected on non-Unix platforms
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to enable raw mode: {ex.Message}");
        }
    }

    private void TryEnableWindowsVTMode ()
    {
        try
        {
            _windowsInputHandle = GetStdHandle (STD_INPUT_HANDLE);

            if (!GetConsoleMode (_windowsInputHandle, out _originalWindowsConsoleMode))
            {
                Logging.Warning ("Failed to get Windows console mode.");
                return;
            }

            // Enable VT input, mouse input, and disable quick edit
            uint newMode = _originalWindowsConsoleMode;
            newMode |= ENABLE_VIRTUAL_TERMINAL_INPUT | ENABLE_MOUSE_INPUT | ENABLE_EXTENDED_FLAGS;
            newMode &= ~(ENABLE_QUICK_EDIT_MODE | ENABLE_PROCESSED_INPUT);

            if (!SetConsoleMode (_windowsInputHandle, newMode))
            {
                Logging.Warning ("Failed to set Windows VT console mode.");
                return;
            }

            _windowsVtModeEnabled = true;
            Logging.Information ("Windows VT input mode enabled successfully.");
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to enable Windows VT mode: {ex.Message}");
        }
    }

    private void WriteRaw (string text)
    {
        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
            Console.Out.Write (text);
        }
        catch
        {
            // ignore exceptions during write
        }
    }

    /// <inheritdoc/>
    public override bool Peek ()
    {
        // Will be called on the input thread.
        Interlocked.Increment (ref _peekCallCount);

        // Check test input first
        if (!_testInput.IsEmpty)
        {
            return true;
        }

        if (!_terminalInitialized || _inputStream == null)
        {
            return false;
        }

        // For Console.OpenStandardInput(), we can't use Peek() directly
        // We need to check if data is available without blocking
        // On Unix, this works; on Windows with redirected input, it may not
        try
        {
            // Try to read with a timeout of 0 (non-blocking check)
            // Note: This might not work on all platforms, but it's the closest to raw mode
            return _inputStream.CanRead && Console.KeyAvailable;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<char> Read ()
    {
        // Will be called on the input thread.
        while (_testInput.TryDequeue (out char input))
        {
            yield return input;
        }

        if (!_terminalInitialized || _inputStream == null)
        {
            yield break;
        }

        // Read raw bytes from the input stream
        // This gives us pure ANSI escape sequences including mouse events
        // On Windows with VT mode, this should work properly
        // On Unix with raw mode, this definitely works
        byte [] buffer = new byte [256];
        int bytesRead;

        try
        {
            // Read available bytes
            // This should not block if Peek() returned true
            bytesRead = _inputStream.Read (buffer, 0, buffer.Length);
        }
        catch
        {
            yield break;
        }

        if (bytesRead == 0)
        {
            yield break;
        }

        // Convert UTF-8 bytes to characters
        // ANSI sequences are ASCII-compatible, but user text might be UTF-8
        string text = Encoding.UTF8.GetString (buffer, 0, bytesRead);

        foreach (char ch in text)
        {
            yield return ch;
        }
    }

    private void FlushConsoleInput ()
    {
        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
        }
        catch
        {
            // ignore
        }
    }

    /// <inheritdoc/>
    public void AddInput (char input)
    {
        //Logging.Trace ($"Enqueuing input: {input.Key}");

        // Will be called on the main loop thread.
        _testInput.Enqueue (input);
    }

    /// <inheritdoc/>
    public override void Dispose ()
    {
        base.Dispose ();

        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
            WriteRaw (EscSeqUtils.CSI_DisableMouseEvents);
            FlushConsoleInput ();
            WriteRaw (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
            WriteRaw (EscSeqUtils.CSI_ShowCursor);

            // Restore original terminal settings if we enabled raw mode
            if (_rawModeEnabled)
            {
                try
                {
                    tcsetattr (STDIN_FILENO, TCSANOW, ref _originalTermios);
                    Logging.Information ("Terminal settings restored.");
                }
                catch (Exception ex)
                {
                    Logging.Warning ($"Failed to restore terminal settings: {ex.Message}");
                }
            }

            // Restore Windows console mode if we changed it
            if (_windowsVtModeEnabled && _windowsInputHandle != IntPtr.Zero)
            {
                try
                {
                    SetConsoleMode (_windowsInputHandle, _originalWindowsConsoleMode);
                    Logging.Information ("Windows console mode restored.");
                }
                catch (Exception ex)
                {
                    Logging.Warning ($"Failed to restore Windows console mode: {ex.Message}");
                }
            }

            // Don't dispose _inputStream - it's the standard input stream
            // Disposing it would break the console for other code
            _inputStream = null;
        }
        catch
        {
            // ignore exceptions during disposal
        }
    }
}
