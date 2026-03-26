// Copilot

using UnitTests;

namespace ViewBaseTests.Draw;

/// <summary>
///     Tests that SubViews of adornments with <see cref="View.SuperViewRendersLineCanvas"/> = true
///     get their border lines auto-joined with the SuperView's border lines.
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
        // Copilot

        IDriver driver = CreateTestDriver (7, 3);
        driver.Clip = new Region (driver.Screen);

        View view = new ()
        {
            Id = "view",
            Driver = driver,
            Width = 7,
            Height = 3,
            BorderStyle = LineStyle.Single
        };
        view.Border.Thickness = new Thickness (0, 1, 0, 0);

        View sub = new ()
        {
            Id = "sub",
            X = 2,
            Y = 0,
            Width = 3,
            Height = 1,
            SuperViewRendersLineCanvas = true
        };
        view.Border.GetOrCreateView ().Add (sub);

        view.BeginInit ();
        view.EndInit ();
        view.Layout ();

        Rectangle subScreen = sub.FrameToScreen ();

        sub.LineCanvas.AddLine (new Point (subScreen.X, subScreen.Y), 3, Orientation.Horizontal, LineStyle.Double);

        view.Draw ();

        // Expected: ──═══──    (double-line from SubView merged with view's single-line)
        DriverAssert.AssertDriverContentsAre ("""
                                              ──═══──
                                              """,
                                              output,
                                              driver);
    }

    /// <summary>
    ///     Proves a Label with its own Border and SuperViewRendersLineCanvas = true
    ///     can serve as a tab header: the Label's border auto-joins with the View's
    ///     border, and the Label's text renders inside.
    ///     This is the foundation for replacing custom tab line drawing in BorderView
    ///     with a simple Label SubView.
    /// </summary>
    [Fact]
    public void Label_With_Border_AutoJoins_Parent_Top ()
    {
        // Copilot

        // 13×5 View with border thickness 3 on top (room for tab header).
        // Label "Test" at offset 1 in the top border, with its own single-line border
        // and SuperViewRendersLineCanvas = true.
        //
        // The Label's border auto-joins with the View's content border line.
        // The ┴ junctions where Label sides meet the content border prove auto-join works.

        IDriver driver = CreateTestDriver (13, 5);

        View view = new ()
        {
            Id = "view",
            Driver = driver,
            Width = 13,
            Height = 5,
            BorderStyle = LineStyle.Single
        };
        view.Border.Thickness = new Thickness (1, 3, 1, 1);
        view.Border.Settings = BorderSettings.None;

        Label tabLabel = new ()
        {
            Id = "tabLabel",
            Text = "Test",
            X = 1,
            Y = 0,
            Width = 6,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true
        };
        tabLabel.Border.Thickness = new Thickness (1);
        tabLabel.Border.Settings = BorderSettings.None;
        view.Border.GetOrCreateView ().Add (tabLabel);

        view.BeginInit ();
        view.EndInit ();
        view.Layout ();
        view.Draw ();

        output.WriteLine ("Driver output:");
        output.WriteLine (driver.ToString ());

        // The Label's border lines auto-join with the View's border:
        // - ┴ junctions where Label side borders meet the content border (row 2)
        // - ┌ and ┐ corners on the Label's top border (row 0)
        // - View's border starts at the content border line (row 2) since
        //   the top 3 rows are all border thickness.
        DriverAssert.AssertDriverContentsAre ("""
                                               ┌────┐
                                               │Test│
                                              ┌┴────┴─────┐
                                              │           │
                                              └───────────┘
                                              """,
                                              output,
                                              driver);
    }
}
