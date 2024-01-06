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


	readonly string [] expecteds = new string [21] {
		@"
┌────────────────────┐
│View with long text │
│                    │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 0             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 1             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 2             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 3             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 4             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 5             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 6             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 7             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 8             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 9             │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 10            │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 11            │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 12            │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 13            │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 14            │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 15            │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 16            │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 17            │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 18            │
│Label 18            │
└────────────────────┘",
		@"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 18            │
│Label 19            │
│Label 19            │
└────────────────────┘"
	};


	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Dim_Add_Operator_With_Text ()
	{
		var top = Application.Top;

		var view = new View ("View with long text") { X = 0, Y = 0, Width = 20, Height = 1 };
		var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
		var count = 0;
		var listLabels = new List<Label> ();

		field.KeyDown += (s, k) => {
			if (k.KeyCode == KeyCode.Enter) {
				((FakeDriver)Application.Driver).SetBufferSize (22, count + 4);
				var pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], _output);
				Assert.Equal (new Rect (0, 0, 22, count + 4), pos);

				if (count < 20) {
					field.Text = $"Label {count}";
					// Label is AutoSize = true
					var label = new Label (field.Text) { X = 0, Y = view.Bounds.Height, Width = 10 };
					view.Add (label);
					Assert.Equal ($"Label {count}",         label.Text);
					Assert.Equal ($"Absolute({count + 1})", label.Y.ToString ());
					listLabels.Add (label);
					//if (count == 0) {
					//	Assert.Equal ($"Absolute({count})", view.Height.ToString ());
					//	view.Height += 2;
					//} else {
					Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
					view.Height += 1;
					//}
					count++;
				}
				Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
			}
		};

		Application.Iteration += (s, a) => {
			while (count < 21) {
				field.NewKeyDownEvent (new Key (KeyCode.Enter));
				if (count == 20) {
					field.NewKeyDownEvent (new Key (KeyCode.Enter));
					break;
				}
			}

			Application.RequestStop ();
		};

		var win = new Window ();
		win.Add (view);
		win.Add (field);

		top.Add (win);

		Application.Run (top);

		Assert.Equal (20,    count);
		Assert.Equal (count, listLabels.Count);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Dim_Subtract_Operator_With_Text ()
	{
		var top = Application.Top;

		// BUGBUG: v2 - If a View's height is zero, it should not be drawn.
		//// Although view height is zero the text it's draw due the SetMinWidthHeight method
		var view = new View ("View with long text") { X = 0, Y = 0, Width = 20, Height = 1 };
		var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
		var count = 20;
		// Label is AutoSize = true
		var listLabels = new List<Label> ();

		for (var i = 0; i < count; i++) {
			field.Text = $"Label {i}";
			// BUGBUG: v2 - view has not been initialied yet; view.Bounds is indeterminate
			var label = new Label (field.Text) { X = 0, Y = i + 1, Width = 10 };
			view.Add (label);
			Assert.Equal ($"Label {i}", label.Text);
			// BUGBUG: Bogus test; views have not been initialized yet
			//Assert.Equal ($"Absolute({i + 1})", label.Y.ToString ());
			listLabels.Add (label);

			//if (i == 0) {
			// BUGBUG: Bogus test; views have not been initialized yet
			//Assert.Equal ($"Absolute({i})", view.Height.ToString ());
			//view.Height += 2;
			// BUGBUG: Bogus test; views have not been initialized yet
			//Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
			//} else {
			// BUGBUG: Bogus test; views have not been initialized yet
			//Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
			view.Height += 1;
			// BUGBUG: Bogus test; views have not been initialized yet
			//Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
			//}
		}

		field.KeyDown += (s, k) => {
			if (k.KeyCode == KeyCode.Enter) {
				((FakeDriver)Application.Driver).SetBufferSize (22, count + 4);
				var pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], _output);
				Assert.Equal (new Rect (0, 0, 22, count + 4), pos);

				if (count > 0) {
					Assert.Equal ($"Label {count - 1}", listLabels [count - 1].Text);
					view.Remove (listLabels [count - 1]);
					listLabels [count - 1].Dispose ();
					listLabels.RemoveAt (count - 1);
					Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
					view.Height -= 1;
					count--;
					if (listLabels.Count > 0) {
						field.Text = listLabels [count - 1].Text;
					} else {
						field.Text = string.Empty;
					}
				}
				Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
			}
		};

		Application.Iteration += (s, a) => {
			while (count > -1) {
				field.NewKeyDownEvent (new Key (KeyCode.Enter));
				if (count == 0) {
					field.NewKeyDownEvent (new Key (KeyCode.Enter));
					break;
				}
			}

			Application.RequestStop ();
		};

		var win = new Window ();
		win.Add (view);
		win.Add (field);

		top.Add (win);

		Application.Run (top);

		Assert.Equal (0, count);
		Assert.Equal (count, listLabels.Count);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_AnchorEnd_Better_Than_Bottom_Equal_Inside_Window ()
	{
		var win = new Window ();

		var label = new Label ("This should be the last line.") {
			ColorScheme = Colors.Menu,
			Width = Dim.Fill (),
			X = 0, // keep unit test focused; don't use Center here
			Y = Pos.AnchorEnd (1)
		};

		win.Add (label);

		var top = Application.Top;
		top.Add (win);
		var rs = Application.Begin (top);
		((FakeDriver)Application.Driver).SetBufferSize (40, 10);

		Assert.True (label.AutoSize);
		Assert.Equal (29,                      label.Text.Length);
		Assert.Equal (new Rect (0, 0, 40, 10), top.Frame);
		Assert.Equal (new Rect (0, 0, 40, 10), win.Frame);
		Assert.Equal (new Rect (0, 7, 38, 1),  label.Frame);
		string expected = @"
┌──────────────────────────────────────┐
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│This should be the last line.         │
└──────────────────────────────────────┘
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);
	}


	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Bottom_Equal_Inside_Window ()
	{
		var win = new Window ();

		var label = new Label ("This should be the last line.") {
			ColorScheme = Colors.Menu,
			Width = Dim.Fill (),
			X = 0,
			Y = Pos.Bottom (win) - 3 // two lines top and bottom borders more one line above the bottom border
		};

		win.Add (label);

		var top = Application.Top;
		top.Add (win);
		var rs = Application.Begin (top);
		((FakeDriver)Application.Driver).SetBufferSize (40, 10);

		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 40, 10), top.Frame);
		Assert.Equal (new Rect (0, 0, 40, 10), win.Frame);
		Assert.Equal (new Rect (0, 7, 38, 1), label.Frame);
		string expected = @"
