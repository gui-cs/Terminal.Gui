using Xunit;
using Xunit.Abstractions;

//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ViewTests;

public class AbsoluteLayoutTests {
	readonly ITestOutputHelper _output;

	public AbsoluteLayoutTests (ITestOutputHelper output) => _output = output;

	[Fact]
	[TestRespondersDisposed]
	public void AbsoluteLayout_Constructor ()
	{
		var frame = Rect.Empty;
		var v = new View (frame);
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		Assert.Equal (frame,                                      v.Frame);
		Assert.Equal (new Rect (0, 0, frame.Width, frame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (0),                                 v.X);
		Assert.Equal (Pos.At (0),                                 v.Y);
		Assert.Equal (Dim.Sized (0),                              v.Width);
		Assert.Equal (Dim.Sized (0),                              v.Height);
		v.Dispose ();

		frame = new Rect (1, 2, 3, 4);
		v = new View (frame);
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		Assert.Equal (frame,                                      v.Frame);
		Assert.Equal (new Rect (0, 0, frame.Width, frame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (1),                                 v.X);
		Assert.Equal (Pos.At (2),                                 v.Y);
		Assert.Equal (Dim.Sized (3),                              v.Width);
		Assert.Equal (Dim.Sized (4),                              v.Height);
		v.Dispose ();

		v = new View (frame, "v");
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		Assert.Equal (frame,                                      v.Frame);
		Assert.Equal (new Rect (0, 0, frame.Width, frame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (1),                                 v.X);
		Assert.Equal (Pos.At (2),                                 v.Y);
		Assert.Equal (Dim.Sized (3),                              v.Width);
		Assert.Equal (Dim.Sized (4),                              v.Height);
		v.Dispose ();

		v = new View (frame.X, frame.Y, "v");
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		// BUGBUG: v2 - I think the default size should be 0,0 not 1,1
		Assert.Equal (new Rect (frame.X, frame.Y, 1, 1), v.Frame);
		Assert.Equal (new Rect (0,       0,       1, 1), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (1),                        v.X);
		Assert.Equal (Pos.At (2),                        v.Y);
		Assert.Equal (Dim.Sized (1),                     v.Width);
		Assert.Equal (Dim.Sized (1),                     v.Height);
		v.Dispose ();

		v = new View ();
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		Assert.Equal (new Rect (0, 0, 0, 0), v.Frame);
		Assert.Equal (new Rect (0, 0, 0, 0), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (0),            v.X);
		Assert.Equal (Pos.At (0),            v.Y);
		Assert.Equal (Dim.Sized (0),         v.Width);
		Assert.Equal (Dim.Sized (0),         v.Height);
		v.Dispose ();

		v = new View {
			X = frame.X,
			Y = frame.Y,
			Width = frame.Width,
			Height = frame.Height
		};
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		Assert.Equal (new Rect (frame.X, frame.Y, 3, 4), v.Frame);
		Assert.Equal (new Rect (0,       0,       3, 4), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (1),                        v.X);
		Assert.Equal (Pos.At (2),                        v.Y);
		Assert.Equal (Dim.Sized (3),                     v.Width);
		Assert.Equal (Dim.Sized (4),                     v.Height);
		v.Dispose ();

	}

	[Fact]
	[TestRespondersDisposed]
	public void AbsoluteLayout_Change_Frame ()
	{
		var frame = new Rect (1,    2, 3,  4);
		var newFrame = new Rect (1, 2, 30, 40);

		var v = new View (frame);
		v.Frame = newFrame;
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		Assert.Equal (newFrame,                                         v.Frame);
		Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (1),                                       v.X);
		Assert.Equal (Pos.At (2),                                       v.Y);
		Assert.Equal (Dim.Sized (30),                                    v.Width);
		Assert.Equal (Dim.Sized (40),                                    v.Height);
		v.Dispose ();

		v = new View (frame.X, frame.Y, "v");
		v.Frame = newFrame;
		Assert.Equal (newFrame,                                         v.Frame);
		Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (1),                                       v.X);
		Assert.Equal (Pos.At (2),                                       v.Y);
		Assert.Equal (Dim.Sized (30),                                   v.Width);
		Assert.Equal (Dim.Sized (40),                                   v.Height);
		v.Dispose ();

		newFrame = new Rect (10, 20, 30, 40);
		v = new View (frame);
		v.Frame = newFrame;
		Assert.Equal (newFrame,                                         v.Frame);
		Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (10),                                       v.X);
		Assert.Equal (Pos.At (20),                                       v.Y);
		Assert.Equal (Dim.Sized (30),                                   v.Width);
		Assert.Equal (Dim.Sized (40),                                   v.Height);
		v.Dispose ();

		v = new View (frame.X, frame.Y, "v");
		v.Frame = newFrame;
		Assert.Equal (newFrame,                                         v.Frame);
		Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (10),                                       v.X);
		Assert.Equal (Pos.At (20),                                       v.Y);
		Assert.Equal (Dim.Sized (30),                                   v.Width);
		Assert.Equal (Dim.Sized (40),                                   v.Height);
		v.Dispose ();
	}

	[Fact]
	[TestRespondersDisposed]
	public void AbsoluteLayout_Change_Height_or_Width_Absolute ()
	{
		var frame = new Rect (1,    2, 3,  4);
		var newFrame = new Rect (1, 2, 30, 40);

		var v = new View (frame);
		v.Height = newFrame.Height;
		v.Width = newFrame.Width;
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		Assert.Equal (newFrame,                                         v.Frame);
		Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal (Pos.At (1),                                      v.X);
		Assert.Equal (Pos.At (2),                                      v.Y);
		Assert.Equal ($"Absolute({newFrame.Height})",                   v.Height.ToString ());
		Assert.Equal ($"Absolute({newFrame.Width})",                    v.Width.ToString ());
		v.Dispose ();
	}

	[Fact]
	[TestRespondersDisposed]
	public void AbsoluteLayout_Change_Height_or_Width_MakesComputed ()
	{
		var v = new View (Rect.Empty);
		v.Height = Dim.Fill ();
		v.Width = Dim.Fill ();
		Assert.True (v.LayoutStyle == LayoutStyle.Computed); 
		v.Dispose ();
	}

	[Fact]
	[TestRespondersDisposed]
	public void AbsoluteLayout_Change_X_or_Y_Absolute ()
	{
		var frame = new Rect (1,     2,  3, 4);
		var newFrame = new Rect (10, 20, 3, 4);

		var v = new View (frame);
		v.X = newFrame.X;
		v.Y = newFrame.Y;
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		Assert.Equal (newFrame,                                         v.Frame);
		Assert.Equal (new Rect (0, 0, newFrame.Width, newFrame.Height), v.Bounds); // With Absolute Bounds *is* deterministic before Layout
		Assert.Equal ($"Absolute({newFrame.X})",                        v.X.ToString ());
		Assert.Equal ($"Absolute({newFrame.Y})",                        v.Y.ToString ());
		Assert.Equal (Dim.Sized (3),                                   v.Width);
		Assert.Equal (Dim.Sized (4),                                   v.Height);
		v.Dispose ();
	}

	[Fact]
	[TestRespondersDisposed]
	public void AbsoluteLayout_Change_X_or_Y_MakesComputed ()
	{
		var v = new View (Rect.Empty);
		v.X = Pos.Center ();
		v.Y = Pos.Center ();
		Assert.True (v.LayoutStyle == LayoutStyle.Computed);
		v.Dispose ();
	}
	
	[Fact]
	[TestRespondersDisposed]
	public void AbsoluteLayout_Change_X_Y_Height_Width_Absolute ()
	{
		var v = new View (Rect.Empty);
		v.X = 1;
		v.Y = 2;
		v.Height = 3;
		v.Width = 4;
		Assert.True (v.LayoutStyle == LayoutStyle.Absolute);
		v.Dispose ();

		v = new View (Rect.Empty);
		v.X = Pos.Center ();
		v.Y = Pos.Center ();
		v.Width = Dim.Fill ();
		v.Height = Dim.Fill ();
		Assert.True (v.LayoutStyle == LayoutStyle.Computed);
		v.Dispose ();

		v = new View (Rect.Empty);
		v.X = Pos.Center ();
		v.Y = Pos.Center ();
		v.Width = Dim.Fill ();
		v.Height = Dim.Fill ();
		Assert.True (v.LayoutStyle == LayoutStyle.Computed);

		v.X = 1;
		Assert.True (v.LayoutStyle == LayoutStyle.Computed);
		v.Dispose ();

		v = new View (Rect.Empty);
		v.X = Pos.Center ();
		v.Y = Pos.Center ();
		v.Width = Dim.Fill ();
		v.Height = Dim.Fill ();
		Assert.True (v.LayoutStyle == LayoutStyle.Computed);
		
		v.Y = 2;
		Assert.True (v.LayoutStyle == LayoutStyle.Computed);
		v.Dispose ();

		v = new View (Rect.Empty);
		v.X = Pos.Center ();
		v.Y = Pos.Center ();
		v.Width = Dim.Fill ();
		v.Height = Dim.Fill ();
		Assert.True (v.LayoutStyle == LayoutStyle.Computed);

		v.Width = 3;
		Assert.True (v.LayoutStyle == LayoutStyle.Computed); 
		v.Dispose ();

		v = new View (Rect.Empty);
		v.X = Pos.Center ();
		v.Y = Pos.Center ();
		v.Width = Dim.Fill ();
		v.Height = Dim.Fill ();
		Assert.True (v.LayoutStyle == LayoutStyle.Computed);

		v.Height = 3;
		Assert.True (v.LayoutStyle == LayoutStyle.Computed);
		v.Dispose ();

		v = new View (Rect.Empty);
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
	public void AbsoluteLayout_LayoutSubviews ()
	{
		var superRect = new Rect (0, 0, 100, 100);
		var super = new View (superRect, "super");
		Assert.True (super.LayoutStyle == LayoutStyle.Absolute);
		var v1 = new View {
			X = 0,
			Y = 0,
			Width = 10,
			Height = 10
		};
		Assert.True (v1.LayoutStyle == LayoutStyle.Absolute);

		var v2 = new View {
			X = 10,
			Y = 10,
			Width = 10,
			Height = 10
		};
		Assert.True (v2.LayoutStyle == LayoutStyle.Absolute);

		super.Add (v1, v2);
		Assert.True (v1.LayoutStyle == LayoutStyle.Absolute);
		Assert.True (v2.LayoutStyle == LayoutStyle.Absolute);

		super.LayoutSubviews ();
		Assert.Equal (new Rect (0,  0,  10, 10), v1.Frame);
		Assert.Equal (new Rect (10, 10, 10, 10), v2.Frame);
		super.Dispose ();
	}
}