namespace Terminal.Gui.Drawing;

/// <summary>
///     Standalone, side-agnostic drawing helper that renders a rectangular protrusion (header) from one side of a
///     content border rectangle onto a <see cref="LineCanvas"/>. The header can be of arbitrary size; this class
///     handles only the border geometry — content rendering is the caller's responsibility.
/// </summary>
/// <remarks>
///     This class has no dependency on <c>Border</c>, <c>BorderView</c>, or <c>View</c>.
///     It operates purely on <see cref="LineCanvas"/>.
/// </remarks>
internal static class TabHeaderRenderer
{
    /// <summary>
    ///     Adds header border lines AND the content border on the header side to <paramref name="lineCanvas"/>.
    ///     The caller must draw the other 3 sides of the content border but NOT the header side — this method
    ///     handles it (continuous when <paramref name="showSeparator"/> is <c>true</c>, split around the gap
    ///     when <c>false</c>).
    /// </summary>
    /// <param name="lineCanvas">The <see cref="LineCanvas"/> to add lines to.</param>
    /// <param name="contentBorderRect">
    ///     The rectangle of the content border (the single-cell-thick border surrounding the content area).
    /// </param>
    /// <param name="side">Which side of the content border the header protrudes from.</param>
    /// <param name="offset">
    ///     Offset along the border edge where the header starts (columns for Top/Bottom, rows for Left/Right).
    /// </param>
    /// <param name="length">
    ///     Total size of the header parallel to the border edge (including border cells on both ends).
    /// </param>
    /// <param name="depth">
    ///     Total size of the header perpendicular to the border edge (including border cells on both ends).
    ///     For a standard single-row tab header, this is 3 (top border + content row + closing border).
    /// </param>
    /// <param name="showSeparator">
    ///     If <c>true</c>, the closing line between header and content is drawn (producing junction glyphs via auto-join).
    ///     If <c>false</c>, the closing line is suppressed (open gap — the header visually merges with the content area).
    /// </param>
    /// <param name="lineStyle">The <see cref="LineStyle"/> for the header border lines.</param>
    /// <param name="lineAttribute">Optional <see cref="Attribute"/> for coloring the header border lines.</param>
    public static void AddLines (LineCanvas lineCanvas,
                                 Rectangle contentBorderRect,
                                 Side side,
                                 int offset,
                                 int length,
                                 int depth,
                                 bool showSeparator,
                                 LineStyle lineStyle,
                                 Attribute? lineAttribute = null)
    {
        if (length <= 0 || depth <= 0)
        {
            return;
        }

        Rectangle headerRect = ComputeHeaderRect (contentBorderRect, side, offset, length, depth);
        Rectangle viewBounds = ComputeViewBounds (contentBorderRect, side, depth);
        Rectangle clipped = Rectangle.Intersect (headerRect, viewBounds);

        if (clipped.IsEmpty)
        {
            return;
        }

        // Add the 3 outer header border lines
        AddOuterHeaderLines (lineCanvas, headerRect, clipped, side, lineStyle, lineAttribute);

        // Add the content border on the header side — either continuous (separator) or split (gap)
        AddTabSideContentBorder (lineCanvas, clipped, contentBorderRect, side, !showSeparator, lineStyle, lineAttribute);
    }

    /// <summary>
    ///     Computes the unclipped header rectangle for the given side, offset, length, and depth.
    /// </summary>
    /// <param name="contentBorderRect">The content border rectangle.</param>
    /// <param name="side">Which side the header protrudes from.</param>
    /// <param name="offset">Offset along the border edge.</param>
    /// <param name="length">Total size parallel to the border edge (including border cells).</param>
    /// <param name="depth">Total size perpendicular to the border edge (including border cells).</param>
    /// <returns>The unclipped header rectangle in absolute coordinates.</returns>
    public static Rectangle ComputeHeaderRect (Rectangle contentBorderRect, Side side, int offset, int length, int depth) =>
        side switch
        {
            Side.Top => new Rectangle (contentBorderRect.X + offset, contentBorderRect.Y - (depth - 1), length, depth),

            Side.Bottom => new Rectangle (contentBorderRect.X + offset, contentBorderRect.Bottom - 1, length, depth),

            Side.Left => new Rectangle (contentBorderRect.X - (depth - 1), contentBorderRect.Y + offset, depth, length),

            Side.Right => new Rectangle (contentBorderRect.Right - 1, contentBorderRect.Y + offset, depth, length),

            _ => Rectangle.Empty
        };

