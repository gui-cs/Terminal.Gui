using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class FrameViewTests (ITestOutputHelper output)
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var fv = new FrameView ();
        Assert.Equal (string.Empty, fv.Title);
        Assert.Equal (string.Empty, fv.Text);
        Assert.Equal (LineStyle.Rounded, fv.BorderStyle);

        fv = new() { Title = "Test" };
        Assert.Equal ("Test", fv.Title);
        Assert.Equal (string.Empty, fv.Text);
        Assert.Equal (LineStyle.Rounded, fv.BorderStyle);

        fv = new()
        {
            X = 1,
            Y = 2,
            Width = 10,
            Height = 20,
            Title = "Test"
        };
        Assert.Equal ("Test", fv.Title);
        Assert.Equal (string.Empty, fv.Text);
        fv.BeginInit ();
        fv.EndInit ();
        Assert.Equal (LineStyle.Rounded, fv.BorderStyle);
        Assert.Equal (new (1, 2, 10, 20), fv.Frame);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Defaults ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (10, 10);
        var fv = new FrameView () { BorderStyle = LineStyle.Single };
        Assert.Equal (string.Empty, fv.Title);
        Assert.Equal (string.Empty, fv.Text);
        var top = new Toplevel ();
        top.Add (fv);
        Application.Begin (top);
        Assert.Equal (new (0, 0, 0, 0), fv.Frame);
        DriverAssert.AssertDriverContentsWithFrameAre (@"", output);

        fv.Height = 5;
        fv.Width = 5;
        Assert.Equal (new (0, 0, 5, 5), fv.Frame);
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
┌───┐
│   │
│   │
│   │
└───┘",
                                                      output
                                                     );

        fv.X = 1;
        fv.Y = 2;
        Assert.Equal (new (1, 2, 5, 5), fv.Frame);
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌───┐
 │   │
 │   │
 │   │
 └───┘",
                                                      output
                                                     );

        fv.X = -1;
        fv.Y = -2;
        Assert.Equal (new (-1, -2, 5, 5), fv.Frame);
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
   │
   │
───┘",
                                                      output
                                                     );

        fv.X = 7;
        fv.Y = 8;
        Assert.Equal (new (7, 8, 5, 5), fv.Frame);
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
       ┌──
       │  ",
                                                      output
                                                     );
        top.Dispose ();
    }
}
