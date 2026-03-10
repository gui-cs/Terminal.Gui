namespace Terminal.Gui.Drivers;

/// <summary>
///     Controls how the ANSI driver detects the terminal's window size.
/// </summary>
public enum SizeDetectionMode
{
    /// <summary>
    ///     Uses <c>ioctl(TIOCGWINSZ)</c> on Unix/macOS or the Console API on Windows.
    ///     Synchronous, immediate, and reliable. This is the default.
    /// </summary>
    Polling,

    /// <summary>
    ///     Sends a <c>CSI 18t</c> ANSI escape-sequence query and parses the
    ///     <c>ESC [ 8 ; height ; width t</c> response.  Works over SSH or any
    ///     ANSI-compatible terminal but is asynchronous and throttled to one
    ///     query per 500 ms.
    /// </summary>
    AnsiQuery
}
