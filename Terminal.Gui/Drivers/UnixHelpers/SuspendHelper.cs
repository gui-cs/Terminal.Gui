using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

internal static class SuspendHelper
{
    private static int _suspendSignal;

    /// <summary>Suspends the process by sending SIGTSTP to the process group.</summary>
    /// <returns>True if the suspension was successful.</returns>
    public static bool Suspend ()
    {
        int signal = GetSuspendSignal ();

        Logging.Information ($"SuspendHelper.Suspend: signal={signal}");

        if (signal == -1)
        {
            Logging.Warning ("SuspendHelper.Suspend: No suspend signal for this platform");

            return false;
        }

        Logging.Information ($"SuspendHelper.Suspend: Calling killpg(0, {signal}) [SIGTSTP]...");
        int result = killpg (0, signal);
        int errno = Marshal.GetLastWin32Error ();
        Logging.Information ($"SuspendHelper.Suspend: killpg returned {result}, errno={errno}");

        return true;
    }

    private static int GetSuspendSignal ()
    {
        if (_suspendSignal != 0)
        {
            return _suspendSignal;
        }

        nint buf = Marshal.AllocHGlobal (8192);

        if (uname (buf) != 0)
        {
            Marshal.FreeHGlobal (buf);
            _suspendSignal = -1;

            return _suspendSignal;
        }

        try
        {
            switch (Marshal.PtrToStringAnsi (buf))
            {
                case "Darwin":
                case "DragonFly":
                case "FreeBSD":
                case "NetBSD":
                case "OpenBSD":
                    _suspendSignal = 18;

                    break;

                case "Linux":
                    // TODO: should fetch the machine name and
                    // if it is MIPS return 24
                    _suspendSignal = 20;

                    break;

                case "Solaris":
                    _suspendSignal = 24;

                    break;

                default:
                    _suspendSignal = -1;

                    break;
            }

            return _suspendSignal;
        }
        finally
        {
            Marshal.FreeHGlobal (buf);
        }
    }

    [DllImport ("libc", SetLastError = true)]
    private static extern int killpg (int pgrp, int sig);

    [DllImport ("libc")]
    private static extern int uname (nint buf);
}
