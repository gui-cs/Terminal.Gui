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
    private readonly SaturationBar sb;

    /// <summary>
    /// The color selected in the picker
    /// </summary>
    public Color Value
    {
        get => _value;
        set
        {
            // External changes made by API caller
            if (_value != value)
            {
                _value = value;
                tfHex.Text = value.ToString ($"#{Value.R:X2}{Value.G:X2}{Value.B:X2}");
                UpdateBarsFromColor (value);
            }
        }
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

        var lbHex = new Label ()
        {
            Text = "Hex:",
            X = 0,
            Y = 3
        };
        tfHex = new TextField ()
        {
            Y = 3,
            X = 4,
            Width = 8
        };

        Add (hb);
        Add (sb);
        Add (lb);
        Add (lbHex);
        Add (tfHex);

        tfHex.Leave += (_, _) => UpdateValueFromTextField ();
    }

    private void UpdateValueFromTextField ()
    {
        if (Color.TryParse (tfHex.Text, out var newColor))
        {
            Value = newColor.Value;
        }
    }

    /// <inheritdoc />
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);
        var normal = GetNormalColor ();
        Driver.SetAttribute (new Attribute (Value, normal.Background));
        AddRune (13, 3, (Rune)'■');
    }

    private void UpdateBarsFromColor (Color color)
    {
        var hsl = ColorConverter.RgbToHsl (new RGB (color.R, color.G, color.B));
        hb.Value = hsl.H;
        sb.Value = hsl.S;
        lb.Value = hsl.L;
    }

    private void RebuildColor (object sender, EventArgs<int> e)
    {
        var hsl = new HSL (hb.Value, (byte)sb.Value, (byte)lb.Value);
        var rgb = ColorConverter.HslToRgb (hsl);
        _value = new Color (rgb.R, rgb.G, rgb.B);

        tfHex.Text = _value.ToString ($"#{Value.R:X2}{Value.G:X2}{Value.B:X2}");
    }
}

public abstract class ColorBar : View
{
    protected int BarStartsAt;

    /// <summary>
    /// 0-1 for how much of the color element is present currently (HSL)
    /// </summary>
    private int _value;

    protected abstract int MaxValue { get; }
    public int Value
    {
        get => _value;
        set
        {
            var clampedValue = Math.Clamp (value, 0, MaxValue);
            if (_value != clampedValue)
            {
                _value = clampedValue;
                OnValueChanged ();
            }
        }
    }

    public event EventHandler<EventArgs<int>> ValueChanged;

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
        Value += delta;
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
            Value = MaxValue * (mouseEvent.Position.X - BarStartsAt) / BarWidth;
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
        BarStartsAt = xOffset;

        DrawBar (xOffset, 0, BarWidth);
    }

    private void DrawBar (int xOffset, int yOffset, int width)
    {
        var factor = (double)Value / MaxValue;

        int selectedCell = (int)(factor * (width - 1));

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
        ValueChanged?.Invoke (this, new EventArgs<int> (_value));
        // Notify subscribers if any, and redraw the view
        this.SetNeedsDisplay ();
    }
}

internal class HueBar : ColorBar
{
    /// <inheritdoc />
    protected override int MaxValue => 360;

    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        var hsl = new HSL ((int)(MaxValue * fraction), 100, 50);
        var rgb = ColorConverter.HslToRgb (hsl);

        return new Color (rgb.R, rgb.G, rgb.B);
    }
}

internal class SaturationBar : ColorBar
{
    public HueBar HBar { get; set; }
    public LightnessBar LBar { get; set; }

    /// <inheritdoc />
    protected override int MaxValue => 100;

    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        var hsl = new HSL (HBar.Value, (byte)(MaxValue * fraction), (byte)LBar.Value);
        var rgb = ColorConverter.HslToRgb (hsl);

        return new Color (rgb.R, rgb.G, rgb.B);
    }
}

internal class LightnessBar : ColorBar
{
    public HueBar HBar { get; set; }
    public SaturationBar SBar { get; set; }

    /// <inheritdoc />
    protected override int MaxValue => 100;

    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        var hsl = new HSL (HBar.Value, (byte)SBar.Value, (byte)(MaxValue * fraction));
        var rgb = ColorConverter.HslToRgb (hsl);

        return new Color (rgb.R, rgb.G, rgb.B);
    }
}
