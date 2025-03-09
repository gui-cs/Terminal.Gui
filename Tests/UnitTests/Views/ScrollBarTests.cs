using UnitTests;
using UnitTests;
using Xunit.Abstractions;
using static Unix.Terminal.Delegates;

namespace Terminal.Gui.ViewsTests;

public class ScrollBarTests (ITestOutputHelper output)
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
    [AutoInitShutdown]
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

        RunState rs = Application.Begin (super);

        // Should Show
        scrollBar.ScrollableContentSize = 21;
        Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        // Should Hide
        scrollBar.ScrollableContentSize = 10;
        Assert.False (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
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

        RunState rs = Application.Begin (super);

        // Should Hide if AutoSize = true, but should not hide if AutoSize = false
        scrollBar.ScrollableContentSize = 10;
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
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

        RunState rs = Application.Begin (super);

        Assert.False (scrollBar.Visible);
        Assert.Equal (1, scrollBar.Frame.Width);
        Assert.Equal (20, scrollBar.Frame.Height);

        scrollBar.ScrollableContentSize = 10;
        Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 30;
        Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.AutoShow = false;
        Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 10;
        Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
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

        RunState rs = Application.Begin (super);

        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (20, scrollBar.VisibleContentSize);
        Assert.False (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;
        Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 30;
        Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;
        Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 21;
        Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.AutoShow = false;
        Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        scrollBar.VisibleContentSize = 10;
        Application.RunIteration (ref rs);
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

    [Theory]
    [SetupFakeDriver]

    #region Draw


    #region Horizontal

    #region Super 10 - ScrollBar 8
    [InlineData (
                    10,
                    1,
                    10,
                    -1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄████████►│
└──────────┘")]

    [InlineData (
                    10,
                    1,
                    20,
                    -1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄████░░░░►│
└──────────┘")]
    [InlineData (
                    10,
                    1,
                    20,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄████░░░░►│
└──────────┘")]

    [InlineData (
                    10,
                    1,
                    20,
                    1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄████░░░░►│
└──────────┘")]

    [InlineData (
                    10,
                    1,
                    20,
                    2,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░████░░░►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    3,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░████░░░►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    4,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░████░░►│
└──────────┘
")]
    [InlineData (
                    10,
                    1,
                    20,
                    5,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░████░░►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    6,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░████░░►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    7,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░████░►│
└──────────┘
")]


    [InlineData (
                    10,
                    1,
                    20,
                    8,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░████░►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    9,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░░████►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    10,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░░████►│
└──────────┘
")]


    [InlineData (
                    10,
                    1,
                    20,
                    19,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░░████►│
└──────────┘
")]


    [InlineData (
                    10,
                    1,
                    20,
                    20,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░░████►│
└──────────┘
")]
    #endregion  Super 10 - ScrollBar 8

    #region  Super 12 - ScrollBar 10
    [InlineData (
                    12,
                    1,
                    10,
                    -1,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄██████████►│
└────────────┘")]

    [InlineData (
                    12,
                    1,
                    20,
                    -1,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄██████░░░░►│
└────────────┘")]
    [InlineData (
                    12,
                    1,
                    20,
                    0,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄██████░░░░►│
└────────────┘")]

    [InlineData (
                    12,
                    1,
                    20,
                    1,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄██████░░░░►│
└────────────┘")]

    [InlineData (
                    12,
                    1,
                    20,
                    2,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░██████░░░►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    3,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░██████░░►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    4,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░██████░░►│
└────────────┘
")]
    [InlineData (
                    12,
                    1,
                    20,
                    5,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░██████░░►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    6,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░██████░►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    7,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]


    [InlineData (
                    12,
                    1,
                    20,
                    8,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    9,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    10,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]


    [InlineData (
                    12,
                    1,
                    20,
                    19,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]


    [InlineData (
                    12,
                    1,
                    20,
                    20,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]
    #endregion Super 12 - ScrollBar 10
    [InlineData (
                    10,
                    3,
                    20,
                    2,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│ ░████░░░ │
│◄░████░░░►│
│ ░████░░░ │
└──────────┘
")]
    #endregion Horizontal

    #region Vertical

    [InlineData (
                    1,
                    10,
                    10,
                    -1,
                    Orientation.Vertical,
                    @"
┌─┐
│▲│
│█│
│█│
│█│
│█│
│█│
│█│
│█│
│█│
│▼│
└─┘")]

    [InlineData (
                    1,
                    10,
                    10,
                    5,
                    Orientation.Vertical,
                    @"
┌─┐
│▲│
│█│
│█│
│█│
│█│
│█│
│█│
│█│
│█│
│▼│
└─┘")]

    [InlineData (
                    1,
                    10,
                    20,
                    5,
                    Orientation.Vertical,
                    @"
┌─┐
│▲│
│░│
│░│
│█│
│█│
│█│
│█│
│░│
│░│
│▼│
└─┘")]

    [InlineData (
                    1,
                    12,
                    20,
                    5,
                    Orientation.Vertical,
                    @"
┌─┐
│▲│
│░│
│░│
│█│
│█│
│█│
│█│
│█│
│█│
│░│
│░│
│▼│
└─┘")]

    [InlineData (
                    3,
                    10,
                    20,
                    2,
                    Orientation.Vertical,
                    @"
┌───┐
│ ▲ │
│░░░│
│███│
│███│
│███│
│███│
│░░░│
│░░░│
│░░░│
│ ▼ │
└───┘
")]
    #endregion Vertical


    public void Draws_Correctly_Default_Settings (int width, int height, int contentSize, int contentPosition, Orientation orientation, string expected)
    {
        var super = new Window
        {
            Id = "super",
            Width = width + 2,
            Height = height + 2,
        };

        var scrollBar = new ScrollBar
        {
            AutoShow = false,
            Orientation = orientation,
        };

        if (orientation == Orientation.Vertical)
        {
            super.SetContentSize (new (width, contentSize));
            scrollBar.Width = width;
        }
        else
        {
            super.SetContentSize (new (contentSize, height));
            scrollBar.Height = height;
        }
        super.Add (scrollBar);

        scrollBar.Position = contentPosition;

        super.Layout ();
        super.Draw ();

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
    }
    #endregion Draw

    #region Mouse



    [Theory]
    [CombinatorialData]
    [AutoInitShutdown]
    public void Mouse_Click_DecrementButton_Decrements ([CombinatorialRange (1, 3, 1)] int increment, Orientation orientation)
    {
        var top = new Toplevel ()
        {
            Id = "top",
            Width = 10,
            Height = 10
        };
        var scrollBar = new ScrollBar
        {
            Id = "scrollBar",
            Orientation = orientation,
            ScrollableContentSize = 20,
            Increment = increment
        };

        top.Add (scrollBar);
        RunState rs = Application.Begin (top);

        // Scroll to end
        scrollBar.Position = 19;
        Assert.Equal (10, scrollBar.Position);
        Application.RunIteration (ref rs);

        Assert.Equal (4, scrollBar.GetSliderPosition ());
        Assert.Equal (10, scrollBar.Position);
        int initialPos = scrollBar.Position;

        Point btnPoint = orientation == Orientation.Vertical
                             ? new (0, 0)
                             : new (0, 0);

        Application.RaiseMouseEvent (new ()
        {
            ScreenPosition = btnPoint,
            Flags = MouseFlags.Button1Clicked
        });

        Application.RunIteration (ref rs);

        Assert.Equal (initialPos - increment, scrollBar.Position);

        Application.ResetState (true);
    }


    [Theory]
    [CombinatorialData]
    [AutoInitShutdown]
    public void Mouse_Click_IncrementButton_Increments ([CombinatorialRange (1, 3, 1)] int increment, Orientation orientation)
    {
        var top = new Toplevel ()
        {
            Id = "top",
            Width = 10,
            Height = 10
        };
        var scrollBar = new ScrollBar
        {
            Id = "scrollBar",
            Orientation = orientation,
            ScrollableContentSize = 20,
            Increment = increment
        };

        top.Add (scrollBar);
        RunState rs = Application.Begin (top);

        // Scroll to top
        scrollBar.Position = 0;
        Application.RunIteration (ref rs);

        Assert.Equal (0, scrollBar.GetSliderPosition ());
        Assert.Equal (0, scrollBar.Position);
        int initialPos = scrollBar.Position;

        Point btnPoint = orientation == Orientation.Vertical
                             ? new (scrollBar.Frame.X, scrollBar.Frame.Height - 1)
                             : new (scrollBar.Frame.Width - 1, scrollBar.Frame.Y);

        Application.RaiseMouseEvent (new ()
        {
            ScreenPosition = btnPoint,
            Flags = MouseFlags.Button1Clicked
        });
        Application.RunIteration (ref rs);

        Assert.Equal (initialPos + increment, scrollBar.Position);

        Application.ResetState (true);
    }
    #endregion Mouse
}
