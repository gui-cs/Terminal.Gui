using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui;

/// <summary>
/// True color picker using HSL
/// </summary>
public class ColorPicker2 : View
{
    private readonly TextField tfHex;

    private Color _value = Color.Red;
    private readonly LightnessBar lb;
    private readonly HueBar hb;
    private SaturationBar sb;

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
        hb = new HueBar ()
        {
            Text = "H:",
            X = 0,
            Y = 0,
            Height = 1,
            Width = Dim.Fill ()
        };

        sb = new SaturationBar ()
        {
            Text = "S:",
            Y = 1,
            Height = 1,
            Width = Dim.Fill ()
        };

        lb = new LightnessBar ()
        {
            Text = "L:",
            Y = 2,
            Height = 1,
            Width = Dim.Fill ()
        };

        sb.HBar = hb;
        sb.LBar = lb;

        lb.HBar = hb;
        lb.SBar = sb;

        hb.ValueChanged += RebuildColor;
        sb.ValueChanged += RebuildColor;
        lb.ValueChanged += RebuildColor;

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


    void RebuildColor (object sender, EventArgs<double> e)
    {
        var hsl = new HSL ((int)(360 * hb.Value), (byte)(100 * sb.Value), (byte)(100 * lb.Value));

        var rgb = ColorConverter.HslToRgb (hsl);
        Value = new Color (rgb.R, rgb.G, rgb.B);
    }
}
public abstract class ColorBar : View
{
    /// <summary>
    /// How much color space is each cell in the bar
    /// </summary>
    private double cellWidth;

    protected int BarStartsAt;

    /// <summary>
    /// 0-1 for how much of the color element is present currently (HSL)
    /// </summary>
    private double _value;
    public double Value
    {
        get => _value;
        private set
        {
            _value = Math.Clamp (value, 0, 1);
            OnValueChanged ();
        }
    }

    public event EventHandler<EventArgs<double>> ValueChanged;

    protected ColorBar ()
    {
        Height = 1;
        Width = Dim.Fill ();
        CanFocus = true;

        AddCommand (Command.Left, (_) => AdjustAndReturn (-1));
        AddCommand (Command.Right, (_) => AdjustAndReturn (1));

        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
    }

    protected bool? AdjustAndReturn (int delta)
    {
        Value += delta * cellWidth;
        return true;
    }

    /// <summary>
    /// Last known width of the bar as passed to <see cref="DrawBar"/>.
    /// </summary>
    protected int BarWidth { get; private set; }


    /// <inheritdoc />
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        if (mouseEvent.Position.X >= BarStartsAt)
        {
            Value = (double)(mouseEvent.Position.X - BarStartsAt) / BarWidth;
            mouseEvent.Handled = true;
            return true;
        }

        return base.OnMouseEvent (mouseEvent);
    }

    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        var xOffset = 0;
        if (!string.IsNullOrWhiteSpace (Text))
        {
            Move (0, 0);
            Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
            Driver.AddStr (Text);

            xOffset = Text.Length;
        }

        BarWidth = viewport.Width - xOffset;
        cellWidth = 1d / BarWidth;
        BarStartsAt = xOffset;

        DrawBar (xOffset, 0, BarWidth);
    }

    private void DrawBar (int xOffset, int yOffset, int width)
    {
        int selectedCell = (int)(Value * (width - 1));

        for (int x = 0; x < width; x++)
        {
            double fraction = (double)x / (width - 1);
            Color color = GetColor (fraction);

            Application.Driver.SetAttribute (new Attribute (color, color));

            if (x == selectedCell)
            {
                // Draw the triangle at the closest position
                Application.Driver.SetAttribute (new Attribute (Color.Black, color));
                AddRune (x + xOffset, yOffset, new Rune ('▲'));
            }
            else
            {
                AddRune (x + xOffset, yOffset, new Rune ('█'));
            }
        }
    }

    protected abstract Color GetColor (double fraction);

    protected virtual void OnValueChanged ()
    {
        ValueChanged?.Invoke (this,new (ref _value));
        // Notify subscribers if any, and redraw the view
        this.SetNeedsDisplay ();
    }
}


internal class HueBar : ColorBar
{
    private readonly Gradient rainbowGradient;

    public HueBar ()
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
        rainbowGradient = new Gradient (stops, steps, true);
    }

    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        return rainbowGradient.GetColorAtFraction (fraction);
    }
}

internal class SaturationBar : ColorBar
{
    public HueBar HBar { get; set; }
    public LightnessBar LBar { get; set; }


    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        var hsl = new HSL ((int)(HBar.Value * 360), (byte)(100 * fraction), (byte)(100 * LBar.Value));
        var rgb = ColorConverter.HslToRgb (hsl);

        return new Color (rgb.R, rgb.G, rgb.B);
    }
}

internal class LightnessBar : ColorBar
{

    public HueBar HBar { get; set; }
    public SaturationBar SBar { get; set; }
    
    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        var hsl = new HSL ((int)(HBar.Value * 360), (byte)(100 * SBar.Value), (byte)(100 * Value));
        var rgb = ColorConverter.HslToRgb (hsl);

        return new Color (rgb.R, rgb.G, rgb.B);
    }
}
