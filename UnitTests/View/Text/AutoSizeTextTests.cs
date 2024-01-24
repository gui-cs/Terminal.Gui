using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Tests of the  <see cref="View.AutoSize"/> property which auto sizes Views based on <see cref="Text"/>.
/// </summary>
public class AutoSizeTextTests {
	readonly ITestOutputHelper _output;

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

	public AutoSizeTextTests (ITestOutputHelper output) => _output = output;

	[Fact]
	[AutoInitShutdown]
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

	[Fact]
	[AutoInitShutdown]
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

	[Fact]
	[AutoInitShutdown]
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

	[Fact]
	[AutoInitShutdown]
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

	[Fact]
	[AutoInitShutdown]
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

	[Fact]
	[AutoInitShutdown]
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
		Assert.Equal (new Rect (0, 0, 5, 1), label.Frame);
		Assert.Equal (new Size (5, 1), label.TextFormatter.Size);
		Assert.Equal (new List<string> { "Label" }, label.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
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
		Assert.Equal (new Size (5, 1), label.TextFormatter.Size);
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

	[Fact]
	[AutoInitShutdown]
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
		Assert.Equal (new Rect (0, 0, 5, 1), label.Frame);
		Assert.Equal (new Size (5, 1), label.TextFormatter.Size);
		Assert.Equal (new List<string> { "Label" }, label.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 10, 4), win.Frame);
		Assert.Equal (new Rect (0, 0, 10, 4), Application.Top.Frame);
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
		Assert.Equal (new Size (5, 1), label.TextFormatter.Size);
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

