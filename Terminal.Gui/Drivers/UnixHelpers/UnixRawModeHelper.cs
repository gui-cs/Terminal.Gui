using System.Runtime.InteropServices;
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for enabling Unix/Mac terminal raw mode using termios.
/// </summary>
/// <remarks>
///     Raw mode disables:
///     <list type="bullet">
///         <item>Line buffering (ICANON) - characters available immediately</item>
///         <item>Echo (ECHO) - typed characters don't appear on screen</item>
///         <item>Signal generation (ISIG) - Ctrl+C doesn't send SIGINT</item>
///         <item>Special character processing (IEXTEN, IXON, ICRNL, etc.)</item>
///     </list>
///     This allows the application to receive raw keyboard input and process all keys,
///     including control sequences and special keys as ANSI escape sequences.
/// </remarks>
internal sealed class UnixRawModeHelper : IDisposable
{
    #region P/Invoke Declarations

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

    // Terminal attribute flags
    private const int STDIN_FILENO = 0;
    private const int TCSANOW = 0;

    // Input flags
    private const uint BRKINT = 0x00000002; // Signal on break
    private const uint ICRNL = 0x00000100; // Map CR to NL
    private const uint INPCK = 0x00000010; // Enable parity checking
    private const uint ISTRIP = 0x00000020; // Strip 8th bit
    private const uint IXON = 0x00000400; // Enable XON/XOFF flow control

    // Output flags
    private const uint OPOST = 0x00000001; // Post-process output

    // Control flags
    private const uint CS8 = 0x00000030; // 8-bit characters

    // Local flags
    private const uint ECHO = 0x00000008; // Echo input
    private const uint ICANON = 0x00000100; // Canonical mode (line buffering)
    private const uint IEXTEN = 0x00008000; // Extended input processing
    private const uint ISIG = 0x00000001; // Generate signals

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcgetattr (int fd, out Termios termios);

    [DllImport ("libc", SetLastError = true)]
    private static extern int tcsetattr (int fd, int optional_actions, ref Termios termios);

    [DllImport ("libc", EntryPoint = "cfmakeraw", SetLastError = false)]
    private static extern void cfmakeraw_ref (ref Termios termios);

    #endregion

    private Termios _originalTermios;
    private bool _disposed;

    /// <summary>
    ///     Gets whether raw mode was successfully enabled.
    /// </summary>
    public bool IsRawModeEnabled { get; private set; }

    /// <summary>
    ///     Attempts to enable raw mode on the terminal.
    /// </summary>
    /// <returns>True if raw mode was enabled successfully; false otherwise.</returns>
    public bool TryEnable ()
    {
        if (IsRawModeEnabled)
        {
            return true;
        }

        // Only attempt on Unix-like platforms
        if (!PlatformDetection.IsUnixLike())
        {
            return false;
        }

        try
        {
            // Get current terminal attributes
            int result = tcgetattr (STDIN_FILENO, out _originalTermios);

            if (result != 0)
            {
                int errno = Marshal.GetLastWin32Error ();
                Logging.Warning ($"tcgetattr failed (errno={errno}). Cannot enable raw mode.");

                return false;
            }

            // Create modified attributes for raw mode
            Termios raw = _originalTermios;

            try
            {
                // Try using cfmakeraw if available (cleaner, platform-specific implementation)
                cfmakeraw_ref (ref raw);
            }
            catch (EntryPointNotFoundException)
            {
                // Manually configure raw mode if cfmakeraw not available
                // This is equivalent to cfmakeraw's behavior
                raw.c_iflag &= ~(BRKINT | ICRNL | INPCK | ISTRIP | IXON);
                raw.c_oflag &= ~OPOST;
                raw.c_cflag |= CS8;
                raw.c_lflag &= ~(ECHO | ICANON | IEXTEN | ISIG);
            }

            // Apply raw mode settings
            result = tcsetattr (STDIN_FILENO, TCSANOW, ref raw);

            if (result != 0)
            {
                int errno = Marshal.GetLastWin32Error ();
                Logging.Warning ($"tcsetattr failed (errno={errno}). Cannot enable raw mode.");

                return false;
            }

            IsRawModeEnabled = true;
            Logging.Information ("Unix raw mode enabled successfully.");

            return true;
        }
        catch (DllNotFoundException)
        {
            // libc not available - expected on non-Unix platforms
            return false;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to enable Unix raw mode: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Restores the terminal to its original state.
    /// </summary>
    public void Restore ()
    {
        if (!IsRawModeEnabled || _disposed)
        {
            return;
        }

        try
        {
            tcsetattr (STDIN_FILENO, TCSANOW, ref _originalTermios);
            IsRawModeEnabled = false;
            Logging.Information ("Unix terminal settings restored.");
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to restore Unix terminal settings: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        Restore ();
        _disposed = true;
    }
}
