using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

[Trait ("Category", "Layout")]
[Trait ("Category", "Output")]
[Trait ("Category", "Drawing")]
public class AbsoluteLayoutTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Change_Height_or_Width_Absolute ()
    {
        var frame = new Rectangle (1, 2, 3, 4);
        var newFrame = new Rectangle (1, 2, 30, 40);

        var v = new View { Frame = frame };
        v.Height = newFrame.Height;
        v.Width = newFrame.Width;
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal ($"Absolute({newFrame.Height})", v.Height.ToString ());
        Assert.Equal ($"Absolute({newFrame.Width})", v.Width.ToString ());
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Change_Height_or_Width_MakesComputed ()
    {
        var v = new View { Frame = Rectangle.Empty };
        v.Height = Dim.Fill ();
        v.Width = Dim.Fill ();
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Change_X_or_Y_Absolute ()
    {
        var frame = new Rectangle (1, 2, 3, 4);
        var newFrame = new Rectangle (10, 20, 3, 4);

        var v = new View { Frame = frame };
        v.X = newFrame.X;
        v.Y = newFrame.Y;
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new (0, 0, newFrame.Width, newFrame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal ($"Absolute({newFrame.X})", v.X.ToString ());
        Assert.Equal ($"Absolute({newFrame.Y})", v.Y.ToString ());
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Change_X_or_Y_MakesComputed ()
    {
        var v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Change_X_Y_Height_Width_Absolute ()
    {
        var v = new View { Frame = Rectangle.Empty };
        v.X = 1;
        v.Y = 2;
        v.Height = 3;
        v.Width = 4;
        v.Dispose ();

        v = new() { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();
        v.Dispose ();

        v = new() { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.X = 1;
        v.Dispose ();

        v = new() { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.Y = 2;
        v.Dispose ();

        v = new() { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.Width = 3;
        v.Dispose ();

        v = new() { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.Height = 3;
        v.Dispose ();

        v = new() { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();

        v.X = 1;
        v.Y = 2;
        v.Height = 3;
        v.Width = 4;
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Constructor ()
    {
        var v = new View ();
        v.Dispose ();

        var frame = Rectangle.Empty;
        v = new() { Frame = frame };
        Assert.Equal (frame, v.Frame);

        Assert.Equal (
                      new (0, 0, frame.Width, frame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (0), v.X);
        Assert.Equal (Pos.Absolute (0), v.Y);
        Assert.Equal (Dim.Absolute (0), v.Width);
        Assert.Equal (Dim.Absolute (0), v.Height);
        v.Dispose ();

        frame = new (1, 2, 3, 4);
        v = new() { Frame = frame };
        Assert.Equal (frame, v.Frame);

        Assert.Equal (
                      new (0, 0, frame.Width, frame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);
        v.Dispose ();

        v = new() { Frame = frame, Text = "v" };
        Assert.Equal (frame, v.Frame);

        Assert.Equal (
                      new (0, 0, frame.Width, frame.Height),
                      v.Viewport
                     ); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);
        v.Dispose ();

        v = new() { X = frame.X, Y = frame.Y, Text = "v" };

        Assert.Equal (new (frame.X, frame.Y, 0, 0), v.Frame);
        Assert.Equal (new (0, 0, 0, 0), v.Viewport); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (0), v.Width);
        Assert.Equal (Dim.Absolute (0), v.Height);
        v.Dispose ();

        v = new ();
        Assert.Equal (new (0, 0, 0, 0), v.Frame);
        Assert.Equal (new (0, 0, 0, 0), v.Viewport); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (0), v.X);
        Assert.Equal (Pos.Absolute (0), v.Y);
        Assert.Equal (Dim.Absolute (0), v.Width);
        Assert.Equal (Dim.Absolute (0), v.Height);
        v.Dispose ();

        v = new() { X = frame.X, Y = frame.Y, Width = frame.Width, Height = frame.Height };
        Assert.Equal (new (frame.X, frame.Y, 3, 4), v.Frame);
        Assert.Equal (new (0, 0, 3, 4), v.Viewport); // With Absolute Viewport *is* deterministic before Layout
        Assert.Equal (Pos.Absolute (1), v.X);
        Assert.Equal (Pos.Absolute (2), v.Y);
        Assert.Equal (Dim.Absolute (3), v.Width);
        Assert.Equal (Dim.Absolute (4), v.Height);
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_LayoutSubviews ()
    {
        var superRect = new Rectangle (0, 0, 100, 100);
        var super = new View { Frame = superRect, Text = "super" };
        var v1 = new View { X = 0, Y = 0, Width = 10, Height = 10 };

        var v2 = new View { X = 10, Y = 10, Width = 10, Height = 10 };

        super.Add (v1, v2);

        super.LayoutSubviews ();
        Assert.Equal (new (0, 0, 10, 10), v1.Frame);
        Assert.Equal (new (10, 10, 10, 10), v2.Frame);
        super.Dispose ();
    }
}
