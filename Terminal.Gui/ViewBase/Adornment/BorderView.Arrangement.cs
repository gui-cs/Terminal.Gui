namespace Terminal.Gui.ViewBase;

// BorderView Arrange Mode

public partial class BorderView
{
    private Arranger? _arranger;

    /// <summary>
    ///     INTERNAL: Gets the <see cref="Arranger"/> responsible for handling Arrange Mode for this <see cref="BorderView"/>.
    /// </summary>
    internal Arranger Arranger => _arranger ??= new Arranger (this);

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouseEvent) => Arranger.HandleMouseEvent (mouseEvent);

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        _arranger?.Dispose ();
        base.Dispose (disposing);
    }
}
