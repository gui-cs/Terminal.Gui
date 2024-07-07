namespace Terminal.Gui.TextEffects;


/// <summary>
/// <see cref="IFill"/> implementation that uses a solid color for all points
/// </summary>
public class SolidFill : IFill
{
    readonly Terminal.Gui.Color _color;

    public SolidFill (Terminal.Gui.Color color)
    {
        _color = color;
    }
    public Gui.Color GetColor (Point point)
    {
        return _color;
    }
}
