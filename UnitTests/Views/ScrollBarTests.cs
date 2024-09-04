using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ScrollBarTests
{
    public ScrollBarTests (ITestOutputHelper output) { _output = output; }
    private readonly ITestOutputHelper _output;

    [Fact]
    [AutoInitShutdown]
    public void AutoHideScrollBar_CheckScrollBarVisibility ()
    {
        var scrollBar = new ScrollBar { Width = 2, Height = Dim.Fill (), Size = 30 };
        View scrollBarSuperView = ScrollBarSuperView ();
        scrollBarSuperView.Add (scrollBar);
        Application.Begin ((scrollBarSuperView.SuperView as Toplevel)!);

        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);
        Assert.Equal ("Absolute(2)", scrollBar.Width!.ToString ());
        Assert.Equal (2, scrollBar.Viewport.Width);
        Assert.Equal ("Fill(Absolute(0))", scrollBar.Height!.ToString ());
        Assert.Equal (25, scrollBar.Viewport.Height);

        scrollBar.Size = 10;
        Assert.False (scrollBar.ShowScrollIndicator);
        Assert.False (scrollBar.Visible);

        scrollBar.Size = 30;
        Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBar.AutoHide = false;
        Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBar.Size = 10;
        Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBarSuperView.SuperView!.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (
                    20,
                    @"
▲
█
█
█
█
░
░
░
░
▼",
                    @"
▲
░
░
█
█
█
█
░
░
▼",
                    @"
▲
░
░
░
░
█
█
█
█
▼",
                    @"
▲
░
░
█
█
░
░
░
░
▼",
                    @"
◄████░░░░►",
                    @"
◄░░████░░►",
                    @"
◄░░░░████►",
                    @"
◄░░██░░░░►")]
    [InlineData (
                    40,
                    @"
▲
█
█
░
░
░
░
░
░
▼",
                    @"
▲
░
█
█
░
░
░
░
░
▼",
                    @"
▲
░
░
█
█
░
░
░
░
▼",
                    @"
▲
░
█
░
░
░
░
░
░
▼",
                    @"
◄██░░░░░░►",
                    @"
◄░██░░░░░►",
                    @"
◄░░██░░░░►",
                    @"
◄░█░░░░░░►")]
    public void Changing_Position_Size_Orientation_Draws_Correctly (
        int size,
        string firstVertExpected,
        string middleVertExpected,
        string endVertExpected,
        string sizeVertExpected,
        string firstHoriExpected,
        string middleHoriExpected,
        string endHoriExpected,
        string sizeHoriExpected
    )
    {
        var scrollBar = new ScrollBar
        {
            Orientation = Orientation.Vertical,
            Size = size,
            Height = 10
        };
        var top = new Toplevel ();
        top.Add (scrollBar);
        Application.Begin (top);

        _ = TestHelpers.AssertDriverContentsWithFrameAre (firstVertExpected, _output);

        scrollBar.Position = 4;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (middleVertExpected, _output);

        scrollBar.Position = 10;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (endVertExpected, _output);

        scrollBar.Size = size * 2;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (sizeVertExpected, _output);

        scrollBar.Orientation = Orientation.Horizontal;
        scrollBar.Width = 10;
        scrollBar.Height = 1;
        scrollBar.Position = 0;
        scrollBar.Size = size;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (firstHoriExpected, _output);

        scrollBar.Position = 4;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (middleHoriExpected, _output);

        scrollBar.Position = 10;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (endHoriExpected, _output);

        scrollBar.Size = size * 2;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (sizeHoriExpected, _output);
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        var scrollBar = new ScrollBar ();
        Assert.False (scrollBar.CanFocus);
        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (0, scrollBar.Size);
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal ("Auto(Content,Absolute(1),)", scrollBar.Width!.ToString ());
        Assert.Equal ("Auto(Content,Absolute(1),)", scrollBar.Height!.ToString ());
        Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.AutoHide);
    }

    [Fact]
    [AutoInitShutdown]
    public void KeepContentInAllViewport_True_False ()
    {
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill () };
        view.Padding.Thickness = new (0, 0, 2, 0);
        view.SetContentSize (new (view.Viewport.Width, 30));
        var scrollBar = new ScrollBar { Width = 2, Height = Dim.Fill (), Size = view.GetContentSize ().Height };
        scrollBar.PositionChanged += (_, e) => view.Viewport = view.Viewport with { Y = e.CurrentValue };
        view.Padding.Add (scrollBar);
        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Assert.True (scrollBar.KeepContentInAllViewport);
        Assert.Equal (80, view.Padding.Viewport.Width);
        Assert.Equal (25, view.Padding.Viewport.Height);
        Assert.Equal (2, scrollBar.Viewport.Width);
        Assert.Equal (25, scrollBar.Viewport.Height);
        Assert.Equal (30, scrollBar.Size);

        scrollBar.KeepContentInAllViewport = false;
        scrollBar.Position = 50;
        Assert.Equal (scrollBar.Position, scrollBar.Size - 1);
        Assert.Equal (scrollBar.Position, view.Viewport.Y);
        Assert.Equal (29, scrollBar.Position);
        Assert.Equal (29, view.Viewport.Y);

        top.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (
                    Orientation.Vertical,
                    20,
                    10,
                    4,
                    @"
▲
░
░
░
░
█
█
█
█
▼",
                    2,
                    @"
▲
░
█
█
█
█
░
░
░
▼")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    5,
                    @"
▲
░
░
█
█
░
░
░
░
▼",
                    18,
                    @"
▲
░
░
░
░
█
█
░
░
▼")]
    [InlineData (
                    Orientation.Horizontal,
                    20,
                    10,
                    4,
                    @"
◄░░░░████►",
                    2,
                    @"
◄░████░░░►")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    5,
                    @"
◄░░██░░░░►",
                    18,
                    @"
◄░░░░██░░►")]
    public void Mouse_On_The_Container (Orientation orientation, int size, int position, int location, string output, int expectedPos, string expectedOut)
    {
        var scrollBar = new ScrollBar
        {
            Width = orientation == Orientation.Vertical ? 1 : 10,
            Height = orientation == Orientation.Vertical ? 10 : 1,
            Orientation = orientation, Size = size,
            Position = position
        };
        var top = new Toplevel ();
        top.Add (scrollBar);
        Application.Begin (top);

        _ = TestHelpers.AssertDriverContentsWithFrameAre (output, _output);

        Application.OnMouseEvent (
                                  new ()
                                  {
                                      Position = orientation == Orientation.Vertical ? new (0, location) : new Point (location, 0),
                                      Flags = MouseFlags.Button1Pressed
                                  });
        Assert.Equal (expectedPos, scrollBar.Position);

        Application.Refresh ();
        _ = TestHelpers.AssertDriverContentsWithFrameAre (expectedOut, _output);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (
                    Orientation.Vertical,
                    20,
                    10,
                    5,
                    5,
                    @"
▲
░
░
░
░
█
█
█
█
▼",
                    MouseFlags.Button1Pressed,
                    10,
                    @"
▲
░
░
░
░
█
█
█
█
▼")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    3,
                    3,
                    @"
▲
░
░
█
█
░
░
░
░
▼",
                    MouseFlags.Button1Pressed,
                    10,
                    @"
▲
░
░
█
█
░
░
░
░
▼")]
    [InlineData (
                    Orientation.Horizontal,
                    20,
                    10,
                    5,
                    5,
                    @"
◄░░░░████►",
                    MouseFlags.Button1Pressed,
                    10,
                    @"
◄░░░░████►")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    3,
                    3,
                    @"
◄░░██░░░░►",
                    MouseFlags.Button1Pressed,
                    10,
                    @"
◄░░██░░░░►")]
    [InlineData (
                    Orientation.Vertical,
                    20,
                    10,
                    5,
                    7,
                    @"
▲
░
░
░
░
█
█
█
█
▼",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    12,
                    @"
▲
░
░
░
░
█
█
█
█
▼")]
    [InlineData (
                    Orientation.Horizontal,
                    20,
                    10,
                    5,
                    4,
                    @"
◄░░░░████►",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    8,
                    @"
◄░░░████░►")]
    [InlineData (
                    Orientation.Vertical,
                    20,
                    10,
                    5,
                    6,
                    @"
▲
░
░
░
░
█
█
█
█
▼",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    12,
                    @"
▲
░
░
░
░
█
█
█
█
▼")]
    [InlineData (
                    Orientation.Horizontal,
                    20,
                    10,
                    5,
                    6,
                    @"
◄░░░░████►",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    12,
                    @"
◄░░░░████►")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    2,
                    1,
                    @"
▲
░
░
█
█
░
░
░
░
▼",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    2,
                    @"
▲
█
█
░
░
░
░
░
░
▼")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    2,
                    1,
                    @"
◄░░██░░░░►",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    2,
                    @"
◄██░░░░░░►")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    3,
                    4,
                    @"
▲
░
░
█
█
░
░
░
░
▼",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    15,
                    @"
▲
░
░
░
█
█
░
░
░
▼")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    3,
                    4,
                    @"
◄░░██░░░░►",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    15,
                    @"
◄░░░██░░░►")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    3,
                    3,
                    @"
▲
░
░
█
█
░
░
░
░
▼",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    10,
                    @"
▲
░
░
█
█
░
░
░
░
▼")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    3,
                    3,
                    @"
◄░░██░░░░►",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    10,
                    @"
◄░░██░░░░►")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    3,
                    5,
                    @"
▲
░
░
█
█
░
░
░
░
▼",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    20,
                    @"
▲
░
░
░
░
█
█
░
░
▼")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    3,
                    5,
                    @"
◄░░██░░░░►",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    20,
                    @"
◄░░░░██░░►")]
    public void Mouse_On_The_Slider (
        Orientation orientation,
        int size,
        int position,
        int startLocation,
        int endLocation,
        string output,
        MouseFlags mouseFlags,
        int expectedPos,
        string expectedOut
    )
    {
        var scrollBar = new ScrollBar
        {
            Width = orientation == Orientation.Vertical ? 1 : 10,
            Height = orientation == Orientation.Vertical ? 10 : 1,
            Orientation = orientation,
            Size = size, Position = position
        };
        var top = new Toplevel ();
        top.Add (scrollBar);
        Application.Begin (top);

        _ = TestHelpers.AssertDriverContentsWithFrameAre (output, _output);

        Assert.Null (Application.MouseGrabView);

        if (mouseFlags.HasFlag (MouseFlags.ReportMousePosition))
        {
            MouseFlags mf = mouseFlags & ~MouseFlags.ReportMousePosition;

            Application.OnMouseEvent (
                                      new ()
                                      {
                                          Position = orientation == Orientation.Vertical ? new (0, startLocation) : new (startLocation, 0),
                                          Flags = mf
                                      });

            Application.OnMouseEvent (
                                      new ()
                                      {
                                          Position = orientation == Orientation.Vertical ? new (0, endLocation) : new (endLocation, 0),
                                          Flags = mouseFlags
                                      });
        }
        else
        {
            Assert.Equal (startLocation, endLocation);

            Application.OnMouseEvent (
                                      new ()
                                      {
                                          Position = orientation == Orientation.Vertical ? new (0, startLocation) : new (startLocation, 0),
                                          Flags = mouseFlags
                                      });
        }

        Assert.Equal ("scrollSlider", Application.MouseGrabView?.Id);
        Assert.IsType<ScrollSlider> (Application.MouseGrabView);
        Assert.Equal (expectedPos, scrollBar.Position);

        Application.Refresh ();
        _ = TestHelpers.AssertDriverContentsWithFrameAre (expectedOut, _output);

        Application.OnMouseEvent (
                                  new ()
                                  {
                                      Position = orientation == Orientation.Vertical ? new (0, startLocation) : new (startLocation, 0),
                                      Flags = MouseFlags.Button1Released
                                  });
        Assert.Null (Application.MouseGrabView);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (Orientation.Vertical)]
    [InlineData (Orientation.Horizontal)]
    public void Mouse_Pressed_On_ScrollButton_Changes_Position (Orientation orientation)
    {
        var scrollBar = new ScrollBar
        {
            X = 10, Y = 10, Width = orientation == Orientation.Vertical ? 1 : 10, Height = orientation == Orientation.Vertical ? 10 : 1, Size = 20,
            Orientation = orientation
        };
        var top = new Toplevel ();
        top.Add (scrollBar);
        Application.Begin (top);

        var scroll = (Scroll)scrollBar.Subviews.FirstOrDefault (x => x is Scroll);
        Rectangle scrollSliderFrame = scroll!.Subviews.FirstOrDefault (x => x is ScrollSlider)!.Frame;
        Assert.Equal (scrollSliderFrame, orientation == Orientation.Vertical ? new (0, 0, 1, 4) : new (0, 0, 4, 1));
        Assert.Equal (0, scrollBar.Position);

        // ScrollButton increase
        for (var i = 0; i < 13; i++)
        {
            Application.OnMouseEvent (
                                      new ()
                                      {
                                          Position = orientation == Orientation.Vertical ? new (10, 19) : new (19, 10), Flags = MouseFlags.Button1Pressed
                                      });

            if (i < 12)
            {
                Assert.Equal (i + 1, scrollBar.Position);
            }
            else
            {
                Assert.Equal (i, scrollBar.Position);

                Assert.Equal (
                              orientation == Orientation.Vertical ? new (0, 4) : new (4, 0),
                              scroll.Subviews.FirstOrDefault (x => x is ScrollSlider)!.Frame.Location);
            }
        }

        for (var i = 12; i > -1; i--)
        {
            Application.OnMouseEvent (new () { Position = new (10, 10), Flags = MouseFlags.Button1Pressed });

            if (i > 0)
            {
                Assert.Equal (i - 1, scrollBar.Position);
            }
            else
            {
                Assert.Equal (0, scrollBar.Position);
                Assert.Equal (new (0, 0), scroll.Subviews.FirstOrDefault (x => x is ScrollSlider)!.Frame.Location);
            }
        }
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (Orientation.Vertical)]
    [InlineData (Orientation.Horizontal)]
    public void Moving_Mouse_Outside_Host_Ensures_Correct_Location (Orientation orientation)
    {
        var scrollBar = new ScrollBar
        {
            X = 10, Y = 10, Width = orientation == Orientation.Vertical ? 1 : 10, Height = orientation == Orientation.Vertical ? 10 : 1, Size = 20,
            Position = 5, Orientation = orientation
        };
        var top = new Toplevel ();
        top.Add (scrollBar);
        Application.Begin (top);

        var scroll = (Scroll)scrollBar.Subviews.FirstOrDefault (x => x is Scroll);
        Rectangle scrollSliderFrame = scroll!.Subviews.FirstOrDefault (x => x is ScrollSlider)!.Frame;
        Assert.Equal (scrollSliderFrame, orientation == Orientation.Vertical ? new (0, 2, 1, 4) : new (2, 0, 4, 1));

        Application.OnMouseEvent (new () { Position = orientation == Orientation.Vertical ? new (10, 14) : new (14, 10), Flags = MouseFlags.Button1Pressed });

        Application.OnMouseEvent (
                                  new ()
                                  {
                                      Position = orientation == Orientation.Vertical ? new (10, 0) : new (0, 10),
                                      Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                  });
        Assert.Equal (new (0, 0), scroll.Subviews.FirstOrDefault (x => x is ScrollSlider)!.Frame.Location);

        Application.OnMouseEvent (
                                  new ()
                                  {
                                      Position = orientation == Orientation.Vertical ? new (0, 25) : new (80, 0),
                                      Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                  });

        Assert.Equal (
                      orientation == Orientation.Vertical ? new (0, 4) : new (4, 0),
                      scroll.Subviews.FirstOrDefault (x => x is ScrollSlider)!.Frame.Location);
    }

    [Theory]
    [InlineData (Orientation.Vertical, 20, 12, 10)]
    [InlineData (Orientation.Vertical, 40, 32, 30)]
    public void Position_Cannot_Be_Negative_Nor_Greater_Than_Size_Minus_Frame_Length (Orientation orientation, int size, int expectedPos1, int expectedPos2)
    {
        var scrollBar = new ScrollBar { Orientation = orientation, Height = 10, Size = size };
        Assert.Equal (0, scrollBar.Position);

        scrollBar.Position = -1;
        Assert.Equal (0, scrollBar.Position);

        scrollBar.Position = size;
        Assert.Equal (expectedPos1, scrollBar.Position);

        scrollBar.Position = expectedPos2;
        Assert.Equal (expectedPos2, scrollBar.Position);
    }

    [Fact]
    public void PositionChanging_Cancelable_And_PositionChanged_Events ()
    {
        var changingCount = 0;
        var changedCount = 0;
        var scrollBar = new ScrollBar { Size = 10 };

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

    [Fact]
    public void PositionChanging_PositionChanged_Events_Only_Raises_Once_If_Position_Was_Really_Changed ()
    {
        var changing = 0;
        var cancel = false;
        var changed = 0;
        var scrollBar = new ScrollBar { Height = 10, Size = 20 };
        scrollBar.PositionChanging += Scroll_PositionChanging;
        scrollBar.PositionChanged += Scroll_PositionChanged;

        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (new (0, 0, 1, 10), scrollBar.Viewport);
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal (0, changing);
        Assert.Equal (0, changed);

        scrollBar.Position = 0;
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal (0, changing);
        Assert.Equal (0, changed);

        scrollBar.Position = 1;
        Assert.Equal (1, scrollBar.Position);
        Assert.Equal (1, changing);
        Assert.Equal (1, changed);

        Reset ();
        cancel = true;
        scrollBar.Position = 2;
        Assert.Equal (1, scrollBar.Position);
        Assert.Equal (1, changing);
        Assert.Equal (0, changed);

        Reset ();
        scrollBar.Position = 10;
        Assert.Equal (10, scrollBar.Position);
        Assert.Equal (1, changing);
        Assert.Equal (1, changed);

        Reset ();
        scrollBar.Position = 11;
        Assert.Equal (11, scrollBar.Position);
        Assert.Equal (1, changing);
        Assert.Equal (1, changed);

        Reset ();
        scrollBar.Position = 12;
        Assert.Equal (12, scrollBar.Position);
        Assert.Equal (1, changing);
        Assert.Equal (1, changed);

        Reset ();
        scrollBar.Position = 13;
        Assert.Equal (12, scrollBar.Position);
        Assert.Equal (0, changing);
        Assert.Equal (0, changed);

        Reset ();
        scrollBar.Position = 0;
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal (1, changing);
        Assert.Equal (1, changed);

        scrollBar.PositionChanging -= Scroll_PositionChanging;
        scrollBar.PositionChanged -= Scroll_PositionChanged;

        void Scroll_PositionChanging (object sender, CancelEventArgs<int> e)
        {
            changing++;
            e.Cancel = cancel;
        }

        void Scroll_PositionChanged (object sender, EventArgs<int> e) { changed++; }

        void Reset ()
        {
            changing = 0;
            cancel = false;
            changed = 0;
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowScrollIndicator_CheckScrollBarVisibility ()
    {
        var scrollBar = new ScrollBar { Width = 2, Height = Dim.Fill (), Size = 30 };
        View scrollBarSuperView = ScrollBarSuperView ();
        scrollBarSuperView.Add (scrollBar);
        Application.Begin ((scrollBarSuperView.SuperView as Toplevel)!);

        Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBar.ShowScrollIndicator = false;
        Assert.True (scrollBar.AutoHide);
        Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBar.AutoHide = false;
        Assert.False (scrollBar.ShowScrollIndicator);
        Assert.False (scrollBar.Visible);

        scrollBarSuperView.SuperView!.Dispose ();
    }

    [Fact]
    public void Size_Cannot_Be_Negative ()
    {
        var scrollBar = new ScrollBar { Height = 10, Size = -1 };
        Assert.Equal (0, scrollBar.Size);
        scrollBar.Size = -10;
        Assert.Equal (0, scrollBar.Size);
    }

    [Fact]
    public void SizeChanged_Event ()
    {
        var count = 0;
        var scrollBar = new ScrollBar ();
        scrollBar.SizeChanged += (s, e) => count++;

        scrollBar.Size = 10;
        Assert.Equal (10, scrollBar.Size);
        Assert.Equal (1, count);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (
                    3,
                    10,
                    1,
                    Orientation.Vertical,
                    @"
┌─┐
│▲│
│█│
│█│
│░│
│░│
│░│
│░│
│▼│
└─┘")]
    [InlineData (
                    10,
                    3,
                    1,
                    Orientation.Horizontal,
                    @"
┌────────┐
│◄██░░░░►│
└────────┘")]
    [InlineData (
                    3,
                    10,
                    3,
                    Orientation.Vertical,
                    @"
┌───┐
│ ▲ │
│███│
│███│
│░░░│
│░░░│
│░░░│
│░░░│
│ ▼ │
└───┘")]
    [InlineData (
                    10,
                    3,
                    3,
                    Orientation.Horizontal,
                    @"
┌────────┐
│ ██░░░░ │
│◄██░░░░►│
│ ██░░░░ │
└────────┘")]
    public void Vertical_Horizontal_Draws_Correctly (int sizeWidth, int sizeHeight, int widthHeight, Orientation orientation, string expected)
    {
        var super = new Window { Id = "super", Width = Dim.Fill (), Height = Dim.Fill () };
        var top = new Toplevel ();
        top.Add (super);

        var scrollBar = new ScrollBar
        {
            Orientation = orientation,
            Size = orientation == Orientation.Vertical ? sizeHeight * 2 : sizeWidth * 2,
            Width = orientation == Orientation.Vertical ? widthHeight : Dim.Fill (),
            Height = orientation == Orientation.Vertical ? Dim.Fill () : widthHeight
        };
        super.Add (scrollBar);

        Application.Begin (top);

        ((FakeDriver)Application.Driver)!.SetBufferSize (
                                                         sizeWidth + (orientation == Orientation.Vertical ? widthHeight - 1 : 0),
                                                         sizeHeight + (orientation == Orientation.Vertical ? 0 : widthHeight - 1));

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    private View ScrollBarSuperView ()
    {
        var view = new View
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        var top = new Toplevel ();
        top.Add (view);

        return view;
    }
}
