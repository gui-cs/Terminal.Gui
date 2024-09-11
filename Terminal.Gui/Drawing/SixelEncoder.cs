using Terminal.Gui.Drawing.Quant;

namespace Terminal.Gui;

/// <summary>
/// Encodes a images into the sixel console image output format.
/// </summary>
public class SixelEncoder
{
    /// <summary>
    /// Gets or sets the quantizer responsible for building a representative
    /// limited color palette for images and for mapping novel colors in
    /// images to their closest palette color
    /// </summary>
    public ColorQuantizer Quantizer { get; set; } = new ();

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

        string pallette = GetColorPallette (pixels );

        string pixelData = WriteSixel (pixels);

        const string terminator = "\u001b\\"; // End sixel sequence

        return start + defaultRatios + completeStartSequence + noScaling + fillArea + pallette + pixelData + terminator;
    }


    /*
        A sixel is a column of 6 pixels - with a width of 1 pixel

     Column controlled by one sixel character:
       [ ]  - Bit 0 (top-most pixel)
       [ ]  - Bit 1
       [ ]  - Bit 2
       [ ]  - Bit 3
       [ ]  - Bit 4
       [ ]  - Bit 5 (bottom-most pixel)
    */

    private string WriteSixel (Color [,] pixels)
    {
        StringBuilder sb = new StringBuilder ();
        int height = pixels.GetLength (1);
        int width = pixels.GetLength (0);
        int n = 1; // Used for checking when to add the line terminator

        // Iterate over each row of the image
        for (int y = 0; y < height; y++)
        {
            int p = y * width;
            Color cachedColor = pixels [0, y];
            int cachedColorIndex = Quantizer.GetNearestColor (cachedColor );
            int count = 1;
            int c = -1;

            // Iterate through each column in the row
            for (int x = 0; x < width; x++)
            {
                Color color = pixels [x, y];
                int colorIndex = Quantizer.GetNearestColor (color);

                if (colorIndex == cachedColorIndex)
                {
                    count++;
                }
                else
                {
                    // Output the cached color first
                    if (cachedColorIndex == -1)
                    {
                        c = 0x3f; // Key color or transparent
                    }
                    else
                    {
                        c = 0x3f + n;
                        sb.AppendFormat ("#{0}", cachedColorIndex);
                    }

                    // If count is less than 3, we simply repeat the character
                    if (count < 3)
                    {
                        sb.Append ((char)c, count);
                    }
                    else
                    {
                        // RLE if count is greater than 3
                        sb.AppendFormat ("!{0}{1}", count, (char)c);
                    }

                    // Reset for the new color
                    count = 1;
                    cachedColorIndex = colorIndex;
                }
            }

            // Handle the last run of the color
            if (c != -1 && count > 1)
            {
                if (cachedColorIndex == -1)
                {
                    c = 0x3f; // Key color
                }
                else
                {
                    sb.AppendFormat ("#{0}", cachedColorIndex);
                }

                if (count < 3)
                {
                    sb.Append ((char)c, count);
                }
                else
                {
                    sb.AppendFormat ("!{0}{1}", count, (char)c);
                }
            }

            // Line terminator or separator depending on `n`
            if (n == 32)
            {
                /*
                 2. Line Separator (-):
                   
                   The line separator instructs the sixel renderer to move to the next row of sixels.
                   After a -, the renderer will start a new row from the leftmost column. This marks the end of one line of sixel data and starts a new line.
                   This ensures that the sixel data drawn after the separator appears below the previous row rather than overprinting it.
               
                   Use case: When you want to start drawing a new line of sixels (e.g., after completing a row of sixel columns).
                */

                n = 1;
                sb.Append ("-"); // Write sixel line separator
            }
            else
            {
                /*
                 *1. Line Terminator ($):
                   
                   The line terminator instructs the sixel renderer to return to the start of the current row but allows subsequent sixel characters to be overprinted on the same row.
                   This is used when you are working with multiple color layers or want to continue drawing in the same row but with a different color.
                   The $ allows you to overwrite sixel characters in the same vertical position by using different colors, effectively allowing you to combine colors on a per-sixel basis.
                   
                   Use case: When you need to draw multiple colors within the same vertical slice of 6 pixels.
                 */

                n <<= 1;
                sb.Append ("$"); // Write line terminator
            }
        }

        return sb.ToString ();
    }



    private string GetColorPallette (Color [,] pixels)
    {
        Quantizer.BuildPalette (pixels);


        // Color definitions in the format "#<index>;<type>;<R>;<G>;<B>" - For type the 2 means RGB.  The values range 0 to 100

        StringBuilder paletteSb = new StringBuilder ();

        for (int i = 0; i < Quantizer.Palette.Count; i++)
        {
            var color = Quantizer.Palette.ElementAt (i);
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
        // Height in pixels is equal to the number of rows in the pixel array
        int height = pixels.GetLength (1);

        // Each SIXEL character represents 6 pixels vertically
        return (height + 5) / 6; // Equivalent to ceiling(height / 6)
    }
    private int GetWidthInChars (Color [,] pixels)
    {
        // Width in pixels is equal to the number of columns in the pixel array
        return pixels.GetLength (0);
    }
}