	[Fact]
	[AutoInitShutdown]
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
		Assert.Equal (new Rect (0, 0, 1, 5), view.Frame);
		Assert.Equal (new Size (1, 5), view.TextFormatter.Size);
		Assert.Equal (new List<string> { "Views" }, view.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 4, 10), win.Frame);
		Assert.Equal (new Rect (0, 0, 4, 10), Application.Top.Frame);
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
		Assert.Equal (new Size (1, 5), view.TextFormatter.Size);
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

	[Fact]
	[AutoInitShutdown]
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
		Assert.Equal (new Rect (0, 0, 2, 5), view.Frame);
		Assert.Equal (new Size (2, 5), view.TextFormatter.Size);
		Assert.Equal (new List<string> { "界View" }, view.TextFormatter.Lines);
		Assert.Equal (new Rect (0, 0, 4, 10), win.Frame);
		Assert.Equal (new Rect (0, 0, 4, 10), Application.Top.Frame);
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
		Assert.Equal (new Size (2, 5), view.TextFormatter.Size);
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
	public void AutoSize_True_Setting_With_Height_Horizontal ()
	{
		var label = new Label ("Hello") { Width = 10, Height = 2, ValidatePosDim = true };
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
		var label = new Label { Width = 2, Height = 10, TextDirection = TextDirection.TopBottom_LeftRight, ValidatePosDim = true };
		var viewX = new View ("X") { X = Pos.Right (label) };
		var viewY = new View ("Y") { Y = Pos.Bottom (label) };

		Application.Top.Add (label, viewX, viewY);
		var rs = Application.Begin (Application.Top);

		Assert.True (label.AutoSize);
		label.Text = "Hello";
		Application.Refresh ();

		// #3127: Label.Text is "Hello" - It's Vertical. So the width should be 2 (honoring Width = 2)
		// and the height is should be 10 (because 10 is greater than length of Hello).
		Assert.Equal (new Rect (0, 0, 2, 10), label.Frame);

		var expected = @"
H X
e  
l  
l  
o  
   
   
   
   
   
Y  
";

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
	public void AutoSize_True_Equal_Before_And_After_IsInitialized_With_Different_Orders ()
	{
		var view1 = new View { Text = "Say Hello view1 你", AutoSize = true, Width = 10, Height = 5, ValidatePosDim = true };
		var view2 = new View { Text = "Say Hello view2 你", Width = 10, Height = 5, AutoSize = true, ValidatePosDim = true };
		var view3 = new View { AutoSize = true, Width = 10, Height = 5, Text = "Say Hello view3 你", ValidatePosDim = true };
		var view4 = new View {
			Text = "Say Hello view4 你",
			AutoSize = true,
			Width = 10,
			Height = 5,
			TextDirection = TextDirection.TopBottom_LeftRight,
			ValidatePosDim = true
		};
		var view5 = new View {
			Text = "Say Hello view5 你",
			Width = 10,
			Height = 5,
			AutoSize = true,
			TextDirection = TextDirection.TopBottom_LeftRight,
			ValidatePosDim = true
		};
		var view6 = new View {
			AutoSize = true,
			Width = 10,
			Height = 5,
			TextDirection = TextDirection.TopBottom_LeftRight,
			Text = "Say Hello view6 你",
			ValidatePosDim = true
		};
		Application.Top.Add (view1, view2, view3, view4, view5, view6);

		Assert.False (view1.IsInitialized);
		Assert.False (view2.IsInitialized);
		Assert.False (view3.IsInitialized);
		Assert.False (view4.IsInitialized);
		Assert.False (view5.IsInitialized);
		Assert.True (view1.AutoSize);
		Assert.Equal (new Rect (0, 0, 18, 5), view1.Frame);
		Assert.Equal ("Absolute(18)", view1.Width.ToString ());
		Assert.Equal ("Absolute(5)", view1.Height.ToString ());
		Assert.True (view2.AutoSize);
		Assert.Equal ("Say Hello view2 你".GetColumns (), view2.Width);
		Assert.Equal (18, view2.Width);
		Assert.Equal (new Rect (0, 0, 18, 5), view2.Frame);
		Assert.Equal ("Absolute(18)", view2.Width.ToString ());
		Assert.Equal ("Absolute(5)", view2.Height.ToString ());
		Assert.True (view3.AutoSize);
		Assert.Equal (new Rect (0, 0, 18, 5), view3.Frame);
		Assert.Equal ("Absolute(18)", view2.Width.ToString ());
		Assert.Equal ("Absolute(5)", view3.Height.ToString ());
		Assert.True (view4.AutoSize);

		Assert.Equal ("Say Hello view4 你".GetColumns (), view2.Width);
		Assert.Equal (18, view2.Width);

		Assert.Equal (new Rect (0, 0, 18, 17), view4.Frame);
		Assert.Equal ("Absolute(18)", view4.Width.ToString ());
		Assert.Equal ("Absolute(17)", view4.Height.ToString ());
		Assert.True (view5.AutoSize);
		Assert.Equal (new Rect (0, 0, 18, 17), view5.Frame);
		Assert.True (view6.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 17), view6.Frame);

		var rs = Application.Begin (Application.Top);

		Assert.True (view1.IsInitialized);
		Assert.True (view2.IsInitialized);
		Assert.True (view3.IsInitialized);
		Assert.True (view4.IsInitialized);
		Assert.True (view5.IsInitialized);
		Assert.True (view1.AutoSize);
		Assert.Equal (new Rect (0, 0, 18, 5), view1.Frame);
		Assert.Equal ("Absolute(18)", view1.Width.ToString ());
		Assert.Equal ("Absolute(5)", view1.Height.ToString ());
		Assert.True (view2.AutoSize);

		Assert.Equal (new Rect (0, 0, 18, 5), view2.Frame);
		Assert.Equal ("Absolute(18)", view2.Width.ToString ());
		Assert.Equal ("Absolute(5)", view2.Height.ToString ());
		Assert.True (view3.AutoSize);
		Assert.Equal (new Rect (0, 0, 18, 5), view3.Frame);
		Assert.Equal ("Absolute(18)", view5.Width.ToString ());
		Assert.Equal ("Absolute(5)", view3.Height.ToString ());
		Assert.True (view4.AutoSize);
		Assert.Equal (new Rect (0, 0, 18, 17), view4.Frame);
		Assert.Equal ("Absolute(18)", view5.Width.ToString ());
		Assert.Equal ("Absolute(17)", view4.Height.ToString ());
		Assert.True (view5.AutoSize);
		Assert.Equal (new Rect (0, 0, 18, 17), view5.Frame);
		Assert.Equal ("Absolute(18)", view5.Width.ToString ());
		Assert.Equal ("Absolute(17)", view5.Height.ToString ());
		Assert.True (view6.AutoSize);
		Assert.Equal (new Rect (0, 0, 10, 17), view6.Frame);
		Assert.Equal ("Absolute(10)", view6.Width.ToString ());
		Assert.Equal ("Absolute(17)", view6.Height.ToString ());
		Application.End (rs);
	}

	[Fact]
	public void SetRelativeLayout_Respects_AutoSize ()
	{
		var view = new View (new Rect (0, 0, 10, 0)) {
			AutoSize = true
		};
		view.Text = "01234567890123456789";

		Assert.True (view.AutoSize);
		Assert.Equal (LayoutStyle.Absolute, view.LayoutStyle);
		Assert.Equal (new Rect (0, 0, 20, 1), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(20)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());

		view.SetRelativeLayout (new Rect (0, 0, 25, 5));

		Assert.True (view.AutoSize);
		Assert.Equal (LayoutStyle.Absolute, view.LayoutStyle);
		Assert.Equal (new Rect (0, 0, 20, 1), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(20)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());
	}

	[Fact]
	[AutoInitShutdown]
	public void Setting_Frame_Dont_Respect_AutoSize_True_On_Layout_Absolute ()
	{
		var view1 = new View (new Rect (0, 0, 10, 0)) {
			Text = "Say Hello view1 你",
			AutoSize = true
		};
		var viewTopBottom_LeftRight = new View (new Rect (0, 0, 0, 10)) {
			Text = "Say Hello view2 你",
			AutoSize = true,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		Application.Top.Add (view1, viewTopBottom_LeftRight);

		var rs = Application.Begin (Application.Top);

		Assert.True (view1.AutoSize);
		Assert.Equal (LayoutStyle.Absolute, view1.LayoutStyle);
		Assert.Equal (new Rect (0, 0, 18, 1), view1.Frame);
		Assert.Equal ("Absolute(0)", view1.X.ToString ());
		Assert.Equal ("Absolute(0)", view1.Y.ToString ());
		Assert.Equal ("Absolute(18)", view1.Width.ToString ());
		Assert.Equal ("Absolute(1)", view1.Height.ToString ());

		Assert.True (viewTopBottom_LeftRight.AutoSize);
		Assert.Equal (LayoutStyle.Absolute, viewTopBottom_LeftRight.LayoutStyle);
		Assert.Equal (new Rect (0, 0, 18, 17), viewTopBottom_LeftRight.Frame);
		Assert.Equal ("Absolute(0)", viewTopBottom_LeftRight.X.ToString ());
		Assert.Equal ("Absolute(0)", viewTopBottom_LeftRight.Y.ToString ());
		Assert.Equal ("Absolute(18)", viewTopBottom_LeftRight.Width.ToString ());
		Assert.Equal ("Absolute(17)", viewTopBottom_LeftRight.Height.ToString ());

		view1.Frame = new Rect (0, 0, 25, 4);
		var firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);

		Assert.True (view1.AutoSize);
		Assert.Equal (LayoutStyle.Absolute, view1.LayoutStyle);
		Assert.Equal (new Rect (0, 0, 25, 4), view1.Frame);
		Assert.Equal ("Absolute(0)", view1.X.ToString ());
		Assert.Equal ("Absolute(0)", view1.Y.ToString ());
		Assert.Equal ("Absolute(25)", view1.Width.ToString ());
		Assert.Equal ("Absolute(4)", view1.Height.ToString ());

		viewTopBottom_LeftRight.Frame = new Rect (0, 0, 1, 25);
		Application.RunIteration (ref rs, ref firstIteration);

		Assert.True (viewTopBottom_LeftRight.AutoSize);
		Assert.Equal (LayoutStyle.Absolute, viewTopBottom_LeftRight.LayoutStyle);
		Assert.Equal (new Rect (0, 0, 2, 25), viewTopBottom_LeftRight.Frame);
		Assert.Equal ("Absolute(0)", viewTopBottom_LeftRight.X.ToString ());
		Assert.Equal ("Absolute(0)", viewTopBottom_LeftRight.Y.ToString ());
		Assert.Equal ("Absolute(2)", viewTopBottom_LeftRight.Width.ToString ());
		Assert.Equal ("Absolute(25)", viewTopBottom_LeftRight.Height.ToString ());
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Stays_True_Center_HotKeySpecifier ()
	{
		var label = new Label {
			X = Pos.Center (),
			Y = Pos.Center (),
			Text = "Say Hello 你"
		};

		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			Title = "Test Demo 你"
		};
		win.Add (label);
		Application.Top.Add (win);

		Assert.True (label.AutoSize);

		var rs = Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);
		var expected = @"
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


	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Dim_Add_Operator_With_Text ()
	{
		var top = Application.Top;

		var view = new View ("View with long text") { X = 0, Y = 0, Width = 20, Height = 1 };
		var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
		var count = 0;
		// Label is AutoSize == true
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
					Assert.Equal ($"Label {count}", label.Text);
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

		Assert.Equal (20, count);
		Assert.Equal (count, listLabels.Count);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Dim_Subtract_Operator_With_Text ()
	{
		var top = Application.Top;
		var view = new View ("View with long text") { X = 0, Y = 0, Width = 20, Height = 1 };
		var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
		var count = 20;
		// Label is AutoSize == true
		var listLabels = new List<Label> ();

		for (var i = 0; i < count; i++) {
			field.Text = $"Label {i}";
			var label = new Label (field.Text) { X = 0, Y = i + 1, Width = 10 };
			view.Add (label);
			Assert.Equal ($"Label {i}", label.Text);
			Assert.Equal ($"Absolute({i + 1})", label.Y.ToString ());
			listLabels.Add (label);

			if (i == 0) {
				Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
				view.Height += 1;
				Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
			} else {
				Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
				view.Height += 1;
				Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
			}
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

		// Label is AutoSize == true
		var label = new Label ("This should be the last line.") {
			ColorScheme = Colors.ColorSchemes ["Menu"],
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
		Assert.Equal (29, label.Text.Length);
		Assert.Equal (new Rect (0, 0, 40, 10), top.Frame);
		Assert.Equal (new Rect (0, 0, 40, 10), win.Frame);
		Assert.Equal (new Rect (0, 7, 38, 1), label.Frame);
		var expected = @"
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

		// Label is AutoSize == true
		var label = new Label ("This should be the last line.") {
			ColorScheme = Colors.ColorSchemes ["Menu"],
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
		var expected = @"
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

		// Label is AutoSize == true
		var label = new Label ("This should be the last line.") {
			ColorScheme = Colors.ColorSchemes ["Menu"],
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
		var expected = @"
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

		// Label is AutoSize == true
		var label = new Label ("This should be the last line.") {
			ColorScheme = Colors.ColorSchemes ["Menu"],
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
		var expected = @"
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
	public void AutoSize_True_TextDirection_Toggle ()
	{
		var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
		// View is AutoSize == true
		var view = new View ();
		win.Add (view);
		Application.Top.Add (win);

		var rs = Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (15, 15);

		Assert.Equal (new Rect (0, 0, 15, 15), win.Frame);
		Assert.Equal (new Rect (0, 0, 15, 15), win.Margin.Frame);
		Assert.Equal (new Rect (0, 0, 15, 15), win.Border.Frame);
		Assert.Equal (new Rect (1, 1, 13, 13), win.Padding.Frame);
		Assert.False (view.AutoSize);
		Assert.Equal (TextDirection.LeftRight_TopBottom, view.TextDirection);
		Assert.Equal (Rect.Empty, view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(0)", view.Width.ToString ());
		Assert.Equal ("Absolute(0)", view.Height.ToString ());
		var expected = @"
┌─────────────┐
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

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
┌─────────────┐
│Hello World  │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		view.AutoSize = true;
		view.Text = "Hello Worlds";
		Application.Refresh ();
		var len = "Hello Worlds".Length;
		Assert.Equal (12, len);
		Assert.Equal (new Rect (0, 0, len, 1), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(12)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());
		expected = @"
┌─────────────┐
│Hello Worlds │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		view.TextDirection = TextDirection.TopBottom_LeftRight;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 12, 12), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(12)", view.Width.ToString ());
		Assert.Equal ("Absolute(12)", view.Height.ToString ());
		expected = @"
┌─────────────┐
│H            │
│e            │
│l            │
│l            │
│o            │
│             │
│W            │
│o            │
│r            │
│l            │
│d            │
│s            │
│             │
└─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		view.AutoSize = false;
		view.Height = 1;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 12, 1), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(12)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());
		// TextDirection.TopBottom_LeftRight - Height of 1 and Width of 12 means 
		// that the text will be spread "vertically" across 1 line.
		// Hence no space.
		expected = @"
┌─────────────┐
│HelloWorlds  │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		view.PreserveTrailingSpaces = true;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 12, 1), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(12)", view.Width.ToString ());
		Assert.Equal ("Absolute(1)", view.Height.ToString ());
		expected = @"
┌─────────────┐
│Hello Worlds │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		view.PreserveTrailingSpaces = false;
		var f = view.Frame;
		view.Width = f.Height;
		view.Height = f.Width;
		view.TextDirection = TextDirection.TopBottom_LeftRight;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 1, 12), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(1)", view.Width.ToString ());
		Assert.Equal ("Absolute(12)", view.Height.ToString ());
		expected = @"
┌─────────────┐
│H            │
│e            │
│l            │
│l            │
│o            │
│             │
│W            │
│o            │
│r            │
│l            │
│d            │
│s            │
│             │
└─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		view.AutoSize = true;
		Application.Refresh ();

		Assert.Equal (new Rect (0, 0, 1, 12), view.Frame);
		Assert.Equal ("Absolute(0)", view.X.ToString ());
		Assert.Equal ("Absolute(0)", view.Y.ToString ());
		Assert.Equal ("Absolute(1)", view.Width.ToString ());
		Assert.Equal ("Absolute(12)", view.Height.ToString ());
		expected = @"
┌─────────────┐
│H            │
│e            │
│l            │
│l            │
│o            │
│             │
│W            │
│o            │
│r            │
│l            │
│d            │
│s            │
│             │
└─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_True_Width_Height_Stay_True_If_TextFormatter_Size_Fit ()
	{
		var text = "Fi_nish 終";
		var horizontalView = new View {
			Id = "horizontalView",
			AutoSize = true,
			HotKeySpecifier = (Rune)'_',
			Text = text
		};
		var verticalView = new View {
			Id = "verticalView",
			Y = 3,
			AutoSize = true,
			HotKeySpecifier = (Rune)'_',
			Text = text,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var win = new Window {
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
		Assert.Equal (new Size (text.GetColumns (), 1), horizontalView.TextFormatter.Size);
		Assert.Equal (new Size (2, 9), verticalView.TextFormatter.Size);
		Assert.Equal (new Rect (0, 0, 9, 1), horizontalView.Frame);
		Assert.Equal ("Absolute(0)", horizontalView.X.ToString ());
		Assert.Equal ("Absolute(0)", horizontalView.Y.ToString ());

		// BUGBUG - v2 - With v1 AutoSize = true Width/Height should always grow or keep initial value, 

		Assert.Equal ("Absolute(9)", horizontalView.Width.ToString ());
		Assert.Equal ("Absolute(1)", horizontalView.Height.ToString ());
		Assert.Equal (new Rect (0, 3, 9, 8), verticalView.Frame);
		Assert.Equal ("Absolute(0)", verticalView.X.ToString ());
		Assert.Equal ("Absolute(3)", verticalView.Y.ToString ());
		Assert.Equal ("Absolute(9)", verticalView.Width.ToString ());
		Assert.Equal ("Absolute(8)", verticalView.Height.ToString ());
		var expected = @"
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

		verticalView.Text = "最初_の行二行目";
		Application.Top.Draw ();
		Assert.True (horizontalView.AutoSize);
		Assert.True (verticalView.AutoSize);
		// height was initialized with 8 and can only grow or keep initial value
		Assert.Equal (new Rect (0, 3, 9, 8), verticalView.Frame);
		Assert.Equal ("Absolute(0)", verticalView.X.ToString ());
		Assert.Equal ("Absolute(3)", verticalView.Y.ToString ());
		Assert.Equal ("Absolute(9)", verticalView.Width.ToString ());
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


	[Fact]
	[AutoInitShutdown]
	public void AutoSize_False_SetWidthHeight_With_Dim_Fill_And_Dim_Absolute_After_IsAdded_And_IsInitialized ()
	{
		var win = new Window (new Rect (0, 0, 30, 50));
		var label = new Label { Width = Dim.Fill () };
		win.Add (label);
		Application.Top.Add (win);

		Assert.True (label.IsAdded);

		Assert.True (label.AutoSize);

		// #3127: Before: 
		//		 Text is empty but height=1 by default, see Label view
		//		 BUGBUG: LayoutSubviews has not been called, so this test is not really valid (pos/dim are indeterminate, not 0)
		//		 Not really a bug because View call OnResizeNeeded method on the SetInitialProperties method
		// #3127: After: Text is empty Width=Dim.Fill is honored. 
		//        LayoutSubViews has not been called, and OnResizeNeeded ends up using Application.Top.Bounds
		//        Which has a width of 80.
		Assert.Equal ("(0,0,80,1)", label.Bounds.ToString ());

		label.Text = "First line\nSecond line";
		Application.Top.LayoutSubviews ();

		Assert.True (label.AutoSize);
		Assert.Equal ("(0,0,28,2)", label.Bounds.ToString ());
		Assert.False (label.IsInitialized);

		var rs = Application.Begin (Application.Top);

		Assert.True (label.AutoSize);
		Assert.Equal ("(0,0,28,2)", label.Bounds.ToString ());
		Assert.True (label.IsInitialized);

		label.AutoSize = false;

		// Width should still be Dim.Fill
		Assert.Equal ("Fill(0)", label.Width.ToString ());

		// Height should be 2
		Assert.Equal ("Absolute(2)", label.Height.ToString ());
		Assert.Equal (2, label.Frame.Height);

		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,28,2)", label.Bounds.ToString ());
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

		// Text is empty but height=1 by default. 
		Assert.True (label.AutoSize);
		Assert.Equal ("(0,0,80,1)", label.Bounds.ToString ());

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
		// #3127: After: (0,0,28,2) because turning off AutoSize leaves
		// Height set to 2.
		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,28,2)", label.Bounds.ToString ());

		label.Text = "First changed line\nSecond changed line\nNew line";
		Application.Refresh ();

		// Here the AutoSize is false and the width 28 (Dim.Fill) and
		// #3127: Before: height 1 because it wasn't set and SetMinWidthHeight ensuring the minimum height
		// #3127: After: (0,0,28,2) because setting Text leaves Height set to 2..
		Assert.False (label.AutoSize);
		Assert.Equal ("(0,0,28,2)", label.Bounds.ToString ());

		label.AutoSize = true;
		Application.Refresh ();

		// Here the AutoSize ensuring the right size with width 19 (width of longest line)
		// and height 3 because the text has 3 lines
		Assert.True (label.AutoSize);
		Assert.Equal ("(0,0,19,3)", label.Bounds.ToString ());

		Application.End (rs);
	}


	//	[Fact]
	//	[AutoInitShutdown]
	//	public void AutoSize_False_TextDirection_Toggle ()
	//	{
	//		var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
	//		// View is AutoSize == true
	//		var view = new View ();
	//		win.Add (view);
	//		Application.Top.Add (win);

	//		var rs = Application.Begin (Application.Top);
	//		((FakeDriver)Application.Driver).SetBufferSize (22, 22);

	//		Assert.Equal (new Rect (0, 0, 22, 22), win.Frame);
	//		Assert.Equal (new Rect (0, 0, 22, 22), win.Margin.Frame);
	//		Assert.Equal (new Rect (0, 0, 22, 22), win.Border.Frame);
	//		Assert.Equal (new Rect (1, 1, 20, 20), win.Padding.Frame);
	//		Assert.False (view.AutoSize);
	//		Assert.Equal (TextDirection.LeftRight_TopBottom, view.TextDirection);
	//		Assert.Equal (Rect.Empty, view.Frame);
	//		Assert.Equal ("Absolute(0)", view.X.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Y.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Width.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Height.ToString ());
	//		var expected = @"
	//┌────────────────────┐
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//└────────────────────┘
	//";

	//		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	//		Assert.Equal (new Rect (0, 0, 22, 22), pos);

	//		view.Text = "Hello World";
	//		view.Width = 11;
	//		view.Height = 1;
	//		win.LayoutSubviews ();
	//		Application.Refresh ();

	//		Assert.Equal (new Rect (0, 0, 11, 1), view.Frame);
	//		Assert.Equal ("Absolute(0)", view.X.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Y.ToString ());
	//		Assert.Equal ("Absolute(11)", view.Width.ToString ());
	//		Assert.Equal ("Absolute(1)", view.Height.ToString ());
	//		expected = @"
	//┌────────────────────┐
	//│Hello World         │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//└────────────────────┘
	//";

	//		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	//		Assert.Equal (new Rect (0, 0, 22, 22), pos);

	//		view.AutoSize = true;
	//		view.Text = "Hello Worlds";
	//		Application.Refresh ();

	//		Assert.Equal (new Rect (0, 0, 12, 1), view.Frame);
	//		Assert.Equal ("Absolute(0)", view.X.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Y.ToString ());
	//		Assert.Equal ("Absolute(11)", view.Width.ToString ());
	//		Assert.Equal ("Absolute(1)", view.Height.ToString ());
	//		expected = @"
	//┌────────────────────┐
	//│Hello Worlds        │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//└────────────────────┘
	//";

	//		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	//		Assert.Equal (new Rect (0, 0, 22, 22), pos);

	//		view.TextDirection = TextDirection.TopBottom_LeftRight;
	//		Application.Refresh ();

	//		Assert.Equal (new Rect (0, 0, 11, 12), view.Frame);
	//		Assert.Equal ("Absolute(0)", view.X.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Y.ToString ());
	//		Assert.Equal ("Absolute(11)", view.Width.ToString ());
	//		Assert.Equal ("Absolute(1)", view.Height.ToString ());
	//		expected = @"
	//┌────────────────────┐
	//│H                   │
	//│e                   │
	//│l                   │
	//│l                   │
	//│o                   │
	//│                    │
	//│W                   │
	//│o                   │
	//│r                   │
	//│l                   │
	//│d                   │
	//│s                   │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//└────────────────────┘
	//";

	//		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	//		Assert.Equal (new Rect (0, 0, 22, 22), pos);

	//		view.AutoSize = false;
	//		view.Height = 1;
	//		Application.Refresh ();

	//		Assert.Equal (new Rect (0, 0, 11, 1), view.Frame);
	//		Assert.Equal ("Absolute(0)", view.X.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Y.ToString ());
	//		Assert.Equal ("Absolute(11)", view.Width.ToString ());
	//		Assert.Equal ("Absolute(1)", view.Height.ToString ());
	//		expected = @"
	//┌────────────────────┐
	//│HelloWorlds         │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//└────────────────────┘
	//";

	//		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	//		Assert.Equal (new Rect (0, 0, 22, 22), pos);

	//		view.PreserveTrailingSpaces = true;
	//		Application.Refresh ();

	//		Assert.Equal (new Rect (0, 0, 11, 1), view.Frame);
	//		Assert.Equal ("Absolute(0)", view.X.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Y.ToString ());
	//		Assert.Equal ("Absolute(11)", view.Width.ToString ());
	//		Assert.Equal ("Absolute(1)", view.Height.ToString ());
	//		expected = @"
	//┌────────────────────┐
	//│Hello World         │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//└────────────────────┘
	//";

	//		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	//		Assert.Equal (new Rect (0, 0, 22, 22), pos);

	//		view.PreserveTrailingSpaces = false;
	//		var f = view.Frame;
	//		view.Width = f.Height;
	//		view.Height = f.Width;
	//		view.TextDirection = TextDirection.TopBottom_LeftRight;
	//		Application.Refresh ();

	//		Assert.Equal (new Rect (0, 0, 1, 11), view.Frame);
	//		Assert.Equal ("Absolute(0)", view.X.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Y.ToString ());
	//		Assert.Equal ("Absolute(1)", view.Width.ToString ());
	//		Assert.Equal ("Absolute(11)", view.Height.ToString ());
	//		expected = @"
	//┌────────────────────┐
	//│H                   │
	//│e                   │
	//│l                   │
	//│l                   │
	//│o                   │
	//│                    │
	//│W                   │
	//│o                   │
	//│r                   │
	//│l                   │
	//│d                   │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//└────────────────────┘
	//";

	//		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	//		Assert.Equal (new Rect (0, 0, 22, 22), pos);

	//		view.AutoSize = true;
	//		Application.Refresh ();

	//		Assert.Equal (new Rect (0, 0, 1, 12), view.Frame);
	//		Assert.Equal ("Absolute(0)", view.X.ToString ());
	//		Assert.Equal ("Absolute(0)", view.Y.ToString ());
	//		Assert.Equal ("Absolute(1)", view.Width.ToString ());
	//		Assert.Equal ("Absolute(12)", view.Height.ToString ());
	//		expected = @"
	//┌────────────────────┐
	//│H                   │
	//│e                   │
	//│l                   │
	//│l                   │
	//│o                   │
	//│                    │
	//│W                   │
	//│o                   │
	//│r                   │
	//│l                   │
	//│d                   │
	//│s                   │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//│                    │
	//└────────────────────┘
	//";

	//		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	//		Assert.Equal (new Rect (0, 0, 22, 22), pos);
	//		Application.End (rs);
	//	}


	[Fact]
	[AutoInitShutdown]
	public void GetTextFormatterBoundsSize_GetSizeNeededForText_HotKeySpecifier ()
	{
		var text = "Say Hello 你";

		// Frame: 0, 0, 12, 1
		var horizontalView = new View {
			AutoSize = true,
			HotKeySpecifier = (Rune)'_'
		};
		horizontalView.Text = text;

		// Frame: 0, 0, 1, 12
		var verticalView = new View {
			AutoSize = true,
			HotKeySpecifier = (Rune)'_',
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		verticalView.Text = text;

		Application.Top.Add (horizontalView, verticalView);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (50, 50);

		Assert.True (horizontalView.AutoSize);
		Assert.Equal (new Rect (0, 0, 12, 1), horizontalView.Frame);
		Assert.Equal (new Size (12, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
		Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

		Assert.True (verticalView.AutoSize);
		Assert.Equal (new Rect (0, 0, 2, 11), verticalView.Frame);
		Assert.Equal (new Size (2, 11), verticalView.GetSizeNeededForTextWithoutHotKey ());
		Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());

		text = "Say He_llo 你";
		horizontalView.Text = text;
		verticalView.Text = text;

		Assert.True (horizontalView.AutoSize);
		Assert.Equal (new Rect (0, 0, 12, 1), horizontalView.Frame);
		Assert.Equal (new Size (12, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
		Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

		Assert.True (verticalView.AutoSize);
		Assert.Equal (new Rect (0, 0, 2, 11), verticalView.Frame);
		Assert.Equal (new Size (2, 11), verticalView.GetSizeNeededForTextWithoutHotKey ());
		Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());
	}


	[Fact]
	[AutoInitShutdown]
	public void AutoSize_True_Width_Height_SetMinWidthHeight_Narrow_Wide_Runes ()
	{
		var text = $"First line{Environment.NewLine}Second line";
		var horizontalView = new View {
			AutoSize = true,
			Width = 20,
			Height = 1,
			Text = text
		};
		var verticalView = new View {
			AutoSize = true,
			Y = 3,
			Height = 20,
			Width = 1,
			Text = text,
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		var win = new Window {
			AutoSize = true,
			Width = Dim.Fill (),
			Height = Dim.Fill (),
			Text = "Window"
		};
		win.Add (horizontalView, verticalView);
		Application.Top.Add (win);
		var rs = Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (20, 20);

		Assert.True (horizontalView.AutoSize);
		Assert.True (verticalView.AutoSize);
		Assert.Equal (new Rect (0, 0, 20, 2), horizontalView.Frame);
		Assert.Equal (new Rect (0, 3, 11, 20), verticalView.Frame);
		var expected = @"
┌──────────────────┐
│First line        │
│Second line       │
│                  │
│FS                │
│ie                │
│rc                │
│so                │
│tn                │
│ d                │
│l                 │
│il                │
│ni                │
│en                │
│ e                │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		verticalView.Text = $"最初の行{Environment.NewLine}二行目";
		Application.Top.Draw ();
		Assert.Equal (new Rect (0, 3, 11, 20), verticalView.Frame);
		expected = @"
┌──────────────────┐
│First line        │
│Second line       │
│                  │
│最二              │
│初行              │
│の目              │
│行                │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);
	}

	[Theory, AutoInitShutdown]
	[InlineData (true)]
	[InlineData (false)]
	public void View_Draw_Horizontal_Simple_TextAlignments (bool autoSize)
	{
		var text = "Hello World";
		var width = 20;
		var lblLeft = new View (text) { Width = width, AutoSize = autoSize };
		var lblCenter = new View (text) { Y = 1, Width = width, TextAlignment = TextAlignment.Centered, AutoSize = autoSize };
		var lblRight = new View (text) { Y = 2, Width = width, TextAlignment = TextAlignment.Right, AutoSize = autoSize };
		var lblJust = new View (text) { Y = 3, Width = width, TextAlignment = TextAlignment.Justified, AutoSize = autoSize };
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };
		frame.Add (lblLeft, lblCenter, lblRight, lblJust);
		Application.Top.Add (frame);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (width + 2, 6);

		Assert.True (lblLeft.AutoSize == autoSize);
		Assert.True (lblCenter.AutoSize == autoSize);
		Assert.True (lblRight.AutoSize == autoSize);
		Assert.True (lblJust.AutoSize == autoSize);
		Assert.True (lblLeft.TextFormatter.AutoSize == autoSize);
		Assert.True (lblCenter.TextFormatter.AutoSize == autoSize);
		Assert.True (lblRight.TextFormatter.AutoSize == autoSize);
		Assert.True (lblJust.TextFormatter.AutoSize == autoSize);
		Assert.Equal (new Rect (0, 0, width, 1), lblLeft.Frame);
		Assert.Equal (new Rect (0, 1, width, 1), lblCenter.Frame);
		Assert.Equal (new Rect (0, 2, width, 1), lblRight.Frame);
		Assert.Equal (new Rect (0, 3, width, 1), lblJust.Frame);
		if (autoSize) {
			Assert.Equal (new Size (11, 1), lblLeft.TextFormatter.Size);
			Assert.Equal (new Size (11, 1), lblCenter.TextFormatter.Size);
			Assert.Equal (new Size (11, 1), lblRight.TextFormatter.Size);
		} else {
			Assert.Equal (new Size (width, 1), lblLeft.TextFormatter.Size);
			Assert.Equal (new Size (width, 1), lblCenter.TextFormatter.Size);
			Assert.Equal (new Size (width, 1), lblRight.TextFormatter.Size);
		}
		Assert.Equal (new Size (width, 1), lblJust.TextFormatter.Size);
		Assert.Equal (new Rect (0, 0, width + 2, 6), frame.Frame);

		var expected = @"
┌────────────────────┐
│Hello World         │
│    Hello World     │
│         Hello World│
│Hello          World│
└────────────────────┘
"
			;

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, width + 2, 6), pos);
	}

	[Theory, AutoInitShutdown]
	[InlineData (true)]
	[InlineData (false)]
	public void View_Draw_Vertical_Simple_TextAlignments (bool autoSize)
	{
		var text = "Hello World";
		var height = 20;
		var lblLeft = new View (text) { Height = height, TextDirection = TextDirection.TopBottom_LeftRight, AutoSize = autoSize };
		var lblCenter = new View (text) { X = 2, Height = height, TextDirection = TextDirection.TopBottom_LeftRight, AutoSize = autoSize, VerticalTextAlignment = VerticalTextAlignment.Middle };
		var lblRight = new View (text) { X = 4, Height = height, TextDirection = TextDirection.TopBottom_LeftRight, AutoSize = autoSize, VerticalTextAlignment = VerticalTextAlignment.Bottom };
		var lblJust = new View (text) { X = 6, Height = height, TextDirection = TextDirection.TopBottom_LeftRight, AutoSize = autoSize, VerticalTextAlignment = VerticalTextAlignment.Justified };
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

		frame.Add (lblLeft, lblCenter, lblRight, lblJust);
		Application.Top.Add (frame);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (9, height + 2);

		Assert.True (lblLeft.AutoSize == autoSize);
		Assert.True (lblCenter.AutoSize == autoSize);
		Assert.True (lblRight.AutoSize == autoSize);
		Assert.True (lblJust.AutoSize == autoSize);
		Assert.True (lblLeft.TextFormatter.AutoSize == autoSize);
		Assert.True (lblCenter.TextFormatter.AutoSize == autoSize);
		Assert.True (lblRight.TextFormatter.AutoSize == autoSize);
		Assert.True (lblJust.TextFormatter.AutoSize == autoSize);
		Assert.Equal (new Rect (0, 0, 11, height), lblLeft.Frame);
		Assert.Equal (new Rect (2, 0, 11, height), lblCenter.Frame);
		Assert.Equal (new Rect (4, 0, 11, height), lblRight.Frame);
		Assert.Equal (new Rect (6, 0, 11, height), lblJust.Frame);
		if (autoSize) {
			Assert.Equal (new Size (1, 11), lblLeft.TextFormatter.Size);
			Assert.Equal (new Size (1, 11), lblCenter.TextFormatter.Size);
			Assert.Equal (new Size (1, 11), lblRight.TextFormatter.Size);
		} else {
			Assert.Equal (new Size (11, height), lblLeft.TextFormatter.Size);
			Assert.Equal (new Size (11, height), lblCenter.TextFormatter.Size);
			Assert.Equal (new Size (11, height), lblRight.TextFormatter.Size);
		}
		Assert.Equal (new Size (11, height), lblJust.TextFormatter.Size);
		Assert.Equal (new Rect (0, 0, 9, height + 2), frame.Frame);

		var expected = @"
┌───────┐
│H     H│
│e     e│
│l     l│
│l     l│
│o H   o│
│  e    │
│W l    │
│o l    │
│r o    │
│l   H  │
│d W e  │
│  o l  │
│  r l  │
│  l o  │
│  d    │
│    W W│
│    o o│
│    r r│
│    l l│
│    d d│
└───────┘
"
			;

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 9, height + 2), pos);
	}
}