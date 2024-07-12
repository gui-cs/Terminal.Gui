using ColorHelper;
using static Terminal.Gui.ColorPicker2;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui;


public enum ColorModel
{
    RGB,
    HSV,
    HSL
}

internal interface IColorBar
{
    int Value { get; set; }
}

internal class ColorModelStrategy
{
    public IEnumerable<ColorBar> CreateBars (ColorModel model)
    {
        switch (model)
        {
            case ColorModel.RGB:
                return CreateRgbBars ();
            case ColorModel.HSV:
                return CreateHsvBars ();
            case ColorModel.HSL:
                return CreateHslBars ();
            default:
                throw new ArgumentOutOfRangeException (nameof (model), model, null);
        }
    }

    private IEnumerable<ColorBar> CreateHslBars ()
    {
        var h = new HueBar ()
        {
            Text = "H:"
        };

        yield return h;

        var s = new SaturationBar ()
        {
            Text = "S:"
        };


        var l = new LightnessBar ()
        {
            Text = "L:",
        };

        s.HBar = h;
        s.LBar = l;

        l.HBar = h;
        l.SBar = s;

        yield return s;
        yield return l;
    }

    private IEnumerable<ColorBar> CreateRgbBars ()
    {
        var r = new RBar ()
        {
            Text = "R:"
        };
        var g = new GBar ()
        {
            Text = "G:"
        };
        var b = new BBar ()
        {
            Text = "B:"
        };
        r.GBar = g;
        r.BBar = b;

        g.RBar = r;
        g.BBar = b;

        b.RBar = r;
        b.GBar = g;

        yield return r;
        yield return g;
        yield return b;
    }
    private IEnumerable<ColorBar> CreateHsvBars ()
    {

        var h = new HueBar ()
        {
            Text = "H:"
        };

        yield return h;

        var s = new SaturationBar ()
        {
            Text = "S:"
        };


        var v = new ValueBar ()
        {
            Text = "V:",
        };

        s.HBar = h;
        s.VBar = v;

        v.HBar = h;
        v.SBar = s;

        yield return s;
        yield return v;
    }
    public Color GetColorFromBars (IList<IColorBar> bars, ColorModel model)
    {
        switch (model)
        {
            case ColorModel.RGB:
                return ToColor (new RGB ((byte)bars [0].Value, (byte)bars [1].Value, (byte)bars [2].Value));
            case ColorModel.HSV:
                return ToColor(
                               ColorConverter.HsvToRgb (new HSV (bars [0].Value, (byte)bars [1].Value, (byte)bars [2].Value))
                               );
            case ColorModel.HSL:
                return ToColor (
                                ColorConverter.HslToRgb (new HSL (bars [0].Value, (byte)bars [1].Value, (byte)bars [2].Value))
                                );
            default:
                throw new ArgumentOutOfRangeException (nameof (model), model, null);
        }
    }

    private Color ToColor (RGB rgb) => new (rgb.R, rgb.G, rgb.B);

    public void SetBarsToColor (IList<IColorBar> bars, Color newValue, ColorModel model)
    {
        switch (model)
        {
            case ColorModel.RGB:
                bars [0].Value = newValue.R;
                bars [1].Value = newValue.G;
                bars [2].Value = newValue.B;
                break;
            case ColorModel.HSV:
                var newHsv = ColorConverter.RgbToHsv (new RGB (newValue.R, newValue.G, newValue.B));
                bars [0].Value = newHsv.H;
                bars [1].Value = newHsv.S;
                bars [2].Value = newHsv.V;
                break;
            case ColorModel.HSL:

                var newHsl = ColorConverter.RgbToHsl (new RGB (newValue.R, newValue.G, newValue.B));
                bars [0].Value = newHsl.H;
                bars [1].Value = newHsl.S;
                bars [2].Value = newHsl.L;
                break;
            default:
                throw new ArgumentOutOfRangeException (nameof (model), model, null);
        }
    }
}

/// <summary>
/// True color picker using HSL
/// </summary>
public class ColorPicker2 : View
{
    private readonly TextField tfHex;

    private Color _value = Color.Red;
    private readonly ColorModelStrategy _strategy = new ColorModelStrategy ();
    private List<IColorBar> _bars = new List<IColorBar> ();

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

    private ColorModel _colorModel = ColorModel.HSV;

    public ColorModel ColorModel
    {
        get => _colorModel;
        set
        {
            _colorModel = value;
            SetupBarsAccordingToModel ();
        }
    }

    public ColorPicker2 ()
    {
        SetupBarsAccordingToModel ();

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

        Add (lbHex);
        Add (tfHex);

        tfHex.Leave += (_, _) => UpdateValueFromTextField ();
    }

