using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

public class FrameTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Frame_Empty_Default ()
    {
        View view = new ();
        Assert.Equal(Rectangle.Empty, view.Frame);

        view.BeginInit();
        view.EndInit();
        Assert.Equal (Rectangle.Empty, view.Frame);
    }

    [Fact]
    public void Frame_Set_Sets ()
    {
        Rectangle frame = new (1, 2, 3, 4);
        View view = new ();
        Assert.Equal (Rectangle.Empty, view.Frame);

        view.BeginInit ();
        view.EndInit ();
        view.Frame = frame;
        Assert.Equal (frame, view.Frame);
    }

    // Moved this test from AbsoluteLayoutTests
    // TODO: Refactor as Theory
    [Fact]
    [TestRespondersDisposed]
    public void Frame_Set ()
    {
        var frame = new Rectangle (1, 2, 3, 4);
        var newFrame = new Rectangle (1, 2, 30, 40);

        var v = new View ();
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        v.Dispose ();

        v = new View { Frame = frame };
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);

        v.Frame = newFrame;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Sized (30), v.Width);
        Assert.Equal (Dim.Sized (40), v.Height);
        v.Dispose ();

        v = new View { X = frame.X, Y = frame.Y, Text = "v" };
        v.Frame = newFrame;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Sized (30), v.Width);
        Assert.Equal (Dim.Sized (40), v.Height);
        v.Dispose ();

        newFrame = new Rectangle (10, 20, 30, 40);
        v = new View { Frame = frame };
        v.Frame = newFrame;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (10), v.X);
        Assert.Equal (Pos.Absolute (20), v.Y);
        Assert.Equal (Dim.Sized (30), v.Width);
        Assert.Equal (Dim.Sized (40), v.Height);
        v.Dispose ();

        v = new View { X = frame.X, Y = frame.Y, Text = "v" };
        v.Frame = newFrame;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (10), v.X);
        Assert.Equal (Pos.Absolute (20), v.Y);
        Assert.Equal (Dim.Sized (30), v.Width);
        Assert.Equal (Dim.Sized (40), v.Height);
        v.Dispose ();
    }
}
