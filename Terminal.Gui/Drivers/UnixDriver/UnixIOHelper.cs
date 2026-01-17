using System.Runtime.InteropServices;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo

namespace Terminal.Gui.Drivers;

/// <summary>
///     Shared helper class for Unix I/O syscalls used by both UnixDriver and AnsiDriver.
///     Provides a centralized location for P/Invoke declarations and common I/O operations.
/// </summary>
internal static class UnixIOHelper
{
    #region File Descriptors

    /// <summary>Standard input file descriptor</summary>
    public const int STDIN_FILENO = 0;

    /// <summary>Standard output file descriptor</summary>
    public const int STDOUT_FILENO = 1;

    #endregion

    #region Poll Structures and Flags

    /// <summary>
    ///     File descriptor structure used by poll() syscall.
    /// </summary>
    [StructLayout (LayoutKind.Sequential)]
    public struct Pollfd
    {
        /// <summary>File descriptor to poll</summary>
        public int fd;

        /// <summary>Events to watch for (input)</summary>
        public short events;

        /// <summary>Events that occurred (output)</summary>
        public readonly short revents;
    }

    /// <summary>
    ///     Poll event conditions/flags.
    /// </summary>
    [Flags]
    public enum Condition : short
    {
        /// <summary>Data available to read</summary>
        PollIn = 1,

        /// <summary>Priority data available</summary>
        PollPri = 2,

        /// <summary>Ready for output</summary>
        PollOut = 4,

        /// <summary>Error condition</summary>
        PollErr = 8,

        /// <summary>Hang up</summary>
        PollHup = 16,

        /// <summary>Invalid request</summary>
        PollNval = 32
    }

    #endregion

    #region P/Invoke Declarations

    /// <summary>
    ///     Poll file descriptors for I/O readiness.
    /// </summary>
    /// <param name="ufds">Array of file descriptors to poll</param>
    /// <param name="nfds">Number of file descriptors</param>
    /// <param name="timeout">Timeout in milliseconds (0 = non-blocking, -1 = infinite)</param>
    /// <returns>Number of file descriptors with events, or -1 on error</returns>
    [DllImport ("libc", SetLastError = true)]
    public static extern int poll ([In][Out] Pollfd [] ufds, uint nfds, int timeout);

    /// <summary>
    ///     Read bytes from a file descriptor.
    /// </summary>
    /// <param name="fd">File descriptor to read from</param>
    /// <param name="buf">Buffer to read into</param>
    /// <param name="count">Maximum number of bytes to read</param>
    /// <returns>Number of bytes read, 0 on EOF, -1 on error</returns>
    [DllImport ("libc", SetLastError = true)]
    public static extern int read (int fd, byte [] buf, int count);

    /// <summary>
    ///     Write bytes to a file descriptor.
    /// </summary>
    /// <param name="fd">File descriptor to write to</param>
    /// <param name="buf">Buffer containing data to write</param>
    /// <param name="count">Number of bytes to write</param>
    /// <returns>Number of bytes written, or -1 on error</returns>
    [DllImport ("libc", SetLastError = true)]
    public static extern int write (int fd, byte [] buf, int count);

    /// <summary>
    ///     Flush (discard) data in the terminal input or output queue.
    /// </summary>
    /// <param name="fd">File descriptor</param>
    /// <param name="queueSelector">Which queue to flush (TCIFLUSH, TCOFLUSH, TCIOFLUSH)</param>
    /// <returns>0 on success, -1 on error</returns>
    [DllImport ("libc", SetLastError = true)]
    public static extern int tcflush (int fd, int queueSelector);

    /// <summary>
    ///     Duplicate a file descriptor.
    /// </summary>
    /// <param name="fd">File descriptor to duplicate</param>
    /// <returns>New file descriptor, or -1 on error</returns>
    [DllImport ("libc", SetLastError = true)]
    public static extern int dup (int fd);

    #endregion

    #region Terminal Queue Selectors

    /// <summary>Flush input queue</summary>
    public const int TCIFLUSH = 0;

    /// <summary>Flush output queue</summary>
    public const int TCOFLUSH = 1;

    /// <summary>Flush both input and output queues</summary>
    public const int TCIOFLUSH = 2;

    #endregion

    #region ioctl Structures and Constants

    /// <summary>
    ///     Window size structure used by ioctl(TIOCGWINSZ).
    /// </summary>
    [StructLayout (LayoutKind.Sequential)]
    public struct WinSize
    {
        /// <summary>Number of rows (characters)</summary>
        public ushort ws_row;

        /// <summary>Number of columns (characters)</summary>
        public ushort ws_col;

        /// <summary>Width in pixels</summary>
        public ushort ws_xpixel;

        /// <summary>Height in pixels</summary>
        public ushort ws_ypixel;
    }

    /// <summary>
    ///     Get window/terminal size using ioctl.
    ///     Platform-specific constant (different on Darwin/BSD vs Linux).
    /// </summary>
    public static readonly uint TIOCGWINSZ =
        RuntimeInformation.IsOSPlatform (OSPlatform.OSX) || RuntimeInformation.IsOSPlatform (OSPlatform.FreeBSD)
            ? 0x40087468u // Darwin/BSD
            : 0x5413u; // Linux

