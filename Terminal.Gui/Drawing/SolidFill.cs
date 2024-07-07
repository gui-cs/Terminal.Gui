namespace Terminal.Gui.Drawing;


/// <summary>
/// <see cref="IFill"/> implementation that uses a solid color for all points
/// </summary>
public class SolidFill : IFill
{
    readonly Color _color;

    public SolidFill (Color color)
    {
        _color = color;
    }
    public Color GetColor (Point point)
    {
        return _color;
    }
}
