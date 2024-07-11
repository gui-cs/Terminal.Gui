using ColorHelper;

namespace Terminal.Gui;

/// <summary>
/// True color picker using HSL
/// </summary>
public class ColorPicker2 : View
{
    private readonly TextField tfHex;

    /// <summary>
    /// The color selected in the picker
    /// </summary>
    public Color Value { get; set; } = Color.Red;

    public ColorPicker2 ()
    {
        tfHex = new TextField ()
        {
            Y = 3,
            X = 4,
            Width = 8
        };

        // Revert to "g" when https://github.com/gui-cs/Terminal.Gui/issues/3603 is fixed
        tfHex.Text = Value.ToString ($"#{Value.R:X2}{Value.G:X2}{Value.B:X2}");

        Add (tfHex);
    }

    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        Move (0, 0);
        Driver.AddStr ("H:");
        DrawHue (2, 0, viewport.Width - 2);

        Driver.SetAttribute (GetNormalColor ());
        Move (0, 1);
        Driver.AddStr ("S:");
        DrawSaturation (2, 1, viewport.Width - 2);

        Driver.SetAttribute (GetNormalColor ());
        Move (0, 2);
        Driver.AddStr ("L:");
        DrawLightness (2, 2, viewport.Width - 2);

        Driver.SetAttribute (GetNormalColor ());
        Move (0, 3);
        Driver.AddStr ("Hex:");
    }

    private void DrawHue (int xOffset, int yOffset, int width)
    {
        // Define the colors of the rainbow
        var stops = new List<Color>
        {
            new Color(255, 0, 0),     // Red
            new Color(255, 165, 0),   // Orange
            new Color(255, 255, 0),   // Yellow
            new Color(0, 128, 0),     // Green
            new Color(0, 0, 255),     // Blue
            new Color(75, 0, 130),    // Indigo
            new Color(238, 130, 238)  // Violet
        };

        // Define the number of steps between each color
        var steps = new List<int>
        {
            20, // between Red and Orange
            20, // between Orange and Yellow
            20, // between Yellow and Green
            20, // between Green and Blue
            20, // between Blue and Indigo
            20  // between Indigo and Violet
        };

        // Create the gradient
        var rainbowGradient = new Gradient (stops, steps, true);

        var selectedHue = ColorHelper.ColorConverter.RgbToHsl (new RGB (Value.R, Value.G, Value.B)).H;

        int closestPosition = 0;
        double minDifference = double.MaxValue;

        for (int x = 0; x < width; x++)
        {
            double fraction = (double)x / (width - 1);
            Color color = rainbowGradient.GetColorAtFraction (fraction);
            double currentHue = ColorHelper.ColorConverter.RgbToHsl (new RGB (color.R, color.G, color.B)).H;

            double difference = Math.Abs (selectedHue - currentHue);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestPosition = x;
            }

            Application.Driver.SetAttribute (new Attribute (color, color));
            AddRune (x + xOffset, yOffset, new Rune ('█'));
        }

        // Draw the triangle at the closest position
        var triangleColor = rainbowGradient.GetColorAtFraction ((double)closestPosition / (width - 1));
        Move (closestPosition + xOffset, yOffset);
        Application.Driver.SetAttribute (new Attribute (Color.Black, triangleColor));
        AddRune (closestPosition + xOffset, yOffset, new Rune ('▲'));
    }

    private void DrawSaturation (int xOffset, int yOffset, int width)
    {
        var hsl = ColorHelper.ColorConverter.RgbToHsl (new RGB (Value.R, Value.G, Value.B));
        var selectedSaturation = hsl.S;

        int closestPosition = 0;
        double minDifference = double.MaxValue;
        Color closestColor = Value;

        for (int x = 0; x < width; x++)
        {
            double fraction = (double)x / (width - 1);
            var rgb = ColorHelper.ColorConverter.HslToRgb (new HSL (hsl.H, (byte)(fraction * 100), hsl.L));
            var color = new Color (rgb.R, rgb.G, rgb.B);
            double currentSaturation = fraction * 100;

            double difference = Math.Abs (selectedSaturation - currentSaturation);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestPosition = x;
                closestColor = color;
            }

            Application.Driver.SetAttribute (new Attribute (color, color));
            AddRune (x + xOffset, yOffset, new Rune ('█'));
        }

        // Draw the triangle at the closest position
        Move (closestPosition + xOffset, yOffset);
        Application.Driver.SetAttribute (new Attribute (ColorName.Black, closestColor));
        AddRune (closestPosition + xOffset, yOffset, new Rune ('▲'));
    }

    private void DrawLightness (int xOffset, int yOffset, int width)
    {
        var hsl = ColorHelper.ColorConverter.RgbToHsl (new RGB (Value.R, Value.G, Value.B));
        var selectedLightness = hsl.L;

        int closestPosition = 0;
        double minDifference = double.MaxValue;
        Color closestColor = Value;

        for (int x = 0; x < width; x++)
        {
            double fraction = (double)x / (width - 1);
            var rgb = ColorHelper.ColorConverter.HslToRgb (new HSL (hsl.H, hsl.S, (byte)(fraction * 100)));
            var color = new Color (rgb.R, rgb.G, rgb.B);
            double currentLightness = fraction * 100;

            double difference = Math.Abs (selectedLightness - currentLightness);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestPosition = x;
                closestColor = color;
            }

            Application.Driver.SetAttribute (new Attribute (color, color));
            AddRune (x + xOffset, yOffset, new Rune ('█'));
        }

        // Draw the triangle at the closest position
        Move (closestPosition + xOffset, yOffset);
        Application.Driver.SetAttribute (new Attribute (ColorName.Black, closestColor));
        AddRune (closestPosition + xOffset, yOffset, new Rune ('▲'));
    }

    private Attribute GetNormalColor ()
    {
        return new Attribute (Color.White, Color.Black);
    }
}
