using ColorHelper;
using ColorConverter = ColorHelper.ColorConverter;

namespace Terminal.Gui;

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
