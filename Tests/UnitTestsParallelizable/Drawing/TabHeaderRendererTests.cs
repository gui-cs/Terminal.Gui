using System.Text;
using UnitTests;

namespace DrawingTests;

/// <summary>
///     TDD tests for <see cref="TabHeaderRenderer"/>. Each test creates a <see cref="LineCanvas"/>,
///     adds content border lines, calls <see cref="TabHeaderRenderer.AddLines"/>, draws via a View,
///     then calls <see cref="TabHeaderRenderer.DrawText"/> in a post-canvas callback so text
///     renders on top of line art.
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
        View v = new () { Width = width, Height = height, Viewport = new (0, 0, width, height) };
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
    ///     optionally skipping one side (the tab side, which <see cref="TabHeaderRenderer"/> handles).
    /// </summary>
    private static void AddContentBorder (LineCanvas canvas, Rectangle rect, LineStyle style = LineStyle.Single, Side? skipSide = null)
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

    #region Round 1 — Side.Top, HasFocus == false

    [Fact]
    public void Top_Unfocused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Top, 0, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 0, "Tab", hasFocus: false, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
            ┌───┐
            │Tab│
            ├───┴───┐
            │       │
            │       │
            └───────┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    [Fact]
    public void Top_Unfocused_TabOffset2 ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Top, 2, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 2, "Tab", hasFocus: false, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
              ┌───┐
              │Tab│
            ┌─┴───┴─┐
            │       │
            │       │
            └───────┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    [Fact]
    public void Top_Unfocused_OverflowRight ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Top, 5, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 5, "Tab", hasFocus: false, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
                 ┌──┐
                 │Ta│
            ┌────┴──┤
            │       │
            │       │
            └───────┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    #endregion

    #region Round 2 — Side.Top, HasFocus == true

    [Fact]
    public void Top_Focused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Top, 0, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 0, "Tab", hasFocus: true, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
            ┌───┐
            │Tab│
            │   └───┐
            │       │
            │       │
            └───────┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    [Fact]
    public void Top_Focused_TabOffset2 ()
    {
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Top, 2, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 2, "Tab", hasFocus: true, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
              ┌───┐
              │Tab│
            ┌─┘   └─┐
            │       │
            │       │
            └───────┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    #endregion

    #region Round 3 — Side.Bottom

    [Fact]
    public void Bottom_Unfocused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 0, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 7, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Bottom, 0, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Bottom);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Bottom, 0, "Tab", hasFocus: false, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
            ┌───────┐
            │       │
            │       │
            ├───┬───┘
            │Tab│
            └───┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    [Fact]
    public void Bottom_Focused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 0, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 7, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Bottom, 0, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Bottom);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Bottom, 0, "Tab", hasFocus: true, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
            ┌───────┐
            │       │
            │       │
            │   ┌───┘
            │Tab│
            └───┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    #endregion

    #region Round 4 — Side.Left and Side.Right

    [Fact]
    public void Left_Unfocused_TabOffset0 ()
    {
        Rectangle contentRect = new (2, 0, 9, 9);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (11, 9, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Left, 0, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Left);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Left, 0, "Tab", hasFocus: false, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
            ┌─┬───────┐
            │T│       │
            │a│       │
            │b│       │
            └─┤       │
              │       │
              │       │
              │       │
              └───────┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    [Fact]
    public void Right_Unfocused_TabOffset0 ()
    {
        Rectangle contentRect = new (0, 0, 9, 9);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (11, 9, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Right, 0, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Right);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Right, 0, "Tab", hasFocus: false, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
            ┌───────┬─┐
            │       │T│
            │       │a│
            │       │b│
            │       ├─┘
            │       │
            │       │
            │       │
            └───────┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    [Fact]
    public void Right_Focused_Overflow ()
    {
        Rectangle contentRect = new (0, 0, 9, 9);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (11, 9, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Right, 6, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Right);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Right, 6, "Tab", hasFocus: true, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
            ┌───────┐
            │       │
            │       │
            │       │
            │       │
            │       │
            │       └─┐
            │        T│
            └─────── a│
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    #endregion

    #region Round 5 — Edge Cases

    [Fact]
    public void Top_Focused_OverflowRight ()
    {
        // Header extends past right edge with focus gap
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Top, 5, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 5, "Tab", hasFocus: true, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
                 ┌──┐
                 │Ta│
            ┌────┘  │
            │       │
            │       │
            └───────┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    [Fact]
    public void Left_Focused_Overflow ()
    {
        // Header on left overflows bottom with focus gap
        Rectangle contentRect = new (2, 0, 9, 9);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (11, 9, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Left, 6, "Tab"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Left);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Left, 6, "Tab", hasFocus: true, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        string expected =
            "  ┌───────┐\n" +
            "  │       │\n" +
            "  │       │\n" +
            "  │       │\n" +
            "  │       │\n" +
            "  │       │\n" +
            "┌─┘       │\n" +
            "│T        │\n" +
            "│a ───────┘";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        v.Dispose ();
    }

    [Fact]
    public void Top_SingleCharText ()
    {
        // Single character tab text
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6, view =>
            TabHeaderRenderer.DrawText (view.Driver!, contentRect, Side.Top, 2, "X"));

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 2, "X", hasFocus: false, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        DriverAssert.AssertDriverContentsAre (
            """
              ┌─┐
              │X│
            ┌─┴─┴───┐
            │       │
            │       │
            └───────┘
            """,
            output, driver, ignoreLeadingWhitespace: true);

        v.Dispose ();
    }

    [Fact]
    public void Top_FullyClipped ()
    {
        // Header entirely outside view bounds — nothing drawn
        Rectangle contentRect = new (0, 2, 9, 4);

        (View v, LineCanvas canvas, IDriver driver) = CreateCanvas (9, 6);

        AddContentBorder (canvas, contentRect, skipSide: Side.Top);
        TabHeaderRenderer.AddLines (canvas, contentRect, Side.Top, 20, "Tab", hasFocus: false, LineStyle.Single);

        v.Draw ();
        output.WriteLine (driver.ToString ()!);

        // Only content border sides drawn (no tab side top, no header)
        // Content starts at y=2, so rows 0-1 are empty
        string expected =
            "│       │\n" +
            "│       │\n" +
            "│       │\n" +
            "└───────┘";

        DriverAssert.AssertDriverContentsAre (expected, output, driver);

        v.Dispose ();
    }

    #endregion
}
