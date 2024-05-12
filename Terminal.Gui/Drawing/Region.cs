#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes the interior of a screen shape composed of rectangles and paths. This class cannot be inherited.
/// </summary>
public sealed class Region
{
    private Rectangle _rect;

    /// <summary>
    ///     Creates an exact copy of this <see cref="Region"/>.
    /// </summary>
    /// <returns></returns>
    public Region Clone () { return new (_rect); }

    /// <inheritdoc/>
    public override bool Equals (object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        Rectangle thisRect = _rect;
        Rectangle otherRect = ((Region)obj)._rect;

        return thisRect == otherRect;
    }

    /// <inheritdoc/>
    public override int GetHashCode () { return HashCode.Combine (_rect); }

    #region Constructors

    /// <summary>
    ///     Initializes a new <see cref="Region"/>.
    /// </summary>
    public Region () : this (Rectangle.Empty) { }

    /// <summary>
    ///     Initializes a new <see cref="Region"/> from the specified <see cref="Rectangle"/> structure.
    /// </summary>
    /// <param name="rect"></param>
    public Region (Rectangle rect) { _rect = rect; }

    /// <summary>
    ///     Initializes a new <see cref="Region"/> from the specified <see cref="RectangleF"/> reference.
    /// </summary>
    /// <param name="rect"></param>
    public Region (RectangleF rect) { _rect = Rectangle.Round (rect); }

    /// <summary>
    ///     Initializes a new <see cref="Region"/> from the specified data.
    /// </summary>
    /// <param name="regionData"></param>
    public Region (RegionData regionData)
    {
        ArgumentNullException.ThrowIfNull (regionData);

        Region region = CreateRegionFromRegionData (regionData.Data);
        _rect = region._rect;
    }

    #endregion

    #region Union

    /// <summary>
    ///     Calculates from the <see cref="Region"/> array to the union and the specified
    ///     <see cref="Rectangle"/> or <see cref="RectangleF"/> structure of each other.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public static Rectangle Union (Region [] regions)
    {
        ArgumentNullException.ThrowIfNull (regions);

        var aggregatedArea = Rectangle.Empty;

        foreach (Region r in regions)
        {
            aggregatedArea = aggregatedArea == Rectangle.Empty ? r._rect : Rectangle.Union (aggregatedArea, r._rect);
        }

        return aggregatedArea;
    }

    /// <summary>
    ///     Calculates from the <see cref="Region"/> <see cref="HashSet{T}"/> to the union and the specified
    ///     <see cref="Rectangle"/> or <see cref="RectangleF"/> structure of each other.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public static Rectangle Union (HashSet<Region> regions)
    {
        ArgumentNullException.ThrowIfNull (regions);

        return Union (regions.ToArray ());
    }

    /// <summary>
    ///     Updates this <see cref="Region"/> to the union of itself and the specified
    ///     <see cref="Rectangle"/> or <see cref="RectangleF"/> structure.
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public Rectangle Union (Rectangle rect)
    {
        _rect = Rectangle.Union (_rect, Rectangle.Round (rect));

        return _rect;
    }

    /// <summary>
    ///     Updates this <see cref="Region"/> to the union of itself and the specified <see cref="Region"/>.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public Rectangle Union (Region region)
    {
        ArgumentNullException.ThrowIfNull (region);

        return Union (region._rect);
    }

    /// <summary>
    ///     Gets a <see cref="Rectangle"/> structure that represents a rectangle that bounds this <see cref="Region"/> on the
    ///     drawing surface of a <see cref="View"/> array.
    /// </summary>
    /// <param name="views"></param>
    /// <returns></returns>
    public static Rectangle GetViewsBounds (View [] views) { return Union (views.Select (c => new Region (c.Viewport)).ToArray ()); }

    #endregion

    #region Intersect

    /// <summary>
    ///     Calculates from the <see cref="Region"/> array to the intersection and the specified
    ///     <see cref="Rectangle"/> or <see cref="RectangleF"/> structure of each other.
    /// </summary>
    /// <param name="regions"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static Rectangle Intersect (Region [] regions, Rectangle rect)
    {
        ArgumentNullException.ThrowIfNull (regions);

        var aggregatedArea = Rectangle.Empty;

        foreach (Region r in regions)
        {
            aggregatedArea = Rectangle.Intersect (rect, r._rect);

            if (aggregatedArea != Rectangle.Empty)
            {
                break;
            }
        }

        return aggregatedArea;
    }

    /// <summary>
    ///     Calculates from the <see cref="Region"/> <see cref="HashSet{T}"/> to the intersection and the specified
    ///     <see cref="Rectangle"/> or <see cref="RectangleF"/> structure of each other.
    /// </summary>
    /// <param name="regions"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static Rectangle Intersect (HashSet<Region> regions, Rectangle rect)
    {
        ArgumentNullException.ThrowIfNull (regions);

        return Intersect (regions.ToArray (), rect);
    }

    /// <summary>
    ///     Updates this <see cref="Region"/> to the intersection of itself with the specified
    ///     <see cref="Rectangle"/> or <see cref="RectangleF"/> structure.
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public Rectangle Intersect (Rectangle rect)
    {
        _rect = Rectangle.Intersect (_rect, Rectangle.Round (rect));

        return _rect;
    }

    /// <summary>
    ///     Updates this <see cref="Region"/> to the intersection of itself with the specified <see cref="Region"/>.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public Rectangle Intersect (Region region)
    {
        ArgumentNullException.ThrowIfNull (region);

        return Intersect (region._rect);
    }

    #endregion

    /// <summary>
    ///     Verify if any of the <see cref="Region"/> <see cref="HashSet{T}"/> is contained on
    ///     the
    ///     <param name="x"></param>
    ///     and
    ///     <param name="y"></param>
    ///     coordinates.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public static bool Contains (HashSet<Region> regions, int x, int y)
    {
        foreach (Region region in regions)
        {
            Rectangle rect = region._rect;

            if (rect.Contains (x, y))
            {
                return true;
            }
        }

        return false;
    }

    #region RegionData

    /// <summary>
    ///     Creates a region that is defined by data obtained from another region.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Region CreateRegionFromRegionData (Rune [] data)
    {
        RectangleF rect = TextFormatter.CalcRect (0, 0, StringExtensions.ToString (data));

        return new (rect);
    }

    /// <summary>
    ///     Returns a <see cref="RegionData"/> that represents the information that describes this <see cref="Region"/>.
    /// </summary>
    /// <returns></returns>
    public RegionData? GetRegionData ()
    {
        var regionSize = (int)(((RectangleF)_rect).Width * ((RectangleF)_rect).Height);

        if (regionSize == 0)
        {
            return null;
        }

        var index = 0;
        Rune [] data = new Rune [regionSize];

        for (var y = (int)((RectangleF)_rect).Y; y < Math.Min ((int)((RectangleF)_rect).Height, Application.Driver.Rows); y++)
        {
            for (var x = (int)((RectangleF)_rect).X; x < Math.Min ((int)((RectangleF)_rect).Width, Application.Driver.Cols); x++)
            {
                Rune rune = Application.Driver.Contents [y, x].Rune;
                data [index] = rune;
                index++;

                if (rune.GetColumns () > 1)
                {
                    x++;
                }
            }
        }

        return new (data);
    }

    #endregion
}
