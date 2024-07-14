using ColorHelper;

namespace Terminal.Gui;

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