    /// <summary>
    ///     Computes the interior content area within a header rectangle, excluding border cells.
    ///     Accounts for clipping — only excludes a border cell on an edge that is actually drawn.
    /// </summary>
    /// <param name="headerRect">The unclipped header rectangle (from <see cref="ComputeHeaderRect"/>).</param>
    /// <param name="clipped">The clipped header rectangle (intersection with view bounds).</param>
    /// <param name="side">Which side the header protrudes from.</param>
    /// <returns>The interior content area where the caller can draw content.</returns>
    public static Rectangle GetContentArea (Rectangle headerRect, Rectangle clipped, Side side)
    {
        int left = clipped.X == headerRect.X ? clipped.X + 1 : clipped.X;
        int top = clipped.Y == headerRect.Y ? clipped.Y + 1 : clipped.Y;
        int right = clipped.Right == headerRect.Right ? clipped.Right - 1 : clipped.Right;
        int bottom = clipped.Bottom == headerRect.Bottom ? clipped.Bottom - 1 : clipped.Bottom;

        // The closing edge (adjacent to content border) is always excluded
        switch (side)
        {
            case Side.Top:
                bottom = clipped.Bottom == headerRect.Bottom ? clipped.Bottom - 1 : clipped.Bottom;

                break;

            case Side.Bottom:
                top = clipped.Y == headerRect.Y ? clipped.Y + 1 : clipped.Y;

                break;

            case Side.Left:
                right = clipped.Right == headerRect.Right ? clipped.Right - 1 : clipped.Right;

                break;

            case Side.Right:
                left = clipped.X == headerRect.X ? clipped.X + 1 : clipped.X;

                break;
        }

        int w = right - left;
        int h = bottom - top;

        if (w <= 0 || h <= 0)
        {
            return Rectangle.Empty;
        }

        return new Rectangle (left, top, w, h);
    }

    /// <summary>
    ///     Computes the full view bounds (content border + header protrusion area).
    /// </summary>
    private static Rectangle ComputeViewBounds (Rectangle contentBorderRect, Side side, int depth) =>
        side switch
        {
            Side.Top => new Rectangle (contentBorderRect.X, contentBorderRect.Y - (depth - 1), contentBorderRect.Width, contentBorderRect.Height + (depth - 1)),

            Side.Bottom => new Rectangle (contentBorderRect.X, contentBorderRect.Y, contentBorderRect.Width, contentBorderRect.Height + (depth - 1)),

            Side.Left => new Rectangle (contentBorderRect.X - (depth - 1),
                                        contentBorderRect.Y,
                                        contentBorderRect.Width + (depth - 1),
                                        contentBorderRect.Height),

            Side.Right => new Rectangle (contentBorderRect.X, contentBorderRect.Y, contentBorderRect.Width + (depth - 1), contentBorderRect.Height),

            _ => contentBorderRect
        };

