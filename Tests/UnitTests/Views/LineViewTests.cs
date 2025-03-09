using UnitTests;

namespace Terminal.Gui.ViewsTests;

public class LineViewTests
{
    [Fact]
    [AutoInitShutdown]
    public void LineView_DefaultConstructor ()
    {
        var horizontal = new LineView ();

        Assert.Equal (Orientation.Horizontal, horizontal.Orientation);
        Assert.Equal (Dim.Fill (), horizontal.Width);
        horizontal.Layout ();
        Assert.Equal (1, horizontal.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void LineView_Horizontal ()
    {
        var horizontal = new LineView (Orientation.Horizontal);

        Assert.Equal (Orientation.Horizontal, horizontal.Orientation);
        Assert.Equal (Dim.Fill (), horizontal.Width);
        horizontal.Layout ();
        Assert.Equal (1, horizontal.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void LineView_Vertical ()
    {
        var vert = new LineView (Orientation.Vertical);

        Assert.Equal (Orientation.Vertical, vert.Orientation);
        Assert.Equal (Dim.Fill (), vert.Height);
        vert.Layout ();
        Assert.Equal (1, vert.Frame.Width);
    }
}
