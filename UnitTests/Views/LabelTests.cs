using System;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class LabelTests {
	readonly ITestOutputHelper _output;

	public LabelTests (ITestOutputHelper output) => _output = output;

	[Fact]
	[AutoInitShutdown]
	public void Constructors_Defaults ()
	{
		var label = new Label ();
		Assert.Equal (string.Empty, label.Text);
		Application.Top.Add (label);
		var rs = Application.Begin (Application.Top);

		Assert.Equal (TextAlignment.Left, label.TextAlignment);
		Assert.True (label.AutoSize);
		Assert.False (label.CanFocus);
		Assert.Equal (new Rect (0, 0, 0, 0), label.Frame);
		Assert.Equal (KeyCode.Null, label.HotKey);
		var expected = @"";
		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);

		label = new Label () {
			Text = "Test"
		};
		Assert.True (label.AutoSize);
		Assert.Equal ("Test", label.Text);
		Application.Top.Add (label);
		rs = Application.Begin (Application.Top);

		Assert.Equal ("Test", label.TextFormatter.Text);
		Assert.Equal (new Rect (0, 0, 4, 1), label.Frame);
		expected = @"
Test
";
		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Application.End (rs);

		label = new Label (3, 4, "Test");
		Assert.Equal ("Test", label.Text);
		Application.Top.Add (label);
		rs = Application.Begin (Application.Top);

		Assert.Equal ("Test", label.TextFormatter.Text);
		Assert.Equal (new Rect (3, 4, 4, 1), label.Frame);
		expected = @"
   Test
";
		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		Application.End (rs);
	}

	[Fact]
	public void TestAssignTextToLabel ()
	{
		View b = new Label { Text = "heya" };
		Assert.Equal ("heya", b.Text);
		Assert.Contains ("heya", b.TextFormatter.Text);
		b.Text = "heyb";
		Assert.Equal ("heyb", b.Text);
		Assert.Contains ("heyb", b.TextFormatter.Text);

		// with cast
		Assert.Equal ("heyb", ((Label)b).Text);
	}

	[Fact]
	[AutoInitShutdown]
	public void Update_Only_On_Or_After_Initialize ()
	{
		var label = new Label () {
			Text = "Say Hello 你",
			X = Pos.Center (),
			Y = Pos.Center ()
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (label);
		Application.Top.Add (win);

		Assert.False (label.IsInitialized);

		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);

		Assert.True (label.IsInitialized);
		Assert.Equal ("Say Hello 你", label.Text);
		Assert.Equal ("Say Hello 你", label.TextFormatter.Text);
		Assert.Equal (new Rect (0, 0, 12, 1), label.Bounds);

		var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";
		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 30, 5), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void Update_Parameterless_Only_On_Or_After_Initialize ()
	{
		var label = new Label {
			X = Pos.Center (),
			Y = Pos.Center (),
			Text = "Say Hello 你",
			AutoSize = true
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (label);
		Application.Top.Add (win);

		Assert.False (label.IsInitialized);

		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);

		Assert.True (label.IsInitialized);
		Assert.Equal ("Say Hello 你", label.Text);
		Assert.Equal ("Say Hello 你", label.TextFormatter.Text);
		Assert.Equal (new Rect (0, 0, 12, 1), label.Bounds);

		var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 30, 5), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Stays_True_With_EmptyText ()
	{
		var label = new Label {
			X = Pos.Center (),
			Y = Pos.Center (),
			AutoSize = true
		};

		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (label);
		Application.Top.Add (win);

		Assert.True (label.AutoSize);

		label.Text = "Say Hello 你";

		Assert.True (label.AutoSize);

		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);
		var expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Stays_True_Center ()
	{
		var label = new Label {
			X = Pos.Center (),
			Y = Pos.Center (),
			Text = "Say Hello 你"
		};

		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (label);
		Application.Top.Add (win);

		Assert.True (label.AutoSize);

		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);
		var expected = @"
┌────────────────────────────┐
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
┌────────────────────────────┐
│                            │
│    Say Hello 你 changed    │
│                            │
└────────────────────────────┘
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void AutoSize_Stays_True_AnchorEnd ()
	{
		var label = new Label {
			Y = Pos.Center (),
			Text = "Say Hello 你",
			AutoSize = true
		};
		label.X = Pos.AnchorEnd () - Pos.Function (() => label.TextFormatter.Text.GetColumns ());

		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (label);
		Application.Top.Add (win);

		Assert.True (label.AutoSize);

		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);
		var expected = @"
