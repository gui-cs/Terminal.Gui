using System.Collections.Concurrent;
using System.Text;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <para>
///         <see cref="IInput{TInputRecord}"/> implementation that uses a character stream for pure ANSI input.
///         Supports both test injection via <see cref="ITestableInput{TInputRecord}"/> and real console reading.
///     </para>
///     <para>
///         <b>Pure ANSI Driver with Limitations</b>
///     </para>
///     <para>
///         This driver reads raw bytes from <see cref="Console.OpenStandardInput()"/> and processes them as
///         ANSI escape sequences, making it a true pure ANSI driver. However, it has a critical limitation:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>No Raw Mode on Unix/Mac</b> - Unlike <see cref="UnixInput"/> which uses termios to disable
///             line buffering, this driver cannot put the terminal in raw mode. On Unix/Mac, input will be
///             line-buffered (requiring Enter key) unless the terminal is already in raw mode.
///         </item>
///         <item>
///             <b>Windows VT Mode Required</b> - On Windows, the console must be in Virtual Terminal mode
///             for ANSI sequences to work properly.
///         </item>
///     </list>
///     <para>
///         <b>When This Driver Works Well:</b>
///     </para>
///     <list type="bullet">
///         <item><b>Unit Tests</b> - Perfect for testing via <see cref="ITestableInput{TInputRecord}"/></item>
///         <item><b>Windows Terminal</b> - Works well in modern Windows terminals with VT support</item>
///         <item><b>Pre-configured Terminals</b> - Works if terminal is already in raw mode</item>
///     </list>
///     <para>
///         <b>For Production Applications, Prefer:</b>
///     </para>
///     <list type="bullet">
///         <item><see cref="UnixInput"/> on Unix/Mac (uses termios for raw mode)</item>
///         <item><c>WindowsInput</c> on Windows (uses Windows Console API directly)</item>
///         <item><c>NetInput</c> for cross-platform (uses <see cref="ConsoleKeyInfo"/> model)</item>
///     </list>
///     <para>
///         <b>Architecture:</b>
///     </para>
///     <para>
///         Reads raw bytes from <see cref="Console.OpenStandardInput()"/>, converts them to UTF-8 characters,
///         and feeds them to <see cref="AnsiResponseParser{TInputRecord}"/> which extracts keyboard events,
///         mouse events (SGR format), and terminal responses.
///     </para>
///     <para>
///         <b>Thread Safety:</b> The <see cref="Peek"/> and <see cref="Read"/> methods execute on the input
///         thread created by <see cref="MainLoopCoordinator{TInputRecord}.StartInputTaskAsync"/>, while
///         <see cref="AddInput"/> is called from the main loop thread for test injection.
///     </para>
/// </summary>
public class FakeInput : InputImpl<char>, ITestableInput<char>
{
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
        byte [] buffer = new byte [256];
        int bytesRead;

        try
        {
            // Read available bytes without blocking
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
