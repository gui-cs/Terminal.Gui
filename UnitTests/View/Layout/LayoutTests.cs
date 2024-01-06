using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests; 

public class LayoutTests {
	readonly ITestOutputHelper _output;

	public LayoutTests (ITestOutputHelper output) => _output = output;

	[Fact]
	public void TopologicalSort_Missing_Add ()
	{
		var root = new View ();
		var sub1 = new View ();
		root.Add (sub1);
		var sub2 = new View ();
		sub1.Width = Dim.Width (sub2);

		Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());

		sub2.Width = Dim.Width (sub1);

		Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());
		root.Dispose ();
		sub1.Dispose ();
		sub2.Dispose ();
	}

	[Fact]
	public void TopologicalSort_Recursive_Ref ()
	{
		var root = new View ();
		var sub1 = new View ();
		root.Add (sub1);
		var sub2 = new View ();
		root.Add (sub2);
		sub2.Width = Dim.Width (sub2);

		var exception = Record.Exception (root.LayoutSubviews);
		Assert.Null (exception);
		root.Dispose ();
		sub1.Dispose ();
		sub2.Dispose ();
	}

	[Fact]
	public void LayoutSubviews_No_SuperView ()
	{
		var root = new View ();
		var first = new View () { Id = "first", X = 1, Y = 2, Height = 3, Width = 4 };
		root.Add (first);

		var second = new View () { Id = "second" };
		root.Add (second);

		second.X = Pos.Right (first) + 1;

		root.LayoutSubviews ();

		Assert.Equal (6, second.Frame.X);
		root.Dispose ();
		first.Dispose ();
		second.Dispose ();
	}

	[Fact]
	public void LayoutSubviews_RootHas_SuperView ()
	{
		var top = new View ();
		var root = new View ();
		top.Add (root);

		var first = new View () { Id = "first", X = 1, Y = 2, Height = 3, Width = 4 };
		root.Add (first);

		var second = new View () { Id = "second" };
		root.Add (second);

		second.X = Pos.Right (first) + 1;

		root.LayoutSubviews ();

		Assert.Equal (6, second.Frame.X);
		root.Dispose ();
		top.Dispose ();
		first.Dispose ();
		second.Dispose ();
	}

	[Fact]
	public void LayoutSubviews_ViewThatRefsSubView_Throws ()
	{
		var root = new View ();
		var super = new View ();
		root.Add (super);
		var sub = new View ();
		super.Add (sub);
		super.Width = Dim.Width (sub);
		Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());
		root.Dispose ();
		super.Dispose ();
	}

	[Fact] [AutoInitShutdown]
	public void TrySetWidth_ForceValidatePosDim ()
	{
		var top = new View () {
			X = 0,
			Y = 0,
			Width = 80
		};

		var v = new View () {
			Width = Dim.Fill (),
			ValidatePosDim = true
		};
		top.Add (v);

		Assert.False (v.TrySetWidth (70, out int rWidth));
		Assert.Equal (70, rWidth);

		v.Width = Dim.Fill (1);
		Assert.False (v.TrySetWidth (70, out rWidth));
		Assert.Equal (69, rWidth);

		v.Width = 0;
		Assert.True (v.TrySetWidth (70, out rWidth));
		Assert.Equal (70, rWidth);
		Assert.False (v.IsInitialized);

		Application.Top.Add (top);
		Application.Begin (Application.Top);

		Assert.True (v.IsInitialized);
		v.Width = 75;
		Assert.True (v.TrySetWidth (60, out rWidth));
		Assert.Equal (60, rWidth);
	}

	[Fact] [AutoInitShutdown]
	public void TrySetHeight_ForceValidatePosDim ()
	{
		var top = new View () {
			X = 0,
			Y = 0,
			Height = 20
		};

		var v = new View () {
			Height = Dim.Fill (),
			ValidatePosDim = true
		};
		top.Add (v);

		Assert.False (v.TrySetHeight (10, out int rHeight));
		Assert.Equal (10, rHeight);

		v.Height = Dim.Fill (1);
		Assert.False (v.TrySetHeight (10, out rHeight));
		Assert.Equal (9, rHeight);

		v.Height = 0;
		Assert.True (v.TrySetHeight (10, out rHeight));
		Assert.Equal (10, rHeight);
		Assert.False (v.IsInitialized);

		Application.Top.Add (top);
		Application.Begin (Application.Top);

		Assert.True (v.IsInitialized);

		v.Height = 15;
		Assert.True (v.TrySetHeight (5, out rHeight));
		Assert.Equal (5, rHeight);
	}

	[Fact] [TestRespondersDisposed]
	public void GetCurrentWidth_TrySetWidth ()
	{
		var top = new View () {
			X = 0,
			Y = 0,
			Width = 80
		};

		var v = new View () {
			Width = Dim.Fill ()
		};
		top.Add (v);
		top.BeginInit ();
		top.EndInit ();
		top.LayoutSubviews ();

		Assert.False (v.AutoSize);
		Assert.True (v.TrySetWidth (0, out _));
		Assert.Equal (80, v.Frame.Width);

		v.Width = Dim.Fill (1);
		top.LayoutSubviews ();

		Assert.True (v.TrySetWidth (0, out _));
		Assert.Equal (79, v.Frame.Width);

		v.AutoSize = true;
		top.LayoutSubviews ();

		Assert.True (v.TrySetWidth (0, out _));
		top.Dispose ();
	}

	[Fact]
	public void GetCurrentHeight_TrySetHeight ()
	{
		var top = new View () {
			X = 0,
			Y = 0,
			Height = 20
		};

		var v = new View () {
			Height = Dim.Fill ()
		};
		top.Add (v);
		top.BeginInit ();
		top.EndInit ();
		top.LayoutSubviews ();

		Assert.False (v.AutoSize);
		Assert.True (v.TrySetHeight (0, out _));
		Assert.Equal (20, v.Frame.Height);

		v.Height = Dim.Fill (1);
		top.LayoutSubviews ();

		Assert.True (v.TrySetHeight (0, out _));
		Assert.Equal (19, v.Frame.Height);

		v.AutoSize = true;
		top.LayoutSubviews ();

		Assert.True (v.TrySetHeight (0, out _));
		top.Dispose ();
	}

	[Fact] [AutoInitShutdown]
	public void Width_Height_SetMinWidthHeight_Narrow_Wide_Runes ()
	{
		string text = $"First line{Environment.NewLine}Second line";
		var horizontalView = new View () {
			Width = 20,
			Height = 1,
			Text = text
		};
		var verticalView = new View () {
			Y = 3,
			Height = 20,
			Width = 1,
			Text = text,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var win = new Window () {
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			Text = "Window"
		};
		win.Add (horizontalView, verticalView);
		Application.Top.Add (win);
		var rs = Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (32, 32);

		Assert.False (horizontalView.AutoSize);
		Assert.False (verticalView.AutoSize);
		Assert.Equal (new Rect (0, 0, 20, 1), horizontalView.Frame);
		Assert.Equal (new Rect (0, 3, 1, 20), verticalView.Frame);
		string expected = @"
┌──────────────────────────────┐
│First line Second li          │
│                              │
│                              │
│F                             │
│i                             │
│r                             │
│s                             │
│t                             │
│                              │
│l                             │
│i                             │
│n                             │
│e                             │
│                              │
│S                             │
│e                             │
│c                             │
│o                             │
│n                             │
│d                             │
│                              │
│l                             │
│i                             │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
└──────────────────────────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 32, 32), pos);

		verticalView.Text = $"最初の行{Environment.NewLine}二行目";
		Application.Top.Draw ();
		Assert.Equal (new Rect (0, 3, 2, 20), verticalView.Frame);
		expected = @"
