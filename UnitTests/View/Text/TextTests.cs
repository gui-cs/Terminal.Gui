using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Tests of the  <see cref="View.Text"/> property with <see cref="View.AutoSize"/> set to false.
/// </summary>
public class TextTests {
	readonly ITestOutputHelper _output;

	public TextTests (ITestOutputHelper output) => _output = output;

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
		Assert.Equal (new Rect (0, 0, 3, 1),      view.Frame);
		Assert.Equal (new Size (3, 1),            view.TextFormatter.Size);
		Assert.Equal (new List<string> { "Vie" }, view.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4),     win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4),     Application.Top.Frame);
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

		Assert.Equal (new Rect (0, 0, 0, 1),             view.Frame);
		Assert.Equal (new Size (0, 1),                   view.TextFormatter.Size);
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
		Assert.Equal (new Size (3, 1),       view.TextFormatter.Size);
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
		Assert.Equal (new Size (0, 1),       view.TextFormatter.Size);
		var exception = Record.Exception (() => Assert.Equal (new List<string> { string.Empty }, view.TextFormatter.Lines));
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
		Assert.Equal (new Rect (0, 0, 3, 1),      label.Frame);
		Assert.Equal (new Size (3, 1),            label.TextFormatter.Size);
		Assert.Equal (new List<string> { "Lab" }, label.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4),     win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4),     Application.Top.Frame);
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
		Assert.Equal (new Rect (0, 0, 0, 1),             label.Frame);
		Assert.Equal (new Size (0, 1),                   label.TextFormatter.Size);
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
		Assert.Equal (new Size (3, 1),       label.TextFormatter.Size);
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
		Assert.Equal (new Size (0, 1),       label.TextFormatter.Size);
		var exception = Record.Exception (() => Assert.Equal (new List<string> { string.Empty }, label.TextFormatter.Lines));
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
		Assert.Equal (new Size (1, 3),       view.TextFormatter.Size);
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
		Assert.Equal (new Size (1, 0),       view.TextFormatter.Size);
		var exception = Record.Exception (() => Assert.Equal (new List<string> { string.Empty }, view.TextFormatter.Lines));
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
		Assert.Equal (new Size (2, 3),       view.TextFormatter.Size);
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
		Assert.Equal (new Size (2, 0),       view.TextFormatter.Size);
		var exception = Record.Exception (() => Assert.Equal (new List<string> { string.Empty }, view.TextFormatter.Lines));
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
		var view2 = new View ("");
		var view3 = new View { Text = "" };

		Assert.False (view1.AutoSize);
		Assert.False (view2.AutoSize);
		Assert.False (view3.AutoSize);
		view1.Dispose ();
		view2.Dispose ();
		view3.Dispose ();
	}

	[Fact]
	public void AutoSize_False_If_Text_Is_Not_Empty ()
	{
		var view1 = new View ();
		view1.Text = "Hello World";
		var view2 = new View ("Hello World");
		var view3 = new View { Text = "Hello World" };

		Assert.False (view1.AutoSize);
		Assert.False (view2.AutoSize);
		Assert.False (view3.AutoSize);
		view1.Dispose ();
		view2.Dispose ();
		view3.Dispose ();
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
		var win = new Window (new Rect (0, 0, 30, 80));
		var label = new Label { AutoSize = false, Width = Dim.Fill (), Height = Dim.Fill () };
		win.Add (label);
		Application.Top.Add (win);

		// Text is empty but height=1 by default, see Label view
		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,0,1)", label.Bounds.ToString ());

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
	public void AutoSize_False_SetWidthHeight_With_Dim_Fill_And_Dim_Absolute_After_IsAdded_And_IsInitialized ()
	{
		var win = new Window (new Rect (0, 0, 30, 80));
		var label = new Label { Width = Dim.Fill () };
		win.Add (label);
		Application.Top.Add (win);

		Assert.True (label.IsAdded);

		// Text is empty but height=1 by default, see Label view
		Assert.True (label.AutoSize);
		// BUGBUG: LayoutSubviews has not been called, so this test is not really valid (pos/dim are indeterminate, not 0)
		// Not really a bug because View call OnResizeNeeded method on the SetInitialProperties method
		Assert.Equal ("(0,0,0,1)", label.Bounds.ToString ());

		label.Text = "First line\nSecond line";
		Application.Top.LayoutSubviews ();

		Assert.True (label.AutoSize);
		// BUGBUG: This test is bogus: label has not been initialized. pos/dim is indeterminate!
		Assert.Equal ("(0,0,28,2)", label.Bounds.ToString ());
		Assert.False (label.IsInitialized);

		var rs = Application.Begin (Application.Top);

		Assert.True (label.AutoSize);
		Assert.Equal ("(0,0,28,2)", label.Bounds.ToString ());
		Assert.True (label.IsInitialized);

		label.AutoSize = false;
		Application.Refresh ();

		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,28,1)", label.Bounds.ToString ());
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_SetWidthHeight_With_Dim_Fill_And_Dim_Absolute_With_Initialization ()
	{
		var win = new Window (new Rect (0, 0, 30, 80));
		var label = new Label { Width = Dim.Fill () };
		win.Add (label);
		Application.Top.Add (win);

		// Text is empty but height=1 by default, see Label view
		Assert.True (label.AutoSize);
		Assert.Equal ("(0,0,0,1)", label.Bounds.ToString ());

		var rs = Application.Begin (Application.Top);

		Assert.True (label.AutoSize);
		// Here the AutoSize ensuring the right size with width 28 (Dim.Fill)
		// and height 0 because wasn't set and the text is empty
		// BUGBUG: Because of #2450, this test is bogus: pos/dim is indeterminate!
		//Assert.Equal ("(0,0,28,0)", label.Bounds.ToString ());

		label.Text = "First line\nSecond line";
		Application.Refresh ();

		// Here the AutoSize ensuring the right size with width 28 (Dim.Fill)
		// and height 2 because wasn't set and the text has 2 lines
		Assert.True (label.AutoSize);
		Assert.Equal ("(0,0,28,2)", label.Bounds.ToString ());

		label.AutoSize = false;
		Application.Refresh ();

		// Here the SetMinWidthHeight ensuring the minimum height
		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,28,1)", label.Bounds.ToString ());

		label.Text = "First changed line\nSecond changed line\nNew line";
		Application.Refresh ();

		// Here the AutoSize is false and the width 28 (Dim.Fill) and
		// height 1 because wasn't set and SetMinWidthHeight ensuring the minimum height
		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,28,1)", label.Bounds.ToString ());

		label.AutoSize = true;
		Application.Refresh ();

		// Here the AutoSize ensuring the right size with width 28 (Dim.Fill)
		// and height 3 because wasn't set and the text has 3 lines
		Assert.True (label.AutoSize);
		// BUGBUG: v2 - AutoSize is broken - temporarily disabling test See #2432
		//Assert.Equal ("(0,0,28,3)", label.Bounds.ToString ());
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
		Assert.Equal ("Absolute(10)",         view1.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view1.Height.ToString ());
		Assert.False (view2.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view2.Frame);
		Assert.Equal ("Absolute(10)",         view2.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view2.Height.ToString ());
		Assert.False (view3.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view3.Frame);
		Assert.Equal ("Absolute(10)",         view3.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view3.Height.ToString ());
		Assert.False (view4.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view4.Frame);
		Assert.Equal ("Absolute(10)",         view4.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view4.Height.ToString ());
		Assert.False (view5.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view5.Frame);
		Assert.Equal ("Absolute(10)",         view5.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view5.Height.ToString ());
		Assert.False (view6.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view6.Frame);
		Assert.Equal ("Absolute(10)",         view6.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view6.Height.ToString ());

		var rs = Application.Begin (Application.Top);

		Assert.True (view1.IsInitialized);
		Assert.True (view2.IsInitialized);
		Assert.True (view3.IsInitialized);
		Assert.True (view4.IsInitialized);
		Assert.True (view5.IsInitialized);
		Assert.False (view1.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view1.Frame);
		Assert.Equal ("Absolute(10)",         view1.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view1.Height.ToString ());
		Assert.False (view2.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view2.Frame);
		Assert.Equal ("Absolute(10)",         view2.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view2.Height.ToString ());
		Assert.False (view3.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view3.Frame);
		Assert.Equal ("Absolute(10)",         view3.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view3.Height.ToString ());
		Assert.False (view4.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view4.Frame);
		Assert.Equal ("Absolute(10)",         view4.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view4.Height.ToString ());
		Assert.False (view5.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view5.Frame);
		Assert.Equal ("Absolute(10)",         view5.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view5.Height.ToString ());
		Assert.False (view6.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 5), view6.Frame);
		Assert.Equal ("Absolute(10)",         view6.Width.ToString ());
		Assert.Equal ("Absolute(5)",          view6.Height.ToString ());
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_TextDirection_Toggle ()
	{
		var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
		// View is AutoSize == true
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
		Assert.Equal (Rect.Empty,                        view.Frame);
		Assert.Equal ("Absolute(0)",                     view.X.ToString ());
		Assert.Equal ("Absolute(0)",                     view.Y.ToString ());
		Assert.Equal ("Absolute(0)",                     view.Width.ToString ());
		Assert.Equal ("Absolute(0)",                     view.Height.ToString ());
		var expected = @"
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
		Assert.Equal ("Absolute(0)",          view.X.ToString ());
		Assert.Equal ("Absolute(0)",          view.Y.ToString ());
		Assert.Equal ("Absolute(11)",         view.Width.ToString ());
		Assert.Equal ("Absolute(1)",          view.Height.ToString ());
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
		Assert.Equal ("Absolute(0)",          view.X.ToString ());
		Assert.Equal ("Absolute(0)",          view.Y.ToString ());
		Assert.Equal ("Absolute(11)",         view.Width.ToString ());
		Assert.Equal ("Absolute(1)",          view.Height.ToString ());
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
		Assert.Equal ("Absolute(0)",           view.X.ToString ());
		Assert.Equal ("Absolute(0)",           view.Y.ToString ());
		Assert.Equal ("Absolute(11)",          view.Width.ToString ());
		Assert.Equal ("Absolute(1)",           view.Height.ToString ());
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
		Assert.Equal ("Absolute(0)",          view.X.ToString ());
		Assert.Equal ("Absolute(0)",          view.Y.ToString ());
		Assert.Equal ("Absolute(11)",         view.Width.ToString ());
		Assert.Equal ("Absolute(1)",          view.Height.ToString ());
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
		Assert.Equal ("Absolute(0)",          view.X.ToString ());
		Assert.Equal ("Absolute(0)",          view.Y.ToString ());
		Assert.Equal ("Absolute(11)",         view.Width.ToString ());
		Assert.Equal ("Absolute(1)",          view.Height.ToString ());
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
		Assert.Equal ("Absolute(0)",          view.X.ToString ());
		Assert.Equal ("Absolute(0)",          view.Y.ToString ());
		Assert.Equal ("Absolute(1)",          view.Width.ToString ());
		Assert.Equal ("Absolute(11)",         view.Height.ToString ());
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
		Assert.Equal ("Absolute(0)",          view.X.ToString ());
		Assert.Equal ("Absolute(0)",          view.Y.ToString ());
		Assert.Equal ("Absolute(1)",          view.Width.ToString ());
		Assert.Equal ("Absolute(12)",         view.Height.ToString ());
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
}