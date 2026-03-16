namespace Terminal.Gui.ViewBase;

/// <summary>
///     Obsolete compatibility alias for <see cref="AdornmentView"/>. Use <see cref="AdornmentView"/> directly.
/// </summary>
[Obsolete ("Use AdornmentView instead.", error: false)]
public class Adornment : AdornmentView
{
    /// <inheritdoc/>
    public Adornment () { }

    /// <inheritdoc/>
    public Adornment (View parent) : base (parent, null!) { }
}
