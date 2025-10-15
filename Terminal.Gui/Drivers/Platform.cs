using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

internal static class Platform
{
    private static int _suspendSignal;

    /// <summary>Suspends the process by sending SIGTSTP to itself</summary>
    /// <returns>True if the suspension was successful.</returns>
    public static bool Suspend ()
    {
        int signal = GetSuspendSignal ();

        if (signal == -1)
        {
            return false;
        }

        killpg (0, signal);

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

    [DllImport ("libc")]
    private static extern int killpg (int pgrp, int pid);

    [DllImport ("libc")]
    private static extern int uname (nint buf);
}