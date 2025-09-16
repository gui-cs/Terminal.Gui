using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

internal class UnixInput : ConsoleInput<char>, IUnixInput
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

    [StructLayout (LayoutKind.Sequential)]
    private struct Pollfd
    {
        public int fd;
        public short events;
        public readonly short revents; // readonly signals "don't touch this in managed code"
    }

    /// <summary>Condition on which to wake up from file descriptor activity.  These match the Linux/BSD poll definitions.</summary>
    [Flags]
    private enum Condition : short
    {
        /// <summary>There is data to read</summary>
        PollIn = 1,

        /// <summary>There is urgent data to read</summary>
        PollPri = 2,

        /// <summary>Writing to the specified descriptor will not block</summary>
        PollOut = 4,

        /// <summary>Error condition on output</summary>
        PollErr = 8,

        /// <summary>Hang-up on output</summary>
        PollHup = 16,

        /// <summary>File descriptor is not open.</summary>
        PollNval = 32
    }

    [DllImport ("libc", SetLastError = true)]
    private static extern int poll ([In][Out] Pollfd [] ufds, uint nfds, int timeout);

    [DllImport ("libc", SetLastError = true)]
    private static extern int read (int fd, byte [] buf, int count);

    // File descriptor for stdout
    private const int STDOUT_FILENO = 1;

    [DllImport ("libc", SetLastError = true)]
    private static extern int write (int fd, byte [] buf, int count);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcflush (int fd, int queueSelector);

    private const int TCIFLUSH = 0;  // flush data received but not read

    private Pollfd [] _pollMap;

    public UnixInput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (UnixInput)}");

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        _pollMap = new Pollfd [1];
        _pollMap [0].fd = STDIN_FILENO; // stdin
        _pollMap [0].events = (short)Condition.PollIn;

        EnableRawModeAndTreatControlCAsInput ();

        //Enable alternative screen buffer.
        WriteRaw (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);

        //Set cursor key to application.
        WriteRaw (EscSeqUtils.CSI_HideCursor);

        WriteRaw (EscSeqUtils.CSI_EnableMouseEvents);
    }

    private void EnableRawModeAndTreatControlCAsInput ()
    {
        if (tcgetattr (STDIN_FILENO, out _original) != 0)
        {
            var e = Marshal.GetLastWin32Error ();
            throw new InvalidOperationException ($"tcgetattr failed errno={e} ({StrError (e)})");
        }

        var raw = _original;

        // Prefer cfmakeraw if available
        try
        {
            cfmakeraw_ref (ref raw);
        }
        catch (EntryPointNotFoundException)
        {
            // fallback: roughly cfmakeraw equivalent
            raw.c_iflag &= ~((uint)BRKINT | (uint)ICRNL | (uint)INPCK | (uint)ISTRIP | (uint)IXON);
            raw.c_oflag &= ~(uint)OPOST;
            raw.c_cflag |= (uint)CS8;
            raw.c_lflag &= ~((uint)ECHO | (uint)ICANON | (uint)IEXTEN | (uint)ISIG);
        }

        if (tcsetattr (STDIN_FILENO, TCSANOW, ref raw) != 0)
        {
            var e = Marshal.GetLastWin32Error ();
            throw new InvalidOperationException ($"tcsetattr failed errno={e} ({StrError (e)})");
        }
    }

    private string StrError (int err)
    {
        var p = strerror (err);
        return p == nint.Zero ? $"errno={err}" : Marshal.PtrToStringAnsi (p) ?? $"errno={err}";
    }

    /// <inheritdoc />
    protected override bool Peek ()
    {
        try
        {
            if (ConsoleDriver.RunningUnitTests)
            {
                return false;
            }

            int n = poll (_pollMap!, (uint)_pollMap!.Length, 0);

            if (n != 0)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            // Optionally log the exception
            Logging.Logger.LogError ($"Error in Peek: {ex.Message}");

            return false;
        }
    }
    private void WriteRaw (string text)
    {
        if (!ConsoleDriver.RunningUnitTests)
        {
            byte [] utf8 = Encoding.UTF8.GetBytes (text);
            // Write to stdout (fd 1)
            write (STDOUT_FILENO, utf8, utf8.Length);
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<char> Read ()
    {
        while (poll (_pollMap!, (uint)_pollMap!.Length, 0) != 0)
        {
            // Check if stdin has data
            if ((_pollMap [0].revents & (int)Condition.PollIn) != 0)
            {
                var buf = new byte [256];
                int bytesRead = read (0, buf, buf.Length); // Read from stdin
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
        if (!ConsoleDriver.RunningUnitTests)
        {
            var fds = new Pollfd [1];
            fds [0].fd = STDIN_FILENO;
            fds [0].events = (short)Condition.PollIn;
            var buf = new byte [256];
            while (poll (fds, 1, 0) > 0)
            {
                read (STDIN_FILENO, buf, buf.Length);
            }
        }
    }

    /// <inheritdoc />
    public override void Dispose ()
    {
        base.Dispose ();

        if (!ConsoleDriver.RunningUnitTests)
        {
            // Disable mouse events first
            WriteRaw (EscSeqUtils.CSI_DisableMouseEvents);

            // Drain any pending input already queued by the terminal
            FlushConsoleInput ();

            // Flush kernel input buffer
            tcflush (STDIN_FILENO, TCIFLUSH);

            //Disable alternative screen buffer.
            WriteRaw (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

            //Set cursor key to cursor.
            WriteRaw (EscSeqUtils.CSI_ShowCursor);

            // Restore terminal to original state
            tcsetattr (STDIN_FILENO, TCSANOW, ref _original);
        }
    }
}
