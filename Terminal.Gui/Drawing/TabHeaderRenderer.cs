using Terminal.Gui.ViewBase;

namespace Terminal.Gui.Drawing;

/// <summary>
///     Standalone, side-agnostic drawing helper that renders a tab header on a <see cref="LineCanvas"/>.
///     The tab header is a rectangular protrusion from one side of a content border rectangle.
///     It supports all four <see cref="Side"/> values, <c>HasFocus</c> (open/closed gap), and overflow clipping.
/// </summary>
/// <remarks>
///     This class has no dependency on <c>Border</c>, <c>BorderView</c>, or <c>View</c>.
///     It operates purely on <see cref="LineCanvas"/> and <see cref="TextFormatter"/>.
/// </remarks>
internal static class TabHeaderRenderer
{
    /// <summary>
    ///     Adds tab header lines AND the content border on the tab side to <paramref name="lineCanvas"/>.
    ///     The caller must draw the other 3 sides of the content border but NOT the tab side — this method
    ///     handles it (continuous for unfocused, split around the gap for focused).
    /// </summary>
    /// <param name="lineCanvas">The <see cref="LineCanvas"/> to add lines to.</param>
    /// <param name="contentBorderRect">
    ///     The rectangle of the content border (the single-cell-thick border surrounding the content area).
    /// </param>
    /// <param name="side">Which side of the content border the tab header protrudes from.</param>
    /// <param name="tabOffset">
    ///     Offset along the border edge where the header starts (columns for Top/Bottom, rows for Left/Right).
    /// </param>
    /// <param name="tabText">The text to display in the tab header.</param>
    /// <param name="hasFocus">
    ///     If <c>true</c>, the closing line between header and content is suppressed (open gap).
    ///     If <c>false</c>, the closing line is drawn (closed tab, junctions via auto-join).
    /// </param>
    /// <param name="lineStyle">The <see cref="LineStyle"/> for the header border lines.</param>
    /// <param name="attribute">Optional <see cref="Attribute"/> for coloring the header lines.</param>
    public static void AddLines (
        LineCanvas lineCanvas,
        Rectangle contentBorderRect,
        Side side,
        int tabOffset,
        string tabText,
        bool hasFocus,
        LineStyle lineStyle,
        Attribute? attribute = null)
    {
        if (string.IsNullOrEmpty (tabText))
        {
            return;
        }

        int textLen = tabText.GetColumns ();

        if (textLen == 0)
        {
            return;
        }

        Rectangle headerRect = ComputeHeaderRect (contentBorderRect, side, tabOffset, textLen);
        Rectangle viewBounds = ComputeViewBounds (contentBorderRect, side, textLen);
        Rectangle clipped = Rectangle.Intersect (headerRect, viewBounds);

        if (clipped.IsEmpty)
        {
            return;
        }

        // Add the 3 outer header border lines
        AddOuterHeaderLines (lineCanvas, headerRect, clipped, side, lineStyle, attribute);

        // Add the content border on the tab side — either continuous (unfocused) or split (focused)
        AddTabSideContentBorder (lineCanvas, headerRect, clipped, contentBorderRect, side, hasFocus, lineStyle, attribute);
    }

    /// <summary>
    ///     Draws the tab text inside the header rectangle. Call this AFTER the <see cref="LineCanvas"/> has been
    ///     rendered to the driver so text appears on top of line art.
    /// </summary>
    /// <param name="driver">The <see cref="IDriver"/> to draw text to.</param>
    /// <param name="contentBorderRect">The content border rectangle (same as passed to <see cref="AddLines"/>).</param>
    /// <param name="side">Which side the header protrudes from.</param>
    /// <param name="tabOffset">Offset along the border edge.</param>
    /// <param name="tabText">The text to display.</param>
    /// <param name="attribute">Optional <see cref="Attribute"/> for text color.</param>
    public static void DrawText (
        IDriver driver,
        Rectangle contentBorderRect,
        Side side,
        int tabOffset,
        string tabText,
        Attribute? attribute = null)
    {
        if (string.IsNullOrEmpty (tabText))
        {
            return;
        }

        int textLen = tabText.GetColumns ();

        if (textLen == 0)
        {
            return;
        }

        Rectangle headerRect = ComputeHeaderRect (contentBorderRect, side, tabOffset, textLen);
        Rectangle viewBounds = ComputeViewBounds (contentBorderRect, side, textLen);
        Rectangle clipped = Rectangle.Intersect (headerRect, viewBounds);

        if (clipped.IsEmpty)
        {
            return;
        }

        DrawTabText (driver, headerRect, clipped, side, tabText, attribute);
    }

