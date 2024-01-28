using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
///         Tests of the  <see cref="View.Text" /> property with <see cref="View.AutoSize" /> set to false.
/// </summary>
public class TextTests {
	readonly ITestOutputHelper _output;

	public TextTests (ITestOutputHelper output) => _output = output;

	public static TheoryData<TextDirection, Size> ValidTextDirectionSize =>
		new () {
			{ TextDirection.LeftRight_TopBottom, new Size (14, 1) },
			{ TextDirection.TopBottom_LeftRight, new Size (2, 7) }
		};

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_View_IsEmpty_False_Return_Null_Lines ()
	{
		var text = "Views";
		var view = new View {
			Width = Dim.Fill () - text.Length,
			Height = 1,
			Text = text
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (view);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (10, 4);

		Assert.Equal (5, text.Length);
		Assert.False (view.AutoSize);
		Assert.Equal (new Rect (0, 0, 3, 1), view.Frame);
		Assert.Equal (new Size (3, 1), view.TextFormatter.Size);
		Assert.Equal (new List<string> { "Vie" }, view.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
		var expected = @"
┌────────┐
│Vie     │
│        │
└────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);

		text = "0123456789";
		Assert.Equal (10, text.Length);
		view.Width = Dim.Fill () - text.Length;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 0, 1), view.Frame);
		Assert.Equal (new Size (0, 1), view.TextFormatter.Size);
		Assert.Equal (new List<string> { string.Empty }, view.TextFormatter.Lines);
		expected = @"
┌────────┐
│        │
│        │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_View_IsEmpty_True_Minimum_Height ()
	{
		var text = "Views";
		var view = new View {
			Width = Dim.Fill () - text.Length,
			Text = text
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (view);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (10, 4);

		Assert.Equal (5, text.Length);
		Assert.False (view.AutoSize);
		Assert.Equal (new Rect (0, 0, 3, 1), view.Frame);
		Assert.Equal (new Size (3, 1), view.TextFormatter.Size);
		Assert.Single (view.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
		var expected = @"
┌────────┐
│Vie     │
│        │
└────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);

		text = "0123456789";
		Assert.Equal (10, text.Length);
		view.Width = Dim.Fill () - text.Length;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 0, 1), view.Frame);
		Assert.Equal (new Size (0, 1), view.TextFormatter.Size);
		var exception = Record.Exception (() =>
			Assert.Equal (new List<string> { string.Empty }, view.TextFormatter.Lines));
		Assert.Null (exception);
		expected = @"
┌────────┐
│        │
│        │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_Label_IsEmpty_True_Return_Null_Lines ()
	{
		var text = "Label";
		var label = new Label {
			Width = Dim.Fill () - text.Length,
			Height = 1,
			Text = text,
			AutoSize = false
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (label);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (10, 4);

		Assert.Equal (5, text.Length);
		Assert.False (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 3, 1), label.Frame);
		Assert.Equal (new Size (3, 1), label.TextFormatter.Size);
		Assert.Equal (new List<string> { "Lab" }, label.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
		var expected = @"
┌────────┐
│Lab     │
│        │
└────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);

		text = "0123456789";
		Assert.Equal (10, text.Length);
		label.Width = Dim.Fill () - text.Length;
		Application.Refresh ();

		Assert.False (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 0, 1), label.Frame);
		Assert.Equal (new Size (0, 1), label.TextFormatter.Size);
		Assert.Equal (new List<string> { string.Empty }, label.TextFormatter.Lines);
		expected = @"
┌────────┐
│        │
│        │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_Label_Height_Zero_Returns_Minimum_Height ()
	{
		var text = "Label";
		var label = new Label {
			Width = Dim.Fill () - text.Length,
			Text = text,
			AutoSize = false
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (label);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (10, 4);

		Assert.Equal (5, text.Length);
		Assert.False (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 3, 1), label.Frame);
		Assert.Equal (new Size (3, 1), label.TextFormatter.Size);
		Assert.Single (label.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
		var expected = @"
┌────────┐
│Lab     │
│        │
└────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);

		text = "0123456789";
		Assert.Equal (10, text.Length);
		label.Width = Dim.Fill () - text.Length;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 0, 1), label.Frame);
		Assert.Equal (new Size (0, 1), label.TextFormatter.Size);
		var exception = Record.Exception (() =>
			Assert.Equal (new List<string> { string.Empty }, label.TextFormatter.Lines));
		Assert.Null (exception);
		expected = @"
