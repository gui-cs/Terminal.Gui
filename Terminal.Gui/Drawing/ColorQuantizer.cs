namespace Terminal.Gui;

/// <summary>
/// Translates colors in an image into a Palette of up to 256 colors.
/// </summary>
public class ColorQuantizer
{
    private Dictionary<Color, int> colorFrequency;
    public List<Color> Palette;
    private const int MaxColors = 256;

    public ColorQuantizer ()
    {
        colorFrequency = new Dictionary<Color, int> ();
        Palette = new List<Color> ();
    }

    public void BuildPalette (Color [,] pixels, IPaletteBuilder builder)
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

        Palette = builder.BuildPalette(allColors,MaxColors);
    }

    public int GetNearestColor (Color toTranslate, IColorDistance distanceAlgorithm)
    {
        // Simple nearest color matching based on Euclidean distance in RGB space
        double minDistance = double.MaxValue;
        int nearestIndex = 0;

        for (var index = 0; index < Palette.Count; index++)
        {
            Color color = Palette [index];
            double distance = distanceAlgorithm.CalculateDistance(color, toTranslate);

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