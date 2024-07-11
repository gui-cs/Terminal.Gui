using ColorHelper;
using static Terminal.Gui.SpinnerStyle;

namespace Terminal.Gui;

/// <summary>
/// True color picker using HSL
/// </summary>
public class ColorPicker2 : View
{
    private readonly TextField tfHex;

    private Color _value = Color.Red;

    /// <summary>
    /// The color selected in the picker
    /// </summary>
    public Color Value
    {
        get => _value;
        set => _value = value;
    }

    public ColorPicker2 ()
    {
        var hb = new HueBar ()
        {
            Text = "H:",
            X = 0,
            Y = 0,
            Height = 1,
            Width = Dim.Fill (),
            Value = Value
        };
        hb.ColorChanged += SubBarChangedColor;

        var sb = new SaturationBar ()
        {
            Text = "S:",
            Y = 1,
            Height = 1,
            Width = Dim.Fill (),
            Value = Value
        };
        sb.ColorChanged += SubBarChangedColor;

        var lb = new LightnessBar ()
        {
            Text = "L:",
            Y = 2,
            Height = 1,
            Width = Dim.Fill (),
            Value = Value
        };
        lb.ColorChanged += SubBarChangedColor;

        tfHex = new TextField ()
        {
            Y = 3,
            X = 4,
            Width = 8
        };

        // Revert to "g" when https://github.com/gui-cs/Terminal.Gui/issues/3603 is fixed
        tfHex.Text = Value.ToString ($"#{Value.R:X2}{Value.G:X2}{Value.B:X2}");

        Add (hb);
        Add (sb);
        Add (lb);
        Add (tfHex);
    }

    private void SubBarChangedColor (object sender, ColorEventArgs e)
    {
        _value = e.Color;

        // Revert to "g" when https://github.com/gui-cs/Terminal.Gui/issues/3603 is fixed
        tfHex.Text = e.Color.ToString ($"#{Value.R:X2}{Value.G:X2}{Value.B:X2}");
    }
}

public abstract class ColorBar : View
{
    private Color _value;
    public Color Value
    {
        get => _value;
        set
        {
            this.OnColorChanged (value, _value);
            _value = value;
        }
    }

    /// <summary>Fired when a color is picked.</summary>
    public event EventHandler<ColorEventArgs> ColorChanged;

    protected int ManyDelta = 5;

    protected ColorBar ()
    {
        Height = 1;
        Width = Dim.Fill ();
        CanFocus = true;

        AddCommand (Command.Left, (_) => AdjustAndReturn (-1));
        AddCommand (Command.Right, (_) => AdjustAndReturn (1));

        AddCommand (Command.LeftExtend, (_) => AdjustAndReturn (-ManyDelta));
        AddCommand (Command.RightExtend, (_) => AdjustAndReturn (ManyDelta));

        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        KeyBindings.Add (Key.CursorLeft.WithShift, Command.LeftExtend);
        KeyBindings.Add (Key.CursorRight.WithShift, Command.RightExtend);
    }

    protected abstract void Adjust (int delta);

    protected bool? AdjustAndReturn (int delta)
    {
        Adjust (delta);
        return true;
    }

    /// <summary>
    /// Last know width of the bar as passed to <see cref="DrawBar"/>.
    /// </summary>
    protected int BarWidth { get; private set; }

    // Used to determine what change should happen when mouse clicks
    private Dictionary<Point, Color> _colorsRendered = new Dictionary<Point, Color> ();

    protected void AddBarCell (int x, int y, Color color)
    {
        _colorsRendered.Add (new Point (x,y),color);

        Application.Driver.SetAttribute (new Attribute (color, color));
        AddRune (x, y, new Rune ('█'));
    }
    protected virtual void OnColorChanged (Color color, Color prevColor)
    {
        // Ensure this view updates when the Value changes.
        ColorChanged?.Invoke (this, new ColorEventArgs { Color = color, PreviousColor = prevColor });

        // Invalidate the view so it gets redrawn.
        this.SetNeedsDisplay ();
    }

