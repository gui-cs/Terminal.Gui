using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

// Border Arrange Mode

public partial class Border
{
    private Arranger? _arranger;

    /// <summary>
    ///     INTERNAL: Gets the <see cref="Arranger"/> responsible for handling Arrange Mode for this <see cref="Border"/>.
    /// </summary>
    internal Arranger Arranger => _arranger ??= CreateArranger ();

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouseEvent)
    {
        return Arranger.HandleMouseEvent (mouseEvent);
    }

    private Arranger CreateArranger ()
    {
        var arranger = new Arranger (this);

        AddCommand (Command.Quit, () => _arranger?.ExitArrangeMode ());
        AddCommand (Command.Up, () => _arranger?.HandleArrangeModeUp ());
        AddCommand (Command.Down, () => _arranger?.HandleArrangeModeDown ());
        AddCommand (Command.Left, () => _arranger?.HandleArrangeModeLeft ());
        AddCommand (Command.Right, () => _arranger?.HandleArrangeModeRight ());
        AddCommand (Command.NextTabStop, () => _arranger?.HandleArrangeModeTab ());
        AddCommand (Command.PreviousTabStop, () => _arranger?.HandleArrangeModeBackTab ());

        return arranger;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        _arranger?.Dispose ();
        base.Dispose (disposing);
    }
}
