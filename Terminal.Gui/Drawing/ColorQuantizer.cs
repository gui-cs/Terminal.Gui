using System.Collections.ObjectModel;
using ColorHelper;

namespace Terminal.Gui;

/// <summary>
/// Translates colors in an image into a Palette of up to 256 colors.
/// </summary>
public class ColorQuantizer
{
    /// <summary>
    /// Gets the current colors in the palette based on the last call to
    /// <see cref="BuildPalette"/>.
    /// </summary>
    public IReadOnlyCollection<Color> Palette { get; private set; } = new List<Color> ();

    /// <summary>
    /// Gets or sets the maximum number of colors to put into the <see cref="Palette"/>.
    /// Defaults to 256 (the maximum for sixel images).
    /// </summary>
    public int MaxColors { get; set; } = 256;

    /// <summary>
    /// Gets or sets the algorithm used to map novel colors into existing
    /// palette colors (closest match). Defaults to <see cref="CIE94ColorDistance"/>
    /// </summary>
    public IColorDistance DistanceAlgorithm { get; set; } = new CIE94ColorDistance ();

    /// <summary>
    /// Gets or sets the algorithm used to build the <see cref="Palette"/>.
    /// Defaults to <see cref="MedianCutPaletteBuilder"/>
    /// </summary>
    public IPaletteBuilder PaletteBuildingAlgorithm { get; set; } = new MedianCutPaletteBuilder ();

    public void BuildPalette (Color [,] pixels)
    {
        List<Color> allColors = new List<Color> ();
        int width = pixels.GetLength (0);
        int height = pixels.GetLength (1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                allColors.Add (pixels [x, y]);
            }
        }

        Palette = PaletteBuildingAlgorithm.BuildPalette(allColors,MaxColors);
    }

    public int GetNearestColor (Color toTranslate)
    {
        // Simple nearest color matching based on Euclidean distance in RGB space
        double minDistance = double.MaxValue;
        int nearestIndex = 0;

        for (var index = 0; index < Palette.Count; index++)
        {
            Color color = Palette.ElementAt(index);
            double distance = DistanceAlgorithm.CalculateDistance(color, toTranslate);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = index;
            }
        }

        return nearestIndex;
    }
}

public interface IPaletteBuilder
{
    List<Color> BuildPalette (List<Color> colors, int maxColors);
}

/// <summary>
/// Interface for algorithms that compute the relative distance between pairs of colors.
/// This is used for color matching to a limited palette, such as in Sixel rendering.
/// </summary>
public interface IColorDistance
{
    /// <summary>
    /// Computes a similarity metric between two <see cref="Color"/> instances.
    /// A larger value indicates more dissimilar colors, while a smaller value indicates more similar colors.
    /// The metric is internally consistent for the given algorithm.
    /// </summary>
    /// <param name="c1">The first color.</param>
    /// <param name="c2">The second color.</param>
    /// <returns>A numeric value representing the distance between the two colors.</returns>
    double CalculateDistance (Color c1, Color c2);
}

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
        x = (x > 0.008856) ? Math.Pow (x, 1.0 / 3.0) : (7.787 * x) + (16.0 / 116.0);
        y = (y > 0.008856) ? Math.Pow (y, 1.0 / 3.0) : (7.787 * y) + (16.0 / 116.0);
        z = (z > 0.008856) ? Math.Pow (z, 1.0 / 3.0) : (7.787 * z) + (16.0 / 116.0);

        // Calculate Lab values
        double l = (116.0 * y) - 16.0;
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


class MedianCutPaletteBuilder : IPaletteBuilder
{
    public List<Color> BuildPalette (List<Color> colors, int maxColors)
    {
        // Initial step: place all colors in one large box
        List<ColorBox> boxes = new List<ColorBox> { new ColorBox (colors) };

        // Keep splitting boxes until we have the desired number of colors
        while (boxes.Count < maxColors)
        {
            // Find the box with the largest range and split it
            ColorBox boxToSplit = FindBoxWithLargestRange (boxes);

            if (boxToSplit == null || boxToSplit.Colors.Count == 0)
            {
                break;
            }

            // Split the box into two smaller boxes
            var splitBoxes = SplitBox (boxToSplit);
            boxes.Remove (boxToSplit);
            boxes.AddRange (splitBoxes);
        }

        // Average the colors in each box to get the final palette
        return boxes.Select (box => box.GetAverageColor ()).ToList ();
    }

    // Find the box with the largest color range (R, G, or B)
    private ColorBox FindBoxWithLargestRange (List<ColorBox> boxes)
    {
        ColorBox largestRangeBox = null;
        int largestRange = 0;

        foreach (var box in boxes)
        {
            int range = box.GetColorRange ();
            if (range > largestRange)
            {
                largestRange = range;
                largestRangeBox = box;
            }
        }

        return largestRangeBox;
    }

    // Split a box at the median point in its largest color channel
    private List<ColorBox> SplitBox (ColorBox box)
    {
        List<ColorBox> result = new List<ColorBox> ();

        // Find the color channel with the largest range (R, G, or B)
        int channel = box.GetLargestChannel ();
        var sortedColors = box.Colors.OrderBy (c => GetColorChannelValue (c, channel)).ToList ();

        // Split the box at the median
        int medianIndex = sortedColors.Count / 2;

        var lowerHalf = sortedColors.Take (medianIndex).ToList ();
        var upperHalf = sortedColors.Skip (medianIndex).ToList ();

        result.Add (new ColorBox (lowerHalf));
        result.Add (new ColorBox (upperHalf));

        return result;
    }

    // Helper method to get the value of a color channel (R = 0, G = 1, B = 2)
    private static int GetColorChannelValue (Color color, int channel)
    {
        switch (channel)
        {
            case 0: return color.R;
            case 1: return color.G;
            case 2: return color.B;
            default: throw new ArgumentException ("Invalid channel index");
        }
    }

    // The ColorBox class to represent a subset of colors
    public class ColorBox
    {
        public List<Color> Colors { get; private set; }

        public ColorBox (List<Color> colors)
        {
            Colors = colors;
        }

        // Get the color channel with the largest range (0 = R, 1 = G, 2 = B)
        public int GetLargestChannel ()
        {
            int rRange = GetColorRangeForChannel (0);
            int gRange = GetColorRangeForChannel (1);
            int bRange = GetColorRangeForChannel (2);

            if (rRange >= gRange && rRange >= bRange)
            {
                return 0;
            }

            if (gRange >= rRange && gRange >= bRange)
            {
                return 1;
            }

            return 2;
        }

        // Get the range of colors for a given channel (0 = R, 1 = G, 2 = B)
        private int GetColorRangeForChannel (int channel)
        {
            int min = int.MaxValue, max = int.MinValue;

            foreach (var color in Colors)
            {
                int value = GetColorChannelValue (color, channel);
                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }
            }

            return max - min;
        }

        // Get the overall color range across all channels (for finding the box to split)
        public int GetColorRange ()
        {
            int rRange = GetColorRangeForChannel (0);
            int gRange = GetColorRangeForChannel (1);
            int bRange = GetColorRangeForChannel (2);

            return Math.Max (rRange, Math.Max (gRange, bRange));
        }

        // Calculate the average color in the box
        public Color GetAverageColor ()
        {
            int totalR = 0, totalG = 0, totalB = 0;

            foreach (var color in Colors)
            {
                totalR += color.R;
                totalG += color.G;
                totalB += color.B;
            }

            int count = Colors.Count;
            return new Color (totalR / count, totalG / count, totalB / count);
        }
    }
}