    /// <inheritdoc />
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        if (_colorsRendered.TryGetValue (mouseEvent.Position, out Color value))
        {
            Value = value;
            mouseEvent.Handled = true;

            // TODO: is this correct or should rest of event chain still get a call in?
            return true;
        }

        return base.OnMouseEvent (mouseEvent);

    }

    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        // Clear our knowledge of what the bar looks like
        _colorsRendered.Clear ();

        var xOffset = 0;
        if (!string.IsNullOrWhiteSpace (Text))
        {
            Move (0, 0);
            Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
            Driver.AddStr (Text);

            // TODO: How to do this properly? TextFormatter relevant methods are private (i.e. Sum Rune Widths)
            xOffset = Text.Length;
        }

        BarWidth = viewport.Width - xOffset;
        DrawBar (xOffset, 0, BarWidth);
    }

    protected abstract void DrawBar (int xOffset, int yOffset, int width);
}

public class HueBar : ColorBar
{
    protected override void DrawBar (int xOffset, int yOffset, int width)
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

            AddBarCell (x + xOffset, yOffset, color);
        }

        // Draw the triangle at the closest position
        var triangleColor = rainbowGradient.GetColorAtFraction ((double)closestPosition / (width - 1));
        Move (closestPosition + xOffset, yOffset);
        Application.Driver.SetAttribute (new Attribute (Color.Black, triangleColor));
        AddRune (closestPosition + xOffset, yOffset, new Rune ('▲'));
    }

    protected override void Adjust (int delta)
    {
        var hsl = ColorHelper.ColorConverter.RgbToHsl (new RGB (Value.R, Value.G, Value.B));
        double stepSize = 360.0 / ((double)BarWidth - 1);
        double newHue = hsl.H + delta * stepSize;

        if (newHue < 0)
        {
            newHue = 0;
        }
        else if (newHue > 360)
        {
            newHue = 360;
        }

        hsl.H = (int)newHue;
        var rgb = ColorHelper.ColorConverter.HslToRgb (hsl);
        Value = new Color (rgb.R, rgb.G, rgb.B);
    }
}

public class SaturationBar : ColorBar
{
    protected override void DrawBar (int xOffset, int yOffset, int width)
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

            AddBarCell (x + xOffset, yOffset, color);
        }

        // Draw the triangle at the closest position
        Move (closestPosition + xOffset, yOffset);
        Application.Driver.SetAttribute (new Attribute (ColorName.Black, closestColor));
        AddRune (closestPosition + xOffset, yOffset, new Rune ('▲'));
    }

    protected override void Adjust (int delta)
    {
        var hsl = ColorHelper.ColorConverter.RgbToHsl (new RGB (Value.R, Value.G, Value.B));
        double stepSize = 100.0 / ((double)BarWidth - 1);
        double newSaturation = hsl.S + delta * stepSize;

        if (newSaturation < 0)
        {
            newSaturation = 0;
        }
        else if (newSaturation > 100)
        {
            newSaturation = 100;
        }

        hsl.S = (byte)newSaturation;
        var rgb = ColorHelper.ColorConverter.HslToRgb (hsl);
        Value = new Color (rgb.R, rgb.G, rgb.B);
    }
}

public class LightnessBar : ColorBar
{
    protected override void DrawBar (int xOffset, int yOffset, int width)
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

            AddBarCell (x+xOffset,yOffset,color);
        }

        // Draw the triangle at the closest position
        Move (closestPosition + xOffset, yOffset);
        Application.Driver.SetAttribute (new Attribute (ColorName.Black, closestColor));
        AddRune (closestPosition + xOffset, yOffset, new Rune ('▲'));
    }


    protected override void Adjust (int delta)
    {
        var hsl = ColorHelper.ColorConverter.RgbToHsl (new RGB (Value.R, Value.G, Value.B));
        double stepSize = 100.0 / ((double)BarWidth - 1);
        double newLightness = hsl.L + delta * stepSize;

        if (newLightness < 0)
        {
            newLightness = 0;
        }
        else if (newLightness > 100)
        {
            newLightness = 100;
        }

        hsl.L = (byte)newLightness;
        var rgb = ColorHelper.ColorConverter.HslToRgb (hsl);
        Value = new Color (rgb.R, rgb.G, rgb.B);
    }
}
