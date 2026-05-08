using System.ComponentModel;

namespace Terminal.Gui.Input;

/// <summary>
///     Event arguments for paste events delivered by the terminal's bracketed paste mode.
///     Set <see cref="HandledEventArgs.Handled"/> to <see langword="true"/> to stop further processing.
/// </summary>
/// <remarks>
///     <para>
///         Bracketed paste delivers the entire pasted payload as a single string, distinct from
///         keyboard input. Subscribers can choose to insert the text, validate or sanitize it, or
///         ignore the paste entirely.
///     </para>
/// </remarks>
public class PasteEventArgs : HandledEventArgs
{
    /// <summary>
    ///     Initializes a new <see cref="PasteEventArgs"/>.
    /// </summary>
    /// <param name="text">The pasted text with bracketing markers stripped.</param>
    public PasteEventArgs (string text) { Text = text; }

    /// <summary>
    ///     The pasted text. Bracketing markers (<c>ESC[200~</c> / <c>ESC[201~</c>) are stripped by
    ///     the parser and are never present in this string.
    /// </summary>
    public string Text { get; }

    /// <inheritdoc/>
    public override string ToString () => $"Paste ({Text.Length} chars)";
}
