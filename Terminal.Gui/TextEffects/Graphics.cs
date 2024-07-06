namespace Terminal.Gui.TextEffects;
using System;
using System.Collections.Generic;
using System.Linq;

public class Color
{
    public string RgbColor { get; private set; }
    public int? XtermColor { get; private set; }

    public Color (string rgbColor)
    {
        if (!ColorUtils.IsValidHexColor (rgbColor))
            throw new ArgumentException ("Invalid RGB hex color format.");

        RgbColor = rgbColor.StartsWith ("#") ? rgbColor.Substring (1).ToUpper () : rgbColor.ToUpper ();
        XtermColor = ColorUtils.HexToXterm (RgbColor);  // Convert RGB to XTerm-256
    }

    public Color (int xtermColor)
    {
        if (!ColorUtils.IsValidXtermColor (xtermColor))
            throw new ArgumentException ("Invalid XTerm-256 color code.");

        XtermColor = xtermColor;
        RgbColor = ColorUtils.XtermToHex (xtermColor); // Perform the actual conversion
    }
    public int R => Convert.ToInt32 (RgbColor.Substring (0, 2), 16);
    public int G => Convert.ToInt32 (RgbColor.Substring (2, 2), 16);
    public int B => Convert.ToInt32 (RgbColor.Substring (4, 2), 16);

    public (int R, int G, int B) GetRgbInts ()
    {
        return (
            Convert.ToInt32 (RgbColor.Substring (0, 2), 16),
            Convert.ToInt32 (RgbColor.Substring (2, 2), 16),
            Convert.ToInt32 (RgbColor.Substring (4, 2), 16)
        );
    }

    public override string ToString () => $"#{RgbColor}";

    public static Color FromRgb (int r, int g, int b)
    {
        // Validate the RGB values to ensure they are within the 0-255 range
        if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            throw new ArgumentOutOfRangeException ("RGB values must be between 0 and 255.");

        // Convert RGB values to a hexadecimal string
        string rgbColor = $"#{r:X2}{g:X2}{b:X2}";

        // Create and return a new Color instance using the hexadecimal string
        return new Color (rgbColor);
    }
}


public class Gradient
{
    public List<Color> Spectrum { get; private set; }

    // Constructor now accepts IEnumerable<int> for steps.
    public Gradient (IEnumerable<Color> stops, IEnumerable<int> steps, bool loop = false)
    {
        if (stops == null || !stops.Any () || stops.Count () < 2)
            throw new ArgumentException ("At least two color stops are required to create a gradient.");
        if (steps == null || !steps.Any ())
            throw new ArgumentException ("Steps are required to define the transitions between colors.");

        Spectrum = GenerateGradient (stops.ToList (), steps.ToList (), loop);
    }

    private List<Color> GenerateGradient (List<Color> stops, List<int> steps, bool loop)
    {
        List<Color> gradient = new List<Color> ();
        if (loop)
            stops.Add (stops [0]); // Loop the gradient back to the first color.

        for (int i = 0; i < stops.Count - 1; i++)
        {
            int currentSteps = i < steps.Count ? steps [i] : steps.Last ();
            gradient.AddRange (InterpolateColors (stops [i], stops [i + 1], currentSteps));
        }

        return gradient;
    }

    private IEnumerable<Color> InterpolateColors (Color start, Color end, int steps)
    {
        for (int step = 0; step <= steps; step++)
        {
            int r = Interpolate (start.R, end.R, steps, step);
            int g = Interpolate (start.G, end.G, steps, step);
            int b = Interpolate (start.B, end.B, steps, step);
            yield return Color.FromRgb (r, g, b);
        }
    }

    private int Interpolate (int start, int end, int steps, int currentStep)
    {
        return start + (int)((end - start) * (double)currentStep / steps);
    }

    public Color GetColorAtFraction (double fraction)
    {
        if (fraction < 0 || fraction > 1)
            throw new ArgumentOutOfRangeException (nameof (fraction), "Fraction must be between 0 and 1.");
        int index = (int)(fraction * (Spectrum.Count - 1));
        return Spectrum [index];
    }
}