    /// <summary>
    ///     Convenience method that calls <see cref="AddLines"/> and optionally <see cref="DrawText"/>.
    ///     Note: If the text is drawn before the LineCanvas is rendered, it will be overwritten.
    ///     Use <see cref="AddLines"/> + <see cref="DrawText"/> separately when you control render order.
    /// </summary>
    public static void Render (
        LineCanvas lineCanvas,
        Rectangle contentBorderRect,
        Side side,
        int tabOffset,
        string tabText,
        bool hasFocus,
        LineStyle lineStyle,
        Attribute? attribute = null,
        IDriver? driver = null,
        Attribute? textAttribute = null)
    {
        AddLines (lineCanvas, contentBorderRect, side, tabOffset, tabText, hasFocus, lineStyle, attribute);

        if (driver is { })
        {
            DrawText (driver, contentBorderRect, side, tabOffset, tabText, textAttribute ?? attribute);
        }
    }

    /// <summary>
    ///     Computes the unclipped header rectangle for the given side, offset, and text length.
    /// </summary>
    private static Rectangle ComputeHeaderRect (Rectangle contentBorderRect, Side side, int tabOffset, int textLen)
    {
        // Header dimensions: for Top/Bottom, width = textLen + 2, height = 3
        // For Left/Right, width = 3, height = textLen + 2
        return side switch
        {
            Side.Top => new Rectangle (
                contentBorderRect.X + tabOffset,
                contentBorderRect.Y - 2,
                textLen + 2,
                3),

            Side.Bottom => new Rectangle (
                contentBorderRect.X + tabOffset,
                contentBorderRect.Bottom - 1,
                textLen + 2,
                3),

            Side.Left => new Rectangle (
                contentBorderRect.X - 2,
                contentBorderRect.Y + tabOffset,
                3,
                textLen + 2),

            Side.Right => new Rectangle (
                contentBorderRect.Right - 1,
                contentBorderRect.Y + tabOffset,
                3,
                textLen + 2),

            _ => Rectangle.Empty
        };
    }

    /// <summary>
    ///     Computes the full view bounds (content border + header protrusion area).
    /// </summary>
    private static Rectangle ComputeViewBounds (Rectangle contentBorderRect, Side side, int textLen)
    {
        return side switch
        {
            Side.Top => new Rectangle (
                contentBorderRect.X,
                contentBorderRect.Y - 2,
                contentBorderRect.Width,
                contentBorderRect.Height + 2),

            Side.Bottom => new Rectangle (
                contentBorderRect.X,
                contentBorderRect.Y,
                contentBorderRect.Width,
                contentBorderRect.Height + 2),

            Side.Left => new Rectangle (
                contentBorderRect.X - 2,
                contentBorderRect.Y,
                contentBorderRect.Width + 2,
                contentBorderRect.Height),

            Side.Right => new Rectangle (
                contentBorderRect.X,
                contentBorderRect.Y,
                contentBorderRect.Width + 2,
                contentBorderRect.Height),

            _ => contentBorderRect
        };
    }

