using System.Diagnostics;
using System.Runtime.InteropServices;
using Trace = Terminal.Gui.Tracing.Trace;

namespace Terminal.Gui.Drivers;

internal static class UnixTerminalHelper
{
    private static bool _savedTermios;
    private static Termios _origTermios;
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

    public static void Suspend (IOutput output)
    {
        if (PlatformDetection.IsWindows ())
        {
            return;
        }

        try
        {
            // Disable mouse events to prevent mouse events from being sent to the application while it is suspended.
            output.Write (EscSeqUtils.CSI_DisableMouseEvents);

            // Check if we have a real console first
            if (DriverImpl.IsRunningInTest)
            {
                Trace.Lifecycle (nameof (AnsiInput), "Init", "Console is running unit tests. Running in degraded mode.");

                return;
            }

            // Save terminal state before suspending
            SaveTerminalState ();

            // Disable alternative screen buffer and show cursor
            output.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

            //Set cursor key to cursor.
            output.Write (EscSeqUtils.CSI_ShowCursor);

            if (!SuspendHelper.Suspend ())
            {
                return;
            }

            // Restore terminal state after resuming
            RestoreTerminalState ();

            //Enable alternative screen buffer.
            output.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (UnixTerminalHelper), "Suspend", $"Error suspending terminal: {ex.Message}");
        }
        finally
        {
            // Enable mouse events to allow mouse events to be sent to the application when it is resumed.
            output.Write (EscSeqUtils.CSI_EnableMouseEvents);
        }
    }

    // P/Invoke and types
    // ReSharper disable IdentifierTypo
    private const int O_RDWR = 2;
    private const int TCSANOW = 0;

    [StructLayout (LayoutKind.Sequential)]
    private struct Termios
    {
#pragma warning disable IDE1006 // Naming Styles
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;

        [MarshalAs (UnmanagedType.ByValArray, SizeConst = 32)]
        public byte [] c_cc;

        public uint c_ispeed;
        public uint c_ospeed;
#pragma warning restore IDE1006 // Naming Styles
    }

    [DllImport ("libc", SetLastError = true)]
    private static extern int open (string path, int oflag);

    [DllImport ("libc", SetLastError = true)]
    private static extern int close (int fd);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcgetattr (int fd, out Termios termios_p);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcsetattr (int fd, int optional_actions, ref Termios termios_p);
}
