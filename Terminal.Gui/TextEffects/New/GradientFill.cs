using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.TextEffects;

/// <summary>
/// Implementation of <see cref="IFill"/> that uses a color gradient (including
/// radial, diagonal etc).
/// </summary>
public class GradientFill : IFill
{
    private Dictionary<Point, Terminal.Gui.Color> _map;

    public GradientFill (Rectangle area, Gradient gradient, Gradient.Direction direction)
    {
        _map = 
            gradient.BuildCoordinateColorMapping (area.Height, area.Width, direction)
            .ToDictionary(
                (k)=> new Point(k.Key.Column,k.Key.Row),
                (v)=> new Terminal.Gui.Color (v.Value.R, v.Value.G, v.Value.B));
    }

    public Terminal.Gui.Color GetColor (Point point)
    {
        return _map [point];
    }
}
