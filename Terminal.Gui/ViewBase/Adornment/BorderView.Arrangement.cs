namespace Terminal.Gui.ViewBase;

// BorderView Arrange Mode

public partial class BorderView
{
    private Arranger? _arranger;

    /// <summary>
    ///     INTERNAL: Gets the <see cref="Arranger"/> responsible for handling Arrange Mode for this <see cref="BorderView"/>.
    ///     The Arranger manages mouse hit-testing on border edges, drag operations for move/resize, and
    ///     keyboard-based arrangement via <c>Ctrl+F5</c>.
    /// </summary>
    /// <remarks>
    ///     See the <see href="https://gui-cs.github.io/Terminal.Gui/docs/arrangement.html">Arrangement Deep Dive</see>.
    /// </remarks>
    internal Arranger Arranger => _arranger ??= new Arranger (this);

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouseEvent) => Arranger.HandleMouseEvent (mouseEvent);

    private void DisposeArranger () => _arranger?.Dispose ();
}
