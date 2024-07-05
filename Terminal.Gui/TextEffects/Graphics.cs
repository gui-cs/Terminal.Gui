namespace Terminal.Gui.TextEffects;
using System;
using System.Collections.Generic;
using System.Linq;

public class Color
{
    public string RgbColor { get; }
    public int? XtermColor { get; }

    public Color (string rgbColor)
    {
        if (!IsValidHexColor (rgbColor))
            throw new ArgumentException ("Invalid RGB hex color format.");

        RgbColor = rgbColor.StartsWith ("#") ? rgbColor.Substring (1) : rgbColor;
        XtermColor = null; // Convert RGB to XTerm-256 here if necessary
    }

    public Color (int xtermColor)
    {
        XtermColor = xtermColor;
        RgbColor = ConvertXtermToHex (xtermColor); // Implement conversion logic
    }

    private bool IsValidHexColor (string color)
    {
        if (color.StartsWith ("#"))
        {
            color = color.Substring (1);
        }
        return color.Length == 6 && int.TryParse (color, System.Globalization.NumberStyles.HexNumber, null, out _);
    }

    private string ConvertXtermToHex (int xtermColor)
    {
        // Dummy conversion for the sake of example
        return "000000"; // Actual conversion logic needed
    }

    public (int, int, int) GetRgbInts ()
    {
        int r = Convert.ToInt32 (RgbColor.Substring (0, 2), 16);
        int g = Convert.ToInt32 (RgbColor.Substring (2, 2), 16);
        int b = Convert.ToInt32 (RgbColor.Substring (4, 2), 16);
        return (r, g, b);
    }

    public override string ToString ()
    {
        return $"#{RgbColor.ToUpper ()}";
    }

    public override bool Equals (object obj)
    {
        return obj is Color other && RgbColor == other.RgbColor;
    }

    public override int GetHashCode ()
    {
        return HashCode.Combine (RgbColor);
    }
}
public class Gradient
{
    public List<Color> Spectrum { get; private set; }

    public Gradient (IEnumerable<Color> stops, IEnumerable<int> steps, bool loop = false)
    {
        if (stops == null || stops.Count () < 2)
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
            gradient.AddRange (InterpolateColors (stops [i], stops [i + 1], i < steps.Count ? steps [i] : steps.Last ()));
        }

        return gradient;
    }

    private IEnumerable<Color> InterpolateColors (Color start, Color end, int steps)
    {
        for (int step = 0; step <= steps; step++)
        {
            int r = Interpolate (start.GetRgbInts ().Item1, end.GetRgbInts ().Item1, steps, step);
            int g = Interpolate (start.GetRgbInts ().Item2, end.GetRgbInts ().Item2, steps, step);
            int b = Interpolate (start.GetRgbInts ().Item3, end.GetRgbInts ().Item3, steps, step);
            yield return new Color ($"#{r:X2}{g:X2}{b:X2}");
        }
    }

    private int Interpolate (int start, int end, int steps, int currentStep)
    {
        return start + (end - start) * currentStep / steps;
    }

    public Color GetColorAtFraction (double fraction)
    {
        if (fraction < 0 || fraction > 1)
            throw new ArgumentOutOfRangeException (nameof (fraction), "Fraction must be between 0 and 1.");
        int index = (int)(fraction * (Spectrum.Count - 1));
        return Spectrum [index];
    }

    public IEnumerable<Color> GetRange (int startIndex, int count)
    {
        return Spectrum.Skip (startIndex).Take (count);
    }

    public override string ToString ()
    {
        return $"Gradient with {Spectrum.Count} colors.";
    }
}

