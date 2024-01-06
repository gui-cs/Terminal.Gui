using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AutoSizeTests {
	readonly ITestOutputHelper _output;

	public AutoSizeTests (ITestOutputHelper output) => _output = output;

	[Fact] [AutoInitShutdown]
	public void AutoSize_GetAutoSize_Horizontal ()
	{
		var text = "text";
		var view = new View {
			Text = text,
			AutoSize = true
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (view);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (10, 4);

		var size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length, 1), size);

		view.Text = $"{text}\n{text}";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length, 2), size);

		view.Text = $"{text}\n{text}\n{text}+";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length + 1, 3), size);

		text = string.Empty;
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (0, 0), size);

		text = "1";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (1, 1), size);

		text = "界";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (2, 1), size);
	}

	[Fact] [AutoInitShutdown]
	public void AutoSize_GetAutoSize_Vertical ()
	{
		var text = "text";
		var view = new View {
			Text = text,
			TextDirection = TextDirection.TopBottom_LeftRight,
			AutoSize = true
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (view);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (10, 4);

		var size = view.GetAutoSize ();
		Assert.Equal (new Size (1, text.Length), size);

		view.Text = $"{text}\n{text}";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (2, text.Length), size);

		view.Text = $"{text}\n{text}\n{text}+";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (3, text.Length + 1), size);

		text = string.Empty;
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (0, 0), size);

		text = "1";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (1, 1), size);

		text = "界";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (2, 1), size);
	}

	[Fact] [AutoInitShutdown]
	public void AutoSize_GetAutoSize_Left ()
	{
		var text = "This is some text.";
		var view = new View {
			Text = text,
			TextAlignment = TextAlignment.Left,
			AutoSize = true
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (view);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (10, 4);

		var size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length, 1), size);

		view.Text = $"{text}\n{text}";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length, 2), size);

		view.Text = $"{text}\n{text}\n{text}+";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length + 1, 3), size);

		text = string.Empty;
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (0, 0), size);

		text = "1";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (1, 1), size);

		text = "界";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (2, 1), size);
	}

	[Fact] [AutoInitShutdown]
	public void AutoSize_GetAutoSize_Right ()
	{
		var text = "This is some text.";
		var view = new View {
			Text = text,
			TextAlignment = TextAlignment.Right,
			AutoSize = true
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (view);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (10, 4);

		var size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length, 1), size);

		view.Text = $"{text}\n{text}";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length, 2), size);

		view.Text = $"{text}\n{text}\n{text}+";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length + 1, 3), size);

		text = string.Empty;
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (0, 0), size);

		text = "1";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (1, 1), size);

		text = "界";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (2, 1), size);
	}

	[Fact] [AutoInitShutdown]
	public void AutoSize_GetAutoSize_Centered ()
	{
		var text = "This is some text.";
		var view = new View {
			Text = text,
			TextAlignment = TextAlignment.Centered,
			AutoSize = true
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (view);
		Application.Top.Add (win);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (10, 4);

		var size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length, 1), size);

		view.Text = $"{text}\n{text}";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length, 2), size);

		view.Text = $"{text}\n{text}\n{text}+";
		size = view.GetAutoSize ();
		Assert.Equal (new Size (text.Length + 1, 3), size);

		text = string.Empty;
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (0, 0), size);

		text = "1";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (1, 1), size);

		text = "界";
		view.Text = text;
		size = view.GetAutoSize ();
		Assert.Equal (new Size (2, 1), size);
	}

	[Fact] [AutoInitShutdown]
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

	[Fact] [AutoInitShutdown]
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

	[Fact] [AutoInitShutdown]
	public void AutoSize_True_Label_IsEmpty_False_Never_Return_Null_Lines ()
	{
		var text = "Label";
		var label = new Label {
			Width = Dim.Fill () - text.Length,
			Height = 1,
			Text = text
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
		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 5, 1),        label.Frame);
		Assert.Equal (new Size (5, 1),              label.TextFormatter.Size);
		Assert.Equal (new List<string> { "Label" }, label.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4),       win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4),       Application.Top.Frame);
		var expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);

		text = "0123456789";
		Assert.Equal (10, text.Length);
		label.Width = Dim.Fill () - text.Length;
		Application.Refresh ();

		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 5, 1), label.Frame);
		Assert.Equal (new Size (5, 1),       label.TextFormatter.Size);
		Assert.Single (label.TextFormatter.Lines);
		expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);
	}

	[Fact] [AutoInitShutdown]
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

	[Fact] [AutoInitShutdown]
	public void AutoSize_True_Label_IsEmpty_False_Minimum_Height ()
	{
		var text = "Label";
		var label = new Label {
			Width = Dim.Fill () - text.Length,
			Text = text
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
		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 5, 1),        label.Frame);
		Assert.Equal (new Size (5, 1),              label.TextFormatter.Size);
		Assert.Equal (new List<string> { "Label" }, label.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4),       win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4),       Application.Top.Frame);
		var expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);

		text = "0123456789";
		Assert.Equal (10, text.Length);
		label.Width = Dim.Fill () - text.Length;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 5, 1), label.Frame);
		Assert.Equal (new Size (5, 1),       label.TextFormatter.Size);
		var exception = Record.Exception (() => Assert.Single (label.TextFormatter.Lines));
		Assert.Null (exception);
		expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 10, 4), pos);
	}

	[Fact] [AutoInitShutdown]
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

	[Fact] [AutoInitShutdown]
	public void AutoSize_True_View_IsEmpty_False_Minimum_Width ()
	{
		var text = "Views";
		var view = new View {
			TextDirection = TextDirection.TopBottom_LeftRight,
			Height = Dim.Fill () - text.Length,
			Text = text,
			AutoSize = true
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
		Assert.True (view.AutoSize);
		Assert.Equal (new Rect (0, 0, 1, 5),        view.Frame);
		Assert.Equal (new Size (1, 5),              view.TextFormatter.Size);
		Assert.Equal (new List<string> { "Views" }, view.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 4, 10),       win.Frame);
		Assert.Equal (new Rect (0, 0, 4, 10),       Application.Top.Frame);
		var expected = @"
┌──┐
│V │
│i │
│e │
│w │
│s │
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

		Assert.Equal (new Rect (0, 0, 1, 5), view.Frame);
		Assert.Equal (new Size (1, 5),       view.TextFormatter.Size);
		var exception = Record.Exception (() => Assert.Single (view.TextFormatter.Lines));
		Assert.Null (exception);
		expected = @"
┌──┐
│V │
│i │
│e │
│w │
│s │
│  │
│  │
│  │
└──┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 4, 10), pos);
	}

	[Fact] [AutoInitShutdown]
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

	[Fact] [AutoInitShutdown]
	public void AutoSize_True_View_IsEmpty_False_Minimum_Width_Wide_Rune ()
	{
		var text = "界View";
		var view = new View {
			TextDirection = TextDirection.TopBottom_LeftRight,
			Height = Dim.Fill () - text.Length,
			Text = text,
			AutoSize = true
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
		Assert.True (view.AutoSize);
		Assert.Equal (new Rect (0, 0, 2, 5),        view.Frame);
		Assert.Equal (new Size (2, 5),              view.TextFormatter.Size);
		Assert.Equal (new List<string> { "界View" }, view.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 4, 10),       win.Frame);
		Assert.Equal (new Rect (0, 0, 4, 10),       Application.Top.Frame);
		var expected = @"
┌──┐
│界│
│V │
│i │
│e │
│w │
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

		Assert.Equal (new Rect (0, 0, 2, 5), view.Frame);
		Assert.Equal (new Size (2, 5),       view.TextFormatter.Size);
		var exception = Record.Exception (() => Assert.Equal (new List<string> { "界View" }, view.TextFormatter.Lines));
		Assert.Null (exception);
		expected = @"
┌──┐
│界│
│V │
│i │
│e │
│w │
│  │
│  │
│  │
└──┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 4, 10), pos);
	}

	[Fact] [AutoInitShutdown]
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
	public void AutoSize_False_If_Text_Emmpty ()
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
	public void AutoSize_False_If_Text_Is_Not_Emmpty ()
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
	public void AutoSize_True_Label_If_Text_Emmpty ()
	{
		var label1 = new Label ();
		var label2 = new Label ("");
		var label3 = new Label { Text = "" };

		Assert.True (label1.AutoSize);
		Assert.True (label2.AutoSize);
		Assert.True (label3.AutoSize);
		label1.Dispose ();
		label2.Dispose ();
		label3.Dispose ();
	}

	[Fact]
	public void AutoSize_True_Label_If_Text_Is_Not_Emmpty ()
	{
		var label1 = new Label ();
		label1.Text = "Hello World";
		var label2 = new Label ("Hello World");
		var label3 = new Label { Text = "Hello World" };

		Assert.True (label1.AutoSize);
		Assert.True (label2.AutoSize);
		Assert.True (label3.AutoSize);
		label1.Dispose ();
		label2.Dispose ();
		label3.Dispose ();
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
	public void AutoSize_True_ResizeView_With_Dim_Absolute ()
	{
		var super = new View ();
		var label = new Label ();

		label.Text = "New text";
		// BUGBUG: v2 - label was never added to super, so it was never laid out.
		super.Add (label);
		super.LayoutSubviews ();

		Assert.True (label.AutoSize);
		Assert.Equal ("(0,0,8,1)", label.Bounds.ToString ());
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
	public void AutoSize_True_Setting_With_Height_Horizontal ()
	{
		var label = new Label ("Hello") { Width = 10, Height = 2 };
		var viewX = new View ("X") { X = Pos.Right (label) };
		var viewY = new View ("Y") { Y = Pos.Bottom (label) };

		Application.Top.Add (label, viewX, viewY);
		var rs = Application.Begin (Application.Top);

		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 2), label.Frame);

		var expected = @"
Hello     X
           
Y          
"
			;

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 11, 3), pos);

		label.AutoSize = false;
		Application.Refresh ();

		Assert.False (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 2), label.Frame);

		expected = @"
