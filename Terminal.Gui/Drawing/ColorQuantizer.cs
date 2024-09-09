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

    public void BuildColorPalette (Color [,] pixels)
    {
        int width = pixels.GetLength (0);
        int height = pixels.GetLength (1);

        // Count the frequency of each color
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color color = pixels [x, y];
                if (colorFrequency.ContainsKey (color))
                {
                    colorFrequency [color]++;
                }
                else
                {
                    colorFrequency [color] = 1;
                }
            }
        }

        // Create a sorted list of colors based on frequency
        var sortedColors = colorFrequency.OrderByDescending (kvp => kvp.Value).ToList ();

        // Build the Palette with the most frequent colors up to MaxColors
        Palette = sortedColors.Take (MaxColors).Select (kvp => kvp.Key).ToList ();


    }

    public int GetNearestColor (Color toTranslate)
    {
        // Simple nearest color matching based on Euclidean distance in RGB space
        double minDistance = double.MaxValue;
        int nearestIndex = 0;

        for (var index = 0; index < Palette.Count; index++)
        {
            Color color = Palette [index];
            double distance = ColorDistance (color, toTranslate);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = index;
            }
        }

        return nearestIndex;
    }

    private double ColorDistance (Color c1, Color c2)
    {
        // Euclidean distance in RGB space
        int rDiff = c1.R - c2.R;
        int gDiff = c1.G - c2.G;
        int bDiff = c1.B - c2.B;
        return Math.Sqrt (rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
    }
}