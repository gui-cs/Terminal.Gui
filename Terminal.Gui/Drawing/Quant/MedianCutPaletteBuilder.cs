namespace Terminal.Gui;

public class MedianCutPaletteBuilder : IPaletteBuilder
{
    public List<Color> BuildPalette (List<Color> colors, int maxColors)
    {
        // Initial step: place all colors in one large box
        List<ColorBox> boxes = new List<ColorBox> { new ColorBox (colors) };

        // Keep splitting boxes until we have the desired number of colors
        while (boxes.Count < maxColors)
        {
            // Find the box with the largest brightness range and split it
            ColorBox boxToSplit = FindBoxWithLargestRange (boxes);

            if (boxToSplit == null || boxToSplit.Colors.Count == 0)
            {
                break;
            }

            // Split the box into two smaller boxes, based on luminance
            var splitBoxes = SplitBoxByLuminance (boxToSplit);
            boxes.Remove (boxToSplit);
            boxes.AddRange (splitBoxes);
        }

        // Average the colors in each box to get the final palette
        return boxes.Select (box => box.GetWeightedAverageColor ()).ToList ();
    }

    // Find the box with the largest brightness range (based on luminance)
    private ColorBox FindBoxWithLargestRange (List<ColorBox> boxes)
    {
        ColorBox largestRangeBox = null;
        double largestRange = 0;

        foreach (var box in boxes)
        {
            double range = box.GetBrightnessRange ();
            if (range > largestRange)
            {
                largestRange = range;
                largestRangeBox = box;
            }
        }

        return largestRangeBox;
    }

    // Split a box at the median point based on brightness (luminance)
    private List<ColorBox> SplitBoxByLuminance (ColorBox box)
    {
        var sortedColors = box.Colors.OrderBy (c => GetBrightness (c)).ToList ();

        // Split the box at the median
        int medianIndex = sortedColors.Count / 2;

        var lowerHalf = sortedColors.Take (medianIndex).ToList ();
        var upperHalf = sortedColors.Skip (medianIndex).ToList ();

        return new List<ColorBox>
        {
            new ColorBox(lowerHalf),
            new ColorBox(upperHalf)
        };
    }

    // Calculate the brightness (luminance) of a color
    private static double GetBrightness (Color color)
    {
        // Luminance formula (standard)
        return 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
    }

    // The ColorBox class to represent a subset of colors
    public class ColorBox
    {
        public List<Color> Colors { get; private set; }

        public ColorBox (List<Color> colors)
        {
            Colors = colors;
        }

        // Get the range of brightness (luminance) in this box
        public double GetBrightnessRange ()
        {
            double minBrightness = double.MaxValue, maxBrightness = double.MinValue;

            foreach (var color in Colors)
            {
                double brightness = GetBrightness (color);
                if (brightness < minBrightness)
                {
                    minBrightness = brightness;
                }

                if (brightness > maxBrightness)
                {
                    maxBrightness = brightness;
                }
            }

            return maxBrightness - minBrightness;
        }

        // Calculate the average color in the box, weighted by brightness (darker colors have more weight)
        public Color GetWeightedAverageColor ()
        {
            double totalR = 0, totalG = 0, totalB = 0;
            double totalWeight = 0;

            foreach (var color in Colors)
            {
                double brightness = GetBrightness (color);
                double weight = 1.0 - brightness / 255.0; // Darker colors get more weight

                totalR += color.R * weight;
                totalG += color.G * weight;
                totalB += color.B * weight;
                totalWeight += weight;
            }

            // Normalize by the total weight
            totalR /= totalWeight;
            totalG /= totalWeight;
            totalB /= totalWeight;

            return new Color ((int)totalR, (int)totalG, (int)totalB);
        }

        // Calculate brightness (luminance) of a color
        private static double GetBrightness (Color color)
        {
            return 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
        }
    }
}