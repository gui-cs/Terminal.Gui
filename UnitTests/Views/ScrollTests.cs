using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ScrollTests
{
    public ScrollTests (ITestOutputHelper output) { _output = output; }
    private readonly ITestOutputHelper _output;

    [Theory]
    [AutoInitShutdown]
    [InlineData (
                    20,
                    @"
█
█
█
█
█
░
░
░
░
░",
                    @"
░
░
█
█
█
█
█
░
░
░",
                    @"
░
░
░
░
░
█
█
█
█
█",
                    @"
░
░
█
█
░
░
░
░
░
░",
                    @"
█████░░░░░",
                    @"
░░█████░░░",
                    @"
░░░░░█████",
                    @"
░░██░░░░░░")]
    [InlineData (
                    40,
                    @"
█
█
░
░
░
░
░
░
░
░",
                    @"
░
█
█
░
░
░
░
░
░
░",
                    @"
░
░
█
█
░
░
░
░
░
░",
                    @"
░
█
░
░
░
░
░
░
░
░",
                    @"
██░░░░░░░░",
                    @"
░██░░░░░░░",
                    @"
░░██░░░░░░",
                    @"
░█░░░░░░░░")]
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
        var scroll = new Scroll
        {
            Orientation = Orientation.Vertical,
            Size = size,
            Height = 10
        };
        var top = new Toplevel ();
        top.Add (scroll);
        Application.Begin (top);

        _ = TestHelpers.AssertDriverContentsWithFrameAre (firstVertExpected, _output);

        scroll.Position = 4;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (middleVertExpected, _output);

        scroll.Position = 10;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (endVertExpected, _output);

        scroll.Size = size * 2;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (sizeVertExpected, _output);

        scroll.Orientation = Orientation.Horizontal;
        scroll.Width = 10;
        scroll.Position = 0;
        scroll.Size = size;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (firstHoriExpected, _output);

        scroll.Position = 4;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (middleHoriExpected, _output);

        scroll.Position = 10;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (endHoriExpected, _output);

        scroll.Size = size * 2;
        Application.Refresh ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (sizeHoriExpected, _output);
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        var scroll = new Scroll ();
        Assert.True (scroll.WantContinuousButtonPressed);
        Assert.False (scroll.ClearOnVisibleFalse);
        Assert.False (scroll.CanFocus);
        Assert.Equal (Orientation.Vertical, scroll.Orientation);
        Assert.Equal (0, scroll.Size);
        Assert.Equal (0, scroll.Position);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (
                    Orientation.Vertical,
                    20,
                    10,
                    4,
                    @"
░
░
░
░
░
█
█
█
█
█",
                    0,
                    @"
█
█
█
█
█
░
░
░
░
░")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    5,
                    @"
░
░
█
█
░
░
░
░
░
░",
                    20,
                    @"
░
░
░
░
░
█
█
░
░
░")]
    [InlineData (
                    Orientation.Horizontal,
                    20,
                    10,
                    4,
                    @"
░░░░░█████",
                    0,
                    @"
█████░░░░░")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    5,
                    @"
░░██░░░░░░",
                    20,
                    @"
░░░░░██░░░")]
    public void Mouse_On_The_Container (Orientation orientation, int size, int position, int location, string output, int expectedPos, string expectedOut)
    {
        var scroll = new Scroll { Width = 10, Height = 10, Orientation = orientation, Size = size, Position = position };
        var top = new Toplevel ();
        top.Add (scroll);
        Application.Begin (top);

        _ = TestHelpers.AssertDriverContentsWithFrameAre (output, _output);

        Application.OnMouseEvent (
                                  new ()
                                  {
                                      Position = orientation == Orientation.Vertical ? new (0, location) : new Point (location, 0),
                                      Flags = MouseFlags.Button1Pressed
                                  });
        Assert.Equal (expectedPos, scroll.Position);

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
░
░
░
░
░
█
█
█
█
█",
                    MouseFlags.Button1Pressed,
                    10,
                    @"
░
░
░
░
░
█
█
█
█
█")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    3,
                    3,
                    @"
░
░
█
█
░
░
░
░
░
░",
                    MouseFlags.Button1Pressed,
                    10,
                    @"
░
░
█
█
░
░
░
░
░
░")]
    [InlineData (
                    Orientation.Horizontal,
                    20,
                    10,
                    5,
                    5,
                    @"
░░░░░█████",
                    MouseFlags.Button1Pressed,
                    10,
                    @"
░░░░░█████")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    3,
                    3,
                    @"
░░██░░░░░░",
                    MouseFlags.Button1Pressed,
                    10,
                    @"
░░██░░░░░░")]
    [InlineData (
                    Orientation.Vertical,
                    20,
                    10,
                    5,
                    4,
                    @"
░
░
░
░
░
█
█
█
█
█",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    8,
                    @"
░
░
░
░
█
█
█
█
█
░")]
    [InlineData (
                    Orientation.Horizontal,
                    20,
                    10,
                    5,
                    4,
                    @"
░░░░░█████",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    8,
                    @"
░░░░█████░")]
    [InlineData (
                    Orientation.Vertical,
                    20,
                    10,
                    5,
                    6,
                    @"
░
░
░
░
░
█
█
█
█
█",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    10,
                    @"
░
░
░
░
░
█
█
█
█
█")]
    [InlineData (
                    Orientation.Horizontal,
                    20,
                    10,
                    5,
                    6,
                    @"
░░░░░█████",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    10,
                    @"
░░░░░█████")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    2,
                    1,
                    @"
░
░
█
█
░
░
░
░
░
░",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    4,
                    @"
░
█
█
░
░
░
░
░
░
░")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    2,
                    1,
                    @"
░░██░░░░░░",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    4,
                    @"
░██░░░░░░░")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    3,
                    4,
                    @"
░
░
█
█
░
░
░
░
░
░",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    12,
                    @"
░
░
░
█
█
░
░
░
░
░")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    3,
                    4,
                    @"
░░██░░░░░░",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    12,
                    @"
░░░██░░░░░")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    2,
                    3,
                    @"
░
░
█
█
░
░
░
░
░
░",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    12,
                    @"
░
░
░
█
█
░
░
░
░
░")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    2,
                    3,
                    @"
░░██░░░░░░",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    12,
                    @"
░░░██░░░░░")]
    [InlineData (
                    Orientation.Vertical,
                    40,
                    10,
                    2,
                    4,
                    @"
░
░
█
█
░
░
░
░
░
░",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    16,
                    @"
░
░
░
░
█
█
░
░
░
░")]
    [InlineData (
                    Orientation.Horizontal,
                    40,
                    10,
                    2,
                    4,
                    @"
░░██░░░░░░",
                    MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition,
                    16,
                    @"
░░░░██░░░░")]
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
        var scroll = new Scroll { Width = 10, Height = 10, Orientation = orientation, Size = size, Position = position };
        var top = new Toplevel ();
        top.Add (scroll);
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

        Assert.Equal ("slider", Application.MouseGrabView?.Id);
        Assert.Equal (expectedPos, scroll.Position);

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
    [InlineData (Orientation.Vertical, 20, 10)]
    [InlineData (Orientation.Vertical, 40, 30)]
    public void Position_Cannot_Be_Negative_Nor_Greater_Than_Size_Minus_Frame_Length (Orientation orientation, int size, int expectedPos)
    {
        var scroll = new Scroll { Orientation = orientation, Height = 10, Size = size };
        Assert.Equal (0, scroll.Position);

        scroll.Position = -1;
        Assert.Equal (0, scroll.Position);

        scroll.Position = size;
        Assert.Equal (0, scroll.Position);

        scroll.Position = expectedPos;
        Assert.Equal (expectedPos, scroll.Position);
    }

    [Fact]
    public void PositionChanging_Cancelable_And_PositionChanged_Events ()
    {
        var changingCount = 0;
        var changedCount = 0;
        var scroll = new Scroll { Size = 10 };

        scroll.PositionChanging += (s, e) =>
                                   {
                                       if (changingCount == 0)
                                       {
                                           e.Cancel = true;
                                       }

                                       changingCount++;
                                   };
        scroll.PositionChanged += (s, e) => changedCount++;

        scroll.Position = 1;
        Assert.Equal (0, scroll.Position);
        Assert.Equal (1, changingCount);
        Assert.Equal (0, changedCount);

        scroll.Position = 1;
        Assert.Equal (1, scroll.Position);
        Assert.Equal (2, changingCount);
        Assert.Equal (1, changedCount);
    }

    [Fact]
    public void SizeChanged_Event ()
    {
        var count = 0;
        var scroll = new Scroll ();
        scroll.SizeChanged += (s, e) => count++;

        scroll.Size = 10;
        Assert.Equal (10, scroll.Size);
        Assert.Equal (1, count);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (
                    3,
                    10,
                    Orientation.Vertical,
                    @"
┌─┐
│█│
│█│
│█│
│░│
│░│
│░│
│░│
│░│
└─┘")]
    [InlineData (
                    10,
                    3,
                    Orientation.Horizontal,
                    @"
┌────────┐
│███░░░░░│
└────────┘")]
    public void Vertical_Horizontal_Draws_Correctly (int width, int height, Orientation orientation, string expected)
    {
        var super = new Window { Id = "super", Width = Dim.Fill (), Height = Dim.Fill () };
        var top = new Toplevel ();
        top.Add (super);

        var scroll = new Scroll
        {
            Orientation = orientation,
            Size = orientation == Orientation.Vertical ? height * 2 : width * 2,
            Width = orientation == Orientation.Vertical ? 1 : Dim.Fill (),
            Height = orientation == Orientation.Vertical ? Dim.Fill () : 1
        };
        super.Add (scroll);

        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (width, height);

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }
}
