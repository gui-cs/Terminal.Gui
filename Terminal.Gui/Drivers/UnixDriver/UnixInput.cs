using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
// ReSharper disable CommentTypo

namespace Terminal.Gui.Drivers;

internal class UnixInput : InputImpl<char>, IUnixInput
{
    private const int STDIN_FILENO = 0;

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

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcgetattr (int fd, out Termios termios);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcsetattr (int fd, int optional_actions, ref Termios termios);

    // try cfmakeraw (glibc and macOS usually export it)
    [DllImport ("libc", EntryPoint = "cfmakeraw", SetLastError = false)]
    private static extern void cfmakeraw_ref (ref Termios termios);

    [DllImport ("libc", SetLastError = true)]
    private static extern nint strerror (int err);

    private const int TCSANOW = 0;

    private const ulong BRKINT = 0x00000002;
    private const ulong ICRNL = 0x00000100;
    private const ulong INPCK = 0x00000010;
    private const ulong ISTRIP = 0x00000020;
    private const ulong IXON = 0x00000400;

    private const ulong OPOST = 0x00000001;

    private const ulong ECHO = 0x00000008;
    private const ulong ICANON = 0x00000100;
    private const ulong IEXTEN = 0x00008000;
    private const ulong ISIG = 0x00000001;

    private const ulong CS8 = 0x00000030;

    private Termios _original;
    private bool _terminalInitialized;

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

    private Pollfd []? _pollMap;

    public UnixInput ()
    {
        Logging.Information ($"Creating {nameof (UnixInput)}");

        try
        {
            _pollMap = new Pollfd [1];
            _pollMap [0].fd = STDIN_FILENO;
            _pollMap [0].events = (short)Condition.PollIn;

            EnableRawModeAndTreatControlCAsInput ();

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

    private void EnableRawModeAndTreatControlCAsInput ()
    {
        try
        {
            int result = tcgetattr (STDIN_FILENO, out _original);

            if (result != 0)
            {
                int e = Marshal.GetLastWin32Error ();
                Logging.Warning ($"tcgetattr failed errno={e} ({StrError (e)}). Running without TTY support.");
                return;
            }

            Termios raw = _original;

            try
            {
                cfmakeraw_ref (ref raw);
            }
            catch (EntryPointNotFoundException)
            {
                raw.c_iflag &= ~((uint)BRKINT | (uint)ICRNL | (uint)INPCK | (uint)ISTRIP | (uint)IXON);
                raw.c_oflag &= ~(uint)OPOST;
                raw.c_cflag |= (uint)CS8;
                raw.c_lflag &= ~((uint)ECHO | (uint)ICANON | (uint)IEXTEN | (uint)ISIG);
            }

            result = tcsetattr (STDIN_FILENO, TCSANOW, ref raw);

            if (result != 0)
            {
                int e = Marshal.GetLastWin32Error ();
                Logging.Warning ($"tcsetattr failed errno={e} ({StrError (e)}). Running without TTY support.");
                return;
            }

            _terminalInitialized = true;
        }
        catch (DllNotFoundException)
        {
            throw; // Re-throw to be caught by constructor
        }
    }

    private string StrError (int err)
    {
        try
        {
            nint p = strerror (err);
            return p == nint.Zero ? $"errno={err}" : Marshal.PtrToStringAnsi (p) ?? $"errno={err}";
        }
        catch
        {
            return $"errno={err}";
        }
    }

    /// <inheritdoc/>
    public override bool Peek ()
    {
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
            tcsetattr (STDIN_FILENO, TCSANOW, ref _original);
        }
        catch
        {
            // ignore exceptions during disposal
        }
    }
}
