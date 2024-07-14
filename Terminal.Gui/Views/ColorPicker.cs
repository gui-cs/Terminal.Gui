using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui;

/// <summary>
/// Describes away of modelling color e.g. Hue
/// Saturation Lightness.
/// </summary>
public enum ColorModel
{
    /// <summary>
    /// Color modelled by storing Red, Green and Blue as (0-255) ints
    /// </summary>
    RGB,

    /// <summary>
    /// Color modelled by storing Hue (360 degrees), Saturation (100%) and SelectedColor (100%)
    /// </summary>
    HSV,

    /// <summary>
    /// Color modelled by storing Hue (360 degrees), Saturation (100%) and Lightness (100%)
    /// </summary>
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
        var h = new HueBar
        {
            Text = "H:"
        };

        yield return h;

        var s = new SaturationBar
        {
            Text = "S:"
        };

        var l = new LightnessBar
        {
            Text = "L:"
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
        var r = new RBar
        {
            Text = "R:"
        };

        var g = new GBar
        {
            Text = "G:"
        };

        var b = new BBar
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
        var h = new HueBar
        {
            Text = "H:"
        };

        yield return h;

        var s = new SaturationBar
        {
            Text = "S:"
        };

        var v = new ValueBar
        {
            Text = "V:"
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
                return ToColor (new ((byte)bars [0].Value, (byte)bars [1].Value, (byte)bars [2].Value));
            case ColorModel.HSV:
                return ToColor (
                                ColorConverter.HsvToRgb (new (bars [0].Value, (byte)bars [1].Value, (byte)bars [2].Value))
                               );
            case ColorModel.HSL:
                return ToColor (
                                ColorConverter.HslToRgb (new (bars [0].Value, (byte)bars [1].Value, (byte)bars [2].Value))
                               );
            default:
                throw new ArgumentOutOfRangeException (nameof (model), model, null);
        }
    }

    private Color ToColor (RGB rgb) { return new (rgb.R, rgb.G, rgb.B); }

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
                HSV newHsv = ColorConverter.RgbToHsv (new (newValue.R, newValue.G, newValue.B));
                bars [0].Value = newHsv.H;
                bars [1].Value = newHsv.S;
                bars [2].Value = newHsv.V;

                break;
            case ColorModel.HSL:

                HSL newHsl = ColorConverter.RgbToHsl (new (newValue.R, newValue.G, newValue.B));
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
/// Contains style settings for <see cref="ColorPicker"/> e.g. which <see cref="ColorModel"/>
/// to use.
/// </summary>
public class ColorPickerStyle
{
    /// <summary>
    ///     The color model for picking colors by RGB, HSV, etc.
    /// </summary>
    public ColorModel ColorModel { get; set; } = ColorModel.HSV;

    /// <summary>
    ///     True to put the numerical value of bars on the right of the color bar
    /// </summary>
    public bool ShowTextFields { get; set; } = true;
}

/// <summary>
///     True color picker using HSL
/// </summary>
public class ColorPicker : View
{
    private TextField _tfHex;
    private Label _lbHex;

    private Color _selectedColor = Color.Black;

    private List<IColorBar> _bars = new ();
    private readonly Dictionary<IColorBar, TextField> _textFields = new ();
    private readonly ColorModelStrategy _strategy = new ();

    /// <summary>
    ///     Style settings for the color picker.  After making changes ensure you call
    ///     <see cref="ApplyStyleChanges"/>.
    /// </summary>
    public ColorPickerStyle Style { get; set; } = new ();

    /// <summary>
    ///     Fired when color is changed.
    /// </summary>
    public event EventHandler<ColorEventArgs> ColorChanged;

    private bool _updating;