┌────────────────────────────┐
│                            │
│                Say Hello 你│
│                            │
└────────────────────────────┘
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		Assert.True (label.AutoSize);
		label.Text = "Say Hello 你 changed";
		Assert.True (label.AutoSize);
		Application.Refresh ();
		expected = @"
┌────────────────────────────┐
│                            │
│        Say Hello 你 changed│
│                            │
└────────────────────────────┘
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Pos_Center_Layout_AutoSize_True ()
	{
		var Label = new Label () {
			Text = "012345678901",
			X = Pos.Center (),
			Y = Pos.Center ()
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (Label);
		Application.Top.Add (win);

		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);
		Assert.True (Label.AutoSize);
		//Assert.Equal (new Rect (5, 1, 18, 1), Label.Frame);
		var expected = @"
┌────────────────────────────┐
│                            │
│        012345678901        │
│                            │
└────────────────────────────┘
";

		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Pos_Center_Layout_AutoSize_False ()
	{
		var Label = new Label () {
			Text = "012345678901",
			X = Pos.Center (),
			Y = Pos.Center (),
			AutoSize = false,
			Width = 20,
			TextAlignment = TextAlignment.Centered
		};
		var win = new Window {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		win.Add (Label);
		Application.Top.Add (win);

		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (30, 5);
		Assert.False (Label.AutoSize);
		Assert.Equal (new Rect (4, 1, 20, 1), Label.Frame);
		var expected = @"
┌────────────────────────────┐
│                            │
│        012345678901        │
│                            │
└────────────────────────────┘
";
		TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_HotKeyChanged_EventFires ()
	{
		var label = new Label ("Yar");
		label.HotKey = 'Y';

		object sender = null;
		KeyChangedEventArgs args = null;

		label.HotKeyChanged += (s, e) => {
			sender = s;
			args = e;
		};

		label.HotKey = Key.R;
		Assert.Same (label, sender);
		Assert.Equal (KeyCode.Y | KeyCode.ShiftMask, args.OldKey);
		Assert.Equal (Key.R, args.NewKey);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_HotKeyChanged_EventFires_WithNone ()
	{
		var label = new Label ();

		object sender = null;
		KeyChangedEventArgs args = null;

		label.HotKeyChanged += (s, e) => {
			sender = s;
			args = e;

		};

		label.HotKey = KeyCode.R;
		Assert.Same (label, sender);
		Assert.Equal (KeyCode.Null, args.OldKey);
		Assert.Equal (KeyCode.R, args.NewKey);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_WordWrap_PreserveTrailingSpaces_Horizontal_With_Simple_Runes ()
	{
		var text = "A sentence has words.";
		var width = 3;
		var height = 8;
		var wrappedLines = TextFormatter.WordWrapText (text, width, true);
		var breakLines = "";
		foreach (var line in wrappedLines) {
			breakLines += $"{line}{Environment.NewLine}";
		}
		var label = new Label (breakLines) { AutoSize = false, Width = Dim.Fill (), Height = Dim.Fill () };
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

		frame.Add (label);
		Application.Top.Add (frame);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (width + 2, height + 2);

		Assert.False (label.AutoSize);
		Assert.Equal (new Rect (0, 0, width, height), label.Frame);
		Assert.Equal (new Rect (0, 0, width + 2, height + 2), frame.Frame);

		var expected = @"
┌───┐
│A  │
│sen│
│ten│
│ce │
│has│
│   │
│wor│
│ds.│
└───┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, width + 2, height + 2), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_WordWrap_PreserveTrailingSpaces_Vertical_With_Simple_Runes ()
	{
		var text = "A sentence has words.";
		var width = 8;
		var height = 3;
		var wrappedLines = TextFormatter.WordWrapText (text, height, true);
		var breakLines = "";
		for (var i = 0; i < wrappedLines.Count; i++) {
			breakLines += $"{wrappedLines [i]}{(i < wrappedLines.Count - 1 ? Environment.NewLine : string.Empty)}";
		}
		var label = new Label (breakLines) {
			AutoSize = false,
			TextDirection = TextDirection.TopBottom_LeftRight,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

		frame.Add (label);
		Application.Top.Add (frame);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (width + 2, height + 2);

		Assert.False (label.AutoSize);
		Assert.Equal (new Rect (0, 0, width, height), label.Frame);
		Assert.Equal (new Rect (0, 0, width + 2, height + 2), frame.Frame);

		var expected = @"
┌────────┐
│Astch wd│
│ eeea os│
│ nn s r.│
└────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, width + 2, height + 2), pos);
	}

	[Theory]
	[AutoInitShutdown]
	[InlineData (false)]
	[InlineData (true)]
	public void Label_WordWrap_PreserveTrailingSpaces_Horizontal_With_Wide_Runes (bool autoSize)
	{
		var text = "文に は言葉 があり ます。";
		var width = 6;
		var height = 8;
		var wrappedLines = TextFormatter.WordWrapText (text, width, true);
		var breakLines = "";
		foreach (var line in wrappedLines) {
			breakLines += $"{line}{Environment.NewLine}";
		}
		var label = new Label (breakLines) { AutoSize = autoSize };
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

		if (!autoSize) {
			label.Width = Dim.Fill ();
			label.Height = Dim.Fill ();
		}

		frame.Add (label);
		Application.Top.Add (frame);
		Application.Begin (Application.Top);

		Assert.True (label.AutoSize == autoSize);
		if (autoSize) {
			// The size of the wrappedLines [1]
			Assert.Equal (new Rect (0, 0, 6, 6), label.Frame);
			Assert.Equal (new Size (width, height - 2), label.TextFormatter.Size);
		} else {
			Assert.Equal (new Rect (0, 0, 78, 23), label.Frame);
			Assert.Equal (new Size (78, 23), label.TextFormatter.Size);
		}
		((FakeDriver)Application.Driver).SetBufferSize (width + 2, height + 2);

		if (autoSize) {
			Assert.Equal (new Size (width, height - 2), label.TextFormatter.Size);
			Assert.Equal (new Rect (0, 0, width, height - 2), label.Frame);
		} else {
			Assert.Equal (new Size (width, height), label.TextFormatter.Size);
			Assert.Equal (new Rect (0, 0, width, height), label.Frame);
		}
		Assert.Equal (new Rect (0, 0, width + 2, height + 2), frame.Frame);

		var expected = @"
┌──────┐
│文に  │
│は言葉│
│ があ │
│り ま │
│す。  │
│      │
│      │
│      │
└──────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, width + 2, height + 2), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_WordWrap_PreserveTrailingSpaces_Vertical_With_Wide_Runes ()
	{
		var text = "文に は言葉 があり ます。";
		var width = 8;
		var height = 4;
		var wrappedLines = TextFormatter.WordWrapText (text, width, true);
		var breakLines = "";
		for (var i = 0; i < wrappedLines.Count; i++) {
			breakLines += $"{wrappedLines [i]}{(i < wrappedLines.Count - 1 ? Environment.NewLine : string.Empty)}";
		}
		var label = new Label (breakLines) {
			AutoSize = false,
			TextDirection = TextDirection.TopBottom_LeftRight,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

		frame.Add (label);
		Application.Top.Add (frame);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (width + 2, height + 2);

		Assert.False (label.AutoSize);
		Assert.Equal (new Rect (0, 0, width, height), label.Frame);
		Assert.Equal (new Rect (0, 0, width + 2, height + 2), frame.Frame);

		var expected = @"
┌────────┐
│文言あす│
│に葉り。│
│        │
│はがま  │
└────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, width + 2, height + 2), pos);
	}

	[Fact, SetupFakeDriver]
	public void Label_Draw_Horizontal_Simple_TextAlignments_Justified ()
	{
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };
		var text = "01234 01234";
		var width = 20;
		var lblJust = new Label (text) { Y = 0, AutoSize = false, Width = width, TextAlignment = TextAlignment.Justified };

		frame.Add (lblJust);
		((FakeDriver)Application.Driver).SetBufferSize (width + 2, 3);
		frame.BeginInit ();
		frame.EndInit ();
		frame.Draw ();

		var expected = @"
┌────────────────────┐
│01234          01234│
└────────────────────┘
";
		Assert.Equal (new Rect (0, 0, width, 1), lblJust.Frame);

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_Draw_Horizontal_Simple_Runes ()
	{
		var label = new Label ("Demo Simple Rune");
		Application.Top.Add (label);
		Application.Begin (Application.Top);

		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 16, 1), label.Frame);

		var expected = @"
Demo Simple Rune
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 16, 1), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_Draw_Vertical_Simple_Runes ()
	{
		var label = new Label () {
			Text = "Demo Simple Rune",
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		Application.Top.Add (label);
		Application.Begin (Application.Top);

		Assert.NotNull (label.Width);
		Assert.NotNull (label.Height);

		var expected = @"
D
e
m
o
 
S
i
m
p
l
e
 
R
u
n
e
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 1, 16), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_Draw_Horizontal_Wide_Runes ()
	{
		var label = new Label ("デモエムポンズ");
		Application.Top.Add (label);
		Application.Begin (Application.Top);

		Assert.True (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 14, 1), label.Frame);

		var expected = @"
デモエムポンズ
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 14, 1), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_Draw_Vertical_Wide_Runes ()
	{
		var label = new Label () {
			Text = "デモエムポンズ",
			TextDirection = TextDirection.TopBottom_LeftRight
		};
		Application.Top.Add (label);
		Application.Begin (Application.Top);

		var expected = @"
デ
モ
エ
ム
ポ
ン
ズ
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 2, 7), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_Draw_Vertical_Wide_Runes_With_ForceValidatePosDim ()
	{
		var label = new Label () {
			Text = "デモエムポンズ",
			AutoSize = false,
			Width = Dim.Fill (),
			Height = Dim.Percent (50f),
			TextDirection = TextDirection.TopBottom_LeftRight,
			ValidatePosDim = true
		};
		Application.Top.Add (label);
		Application.Begin (Application.Top);

		Assert.False (label.AutoSize);
		Assert.Equal (new Rect (0, 0, 80, 12), label.Frame);

		var expected = @"
デ
モ
エ
ム
ポ
ン
ズ
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 2, 7), pos);
	}

	[Theory, SetupFakeDriver]
	[InlineData (true)]
	[InlineData (false)]
	public void Label_Draw_Horizontal_Simple_TextAlignments (bool autoSize)
	{
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

		var text = "Hello World";
		var width = 20;
		var lblLeft = new Label (text) { AutoSize = autoSize };
		var lblCenter = new Label (text) { AutoSize = autoSize, Y = 1, TextAlignment = TextAlignment.Centered };
		var lblRight = new Label (text) { AutoSize = autoSize, Y = 2, TextAlignment = TextAlignment.Right };
		var lblJust = new Label (text) { AutoSize = autoSize, Y = 3, TextAlignment = TextAlignment.Justified };

		if (!autoSize) {
			lblLeft.Width = lblCenter.Width = lblRight.Width = lblJust.Width = width;
		}

		frame.Add (lblLeft, lblCenter, lblRight, lblJust);
		((FakeDriver)Application.Driver).SetBufferSize (width + 2, 6);
		frame.BeginInit ();
		frame.EndInit ();
		frame.Draw ();

		Assert.True (lblLeft.AutoSize == autoSize);
		Assert.True (lblCenter.AutoSize == autoSize);
		Assert.True (lblRight.AutoSize == autoSize);
		Assert.True (lblJust.AutoSize == autoSize);
		Assert.True (lblLeft.TextFormatter.AutoSize == autoSize);
		Assert.True (lblCenter.TextFormatter.AutoSize == autoSize);
		Assert.True (lblRight.TextFormatter.AutoSize == autoSize);
		Assert.True (lblJust.TextFormatter.AutoSize == autoSize);
		string expected;
		if (autoSize) {
			Assert.Equal (new Rect (0, 0, 11, 1), lblLeft.Frame);
			Assert.Equal (new Rect (0, 1, 11, 1), lblCenter.Frame);
			Assert.Equal (new Rect (0, 2, 11, 1), lblRight.Frame);
			Assert.Equal (new Rect (0, 3, 11, 1), lblJust.Frame);
			Assert.Equal (new Size (11, 1), lblLeft.TextFormatter.Size);
			Assert.Equal (new Size (11, 1), lblCenter.TextFormatter.Size);
			Assert.Equal (new Size (11, 1), lblRight.TextFormatter.Size);
			Assert.Equal (new Size (11, 1), lblJust.TextFormatter.Size);
			expected = @"
┌────────────────────┐
│Hello World         │
│Hello World         │
│Hello World         │
│Hello World         │
└────────────────────┘
";

		} else {
			Assert.Equal (new Rect (0, 0, width, 1), lblLeft.Frame);
			Assert.Equal (new Rect (0, 1, width, 1), lblCenter.Frame);
			Assert.Equal (new Rect (0, 2, width, 1), lblRight.Frame);
			Assert.Equal (new Rect (0, 3, width, 1), lblJust.Frame);
			Assert.Equal (new Size (width, 1), lblLeft.TextFormatter.Size);
			Assert.Equal (new Size (width, 1), lblCenter.TextFormatter.Size);
			Assert.Equal (new Size (width, 1), lblRight.TextFormatter.Size);
			Assert.Equal (new Size (width, 1), lblJust.TextFormatter.Size);
			expected = @"
┌────────────────────┐
│Hello World         │
│    Hello World     │
│         Hello World│
│Hello          World│
└────────────────────┘
";
		}
		Assert.Equal (new Rect (0, 0, width + 2, 6), frame.Frame);
		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Theory]
	[AutoInitShutdown]
	[InlineData (true)]
	[InlineData (false)]
	public void Label_Draw_Vertical_Simple_TextAlignments (bool autoSize)
	{
		var text = "Hello World";
		var height = 20;
		var lblLeft = new Label (text, TextDirection.TopBottom_LeftRight, autoSize) { };
		var lblCenter = new Label (text, TextDirection.TopBottom_LeftRight, autoSize) { X = 2, VerticalTextAlignment = VerticalTextAlignment.Middle };
		var lblRight = new Label (text, TextDirection.TopBottom_LeftRight, autoSize) { X = 4, VerticalTextAlignment = VerticalTextAlignment.Bottom };
		var lblJust = new Label (text, TextDirection.TopBottom_LeftRight, autoSize) { X = 6, VerticalTextAlignment = VerticalTextAlignment.Justified };
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

		if (!autoSize) {
			lblLeft.Height = lblCenter.Height = lblRight.Height = lblJust.Height = height;
		}

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
		string expected;
		if (autoSize) {
			Assert.Equal (new Size (1, 11), lblLeft.TextFormatter.Size);
			Assert.Equal (new Size (1, 11), lblCenter.TextFormatter.Size);
			Assert.Equal (new Size (1, 11), lblRight.TextFormatter.Size);

			expected = @"
┌───────┐
│H H H H│
│e e e e│
│l l l l│
│l l l l│
│o o o o│
│       │
│W W W W│
│o o o o│
│r r r r│
│l l l l│
│d d d d│
│       │
│       │
│       │
│       │
│       │
│       │
│       │
│       │
│       │
└───────┘
";
		} else {
			Assert.Equal (new Size (1, height), lblLeft.TextFormatter.Size);
			Assert.Equal (new Size (1, height), lblCenter.TextFormatter.Size);
			Assert.Equal (new Size (1, height), lblRight.TextFormatter.Size);
			Assert.Equal (new Rect (0, 0, 1, height), lblLeft.Frame);
			Assert.Equal (new Rect (2, 0, 1, height), lblCenter.Frame);
			Assert.Equal (new Rect (4, 0, 1, height), lblRight.Frame);
			Assert.Equal (new Rect (6, 0, 1, height), lblJust.Frame);
			Assert.Equal (new Size (1, height), lblJust.TextFormatter.Size);

			expected = @"
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
";
		}
		Assert.Equal (new Rect (0, 0, 9, height + 2), frame.Frame);

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_Draw_Horizontal_Wide_TextAlignments ()
	{
		var text = "こんにちは 世界";
		var width = 25;
		var lblLeft = new Label (text) { AutoSize = false, Width = width };
		var lblCenter = new Label (text) { AutoSize = false, Y = 1, Width = width, TextAlignment = TextAlignment.Centered };
		var lblRight = new Label (text) { AutoSize = false, Y = 2, Width = width, TextAlignment = TextAlignment.Right };
		var lblJust = new Label (text) { AutoSize = false, Y = 3, Width = width, TextAlignment = TextAlignment.Justified };
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

		frame.Add (lblLeft, lblCenter, lblRight, lblJust);
		Application.Top.Add (frame);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (width + 2, 6);

		Assert.False (lblLeft.AutoSize);
		Assert.False (lblCenter.AutoSize);
		Assert.False (lblRight.AutoSize);
		Assert.False (lblJust.AutoSize);
		Assert.Equal (new Rect (0, 0, width, 1), lblLeft.Frame);
		Assert.Equal (new Rect (0, 1, width, 1), lblCenter.Frame);
		Assert.Equal (new Rect (0, 2, width, 1), lblRight.Frame);
		Assert.Equal (new Rect (0, 3, width, 1), lblJust.Frame);
		Assert.Equal (new Rect (0, 0, width + 2, 6), frame.Frame);

		var expected = @"
┌─────────────────────────┐
│こんにちは 世界          │
│     こんにちは 世界     │
│          こんにちは 世界│
│こんにちは           世界│
└─────────────────────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_Draw_Vertical_Wide_TextAlignments ()
	{
		var text = "こんにちは 世界";
		var height = 23;
		var lblLeft = new Label (text) { AutoSize = false, Width = 2, Height = height, TextDirection = TextDirection.TopBottom_LeftRight };
		var lblCenter = new Label (text) { AutoSize = false, X = 3, Width = 2, Height = height, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Middle };
		var lblRight = new Label (text) { AutoSize = false, X = 6, Width = 2, Height = height, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Bottom };
		var lblJust = new Label (text) { AutoSize = false, X = 9, Width = 2, Height = height, TextDirection = TextDirection.TopBottom_LeftRight, VerticalTextAlignment = VerticalTextAlignment.Justified };
		var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

		frame.Add (lblLeft, lblCenter, lblRight, lblJust);
		Application.Top.Add (frame);
		Application.Begin (Application.Top);
		((FakeDriver)Application.Driver).SetBufferSize (13, height + 2);

		// All AutoSize are false because the Frame.Height != TextFormatter.Size.Height
		Assert.False (lblLeft.AutoSize);
		Assert.False (lblCenter.AutoSize);
		Assert.False (lblRight.AutoSize);
		Assert.False (lblJust.AutoSize);
		Assert.Equal (new Rect (0, 0, 2, height), lblLeft.Frame);
		Assert.Equal (new Rect (3, 0, 2, height), lblCenter.Frame);
		Assert.Equal (new Rect (6, 0, 2, height), lblRight.Frame);
		Assert.Equal (new Rect (9, 0, 2, height), lblJust.Frame);
		Assert.Equal (new Rect (0, 0, 13, height + 2), frame.Frame);

		var expected = @"
┌───────────┐
│こ       こ│
│ん       ん│
│に       に│
│ち       ち│
│は       は│
│           │
│世         │
│界 こ      │
│   ん      │
│   に      │
│   ち      │
│   は      │
│           │
│   世      │
│   界      │
│      こ   │
│      ん   │
│      に   │
│      ち   │
│      は   │
│           │
│      世 世│
│      界 界│
└───────────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 13, height + 2), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void Label_Draw_Fill_Remaining ()
	{
		var view = new View ("This view needs to be cleared before rewritten.");

		var tf1 = new TextFormatter { Direction = TextDirection.LeftRight_TopBottom };
		tf1.Text = "This TextFormatter (tf1) without fill will not be cleared on rewritten.";
		var tf1Size = tf1.Size;

		var tf2 = new TextFormatter { Direction = TextDirection.LeftRight_TopBottom };
		tf2.Text = "This TextFormatter (tf2) with fill will be cleared on rewritten.";
		var tf2Size = tf2.Size;

		Application.Top.Add (view);
		Application.Begin (Application.Top);

		tf1.Draw (new Rect (new Point (0, 1), tf1Size), view.GetNormalColor (), view.ColorScheme.HotNormal, default, false);

		tf2.Draw (new Rect (new Point (0, 2), tf2Size), view.GetNormalColor (), view.ColorScheme.HotNormal);

		TestHelpers.AssertDriverContentsWithFrameAre (@"
This view needs to be cleared before rewritten.                        
This TextFormatter (tf1) without fill will not be cleared on rewritten.
This TextFormatter (tf2) with fill will be cleared on rewritten.       
", _output);

		view.Text = "This view is rewritten.";
		view.Draw ();

		tf1.Text = "This TextFormatter (tf1) is rewritten.";
		tf1.Draw (new Rect (new Point (0, 1), tf1Size), view.GetNormalColor (), view.ColorScheme.HotNormal, default, false);

		tf2.Text = "This TextFormatter (tf2) is rewritten.";
		tf2.Draw (new Rect (new Point (0, 2), tf2Size), view.GetNormalColor (), view.ColorScheme.HotNormal);

		TestHelpers.AssertDriverContentsWithFrameAre (@"
This view is rewritten.                                                
This TextFormatter (tf1) is rewritten.will not be cleared on rewritten.
This TextFormatter (tf2) is rewritten.                                 
", _output);
	}
}