    private void SetupBarsAccordingToModel ()
    {
        DisposeOldBars ();

        int y = 0;
        foreach (var bar in _strategy.CreateBars (_colorModel))
        {
            bar.Y = y++;
            bar.ValueChanged += RebuildColor;
            _bars.Add (bar);
            Add (bar);
        }
    }

    private void DisposeOldBars ()
    {
        foreach (ColorBar bar in _bars.Cast<ColorBar> ())
        {
            bar.ValueChanged -= RebuildColor;
            Remove (bar);
        }

        _bars = new List<IColorBar> ();
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
        _strategy.SetBarsToColor (_bars,color, _colorModel);
    }

    private void RebuildColor (object sender, EventArgs<int> e)
    {
        _value = _strategy.GetColorFromBars (_bars, _colorModel);
        tfHex.Text = _value.ToString ($"#{Value.R:X2}{Value.G:X2}{Value.B:X2}");
    }
}

public abstract class ColorBar : View, IColorBar
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

        AddCommand (Command.Left, (_) => Adjust (-1));
        AddCommand (Command.Right, (_) => Adjust (1));

        AddCommand (Command.LeftExtend, (_) => Adjust (- MaxValue/20));
        AddCommand (Command.RightExtend, (_) => Adjust (MaxValue / 20));

        AddCommand (Command.LeftHome, (_) => SetZero ());
        AddCommand (Command.RightEnd, (_) => SetMax ());


        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorLeft.WithShift, Command.LeftExtend);
        KeyBindings.Add (Key.CursorRight.WithShift, Command.RightExtend);
        KeyBindings.Add (Key.Home, Command.LeftHome);
        KeyBindings.Add (Key.End, Command.RightEnd);
    }

    private bool? SetMax ()
    {
        Value = MaxValue;

        return true;
    }

    private bool? SetZero ()
    {
        Value = 0;

        return true;
    }

    protected bool? Adjust (int delta)
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
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed) && mouseEvent.Position.X >= BarStartsAt)
        {
            Value = MaxValue * (mouseEvent.Position.X - BarStartsAt) / BarWidth;
            mouseEvent.Handled = true;
            FocusFirst ();
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

    // Should only have either LBar or VBar not both
    public LightnessBar LBar { get; set; }
    public ValueBar VBar { get; set; }

    /// <inheritdoc />
    protected override int MaxValue => 100;

    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        if (LBar != null)
        {
            var hsl = new HSL (HBar.Value, (byte)(MaxValue * fraction), (byte)LBar.Value);
            var rgb = ColorConverter.HslToRgb (hsl);

            return new Color (rgb.R, rgb.G, rgb.B);
        }

        if (VBar != null)
        {

            var hsv = new HSV (HBar.Value, (byte)(MaxValue * fraction), (byte)VBar.Value);
            var rgb = ColorConverter.HsvToRgb (hsv);

            return new Color (rgb.R, rgb.G, rgb.B);
        }

        throw new Exception ("SaturationBar requires either Lightness or Value to render");

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
class ValueBar : ColorBar
{
    public HueBar HBar { get; set; }
    public SaturationBar SBar { get; set; }

    /// <inheritdoc />
    protected override int MaxValue => 100;

    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        var hsv = new HSV (HBar.Value, (byte)SBar.Value, (byte)(MaxValue * fraction));
        var rgb = ColorConverter.HsvToRgb (hsv);

        return new Color (rgb.R, rgb.G, rgb.B);
    }
}


class RBar : ColorBar
{
    public GBar GBar { get; set; }
    public BBar BBar { get; set; }

    /// <inheritdoc />
    protected override int MaxValue => 255;

    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        var rgb = new RGB ((byte)(MaxValue*fraction), (byte)GBar.Value, (byte)BBar.Value);
        return new Color (rgb.R, rgb.G, rgb.B);
    }
}

class GBar : ColorBar
{
    public RBar RBar { get; set; }
    public BBar BBar { get; set; }

    /// <inheritdoc />
    protected override int MaxValue => 255;

    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        var rgb = new RGB ((byte)RBar.Value, (byte)(MaxValue * fraction), (byte)BBar.Value);
        return new Color (rgb.R, rgb.G, rgb.B);
    }
}
class BBar : ColorBar
{
    public RBar RBar { get; set; }
    public GBar GBar { get; set; }

    /// <inheritdoc />
    protected override int MaxValue => 255;

    /// <inheritdoc />
    protected override Color GetColor (double fraction)
    {
        var rgb = new RGB ((byte)RBar.Value, (byte)GBar.Value, (byte)(MaxValue * fraction));
        return new Color (rgb.R, rgb.G, rgb.B);
    }
}