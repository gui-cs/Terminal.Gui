using Xunit.Abstractions;
using static Terminal.Gui.Dim;
using static Terminal.Gui.Pos;

namespace Terminal.Gui.LayoutTests;

public class PosCenterTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void PosCenter_Constructor ()
    {
        var posCenter = new PosCenter ();
        Assert.NotNull (posCenter);
    }

    [Fact]
    public void PosCenter_ToString ()
    {
        var posCenter = new PosCenter ();
        var expectedString = "Center";

        Assert.Equal (expectedString, posCenter.ToString ());
    }

    [Fact]
    public void PosCenter_GetAnchor ()
    {
        var posCenter = new PosCenter ();
        var width = 50;
        int expectedAnchor = width / 2;

        Assert.Equal (expectedAnchor, posCenter.GetAnchor (width));
    }

    [Fact]
    public void PosCenter_CreatesCorrectInstance ()
    {
        Pos pos = Center ();
        Assert.IsType<PosCenter> (pos);
    }

    [Theory]
    [InlineData (10, 2, 4)]
    [InlineData (10, 10, 0)]
    [InlineData (10, 11, 0)]
    [InlineData (10, 12, -1)]
    [InlineData (19, 20, 0)]
    public void PosCenter_Calculate_ReturnsExpectedValue (int superviewDimension, int width, int expectedX)
    {
        var posCenter = new PosCenter ();
        int result = posCenter.Calculate (superviewDimension, new DimAbsolute (width), null!, Dimension.Width);
        Assert.Equal (expectedX, result);
    }


    [Fact]
    public void PosCenter_Bigger_Than_SuperView ()
    {
        var superView = new View { Width = 10, Height = 10 };
        var view = new View { X = Center (), Y = Center (), Width = 20, Height = 20 };
        superView.Add (view);
        superView.LayoutSubviews ();

        Assert.Equal (-5, view.Frame.Left);
        Assert.Equal (-5, view.Frame.Top);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void PosCenter_SubView_85_Percent_Height (int height)
    {
        var win = new Window { Width = Fill (), Height = Fill () };

        var subview = new Window
        {
            X = Center (), Y = Center (), Width = Dim.Percent (85), Height = Dim.Percent (85)
        };

        win.Add (subview);

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (20, height);
        Application.RunIteration (ref rs, firstIteration);
        var expected = string.Empty;

        switch (height)
        {
            case 1:
                //Assert.Equal (new (0, 0, 17, 0), subview.Frame);
                expected = @"
────────────────────";

                break;
            case 2:
                //Assert.Equal (new (0, 0, 17, 1), subview.Frame);
                expected = @"
┌──────────────────┐
└──────────────────┘
";

                break;
            case 3:
                //Assert.Equal (new (0, 0, 17, 2), subview.Frame);
                expected = @"
┌──────────────────┐
│                  │
└──────────────────┘
";

                break;
            case 4:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ───────────────  │
│                  │
└──────────────────┘";

                break;
            case 5:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 6:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 7:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 8:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 9:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 10:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘"
                    ;

                break;
        }

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
        win.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void PosCenter_SubView_85_Percent_Width (int width)
    {
        var win = new Window { Width = Fill (), Height = Fill () };

        var subview = new Window
        {
            X = Center (), Y = Center (), Width = Dim.Percent (85), Height = Dim.Percent (85)
        };

        win.Add (subview);

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver!).SetBufferSize (width, 7);
        Application.RunIteration (ref rs, firstIteration);
        var expected = string.Empty;

        switch (width)
        {
            case 1:
                Assert.Equal (new (0, 0, 0, 4), subview.Frame);

                expected = @"
│
│
│
│
│
│
│";

                break;
            case 2:
                Assert.Equal (new (0, 0, 0, 4), subview.Frame);

                expected = @"
┌┐
││
││
││
││
││
└┘";

                break;
            case 3:
                Assert.Equal (new (0, 0, 0, 4), subview.Frame);

                expected = @"
┌─┐
│ │
│ │
│ │
│ │
│ │
└─┘";

                break;
            case 4:
                Assert.Equal (new (0, 0, 1, 4), subview.Frame);

                expected = @"
┌──┐
││ │
││ │
││ │
││ │
│  │
└──┘";

                break;
            case 5:
                Assert.Equal (new (0, 0, 2, 4), subview.Frame);

                expected = @"
┌───┐
│┌┐ │
│││ │
│││ │
│└┘ │
│   │
└───┘";

                break;
            case 6:
                Assert.Equal (new (0, 0, 3, 4), subview.Frame);

                expected = @"
┌────┐
│┌─┐ │
││ │ │
││ │ │
│└─┘ │
│    │
└────┘";

                break;
            case 7:
                Assert.Equal (new (0, 0, 4, 4), subview.Frame);

                expected = @"
┌─────┐
│┌──┐ │
││  │ │
││  │ │
│└──┘ │
│     │
└─────┘";

                break;
            case 8:
                Assert.Equal (new (0, 0, 5, 4), subview.Frame);

                expected = @"
┌──────┐
│┌───┐ │
││   │ │
││   │ │
│└───┘ │
│      │
└──────┘";

                break;
            case 9:
                Assert.Equal (new (1, 0, 5, 4), subview.Frame);

                expected = @"
┌───────┐
│ ┌───┐ │
│ │   │ │
│ │   │ │
│ └───┘ │
│       │
└───────┘";

                break;
            case 10:
                Assert.Equal (new (1, 0, 6, 4), subview.Frame);

                expected = @"
┌────────┐
│ ┌────┐ │
│ │    │ │
│ │    │ │
│ └────┘ │
│        │
└────────┘"
                    ;

                break;
        }

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
        win.Dispose ();
    }
}
