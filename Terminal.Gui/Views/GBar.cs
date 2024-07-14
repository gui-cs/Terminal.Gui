using ColorHelper;

namespace Terminal.Gui;

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