    /// <summary>
    ///     The color selected in the picker
    /// </summary>
    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            try
            {
                _updating = true;

                if (_selectedColor != value)
                {
                    Color old = _selectedColor;
                    _selectedColor = value;
                    SetTextFieldToValue ();
                    UpdateBarsFromColor (value);

                    ColorChanged?.Invoke (
                                          this,
                                          new()
                                          {
                                              Color = value,
                                              PreviousColor = old
                                          });
                }
            }
            finally
            {
                _updating = false;
            }
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="ColorPicker"/>. Use
    /// <see cref="Style"/> to change color model. Use <see cref="SelectedColor"/>
    /// to change initial <see cref="Color"/>.
    /// </summary>
    public ColorPicker ()
    {
        Height = 4;
        Width = Dim.Fill ();
        ApplyStyleChanges ();
    }

    /// <summary>
    /// Rebuild the user interface to reflect the new state of <see cref="Style"/>.
    /// </summary>
    public void ApplyStyleChanges ()
    {
        Color oldValue = _selectedColor;
        DisposeOldViews ();

        var y = 0;
        const int textFieldWidth = 4;

        foreach (ColorBar bar in _strategy.CreateBars (Style.ColorModel))
        {
            bar.Y = y;
            bar.Width = Dim.Fill (Style.ShowTextFields ? textFieldWidth : 0);

            if (Style.ShowTextFields)
            {
                var tfValue = new TextField
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

        UpdateBarsFromColor (SelectedColor);
        RebuildColor (this, default (EventArgs<int>));
        SelectedColor = oldValue;
    }

    private void CreateTextField ()
    {
        _lbHex = new()
        {
            Text = "Hex:",
            X = 0,
            Y = 3
        };

        _tfHex = new()
        {
            Y = 3,
            X = 4,
            Width = 8
        };

        Add (_lbHex);
        Add (_tfHex);

        _tfHex.Leave += UpdateValueFromTextField;
    }

    private void UpdateSingleBarValueFromTextField (object sender, FocusEventArgs e)
    {
        foreach (KeyValuePair<IColorBar, TextField> kvp in _textFields)
        {
            if (kvp.Value == sender)
            {
                if (int.TryParse (kvp.Value.Text, out int v))
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

        _bars = new ();
        _textFields.Clear ();

        if (_lbHex != null)
        {
            Remove (_lbHex);
            _lbHex.Dispose ();
            _lbHex = null;
        }

        if (_tfHex != null)
        {
            Remove (_tfHex);
            _tfHex.Leave -= UpdateValueFromTextField;
            _tfHex.Dispose ();
            _tfHex = null;
        }
    }

    private void UpdateValueFromTextField (object sender, FocusEventArgs e)
    {
        if (Color.TryParse (_tfHex.Text, out Color? newColor))
        {
            SelectedColor = newColor.Value;
        }
        else
        {
            // value is invalid, revert the value in the text field back to current state
            SetTextFieldToValue ();
        }
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);
        Attribute normal = GetNormalColor ();
        Driver.SetAttribute (new (SelectedColor, normal.Background));
        AddRune (13, 3, (Rune)'■');
    }

    private void UpdateBarsFromColor (Color color)
    {
        _strategy.SetBarsToColor (_bars, color, Style.ColorModel);
        SetTextFieldToValue ();
    }

    private void RebuildColor (object sender, EventArgs<int> e)
    {
        foreach (KeyValuePair<IColorBar, TextField> kvp in _textFields)
        {
            kvp.Value.Text = kvp.Key.Value.ToString ();
        }

        if (!_updating)
        {
            SelectedColor = _strategy.GetColorFromBars (_bars, Style.ColorModel);
        }

        SetTextFieldToValue ();
    }

    private void SetTextFieldToValue () { _tfHex.Text = _selectedColor.ToString ($"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}"); }
}

/// <summary>
/// A bar representing a single component of a <see cref="Color"/> e.g.
/// the Red portion of a <see cref="ColorModel.RGB"/>.
/// </summary>
public abstract class ColorBar : View, IColorBar
{
    /// <summary>
    /// X coordinate that the bar starts at excluding any label.
    /// </summary>
    private int _barStartsAt;

    /// <summary>
    ///     0-1 for how much of the color element is present currently (HSL)
    /// </summary>
    private int _value;

    /// <summary>
    ///     The amount of <see cref="Value"/> represented by each cell width on the bar
    ///     Can be less than 1 e.g. if Saturation (0-100) and width > 100
    /// </summary>
    private double _cellValue = 1d;

    /// <summary>
    /// The maximum value allowed for this component e.g. Saturation allows up to 100 as it
    /// is a percentage while Hue allows up to 360 as it is measured in degrees.
    /// </summary>
    protected abstract int MaxValue { get; }

    /// <summary>
    /// The currently selected amount of the color component stored by this class e.g.
    /// the amount of Hue in a <see cref="ColorModel.HSL"/>.
    /// </summary>
    public int Value
    {
        get => _value;
        set
        {
            int clampedValue = Math.Clamp (value, 0, MaxValue);

            if (_value != clampedValue)
            {
                _value = clampedValue;
                OnValueChanged ();
            }
        }
    }

    /// <summary>
    /// The last drawn location in View's viewport where the Triangle appeared.
    /// Used exclusively for tests.
    /// </summary>
    internal int TrianglePosition { get; private set; }

    /// <summary>
    /// Event fired when <see cref="Value"/> is changed to a new value
    /// </summary>
    public event EventHandler<EventArgs<int>> ValueChanged;

    /// <summary>
    /// Creates a new instance of the <see cref="ColorBar"/> class.
    /// </summary>
    protected ColorBar ()
    {
        Height = 1;
        Width = Dim.Fill ();
        CanFocus = true;

        AddCommand (Command.Left, _ => Adjust (-1));
        AddCommand (Command.Right, _ => Adjust (1));

        AddCommand (Command.LeftExtend, _ => Adjust (-MaxValue / 20));
        AddCommand (Command.RightExtend, _ => Adjust (MaxValue / 20));

        AddCommand (Command.LeftHome, _ => SetZero ());
        AddCommand (Command.RightEnd, _ => SetMax ());

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

    private bool? Adjust (int delta)
    {
        var change = (int)(delta * _cellValue);

        // Ensure that the change is at least 1 or -1 if delta is non-zero
        if (change == 0 && delta != 0)
        {
            change = delta > 0 ? 1 : -1;
        }

        Value += change;

        return true;
    }

    /// <summary>
    ///     Last known width of the bar as passed to <see cref="DrawBar"/>.
    /// </summary>
    private int _barWidth;


    /// <inheritdoc/>
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            if (mouseEvent.Position.X >= _barStartsAt)
            {
                double v = MaxValue * ((double)mouseEvent.Position.X - _barStartsAt) / (_barWidth - 1);
                Value = Math.Clamp ((int)v, 0, MaxValue);
            }

            mouseEvent.Handled = true;
            FocusFirst ();

            return true;
        }

        return base.OnMouseEvent (mouseEvent);
    }

    /// <inheritdoc/>
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

        _barWidth = viewport.Width - xOffset;
        _barStartsAt = xOffset;

        DrawBar (xOffset, 0, _barWidth);
    }

    private void DrawBar (int xOffset, int yOffset, int width)
    {
        // Each 1 unit of X in the bar corresponds to this much of SelectedColor
        _cellValue = (double)MaxValue / (width - 1);

        for (var x = 0; x < width; x++)
        {
            double fraction = (double)x / (width - 1);
            Color color = GetColor (fraction);

            // Adjusted isSelectedCell calculation
            bool isSelectedCell = Value > (x - 1) * _cellValue && Value <= x * _cellValue;

            // Check the brightness of the background color
            double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

            Color triangleColor = Color.Black;

            if (brightness < 0.15) // Threshold to determine if the color is too close to black
            {
                triangleColor = Color.DarkGray;
            }

            if (isSelectedCell)
            {
                // Draw the triangle at the closest position
                Application.Driver.SetAttribute (new (triangleColor, color));
                AddRune (x + xOffset, yOffset, new ('▲'));

                // Record for tests
                TrianglePosition = x + xOffset;
            }
            else
            {
                Application.Driver.SetAttribute (new (color, color));
                AddRune (x + xOffset, yOffset, new ('█'));
            }
        }
    }

    /// <summary>
    /// When overriden in a derived class, returns the <see cref="Color"/> to
    /// render at <paramref name="fraction"/> proportion of the full bars width.
    /// e.g. 0.5 fraction of Saturation is 50% because Saturation goes from 0-100.
    /// </summary>
    /// <param name="fraction"></param>
    /// <returns></returns>
    protected abstract Color GetColor (double fraction);

    private void OnValueChanged ()
    {
        ValueChanged?.Invoke (this, new (in _value));

        // Notify subscribers if any, and redraw the view
        SetNeedsDisplay ();
    }
}

internal class HueBar : ColorBar
{
    /// <inheritdoc/>
    protected override int MaxValue => 360;

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        var hsl = new HSL ((int)(MaxValue * fraction), 100, 50);
        RGB rgb = ColorConverter.HslToRgb (hsl);

