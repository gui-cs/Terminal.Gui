using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.ViewsTests;

public class ScrollBarTests (ITestOutputHelper output)
{
    #region Draw

    [Theory]
    [SetupFakeApplication]

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
            Driver = ApplicationImpl.Instance.Driver,

            Id = "super",
            Width = width + 2,
            Height = height + 2,
        };

        var scrollBar = new ScrollBar
        {
            ShowScroll = true,
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

        scrollBar.Value = contentPosition;

        super.Layout ();
        super.Draw ();

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
    }
    #endregion Draw

    #region Mouse

    [Theory (Skip = "Broken in #4474")]
    [CombinatorialData]
    [AutoInitShutdown]
    public void Mouse_Click_DecrementButton_Decrements ([CombinatorialRange (1, 3, 1)] int increment, Orientation orientation)
    {
        var top = new Runnable ()
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
        SessionToken rs = Application.Begin (top);

        // Scroll to end
        scrollBar.Value = 19;
        Assert.Equal (10, scrollBar.Value);
        AutoInitShutdownAttribute.RunIteration ();

        Assert.Equal (4, scrollBar.GetSliderPosition ());
        Assert.Equal (10, scrollBar.Value);
        int initialPos = scrollBar.Value;

        Point btnPoint = orientation == Orientation.Vertical
                             ? new (0, 0)
                             : new (0, 0);

        Application.RaiseMouseEvent (new ()
        {
            ScreenPosition = btnPoint,
            Flags = MouseFlags.LeftButtonClicked
        });

        AutoInitShutdownAttribute.RunIteration ();

        Assert.Equal (initialPos - increment, scrollBar.Value);

        Application.ResetState (true);
    }


    [Theory (Skip = "Broken in #4474")]
    [CombinatorialData]
    [AutoInitShutdown]
    public void Mouse_Click_IncrementButton_Increments ([CombinatorialRange (1, 3, 1)] int increment, Orientation orientation)
    {
        var top = new Runnable ()
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
        SessionToken rs = Application.Begin (top);

        // Scroll to top
        scrollBar.Value = 0;
        AutoInitShutdownAttribute.RunIteration ();

        Assert.Equal (0, scrollBar.GetSliderPosition ());
        Assert.Equal (0, scrollBar.Value);
        int initialPos = scrollBar.Value;

        Point btnPoint = orientation == Orientation.Vertical
                             ? new (scrollBar.Frame.X, scrollBar.Frame.Height - 1)
                             : new (scrollBar.Frame.Width - 1, scrollBar.Frame.Y);

        Application.RaiseMouseEvent (new ()
        {
            ScreenPosition = btnPoint,
            Flags = MouseFlags.LeftButtonClicked
        });
        AutoInitShutdownAttribute.RunIteration ();

        Assert.Equal (initialPos + increment, scrollBar.Value);

        Application.ResetState (true);
    }
    #endregion Mouse
}
