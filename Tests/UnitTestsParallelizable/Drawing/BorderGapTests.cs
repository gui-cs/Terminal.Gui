// Claude - Opus 4.6

using UnitTests;

namespace Terminal.Gui.DrawingTests;

public class BorderGapTests (ITestOutputHelper output) : TestDriverBase
{
    [Fact]
    public void BorderGap_Constructor_SetsProperties ()
    {
        BorderGap gap = new (5, 3);

        Assert.Equal (5, gap.Position);
        Assert.Equal (3, gap.Length);
    }

    [Fact]
    public void BorderGap_RecordEquality ()
    {
        BorderGap gap1 = new (5, 3);
        BorderGap gap2 = new (5, 3);
        BorderGap gap3 = new (1, 2);

        Assert.Equal (gap1, gap2);
        Assert.NotEqual (gap1, gap3);
    }

    [Fact]
    public void BorderGap_DefaultValues ()
    {
        BorderGap gap = new ();

        Assert.Equal (0, gap.Position);
        Assert.Equal (0, gap.Length);
    }

    [Fact]
    public void Border_GapLists_AreEmptyByDefault ()
    {
        Border border = new ();

        Assert.Empty (border.TopGaps);
        Assert.Empty (border.BottomGaps);
        Assert.Empty (border.LeftGaps);
        Assert.Empty (border.RightGaps);
    }

    [Fact]
    public void Border_GapLists_CanAddGaps ()
    {
        Border border = new ();

        border.TopGaps.Add (new BorderGap (2, 5));
        border.BottomGaps.Add (new BorderGap (3, 4));
        border.LeftGaps.Add (new BorderGap (1, 3));
        border.RightGaps.Add (new BorderGap (0, 2));

        Assert.Single (border.TopGaps);
        Assert.Equal (new BorderGap (2, 5), border.TopGaps [0]);

        Assert.Single (border.BottomGaps);
        Assert.Equal (new BorderGap (3, 4), border.BottomGaps [0]);

        Assert.Single (border.LeftGaps);
        Assert.Equal (new BorderGap (1, 3), border.LeftGaps [0]);

        Assert.Single (border.RightGaps);
        Assert.Equal (new BorderGap (0, 2), border.RightGaps [0]);
    }

    [Fact]
    public void Border_GapLists_CanAddMultipleGaps ()
    {
        Border border = new ();

        border.TopGaps.Add (new BorderGap (1, 2));
        border.TopGaps.Add (new BorderGap (5, 3));
        border.TopGaps.Add (new BorderGap (10, 1));

        Assert.Equal (3, border.TopGaps.Count);
        Assert.Equal (new BorderGap (1, 2), border.TopGaps [0]);
        Assert.Equal (new BorderGap (5, 3), border.TopGaps [1]);
        Assert.Equal (new BorderGap (10, 1), border.TopGaps [2]);
    }

    [Fact]
    public void Border_ClearAllGaps_ClearsAllLists ()
    {
        Border border = new ();
        border.TopGaps.Add (new BorderGap (0, 5));
        border.BottomGaps.Add (new BorderGap (1, 3));
        border.LeftGaps.Add (new BorderGap (2, 4));
        border.RightGaps.Add (new BorderGap (3, 2));

        border.ClearAllGaps ();

        Assert.Empty (border.TopGaps);
        Assert.Empty (border.BottomGaps);
        Assert.Empty (border.LeftGaps);
        Assert.Empty (border.RightGaps);
    }

    [Fact]
    public void Border_ClearAllGaps_OnEmptyLists_DoesNotThrow ()
    {
        Border border = new ();

        Exception? ex = Record.Exception (() => border.ClearAllGaps ());

        Assert.Null (ex);
    }

    [Fact]
    public void Border_TopGap_ExcludesFromRendering ()
    {
        IDriver driver = CreateTestDriver ();

        View view = new () { Width = 12, Height = 5, BorderStyle = LineStyle.Single };
        view.Driver = driver;

        // Gap at position 3, length 4 — punches out columns 3..6 on the top border
        view.Border!.TopGaps.Add (new BorderGap (3, 4));

        Runnable top = new () { Width = driver.Cols, Height = driver.Rows };
        top.Driver = driver;
        top.Add (view);
        top.Layout ();
        top.Draw ();

        DriverAssert.AssertDriverContentsAre ("""

                                              ┌──    ────┐
                                              │          │
                                              │          │
                                              │          │
                                              └──────────┘
                                              """,
                                              output,
                                              driver);
    }

    [Fact]
    public void Border_BottomGap_ExcludesFromRendering ()
    {
        IDriver driver = CreateTestDriver ();

        View view = new () { Width = 12, Height = 5, BorderStyle = LineStyle.Single };
        view.Driver = driver;

        // Gap at position 3, length 4 — punches out columns 3..6 on the bottom border
        view.Border!.BottomGaps.Add (new BorderGap (3, 4));

        Runnable top = new () { Width = driver.Cols, Height = driver.Rows };
        top.Driver = driver;
        top.Add (view);
        top.Layout ();
        top.Draw ();

        DriverAssert.AssertDriverContentsAre ("""

                                              ┌──────────┐
                                              │          │
                                              │          │
                                              │          │
                                              └──    ────┘
                                              """,
                                              output,
                                              driver);
    }
}
