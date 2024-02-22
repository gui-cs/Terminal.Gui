using Xunit.Abstractions;

//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ViewTests;

public class AbsoluteLayoutTests
{
    private readonly ITestOutputHelper _output;
    public AbsoluteLayoutTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Change_Frame ()
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
                      v.Bounds
                     ); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (30), v.Width);
        Assert.Equal (Dim.Sized (40), v.Height);
        v.Dispose ();

        v = new View { X = frame.X, Y = frame.Y, Text = "v" };
        v.Frame = newFrame;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, newFrame.Width, newFrame.Height),
                      v.Bounds
                     ); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
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
                      v.Bounds
                     ); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (10), v.X);
        Assert.Equal (Pos.At (20), v.Y);
        Assert.Equal (Dim.Sized (30), v.Width);
        Assert.Equal (Dim.Sized (40), v.Height);
        v.Dispose ();

        v = new View { X = frame.X, Y = frame.Y, Text = "v" };
        v.Frame = newFrame;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, newFrame.Width, newFrame.Height),
                      v.Bounds
                     ); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (10), v.X);
        Assert.Equal (Pos.At (20), v.Y);
        Assert.Equal (Dim.Sized (30), v.Width);
        Assert.Equal (Dim.Sized (40), v.Height);
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Change_Height_or_Width_Absolute ()
    {
        var frame = new Rectangle (1, 2, 3, 4);
        var newFrame = new Rectangle (1, 2, 30, 40);

        var v = new View { Frame = frame };
        v.Height = newFrame.Height;
        v.Width = newFrame.Width;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, newFrame.Width, newFrame.Height),
                      v.Bounds
                     ); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
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
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);
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
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newFrame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, newFrame.Width, newFrame.Height),
                      v.Bounds
                     ); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal ($"Absolute({newFrame.X})", v.X.ToString ());
        Assert.Equal ($"Absolute({newFrame.Y})", v.Y.ToString ());
        Assert.Equal (Dim.Sized (3), v.Width);
        Assert.Equal (Dim.Sized (4), v.Height);
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Change_X_or_Y_MakesComputed ()
    {
        var v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);
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
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);

        v.X = 1;
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);

        v.Y = 2;
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);

        v.Width = 3;
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);

        v.Height = 3;
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);
        v.Dispose ();

        v = new View { Frame = Rectangle.Empty };
        v.X = Pos.Center ();
        v.Y = Pos.Center ();
        v.Width = Dim.Fill ();
        v.Height = Dim.Fill ();
        Assert.True (v.LayoutStyle == LayoutStyle.Computed);

        v.X = 1;
        v.Y = 2;
        v.Height = 3;
        v.Width = 4;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_Constructor ()
    {
        var v = new View ();
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        v.Dispose ();

        var frame = Rectangle.Empty;
        v = new View { Frame = frame };
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (frame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, frame.Width, frame.Height),
                      v.Bounds
                     ); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (0), v.X);
        Assert.Equal (Pos.At (0), v.Y);
        Assert.Equal (Dim.Sized (0), v.Width);
        Assert.Equal (Dim.Sized (0), v.Height);
        v.Dispose ();

        frame = new Rectangle (1, 2, 3, 4);
        v = new View { Frame = frame };
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (frame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, frame.Width, frame.Height),
                      v.Bounds
                     ); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (3), v.Width);
        Assert.Equal (Dim.Sized (4), v.Height);
        v.Dispose ();

        v = new View { Frame = frame, Text = "v" };
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (frame, v.Frame);

        Assert.Equal (
                      new Rectangle (0, 0, frame.Width, frame.Height),
                      v.Bounds
                     ); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (3), v.Width);
        Assert.Equal (Dim.Sized (4), v.Height);
        v.Dispose ();

        v = new View { X = frame.X, Y = frame.Y, Text = "v" };
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);

        // BUGBUG: v2 - I think the default size should be 0,0 not 1,1
        // That is correct it should be 0,0 because AutoSize is false
        // and the size wasn't set on the initializer
        Assert.Equal (new Rectangle (frame.X, frame.Y, 0, 0), v.Frame);
        Assert.Equal (new Rectangle (0, 0, 0, 0), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (0), v.Width);
        Assert.Equal (Dim.Sized (0), v.Height);
        v.Dispose ();

        v = new View ();
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (new Rectangle (0, 0, 0, 0), v.Frame);
        Assert.Equal (new Rectangle (0, 0, 0, 0), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (0), v.X);
        Assert.Equal (Pos.At (0), v.Y);
        Assert.Equal (Dim.Sized (0), v.Width);
        Assert.Equal (Dim.Sized (0), v.Height);
        v.Dispose ();

        v = new View { X = frame.X, Y = frame.Y, Width = frame.Width, Height = frame.Height };
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (new Rectangle (frame.X, frame.Y, 3, 4), v.Frame);
        Assert.Equal (new Rectangle (0, 0, 3, 4), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (3), v.Width);
        Assert.Equal (Dim.Sized (4), v.Height);
        v.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void AbsoluteLayout_LayoutSubviews ()
    {
        var superRect = new Rectangle (0, 0, 100, 100);
        var super = new View { Frame = superRect, Text = "super" };
        Assert.True (super.LayoutStyle == LayoutStyle.Absolute);
        var v1 = new View { X = 0, Y = 0, Width = 10, Height = 10 };
        Assert.True (v1.LayoutStyle == LayoutStyle.Absolute);

        var v2 = new View { X = 10, Y = 10, Width = 10, Height = 10 };
        Assert.True (v2.LayoutStyle == LayoutStyle.Absolute);

        super.Add (v1, v2);
        Assert.True (v1.LayoutStyle == LayoutStyle.Absolute);
        Assert.True (v2.LayoutStyle == LayoutStyle.Absolute);

        super.LayoutSubviews ();
        Assert.Equal (new Rectangle (0, 0, 10, 10), v1.Frame);
        Assert.Equal (new Rectangle (10, 10, 10, 10), v2.Frame);
        super.Dispose ();
    }

    [Fact]
    public void AbsoluteLayout_Setting_Bounds_Location_NotEmpty ()
    {
        // TODO: Should we enforce Bounds.X/Y == 0? The code currently ignores value.X/Y which is
        // TODO: correct behavior, but is silent. Perhaps an exception?
        var frame = new Rectangle (1, 2, 3, 4);
        var newBounds = new Rectangle (10, 20, 30, 40);
        var view = new View { Frame = frame };
        view.Bounds = newBounds;
        Assert.Equal (new Rectangle (0, 0, 30, 40), view.Bounds);
        Assert.Equal (new Rectangle (1, 2, 30, 40), view.Frame);
    }

    [Fact]
    public void AbsoluteLayout_Setting_Bounds_Sets_Frame ()
    {
        var frame = new Rectangle (1, 2, 3, 4);
        var newBounds = new Rectangle (0, 0, 30, 40);

        var v = new View { Frame = frame };
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);

        v.Bounds = newBounds;
        Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
        Assert.Equal (newBounds, v.Bounds);
        Assert.Equal (new Rectangle (1, 2, newBounds.Width, newBounds.Height), v.Frame);
        Assert.Equal (new Rectangle (0, 0, newBounds.Width, newBounds.Height), v.Bounds);
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (30), v.Width);
        Assert.Equal (Dim.Sized (40), v.Height);

        newBounds = new Rectangle (0, 0, 3, 4);
        v.Bounds = newBounds;
        Assert.Equal (newBounds, v.Bounds);
        Assert.Equal (new Rectangle (1, 2, newBounds.Width, newBounds.Height), v.Frame);
        Assert.Equal (new Rectangle (0, 0, newBounds.Width, newBounds.Height), v.Bounds);
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (3), v.Width);
        Assert.Equal (Dim.Sized (4), v.Height);

        v.BorderStyle = LineStyle.Single;

        // Bounds should shrink
        Assert.Equal (new Rectangle (0, 0, 1, 2), v.Bounds);

        // Frame should not change
        Assert.Equal (new Rectangle (1, 2, 3, 4), v.Frame);
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (3), v.Width);
        Assert.Equal (Dim.Sized (4), v.Height);

        // Now set bounds bigger as before
        newBounds = new Rectangle (0, 0, 3, 4);
        v.Bounds = newBounds;
        Assert.Equal (newBounds, v.Bounds);

        // Frame grows because there's now a border
        Assert.Equal (new Rectangle (1, 2, 5, 6), v.Frame);
        Assert.Equal (new Rectangle (0, 0, newBounds.Width, newBounds.Height), v.Bounds);
        Assert.Equal (Pos.At (1), v.X);
        Assert.Equal (Pos.At (2), v.Y);
        Assert.Equal (Dim.Sized (5), v.Width);
        Assert.Equal (Dim.Sized (6), v.Height);
    }
}
