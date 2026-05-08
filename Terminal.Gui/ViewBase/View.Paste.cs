namespace Terminal.Gui.ViewBase;

public partial class View // Paste APIs
{
    /// <summary>
    ///     Called when bracketed-paste content is dispatched to this view (typically the focused
    ///     view). Raises the cancellable <see cref="OnPasted"/> / <see cref="Pasted"/> events; if the
    ///     paste is not handled, it bubbles up to <see cref="SuperView"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Override <see cref="OnPasted"/> in a subclass to provide default paste behavior (for
    ///         example, <see cref="TextField"/> inserts the pasted text). Subscribe to <see cref="Pasted"/>
    ///         to handle pastes externally.
    ///     </para>
    ///     <para>
    ///         Pastes are only delivered through this method on terminals that support bracketed paste
    ///         mode. On terminals that do not, pasted text is delivered character-by-character through
    ///         the normal keyboard pipeline.
    ///     </para>
    /// </remarks>
    /// <param name="args">Carries the pasted text and a cancellation flag.</param>
    /// <returns><see langword="true"/> if the paste was handled.</returns>
    public bool NewPasteEvent (PasteEventArgs args)
    {
        if (!Enabled)
        {
            return false;
        }

        if (OnPasted (args) || args.Handled)
        {
            return true;
        }

        Pasted?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        // Bubble to SuperView so a container can provide default paste handling for its children.
        if (SuperView is { } superView)
        {
            return superView.NewPasteEvent (args);
        }

        return false;
    }

    /// <summary>
    ///     Called before the <see cref="Pasted"/> event is raised. Override to provide default paste
    ///     handling (for example, inserting the pasted text into a text-input view). Set
    ///     <see cref="System.ComponentModel.HandledEventArgs.Handled"/> on <paramref name="args"/> or
    ///     return <see langword="true"/> to stop further processing.
    /// </summary>
    /// <param name="args">Carries the pasted text.</param>
    /// <returns><see langword="true"/> if the paste was handled.</returns>
    protected virtual bool OnPasted (PasteEventArgs args) => false;

    /// <summary>
    ///     Raised when bracketed-paste content is delivered to this view. Set
    ///     <see cref="System.ComponentModel.HandledEventArgs.Handled"/> to <see langword="true"/> to
    ///     stop the paste from being processed further (including bubbling to the SuperView).
    /// </summary>
    public event EventHandler<PasteEventArgs>? Pasted;
}
