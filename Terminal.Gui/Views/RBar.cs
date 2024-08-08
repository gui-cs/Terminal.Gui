#nullable enable

using ColorHelper;

namespace Terminal.Gui;

internal class RBar : ColorBar
{
    public GBar? GBar { get; set; }
    public BBar? BBar { get; set; }

    /// <inheritdoc/>
    protected override int MaxValue => 255;

    /// <inheritdoc/>
    protected override Color GetColor (double fraction)
    {
        if (GBar == null || BBar == null)
        {
            throw new Exception ($"{nameof (RBar)} has not been set up correctly before drawing");
        }

        var rgb = new RGB ((byte)(MaxValue * fraction), (byte)GBar.Value, (byte)BBar.Value);

        return new (rgb.R, rgb.G, rgb.B);
    }
}