┌────────┐
│        │
│        │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_View_Width_Null_Returns_Host_Frame_Width ()
	{
		var text = "Views";
		var view = new View {
			TextDirection = TextDirection.TopBottom_LeftRight,
			Height = Dim.Fill () - text.Length,
			Text = text
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (view);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (4, 10);

		Assert.Equal (5, text.Length);
		Assert.False (view.AutoSize);
		Assert.Equal (new Rect (0, 0, 1, 3), view.Frame);
		Assert.Equal (new Size (1, 3), view.TextFormatter.Size);
		Assert.Single (view.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 4, 10), win.Frame);
		Assert.Equal (new Rect (0, 0, 4, 10), Application.Top.Frame);
		var expected = @"
┌──┐
│V │
│i │
│e │
│  │
│  │
│  │
│  │
│  │
└──┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 4, 10), pos);

		text = "0123456789";
		Assert.Equal (10, text.Length);
		view.Height = Dim.Fill () - text.Length;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 1, 0), view.Frame);
		Assert.Equal (new Size (1, 0), view.TextFormatter.Size);
		var exception = Record.Exception (() =>
			Assert.Equal (new List<string> { string.Empty }, view.TextFormatter.Lines));
		Assert.Null (exception);
		expected = @"
┌──┐
│  │
│  │
│  │
│  │
│  │
│  │
│  │
│  │
└──┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 4, 10), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_View_Width_Zero_Returns_Minimum_Width_With_Wide_Rune ()
	{
		var text = "界View";
		var view = new View {
			TextDirection = TextDirection.TopBottom_LeftRight,
			Height = Dim.Fill () - text.Length,
			Text = text
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (view);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (4, 10);

		Assert.Equal (5, text.Length);
		Assert.False (view.AutoSize);
		Assert.Equal (new Rect (0, 0, 2, 3), view.Frame);
		Assert.Equal (new Size (2, 3), view.TextFormatter.Size);
		Assert.Single (view.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 4, 10), win.Frame);
		Assert.Equal (new Rect (0, 0, 4, 10), Application.Top.Frame);
		var expected = @"
┌──┐
│界│
│V │
│i │
│  │
│  │
│  │
│  │
│  │
└──┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 4, 10), pos);

		text = "0123456789";
		Assert.Equal (10, text.Length);
		view.Height = Dim.Fill () - text.Length;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 2, 0), view.Frame);
		Assert.Equal (new Size (2, 0), view.TextFormatter.Size);
		var exception = Record.Exception (() =>
			Assert.Equal (new List<string> { string.Empty }, view.TextFormatter.Lines));
		Assert.Null (exception);
		expected = @"
