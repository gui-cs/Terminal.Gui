using System.Text;
using UnitTests;

namespace DrawingTests;

/// <summary>
///     TDD tests for <see cref="TabHeaderRenderer"/>. Each test creates a <see cref="LineCanvas"/>,
///     adds content border lines, calls <see cref="TabHeaderRenderer.AddLines"/> with geometry-only params,
///     then draws text manually in a post-canvas callback. The renderer has no text-drawing responsibility.
/// </summary>

// Copilot
public class TabHeaderRendererTests (ITestOutputHelper output) : TestDriverBase
{
    /// <summary>
    ///     Creates a View wired to a driver that renders a <see cref="LineCanvas"/> on draw.
    ///     The <paramref name="postCanvasDraw"/> action is called after the canvas map is rendered,
    ///     allowing text to be drawn on top of the line art.
    /// </summary>
    private (View view, LineCanvas canvas, IDriver driver) CreateCanvas (int width, int height, Action<View>? postCanvasDraw = null)
    {
        IDriver driver = CreateTestDriver (width, height);
        View v = new () { Width = width, Height = height, Viewport = new Rectangle (0, 0, width, height) };
        v.Driver = driver;

        LineCanvas canvas = new ();
        LineCanvas canvasCopy = canvas;

        v.DrawComplete += (_, _) =>
                          {
                              v.FillRect (v.Viewport);

                              foreach (KeyValuePair<Point, Rune> p in canvasCopy.GetMap ())
                              {
                                  v.AddRune (p.Key.X, p.Key.Y, p.Value);
                              }

                              canvasCopy.Clear ();
                              postCanvasDraw?.Invoke (v);
                          };

        return (v, canvas, driver);
    }

    /// <summary>
    ///     Adds a content border rectangle to the <see cref="LineCanvas"/>,
    ///     optionally skipping one side (the header side, which <see cref="TabHeaderRenderer"/> handles).
    /// </summary>
    private static void AddContentBorder (LineCanvas canvas, Rectangle rect, LineStyle style = LineStyle.Rounded, Side? skipSide = null)
    {
        if (skipSide != Side.Top)
        {
            canvas.AddLine (new Point (rect.X, rect.Y), rect.Width, Orientation.Horizontal, style);
        }

        if (skipSide != Side.Bottom)
        {
            canvas.AddLine (new Point (rect.X, rect.Bottom - 1), rect.Width, Orientation.Horizontal, style);
        }

        if (skipSide != Side.Left)
        {
            canvas.AddLine (new Point (rect.X, rect.Y), rect.Height, Orientation.Vertical, style);
        }

        if (skipSide != Side.Right)
        {
            canvas.AddLine (new Point (rect.Right - 1, rect.Y), rect.Height, Orientation.Vertical, style);
        }
    }

    /// <summary>
    ///     Draws text inside the header content area. For Top/Bottom, text is horizontal.
    ///     For Left/Right, text is vertical. This simulates what a caller would do after
    ///     the renderer draws border lines.
    /// </summary>
    private static void DrawTextInHeader (IDriver driver, Rectangle contentBorderRect, Side side, int offset, int length, int depth, string text)
    {
        Rectangle headerRect = TabHeaderRenderer.ComputeHeaderRect (contentBorderRect, side, offset, length, depth);

        Rectangle viewBounds = new (side == Side.Left ? contentBorderRect.X - (depth - 1) : contentBorderRect.X,
                                    side == Side.Top ? contentBorderRect.Y - (depth - 1) : contentBorderRect.Y,
                                    contentBorderRect.Width + (side is Side.Left or Side.Right ? depth - 1 : 0),
                                    contentBorderRect.Height + (side is Side.Top or Side.Bottom ? depth - 1 : 0));
        Rectangle clipped = Rectangle.Intersect (headerRect, viewBounds);

        if (clipped.IsEmpty)
        {
            return;
        }

        Rectangle contentArea = TabHeaderRenderer.GetContentArea (headerRect, clipped, side);

        if (contentArea.IsEmpty)
        {
            return;
        }

        switch (side)
        {
            case Side.Top:
            case Side.Bottom:
            {
                int textY = contentArea.Y;

                for (var i = 0; i < text.Length; i++)
                {
                    int x = contentArea.X + i;

                    if (x >= contentArea.Right)
                    {
                        break;
                    }

                    driver.Move (x, textY);
                    driver.AddRune ((Rune)text [i]);
                }

                break;
            }

            case Side.Left:
            case Side.Right:
            {
                int textX = contentArea.X;

                for (var i = 0; i < text.Length; i++)
                {
                    int y = contentArea.Y + i;

                    if (y >= contentArea.Bottom)
                    {
                        break;
                    }

                    driver.Move (textX, y);
                    driver.AddRune ((Rune)text [i]);
                }

                break;
            }
        }
    }