    /// <summary>
    ///     Adds the 3 outer border lines of the header (the sides NOT adjacent to the content border).
    /// </summary>
    private static void AddOuterHeaderLines (
        LineCanvas lineCanvas,
        Rectangle headerRect,
        Rectangle clipped,
        Side side,
        LineStyle lineStyle,
        Attribute? attribute)
    {
        switch (side)
        {
            case Side.Top:
                // Top edge of header (the outermost line)
                if (clipped.Y == headerRect.Y)
                {
                    lineCanvas.AddLine (
                        new Point (clipped.X, clipped.Y),
                        clipped.Width,
                        Orientation.Horizontal,
                        lineStyle,
                        attribute);
                }

                // Left edge of header
                lineCanvas.AddLine (
                    new Point (clipped.X, clipped.Y),
                    clipped.Height,
                    Orientation.Vertical,
                    lineStyle,
                    attribute);

                // Right edge of header
                lineCanvas.AddLine (
                    new Point (clipped.Right - 1, clipped.Y),
                    clipped.Height,
                    Orientation.Vertical,
                    lineStyle,
                    attribute);

                break;

            case Side.Bottom:
                // Bottom edge of header (the outermost line)
                if (clipped.Bottom == headerRect.Bottom)
                {
                    lineCanvas.AddLine (
                        new Point (clipped.X, clipped.Bottom - 1),
                        clipped.Width,
                        Orientation.Horizontal,
                        lineStyle,
                        attribute);
                }

                // Left edge of header
                lineCanvas.AddLine (
                    new Point (clipped.X, clipped.Y),
                    clipped.Height,
                    Orientation.Vertical,
                    lineStyle,
                    attribute);

                // Right edge of header
                lineCanvas.AddLine (
                    new Point (clipped.Right - 1, clipped.Y),
                    clipped.Height,
                    Orientation.Vertical,
                    lineStyle,
                    attribute);

                break;

            case Side.Left:
                // Left edge of header (the outermost line)
                if (clipped.X == headerRect.X)
                {
                    lineCanvas.AddLine (
                        new Point (clipped.X, clipped.Y),
                        clipped.Height,
                        Orientation.Vertical,
                        lineStyle,
                        attribute);
                }

                // Top edge of header
                lineCanvas.AddLine (
                    new Point (clipped.X, clipped.Y),
                    clipped.Width,
                    Orientation.Horizontal,
                    lineStyle,
                    attribute);

                // Bottom edge of header
                if (clipped.Bottom == headerRect.Bottom)
                {
                    lineCanvas.AddLine (
                        new Point (clipped.X, clipped.Bottom - 1),
                        clipped.Width,
                        Orientation.Horizontal,
                        lineStyle,
                        attribute);
                }

                break;

            case Side.Right:
                // Right edge of header (the outermost line)
                if (clipped.Right == headerRect.Right)
                {
                    lineCanvas.AddLine (
                        new Point (clipped.Right - 1, clipped.Y),
                        clipped.Height,
                        Orientation.Vertical,
                        lineStyle,
                        attribute);
                }

                // Top edge of header
                lineCanvas.AddLine (
                    new Point (clipped.X, clipped.Y),
                    clipped.Width,
                    Orientation.Horizontal,
                    lineStyle,
                    attribute);

                // Bottom edge of header
                if (clipped.Bottom == headerRect.Bottom)
                {
                    lineCanvas.AddLine (
                        new Point (clipped.X, clipped.Bottom - 1),
                        clipped.Width,
                        Orientation.Horizontal,
                        lineStyle,
                        attribute);
                }

                break;
        }
    }