        return new (rgb.R, rgb.G, rgb.B);
    }
}

internal class SaturationBar : ColorBar
{
    public HueBar HBar { get; set; }

    // Should only have either LBar or VBar not both
    public LightnessBar LBar { get; set; }
    public ValueBar VBar { get; set; }

    /// <inheritdoc/>
    protected override int MaxValue => 100;

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        if (LBar != null)
        {
            var hsl = new HSL (HBar.Value, (byte)(MaxValue * fraction), (byte)LBar.Value);
            RGB rgb = ColorConverter.HslToRgb (hsl);

            return new (rgb.R, rgb.G, rgb.B);
        }

        if (VBar != null)
        {
            var hsv = new HSV (HBar.Value, (byte)(MaxValue * fraction), (byte)VBar.Value);
            RGB rgb = ColorConverter.HsvToRgb (hsv);

            return new (rgb.R, rgb.G, rgb.B);
        }

        throw new ("SaturationBar requires either Lightness or SelectedColor to render");
    }
}

internal class LightnessBar : ColorBar
{
    public HueBar HBar { get; set; }
    public SaturationBar SBar { get; set; }

    /// <inheritdoc/>
    protected override int MaxValue => 100;

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        var hsl = new HSL (HBar.Value, (byte)SBar.Value, (byte)(MaxValue * fraction));
        RGB rgb = ColorConverter.HslToRgb (hsl);