┌──────────────────────────────┐
│First line Second li          │
│                              │
│                              │
│最                            │
│初                            │
│の                            │
│行                            │
│                              │
│二                            │
│行                            │
│目                            │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
└──────────────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 32, 32), pos);
		Application.End (rs);
	}

	[Fact] [AutoInitShutdown]
	public void TextDirection_Toggle ()
	{
		var win = new Window () { Width = Dim.Fill (), Height = Dim.Fill () };
		var view = new View ();
		win.Add (view);
		Application.Top.Add (win);

		var rs = Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (22, 22);

		Assert.Equal (new Rect (0, 0, 22, 22), win.Frame);
		Assert.Equal (new Rect (0, 0, 22, 22), win.Margin.Frame);
		Assert.Equal (new Rect (0, 0, 22, 22), win.Border.Frame);
		Assert.Equal (new Rect (1, 1, 20, 20), win.Padding.Frame);
		Assert.False (view.AutoSize);
		Assert.Equal (TextDirection.LeftRight_TopBottom, view.TextDirection);
		Assert.Equal (Rect.Empty, view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(0)", view.Width.ToString ());
		Assert.Equal ("Absolute(0)", view.Height.ToString ());
		string expected = @"
┌────────────────────┐
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);

		view.Text = "Hello World";
		view.Width = 11;
		view.Height = 1;
		win.LayoutSubviews ();
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 11, 1), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(11)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());
		expected = @"
