namespace Terminal.Gui;

/// <summary>
///     Describes the current state of an <see cref="IAnsiResponseParser"/>
/// </summary>
public enum AnsiResponseParserState
{
    /// <summary>
    ///     Parser is reading normal input e.g. keys typed by user.
    /// </summary>
    Normal,

    /// <summary>
    ///     Parser has encountered an Esc and is waiting to see if next
    ///     key(s) continue to form an Ansi escape sequence (typically '[' but
    ///     also other characters e.g. O for SS3).
    /// </summary>
    ExpectingEscapeSequence,

    /// <summary>
    ///     Parser has encountered Esc[ and considers that it is in the process
    ///     of reading an ANSI sequence.
    /// </summary>
    InResponse
}
