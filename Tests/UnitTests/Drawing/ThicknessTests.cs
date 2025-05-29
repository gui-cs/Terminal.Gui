using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.DrawingTests;

public class ThicknessTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void DrawTests ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (60, 60);
        var t = new Thickness (0, 0, 0, 0);
        var r = new Rectangle (5, 5, 40, 15);

        Application.Driver?.FillRect (
                                      new (0, 0, Application.Driver!.Cols, Application.Driver!.Rows),
                                      (Rune)' '
                                     );
        t.Draw (r, ViewDiagnosticFlags.Thickness, "Test");

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
       Test (Left=0,Top=0,Right=0,Bottom=0)",
                                                       output
                                                      );

        t = new (1, 1, 1, 1);
        r = new (5, 5, 40, 15);

        Application.Driver?.FillRect (
                                      new (0, 0, Application.Driver!.Cols, Application.Driver!.Rows),
                                      (Rune)' '
                                     );
        t.Draw (r, ViewDiagnosticFlags.Thickness, "Test");

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     T                                      T
     TTTest (Left=1,Top=1,Right=1,Bottom=1)TT",
                                                       output
                                                      );

        t = new (1, 2, 3, 4);
        r = new (5, 5, 40, 15);

        Application.Driver?.FillRect (
                                      new (0, 0, Application.Driver!.Cols, Application.Driver!.Rows),
                                      (Rune)' '
                                     );
        t.Draw (r, ViewDiagnosticFlags.Thickness, "Test");

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     T                                    TTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
     TTTest (Left=1,Top=2,Right=3,Bottom=4)TT",
                                                       output
                                                      );

        t = new (-1, 1, 1, 1);
        r = new (5, 5, 40, 15);

        Application.Driver?.FillRect (
                                      new (0, 0, Application.Driver!.Cols, Application.Driver!.Rows),
                                      (Rune)' '
                                     );
        t.Draw (r, ViewDiagnosticFlags.Thickness, "Test");

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
     TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
                                            T
     TTest (Left=-1,Top=1,Right=1,Bottom=1)TT",
                                                       output
                                                      );
    }

    [Fact]
    [AutoInitShutdown]
    public void DrawTests_Ruler ()
    {
        // Add a frame so we can see the ruler
        var f = new FrameView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single};

        var top = new Toplevel ();
        top.Add (f);
        RunState rs = Application.Begin (top);

        ((FakeDriver)Application.Driver!).SetBufferSize (45, 20);
        var t = new Thickness (0, 0, 0, 0);
        var r = new Rectangle (2, 2, 40, 15);
        Application.RunIteration (ref rs);

        t.Draw (r, ViewDiagnosticFlags.Ruler, "Test");

        DriverAssert.AssertDriverContentsAre (
                                              @"
┌───────────────────────────────────────────┐
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
└───────────────────────────────────────────┘",
                                              output
                                             );

        t = new (1, 1, 1, 1);
        r = new (1, 1, 40, 15);
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);
        t.Draw (r, ViewDiagnosticFlags.Ruler, "Test");

        DriverAssert.AssertDriverContentsAre (
                                              @"
┌───────────────────────────────────────────┐
│|123456789|123456789|123456789|123456789   │
│1                                      1   │
│2                                      2   │
│3                                      3   │
│4                                      4   │
│5                                      5   │
│6                                      6   │
│7                                      7   │
│8                                      8   │
│9                                      9   │
│-                                      -   │
│1                                      1   │
│2                                      2   │
│3                                      3   │
│|123456789|123456789|123456789|123456789   │
│                                           │
│                                           │
│                                           │
└───────────────────────────────────────────┘",
                                              output
                                             );

        t = new (1, 2, 3, 4);
        r = new (2, 2, 40, 15);
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);
        t.Draw (r, ViewDiagnosticFlags.Ruler, "Test");

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌───────────────────────────────────────────┐
│                                           │
│ |123456789|123456789|123456789|123456789  │
│ 1                                      1  │
│ 2                                      2  │
│ 3                                      3  │
│ 4                                      4  │
│ 5                                      5  │
│ 6                                      6  │
│ 7                                      7  │
│ 8                                      8  │
│ 9                                      9  │
│ -                                      -  │
│ 1                                      1  │
│ 2                                      2  │
│ 3                                      3  │
│ |123456789|123456789|123456789|123456789  │
│                                           │
│                                           │
└───────────────────────────────────────────┘",
                                                       output
                                                      );

        t = new (-1, 1, 1, 1);
        r = new (5, 5, 40, 15);
        top.SetNeedsDraw ();
        Application.RunIteration (ref rs);
        t.Draw (r, ViewDiagnosticFlags.Ruler, "Test");

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌───────────────────────────────────────────┐
│                                           │
│                                           │
│                                           │
│                                           │
│    |123456789|123456789|123456789|123456789
│                                           1
│                                           2
│                                           3
│                                           4
│                                           5
│                                           6
│                                           7
│                                           8
│                                           9
│                                           -
│                                           1
│                                           2
│                                           3
└────|123456789|123456789|123456789|123456789",
                                                       output
                                                      );
        top.Dispose ();
    }
}
