using UnitTests;
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
        DriverAssert.AssertDriverContentsWithFrameAre (@"", _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Horizontal ()
    {
        var len = 15;

        var r = new Ruler ();
        Assert.Equal (Orientation.Horizontal, r.Orientation);

        r.Length = len;
        r.Draw (Point.Empty);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
|123456789|1234",
                                                       _output
                                                      );

        // Postive offset
        r.Draw (new (1, 1));

        DriverAssert.AssertDriverContentsAre (
                                              @"
|123456789|1234
 |123456789|1234
",
                                              _output
                                             );

        // Negative offset
        r.Draw (new (-1, 3));

        DriverAssert.AssertDriverContentsAre (
                                              @"
|123456789|1234
 |123456789|1234
123456789|1234
",
                                              _output
                                             );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Vertical ()
    {
        var len = 15;

        var r = new Ruler ();
        r.Orientation = Orientation.Vertical;
        r.Length = len;
        r.Draw (Point.Empty);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
-
1
2
3
4
5
6
7
8
9
-
1
2
3
4",
                                                       _output
                                                      );

        r.Draw (new (1, 1));

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
- 
1-
21
32
43
54
65
76
87
98
-9
1-
21
32
43
 4",
                                                       _output
                                                      );

        // Negative offset
        r.Draw (new (2, -1));

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
- 1
1-2
213
324
435
546
657
768
879
98-
-91
1-2
213
324
43 
 4 ",
                                                       _output
                                                      );
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
