using UnitTests;
using Xunit.Abstractions;
using static Terminal.Gui.ViewBase.Dim;
using static Terminal.Gui.ViewBase.Pos;

namespace UnitTests.LayoutTests;

public class PosCenterTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

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

        Application.Driver!.SetScreenSize (20, height);
        AutoInitShutdownAttribute.RunIteration ();
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

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, _output);
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

        Application.Driver!.SetScreenSize (width, 7);
        AutoInitShutdownAttribute.RunIteration ();
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

        _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
        win.Dispose ();
    }
}
