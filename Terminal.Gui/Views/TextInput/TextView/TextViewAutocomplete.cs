namespace Terminal.Gui.Views;

/// <summary>
///     Renders an overlay on another view at a given point that allows selecting from a range of 'autocomplete'
///     options. An implementation on a TextView.
/// </summary>
public class TextViewAutocomplete : PopupAutocomplete
{
    /// <inheritdoc/>
    protected override void DeleteTextBackwards () { ((TextView)HostControl!).DeleteCharLeft (); }

    /// <inheritdoc/>
    protected override void InsertText (string accepted) { ((TextView)HostControl!).InsertText (accepted); }

    /// <inheritdoc/>
    protected override void SetCursorPosition (int column)
    {
        ((TextView)HostControl!).InsertionPoint =
            new (column, ((TextView)HostControl).CurrentRow);
    }
}
