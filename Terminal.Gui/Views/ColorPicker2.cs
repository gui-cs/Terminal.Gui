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

public class ColorPickerStyle
{
    /// <summary>
    /// The color model for picking colors by RGB, HSV, etc.
    /// </summary>
    public ColorModel ColorModel { get; set; } = ColorModel.HSV;

    /// <summary>
    /// True to put the numerical value of bars on the right of the color bar
    /// </summary>
    public bool ShowTextFields { get; set; } = true;
}

/// <summary>
/// True color picker using HSL
/// </summary>
public class ColorPicker2 : View
{
    private TextField tfHex;
    private Label lbHex;

    private Color _value = Color.Black;

    private List<IColorBar> _bars = new List<IColorBar> ();
    private Dictionary<IColorBar, TextField> _textFields = new Dictionary<IColorBar, TextField> ();
    private readonly ColorModelStrategy _strategy = new ColorModelStrategy ();

    /// <summary>
    /// Style settings for the color picker.  After making changes ensure you call
    /// <see cref="ApplyStyleChanges"/>.
    /// </summary>
    public ColorPickerStyle Style { get; set; } = new ColorPickerStyle();

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
                SetTextFieldToValue ();
                UpdateBarsFromColor (value);
            }
        }
    }


    public ColorPicker2 ()
    {
        ApplyStyleChanges ();
    }

    public void ApplyStyleChanges ()
    {
        DisposeOldViews ();

        int y = 0;
        const int textFieldWidth = 4;

        foreach (var bar in _strategy.CreateBars (Style.ColorModel))
        {
            bar.Y = y;
            bar.Width = Dim.Fill (Style.ShowTextFields ? textFieldWidth:0);

            if (Style.ShowTextFields)
            {
                var tfValue = new TextField ()
                {
                    X = Pos.AnchorEnd (textFieldWidth),
                    Y = y,
                    Width = textFieldWidth
                };
                tfValue.Leave += UpdateSingleBarValueFromTextField;
                _textFields.Add (bar, tfValue);
                Add (tfValue);
            }

            y++;

            bar.ValueChanged += RebuildColor;

            _bars.Add (bar);

            Add (bar);
        }

        CreateTextField ();
        SetTextFieldToValue ();

        UpdateBarsFromColor (Value);
        RebuildColor (this, default (EventArgs<int>));
    }

    private void CreateTextField ()
    {
        lbHex = new Label ()
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

        tfHex.Leave += UpdateValueFromTextField;
    }

    private void UpdateSingleBarValueFromTextField (object sender, FocusEventArgs e)
    {
        foreach (var kvp in _textFields)
        {
            if (kvp.Value == sender)
            {
                if(int.TryParse(kvp.Value.Text, out var v))
                {
                    kvp.Key.Value = v;
                }
            }
        }
    }

    private void DisposeOldViews ()
    {
        foreach (ColorBar bar in _bars.Cast<ColorBar> ())
        {
            bar.ValueChanged -= RebuildColor;

            if (_textFields.TryGetValue (bar, out TextField tf))
            {
                tf.Leave -= UpdateSingleBarValueFromTextField;
                Remove (tf);
                tf.Dispose ();

            }
            Remove (bar);
        }

        _bars = new List<IColorBar> ();
        _textFields.Clear ();

        if (lbHex != null)
        {
            Remove (lbHex);
            lbHex.Dispose ();
            lbHex = null;
        }

        if (tfHex != null)
        {
            Remove (tfHex);
            tfHex.Leave -= UpdateValueFromTextField;
            tfHex.Dispose ();
            tfHex = null;
        }
    }

    private void UpdateValueFromTextField (object sender, FocusEventArgs e)
    {
        if (Color.TryParse (tfHex.Text, out var newColor))
        {
            Value = newColor.Value;
        }
        else
        {
            // value is invalid, revert the value in the text field back to current state
            SetTextFieldToValue ();
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
        _strategy.SetBarsToColor (_bars,color, Style.ColorModel);
        SetTextFieldToValue ();
    }

    private void RebuildColor (object sender, EventArgs<int> e)
    {
        foreach(var kvp in _textFields)
        {
            kvp.Value.Text = kvp.Key.Value.ToString ();
        }

        _value = _strategy.GetColorFromBars (_bars, Style.ColorModel);
        SetTextFieldToValue ();
    }

    private void SetTextFieldToValue ()
    {
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

    /// <summary>
    /// The amount of <see cref="Value"/> represented by each cell width on the bar
    /// Can be less than 1 e.g. if Saturation (0-100) and width > 100
    /// </summary>
    private double _cellValue = 1d;

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
        int change = (int)(delta * _cellValue);

        // Ensure that the change is at least 1 or -1 if delta is non-zero
        if (change == 0 && delta != 0)
        {
            change = delta > 0 ? 1 : -1;
        }

        Value += change;
        return true;
    }

    /// <summary>
    /// Last known width of the bar as passed to <see cref="DrawBar"/>.
    /// </summary>
    protected int BarWidth { get; private set; }

    /// <inheritdoc />
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed) )
        {
            if (mouseEvent.Position.X >= BarStartsAt)
            {
                var v = MaxValue * ((double)mouseEvent.Position.X - BarStartsAt) / (BarWidth - 1);
                Value = Math.Clamp ((int)v, 0, MaxValue);
            }

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
        // Each 1 unit of X in the bar corresponds to this much of Value
        _cellValue = (double)MaxValue / (width - 1);

        for (int x = 0; x < width; x++)
        {
            double fraction = (double)x / (width - 1);
            Color color = GetColor (fraction);

            // Adjusted isSelectedCell calculation
            bool isSelectedCell = Value > (x-1) * _cellValue && Value <= x * _cellValue;

            if (isSelectedCell)
            {
                // Draw the triangle at the closest position
                Application.Driver.SetAttribute (new Attribute (Color.Black, color));
                AddRune (x + xOffset, yOffset, new Rune ('▲'));
            }
            else
            {
                Application.Driver.SetAttribute (new Attribute (color, color));
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