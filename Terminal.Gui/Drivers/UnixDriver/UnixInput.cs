using System.Collections.Concurrent;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo

namespace Terminal.Gui.Drivers;

internal class UnixInput : InputImpl<char>, IUnixInput, ITestableInput<char>
{
    // Queue for storing injected input for testing
    private readonly ConcurrentQueue<char> _testInput = new ();

    // Platform-specific raw mode helper
    private readonly UnixRawModeHelper _rawModeHelper = new ();

    private readonly bool _terminalInitialized;
    private readonly UnixIOHelper.Pollfd []? _pollMap;

    public UnixInput ()
    {
        //Logging.Information ($"Creating {nameof (UnixInput)}");

        try
        {
            // Set up poll map using shared helper
            _pollMap = UnixIOHelper.CreateStdinPollMap ();

            // Enable raw mode using the helper
            _terminalInitialized = _rawModeHelper.TryEnable ();

            if (_terminalInitialized)
            {
                WriteRaw (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
                WriteRaw (EscSeqUtils.CSI_HideCursor);

                // CSI_EnableMouseEvents enables
                // Mode 1003 (any-event) - Reports all mouse events including motion with/without buttons
                // Mode 1015 (URXVT) - UTF-8 coordinate encoding (fallback for older terminals)
                // Mode 1006 (SGR) - Modern decimal format with unlimited coordinates (preferred)
                WriteRaw (EscSeqUtils.CSI_EnableMouseEvents);
            }
        }
        catch (DllNotFoundException ex)
        {
            Logging.Information ($"UnixInput: libc not available: {ex.Message}. Running in degraded mode.");
            _terminalInitialized = false;
        }
        catch (Exception ex)
        {
            Logging.Information ($"UnixInput: Failed to initialize terminal: {ex.Message}. Running in degraded mode.");
            _terminalInitialized = false;
        }
    }

    /// <inheritdoc/>
    public override bool Peek ()
    {
        // Check test input first
        if (!_testInput.IsEmpty)
        {
            return true;
        }

        if (!_terminalInitialized || _pollMap is null)
        {
            return false;
        }

        return UnixIOHelper.IsInputAvailable (_pollMap);
    }

    private void WriteRaw (string text)
    {
        if (!_terminalInitialized)
        {
            return;
        }

        UnixIOHelper.TryWriteStdout (text);
    }

    /// <inheritdoc/>
    public override IEnumerable<char> Read ()
    {
        // Return test input first if available
        while (_testInput.TryDequeue (out char testChar))
        {
            yield return testChar;
        }

        if (!_terminalInitialized || _pollMap is null)
        {
            yield break;
        }

        while (UnixIOHelper.IsInputAvailable (_pollMap))
        {
            if ((_pollMap [0].revents & (int)UnixIOHelper.Condition.PollIn) == 0)
            {
                continue;
            }

            byte [] buf = new byte [256];

            if (!UnixIOHelper.TryReadStdin (buf, out int bytesRead) || bytesRead <= 0)
            {
                continue;
            }

            string input = Encoding.UTF8.GetString (buf, 0, bytesRead);

            foreach (char ch in input)
            {
                yield return ch;
            }
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
            if (_pollMap == null)
            {
                return;
            }

            var buf = new byte [256];

            while (UnixIOHelper.IsInputAvailable (_pollMap))
            {
                UnixIOHelper.TryReadStdin (buf, out _);
            }
        }
        catch
        {
            // ignore
        }
    }

    /// <inheritdoc/>
    public void InjectInput (char input) { _testInput.Enqueue (input); }

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
            UnixIOHelper.TryFlushStdin ();
            WriteRaw (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
            WriteRaw (EscSeqUtils.CSI_ShowCursor);

            // Restore terminal settings using the helper
            _rawModeHelper.Dispose ();
        }
        catch
        {
            // ignore exceptions during disposal
        }
    }
}
