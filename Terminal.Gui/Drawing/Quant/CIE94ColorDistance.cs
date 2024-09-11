namespace Terminal.Gui.Drawing.Quant;

/// <summary>
/// CIE94 improves on CIE76 by introducing adjustments for chroma (color intensity) and lightness.
/// This algorithm considers human visual perception more accurately by scaling differences in lightness and chroma.
/// It is better but slower than <see cref="CIE76ColorDistance"/>.
/// </summary>
public class CIE94ColorDistance : LabColorDistance
{
    // Constants for CIE94 formula (can be modified for different use cases like textiles or graphics)
    private const double kL = 1.0;
    private const double kC = 1.0;
    private const double kH = 1.0;

    public override double CalculateDistance (Color first, Color second)
    {
        var lab1 = RgbToLab (first);
        var lab2 = RgbToLab (second);

        // Delta L, A, B
        double deltaL = lab1.L - lab2.L;
        double deltaA = lab1.A - lab2.A;
        double deltaB = lab1.B - lab2.B;

        // Chroma values for both colors
        double c1 = Math.Sqrt (lab1.A * lab1.A + lab1.B * lab1.B);
        double c2 = Math.Sqrt (lab2.A * lab2.A + lab2.B * lab2.B);
        double deltaC = c1 - c2;

        // Delta H (calculated indirectly)
        double deltaH = Math.Sqrt (Math.Pow (deltaA, 2) + Math.Pow (deltaB, 2) - Math.Pow (deltaC, 2));

        // Scaling factors
        double sL = 1.0;
        double sC = 1.0 + 0.045 * c1;
        double sH = 1.0 + 0.015 * c1;

        // CIE94 color difference formula
        return Math.Sqrt (
                          Math.Pow (deltaL / (kL * sL), 2) +
                          Math.Pow (deltaC / (kC * sC), 2) +
                          Math.Pow (deltaH / (kH * sH), 2)
                         );
    }
}
