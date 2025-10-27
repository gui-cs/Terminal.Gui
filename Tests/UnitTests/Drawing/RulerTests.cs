using Xunit.Abstractions;

namespace UnitTests.DrawingTests;

public class RulerTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void Draw_Default ()
    {
        Application.Driver?.SetScreenSize (25, 25);

        var r = new Ruler ();
        r.Draw (Point.Empty);
        DriverAssert.AssertDriverContentsWithFrameAre (@"", output);
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
                                                       output
                                                      );

        // Postive offset
        r.Draw (new (1, 1));

        DriverAssert.AssertDriverContentsAre (
                                              @"
|123456789|1234
 |123456789|1234
",
                                              output
                                             );

        // Negative offset
        r.Draw (new (-1, 3));

        DriverAssert.AssertDriverContentsAre (
                                              @"
|123456789|1234
 |123456789|1234
123456789|1234
",
                                              output
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
                                                       output
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
                                                       output
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
                                                       output
                                                      );
    }
}
