namespace Terminal.Gui.Drawing.Quant;

/// <summary>
/// Calculates the distance between two colors using Euclidean distance in 3D RGB space.
/// This measures the straight-line distance between the two points representing the colors.
/// </summary>
public class EuclideanColorDistance : IColorDistance
{
    public double CalculateDistance (Color c1, Color c2)
    {
        int rDiff = c1.R - c2.R;
        int gDiff = c1.G - c2.G;
        int bDiff = c1.B - c2.B;
        return Math.Sqrt (rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
    }
}
