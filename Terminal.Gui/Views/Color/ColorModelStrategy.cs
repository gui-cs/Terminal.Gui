#nullable enable

using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui.Views;

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

    public void SetBarsToColor (IList<IColorBar> bars, Color newValue, ColorModel model)
    {
        if (bars.Count == 0)
        {
            return;
        }
        switch (model)
        {
            case ColorModel.RGB:
                bars [0].SetValueWithoutRaisingEvent (newValue.R);
                bars [1].SetValueWithoutRaisingEvent (newValue.G);
                bars [2].SetValueWithoutRaisingEvent (newValue.B);

                break;
            case ColorModel.HSV:
                HSV newHsv = ColorConverter.RgbToHsv (new (newValue.R, newValue.G, newValue.B));
                bars [0].SetValueWithoutRaisingEvent (newHsv.H);
                bars [1].SetValueWithoutRaisingEvent (newHsv.S);
                bars [2].SetValueWithoutRaisingEvent (newHsv.V);

                break;
            case ColorModel.HSL:

                HSL newHsl = ColorConverter.RgbToHsl (new (newValue.R, newValue.G, newValue.B));
                bars [0].SetValueWithoutRaisingEvent (newHsl.H);
                bars [1].SetValueWithoutRaisingEvent (newHsl.S);
                bars [2].SetValueWithoutRaisingEvent (newHsl.L);

                break;
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

    private Color ToColor (RGB rgb) { return new (rgb.R, rgb.G, rgb.B); }
}
