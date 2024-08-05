using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests;

public class RulerTests
{
    private readonly ITestOutputHelper _output;
    public RulerTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void Attribute_set ()
    {
        var newAttribute = new Attribute (Color.Red, Color.Green);

        var r = new Ruler ();
        r.Attribute = newAttribute;
        Assert.Equal (newAttribute, r.Attribute);
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        var r = new Ruler ();
        Assert.Equal (0, r.Length);
        Assert.Equal (Orientation.Horizontal, r.Orientation);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Default ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (25, 25);

        var r = new Ruler ();
        r.Draw (Point.Empty);
        TestHelpers.AssertDriverContentsWithFrameAre (@"", _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Horizontal ()
    {
        var len = 15;

        // Add a frame so we can see the ruler
        var f = new FrameView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        var top = new Toplevel ();
        top.Add (f);
        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (len + 5, 5);
        Assert.Equal (new (0, 0, len + 5, 5), f.Frame);

        var r = new Ruler ();
        Assert.Equal (Orientation.Horizontal, r.Orientation);

        r.Length = len;
        r.Draw (Point.Empty);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
|123456789|1234────┐
│                  │
│                  │
│                  │
└──────────────────┘",
                                                      _output
                                                     );

        // Postive offset
        Application.Refresh ();
        r.Draw (new (1, 1));

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│|123456789|1234   │
│                  │
│                  │
└──────────────────┘",
                                                      _output
                                                     );

        // Negative offset
        Application.Refresh ();
        r.Draw (new (-1, 1));

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
123456789|1234     │
│                  │
│                  │
└──────────────────┘",
                                                      _output
                                                     );

        // Clip
        Application.Refresh ();
        r.Draw (new (10, 1));

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│         |123456789
│                  │
│                  │
└──────────────────┘",
                                                      _output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Horizontal_Start ()
    {
        var len = 15;

        // Add a frame so we can see the ruler
        var f = new FrameView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
        var top = new Toplevel ();
        top.Add (f);
        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (len + 5, 5);
        Assert.Equal (new (0, 0, len + 5, 5), f.Frame);

        var r = new Ruler ();
        Assert.Equal (Orientation.Horizontal, r.Orientation);

        r.Length = len;
        r.Draw (Point.Empty, 1);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
123456789|12345────┐
│                  │
│                  │
│                  │
└──────────────────┘",
                                                      _output
                                                     );

        Application.Refresh ();
        r.Length = len;
        r.Draw (new (1, 0), 1);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌123456789|12345───┐
│                  │
│                  │
│                  │
└──────────────────┘",
                                                      _output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Vertical ()
    {
        var len = 15;

        // Add a frame so we can see the ruler
        var f = new FrameView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        var top = new Toplevel ();
        top.Add (f);
        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (5, len + 5);
        Assert.Equal (new (0, 0, 5, len + 5), f.Frame);

        var r = new Ruler ();
        r.Orientation = Orientation.Vertical;
        r.Length = len;
        r.Draw (Point.Empty);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
-───┐
1   │
2   │
3   │
4   │
5   │
6   │
7   │
8   │
9   │
-   │
1   │
2   │
3   │
4   │
│   │
│   │
│   │
│   │
└───┘",
                                                      _output
                                                     );

        // Postive offset
        Application.Refresh ();
        r.Draw (new (1, 1));

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌───┐
│-  │
│1  │
│2  │
│3  │
│4  │
│5  │
│6  │
│7  │
│8  │
│9  │
│-  │
│1  │
│2  │
│3  │
│4  │
│   │
│   │
│   │
└───┘",
                                                      _output
                                                     );

        // Negative offset
        Application.Refresh ();
        r.Draw (new (1, -1));

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌1──┐
│2  │
│3  │
│4  │
│5  │
│6  │
│7  │
│8  │
│9  │
│-  │
│1  │
│2  │
│3  │
│4  │
│   │
│   │
│   │
│   │
│   │
└───┘",
                                                      _output
                                                     );

        // Clip
        Application.Refresh ();
        r.Draw (new (1, 10));

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌───┐
│   │
│   │
│   │
│   │
│   │
│   │
│   │
│   │
│   │
│-  │
│1  │
│2  │
│3  │
│4  │
│5  │
│6  │
│7  │
│8  │
└9──┘",
                                                      _output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Vertical_Start ()
    {
        var len = 15;

        // Add a frame so we can see the ruler
        var f = new FrameView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

        var top = new Toplevel ();
        top.Add (f);
        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (5, len + 5);
        Assert.Equal (new (0, 0, 5, len + 5), f.Frame);

        var r = new Ruler ();
        r.Orientation = Orientation.Vertical;
        r.Length = len;
        r.Draw (Point.Empty, 1);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
1───┐
2   │
3   │
4   │
5   │
6   │
7   │
8   │
9   │
-   │
1   │
2   │
3   │
4   │
5   │
│   │
│   │
│   │
│   │
└───┘",
                                                      _output
                                                     );

        Application.Refresh ();
        r.Length = len;
        r.Draw (new (0, 1), 1);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌───┐
1   │
2   │
3   │
4   │
5   │
6   │
7   │
8   │
9   │
-   │
1   │
2   │
3   │
4   │
5   │
│   │
│   │
│   │
└───┘",
                                                      _output
                                                     );
        top.Dispose ();
    }

    [Fact]
    public void Length_set ()
    {
        var r = new Ruler ();
        Assert.Equal (0, r.Length);
        r.Length = 42;
        Assert.Equal (42, r.Length);
    }

    [Fact]
    public void Orientation_set ()
    {
        var r = new Ruler ();
        Assert.Equal (Orientation.Horizontal, r.Orientation);
        r.Orientation = Orientation.Vertical;
        Assert.Equal (Orientation.Vertical, r.Orientation);
    }
}
