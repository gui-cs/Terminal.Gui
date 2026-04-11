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

    /// <summary>
    ///     The dynamic row offset applied to all cursor positioning during inline-mode rendering.
    ///     Starts at <see cref="InlineCursorRow"/> but decreases as the terminal scrolls to
    ///     accommodate growth of the inline region.
    /// </summary>
    public int InlineRowOffset { get; set; }

    /// <summary>
    ///     The current height of the inline content region (the view's rendered height).
    ///     Updated when the view grows. Used to position the cursor just below the inline
    ///     region on exit.
    /// </summary>
    public int InlineContentHeight { get; set; }
}