┌──────────────────────────────────────┐
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│This should be the last line.         │
└──────────────────────────────────────┘
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);
	}



	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Bottom_Equal_Inside_Window_With_MenuBar_And_StatusBar_On_Toplevel ()
	{
		var win = new Window ();

		var label = new Label ("This should be the last line.") {
			ColorScheme = Colors.Menu,
			Width = Dim.Fill (),
			X = 0,
			Y = Pos.Bottom (win) - 4 // two lines top and bottom borders more two lines above border
		};

		win.Add (label);

		var menu = new MenuBar (new MenuBarItem [] { new ("Menu", "", null) });
		var status = new StatusBar (new StatusItem [] { new (KeyCode.F1, "~F1~ Help", null) });
		var top = Application.Top;
		top.Add (win, menu, status);
		var rs = Application.Begin (top);

		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 80, 25), top.Frame);
		Assert.Equal (new Rect (0, 0, 80, 1), menu.Frame);
		Assert.Equal (new Rect (0, 24, 80, 1), status.Frame);
		Assert.Equal (new Rect (0, 1, 80, 23), win.Frame);
		Assert.Equal (new Rect (0, 20, 78, 1), label.Frame);
		string expected = @"
 Menu                                                                           
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│This should be the last line.                                                 │
└──────────────────────────────────────────────────────────────────────────────┘
 F1 Help                                                                        
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_AnchorEnd_Better_Than_Bottom_Equal_Inside_Window_With_MenuBar_And_StatusBar_On_Toplevel ()
	{
		var win = new Window ();

		var label = new Label ("This should be the last line.") {
			ColorScheme = Colors.Menu,
			Width = Dim.Fill (),
			X = 0,
			Y = Pos.AnchorEnd (1)
		};

		win.Add (label);

		var menu = new MenuBar (new MenuBarItem [] { new ("Menu", "", null) });
		var status = new StatusBar (new StatusItem [] { new (KeyCode.F1, "~F1~ Help", null) });
		var top = Application.Top;
		top.Add (win, menu, status);
		var rs = Application.Begin (top);

		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 80, 25), top.Frame);
		Assert.Equal (new Rect (0, 0, 80, 1), menu.Frame);
		Assert.Equal (new Rect (0, 24, 80, 1), status.Frame);
		Assert.Equal (new Rect (0, 1, 80, 23), win.Frame);
		Assert.Equal (new Rect (0, 20, 78, 1), label.Frame);
		string expected = @"
 Menu                                                                           
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│This should be the last line.                                                 │
└──────────────────────────────────────────────────────────────────────────────┘
 F1 Help                                                                        
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);
	}

	[Fact]
	public void AutoSize_Pos_Validation_Do_Not_Throws_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
	{
		Application.Init (new FakeDriver ());

		var t = Application.Top;

		var w = new Window () {
			X = Pos.Left (t) + 2,
			Y = Pos.At (2)
		};

		// View is AutoSize = true
		var v = new View () {
			X = Pos.Center (),
			Y = Pos.Percent (10)
		};

		w.Add (v);
		t.Add (w);

		t.Ready += (s, e) => {
			v.LayoutStyle = LayoutStyle.Absolute;
			Assert.Equal (2, v.X = 2);
			Assert.Equal (2, v.Y = 2);
		};

		Application.Iteration += (s, a) => Application.RequestStop ();

		Application.Run ();
		Application.Shutdown ();
	}
}