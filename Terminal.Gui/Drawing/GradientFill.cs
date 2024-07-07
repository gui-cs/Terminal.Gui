namespace Terminal.Gui;

/// <summary>
/// Implementation of <see cref="IFill"/> that uses a color gradient (including
/// radial, diagonal etc).
/// </summary>
public class GradientFill : IFill
{
    private Dictionary<Point, Color> _map;

    /// <summary>
    /// Creates a new instance of the <see cref="GradientFill"/> class that can return
    /// color for any point in the given <paramref name="area"/> using the provided
    /// <paramref name="gradient"/> and <paramref name="direction"/>.
    /// </summary>
    /// <param name="area"></param>
    /// <param name="gradient"></param>
    /// <param name="direction"></param>
    public GradientFill (Rectangle area, Gradient gradient, GradientDirection direction)
    {
        _map = gradient.BuildCoordinateColorMapping (area.Height-1, area.Width-1, direction);
    }

    /// <summary>
    /// Returns the color to use for the given <paramref name="point"/> or Black if it
    /// lies outside of the prepared gradient area (see constructor).
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Color GetColor (Point point)
    {
        if (_map.TryGetValue (point, out var color))
        {
            return color;
        }
        return new Color (0, 0, 0); // Default to black if point not found
    }
}