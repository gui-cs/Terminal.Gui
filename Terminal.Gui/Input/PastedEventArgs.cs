namespace Terminal.Gui.Input;

/// <summary>
///     Event arguments for the <see cref="View.Pasted"/> event raised after the default
///     <see cref="Command.Paste"/> handler has consumed a paste. Observation only — handlers cannot
///     cancel or alter what has already been inserted.
/// </summary>
public class PastedEventArgs : EventArgs
{
    /// <summary>Initializes a new <see cref="PastedEventArgs"/>.</summary>
    /// <param name="text">The text that was inserted into the view.</param>
    public PastedEventArgs (string text) { Text = text; }

    /// <summary>The text that was inserted into the view (post-sanitization, post-<see cref="View.Pasting"/>).</summary>
    public string Text { get; }

    /// <inheritdoc/>
    public override string ToString () => $"Pasted ({Text.Length} chars)";
}
