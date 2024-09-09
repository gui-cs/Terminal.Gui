using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Terminal.Gui.Color;

namespace UnitTests.Drawing;

public class SixelEncoderTests
{

    [Fact]
    public void EncodeSixel_RedSquare12x12_ReturnsExpectedSixel ()
    {

        var expected = "\u001bP" + // Start sixel sequence
                            "0;0;0" + // Defaults for aspect ratio and grid size
                            "q" + // Signals beginning of sixel image data
                            "\"1;1;3;2" + // no scaling factors (1x1) and filling 3 runes horizontally and 2 vertically
                            "#0;2;100;0;0" + // Red color definition in the format "#<index>;<type>;<R>;<G>;<B>" - 2 means RGB.  The values range 0 to 100
                            "~~~~$-" + // First 6 rows of red pixels
                            "~~~~$-" + // Next 6 rows of red pixels
                            "\u001b\\"; // End sixel sequence

        // Arrange: Create a 12x12 bitmap filled with red
        var pixels = new Color [12, 12];
        for (int x = 0; x < 12; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                pixels [x, y] = new Color(255,0,0);
            }
        }

        // Act: Encode the image
        var encoder = new SixelEncoder (); // Assuming SixelEncoder is the class that contains the EncodeSixel method
        string result = encoder.EncodeSixel (pixels);


        Assert.Equal (expected, result);
    }

}