    #region Round 1 — Side.Top, showSeparator == true (closed)

    [Fact]
    public void Top_Unfocused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view => DrawTextInHeader (view.Driver!, contentRect, Side.Top, 0, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 0, 5, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───╮
                                              │Tab│
                                              ├───┴───╮
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Top_Unfocused_TabOffset2 ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view => DrawTextInHeader (view.Driver!, contentRect, Side.Top, 2, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 2, 5, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                                ╭───╮
                                                │Tab│
                                              ╭─┴───┴─╮
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Top_Unfocused_OverflowRight ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view => DrawTextInHeader (view.Driver!, contentRect, Side.Top, 5, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 5, 5, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        // Right side of header is open (clipped off-screen)
        DriverAssert.AssertDriverContentsAre ("""
                                                   ╭───
                                                   │Tab
                                              ╭────┴──╮
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    #endregion

    #region Round 2 — Side.Top, showSeparator == false (open gap)

    [Fact]
    public void Top_Focused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view => DrawTextInHeader (view.Driver!, contentRect, Side.Top, 0, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 0, 5, 3, false, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───╮
                                              │Tab│
                                              │   ╰───╮
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Top_Focused_TabOffset2 ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view => DrawTextInHeader (view.Driver!, contentRect, Side.Top, 2, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 2, 5, 3, false, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                                ╭───╮
                                                │Tab│
                                              ╭─╯   ╰─╮
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    #endregion

    #region Round 3 — Side.Bottom

    [Fact]
    public void Bottom_Unfocused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 0, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 7, view => DrawTextInHeader (view.Driver!, contentRect, Side.Bottom, 0, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Bottom);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Bottom, 0, 5, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────╮
                                              │       │
                                              │       │
                                              ├───┬───╯
                                              │Tab│
                                              ╰───╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Bottom_Focused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 0, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 7, view => DrawTextInHeader (view.Driver!, contentRect, Side.Bottom, 0, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Bottom);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Bottom, 0, 5, 3, false, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────╮
                                              │       │
                                              │       │
                                              │   ╭───╯
                                              │Tab│
                                              ╰───╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    #endregion

    #region Round 4 — Side.Left and Side.Right

    [Fact]
    public void Left_Unfocused_TabOffset0 ()
    {
        Rectangle contentRect = new (2, 0, 9, 9);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (11, 9, view => DrawTextInHeader (view.Driver!, contentRect, Side.Left, 0, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Left);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Left, 0, 5, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭─┬───────╮
                                              │T│       │
                                              │a│       │
                                              │b│       │
                                              ╰─┤       │
                                                │       │
                                                │       │
                                                │       │
                                                ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Right_Unfocused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 0, 9, 9);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (11, 9, view => DrawTextInHeader (view.Driver!, contentRect, Side.Right, 0, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Right);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Right, 0, 5, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────┬─╮
                                              │       │T│
                                              │       │a│
                                              │       │b│
                                              │       ├─╯
                                              │       │
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Right_Focused_Overflow ()
    {
        Rectangle contentRect = new (0, 0, 9, 9);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (11, 9, view => DrawTextInHeader (view.Driver!, contentRect, Side.Right, 6, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Right);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Right, 6, 5, 3, false, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        // Bottom of header is open (clipped). Gap suppresses corner at (8,8).
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────╮
                                              │       │
                                              │       │
                                              │       │
                                              │       │
                                              │       │
                                              │       ╰─╮
                                              │        T│
                                              ╰─────── a│
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    #endregion

    #region Round 5 — Edge Cases

    [Fact]
    public void Top_Focused_OverflowRight ()
    {
        // Header extends past right edge with open gap — right side open
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view => DrawTextInHeader (view.Driver!, contentRect, Side.Top, 5, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 5, 5, 3, false, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        // Right side open, gap from x=5 to right edge
        DriverAssert.AssertDriverContentsAre ("""
                                                   ╭───
                                                   │Tab
                                              ╭────╯  │
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Left_Focused_Overflow ()
    {
        // Header on left overflows bottom with open gap — bottom side open
        Rectangle contentRect = new (2, 0, 9, 9);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (11, 9, view => DrawTextInHeader (view.Driver!, contentRect, Side.Left, 6, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Left);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Left, 6, 5, 3, false, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        // Bottom of header is open (clipped). Gap suppresses corner at (2,8).
        DriverAssert.AssertDriverContentsAre ("""
                                                ╭───────╮
                                                │       │
                                                │       │
                                                │       │
                                                │       │
                                                │       │
                                              ╭─╯       │
                                              │T        │
                                              │a ───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Top_SingleCharText ()
    {
        // Single character header (length = 3: 1 border + 1 content + 1 border)
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view => DrawTextInHeader (view.Driver!, contentRect, Side.Top, 2, 3, 3, "X"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 2, 3, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        DriverAssert.AssertDriverContentsAre ("""
                                                ╭─╮
                                                │X│
                                              ╭─┴─┴───╮
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Top_FullyClipped ()
    {
        // Header entirely outside view bounds — nothing drawn
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6);

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 20, 5, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        // Only 3 content border sides drawn (no top, no header). Content starts at y=2.
        DriverAssert.AssertDriverContentsAre ("""
                                              │       │
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    #endregion

    #region Round 6 — Negative offset (overflow at start edge)

    [Fact]
    public void Top_Unfocused_NegativeOffset ()
    {
        // Offset = -1: header left side extends beyond content left border
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view => DrawTextInHeader (view.Driver!, contentRect, Side.Top, -1, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, -1, 5, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        // Left side of header is open (clipped off-screen)
        DriverAssert.AssertDriverContentsAre ("""
                                              ───╮
                                              Tab│
                                              ╭──┴────╮
                                              │       │
                                              │       │
                                              ╰───────╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    [Fact]
    public void Bottom_Unfocused_NegativeOffset ()
    {
        // Offset = -1: header left side extends beyond content left border
        Rectangle contentRect = new (0, 0, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 7, view => DrawTextInHeader (view.Driver!, contentRect, Side.Bottom, -1, 5, 3, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Bottom);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Bottom, -1, 5, 3, true, LineStyle.Rounded);

        v.Draw ();
        output.WriteLine (driver.ToString ());

        // Left side of header is open (clipped off-screen)
        DriverAssert.AssertDriverContentsAre ("""
                                              ╭───────╮
                                              │       │
                                              │       │
                                              ╰──┬────╯
                                              Tab│
                                              ───╯
                                              """,
                                              output,
                                              driver);

        v.Dispose ();
    }

    #endregion

    #region Round 7 — ComputeHeaderRect and GetContentArea

    [Fact]
    public void ComputeHeaderRect_Top ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);
        Rectangle headerRect = TabHeaderRenderer.ComputeHeaderRect (contentRect, Side.Top, 0, 5, 3);
        Assert.Equal (new Rectangle (0, 0, 5, 3), headerRect);
    }

    [Fact]
    public void ComputeHeaderRect_Bottom ()
    {
        Rectangle contentRect = new (0, 0, 9, 4);
        Rectangle headerRect = TabHeaderRenderer.ComputeHeaderRect (contentRect, Side.Bottom, 0, 5, 3);
        Assert.Equal (new Rectangle (0, 3, 5, 3), headerRect);
    }

    [Fact]
    public void ComputeHeaderRect_Left ()
    {
        Rectangle contentRect = new (2, 0, 9, 9);
        Rectangle headerRect = TabHeaderRenderer.ComputeHeaderRect (contentRect, Side.Left, 0, 5, 3);
        Assert.Equal (new Rectangle (0, 0, 3, 5), headerRect);
    }

    [Fact]
    public void ComputeHeaderRect_Right ()
    {
        Rectangle contentRect = new (0, 0, 9, 9);
        Rectangle headerRect = TabHeaderRenderer.ComputeHeaderRect (contentRect, Side.Right, 0, 5, 3);
        Assert.Equal (new Rectangle (8, 0, 3, 5), headerRect);
    }

    [Fact]
    public void GetContentArea_Unclipped ()
    {
        Rectangle headerRect = new (0, 0, 5, 3);
        Rectangle clipped = headerRect;
        Rectangle content = TabHeaderRenderer.GetContentArea (headerRect, clipped, Side.Top);
        Assert.Equal (new Rectangle (1, 1, 3, 1), content);
    }

    [Fact]
    public void GetContentArea_ClippedRight ()
    {
        Rectangle headerRect = new (5, 0, 5, 3);
        Rectangle clipped = new (5, 0, 4, 3); // right edge clipped
        Rectangle content = TabHeaderRenderer.GetContentArea (headerRect, clipped, Side.Top);

        // Left border drawn (clipped.X == headerRect.X), right border NOT drawn, closing (bottom) excluded
        Assert.Equal (new Rectangle (6, 1, 3, 1), content);
    }

    [Fact]
    public void ComputeHeaderRect_ArbitraryDepth ()
    {
        // depth=5 means the header protrudes 5 cells (including both border edges)
        Rectangle contentRect = new (0, 4, 9, 4);
        Rectangle headerRect = TabHeaderRenderer.ComputeHeaderRect (contentRect, Side.Top, 0, 5, 5);
        Assert.Equal (new Rectangle (0, 0, 5, 5), headerRect);
    }

    #endregion
}
