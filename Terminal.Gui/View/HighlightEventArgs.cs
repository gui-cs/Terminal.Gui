namespace Terminal.Gui;

/// <summary>
/// Event arguments for the <see cref="View.Highlight"/> event.
/// </summary>
public class HighlightEventArgs : CancelEventArgs<HighlightStyle>
{
    /// <inheritdoc />
    public HighlightEventArgs (HighlightStyle currentValue, HighlightStyle newValue) : base (currentValue, newValue) { }
}
