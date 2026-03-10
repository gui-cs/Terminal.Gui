namespace Terminal.Gui.Drivers;

/// <summary>
///     Controls how the ANSI driver detects the terminal's window size.
/// </summary>
public enum SizeDetectionMode
{
    /// <summary>
    ///     Sends a <c>CSI 18t</c> ANSI escape-sequence query and parses the
    ///     <c>ESC [ 8 ; height ; width t</c> response. Works over SSH or any
    ///     ANSI-compatible terminal. This is the default.
    /// </summary>
    AnsiQuery,

    /// <summary>
    ///     Uses <c>ioctl(TIOCGWINSZ)</c> on Unix/macOS or the Console API on Windows.
    ///     Synchronous, immediate, and reliable. Useful when the ANSI query response
    ///     does not reflect the remote terminal size (e.g., over an SSH tunnel).
    /// </summary>
    Polling
}
