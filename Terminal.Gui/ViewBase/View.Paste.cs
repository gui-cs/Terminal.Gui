using System.Text;

namespace Terminal.Gui.ViewBase;

public partial class View // Paste APIs
{
    /// <summary>
    ///     Default handler for <see cref="Command.Paste"/>. Resolves the paste payload (from
    ///     <see cref="ICommandContext.Value"/> when bracketed paste delivered one, otherwise from
    ///     <see cref="IApplication.Clipboard"/>), sanitizes it via <see cref="OnSanitizingPaste"/>,
    ///     raises the cancellable <see cref="Pasting"/> event, calls <see cref="OnPaste"/> to insert
    ///     the text, then raises <see cref="Pasted"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Plain <see cref="View"/> instances return <see langword="false"/> from
    ///         <see cref="OnPaste"/> by default. Subclasses that accept text (for example
    ///         <see cref="TextField"/> and <see cref="TextView"/>) override
    ///         <see cref="OnPaste"/> to perform the insertion.
    ///     </para>
    ///     <para>
    ///         Bracketed-paste payloads carry the raw bytes delivered by the terminal between
    ///         <c>ESC[200~</c> and <c>ESC[201~</c>. Keyboard-driven pastes (<c>Ctrl+V</c>) have no
    ///         payload and fall through to the clipboard. Both paths share this handler, so any
    ///         sanitization or event subscription works for both.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the paste was consumed (sanitized text inserted, or cancelled
    ///     by a subscriber); <see langword="false"/> if nothing was pasted.
    /// </returns>
    private bool? DefaultPasteHandler (ICommandContext? ctx)
    {
        if (!Enabled)
        {
            return false;
        }

        string? payload = ctx?.Value as string ?? App?.Clipboard?.GetClipboardData ();

        if (string.IsNullOrEmpty (payload))
        {
            return false;
        }

        string sanitized = OnSanitizingPaste (payload);

        if (string.IsNullOrEmpty (sanitized))
        {
            return false;
        }

        PastingEventArgs pasting = new (sanitized);
        Pasting?.Invoke (this, pasting);

        if (pasting.Handled)
        {
            return true;
        }

        if (string.IsNullOrEmpty (pasting.Text))
        {
            return false;
        }

        if (!OnPaste (pasting.Text))
        {
            return false;
        }

        Pasted?.Invoke (this, new (pasting.Text));

        return true;
    }

    /// <summary>
    ///     Override to filter or transform raw paste payloads before they are inserted into the
    ///     view. The default implementation strips C0/C1 control characters (including ESC) but
    ///     preserves tab, line feed, and carriage return — matching Windows Terminal's
    ///     <c>FilterStringForPaste</c> baseline.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="TextField"/> overrides this to take only the first line and drop tab/CR/LF.
    ///         <see cref="TextView"/> overrides this to normalize <c>\r\n</c> / <c>\r</c> to <c>\n</c>.
    ///     </para>
    /// </remarks>
    /// <param name="raw">The raw payload, either from the terminal or the clipboard.</param>
    /// <returns>The sanitized text that will be passed to <see cref="OnPaste"/>.</returns>
    protected virtual string OnSanitizingPaste (string raw) => StripControlCharsExceptTabAndNewline (raw);

    /// <summary>
    ///     Override to insert sanitized paste text into the view. The default returns
    ///     <see langword="false"/> because a plain <see cref="View"/> has no text model. Text-input
    ///     views (<see cref="TextField"/>, <see cref="TextView"/>) override this to perform the
    ///     insertion.
    /// </summary>
    /// <param name="text">The sanitized text to insert. Never <see langword="null"/> or empty.</param>
    /// <returns><see langword="true"/> if the view consumed the paste.</returns>
    protected virtual bool OnPaste (string text) => false;

    /// <summary>
    ///     Raised by the default <see cref="Command.Paste"/> handler after sanitization but before
    ///     insertion. Subscribers may rewrite <see cref="PastingEventArgs.Text"/> or set
    ///     <see cref="System.ComponentModel.HandledEventArgs.Handled"/> to cancel.
    /// </summary>
    public event EventHandler<PastingEventArgs>? Pasting;

    /// <summary>
    ///     Raised by the default <see cref="Command.Paste"/> handler after <see cref="OnPaste"/>
    ///     consumes a paste. Observation only — the text has already been inserted.
    /// </summary>
    public event EventHandler<PastedEventArgs>? Pasted;

    private static string StripControlCharsExceptTabAndNewline (string text)
    {
        StringBuilder sb = new (text.Length);

        foreach (char c in text)
        {
            if (c == '\t' || c == '\n' || c == '\r' || (c >= 0x20 && c < 0x7F) || c >= 0xA0)
            {
                sb.Append (c);
            }
        }

        return sb.ToString ();
    }
}
