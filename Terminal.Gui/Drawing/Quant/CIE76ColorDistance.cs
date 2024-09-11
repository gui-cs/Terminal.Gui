namespace Terminal.Gui.Drawing.Quant;

/// <summary>
/// This is the simplest method to measure color difference in the CIE Lab color space. The Euclidean distance in Lab
/// space is more aligned with human perception than RGB space, as Lab attempts to model how humans perceive color differences.
/// </summary>
public class CIE76ColorDistance : LabColorDistance
{
    public override double CalculateDistance (Color c1, Color c2)
    {
        var lab1 = RgbToLab (c1);
        var lab2 = RgbToLab (c2);

        // Euclidean distance in Lab color space
        return Math.Sqrt (Math.Pow (lab1.L - lab2.L, 2) + Math.Pow (lab1.A - lab2.A, 2) + Math.Pow (lab1.B - lab2.B, 2));
    }
}