    /// <summary>
    ///     Adds the content border on the tab side. For unfocused tabs, this is the full border line
    ///     (producing junction glyphs where the header meets it via auto-join). For focused tabs,
    ///     the border is split into two segments around the gap, producing corner glyphs naturally.
    /// </summary>
    private static void AddTabSideContentBorder (
        LineCanvas lineCanvas,
        Rectangle headerRect,
        Rectangle clipped,
        Rectangle contentBorderRect,
        Side side,
        bool hasFocus,
        LineStyle lineStyle,
        Attribute? attribute)
    {
        switch (side)
        {
            case Side.Top:
            {
                int borderY = contentBorderRect.Y;

                if (!hasFocus)
                {
                    lineCanvas.AddLine (
                        new Point (contentBorderRect.X, borderY),
                        contentBorderRect.Width,
                        Orientation.Horizontal,
                        lineStyle, attribute);
                }
                else
                {
                    int headerLeft = clipped.X;
                    int headerRight = clipped.Right - 1;

                    // Left segment: from content left to headerLeft (inclusive for junction)
                    if (headerLeft > contentBorderRect.X)
                    {
                        lineCanvas.AddLine (
                            new Point (contentBorderRect.X, borderY),
                            headerLeft - contentBorderRect.X + 1,
                            Orientation.Horizontal,
                            lineStyle, attribute);
                    }

                    // Right segment: from headerRight (inclusive for junction) to content right
                    if (headerRight < contentBorderRect.Right - 1)
                    {
                        lineCanvas.AddLine (
                            new Point (headerRight, borderY),
                            contentBorderRect.Right - headerRight,
                            Orientation.Horizontal,
                            lineStyle, attribute);
                    }
                }

                break;
            }

            case Side.Bottom:
            {
                int borderY = contentBorderRect.Bottom - 1;

                if (!hasFocus)
                {
                    lineCanvas.AddLine (
                        new Point (contentBorderRect.X, borderY),
                        contentBorderRect.Width,
                        Orientation.Horizontal,
                        lineStyle, attribute);
                }
                else
                {
                    int headerLeft = clipped.X;
                    int headerRight = clipped.Right - 1;

                    if (headerLeft > contentBorderRect.X)
                    {
                        lineCanvas.AddLine (
                            new Point (contentBorderRect.X, borderY),
                            headerLeft - contentBorderRect.X + 1,
                            Orientation.Horizontal,
                            lineStyle, attribute);
                    }

                    if (headerRight < contentBorderRect.Right - 1)
                    {
                        lineCanvas.AddLine (
                            new Point (headerRight, borderY),
                            contentBorderRect.Right - headerRight,
                            Orientation.Horizontal,
                            lineStyle, attribute);
                    }
                }

                break;
            }

            case Side.Left:
            {
                int borderX = contentBorderRect.X;

                if (!hasFocus)
                {
                    lineCanvas.AddLine (
                        new Point (borderX, contentBorderRect.Y),
                        contentBorderRect.Height,
                        Orientation.Vertical,
                        lineStyle, attribute);
                }
                else
                {
                    int headerTop = clipped.Y;
                    int headerBottom = clipped.Bottom - 1;

                    if (headerTop > contentBorderRect.Y)
                    {
                        lineCanvas.AddLine (
                            new Point (borderX, contentBorderRect.Y),
                            headerTop - contentBorderRect.Y + 1,
                            Orientation.Vertical,
                            lineStyle, attribute);
                    }
                    else
                    {
                        lineCanvas.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Y, 1, 1)));
                    }

                    if (headerBottom < contentBorderRect.Bottom - 1)
                    {
                        lineCanvas.AddLine (
                            new Point (borderX, headerBottom),
                            contentBorderRect.Bottom - headerBottom,
                            Orientation.Vertical,
                            lineStyle, attribute);
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

                if (!hasFocus)
                {
                    lineCanvas.AddLine (
                        new Point (borderX, contentBorderRect.Y),
                        contentBorderRect.Height,
                        Orientation.Vertical,
                        lineStyle, attribute);
                }
                else
                {
                    int headerTop = clipped.Y;
                    int headerBottom = clipped.Bottom - 1;

                    if (headerTop > contentBorderRect.Y)
                    {
                        lineCanvas.AddLine (
                            new Point (borderX, contentBorderRect.Y),
                            headerTop - contentBorderRect.Y + 1,
                            Orientation.Vertical,
                            lineStyle, attribute);
                    }
                    else
                    {
                        // Gap reaches the top corner — suppress it so adjacent border doesn't show
                        lineCanvas.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Y, 1, 1)));
                    }