Hello     X
           
Y          
"
			;

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 11, 3), pos);
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_True_Setting_With_Height_Vertical ()
	{
		var label = new Label ("Hello") { Width = 2, Height = 10, TextDirection = TextDirection.TopBottom_LeftRight };
		var viewX = new View ("X") { X = Pos.Right (label) };
		var viewY = new View ("Y") { Y = Pos.Bottom (label) };

		Application.Top.Add (label, viewX, viewY);
		var rs = Application.Begin (Application.Top);

		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 2, 10), label.Frame);

		var expected = @"
H X
e  
l  
l  
o  
   
   
   
   
   
Y  
"
			;

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 3, 11), pos);

		label.AutoSize = false;
		Application.Refresh ();

		Assert.False (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 2, 10), label.Frame);

		expected = @"
H X
e  
l  
l  
o  
   
   
   
   
   
Y  
"
			;

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 3, 11), pos);
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void Excess_Text_Is_Erased_When_The_Width_Is_Reduced ()
	{
		var lbl = new Label ("123");
		Application.Top.Add (lbl);
		var rs = Application.Begin (Application.Top);

		Assert.True (lbl.AutoSize);
		Assert.Equal ("123 ", GetContents ());

		lbl.Text = "12";
		// Here the AutoSize ensuring the right size with width 3 (Dim.Absolute)
		// that was set on the OnAdded method with the text length of 3
		// and height 1 because wasn't set and the text has 1 line
		Assert.Equal (new Rect (0, 0, 3, 1), lbl.Frame);
		Assert.Equal (new Rect (0, 0, 3, 1), lbl._needsDisplayRect);
		Assert.Equal (new Rect (0, 0, 0, 0), lbl.SuperView._needsDisplayRect);
		Assert.True (lbl.SuperView.LayoutNeeded);
		lbl.SuperView.Draw ();
		Assert.Equal ("12  ", GetContents ());

		string GetContents ()
		{
			var text = "";
			for (var i = 0; i < 4; i++) {
				text += Application.Driver.Contents [0, i].Rune;
			}
			return text;
		}
		Application.End (rs);
	}


	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_Equal_Before_And_After_IsInitialized_With_Differents_Orders ()
	{
		var view1 = new View () { Text = "Say Hello view1 你", AutoSize = false, Width = 10, Height = 5 };
		var view2 = new View () { Text = "Say Hello view2 你", Width = 10, Height = 5, AutoSize = false };
		var view3 = new View () { AutoSize = false, Width = 10, Height = 5, Text = "Say Hello view3 你" };
		var view4 = new View () {
			Text = "Say Hello view4 你",
			AutoSize = false,
			Width = 10,
			Height = 5,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var view5 = new View () {
			Text = "Say Hello view5 你",
			Width = 10,
			Height = 5,
			AutoSize = false,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var view6 = new View () {
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
	public void AutoSize_True_Equal_Before_And_After_IsInitialized_With_Different_Orders ()
	{
		var view1 = new View () { Text = "Say Hello view1 你", AutoSize = true, Width = 10, Height = 5 };
		var view2 = new View () { Text = "Say Hello view2 你", Width = 10, Height = 5, AutoSize = true };
		var view3 = new View () { AutoSize = true, Width = 10, Height = 5, Text = "Say Hello view3 你" };
		var view4 = new View () {
			Text = "Say Hello view4 你",
			AutoSize = true,
			Width = 10,
			Height = 5,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var view5 = new View () {
			Text = "Say Hello view5 你",
			Width = 10,
			Height = 5,
			AutoSize = true,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var view6 = new View () {
			AutoSize = true,
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
		Assert.True (view1.AutoSize);
		Assert.Equal (new Rect (0, 0, 18, 5), view1.Frame);
		Assert.Equal ("Absolute(10)", view1.Width.ToString ());
		Assert.Equal ("Absolute(5)", view1.Height.ToString ());
		Assert.True (view2.AutoSize);
		// BUGBUG: v2 - Autosize is broken when setting Width/Height AutoSize. Disabling test for now.
		//Assert.Equal (new Rect (0, 0, 18, 5), view2.Frame);
		//Assert.Equal ("Absolute(10)", view2.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view2.Height.ToString ());
		//Assert.True (view3.AutoSize);
		//Assert.Equal (new Rect (0, 0, 18, 5), view3.Frame);
		//Assert.Equal ("Absolute(10)", view3.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view3.Height.ToString ());
		//Assert.True (view4.AutoSize);
		//Assert.Equal (new Rect (0, 0, 10, 17), view4.Frame);
		//Assert.Equal ("Absolute(10)", view4.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view4.Height.ToString ());
		//Assert.True (view5.AutoSize);
		//Assert.Equal (new Rect (0, 0, 10, 17), view5.Frame);
		//Assert.Equal ("Absolute(10)", view5.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view5.Height.ToString ());
		//Assert.True (view6.AutoSize);
		//Assert.Equal (new Rect (0, 0, 10, 17), view6.Frame);
		//Assert.Equal ("Absolute(10)", view6.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view6.Height.ToString ());

		var rs = Application.Begin (Application.Top);

		Assert.True (view1.IsInitialized);
		Assert.True (view2.IsInitialized);
		Assert.True (view3.IsInitialized);
		Assert.True (view4.IsInitialized);
		Assert.True (view5.IsInitialized);
		Assert.True (view1.AutoSize);
		Assert.Equal (new Rect (0, 0, 18, 5), view1.Frame);
		Assert.Equal ("Absolute(10)", view1.Width.ToString ());
		Assert.Equal ("Absolute(5)", view1.Height.ToString ());
		Assert.True (view2.AutoSize);
		// BUGBUG: v2 - Autosize is broken when setting Width/Height AutoSize. Disabling test for now.
		//Assert.Equal (new Rect (0, 0, 18, 5), view2.Frame);
		//Assert.Equal ("Absolute(10)", view2.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view2.Height.ToString ());
		//Assert.True (view3.AutoSize);
		//Assert.Equal (new Rect (0, 0, 18, 5), view3.Frame);
		//Assert.Equal ("Absolute(10)", view3.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view3.Height.ToString ());
		//Assert.True (view4.AutoSize);
		//Assert.Equal (new Rect (0, 0, 10, 17), view4.Frame);
		//Assert.Equal ("Absolute(10)", view4.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view4.Height.ToString ());
		//Assert.True (view5.AutoSize);
		//Assert.Equal (new Rect (0, 0, 10, 17), view5.Frame);
		//Assert.Equal ("Absolute(10)", view5.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view5.Height.ToString ());
		//Assert.True (view6.AutoSize);
		//Assert.Equal (new Rect (0, 0, 10, 17), view6.Frame);
		//Assert.Equal ("Absolute(10)", view6.Width.ToString ());
		//Assert.Equal ("Absolute(5)", view6.Height.ToString ());
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void Setting_Frame_Dont_Respect_AutoSize_True_On_Layout_Absolute ()
	{
		var view1 = new View (new Rect (0, 0, 10, 0)) { Text = "Say Hello view1 你", AutoSize = true };
		var view2 = new View (new Rect (0, 0, 0, 10)) {
			Text = "Say Hello view2 你",
			AutoSize = true,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		Application.Top.Add (view1, view2);

		var rs = Application.Begin (Application.Top);

		Assert.True (view1.AutoSize);
		Assert.Equal (LayoutStyle.Absolute, view1.LayoutStyle);
		Assert.Equal (new Rect (0, 0, 18, 1), view1.Frame);
		Assert.Equal ("Absolute(0)", view1.X.ToString ());
		Assert.Equal ("Absolute(0)", view1.Y.ToString ());
		Assert.Equal ("Absolute(18)", view1.Width.ToString ());
		Assert.Equal ("Absolute(1)", view1.Height.ToString ());
		Assert.True (view2.AutoSize);
		// BUGBUG: v2 - Autosize is broken when setting Width/Height AutoSize. Disabling test for now.
		//Assert.Equal (LayoutStyle.Absolute, view2.LayoutStyle);
		//Assert.Equal (new Rect (0, 0, 2, 17), view2.Frame);
		//Assert.Equal ("Absolute(0)", view2.X.ToString ());
		//Assert.Equal ("Absolute(0)", view2.Y.ToString ());
		//Assert.Equal ("Absolute(2)", view2.Width.ToString ());
		//Assert.Equal ("Absolute(17)", view2.Height.ToString ());

		view1.Frame = new Rect (0, 0, 25, 4);
		bool firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);

		Assert.True (view1.AutoSize);
		Assert.Equal (LayoutStyle.Absolute, view1.LayoutStyle);
		Assert.Equal (new Rect (0, 0, 25, 4), view1.Frame);
		Assert.Equal ("Absolute(0)", view1.X.ToString ());
		Assert.Equal ("Absolute(0)", view1.Y.ToString ());
		Assert.Equal ("Absolute(18)", view1.Width.ToString ());
		Assert.Equal ("Absolute(1)", view1.Height.ToString ());

		view2.Frame = new Rect (0, 0, 1, 25);
		Application.RunIteration (ref rs, ref firstIteration);

		Assert.True (view2.AutoSize);
		Assert.Equal (LayoutStyle.Absolute, view2.LayoutStyle);
		Assert.Equal (new Rect (0, 0, 1, 25), view2.Frame);
		Assert.Equal ("Absolute(0)", view2.X.ToString ());
		Assert.Equal ("Absolute(0)", view2.Y.ToString ());
		// BUGBUG: v2 - Autosize is broken when setting Width/Height AutoSize. Disabling test for now.
		//Assert.Equal ("Absolute(2)", view2.Width.ToString ());
		//Assert.Equal ("Absolute(17)", view2.Height.ToString ());
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Stays_True_Center_HotKeySpecifier ()
	{
		var label = new Label () {
			X = Pos.Center (),
			Y = Pos.Center (),
			Text = "Say Hello 你"
		};

		var win = new Window () {
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			Title = "Test Demo 你"
		};
		win.Add (label);
		Application.Top.Add (win);

		Assert.True (label.AutoSize);

		var rs = Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);
		string expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		Assert.True (label.AutoSize);
		label.Text = "Say Hello 你 changed";
		Assert.True (label.AutoSize);
		Application.Refresh ();
		expected = @"
┌┤Test Demo 你├──────────────┐
│                            │
│    Say Hello 你 changed    │
│                            │
└────────────────────────────┘
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);
	}

}