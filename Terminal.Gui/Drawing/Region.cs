#nullable enable
namespace Terminal.Gui;

/// <summary>
/// Describes the interior of a screen shape composed of rectangles and paths. This class cannot be inherited.
/// </summary>
public sealed class Region : IDisposable
{
    private dynamic _rect;

    #region Constructors

    /// <summary>
    /// Initializes a new <see cref="Region"/>.
    /// </summary>
    public Region () : this (Rectangle.Empty) { }

    /// <summary>
    /// Initializes a new <see cref="Region"/> from the specified <see cref="Rectangle"/> structure.
    /// </summary>
    /// <param name="rect"></param>
    public Region (Rectangle rect)
    {
        _rect = rect;
    }

    /// <summary>
    /// Initializes a new <see cref="Region"/> from the specified <see cref="RectangleF"/> structure.
    /// </summary>
    /// <param name="rect"></param>
    public Region (RectangleF rect)
    {
        _rect = rect;
    }

    /// <summary>
    /// <summary>
    /// Destructor for disposing this.
    /// </summary>
    ~Region () { Dispose (); }

    #endregion

    /// <inheritdoc />
    public void Dispose ()
    {
        GC.SuppressFinalize (this);
    }

    /// <inheritdoc />
    public override bool Equals (object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        dynamic thisRect = (RectangleF)_rect;
        dynamic otherRect = (RectangleF)((Region)obj)._rect;

        return thisRect == otherRect;
    }

    /// <inheritdoc />
    public override int GetHashCode () { return HashCode.Combine (_rect); }

    #region Rectangle, RectangleF

    /// <summary>
    /// Calculates from the <see cref="Region"/> array to the union and the specified
    /// <see cref="Rectangle"/> or <see cref="RectangleF"/> structure of each other.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public static dynamic? Union (Region [] regions)
    {
        ArgumentNullException.ThrowIfNull (regions);

        dynamic? rBase = null;

        foreach (Region r in regions)
        {
            if (rBase is null)
            {
                rBase = r._rect;
                continue;
            }
            if (rBase.GetType ().Name == "Rectangle")
            {
                Rectangle rRect = r._rect.GetType ().Name == "RectangleF" ? Rectangle.Round (r._rect) : r._rect;
                rBase = Rectangle.Union (rBase, rRect);
            }
            else
            {
                RectangleF rRect = r._rect;
                rBase = RectangleF.Union (rBase, rRect);
            }
        }

        return rBase;
    }

    /// <summary>
    /// Calculates from the <see cref="Region"/> <see cref="HashSet{T}"/> to the union and the specified
    /// <see cref="Rectangle"/> or <see cref="RectangleF"/> structure of each other.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public static dynamic? Union (HashSet<Region> regions)
    {
        ArgumentNullException.ThrowIfNull (regions);

        return Union (regions.ToArray ());
    }

    /// <summary>
    /// Updates this <see cref="Region"/> to the union of itself and the specified
    /// <see cref="Rectangle"/> or <see cref="RectangleF"/> structure.
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public dynamic Union (dynamic rect)
    {
        _rect = _rect.GetType ().Name == "Rectangle" ? Rectangle.Union (_rect, Rectangle.Round (rect)) : RectangleF.Union (_rect, (RectangleF)rect);

        return _rect;
    }

    /// <summary>
    /// Updates this <see cref="Region"/> to the union of itself and the specified <see cref="Region"/>.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public dynamic Union (Region region)
    {
        ArgumentNullException.ThrowIfNull (region);

        return Union (region._rect);
    }

    /// <summary>
    /// Gets a <see cref="Rectangle"/> structure that represents a rectangle that bounds this <see cref="Region"/> on the drawing surface of a <see cref="View"/> array.
    /// </summary>
    /// <param name="views"></param>
    /// <returns></returns>
    public static Rectangle GetViewsBounds (View [] views)
    {
        return Union (views.Select (c => new Region (c.Viewport)).ToArray ());
    }

    /// <summary>
    /// Calculates from the <see cref="Region"/> array to the intersection and the specified
    /// <see cref="Rectangle"/> or <see cref="RectangleF"/> structure of each other.
    /// </summary>
    /// <param name="regions"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static dynamic? Intersect (Region [] regions, dynamic rect)
    {
        ArgumentNullException.ThrowIfNull (regions);

        dynamic? rBase = null;

        foreach (Region r in regions)
        {
            if (r._rect.GetType ().Name == "Rectangle")
            {
                rBase = Rectangle.Intersect (rect, r._rect);

                if ((Rectangle)rBase != Rectangle.Empty)
                {
                    break;
                }
            }
            else
            {
                rBase = RectangleF.Intersect (rect, r._rect);

                if ((RectangleF)rBase != RectangleF.Empty)
                {
                    break;
                }
            }

        }

        return rBase;
    }

    /// <summary>
    /// Calculates from the <see cref="Region"/> <see cref="HashSet{T}"/> to the intersection and the specified
    /// <see cref="Rectangle"/> or <see cref="RectangleF"/> structure of each other.
    /// </summary>
    /// <param name="regions"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static dynamic Intersect (HashSet<Region> regions, dynamic rect)
    {
        ArgumentNullException.ThrowIfNull (regions);

        return Intersect (regions.ToArray (), rect);
    }

    /// <summary>
    /// Updates this <see cref="Region"/> to the intersection of itself with the specified
    /// <see cref="Rectangle"/> or <see cref="RectangleF"/> structure.
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public dynamic Intersect (dynamic rect)
    {
        _rect = _rect.GetType ().Name == "Rectangle" ? Rectangle.Intersect (_rect, Rectangle.Round (rect)) : RectangleF.Intersect (_rect, (RectangleF)rect);

        return _rect;
    }

    /// <summary>
    /// Updates this <see cref="Region"/> to the intersection of itself with the specified <see cref="Region"/>.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public dynamic Intersect (Region region)
    {
        ArgumentNullException.ThrowIfNull (region);

        return Intersect (region._rect);
    }

    /// <summary>
    /// Verify if any of the <see cref="Region"/> <see cref="HashSet{T}"/> is contained on
    /// the <param name="x"></param> and <param name="y"></param> coordinates.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public static bool Contains (HashSet<Region> regions, int x, int y)
    {
        foreach (Region region in regions)
        {
            dynamic rect = region._rect;

            if (rect.Contains (x, y))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    /// <summary>
    /// Creates an exact copy of this <see cref="Region"/>.
    /// </summary>
    /// <returns></returns>
    public Region Clone ()
    {
        return new Region (_rect);
    }
}
