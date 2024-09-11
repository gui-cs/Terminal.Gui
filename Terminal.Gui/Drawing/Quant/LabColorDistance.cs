using ColorHelper;

namespace Terminal.Gui.Drawing.Quant;

public abstract class LabColorDistance : IColorDistance
{
    // Reference white point for D65 illuminant (can be moved to constants)
    private const double RefX = 95.047;
    private const double RefY = 100.000;
    private const double RefZ = 108.883;

    // Conversion from RGB to Lab
    protected LabColor RgbToLab (Color c)
    {
        var xyz = ColorHelper.ColorConverter.RgbToXyz (new RGB (c.R, c.G, c.B));

        // Normalize XYZ values by reference white point
        double x = xyz.X / RefX;
        double y = xyz.Y / RefY;
        double z = xyz.Z / RefZ;

        // Apply the nonlinear transformation for Lab
        x = x > 0.008856 ? Math.Pow (x, 1.0 / 3.0) : 7.787 * x + 16.0 / 116.0;
        y = y > 0.008856 ? Math.Pow (y, 1.0 / 3.0) : 7.787 * y + 16.0 / 116.0;
        z = z > 0.008856 ? Math.Pow (z, 1.0 / 3.0) : 7.787 * z + 16.0 / 116.0;

        // Calculate Lab values
        double l = 116.0 * y - 16.0;
        double a = 500.0 * (x - y);
        double b = 200.0 * (y - z);

        return new LabColor (l, a, b);
    }

    // LabColor class encapsulating L, A, and B values
    protected class LabColor
    {
        public double L { get; }
        public double A { get; }
        public double B { get; }

        public LabColor (double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }
    }

    /// <inheritdoc />
    public abstract double CalculateDistance (Color c1, Color c2);
}
