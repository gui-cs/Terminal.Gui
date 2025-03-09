using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ScrollBarTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var scrollBar = new ScrollBar ();
        Assert.False (scrollBar.CanFocus);
        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (0, scrollBar.ScrollableContentSize);
        Assert.Equal (0, scrollBar.VisibleContentSize);
        Assert.Equal (0, scrollBar.GetSliderPosition ());
        Assert.Equal (0, scrollBar.Position);
        Assert.False (scrollBar.AutoShow);
    }

    #region AutoHide
    [Fact]
    public void AutoHide_False_Is_Default_CorrectlyHidesAndShows ()
    {
        var super = new Toplevel ()
        {
            Id = "super",
            Width = 1,
            Height = 20
        };

        var scrollBar = new ScrollBar
        {
        };
        super.Add (scrollBar);
        Assert.False (scrollBar.AutoShow);
        Assert.True (scrollBar.Visible);

        scrollBar.AutoShow = true;
        Assert.True (scrollBar.AutoShow);
        Assert.True (scrollBar.Visible);

        // Should Show
        scrollBar.ScrollableContentSize = 21;
        Assert.True (scrollBar.Visible);

        // Should Hide
        scrollBar.ScrollableContentSize = 10;
        super.Layout (new (100, 100));
        Assert.False (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    public void AutoHide_False_CorrectlyHidesAndShows ()
    {
        var super = new Toplevel ()
        {
            Id = "super",
            Width = 1,
            Height = 20
        };

        var scrollBar = new ScrollBar
        {
            ScrollableContentSize = 20,
            AutoShow = false
        };
        super.Add (scrollBar);
        Assert.False (scrollBar.AutoShow);
        Assert.True (scrollBar.Visible);

        // Should Hide if AutoSize = true, but should not hide if AutoSize = false
        scrollBar.ScrollableContentSize = 10;
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    public void AutoHide_True_Changing_ScrollableContentSize_CorrectlyHidesAndShows ()
    {
        var super = new Toplevel ()
        {
            Id = "super",
            Width = 1,
            Height = 20
        };

        var scrollBar = new ScrollBar
        {
            ScrollableContentSize = 20,
        };
        super.Add (scrollBar);
        Assert.False (scrollBar.AutoShow);
        Assert.True (scrollBar.Visible);

        scrollBar.AutoShow = true;

        super.Layout (new (100, 100));
        Assert.False (scrollBar.Visible);
        Assert.Equal (1, scrollBar.Frame.Width);
        Assert.Equal (20, scrollBar.Frame.Height);

        scrollBar.ScrollableContentSize = 10;
        super.Layout (new (100, 100));
        Assert.False (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 30;
        super.Layout (new (100, 100));
        Assert.True (scrollBar.Visible);

        scrollBar.AutoShow = false;
        super.Layout (new (100, 100));
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 10;
        super.Layout (new (100, 100));
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    public void AutoHide_Change_VisibleContentSize_CorrectlyHidesAndShows ()
    {
        var super = new Toplevel ()
        {
            Id = "super",
            Width = 1,
            Height = 20
        };

        var scrollBar = new ScrollBar
        {
            ScrollableContentSize = 20,
            VisibleContentSize = 20
        };
        super.Add (scrollBar);
        Assert.False (scrollBar.AutoShow);
        Assert.True (scrollBar.Visible);

        scrollBar.AutoShow = true;

        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (20, scrollBar.VisibleContentSize);
        Assert.False (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;
        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 30;
        //Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;
        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 21;
        //Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.AutoShow = false;
        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;
        //Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    #endregion AutoHide

    #region Orientation
    [Fact]
    public void OnOrientationChanged_Keeps_Size ()
    {
        var scroll = new ScrollBar ();
        scroll.Layout ();
        scroll.ScrollableContentSize = 1;

        scroll.Orientation = Orientation.Horizontal;
        Assert.Equal (1, scroll.ScrollableContentSize);
    }

    [Fact]
    public void OnOrientationChanged_Sets_Position_To_0 ()
    {
        View super = new View ()
        {
            Id = "super",
            Width = 10,
            Height = 10
        };
        var scrollBar = new ScrollBar ()
        {
        };
        super.Add (scrollBar);
        scrollBar.Layout ();
        scrollBar.Position = 1;
        scrollBar.Orientation = Orientation.Horizontal;

        Assert.Equal (0, scrollBar.GetSliderPosition ());
    }

    #endregion Orientation


    #region Size

    // TODO: Add tests.

    #endregion Size

    #region Position
    [Fact]
    public void Position_Event_Cancels ()
    {
        var changingCount = 0;
        var changedCount = 0;
        var scrollBar = new ScrollBar { };
        scrollBar.ScrollableContentSize = 5;
        scrollBar.Frame = new Rectangle (0, 0, 1, 4); // Needs to be at least 4 for slider to move

        scrollBar.PositionChanging += (s, e) =>
                                            {
                                                if (changingCount == 0)
                                                {
                                                    e.Cancel = true;
                                                }

                                                changingCount++;
                                            };
        scrollBar.PositionChanged += (s, e) => changedCount++;

        scrollBar.Position = 1;
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal (1, changingCount);
        Assert.Equal (0, changedCount);

        scrollBar.Position = 1;
        Assert.Equal (1, scrollBar.Position);
        Assert.Equal (2, changingCount);
        Assert.Equal (1, changedCount);
    }
    #endregion Position


    [Fact]
    public void ScrollableContentSize_Cannot_Be_Negative ()
    {
        var scrollBar = new ScrollBar { Height = 10, ScrollableContentSize = -1 };
        Assert.Equal (0, scrollBar.ScrollableContentSize);
        scrollBar.ScrollableContentSize = -10;
        Assert.Equal (0, scrollBar.ScrollableContentSize);
    }

    [Fact]
    public void ScrollableContentSizeChanged_Event ()
    {
        var count = 0;
        var scrollBar = new ScrollBar ();
        scrollBar.ScrollableContentSizeChanged += (s, e) => count++;

        scrollBar.ScrollableContentSize = 10;
        Assert.Equal (10, scrollBar.ScrollableContentSize);
        Assert.Equal (1, count);
    }
}
