using System.Reflection.Metadata;

namespace Terminal.Gui;

/// <summary>
/// Encodes a images into the sixel console image output format.
/// </summary>
public class SixelEncoder
{
    /// <summary>
    /// Encode the given bitmap into sixel encoding
    /// </summary>
    /// <param name="pixels"></param>
    /// <returns></returns>
    public string EncodeSixel (Color [,] pixels)
    {

        const string start = "\u001bP"; // Start sixel sequence

        const string defaultRatios = "0;0;0"; // Defaults for aspect ratio and grid size
        const string completeStartSequence = "q"; // Signals beginning of sixel image data
        const string noScaling = "\"1;1;"; // no scaling factors (1x1);

        string fillArea = GetFillArea (pixels);

        string pallette = GetColorPallette (pixels, out var dictionary);

        const string pixelData =
            "~~~~$-"
            + // First 6 rows of red pixels
            "~~~~$-";  // Next 6 rows of red pixels

        const string terminator = "\u001b\\"; // End sixel sequence

        return start + defaultRatios + completeStartSequence + noScaling + fillArea + pallette + pixelData + terminator;
    }

    private string GetColorPallette (Color [,] pixels, out ColorQuantizer quantizer)
    {
        quantizer = new ColorQuantizer ();
        quantizer.BuildColorPalette (pixels);


        // Color definitions in the format "#<index>;<type>;<R>;<G>;<B>" - For type the 2 means RGB.  The values range 0 to 100

        StringBuilder paletteSb = new StringBuilder ();

        for (int i = 0; i < quantizer.Palette.Count; i++)
        {
            var color = quantizer.Palette [i];
            paletteSb.AppendFormat ("#{0};2;{1};{2};{3}",
                                    i,
                                    color.R * 100 / 255,
                                    color.G * 100 / 255,
                                    color.B * 100 / 255);
        }

        return paletteSb.ToString ();
    }

    private string GetFillArea (Color [,] pixels)
    {
        int widthInChars = GetWidthInChars (pixels);
        int heightInChars = GetHeightInChars (pixels);

        return $"{widthInChars};{heightInChars}";
    }

    private int GetHeightInChars (Color [,] pixels)
    {
        // TODO
        return 2;
    }

    private int GetWidthInChars (Color [,] pixels)
    {
        return 3;
    }
}