┌────────────────────┐
│Hello World         │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);

		view.AutoSize = true;
		view.Text = "Hello Worlds";
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 12, 1), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(11)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());
		expected = @"
┌────────────────────┐
│Hello Worlds        │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);

		view.TextDirection = TextDirection.TopBottom_LeftRight;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 11, 12), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(11)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());
		expected = @"
┌────────────────────┐
│H                   │
│e                   │
│l                   │
│l                   │
│o                   │
│                    │
│W                   │
│o                   │
│r                   │
│l                   │
│d                   │
│s                   │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);

		view.AutoSize = false;
		view.Height = 1;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 11, 1), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(11)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());
		expected = @"
┌────────────────────┐
│HelloWorlds         │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);

		view.PreserveTrailingSpaces = true;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 11, 1), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(11)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());
		expected = @"
┌────────────────────┐
│Hello World         │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);

		view.PreserveTrailingSpaces = false;
		var f = view.Frame;
		view.Width = f.Height;
		view.Height = f.Width;
		view.TextDirection = TextDirection.TopBottom_LeftRight;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 1, 11), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(1)", view.Width.ToString ());
		Assert.Equal ("Absolute(11)", view.Height.ToString ());
		expected = @"
┌────────────────────┐
│H                   │
│e                   │
│l                   │
│l                   │
│o                   │
│                    │
│W                   │
│o                   │
│r                   │
│l                   │
│d                   │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);

		view.AutoSize = true;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 1, 12), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(1)", view.Width.ToString ());
		Assert.Equal ("Absolute(12)", view.Height.ToString ());
		expected = @"