    /// <summary>
    ///     Adds the outer border lines of the header (the sides NOT adjacent to the content border).
    ///     For depth >= 2, draws cap + two side edges. For depth == 1, the cap coincides with the
    ///     closing edge (handled by <see cref="AddTabSideContentBorder"/>), so only side edges are drawn.
    /// </summary>
    private static void AddOuterHeaderLines (LineCanvas lineCanvas,
                                             Rectangle headerRect,
                                             Rectangle clipped,
                                             Side side,
                                             LineStyle lineStyle,
                                             Attribute? attribute)
    {
        bool isDepth1 = side is Side.Top or Side.Bottom ? clipped.Height == 1 : clipped.Width == 1;

        switch (side)
        {
            case Side.Top:
                // Cap line — skip for depth=1 (cap coincides with content border)
                if (!isDepth1 && clipped.Y == headerRect.Y)
                {
                    lineCanvas.AddLine (new Point (clipped.X, clipped.Y), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                // Left edge — for depth=1, extend 1 cell outward for correct corner auto-join
                if (clipped.X == headerRect.X)
                {
                    int startY = isDepth1 ? clipped.Y - 1 : clipped.Y;
                    int height = isDepth1 ? 2 : clipped.Height;
                    lineCanvas.AddLine (new Point (clipped.X, startY), height, Orientation.Vertical, lineStyle, attribute);
                }

                // Right edge
                if (clipped.Right == headerRect.Right)
                {
                    int startY = isDepth1 ? clipped.Y - 1 : clipped.Y;
                    int height = isDepth1 ? 2 : clipped.Height;
                    lineCanvas.AddLine (new Point (clipped.Right - 1, startY), height, Orientation.Vertical, lineStyle, attribute);
                }

                break;

            case Side.Bottom:
                // Cap line — skip for depth=1
                if (!isDepth1 && clipped.Bottom == headerRect.Bottom)
                {
                    lineCanvas.AddLine (new Point (clipped.X, clipped.Bottom - 1), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                // Left edge
                if (clipped.X == headerRect.X)
                {
                    int height = isDepth1 ? 2 : clipped.Height;
                    lineCanvas.AddLine (new Point (clipped.X, clipped.Y), height, Orientation.Vertical, lineStyle, attribute);
                }

                // Right edge
                if (clipped.Right == headerRect.Right)
                {
                    int height = isDepth1 ? 2 : clipped.Height;
                    lineCanvas.AddLine (new Point (clipped.Right - 1, clipped.Y), height, Orientation.Vertical, lineStyle, attribute);
                }

                break;

            case Side.Left:
                // Cap line — skip for depth=1
                if (!isDepth1 && clipped.X == headerRect.X)
                {
                    lineCanvas.AddLine (new Point (clipped.X, clipped.Y), clipped.Height, Orientation.Vertical, lineStyle, attribute);
                }

                // Top edge — skip for depth=1 (content top border handles this)
                if (!isDepth1 && clipped.Y == headerRect.Y)
                {
                    lineCanvas.AddLine (new Point (clipped.X, clipped.Y), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                // Bottom edge — skip for depth=1
                if (!isDepth1 && clipped.Bottom == headerRect.Bottom)
                {
                    lineCanvas.AddLine (new Point (clipped.X, clipped.Bottom - 1), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                break;

            case Side.Right:
                // Cap line — skip for depth=1
                if (!isDepth1 && clipped.Right == headerRect.Right)
                {
                    lineCanvas.AddLine (new Point (clipped.Right - 1, clipped.Y), clipped.Height, Orientation.Vertical, lineStyle, attribute);
                }

                // Top edge — skip for depth=1
                if (!isDepth1 && clipped.Y == headerRect.Y)
                {
                    lineCanvas.AddLine (new Point (clipped.X, clipped.Y), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                // Bottom edge — skip for depth=1
                if (!isDepth1 && clipped.Bottom == headerRect.Bottom)
                {
                    lineCanvas.AddLine (new Point (clipped.X, clipped.Bottom - 1), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                break;

            default: throw new ArgumentOutOfRangeException (nameof (side), side, null);
        }
    }

    /// <summary>
    ///     Adds the content border on the header side. When <paramref name="openGap"/> is <c>false</c>,
    ///     this is the full border line (producing junction glyphs where the header meets it via auto-join).
    ///     When <paramref name="openGap"/> is <c>true</c>, the border is split into two segments around
    ///     the gap, producing corner glyphs naturally.
    /// </summary>
    private static void AddTabSideContentBorder (LineCanvas lineCanvas,
                                                 Rectangle clipped,
                                                 Rectangle contentBorderRect,
                                                 Side side,
                                                 bool openGap,
                                                 LineStyle lineStyle,
                                                 Attribute? attribute)
    {
        switch (side)
        {
            case Side.Top:
            {
                int borderY = contentBorderRect.Y;

                if (!openGap)
                {
                    lineCanvas.AddLine (new Point (contentBorderRect.X, borderY), contentBorderRect.Width, Orientation.Horizontal, lineStyle, attribute);
                }
                else
                {
                    int headerLeft = clipped.X;
                    int headerRight = clipped.Right - 1;

                    // Left segment: from content left to headerLeft (inclusive for junction)
                    if (headerLeft > contentBorderRect.X)
                    {
                        lineCanvas.AddLine (new Point (contentBorderRect.X, borderY),
                                            headerLeft - contentBorderRect.X + 1,
                                            Orientation.Horizontal,
                                            lineStyle,
                                            attribute);
                    }

                    // Right segment: from headerRight (inclusive for junction) to content right
                    if (headerRight < contentBorderRect.Right - 1)
                    {
                        lineCanvas.AddLine (new Point (headerRight, borderY),
                                            contentBorderRect.Right - headerRight,
                                            Orientation.Horizontal,
                                            lineStyle,
                                            attribute);
                    }
                }

                break;
            }

            case Side.Bottom:
            {
                int borderY = contentBorderRect.Bottom - 1;

                if (!openGap)
                {
                    lineCanvas.AddLine (new Point (contentBorderRect.X, borderY), contentBorderRect.Width, Orientation.Horizontal, lineStyle, attribute);
                }
                else
                {
                    int headerLeft = clipped.X;
                    int headerRight = clipped.Right - 1;

                    if (headerLeft > contentBorderRect.X)
                    {
                        lineCanvas.AddLine (new Point (contentBorderRect.X, borderY),
                                            headerLeft - contentBorderRect.X + 1,
                                            Orientation.Horizontal,
                                            lineStyle,
                                            attribute);
                    }

                    if (headerRight < contentBorderRect.Right - 1)
                    {
                        lineCanvas.AddLine (new Point (headerRight, borderY),
                                            contentBorderRect.Right - headerRight,
                                            Orientation.Horizontal,
                                            lineStyle,
                                            attribute);
                    }
                }

                break;
            }

            case Side.Left:
            {
                int borderX = contentBorderRect.X;

                if (!openGap)
                {
                    lineCanvas.AddLine (new Point (borderX, contentBorderRect.Y), contentBorderRect.Height, Orientation.Vertical, lineStyle, attribute);
                }
                else
                {
                    int headerTop = clipped.Y;
                    int headerBottom = clipped.Bottom - 1;

                    if (headerTop > contentBorderRect.Y)
                    {
                        lineCanvas.AddLine (new Point (borderX, contentBorderRect.Y),
                                            headerTop - contentBorderRect.Y + 1,
                                            Orientation.Vertical,
                                            lineStyle,
                                            attribute);
                    }
                    else
                    {
                        lineCanvas.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Y, 1, 1)));
                    }

                    if (headerBottom < contentBorderRect.Bottom - 1)
                    {
                        lineCanvas.AddLine (new Point (borderX, headerBottom),
                                            contentBorderRect.Bottom - headerBottom,
                                            Orientation.Vertical,
                                            lineStyle,
                                            attribute);
                    }
                    else
                    {
                        lineCanvas.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Bottom - 1, 1, 1)));
                    }
                }

                break;
            }

            case Side.Right:
            {
                int borderX = contentBorderRect.Right - 1;

                if (!openGap)
                {
                    lineCanvas.AddLine (new Point (borderX, contentBorderRect.Y), contentBorderRect.Height, Orientation.Vertical, lineStyle, attribute);
                }
                else
                {
                    int headerTop = clipped.Y;
                    int headerBottom = clipped.Bottom - 1;

                    if (headerTop > contentBorderRect.Y)
                    {
                        lineCanvas.AddLine (new Point (borderX, contentBorderRect.Y),
                                            headerTop - contentBorderRect.Y + 1,
                                            Orientation.Vertical,
                                            lineStyle,
                                            attribute);
                    }
                    else
                    {
                        // Gap reaches the top corner — suppress it so adjacent border doesn't show
                        lineCanvas.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Y, 1, 1)));
                    }

                    if (headerBottom < contentBorderRect.Bottom - 1)
                    {
                        lineCanvas.AddLine (new Point (borderX, headerBottom),
                                            contentBorderRect.Bottom - headerBottom,
                                            Orientation.Vertical,
                                            lineStyle,
                                            attribute);
                    }
                    else
                    {
                        // Gap reaches the bottom corner — suppress it so adjacent border doesn't show
                        lineCanvas.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Bottom - 1, 1, 1)));
                    }
                }

                break;
            }

            default: throw new ArgumentOutOfRangeException (nameof (side), side, null);
        }
    }
}
