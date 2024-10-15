// This code is based on existing implementations of sixel algorithm in MIT licensed open source libraries
// node-sixel (Typescript) - https://github.com/jerch/node-sixel/tree/master/src
// Copyright (c) 2019, Joerg Breitbart @license MIT
// libsixel (C/C++) - https://github.com/saitoha/libsixel
// Copyright (c) 2014-2016 Hayaki Saito @license MIT

namespace Terminal.Gui;

/// <summary>
///     Encodes a images into the sixel console image output format.
/// </summary>
public class SixelEncoder
{
    /*

    A sixel is a column of 6 pixels - with a width of 1 pixel

    Column controlled by one sixel character:
      [ ]  - Bit 0 (top-most pixel)
      [ ]  - Bit 1
      [ ]  - Bit 2
      [ ]  - Bit 3
      [ ]  - Bit 4
      [ ]  - Bit 5 (bottom-most pixel)

   Special Characters
       The '-' acts like '\n'. It moves the drawing cursor
       to beginning of next line

       The '$' acts like the <Home> key.  It moves drawing
       cursor back to beginning of the current line
       e.g. to draw more color layers.

   */

    /// <summary>
    ///     Gets or sets the quantizer responsible for building a representative
    ///     limited color palette for images and for mapping novel colors in
    ///     images to their closest palette color
    /// </summary>
    public ColorQuantizer Quantizer { get; set; } = new ();

    /// <summary>
    ///     Encode the given bitmap into sixel encoding
    /// </summary>
    /// <param name="pixels"></param>
    /// <returns></returns>
    public string EncodeSixel (Color [,] pixels)
    {
        const string start = "\u001bP"; // Start sixel sequence

        string defaultRatios = AnyHasAlphaOfZero (pixels) ? "0;1;0" : "0;0;0"; // Defaults for aspect ratio and grid size
        const string completeStartSequence = "q"; // Signals beginning of sixel image data
        const string noScaling = "\"1;1;"; // no scaling factors (1x1);

        string fillArea = GetFillArea (pixels);

        string pallette = GetColorPalette (pixels);

        string pixelData = WriteSixel (pixels);

        const string terminator = "\u001b\\"; // End sixel sequence

        return start + defaultRatios + completeStartSequence + noScaling + fillArea + pallette + pixelData + terminator;
    }

    private string WriteSixel (Color [,] pixels)
    {
        var sb = new StringBuilder ();
        int height = pixels.GetLength (1);
        int width = pixels.GetLength (0);

        // Iterate over each 'row' of the image. Because each sixel write operation
        // outputs a screen area 6 pixels high (and 1+ across) we must process the image
        // 6 'y' units at once (1 band)
        for (var y = 0; y < height; y += 6)
        {
            sb.Append (ProcessBand (pixels, y, Math.Min (6, height - y), width));

            // Line separator between bands
            if (y + 6 < height) // Only add separator if not the last band
            {
                // This completes the drawing of the current line of sixel and
                // returns the 'cursor' to beginning next line, newly drawn sixel
                // after this will draw in the next 6 pixel high band (i.e. below).
                sb.Append ("-");
            }
        }

        return sb.ToString ();
    }

    private string ProcessBand (Color [,] pixels, int startY, int bandHeight, int width)
    {
        var last = new sbyte [Quantizer.Palette.Count + 1];
        var code = new byte [Quantizer.Palette.Count + 1];
        var accu = new ushort [Quantizer.Palette.Count + 1];
        var slots = new short [Quantizer.Palette.Count + 1];

        Array.Fill (last, (sbyte)-1);
        Array.Fill (accu, (ushort)1);
        Array.Fill (slots, (short)-1);

        List<int> usedColorIdx = new List<int> ();
        List<List<string>> targets = new List<List<string>> ();

        // Process columns within the band
        for (var x = 0; x < width; ++x)
        {
            Array.Clear (code, 0, usedColorIdx.Count);

            // Process each row in the 6-pixel high band
            for (var row = 0; row < bandHeight; ++row)
            {
                Color color = pixels [x, startY + row];

                int colorIndex = Quantizer.GetNearestColor (color);

                if (color.A == 0) // Skip fully transparent pixels
                {
                    continue;
                }

                if (slots [colorIndex] == -1)
                {
                    targets.Add (new ());

                    if (x > 0)
                    {
                        last [usedColorIdx.Count] = 0;
                        accu [usedColorIdx.Count] = (ushort)x;
                    }

                    slots [colorIndex] = (short)usedColorIdx.Count;
                    usedColorIdx.Add (colorIndex);
                }

                code [slots [colorIndex]] |= (byte)(1 << row); // Accumulate SIXEL data
            }

            // Handle transitions between columns
            for (var j = 0; j < usedColorIdx.Count; ++j)
            {
                if (code [j] == last [j])
                {
                    accu [j]++;
                }
                else
                {
                    if (last [j] != -1)
                    {
                        targets [j].Add (CodeToSixel (last [j], accu [j]));
                    }

                    last [j] = (sbyte)code [j];
                    accu [j] = 1;
                }
            }
        }

        // Process remaining data for this band
        for (var j = 0; j < usedColorIdx.Count; ++j)
        {
            if (last [j] != 0)
            {
                targets [j].Add (CodeToSixel (last [j], accu [j]));
            }
        }

        // Build the final output for this band
        var result = new StringBuilder ();

        for (var j = 0; j < usedColorIdx.Count; ++j)
        {
            result.Append ($"#{usedColorIdx [j]}{string.Join ("", targets [j])}$");
        }

        return result.ToString ();
    }

    private static string CodeToSixel (int code, int repeat)
    {
        var c = (char)(code + 63);

        if (repeat > 3)
        {
            return "!" + repeat + c;
        }

        if (repeat == 3)
        {
            return c.ToString () + c + c;
        }

        if (repeat == 2)
        {
            return c.ToString () + c;
        }

        return c.ToString ();
    }

    private string GetColorPalette (Color [,] pixels)
    {
        Quantizer.BuildPalette (pixels);

        var paletteSb = new StringBuilder ();

        for (var i = 0; i < Quantizer.Palette.Count; i++)
        {
            Color color = Quantizer.Palette.ElementAt (i);

            paletteSb.AppendFormat (
                                    "#{0};2;{1};{2};{3}",
                                    i,
                                    color.R * 100 / 255,
                                    color.G * 100 / 255,
                                    color.B * 100 / 255);
        }

        return paletteSb.ToString ();
    }

    private string GetFillArea (Color [,] pixels)
    {
        int widthInChars = pixels.GetLength (0);
        int heightInChars = pixels.GetLength (1);

        return $"{widthInChars};{heightInChars}";
    }

    private bool AnyHasAlphaOfZero (Color [,] pixels)
    {
        int width = pixels.GetLength (0);
        int height = pixels.GetLength (1);

        // Loop through each pixel in the 2D array
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                // Check if the alpha component (A) is 0
                if (pixels [x, y].A == 0)
                {
                    return true; // Found a pixel with A of 0
                }
            }
        }

        return false; // No pixel with A of 0 was found
    }
}
