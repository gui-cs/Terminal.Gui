using Xunit;
using Xunit.Abstractions;
using static Terminal.Gui.Application;

namespace Terminal.Gui.DialogTests;

public class DialogTests {
	readonly ITestOutputHelper _output;

	public DialogTests (ITestOutputHelper output) => _output = output;

	(RunState, Dialog) RunButtonTestDialog (string title, int width, Dialog.ButtonAlignments align, params Button [] btns)
	{
		var dlg = new Dialog (btns) {
			Title = title,
			X = 0,
			Y = 0,
			Width = width,
			Height = 1,
			ButtonAlignment = align
		};
		// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
		dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
		return (Begin (dlg), dlg);
	}

	[Fact]
	[AutoInitShutdown]
	public void Size_Default ()
	{
		var d = new Dialog ();
		Begin (d);
		((FakeDriver)Driver).SetBufferSize (100, 100);

		// Default size is Percent(85) 
		Assert.Equal (new Size ((int)(100 * .85), (int)(100 * .85)), d.Frame.Size);
	}

	[Fact]
	[AutoInitShutdown]
	public void Location_Default ()
	{
		var d = new Dialog ();
		Begin (d);
		((FakeDriver)Driver).SetBufferSize (100, 100);

		// Default location is centered, so 100 / 2 - 85 / 2 = 7
		var expected = 7;
		Assert.Equal (new Point (expected, expected), d.Frame.Location);
	}

	[Fact]
	[AutoInitShutdown]
	public void Size_Not_Default ()
	{
		var d = new Dialog {
			Width = 50,
			Height = 50
		};

		Begin (d);
		((FakeDriver)Driver).SetBufferSize (100, 100);

		// Default size is Percent(85) 
		Assert.Equal (new Size (50, 50), d.Frame.Size);
	}

	[Fact]
	[AutoInitShutdown]
	public void Location_Not_Default ()
	{
		var d = new Dialog {
			X = 1,
			Y = 1
		};
		Begin (d);
		((FakeDriver)Driver).SetBufferSize (100, 100);

		// Default location is centered, so 100 / 2 - 85 / 2 = 7
		var expected = 1;
		Assert.Equal (new Point (expected, expected), d.Frame.Location);
	}

