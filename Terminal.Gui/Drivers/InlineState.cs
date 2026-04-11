namespace Terminal.Gui.Drivers;

/// <summary>
///     Encapsulates the state needed for inline-mode rendering, where the application draws
///     into a portion of the primary terminal buffer rather than switching to the alternate screen.
/// </summary>
public struct InlineState
{
    /// <summary>
    ///     The terminal row (0-indexed) where the cursor was when the inline-mode app started.
    ///     This is a static initial value set once from the ANSI CPR response and never modified
    ///     after initial setup.
    /// </summary>
    public int InlineCursorRow { get; set; }
}
