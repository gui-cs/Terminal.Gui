namespace Terminal.Gui.Drivers;

/// <summary>
///     Size monitor that uses ANSI escape sequences to query terminal size.
///     This demonstrates proper use of <see cref="AnsiResponseParser"/> for detecting terminal resize events.
/// </summary>
/// <remarks>
///     <para>
///         Unlike platform-specific size monitors that use native APIs (e.g., SIGWINCH on Unix or
///         console buffer events on Windows), <see cref="AnsiSizeMonitor"/> uses pure ANSI escape
///         sequences to query the terminal size, making it portable across all ANSI-compatible terminals.
///     </para>
///     <para>
///         <b>How it works:</b>
///     </para>
///     <list type="number">
///         <item><see cref="Poll"/> sends <see cref="EscSeqUtils.CSI_ReportWindowSizeInChars"/> periodically</item>
///         <item>Terminal responds with: ESC [ 8 ; height ; width t</item>
///         <item><see cref="HandleSizeResponse"/> parses the response and updates cached size</item>
///         <item>If size changed, <see cref="ISizeMonitor.SizeChanged"/> event is raised</item>
///     </list>
/// </remarks>
internal class AnsiSizeMonitor : ISizeMonitor
{
    private readonly AnsiOutput _output;
    private Action<AnsiEscapeSequenceRequest>? _queueAnsiRequest;
    private Size _lastSize;
    private DateTime _lastQuery = DateTime.MinValue;
    private readonly TimeSpan _queryThrottle = TimeSpan.FromMilliseconds (500); // Don't spam queries
    private bool _expectingResponse;

    /// <summary>
    ///     Creates a new ANSISizeMonitor.
    /// </summary>
    /// <param name="output">The ANSIOutput instance to query for size</param>
    /// <param name="queueAnsiRequest">Callback to queue ANSI requests (provided by driver/scheduler)</param>
    public AnsiSizeMonitor (AnsiOutput output, Action<AnsiEscapeSequenceRequest>? queueAnsiRequest = null)
    {
        _output = output;
        _queueAnsiRequest = queueAnsiRequest;

        // Get initial size from console or fallback
        _lastSize = _output.GetSize ();
    }

    /// <inheritdoc/>
    public void Initialize (IDriver? driver)
    {
        if (driver is null)
        {
            Logging.Warning ("ANSISizeMonitor: Initialize called with null driver");

            return;
        }

        // Set up the callback to queue ANSI requests through the driver
        _queueAnsiRequest = driver.QueueAnsiRequest;

        Logging.Information ("ANSISizeMonitor: Initialized with driver, sending initial size query");

        // Send the initial size query - response will arrive asynchronously
        // once the input thread starts reading. We don't block here because:
        // 1. The input thread may not have started yet
        // 2. Blocking would create a deadlock (waiting for input that can't be read yet)
        // 3. The response typically arrives within milliseconds after the input thread starts
        SendSizeQuery ();
    }

    private void SendSizeQuery ()
    {
        if (_queueAnsiRequest is null)
        {
            Logging.Warning ("ANSISizeMonitor: Cannot send size query - _queueAnsiRequest is null");

            return;
        }

        _expectingResponse = true;
        _lastQuery = DateTime.Now;

        var request = new AnsiEscapeSequenceRequest
        {
            Request = EscSeqUtils.CSI_ReportWindowSizeInChars.Request,
            Terminator = EscSeqUtils.CSI_ReportWindowSizeInChars.Terminator,
            ResponseReceived = HandleSizeResponse,
            Abandoned = () => { _expectingResponse = false; }
        };

        _queueAnsiRequest! (request);
    }

    /// <inheritdoc/>
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <inheritdoc/>
    public bool Poll ()
    {
        // Throttle queries to avoid spamming the terminal
        if (DateTime.Now - _lastQuery < _queryThrottle)
        {
            // Still check if size changed (in case response came in)
            return CheckSizeChanged ();
        }

        // Send ANSI query if we have a way to queue requests
        if (_queueAnsiRequest != null && !_expectingResponse)
        {
            SendSizeQuery ();
        }

        // Check if size changed
        return CheckSizeChanged ();
    }

    private bool CheckSizeChanged ()
    {
        Size currentSize = _output.GetSize ();

        if (currentSize != _lastSize)
        {
            _lastSize = currentSize;
            SizeChanged?.Invoke (this, new (currentSize));

            return true;
        }

        return false;
    }

    private void HandleSizeResponse (string? response)
    {
        _expectingResponse = false;

        if (string.IsNullOrEmpty (response))
        {
            return;
        }

        // The response is handled by ANSIOutput.HandleSizeQueryResponse
        // which updates the cached size. We just need to check if it changed.
        _output.HandleSizeQueryResponse (response);

        // Check for size change after the response is processed
        CheckSizeChanged ();
    }
}
