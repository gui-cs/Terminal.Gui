#nullable enable
namespace Terminal.Gui;

/// <summary>
/// Describes the interior of a screen shape composed of rectangles and paths. This class cannot be inherited.
/// </summary>
public sealed class Region : IDisposable
{
    private dynamic _rect;

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

    /// <inheritdoc />
    public void Dispose ()
    {
        throw new NotImplementedException ();
    }

    /// <inheritdoc />
    public override bool Equals (object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        dynamic thisRect;
        dynamic otherRect;

        if (_rect.GetType ().Name == "Rectangle")
        {
            thisRect = (Rectangle)_rect;
            otherRect = (Rectangle)((Region)obj)._rect;
        }
        else
        {
            thisRect = (RectangleF)_rect;
            otherRect = (RectangleF)((Region)obj)._rect;
        }

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
    public static dynamic Union (params Region [] regions)
    {
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

        if (rBase is { })
        {
            return rBase;
        }

        if (rBase.GetType ().Name == "Rectangle")
        {
            return Rectangle.Empty;
        }

        return RectangleF.Empty;
    }

    /// <summary>
    /// Calculates from the <see cref="Region"/> <see cref="HashSet{T}"/> to the union and the specified
    /// <see cref="Rectangle"/> or <see cref="RectangleF"/> structure of each other.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public static dynamic Union (HashSet<Region> regions)
    {
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
        _rect = rect.GetType ().Name == "Rectangle" ? Rectangle.Union ((Rectangle)_rect, rect) : RectangleF.Union ((RectangleF)_rect, rect);

        return _rect;
    }

    /// <summary>
    /// Updates this <see cref="Region"/> to the union of itself and the specified <see cref="Region"/>.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public dynamic Union (Region region)
    {
        return Union (region._rect);
    }

    /// <summary>
    /// Gets a <see cref="Rectangle"/> structure that represents a rectangle that bounds this <see cref="Region"/> on the drawing surface of a <see cref="View"/> array.
    /// </summary>
    /// <param name="views"></param>
    /// <returns></returns>
    public static Rectangle GetViewsBounds (params View [] views)
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
    public static dynamic Intersect (Region [] regions, dynamic rect)
    {
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

        if (rBase is { })
        {
            return rBase;
        }

        if (rBase.GetType ().Name == "Rectangle")
        {
            return Rectangle.Empty;
        }

        return RectangleF.Empty;
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
        _rect = rect.GetType ().Name == "Rectangle" ? Rectangle.Intersect ((Rectangle)_rect, rect) : RectangleF.Intersect ((RectangleF)_rect, rect);

        return _rect;
    }

    /// <summary>
    /// Updates this <see cref="Region"/> to the intersection of itself with the specified <see cref="Region"/>.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public dynamic Intersect (Region region)
    {
        return Intersect (region._rect);
    }

    #endregion
}