┌────────────────────┐
│H                   │
│e                   │
│l                   │
│l                   │
│o                   │
│                    │
│W                   │
│o                   │
│r                   │
│l                   │
│d                   │
│s                   │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);
		Application.End (rs);
	}

	[Fact] [AutoInitShutdown]
	public void Width_Height_AutoSize_True_Stay_True_If_TextFormatter_Size_Fit ()
	{
		string text = $"Fi_nish 終";
		var horizontalView = new View () {
			Id = "horizontalView",
			AutoSize = true,
			HotKeySpecifier = (Rune)'_',
			Text = text
		};
		var verticalView = new View () {
			Id = "verticalView",
			Y = 3,
			AutoSize = true,
			HotKeySpecifier = (Rune)'_',
			Text = text,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var win = new Window () {
			Id = "win",
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			Text = "Window"
		};
		win.Add (horizontalView, verticalView);
		Application.Top.Add (win);
		var rs = Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (22, 22);

		Assert.True (horizontalView.AutoSize);
		Assert.True (verticalView.AutoSize);
		Assert.Equal (new Size (10, 1), horizontalView.TextFormatter.Size);
		Assert.Equal (new Size (2, 9), verticalView.TextFormatter.Size);
		Assert.Equal (new Rect (0, 0, 9, 1), horizontalView.Frame);
		Assert.Equal ("Absolute(0)", horizontalView.X.ToString ());
		Assert.Equal ("Absolute(0)", horizontalView.Y.ToString ());
		// BUGBUG - v2 - With v1 AutoSize = true Width/Height should always grow or keep initial value, 
		// but in v2, autosize will be replaced by Dim.Fit. Disabling test for now.
		Assert.Equal ("Absolute(9)", horizontalView.Width.ToString ());
		Assert.Equal ("Absolute(1)", horizontalView.Height.ToString ());
		Assert.Equal (new Rect (0, 3, 2, 8), verticalView.Frame);
		Assert.Equal ("Absolute(0)", verticalView.X.ToString ());
		Assert.Equal ("Absolute(3)", verticalView.Y.ToString ());
		Assert.Equal ("Absolute(2)", verticalView.Width.ToString ());
		Assert.Equal ("Absolute(8)", verticalView.Height.ToString ());
		string expected = @"
┌────────────────────┐
│Finish 終           │
│                    │
│                    │
│F                   │
│i                   │
│n                   │
│i                   │
│s                   │
│h                   │
│                    │
│終                  │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);

		verticalView.Text = $"最初_の行二行目";
		Application.Top.Draw ();
		Assert.True (horizontalView.AutoSize);
		Assert.True (verticalView.AutoSize);
		// height was initialized with 8 and can only grow or keep initial value
		Assert.Equal (new Rect (0, 3, 2, 8), verticalView.Frame);
		Assert.Equal ("Absolute(0)", verticalView.X.ToString ());
		Assert.Equal ("Absolute(3)", verticalView.Y.ToString ());
		Assert.Equal ("Absolute(2)", verticalView.Width.ToString ());
		Assert.Equal ("Absolute(8)", verticalView.Height.ToString ());
		expected = @"
┌────────────────────┐
│Finish 終           │
│                    │
│                    │
│最                  │
│初                  │
│の                  │
│行                  │
│二                  │
│行                  │
│目                  │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 22, 22), pos);
		Application.End (rs);
	}

	// Tested in AbsoluteLayoutTests.cs
	// public void Pos_Dim_Are_Null_If_Not_Initialized_On_Constructor_IsAdded_False ()

	[Theory] [AutoInitShutdown]
	[InlineData (1)]
	[InlineData (2)]
	[InlineData (3)]
	[InlineData (4)]
	[InlineData (5)]
	[InlineData (6)]
	[InlineData (7)]
	[InlineData (8)]
	[InlineData (9)]
	[InlineData (10)]
	public void Dim_CenteredSubView_85_Percent_Height (int height)
	{
		var win = new Window () {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};

		var subview = new Window () {
			X = Pos.Center (),
			Y = Pos.Center (),
			Width = Dim.Percent (85),
			Height = Dim.Percent (85)
		};

		win.Add (subview);

		var rs = Application.Begin (win);
		bool firstIteration = false;


		((FakeDriver)Application.Driver).SetBufferSize (20, height);
		Application.RunIteration (ref rs, ref firstIteration);
		string expected = string.Empty;

		switch (height) {
		case 1:
			//Assert.Equal (new Rect (0, 0, 17, 0), subview.Frame);
			expected = @"
────────────────────";
			break;
		case 2:
			//Assert.Equal (new Rect (0, 0, 17, 1), subview.Frame);
			expected = @"
┌──────────────────┐
└──────────────────┘
";
			break;
		case 3:
			//Assert.Equal (new Rect (0, 0, 17, 2), subview.Frame);
			expected = @"
┌──────────────────┐
│                  │
└──────────────────┘
";
			break;
		case 4:
			//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
			expected = @"
┌──────────────────┐
│ ───────────────  │
│                  │
└──────────────────┘";
			break;
		case 5:
			//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
			expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ └─────────────┘  │
│                  │
└──────────────────┘";
			break;
		case 6:
			//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
			expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";
			break;
		case 7:
			//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
			expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";
			break;
		case 8:
			//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
			expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";
			break;
		case 9:
			//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
			expected = @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";
			break;
		case 10:
			//Assert.Equal (new Rect (0, 0, 17, 3), subview.Frame);
			expected = @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";
			break;
		}
		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);
	}

	[Theory] [AutoInitShutdown]
	[InlineData (1)]
	[InlineData (2)]
	[InlineData (3)]
	[InlineData (4)]
	[InlineData (5)]
	[InlineData (6)]
	[InlineData (7)]
	[InlineData (8)]
	[InlineData (9)]
	[InlineData (10)]
	public void Dim_CenteredSubView_85_Percent_Width (int width)
	{
		var win = new Window () {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};

		var subview = new Window () {
			X = Pos.Center (),
			Y = Pos.Center (),
			Width = Dim.Percent (85),
			Height = Dim.Percent (85)
		};

		win.Add (subview);

		var rs = Application.Begin (win);
		bool firstIteration = false;


		((FakeDriver)Application.Driver).SetBufferSize (width, 7);
		Application.RunIteration (ref rs, ref firstIteration);
		string expected = string.Empty;

		switch (width) {
		case 1:
			Assert.Equal (new Rect (0, 0, 0, 4), subview.Frame);
			expected = @"
│
│
│
│
│
│
│";
			break;
		case 2:
			Assert.Equal (new Rect (0, 0, 0, 4), subview.Frame);
			expected = @"
┌┐
││
││
││
││
││
└┘";
			break;
		case 3:
			Assert.Equal (new Rect (0, 0, 0, 4), subview.Frame);
			expected = @"
┌─┐
│ │
│ │
│ │
│ │
│ │
└─┘";
			break;
		case 4:
			Assert.Equal (new Rect (0, 0, 1, 4), subview.Frame);
			expected = @"
┌──┐
││ │
││ │
││ │
││ │
│  │
└──┘";
			break;
		case 5:
			Assert.Equal (new Rect (0, 0, 2, 4), subview.Frame);
			expected = @"
┌───┐
│┌┐ │
│││ │
│││ │
│└┘ │
│   │
└───┘";
			break;
		case 6:
			Assert.Equal (new Rect (0, 0, 3, 4), subview.Frame);
			expected = @"
┌────┐
│┌─┐ │
││ │ │
││ │ │
│└─┘ │
│    │
└────┘";
			break;
		case 7:
			Assert.Equal (new Rect (0, 0, 4, 4), subview.Frame);
			expected = @"
┌─────┐
│┌──┐ │
││  │ │
││  │ │
│└──┘ │
│     │
└─────┘";
			break;
		case 8:
			Assert.Equal (new Rect (0, 0, 5, 4), subview.Frame);
			expected = @"
┌──────┐
│┌───┐ │
││   │ │
││   │ │
│└───┘ │
│      │
└──────┘";
			break;
		case 9:
			Assert.Equal (new Rect (1, 0, 5, 4), subview.Frame);
			expected = @"
┌───────┐
│ ┌───┐ │
│ │   │ │
│ │   │ │
│ └───┘ │
│       │
└───────┘";
			break;
		case 10:
			Assert.Equal (new Rect (1, 0, 6, 4), subview.Frame);
			expected = @"
┌────────┐
│ ┌────┐ │
│ │    │ │
│ │    │ │
│ └────┘ │
│        │
└────────┘";
			break;
		}
		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);
	}

	[Fact] [AutoInitShutdown]
	public void PosCombine_DimCombine_View_With_SubViews ()
	{
		bool clicked = false;
		var top = Application.Top;
		var win1 = new Window () { Id = "win1", Width = 20, Height = 10 };
		var label = new Label ("[ ok ]");
		var win2 = new Window () { Id = "win2", Y = Pos.Bottom (label) + 1, Width = 10, Height = 3 };
		var view1 = new View () { Id = "view1", Width = Dim.Fill (), Height = 1, CanFocus = true };
		view1.MouseClick += (sender, e) => clicked = true;
		var view2 = new View () { Id = "view2", Width = Dim.Fill (1), Height = 1, CanFocus = true };

		view1.Add (view2);
		win2.Add (view1);
		win1.Add (label, win2);
		top.Add (win1);

		var rs = Application.Begin (top);

		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│[ ok ]            │
│                  │
│┌────────┐        │
││        │        │
│└────────┘        │
│                  │
│                  │
│                  │
└──────────────────┘", _output);
		Assert.Equal (new Rect (0, 0, 80, 25), top.Frame);
		Assert.Equal (new Rect (0, 0, 6, 1), label.Frame);
		Assert.Equal (new Rect (0, 0, 20, 10), win1.Frame);
		Assert.Equal (new Rect (0, 2, 10, 3), win2.Frame);
		Assert.Equal (new Rect (0, 0, 8, 1), view1.Frame);
		Assert.Equal (new Rect (0, 0, 7, 1), view2.Frame);
		var foundView = View.FindDeepestView (top, 9, 4, out int rx, out int ry);
		Assert.Equal (foundView, view1);
		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () {
			X = 9,
			Y = 4,
			Flags = MouseFlags.Button1Clicked
		}));
		Assert.True (clicked);

		Application.End (rs);
	}

	[Fact] [TestRespondersDisposed]
	public void Draw_Vertical_Throws_IndexOutOfRangeException_With_Negative_Bounds ()
	{
		Application.Init (new FakeDriver ());

		var top = Application.Top;

		var view = new View ("view") {
			Y = -2,
			Height = 10,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		top.Add (view);

		Application.Iteration += (s, a) => {
			Assert.Equal (-2, view.Y);

			Application.RequestStop ();
		};

		try {
			Application.Run ();
		} catch (IndexOutOfRangeException ex) {
			// After the fix this exception will not be caught.
			Assert.IsType<IndexOutOfRangeException> (ex);
		}

		// Shutdown must be called to safely clean up Application if Init has been called
		Application.Shutdown ();
	}

	[Fact] [TestRespondersDisposed]
	public void Draw_Throws_IndexOutOfRangeException_With_Negative_Bounds ()
	{
		Application.Init (new FakeDriver ());

		var top = Application.Top;

		var view = new View ("view") { X = -2 };
		top.Add (view);

		Application.Iteration += (s, a) => {
			Assert.Equal (-2, view.X);

			Application.RequestStop ();
		};

		try {
			Application.Run ();
		} catch (IndexOutOfRangeException ex) {
			// After the fix this exception will not be caught.
			Assert.IsType<IndexOutOfRangeException> (ex);
		}

		// Shutdown must be called to safely clean up Application if Init has been called
		Application.Shutdown ();
	}
}