        return new (rgb.R, rgb.G, rgb.B);
    }
}

internal class ValueBar : ColorBar
{
    public HueBar HBar { get; set; }
    public SaturationBar SBar { get; set; }

    /// <inheritdoc/>
    protected override int MaxValue => 100;

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        var hsv = new HSV (HBar.Value, (byte)SBar.Value, (byte)(MaxValue * fraction));
        RGB rgb = ColorConverter.HsvToRgb (hsv);

        return new (rgb.R, rgb.G, rgb.B);
    }
}

internal class RBar : ColorBar
{
    public GBar GBar { get; set; }
    public BBar BBar { get; set; }

    /// <inheritdoc/>
    protected override int MaxValue => 255;

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        var rgb = new RGB ((byte)(MaxValue * fraction), (byte)GBar.Value, (byte)BBar.Value);

        return new (rgb.R, rgb.G, rgb.B);
    }
}

internal class GBar : ColorBar
{
    public RBar RBar { get; set; }
    public BBar BBar { get; set; }

    /// <inheritdoc/>
    protected override int MaxValue => 255;

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        var rgb = new RGB ((byte)RBar.Value, (byte)(MaxValue * fraction), (byte)BBar.Value);

        return new (rgb.R, rgb.G, rgb.B);
    }
}

internal class BBar : ColorBar
{
    public RBar RBar { get; set; }
    public GBar GBar { get; set; }

    /// <inheritdoc/>
    protected override int MaxValue => 255;

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        var rgb = new RGB ((byte)RBar.Value, (byte)GBar.Value, (byte)(MaxValue * fraction));

        return new (rgb.R, rgb.G, rgb.B);
    }
}