                    if (headerBottom < contentBorderRect.Bottom - 1)
                    {
                        lineCanvas.AddLine (
                            new Point (borderX, headerBottom),
                            contentBorderRect.Bottom - headerBottom,
                            Orientation.Vertical,
                            lineStyle, attribute);
                    }
                    else
                    {
                        // Gap reaches the bottom corner — suppress it so adjacent border doesn't show
                        lineCanvas.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Bottom - 1, 1, 1)));
                    }
                }

                break;
            }
        }
    }

    /// <summary>
    ///     Draws the tab text inside the header rectangle.
    ///     For Top/Bottom, text is horizontal. For Left/Right, text is vertical.
    /// </summary>
    private static void DrawTabText (
        IDriver driver,
        Rectangle headerRect,
        Rectangle clipped,
        Side side,
        string tabText,
        Attribute? attribute)
    {
        if (attribute is { })
        {
            driver.SetAttribute (attribute.Value);
        }

        switch (side)
        {
            case Side.Top:
            {
                int textY = headerRect.Y + 1;

                if (textY >= clipped.Y && textY < clipped.Bottom)
                {
                    int textX = headerRect.X + 1;

                    // Vertical borders are always drawn, so always exclude them
                    int clipLeft = Math.Max (clipped.X + 1, headerRect.X + 1);
                    int clipRight = clipped.Right - 1;

                    for (var i = 0; i < tabText.Length; i++)
                    {
                        int x = textX + i;

                        if (x >= clipLeft && x < clipRight)
                        {
                            driver.Move (x, textY);
                            driver.AddRune ((Rune)tabText [i]);
                        }
                    }
                }

                break;
            }

            case Side.Bottom:
            {
                int textY = headerRect.Bottom - 2;

                if (textY >= clipped.Y && textY < clipped.Bottom)
                {
                    int textX = headerRect.X + 1;

                    // Vertical borders are always drawn, so always exclude them
                    int clipLeft = Math.Max (clipped.X + 1, headerRect.X + 1);
                    int clipRight = clipped.Right - 1;

                    for (var i = 0; i < tabText.Length; i++)
                    {
                        int x = textX + i;

                        if (x >= clipLeft && x < clipRight)
                        {
                            driver.Move (x, textY);
                            driver.AddRune ((Rune)tabText [i]);
                        }
                    }
                }

                break;
            }

            case Side.Left:
            {
                int textX = headerRect.X + 1;

                if (textX >= clipped.X && textX < clipped.Right)
                {
                    int textY = headerRect.Y + 1;

                    // Top border always drawn. Bottom border only drawn when not clipped.
                    int clipTop = Math.Max (clipped.Y + 1, headerRect.Y + 1);
                    int clipBottom = clipped.Bottom == headerRect.Bottom ? clipped.Bottom - 1 : clipped.Bottom;

                    for (var i = 0; i < tabText.Length; i++)
                    {
                        int y = textY + i;

                        if (y >= clipTop && y < clipBottom)
                        {
                            driver.Move (textX, y);
                            driver.AddRune ((Rune)tabText [i]);
                        }
                    }
                }

                break;
            }

            case Side.Right:
            {
                int textX = headerRect.Right - 2;

                if (textX >= clipped.X && textX < clipped.Right)
                {
                    int textY = headerRect.Y + 1;

                    // Top border always drawn. Bottom border only drawn when not clipped.
                    int clipTop = Math.Max (clipped.Y + 1, headerRect.Y + 1);
                    int clipBottom = clipped.Bottom == headerRect.Bottom ? clipped.Bottom - 1 : clipped.Bottom;

                    for (var i = 0; i < tabText.Length; i++)
                    {
                        int y = textY + i;

                        if (y >= clipTop && y < clipBottom)
                        {
                            driver.Move (textX, y);
                            driver.AddRune ((Rune)tabText [i]);
                        }
                    }
                }

                break;
            }
        }
    }
}
