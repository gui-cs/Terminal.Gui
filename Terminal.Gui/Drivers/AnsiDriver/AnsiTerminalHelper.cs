using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

internal static class AnsiTerminalHelper
{
    public static bool IsAttachedToTerminal (out bool inputAttached, out bool outputAttached)
    {
        inputAttached = outputAttached = false;

        if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
        {
            const int STD_INPUT_HANDLE = -10;
            const int STD_OUTPUT_HANDLE = -11;
            nint inH = GetStdHandle (STD_INPUT_HANDLE);
            nint outH = GetStdHandle (STD_OUTPUT_HANDLE);

            inputAttached = inH != nint.Zero && GetConsoleMode (inH, out _);
            outputAttached = outH != nint.Zero && GetConsoleMode (outH, out _);

            return inputAttached && outputAttached;
        }
        const int STDIN_FILENO = 0;
        const int STDOUT_FILENO = 1;
        inputAttached = isatty (STDIN_FILENO) == 1;
        outputAttached = isatty (STDOUT_FILENO) == 1;

        return inputAttached && outputAttached;
    }

    public static void FlushNative (AnsiPlatform platform)
    {
        try
        {
            switch (platform)
            {
                case AnsiPlatform.UnixRaw:
                    FlushUnix ();

                    break;

                case AnsiPlatform.WindowsVT:
                    FlushWindows ();

                    break;
            }
        }
        catch
        {
            // ignore any exceptions during flush, as we don't want to crash the app if the flush fails in unit tests.
        }
    }

    /* Unix: wait until output has been transmitted to the terminal.
       Prefer tcdrain(STDOUT_FILENO). If it fails, fall back to fsync. */
    private static void FlushUnix ()
    {
        const int STDOUT_FILENO = 1;

        if (tcdrain (STDOUT_FILENO) == 0)
        {
            return;
        }

        // fallback
        try
        {
            fsync (STDOUT_FILENO);
        }
        catch
        { /* ignore */
        }
    }

    /* Windows: flush the stdout handle. */
    private static void FlushWindows ()
    {
        const int STD_OUTPUT_HANDLE = -11;
        nint h = GetStdHandle (STD_OUTPUT_HANDLE);

        if (h != nint.Zero && h != new nint (-1))
        {
            FlushFileBuffers (h); // returns false on failure
        }
    }

    // Unix
    [DllImport ("libc", SetLastError = true)]
    private static extern int isatty (int fd);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcdrain (int fd);

    [DllImport ("libc", SetLastError = true)]
    private static extern int fsync (int fd);

    // Windows
    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll", SetLastError = true)]
    [return: MarshalAs (UnmanagedType.Bool)]
    private static extern bool FlushFileBuffers (nint hFile);
}
