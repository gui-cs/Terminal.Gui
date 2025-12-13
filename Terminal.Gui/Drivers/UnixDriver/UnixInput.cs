using System.Collections.Concurrent;
using System.Runtime.InteropServices;

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

    private const int STDIN_FILENO = 0;
    private readonly bool _terminalInitialized;

    [StructLayout (LayoutKind.Sequential)]
    private struct Pollfd
    {
        public int fd;
        public short events;
        public readonly short revents;
    }

    [Flags]
    private enum Condition : short
    {
        PollIn = 1,
        PollPri = 2,
        PollOut = 4,
        PollErr = 8,
        PollHup = 16,
        PollNval = 32
    }

    [DllImport ("libc", SetLastError = true)]
    private static extern int poll ([In] [Out] Pollfd [] ufds, uint nfds, int timeout);

    [DllImport ("libc", SetLastError = true)]
    private static extern int read (int fd, byte [] buf, int count);

    private const int STDOUT_FILENO = 1;

    [DllImport ("libc", SetLastError = true)]
    private static extern int write (int fd, byte [] buf, int count);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcflush (int fd, int queueSelector);

    private const int TCIFLUSH = 0;

    private readonly Pollfd []? _pollMap;

    public UnixInput ()
    {
        Logging.Information ($"Creating {nameof (UnixInput)}");

        try
        {
            _pollMap = new Pollfd [1];
            _pollMap [0].fd = STDIN_FILENO;
            _pollMap [0].events = (short)Condition.PollIn;

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
            Logging.Warning ($"UnixInput: libc not available: {ex.Message}. Running in degraded mode.");
            _terminalInitialized = false;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"UnixInput: Failed to initialize terminal: {ex.Message}. Running in degraded mode.");
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

        try
        {
            int n = poll (_pollMap, (uint)_pollMap.Length, 0);

            return n != 0;
        }
        catch (Exception ex)
        {
            Logging.Error ($"Error in Peek: {ex.Message}");

            return false;
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
            byte [] utf8 = Encoding.UTF8.GetBytes (text);
            write (STDOUT_FILENO, utf8, utf8.Length);
        }
        catch
        {
            // ignore exceptions during write
        }
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

        while (poll (_pollMap, (uint)_pollMap.Length, 0) != 0)
        {
            if ((_pollMap [0].revents & (int)Condition.PollIn) != 0)
            {
                var buf = new byte [256];
                int bytesRead = read (0, buf, buf.Length);
                string input = Encoding.UTF8.GetString (buf, 0, bytesRead);

                foreach (char ch in input)
                {
                    yield return ch;
                }
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
            Pollfd [] fds = new Pollfd [1];
            fds [0].fd = STDIN_FILENO;
            fds [0].events = (short)Condition.PollIn;
            var buf = new byte [256];

            while (poll (fds, 1, 0) > 0)
            {
                read (STDIN_FILENO, buf, buf.Length);
            }
        }
        catch
        {
            // ignore
        }
    }

    /// <inheritdoc/>
    public void AddInput (char input) { _testInput.Enqueue (input); }

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
            tcflush (STDIN_FILENO, TCIFLUSH);
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
