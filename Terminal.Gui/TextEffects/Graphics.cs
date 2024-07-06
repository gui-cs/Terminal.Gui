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
    private readonly bool _loop;
    private readonly List<Color> _stops;
    private readonly List<int> _steps;

    public enum Direction
    {
        Vertical,
        Horizontal,
        Radial,
        Diagonal
    }

    public Gradient (IEnumerable<Color> stops, IEnumerable<int> steps, bool loop = false)
    {
        _stops = stops.ToList ();
        if (_stops.Count < 1)
            throw new ArgumentException ("At least one color stop must be provided.");

        _steps = steps.ToList ();
        if (_steps.Any (step => step < 1))
            throw new ArgumentException ("Steps must be greater than 0.");

        _loop = loop;
        Spectrum = GenerateGradient (_steps);
    }

    public Color GetColorAtFraction (double fraction)
    {
        if (fraction < 0 || fraction > 1)
            throw new ArgumentOutOfRangeException (nameof (fraction), "Fraction must be between 0 and 1.");
        int index = (int)(fraction * (Spectrum.Count - 1));
        return Spectrum [index];
    }

    private List<Color> GenerateGradient (IEnumerable<int> steps)
    {
        List<Color> gradient = new List<Color> ();
        if (_stops.Count == 1)
        {
            for (int i = 0; i < steps.Sum (); i++)
            {
                gradient.Add (_stops [0]);
            }
            return gradient;
        }

        if (_loop)
        {
            _stops.Add (_stops [0]);
        }

        var colorPairs = _stops.Zip (_stops.Skip (1), (start, end) => new { start, end });
        var stepsList = _steps.ToList ();

        foreach (var (colorPair, thesteps) in colorPairs.Zip (stepsList, (pair, step) => (pair, step)))
        {
            gradient.AddRange (InterpolateColors (colorPair.start, colorPair.end, thesteps));
        }

        return gradient;
    }

    private IEnumerable<Color> InterpolateColors (Color start, Color end, int steps)
    {
        for (int step = 0; step <= steps; step++)
        {
            double fraction = (double)step / steps;
            int r = (int)(start.R + fraction * (end.R - start.R));
            int g = (int)(start.G + fraction * (end.G - start.G));
            int b = (int)(start.B + fraction * (end.B - start.B));
            yield return Color.FromRgb (r, g, b);
        }
    }

    public Dictionary<Coord, Color> BuildCoordinateColorMapping (int maxRow, int maxColumn, Direction direction)
    {
        var gradientMapping = new Dictionary<Coord, Color> ();

        switch (direction)
        {
            case Direction.Vertical:
                for (int row = 0; row <= maxRow; row++)
                {
                    double fraction = maxRow == 0 ? 1.0 : (double)row / maxRow;
                    Color color = GetColorAtFraction (fraction);
                    for (int col = 0; col <= maxColumn; col++)
                    {
                        gradientMapping [new Coord (col, row)] = color;
                    }
                }
                break;

            case Direction.Horizontal:
                for (int col = 0; col <= maxColumn; col++)
                {
                    double fraction = maxColumn == 0 ? 1.0 : (double)col / maxColumn;
                    Color color = GetColorAtFraction (fraction);
                    for (int row = 0; row <= maxRow; row++)
                    {
                        gradientMapping [new Coord (col, row)] = color;
                    }
                }
                break;

            case Direction.Radial:
                for (int row = 0; row <= maxRow; row++)
                {
                    for (int col = 0; col <= maxColumn; col++)
                    {
                        double distanceFromCenter = FindNormalizedDistanceFromCenter (maxRow, maxColumn, new Coord (col, row));
                        Color color = GetColorAtFraction (distanceFromCenter);
                        gradientMapping [new Coord (col, row)] = color;
                    }
                }
                break;

            case Direction.Diagonal:
                for (int row = 0; row <= maxRow; row++)
                {
                    for (int col = 0; col <= maxColumn; col++)
                    {
                        double fraction = ((double)row * 2 + col) / ((maxRow * 2) + maxColumn);
                        Color color = GetColorAtFraction (fraction);
                        gradientMapping [new Coord (col, row)] = color;
                    }
                }
                break;
        }

        return gradientMapping;
    }

    private double FindNormalizedDistanceFromCenter (int maxRow, int maxColumn, Coord coord)
    {
        double centerX = maxColumn / 2.0;
        double centerY = maxRow / 2.0;
        double dx = coord.Column - centerX;
        double dy = coord.Row - centerY;
        double distance = Math.Sqrt (dx * dx + dy * dy);
        double maxDistance = Math.Sqrt (centerX * centerX + centerY * centerY);
        return distance / maxDistance;
    }
}