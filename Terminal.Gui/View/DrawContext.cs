#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Tracks the region that has been drawn during <see cref="View.Draw(DrawContext?)"/>. This is primarily
///     in support of <see cref="ViewportSettings.Transparent"/>.
/// </summary>
public class DrawContext
{
    private readonly Region _drawnRegion = new Region ();

    /// <summary>
    /// Gets a copy of the region drawn so far in this context.
    /// </summary>
    public Region GetDrawnRegion () => _drawnRegion.Clone ();

    /// <summary>
    /// Reports that a rectangle has been drawn.
    /// </summary>
    /// <param name="rect">The rectangle that was drawn.</param>
    public void AddDrawnRectangle (Rectangle rect)
    {
        _drawnRegion.Combine (rect, RegionOp.Union);
    }

    /// <summary>
    /// Reports that a region has been drawn.
    /// </summary>
    /// <param name="region">The region that was drawn.</param>
    public void AddDrawnRegion (Region region)
    {
        _drawnRegion.Combine (region, RegionOp.Union);
    }

    /// <summary>
    /// Clips (intersects) the drawn region with the specified rectangle.
    /// This modifies the internal drawn region directly.
    /// </summary>
    /// <param name="clipRect">The clipping rectangle.</param>
    public void ClipDrawnRegion (Rectangle clipRect)
    {
        _drawnRegion.Intersect (clipRect);
    }

    /// <summary>
    /// Clips (intersects) the drawn region with the specified region.
    /// This modifies the internal drawn region directly.
    /// </summary>
    /// <param name="clipRegion">The clipping region.</param>
    public void ClipDrawnRegion (Region clipRegion)
    {
        _drawnRegion.Intersect (clipRegion);
    }
}
