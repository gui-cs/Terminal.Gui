// Copilot
using Terminal.Gui.Tracing;
using UnitTests;

namespace ViewBaseTests.Draw;

/// <summary>
///     Tests that SubViews of adornments with <see cref="View.SuperViewRendersLineCanvas"/> = true
///     get their border lines auto-joined with the parent View's border lines.
///
///     BUG (#4854): <see cref="View.DoDrawAdornmentsSubViews"/> runs AFTER
///     <see cref="View.DoRenderLineCanvas"/> in the draw pipeline, so merged lines arrive too late.
/// </summary>
public class AdornmentSubViewLineCanvasTests (ITestOutputHelper output) : TestDriverBase
{
    /// <summary>
    ///     Simplest repro: A 7×3 View with only a top border line. A SubView in the Border
    ///     adds a double-line segment via SuperViewRendersLineCanvas. The ═ should appear
    ///     but doesn't because the merge happens after the LineCanvas was already rendered.
    /// </summary>
    [Fact]
    public void BorderSubView_Lines_Not_Rendered ()
    {
        using IDisposable tracing = TestLogging.Verbose (output, TraceCategory.Draw);

        IDriver driver = CreateTestDriver (7, 3);
        driver.Clip = new Region (driver.Screen);

        View parent = new ()
        {
            Id = "parent",
            Driver = driver,
            Width = 7,
            Height = 3,
            BorderStyle = LineStyle.Single
        };
        parent.Border.Thickness = new Thickness (0, 1, 0, 0);

        View sub = new ()
        {
            Id = "sub",
            X = 2,
            Y = 0,
            Width = 3,
            Height = 1,
            SuperViewRendersLineCanvas = true
        };
        parent.Border.GetOrCreateView ().Add (sub);

        parent.BeginInit ();
        parent.EndInit ();
        parent.Layout ();

        Rectangle subScreen = sub.FrameToScreen ();
        output.WriteLine ($"subScreen: {subScreen}");

        sub.LineCanvas.AddLine (
            new Point (subScreen.X, subScreen.Y),
            3,
            Orientation.Horizontal,
            LineStyle.Double);

        output.WriteLine ($"sub.LC.Bounds before Draw: {sub.LineCanvas.Bounds}");

        parent.Draw ();

        output.WriteLine ($"Driver output:");
        output.WriteLine (driver.ToString ());

        // Expected: ──═══──    (double-line from SubView merged with parent's single-line)
        DriverAssert.AssertDriverContentsAre (
            """
            ──═══──
            """,
            output,
            driver);
    }
}
