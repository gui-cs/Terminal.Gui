// Copilot
using UnitTests;

namespace ViewBaseTests.Draw;

/// <summary>
///     Tests that SubViews of Border (and Padding) adornments can have their own border lines
///     that auto-join with the parent View's border lines via <see cref="LineCanvas"/> merging.
///     These tests verify that the draw pipeline renders all lines in a single frame — no
///     one-frame delay for auto-join between adornment SubView borders and parent borders.
///
///     BUG: <see cref="View.DoDrawAdornmentsSubViews"/> runs AFTER <see cref="View.DoRenderLineCanvas"/>,
///     so adornment SubView border lines merged via <see cref="LineCanvas.Merge"/> arrive too late for
///     auto-join. The fix is to move <see cref="View.DoDrawAdornmentsSubViews"/> BEFORE
///     <see cref="View.DoRenderLineCanvas"/> in the draw pipeline.
/// </summary>
public class AdornmentSubViewLineCanvasTests (ITestOutputHelper output) : TestDriverBase
{
    /// <summary>
    ///     A View with a bordered SubView inside its Border adornment. The SubView's bottom border
    ///     sits on the same row as the parent's content border. With correct auto-join, the
    ///     SubView's bottom-left corner should become a ┴ junction (bottom of SubView side edge
    ///     meets parent's horizontal content border). Without auto-join, it's └ (no merge).
    /// </summary>
    [Fact (Skip = "BUG: DoDrawAdornmentsSubViews runs after DoRenderLineCanvas — auto-join fails")]
    public void BorderSubView_WithBorder_AutoJoins_ParentBorder ()
    {
        IDriver driver = CreateTestDriver (12, 6);
        driver.Clip = new Region (driver.Screen);

        // Parent: 12×6, top border thickness = 3 so rows 0-2 are border, row 2 is content border
        View parent = new ()
        {
            Driver = driver,
            Width = 12,
            Height = 6,
            BorderStyle = LineStyle.Single
        };
        parent.Border.Thickness = new Thickness (1, 3, 1, 1);

        // SubView: 4×3 at (1,0) in border area. Its bottom edge is at row 2 = content border row.
        // SuperViewRendersLineCanvas = true so its border lines merge into parent's LineCanvas.
        View borderSubView = new ()
        {
            X = 1,
            Y = 0,
            Width = 4,
            Height = 3,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Text = "Hi"
        };
        parent.Border.GetOrCreateView ().Add (borderSubView);

        parent.BeginInit ();
        parent.EndInit ();
        parent.Layout ();
        parent.Draw ();

        output.WriteLine (driver.ToString ());

        string driverContents = driver.ToString ()!;

        // Verify the SubView's text is visible
        Assert.Contains ("Hi", driverContents);

        // With auto-join working correctly, the SubView's bottom-left corner (└) should merge
        // with the parent's horizontal content border (─) to become ┴ (T-junction from above).
        // Similarly bottom-right corner (┘) → ┴.
        //
        // Without the fix: SubView's lines are merged AFTER parent LC is rendered.
        // Auto-join doesn't happen — ┴ never appears.
        Assert.Contains ("┴", driverContents);
    }

    /// <summary>
    ///     Same as above but with the SubView on the left side of the border.
    ///     Verifies auto-join works for vertical borders meeting horizontal borders.
    /// </summary>
    [Fact (Skip = "BUG: DoDrawAdornmentsSubViews runs after DoRenderLineCanvas — auto-join fails")]
    public void PaddingSubView_WithBorder_AutoJoins_ParentBorder ()
    {
        IDriver driver = CreateTestDriver (12, 6);
        driver.Clip = new Region (driver.Screen);

        // Parent: 12×6, padding thickness = 2 on top, border = 1 all around
        View parent = new ()
        {
            Driver = driver,
            Width = 12,
            Height = 6,
            BorderStyle = LineStyle.Single
        };
        parent.Border.Thickness = new Thickness (1);
        parent.Padding.Thickness = new Thickness (0, 2, 0, 0);

        // SubView in Padding with its own border, SuperViewRendersLineCanvas = true
        View paddingSubView = new ()
        {
            X = 1,
            Y = 0,
            Width = 4,
            Height = 2,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Text = "P"
        };
        parent.Padding.GetOrCreateView ().Add (paddingSubView);

        parent.BeginInit ();
        parent.EndInit ();
        parent.Layout ();
        parent.Draw ();

        output.WriteLine (driver.ToString ());

        string driverContents = driver.ToString ()!;

        // Verify SubView text is visible
        Assert.Contains ("P", driverContents);

        // The SubView's border should auto-join with the parent's border lines.
        // Without the pipeline fix, auto-join fails for Padding SubViews too.
        Assert.Contains ("┴", driverContents);
    }
}
