using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <para>
///         Pure ANSI Driver with VT Input Mode on Windows and termios raw mode on Unix/Mac.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         <see cref="IInput{TInputRecord}"/> implementation that uses a character stream for pure ANSI input.
///         Supports both test injection via <see cref="ITestableInput{TInputRecord}"/> and real console reading.
///     </para>
///     <para>
///         This driver reads raw bytes from <see cref="Console.OpenStandardInput()"/> and processes them as
///         ANSI escape sequences. It configures the terminal for proper ANSI input:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Unix/Mac</b> - Uses <see cref="UnixRawModeHelper"/> to disable echo and line buffering (raw mode).
///             This works reliably on all Unix-like systems.
///         </item>
///         <item>
///             <b>Windows</b> - Uses <see cref="WindowsVTInputHelper"/> to enable Virtual Terminal Input mode.
///             This mode converts console input to ANSI escape sequences that can be read via
///             <see cref="Console.OpenStandardInput()"/>. Mouse events, keyboard input, etc. are all
///             provided as VT sequences.
///         </item>
///     </list>
///     <para>
///         <b>How It Works on Windows:</b>
///     </para>
///     <para>
///         When <c>ENABLE_VIRTUAL_TERMINAL_INPUT</c> is enabled, the Windows Console converts user input
///         (keyboard, mouse) into Console Virtual Terminal Sequences. These sequences can then be read
///         via <see cref="Console.OpenStandardInput()"/> just like on Unix systems. This provides a
///         unified, cross-platform ANSI input mechanism.
///     </para>
///     <para>
///         <b>Implementation Notes:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Windows</b>: Uses <c>ReadFile</c> API (via <see cref="WindowsVTInputHelper"/>) to read ANSI sequences
///         </item>
///         <item>
///             <b>Unix/Mac</b>: Uses <c>ReadAsync</c> with short timeouts (10-15ms) on
///             <see cref="Console.OpenStandardInput()"/>
///         </item>
///         <item>
///             <b>Windows</b>: Uses <c>GetNumberOfConsoleInputEvents</c> to reliably check for available input
///         </item>
///         <item>
///             <b>Unix/Mac</b>: Always attempts read (with timeout) as stream peeking is unreliable
///         </item>
///         <item>
///             Throttled by <see cref="InputImpl{TInputRecord}.Run"/> (20ms delay between polls)
///         </item>
///         <item>
///             Suitable for both production use and unit testing
///         </item>
///     </list>
///     <para>
///         <b>Platform Support:</b>
///     </para>
///     <list type="bullet">
///         <item><b>Unix/Mac</b> - Uses termios for raw mode (like UnixInput)</item>
///         <item><b>Windows</b> - Uses VT input mode for ANSI sequence reading</item>
///         <item><b>Unit Tests</b> - Always works via <see cref="ITestableInput{TInputRecord}"/></item>
///     </list>
///     <para>
///         <b>Architecture:</b>
///     </para>
///     <para>
///         Reads raw bytes from <see cref="Console.OpenStandardInput()"/>, converts them to UTF-8 characters,
///         and feeds them to <see cref="AnsiResponseParser{TInputRecord}"/> which extracts keyboard events,
///         mouse events (SGR format), and terminal responses.
///     </para>
/// </remarks>
public class AnsiInput : InputImpl<char>, ITestableInput<char>
{
    // Platform-specific helpers
    private readonly UnixRawModeHelper? _unixRawMode;
    private readonly WindowsVTInputHelper? _windowsVTInput;

    // Queue for storing injected input that will be returned by Peek/Read
    private readonly ConcurrentQueue<char> _testInput = new ();

    private int _peekCallCount;

    /// <summary>
    ///     Gets the number of times <see cref="Peek"/> has been called.
    ///     This is useful for verifying that the input loop throttling is working correctly.
    /// </summary>
    internal int PeekCallCount => _peekCallCount;

    private readonly bool _terminalInitialized;
    private Stream? _inputStream;

    /// <summary>
    ///     Creates a new ANSIInput.
    /// </summary>
    public AnsiInput ()
    {
        Logging.Information ($"Creating {nameof (AnsiInput)}");

        try
        {
            // Check if we have a real console first
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                Logging.Warning ("Console is redirected. Running in degraded mode.");
                _terminalInitialized = false;

                return;
            }

            // Initialize platform-specific input helpers
            if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
            {
                _windowsVTInput = new ();
                _windowsVTInput.TryEnable ();
            }
            else
            {
                _unixRawMode = new ();
                _unixRawMode.TryEnable ();
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

            // NOTE: Output operations (alternate buffer, cursor visibility, mouse events)
            // NOTE: are handled by ANSIOutput, not here. ANSIInput only handles input.

            _terminalInitialized = true;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to initialize terminal: {ex.Message}. Running in degraded mode.");
            _terminalInitialized = false;
        }
    }

