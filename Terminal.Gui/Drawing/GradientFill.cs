namespace Terminal.Gui;

/// <summary>
/// Implementation of <see cref="IFill"/> that uses a color gradient (including
/// radial, diagonal etc).
/// </summary>
public class GradientFill : IFill
{
    private Dictionary<Point, Color> _map;

    public GradientFill (Rectangle area, Gradient gradient, Gradient.Direction direction)
    {
        _map = gradient.BuildCoordinateColorMapping (area.Height, area.Width, direction);
    }

    public Color GetColor (Point point)
    {
        if (_map.TryGetValue (point, out var color))
        {
            return color;
        }
        return new Color (0, 0, 0); // Default to black if point not found
    }
}