    /// <summary>
    ///     I/O control operations on file descriptors.
    /// </summary>
    /// <param name="fd">File descriptor</param>
    /// <param name="request">Request code (e.g., TIOCGWINSZ)</param>
    /// <param name="ws">Window size structure (output)</param>
    /// <returns>0 on success, -1 on error</returns>
    [DllImport ("libc", SetLastError = true)]
    public static extern int ioctl (int fd, uint request, out WinSize ws);

    /// <summary>
    /// ioctl definition for Darwin/FreeBSD on ARM64.
    /// See https://github.com/dotnet/runtime/issues/48796#issuecomment-3695794860.
    /// </summary>
    /// <param name="fd">File descriptor</param>
    /// <param name="request">Request code (e.g., TIOCGWINSZ)</param>
    /// <param name="r3">placeholder to pass <paramref name="ws"/> on stack</param>
    /// <param name="r4">placeholder to pass <paramref name="ws"/> on stack</param>
    /// <param name="r5">placeholder to pass <paramref name="ws"/> on stack</param>
    /// <param name="r6">placeholder to pass <paramref name="ws"/> on stack</param>
    /// <param name="r7">placeholder to pass <paramref name="ws"/> on stack</param>
    /// <param name="r8">placeholder to pass <paramref name="ws"/> on stack</param>
    /// <param name="ws">Window size structure (output)</param>
    /// <returns>0 on success, -1 on error</returns>
    [DllImport ("libc", EntryPoint = "ioctl", SetLastError = true)]
    public static extern int ioctl_arm64 (int fd, ulong request, nint r3, nint r4, nint r5, nint r6, nint r7, nint r8, out WinSize ws);

    #endregion

    #region Helper Methods

    /// <summary>
    ///     Checks if input is available on stdin using poll().
    /// </summary>
    /// <param name="pollMap">Pre-initialized poll map for stdin</param>
    /// <param name="timeoutMs">Timeout in milliseconds (0 = non-blocking)</param>
    /// <returns>True if input is available, false otherwise</returns>
    public static bool IsInputAvailable (Pollfd [] pollMap, int timeoutMs = 0)
    {
        try
        {
            int n = poll (pollMap, (uint)pollMap.Length, timeoutMs);

            return n > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Reads bytes from stdin.
    /// </summary>
    /// <param name="buffer">Buffer to read into</param>
    /// <param name="bytesRead">Number of bytes actually read</param>
    /// <returns>True if read was successful, false otherwise</returns>
    public static bool TryReadStdin (byte [] buffer, out int bytesRead)
    {
        try
        {
            bytesRead = read (STDIN_FILENO, buffer, buffer.Length);

            return bytesRead >= 0;
        }
        catch
        {
            bytesRead = 0;

            return false;
        }
    }

    /// <summary>
    ///     Writes bytes to stdout.
    /// </summary>
    /// <param name="buffer">Buffer containing data to write</param>
    /// <returns>True if write was successful, false otherwise</returns>
    public static bool TryWriteStdout (byte [] buffer)
    {
        try
        {
            int written = write (STDOUT_FILENO, buffer, buffer.Length);

            return written >= 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Writes a UTF-8 string to stdout.
    /// </summary>
    /// <param name="text">Text to write</param>
    /// <returns>True if write was successful, false otherwise</returns>
    public static bool TryWriteStdout (string text)
    {
        try
        {
            byte [] utf8 = Encoding.UTF8.GetBytes (text);

            return TryWriteStdout (utf8);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Flushes the stdin input queue.
    /// </summary>
    /// <returns>True if flush was successful, false otherwise</returns>
    public static bool TryFlushStdin ()
    {
        try
        {
            return tcflush (STDIN_FILENO, TCIFLUSH) == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Gets the terminal size using ioctl.
    /// </summary>
    /// <param name="size">Output size (width, height)</param>
    /// <returns>True if size was retrieved successfully, false otherwise</returns>
    public static bool TryGetTerminalSize (out Size size)
    {
        try
        {
            int ioctlResult = 0;
            WinSize ws;
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64 &&
              (RuntimeInformation.IsOSPlatform (OSPlatform.OSX) || RuntimeInformation.IsOSPlatform (OSPlatform.FreeBSD)))
            {
                ioctlResult = ioctl_arm64 (STDOUT_FILENO, TIOCGWINSZ, 0, 0, 0, 0, 0, 0, out ws);
            }
            else
            {
                ioctlResult = ioctl (STDOUT_FILENO, TIOCGWINSZ, out ws);
            }

            if (ioctlResult == 0)
            {
                if (ws.ws_col > 0 && ws.ws_row > 0)
                {
                    size = new (ws.ws_col, ws.ws_row);

                    return true;
                }
            }
        }
        catch
        {
            // ignore
        }

        size = new (80, 25); // fallback

        return false;
    }

    /// <summary>
    ///     Creates a poll map for monitoring stdin.
    /// </summary>
    /// <returns>Initialized Pollfd array</returns>
    public static Pollfd [] CreateStdinPollMap ()
    {
        Pollfd [] pollMap = new Pollfd [1];
        pollMap [0].fd = STDIN_FILENO;
        pollMap [0].events = (short)Condition.PollIn;

        return pollMap;
    }

    #endregion
}
