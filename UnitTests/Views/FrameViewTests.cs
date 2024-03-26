using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class FrameViewTests
{
    private readonly ITestOutputHelper _output;
    public FrameViewTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void Constructors_Defaults ()
    {
        var fv = new FrameView ();
        Assert.Equal (string.Empty, fv.Title);
        Assert.Equal (string.Empty, fv.Text);
        Assert.Equal (LineStyle.Single, fv.BorderStyle);

        fv = new FrameView { Title = "Test" };
        Assert.Equal ("Test", fv.Title);
        Assert.Equal (string.Empty, fv.Text);
        Assert.Equal (LineStyle.Single, fv.BorderStyle);

        fv = new FrameView
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
        Assert.Equal (LineStyle.Single, fv.BorderStyle);
        Assert.Equal (new Rectangle (1, 2, 10, 20), fv.Frame);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Defaults ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (10, 10);
        var fv = new FrameView ();
        Assert.Equal (string.Empty, fv.Title);
        Assert.Equal (string.Empty, fv.Text);
        var top = new Toplevel ();
        top.Add (fv);
        Application.Begin (top);
        Assert.Equal (new Rectangle (0, 0, 0, 0), fv.Frame);
        TestHelpers.AssertDriverContentsWithFrameAre (@"", _output);

        fv.Height = 5;
        fv.Width = 5;
        Assert.Equal (new Rectangle (0, 0, 5, 5), fv.Frame);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌───┐
│   │
│   │
│   │
└───┘",
                                                      _output
                                                     );

        fv.X = 1;
        fv.Y = 2;
        Assert.Equal (new Rectangle (1, 2, 5, 5), fv.Frame);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 ┌───┐
 │   │
 │   │
 │   │
 └───┘",
                                                      _output
                                                     );

        fv.X = -1;
        fv.Y = -2;
        Assert.Equal (new Rectangle (-1, -2, 5, 5), fv.Frame);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
   │
   │
───┘",
                                                      _output
                                                     );

        fv.X = 7;
        fv.Y = 8;
        Assert.Equal (new Rectangle (7, 8, 5, 5), fv.Frame);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
       ┌──
       │  ",
                                                      _output
                                                     );
    }
}
