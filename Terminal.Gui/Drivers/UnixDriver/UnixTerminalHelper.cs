using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

internal static class UnixTerminalHelper
{
    private static bool _savedTermios;
    private static termios _origTermios;
    private static int _ttyFd = -1;

    public static void SaveTerminalState ()
    {
        if (_ttyFd == -1)
        {
            _ttyFd = open ("/dev/tty", O_RDWR);
        }

        if (_ttyFd != -1 && tcgetattr (_ttyFd, out _origTermios) == 0)
        {
            _savedTermios = true;
        }
        else
        {
            try
            {
                _ = close (_ttyFd);
            }
            catch
            {
                // Ignore any exceptions during close, as we're already in a cleanup phase
            }
            _ttyFd = -1;
        }
    }

    public static void RestoreTerminalState ()
    {
        if (_ttyFd != -1)
        {
            if (_savedTermios)
            {
                if (tcsetattr (_ttyFd, TCSANOW, ref _origTermios) != 0)
                {
                    // fallback to stty sane
                    RunSttySane ();
                }
            }
            else
            {
                // fallback to stty sane
                RunSttySane ();
            }

            // close the fd we opened earlier
            try
            {
                _ = close (_ttyFd);
            }
            catch
            {
                // Ignore any exceptions during close, as we're already in a cleanup phase
            }
            _ttyFd = -1;
            _savedTermios = false;
        }
        else
        {
            // fallback to stty sane
            RunSttySane ();
        }
    }

    private static void RunSttySane ()
    {
        try
        {
            var psi = new ProcessStartInfo ("/bin/sh", "-c \"stty sane < /dev/tty\"")
            {
                RedirectStandardOutput = false, RedirectStandardError = false, UseShellExecute = false, CreateNoWindow = true
            };
            Process.Start (psi)?.WaitForExit ();
        }
        catch
        {
            // Ignore any exceptions, as this is a best-effort attempt to restore terminal state
        }
    }

    // P/Invoke and types
    private const int O_RDWR = 2;
    private const int TCSANOW = 0;

    [StructLayout (LayoutKind.Sequential)]
    private struct termios
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
    private static extern int open (string path, int oflag);

    [DllImport ("libc", SetLastError = true)]
    private static extern int close (int fd);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcgetattr (int fd, out termios termios_p);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcsetattr (int fd, int optional_actions, ref termios termios_p);
}