	[Fact]
	[AutoInitShutdown]
	public void Location_When_Application_Top_Not_Default ()
	{
		var expected = 5;
		var d = new Dialog {
			X = expected,
			Y = expected,
			Height = 5,
			Width = 5
		};
		Begin (d);
		((FakeDriver)Driver).SetBufferSize (20, 10);

		// Default location is centered, so 100 / 2 - 85 / 2 = 7
		Assert.Equal (new Point (expected, expected), d.Frame.Location);

		TestHelpers.AssertDriverContentsWithFrameAre (@"
     ┌───┐
     │   │
     │   │
     │   │
     └───┘", _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Location_When_Not_Application_Top_Not_Default ()
	{
		Top.BorderStyle = LineStyle.Double;

		var iterations = -1;
		Iteration += (s, a) => {
			iterations++;

			if (iterations == 0) {
				var d = new Dialog {
					X = 5,
					Y = 5,
					Height = 3,
					Width = 5
				};
				Begin (d);

				Assert.Equal (new Point (5, 5), d.Frame.Location);
				TestHelpers.AssertDriverContentsWithFrameAre (@"
╔══════════════════╗
║                  ║
║                  ║
║                  ║
║                  ║
║    ┌───┐         ║
║    │   │         ║
║    └───┘         ║
║                  ║
╚══════════════════╝", _output);

				d = new Dialog {
					X = 5,
					Y = 5
				};
				Begin (d);

				// This is because of PostionTopLevels and EnsureVisibleBounds
				Assert.Equal (new Point (3, 2), d.Frame.Location);
				// #3127: Before					
				//					Assert.Equal (new Size (17, 8), d.Frame.Size);
				//					TestHelpers.AssertDriverContentsWithFrameAre (@"
				//╔══════════════════╗
				//║                  ║
				//║  ┌───────────────┐
				//║  │               │
				//║  │               │
				//║  │               │
				//║  │               │
				//║  │               │
				//║  │               │
				//╚══└───────────────┘", _output);

				// #3127: After: Because Toplevel is now Width/Height = Dim.Filll
				Assert.Equal (new Size (15, 6), d.Frame.Size);
				TestHelpers.AssertDriverContentsWithFrameAre (@"
╔══════════════════╗
║                  ║
║  ┌─────────────┐ ║
║  │             │ ║
║  │             │ ║
║  │             │ ║
║  │             │ ║
║  └─────────────┘ ║
║                  ║
╚══════════════════╝", _output);

			} else if (iterations > 0) {
				RequestStop ();
			}
		};

		Begin (Top);
		((FakeDriver)Driver).SetBufferSize (20, 10);
		Run ();
	}

	[Fact]
	[AutoInitShutdown]
	public void ButtonAlignment_One ()
	{
		var d = (FakeDriver)Driver;
		RunState runstate = null;

		var title = "1234";
		// E.g "|[ ok ]|"
		var btnText = "ok";
		var buttonRow = $"{CM.Glyphs.VLine}  {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}  {CM.Glyphs.VLine}";
		var width = buttonRow.Length;

		d.SetBufferSize (width, 1);

		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btnText));
		// Center
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify 
		buttonRow = $"{CM.Glyphs.VLine}    {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btnText));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		buttonRow = $"{CM.Glyphs.VLine}    {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btnText));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		buttonRow = $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}    {CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btnText));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Wider
		buttonRow = $"{CM.Glyphs.VLine}   {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}   {CM.Glyphs.VLine}";
		width = buttonRow.Length;