    /// <inheritdoc/>
    public override bool Peek ()
    {
        // Will be called on the input thread.
        Interlocked.Increment (ref _peekCallCount);

        // Check test input first - this allows immediate test input processing
        if (!_testInput.IsEmpty)
        {
            return true;
        }

        if (!_terminalInitialized)
        {
            return false;
        }

        // On Windows with VT mode, use helper to check for console input events
        if (_windowsVTInput?.IsVTModeEnabled == true)
        {
            if (_windowsVTInput.TryGetInputEventCount (out uint numEvents))
            {
                bool hasEvents = numEvents > 0;

                //if (hasEvents && _peekCallCount % 100 == 0)
                //{
                //    Logging.Trace ($"Peek: {numEvents} events available");
                //}

                return hasEvents;
            }

            return false;
        }

        // On Unix, we can't reliably peek the stream, so always return true
        // and let Read() handle the timeout-based check
        return _inputStream != null;
    }

    /// <inheritdoc/>
    public override IEnumerable<char> Read ()
    {
        // Will be called on the input thread.
        while (_testInput.TryDequeue (out char input))
        {
            yield return input;
        }

        if (!_terminalInitialized)
        {
            yield break;
        }

        var buffer = new byte [256];
        int bytesRead;

        // On Windows with VT mode, use helper to read ANSI sequences
        if (_windowsVTInput?.IsVTModeEnabled == true)
        {
            if (!_windowsVTInput.TryRead (buffer, out bytesRead))
            {
                yield break;
            }
        }

        // On Unix, use the stream with timeout-based async read
        else if (_inputStream != null)
        {
            try
            {
                // Use a very short timeout for non-blocking behavior
                using var cts = new CancellationTokenSource (10);
                Task<int> readTask = _inputStream.ReadAsync (buffer, 0, buffer.Length, cts.Token);

                // Wait for the read with a slightly longer timeout than the cancellation token
                if (!readTask.Wait (15))
                {
                    // Timeout - no data available
                    yield break;
                }

                bytesRead = readTask.Result;
            }
            catch (OperationCanceledException)
            {
                // Timeout - no data actually available
                yield break;
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
                // Timeout from the task
                yield break;
            }
            catch (Exception ex)
            {
                Logging.Warning ($"Error reading input stream: {ex.Message}");

                yield break;
            }
        }
        else
        {
            yield break;
        }

        if (bytesRead == 0)
        {
            yield break;
        }

        // Convert UTF-8 bytes to characters
        // With ENABLE_VIRTUAL_TERMINAL_INPUT, Windows provides ANSI escape sequences
        // These are UTF-8 compatible (ANSI sequences are ASCII, user input is UTF-8)
        string text = Encoding.UTF8.GetString (buffer, 0, bytesRead);

        foreach (char ch in text)
        {
            yield return ch;
        }
    }

    /// <summary>
    ///     Flushes any pending input from the console buffer.
    ///     This prevents ANSI responses from leaking into the shell after the app exits.
    /// </summary>
    private void FlushInput ()
    {
        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
            // On Unix, read with very short timeout until no more data
            // Note: On Windows, we skip flushing because the console handles it automatically
            // when we restore the console mode, and attempting to flush while shutting down
            // can cause ReadFile to block indefinitely.
            if (_inputStream != null && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                byte [] buffer = new byte [256];
                int flushCount = 0;
                const int MAX_FLUSH_ATTEMPTS = 10;

                while (flushCount < MAX_FLUSH_ATTEMPTS)
                {
                    try
                    {
                        using CancellationTokenSource cts = new CancellationTokenSource (5); // Very short timeout
                        Task<int> readTask = _inputStream.ReadAsync (buffer, 0, buffer.Length, cts.Token);

                        if (!readTask.Wait (10) || readTask.Result == 0)
                        {
                            break;
                        }

                        flushCount++;
                        Logging.Trace ($"FlushInput: Discarded {readTask.Result} bytes (attempt {flushCount})");
                    }
                    catch (OperationCanceledException)
                    {
                        break; // No more data
                    }
                    catch (AggregateException)
                    {
                        break; // No more data
                    }
                }

                if (flushCount > 0)
                {
                    Logging.Information ($"FlushInput: Flushed input buffer ({flushCount} read attempts)");
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Error flushing input: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void InjectInput (char input)
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
            // Flush any pending input (Unix only - Windows handles this automatically)
            // This prevents ANSI responses (like size queries) from leaking into the shell
            FlushInput ();

            // Restore platform-specific terminal settings
            _unixRawMode?.Dispose ();
            _windowsVTInput?.Dispose ();

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
