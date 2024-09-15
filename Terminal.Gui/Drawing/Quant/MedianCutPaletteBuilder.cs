using Terminal.Gui;
using Color = Terminal.Gui.Color;

public class MedianCutPaletteBuilder : IPaletteBuilder
{
    private readonly IColorDistance _colorDistance;

    public MedianCutPaletteBuilder (IColorDistance colorDistance)
    {
        _colorDistance = colorDistance;
    }

    public List<Color> BuildPalette (List<Color> colors, int maxColors)
    {
        if (colors == null || colors.Count == 0 || maxColors <= 0)
        {
            return new List<Color> ();
        }

        return MedianCut (colors, maxColors);
    }

    private List<Color> MedianCut (List<Color> colors, int maxColors)
    {
        var cubes = new List<List<Color>> () { colors };

        // Recursively split color regions
        while (cubes.Count < maxColors)
        {
            bool added = false;
            cubes.Sort ((a, b) => Volume (a).CompareTo (Volume (b)));

            var largestCube = cubes.Last ();
            cubes.RemoveAt (cubes.Count - 1);

            // Check if the largest cube contains only one unique color
            if (IsSingleColorCube (largestCube))
            {
                // Add back and stop splitting this cube
                cubes.Add (largestCube);
                break;
            }

            var (cube1, cube2) = SplitCube (largestCube);

            if (cube1.Any ())
            {
                cubes.Add (cube1);
                added = true;
            }

            if (cube2.Any ())
            {
                cubes.Add (cube2);
                added = true;
            }

            // Break the loop if no new cubes were added
            if (!added)
            {
                break;
            }
        }

        // Calculate average color for each cube
        return cubes.Select (AverageColor).Distinct ().ToList ();
    }

    // Checks if all colors in the cube are the same
    private bool IsSingleColorCube (List<Color> cube)
    {
        var firstColor = cube.First ();
        return cube.All (c => c.R == firstColor.R && c.G == firstColor.G && c.B == firstColor.B);
    }

    // Splits the cube based on the largest color component range
    private (List<Color>, List<Color>) SplitCube (List<Color> cube)
    {
        var (component, range) = FindLargestRange (cube);

        // Sort by the largest color range component (either R, G, or B)
        cube.Sort ((c1, c2) => component switch
        {
            0 => c1.R.CompareTo (c2.R),
            1 => c1.G.CompareTo (c2.G),
            2 => c1.B.CompareTo (c2.B),
            _ => 0
        });

        var medianIndex = cube.Count / 2;
        var cube1 = cube.Take (medianIndex).ToList ();
        var cube2 = cube.Skip (medianIndex).ToList ();

        return (cube1, cube2);
    }

    private (int, int) FindLargestRange (List<Color> cube)
    {
        var minR = cube.Min (c => c.R);
        var maxR = cube.Max (c => c.R);
        var minG = cube.Min (c => c.G);
        var maxG = cube.Max (c => c.G);
        var minB = cube.Min (c => c.B);
        var maxB = cube.Max (c => c.B);

        var rangeR = maxR - minR;
        var rangeG = maxG - minG;
        var rangeB = maxB - minB;

        if (rangeR >= rangeG && rangeR >= rangeB) return (0, rangeR);
        if (rangeG >= rangeR && rangeG >= rangeB) return (1, rangeG);
        return (2, rangeB);
    }

    private Color AverageColor (List<Color> cube)
    {
        var avgR = (byte)(cube.Average (c => c.R));
        var avgG = (byte)(cube.Average (c => c.G));
        var avgB = (byte)(cube.Average (c => c.B));

        return new Color (avgR, avgG, avgB);
    }

    private int Volume (List<Color> cube)
    {
        if (cube == null || cube.Count == 0)
        {
            // Return a volume of 0 if the cube is empty or null
            return 0;
        }

        var minR = cube.Min (c => c.R);
        var maxR = cube.Max (c => c.R);
        var minG = cube.Min (c => c.G);
        var maxG = cube.Max (c => c.G);
        var minB = cube.Min (c => c.B);
        var maxB = cube.Max (c => c.B);

        return (maxR - minR) * (maxG - minG) * (maxB - minB);
    }
}