		d.SetBufferSize (width, 1);

		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btnText));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify
		buttonRow = $"{CM.Glyphs.VLine}      {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btnText));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		buttonRow = $"{CM.Glyphs.VLine}      {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btnText));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		buttonRow = $"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}      {CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btnText));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void ButtonAlignment_Two ()
	{
		RunState runstate = null;

		var d = (FakeDriver)Driver;

		var title = "1234";
		// E.g "|[ yes ][ no ]|"
		var btn1Text = "yes";
		var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
		var btn2Text = "no";
		var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";

		var buttonRow = $@"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}";
		var width = buttonRow.Length;

		d.SetBufferSize (buttonRow.Length, 3);

		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify
		buttonRow = $@"{CM.Glyphs.VLine}{btn1}   {btn2}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		buttonRow = $@"{CM.Glyphs.VLine}  {btn1} {btn2}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		buttonRow = $@"{CM.Glyphs.VLine}{btn1} {btn2}  {CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void ButtonAlignment_Two_Hidden ()
	{
		RunState runstate = null;
		var firstIteration = false;

		var d = (FakeDriver)Driver;

		var title = "1234";
		// E.g "|[ yes ][ no ]|"
		var btn1Text = "yes";
		var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
		var btn2Text = "no";
		var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";

		var buttonRow = $@"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}";
		var width = buttonRow.Length;

		d.SetBufferSize (buttonRow.Length, 3);

		Dialog dlg = null;
		Button button1, button2;

		// Default (Center)
		button1 = new Button (btn1Text);
		button2 = new Button (btn2Text);
		(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, button1, button2);
		button1.Visible = false;
		RunIteration (ref runstate, ref firstIteration);
		buttonRow = $@"{CM.Glyphs.VLine}         {btn2} {CM.Glyphs.VLine}";
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify
		Assert.Equal (width, buttonRow.Length);
		button1 = new Button (btn1Text);
		button2 = new Button (btn2Text);
		(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, button1, button2);
		button1.Visible = false;
		RunIteration (ref runstate, ref firstIteration);
		buttonRow = $@"{CM.Glyphs.VLine}          {btn2}{CM.Glyphs.VLine}";
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		Assert.Equal (width, buttonRow.Length);
		button1 = new Button (btn1Text);
		button2 = new Button (btn2Text);
		(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, button1, button2);
		button1.Visible = false;
		RunIteration (ref runstate, ref firstIteration);
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		Assert.Equal (width, buttonRow.Length);
		button1 = new Button (btn1Text);
		button2 = new Button (btn2Text);
		(runstate, dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, button1, button2);
		button1.Visible = false;
		RunIteration (ref runstate, ref firstIteration);
		buttonRow = $@"{CM.Glyphs.VLine}        {btn2}  {CM.Glyphs.VLine}";
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void ButtonAlignment_Three ()
	{
		RunState runstate = null;

		var d = (FakeDriver)Driver;

		var title = "1234";
		// E.g "|[ yes ][ no ][ maybe ]|"
		var btn1Text = "yes";
		var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
		var btn2Text = "no";
		var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
		var btn3Text = "maybe";
		var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";

		var buttonRow = $@"{CM.Glyphs.VLine} {btn1} {btn2} {btn3} {CM.Glyphs.VLine}";
		var width = buttonRow.Length;

		d.SetBufferSize (buttonRow.Length, 3);

		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify
		buttonRow = $@"{CM.Glyphs.VLine}{btn1}  {btn2}  {btn3}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		buttonRow = $@"{CM.Glyphs.VLine}  {btn1} {btn2} {btn3}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		buttonRow = $@"{CM.Glyphs.VLine}{btn1} {btn2} {btn3}  {CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void ButtonAlignment_Four ()
	{
		RunState runstate = null;

		var d = (FakeDriver)Driver;

		var title = "1234";

		// E.g "|[ yes ][ no ][ maybe ]|"
		var btn1Text = "yes";
		var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
		var btn2Text = "no";
		var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
		var btn3Text = "maybe";
		var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
		var btn4Text = "never";
		var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";

		var buttonRow = $"{CM.Glyphs.VLine} {btn1} {btn2} {btn3} {btn4} {CM.Glyphs.VLine}";
		var width = buttonRow.Length;
		d.SetBufferSize (buttonRow.Length, 3);

		// Default - Center
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify
		buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2}  {btn3}  {btn4}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		buttonRow = $"{CM.Glyphs.VLine}  {btn1} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}  {CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void ButtonAlignment_Four_On_Too_Small_Width ()
	{
		RunState runstate = null;

		var d = (FakeDriver)Driver;

		var title = "1234";

		// E.g "|[ yes ][ no ][ maybe ][ never ]|"
		var btn1Text = "yes";
		var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
		var btn2Text = "no";
		var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
		var btn3Text = "maybe";
		var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
		var btn4Text = "never";
		var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";
		var buttonRow = string.Empty;

		var width = 30;
		d.SetBufferSize (width, 1);

		// Default - Center
		buttonRow = $"{CM.Glyphs.VLine}es {CM.Glyphs.RightBracket} {btn2} {btn3} {CM.Glyphs.LeftBracket} neve{CM.Glyphs.VLine}";
		(runstate, var dlg) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		Assert.Equal (new Size (width, 1), dlg.Frame.Size);
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify
		buttonRow =
			$"{CM.Glyphs.VLine}{CM.Glyphs.LeftBracket} yes {CM.Glyphs.LeftBracket} no {CM.Glyphs.LeftBracket} maybe {CM.Glyphs.LeftBracket} never {CM.Glyphs.RightBracket}{CM.Glyphs.VLine}";
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		buttonRow = $"{CM.Glyphs.VLine}{CM.Glyphs.RightBracket} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {CM.Glyphs.LeftBracket} n{CM.Glyphs.VLine}";
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void ButtonAlignment_Four_Wider ()
	{
		RunState runstate = null;

		var d = (FakeDriver)Driver;

		var title = "1234";

		// E.g "|[ yes ][ no ][ maybe ]|"
		var btn1Text = "yes";
		var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
		var btn2Text = "no";
		var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
		var btn3Text = "你你你你你"; // This is a wide char
		var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
		// Requires a Nerd Font
		var btn4Text = "\uE36E\uE36F\uE370\uE371\uE372\uE373";
		var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";

		// Note extra spaces to make dialog even wider
		//                         123456                           123456
		var buttonRow = $"{CM.Glyphs.VLine}      {btn1} {btn2} {btn3} {btn4}      {CM.Glyphs.VLine}";
		var width = buttonRow.GetColumns ();
		d.SetBufferSize (width, 3);

		// Default - Center
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify
		buttonRow = $"{CM.Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.GetColumns ());
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		buttonRow = $"{CM.Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.GetColumns ());
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.GetColumns ());
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void ButtonAlignment_Four_WideOdd ()
	{
		RunState runstate = null;

		var d = (FakeDriver)Driver;

		var title = "1234";

		// E.g "|[ yes ][ no ][ maybe ]|"
		var btn1Text = "really long button 1";
		var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
		var btn2Text = "really long button 2";
		var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";
		var btn3Text = "really long button 3";
		var btn3 = $"{CM.Glyphs.LeftBracket} {btn3Text} {CM.Glyphs.RightBracket}";
		var btn4Text = "really long button 44"; // 44 is intentional to make length different than rest
		var btn4 = $"{CM.Glyphs.LeftBracket} {btn4Text} {CM.Glyphs.RightBracket}";

		// Note extra spaces to make dialog even wider
		//                         123456                          1234567
		var buttonRow = $"{CM.Glyphs.VLine}      {btn1} {btn2} {btn3} {btn4}      {CM.Glyphs.VLine}";
		var width = buttonRow.Length;
		d.SetBufferSize (buttonRow.Length, 1);

		// Default - Center
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify
		buttonRow = $"{CM.Glyphs.VLine}{btn1}     {btn2}     {btn3}     {btn4}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Justify, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		buttonRow = $"{CM.Glyphs.VLine}            {btn1} {btn2} {btn3} {btn4}{CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Right, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2} {btn3} {btn4}            {CM.Glyphs.VLine}";
		Assert.Equal (width, buttonRow.Length);
		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Left, new Button (btn1Text), new Button (btn2Text), new Button (btn3Text), new Button (btn4Text));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void Zero_Buttons_Works ()
	{
		RunState runstate = null;

		var d = (FakeDriver)Driver;

		var title = "1234";

		var buttonRow = $"{CM.Glyphs.VLine}        {CM.Glyphs.VLine}";
		var width = buttonRow.Length;
		d.SetBufferSize (buttonRow.Length, 3);

		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, null);
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void One_Button_Works ()
	{
		RunState runstate = null;

		var d = (FakeDriver)Driver;

		var title = "";
		var btnText = "ok";
		var buttonRow = $"{CM.Glyphs.VLine}   {CM.Glyphs.LeftBracket} {btnText} {CM.Glyphs.RightBracket}   {CM.Glyphs.VLine}";

		var width = buttonRow.Length;
		d.SetBufferSize (buttonRow.Length, 10);

		(runstate, var _) = RunButtonTestDialog (title, width, Dialog.ButtonAlignments.Center, new Button (btnText));
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void Add_Button_Works ()
	{
		RunState runstate = null;

		var d = (FakeDriver)Driver;

		var title = "1234";
		var btn1Text = "yes";
		var btn1 = $"{CM.Glyphs.LeftBracket} {btn1Text} {CM.Glyphs.RightBracket}";
		var btn2Text = "no";
		var btn2 = $"{CM.Glyphs.LeftBracket} {btn2Text} {CM.Glyphs.RightBracket}";

		// We test with one button first, but do this to get the width right for 2
		var width = $@"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}".Length;
		d.SetBufferSize (width, 1);

		// Default (center)
		var dlg = new Dialog (new Button (btn1Text)) { Title = title, Width = width, Height = 1, ButtonAlignment = Dialog.ButtonAlignments.Center };
		// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
		dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
		runstate = Begin (dlg);
		var buttonRow = $"{CM.Glyphs.VLine}     {btn1}    {CM.Glyphs.VLine}";
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

		// Now add a second button
		buttonRow = $"{CM.Glyphs.VLine} {btn1} {btn2} {CM.Glyphs.VLine}";
		dlg.AddButton (new Button (btn2Text));
		var first = false;
		RunIteration (ref runstate, ref first);
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Justify
		dlg = new Dialog (new Button (btn1Text)) { Title = title, Width = width, Height = 1, ButtonAlignment = Dialog.ButtonAlignments.Justify };
		// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
		dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
		runstate = Begin (dlg);
		buttonRow = $"{CM.Glyphs.VLine}         {btn1}{CM.Glyphs.VLine}";
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

		// Now add a second button
		buttonRow = $"{CM.Glyphs.VLine}{btn1}   {btn2}{CM.Glyphs.VLine}";
		dlg.AddButton (new Button (btn2Text));
		first = false;
		RunIteration (ref runstate, ref first);
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Right
		dlg = new Dialog (new Button (btn1Text)) { Title = title, Width = width, Height = 1, ButtonAlignment = Dialog.ButtonAlignments.Right };
		// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
		dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
		runstate = Begin (dlg);
		buttonRow = $"{CM.Glyphs.VLine}{new string (' ', width - btn1.Length - 2)}{btn1}{CM.Glyphs.VLine}";
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

		// Now add a second button
		buttonRow = $"{CM.Glyphs.VLine}  {btn1} {btn2}{CM.Glyphs.VLine}";
		dlg.AddButton (new Button (btn2Text));
		first = false;
		RunIteration (ref runstate, ref first);
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);

		// Left
		dlg = new Dialog (new Button (btn1Text)) { Title = title, Width = width, Height = 1, ButtonAlignment = Dialog.ButtonAlignments.Left };
		// Create with no top or bottom border to simplify testing button layout (no need to account for title etc..)
		dlg.Border.Thickness = new Thickness (1, 0, 1, 0);
		runstate = Begin (dlg);
		buttonRow = $"{CM.Glyphs.VLine}{btn1}{new string (' ', width - btn1.Length - 2)}{CM.Glyphs.VLine}";
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);

		// Now add a second button
		buttonRow = $"{CM.Glyphs.VLine}{btn1} {btn2}  {CM.Glyphs.VLine}";
		dlg.AddButton (new Button (btn2Text));
		first = false;
		RunIteration (ref runstate, ref first);
		TestHelpers.AssertDriverContentsWithFrameAre ($"{buttonRow}", _output);
		End (runstate);
	}

	[Fact]
	[AutoInitShutdown]
	public void FileDialog_FileSystemWatcher ()
	{
		for (var i = 0; i < 8; i++) {
			var fd = new FileDialog ();
			fd.Ready += (s, e) => RequestStop ();
			Run (fd);
		}
	}

	[Fact]
	[AutoInitShutdown]
	public void Dialog_Opened_From_Another_Dialog ()
	{
		((FakeDriver)Driver).SetBufferSize (30, 10);

		var btn1 = new Button ("press me 1");
		Button btn2 = null;
		Button btn3 = null;
		string expected = null;
		btn1.Clicked += (s, e) => {
			btn2 = new Button ("Show Sub");
			btn3 = new Button ("Close");
			btn3.Clicked += (s, e) => RequestStop ();
			btn2.Clicked += (s, e) => {
				// Don't test MessageBox in Dialog unit tests!
				var subBtn = new Button ("Ok") { IsDefault = true };
				var subDlg = new Dialog (subBtn) { Text = "ya", Width = 20, Height = 5 };
				subBtn.Clicked += (s, e) => RequestStop (subDlg);
				Run (subDlg);
			};
			var dlg = new Dialog (btn2, btn3);

			Run (dlg);
		};
		var btn = $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} Ok {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";

		var iterations = -1;
		Iteration += (s, a) => {
			iterations++;
			if (iterations == 0) {
				Assert.True (btn1.NewKeyDownEvent (new Key (KeyCode.Space)));
			} else if (iterations == 1) {
				expected = @$"
  ┌───────────────────────┐
  │                       │
  │                       │
  │                       │
  │                       │
  │                       │
  │{CM.Glyphs.LeftBracket} Show Sub {CM.Glyphs.RightBracket} {CM.Glyphs.LeftBracket} Close {CM.Glyphs.RightBracket} │
  └───────────────────────┘";
				TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

				Assert.True (btn2.NewKeyDownEvent (new Key (KeyCode.Space)));
			} else if (iterations == 2) {
				TestHelpers.AssertDriverContentsWithFrameAre (@$"
  ┌───────────────────────┐
  │  ┌──────────────────┐ │
  │  │ya                │ │
  │  │                  │ │
  │  │     {btn}     │ │
  │  └──────────────────┘ │
  │{CM.Glyphs.LeftBracket} Show Sub {CM.Glyphs.RightBracket} {CM.Glyphs.LeftBracket} Close {CM.Glyphs.RightBracket} │
  └───────────────────────┘", _output);

				Assert.True (Current.NewKeyDownEvent (new Key (KeyCode.Enter)));
			} else if (iterations == 3) {
				TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

				Assert.True (btn3.NewKeyDownEvent (new Key (KeyCode.Space)));
			} else if (iterations == 4) {
				TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

				RequestStop ();
			}
		};

		Run ();
		Shutdown ();

		Assert.Equal (4, iterations);
	}

	[Fact]
	[AutoInitShutdown]
	public void Dialog_In_Window_With_Size_One_Button_Aligns ()
	{
		((FakeDriver)Driver).SetBufferSize (20, 5);

		var win = new Window ();

		var iterations = 0;
		Iteration += (s, a) => {
			if (++iterations > 2) {
				RequestStop ();
			}
		};
		var btn = $"{CM.Glyphs.LeftBracket} Ok {CM.Glyphs.RightBracket}";

		win.Loaded += (s, a) => {
			var dlg = new Dialog (new Button ("Ok")) { Width = 18, Height = 3 };

			dlg.Loaded += (s, a) => {
				Refresh ();
				var expected = @$"
┌──────────────────┐
│┌────────────────┐│
││     {btn}     ││
│└────────────────┘│
└──────────────────┘";
				_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
			};

			Run (dlg);
		};
		Run (win);
	}

	[Theory]
	[AutoInitShutdown]
	[InlineData (5, @"
┌┌───────────────┐─┐
││               │ │
││     ⟦ Ok ⟧    │ │
│└───────────────┘ │
└──────────────────┘")]
	[InlineData (6, @"
┌┌───────────────┐─┐
││               │ │
││               │ │
││     ⟦ Ok ⟧    │ │
│└───────────────┘ │
└──────────────────┘")]
	[InlineData (7, @"
┌──────────────────┐
│┌───────────────┐ │
││               │ │
││               │ │
││     ⟦ Ok ⟧    │ │
│└───────────────┘ │
└──────────────────┘")]
	[InlineData (8, @"
┌──────────────────┐
│┌───────────────┐ │
││               │ │
││               │ │
││               │ │
││     ⟦ Ok ⟧    │ │
│└───────────────┘ │
└──────────────────┘")]
	[InlineData (9, @"
┌──────────────────┐
│┌───────────────┐ │
││               │ │
││               │ │
││               │ │
││               │ │
││     ⟦ Ok ⟧    │ │
│└───────────────┘ │
└──────────────────┘")]
	public void Dialog_In_Window_Without_Size_One_Button_Aligns (int height, string expected)
	{
		((FakeDriver)Driver).SetBufferSize (20, height);
		var win = new Window ();

		var iterations = -1;
		Iteration += (s, a) => {
			iterations++;
			if (iterations == 0) {
				var dlg = new Dialog (new Button ("Ok"));
				Run (dlg);
			} else if (iterations == 1) {
				// BUGBUG: This seems wrong; is it a bug in Dim.Percent(85)?? No
				_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
			} else {
				RequestStop ();
			}
		};

		Run (win);
	}

	// TODO: This is not really a Dialog test, but a ViewLayout test (Width = Dim.Fill (1) - Dim.Function (Btn_Width))
	// TODO: Move (and simplify)
	[Fact]
	[AutoInitShutdown]
	public void Dialog_In_Window_With_TextField_And_Button_AnchorEnd ()
	{
		((FakeDriver)Driver).SetBufferSize (20, 5);

		var win = new Window ();

		var iterations = 0;
		Iteration += (s, a) => {
			if (++iterations > 2) {
				RequestStop ();
			}
		};
		var b = $"{CM.Glyphs.LeftBracket} Ok {CM.Glyphs.RightBracket}";

		win.Loaded += (s, a) => {
			var dlg = new Dialog { Width = 18, Height = 3 };
			Assert.Equal (16, dlg.Bounds.Width);

			Button btn = null;
			btn = new Button ("Ok") {
				X = Pos.AnchorEnd () - Pos.Function (Btn_Width)
			};
			btn.SetRelativeLayout (dlg.Bounds);
			Assert.Equal (6, btn.Bounds.Width);
			Assert.Equal (10, btn.Frame.X); // dlg.Bounds.Width (16) - btn.Frame.Width (6) = 10
			Assert.Equal (0, btn.Frame.Y);
			Assert.Equal (6, btn.Frame.Width);
			Assert.Equal (1, btn.Frame.Height);
			int Btn_Width ()
			{
				return btn?.Bounds.Width ?? 0;
			}
			var tf = new TextField ("01234567890123456789") {
				// Dim.Fill (1) fills remaining space minus 1
				// Dim.Function (Btn_Width) is 6
				Width = Dim.Fill (1) - Dim.Function (Btn_Width)
			};
			tf.SetRelativeLayout (dlg.Bounds);
			Assert.Equal (9, tf.Bounds.Width); // dlg.Bounds.Width (16) - Dim.Fill (1) - Dim.Function (6) = 9
			Assert.Equal (0, tf.Frame.X);
			Assert.Equal (0, tf.Frame.Y);
			Assert.Equal (9, tf.Frame.Width);
			Assert.Equal (1, tf.Frame.Height);

			dlg.Loaded += (s, a) => {
				Refresh ();
				Assert.Equal (new Rect (10, 0, 6, 1), btn.Frame);
				Assert.Equal (new Rect (0, 0, 6, 1), btn.Bounds);

				var expected = @"
┌──────────────────┐
│┌────────────────┐│
││012345678 ⟦ Ok ⟧││
│└────────────────┘│
└──────────────────┘";

				_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

				dlg.SetNeedsLayout ();
				dlg.LayoutSubviews ();
				Refresh ();
				Assert.Equal (new Rect (10, 0, 6, 1), btn.Frame);
				Assert.Equal (new Rect (0, 0, 6, 1), btn.Bounds);
				expected = @"
┌──────────────────┐
│┌────────────────┐│
││012345678 ⟦ Ok ⟧││
│└────────────────┘│
└──────────────────┘";
				_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
			};
			dlg.Add (btn, tf);

			Run (dlg);
		};
		Run (win);
	}
}