┌──┐
│  │
│  │
│  │
│  │
│  │
│  │
│  │
│  │
└──┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 4, 10), pos);
	}


	[Fact]
	public void AutoSize_False_If_Text_Empty ()
	{
		var view1 = new View ();
		var view2 = new View { Text = "" };

		Assert.False (view1.AutoSize);
		Assert.False (view2.AutoSize);
		view1.Dispose ();
		view2.Dispose ();
	}

	[Fact]
	public void AutoSize_False_If_Text_Is_Not_Empty ()
	{
		var view1 = new View ();
		view1.Text = "Hello World";
		var view2 = new View { Text = "Hello World" };

		Assert.False (view1.AutoSize);
		Assert.False (view2.AutoSize);
		view1.Dispose ();
		view2.Dispose ();
	}

	[Fact]
	public void AutoSize_False_ResizeView_Is_Always_False ()
	{
		var super = new View ();
		var label = new Label { AutoSize = false };
		super.Add (label);

		label.Text = "New text";
		super.LayoutSubviews ();

		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,0,1)", label.Bounds.ToString ());
		super.Dispose ();
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_ResizeView_With_Dim_Fill_After_IsInitialized ()
	{
		var win = new Window { Width = 30, Height = 80 };
		var label = new Label { AutoSize = false, Width = Dim.Fill (), Height = Dim.Fill () };
		win.Add (label);
		Application.Top.Add (win);

		Assert.False (label.AutoSize);
		// BUGBUG: IsInitialized need to be true before calculating
		Assert.Equal ("(0,0,0,0)", label.Bounds.ToString ());

		label.Text = "New text\nNew line";
		Application.Top.LayoutSubviews ();

		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,28,78)", label.Bounds.ToString ());
		Assert.False (label.IsInitialized);

		var rs = Application.Begin (Application.Top);
		Assert.True (label.IsInitialized);
		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,28,78)", label.Bounds.ToString ());
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_Equal_Before_And_After_IsInitialized_With_Differents_Orders ()
	{
		var view1 = new View { Text = "Say Hello view1 你", AutoSize = false, Width = 10, Height = 5 };
		var view2 = new View { Text = "Say Hello view2 你", Width = 10, Height = 5, AutoSize = false };
		var view3 = new View { AutoSize = false, Width = 10, Height = 5, Text = "Say Hello view3 你" };
		var view4 = new View {
			Text = "Say Hello view4 你",
			AutoSize = false,
			Width = 10,
			Height = 5,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var view5 = new View {
			Text = "Say Hello view5 你",
			Width = 10,
			Height = 5,
			AutoSize = false,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var view6 = new View {
			AutoSize = false,
			Width = 10,
			Height = 5,
			TextDirection = TextDirection.TopBottom_LeftRight,
			Text = "Say Hello view6 你"
		};
		Application.Top.Add (view1, view2, view3, view4, view5, view6);

		Assert.False (view1.IsInitialized);
		Assert.False (view2.IsInitialized);
		Assert.False (view3.IsInitialized);
		Assert.False (view4.IsInitialized);
		Assert.False (view5.IsInitialized);
		Assert.False (view1.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view1.Frame);
		Assert.Equal ("Absolute(10)", view1.Width.ToString ());
		Assert.Equal ("Absolute(5)", view1.Height.ToString ());
		Assert.False (view2.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view2.Frame);
		Assert.Equal ("Absolute(10)", view2.Width.ToString ());
		Assert.Equal ("Absolute(5)", view2.Height.ToString ());
		Assert.False (view3.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view3.Frame);
		Assert.Equal ("Absolute(10)", view3.Width.ToString ());
		Assert.Equal ("Absolute(5)", view3.Height.ToString ());
		Assert.False (view4.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view4.Frame);
		Assert.Equal ("Absolute(10)", view4.Width.ToString ());
		Assert.Equal ("Absolute(5)", view4.Height.ToString ());
		Assert.False (view5.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view5.Frame);
		Assert.Equal ("Absolute(10)", view5.Width.ToString ());
		Assert.Equal ("Absolute(5)", view5.Height.ToString ());
		Assert.False (view6.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view6.Frame);
		Assert.Equal ("Absolute(10)", view6.Width.ToString ());
		Assert.Equal ("Absolute(5)", view6.Height.ToString ());

		var rs = Application.Begin (Application.Top);

		Assert.True (view1.IsInitialized);
		Assert.True (view2.IsInitialized);
		Assert.True (view3.IsInitialized);
		Assert.True (view4.IsInitialized);
		Assert.True (view5.IsInitialized);
		Assert.False (view1.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view1.Frame);
		Assert.Equal ("Absolute(10)", view1.Width.ToString ());
		Assert.Equal ("Absolute(5)", view1.Height.ToString ());
		Assert.False (view2.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view2.Frame);
		Assert.Equal ("Absolute(10)", view2.Width.ToString ());
		Assert.Equal ("Absolute(5)", view2.Height.ToString ());
		Assert.False (view3.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view3.Frame);
		Assert.Equal ("Absolute(10)", view3.Width.ToString ());
		Assert.Equal ("Absolute(5)", view3.Height.ToString ());
		Assert.False (view4.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view4.Frame);
		Assert.Equal ("Absolute(10)", view4.Width.ToString ());
		Assert.Equal ("Absolute(5)", view4.Height.ToString ());
		Assert.False (view5.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view5.Frame);
		Assert.Equal ("Absolute(10)", view5.Width.ToString ());
		Assert.Equal ("Absolute(5)", view5.Height.ToString ());
		Assert.False (view6.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view6.Frame);
		Assert.Equal ("Absolute(10)", view6.Width.ToString ());
		Assert.Equal ("Absolute(5)", view6.Height.ToString ());
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_Width_Height_SetMinWidthHeight_Narrow_Wide_Runes ()
	{
		var text = $"First line{Environment.NewLine}Second line";
		var horizontalView = new View {
			Width = 20,
			Height = 1,
			Text = text
		};
		var verticalView = new View {
			Y = 3,
			Height = 20,
			Width = 1,
			Text = text,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var win = new Window {
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
		var expected = @"
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

		verticalView.Text = $"最初の行{Environment.NewLine}二行目";
		Application.Top.Draw ();
		// BUGBUG: #3127 - If AutoSize == false, setting text should NOT change the size of the view.
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
		Application.End (rs);
	}

	[Theory]
	[MemberData (nameof (ValidTextDirectionSize))]
	public void GetAutoSize_Ignores_HotKeySpecifier (TextDirection textDirection, Size size)
	{
		var view = new View {
			TextDirection = textDirection,
			Text = "最初_の行二行目",
			AutoSize = true,
			HotKeySpecifier = (Rune)'_'
		};
		Assert.Equal (15, view.Text.GetColumns ());
		Assert.Equal (8, view.Text.GetRuneCount ());
		Assert.Equal (size, view.GetAutoSize ());
	}
}