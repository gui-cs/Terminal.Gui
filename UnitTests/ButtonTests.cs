using System;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.Views {
	public class ButtonTests {
		readonly ITestOutputHelper output;

		public ButtonTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact, AutoInitShutdown]
		public void Constructors_Defaults ()
		{
			var btn = new Button ();
			Assert.Equal (string.Empty, btn.Text);
			Application.Top.Add (btn);
			btn.Redraw (btn.Bounds);
			Assert.Equal ("[  ]", GetContents (btn.Bounds.Width));
			Assert.False (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (0, 0, 4, 1), btn.Frame);
			Assert.Equal (Key.Null, btn.HotKey);

			btn = new Button ("ARGS", true) { Text = "Test" };
			Assert.Equal ("Test", btn.Text);
			Application.Top.Add (btn);
			btn.Redraw (btn.Bounds);
			Assert.Equal ("[◦ Test ◦]", GetContents (btn.Bounds.Width));
			Assert.True (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (0, 0, 10, 1), btn.Frame);
			Assert.Equal (Key.T, btn.HotKey);

			btn = new Button (3, 4, "Test", true);
			Assert.Equal ("Test", btn.Text);
			Application.Top.Add (btn);
			btn.Redraw (btn.Bounds);
			Assert.Equal ("[◦ Test ◦]", GetContents (btn.Bounds.Width));
			Assert.True (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (3, 4, 10, 1), btn.Frame);
			Assert.Equal (Key.T, btn.HotKey);
		}

		private string GetContents (int width)
		{
			string output = "";
			for (int i = 0; i < width; i++) {
				output += (char)Application.Driver.Contents [0, i, 0];
			}
			return output;
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var clicked = false;
			Button btn = new Button ("Test");
			btn.Clicked += () => clicked = true;
			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			Assert.Equal (Key.T, btn.HotKey);
			Assert.False (btn.ProcessHotKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.False (clicked);
			Assert.True (btn.ProcessHotKey (new KeyEvent (Key.T | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (clicked);
			clicked = false;
			Assert.False (btn.IsDefault);
			Assert.False (btn.ProcessColdKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (clicked);
			btn.IsDefault = true;
			Assert.True (btn.ProcessColdKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessColdKey (new KeyEvent (Key.AltMask | Key.T, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Space, new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent ((Key)'t', new KeyModifiers ())));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Space | btn.HotKey, new KeyModifiers ())));
			Assert.True (clicked);
			btn.Text = "Te_st";
			clicked = false;
			Assert.True (btn.ProcessKey (new KeyEvent (Key.Space | btn.HotKey, new KeyModifiers ())));
			Assert.True (clicked);
		}

		[Fact]
		[AutoInitShutdown]
		public void ChangeHotKey ()
		{
			var clicked = false;
			Button btn = new Button ("Test");
			btn.Clicked += () => clicked = true;
			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			Assert.Equal (Key.T, btn.HotKey);
			Assert.False (btn.ProcessHotKey (new KeyEvent (Key.T, new KeyModifiers ())));
			Assert.False (clicked);
			Assert.True (btn.ProcessHotKey (new KeyEvent (Key.T | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (clicked);
			clicked = false;

			btn.HotKey = Key.E;
			Assert.True (btn.ProcessHotKey (new KeyEvent (Key.E | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (clicked);
		}


		/// <summary>
		/// This test demonstrates how to change the activation key for Button
		/// as described in the README.md keyboard handling section
		/// </summary>
		[Fact]
		[AutoInitShutdown]
		public void KeyBindingExample ()
		{
			int pressed = 0;
			var btn = new Button ("Press Me");
			btn.Clicked += () => pressed++;

			// The Button class supports the Accept command
			Assert.Contains (Command.Accept, btn.GetSupportedCommands ());

			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			// default keybinding is Enter which results in keypress
			Application.Driver.SendKeys ('\n', ConsoleKey.Enter, false, false, false);
			Assert.Equal (1, pressed);

			// remove the default keybinding (Enter)
			btn.ClearKeybinding (Command.Accept);

			// After clearing the default keystroke the Enter button no longer does anything for the Button
			Application.Driver.SendKeys ('\n', ConsoleKey.Enter, false, false, false);
			Assert.Equal (1, pressed);

			// Set a new binding of b for the click (Accept) event
			btn.AddKeyBinding (Key.b, Command.Accept);

			// now pressing B should call the button click event
			Application.Driver.SendKeys ('b', ConsoleKey.B, false, false, false);
			Assert.Equal (2, pressed);
		}

		[Fact]
		public void TestAssignTextToButton ()
		{
			View b = new Button () { Text = "heya" };
			Assert.Equal ("heya", b.Text);
			Assert.True (b.TextFormatter.Text.Contains ("heya"));
			b.Text = "heyb";
			Assert.Equal ("heyb", b.Text);
			Assert.True (b.TextFormatter.Text.Contains ("heyb"));

			// with cast
			Assert.Equal ("heyb", ((Button)b).Text);
		}

		[Fact]
		public void Setting_Empty_Text_Sets_HoKey_To_KeyNull ()
		{
			var btn = new Button ("Test");
			Assert.Equal ("Test", btn.Text);
			Assert.Equal (Key.T, btn.HotKey);

			btn.Text = string.Empty;
			Assert.Equal ("", btn.Text);
			Assert.Equal (Key.Null, btn.HotKey);

			btn.Text = "Te_st";
			Assert.Equal ("Te_st", btn.Text);
			Assert.Equal (Key.S, btn.HotKey);
		}

		[Fact, AutoInitShutdown]
		public void Update_Only_On_Or_After_Initialize ()
		{
			var btn = new Button ("Say Hello 你") {
				X = Pos.Center (),
				Y = Pos.Center ()
			};
			var win = new Window ("Test Demo 你") {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.False (btn.IsInitialized);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.True (btn.IsInitialized);
			Assert.Equal ("Say Hello 你", btn.Text);
			Assert.Equal ("[ Say Hello 你 ]", btn.TextFormatter.Text);
			Assert.Equal (new Rect (0, 0, 16, 1), btn.Bounds);

			var expected = @"
┌ Test Demo 你 ──────────────┐
│                            │
│      [ Say Hello 你 ]      │
│                            │
└────────────────────────────┘
";

			var pos = GraphViewTests.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void Update_Parameterless_Only_On_Or_After_Initialize ()
		{
			var btn = new Button () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = "Say Hello 你"
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.False (btn.IsInitialized);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.True (btn.IsInitialized);
			Assert.Equal ("Say Hello 你", btn.Text);
			Assert.Equal ("[ Say Hello 你 ]", btn.TextFormatter.Text);
			Assert.Equal (new Rect (0, 0, 16, 1), btn.Bounds);

			var expected = @"
┌ Test Demo 你 ──────────────┐
│                            │
│      [ Say Hello 你 ]      │
│                            │
└────────────────────────────┘
";

			var pos = GraphViewTests.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_Center ()
		{
			var btn = new Button () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = "Say Hello 你",
				AutoSize = true
			};

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.True (btn.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @"
┌ Test Demo 你 ──────────────┐
│                            │
│      [ Say Hello 你 ]      │
│                            │
└────────────────────────────┘
";

			GraphViewTests.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (btn.AutoSize);
			btn.Text = "Say Hello 你 changed";
			Assert.True (btn.AutoSize);
			Application.Refresh ();
			expected = @"
┌ Test Demo 你 ──────────────┐
│                            │
│  [ Say Hello 你 changed ]  │
│                            │
└────────────────────────────┘
";

			GraphViewTests.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_AnchorEnd ()
		{
			var btn = new Button () {
				Y = Pos.Center (),
				Text = "Say Hello 你",
				AutoSize = true
			};
			btn.X = Pos.AnchorEnd () - Pos.Function (() => TextFormatter.GetTextWidth  (btn.TextFormatter.Text));

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.True (btn.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @"
┌ Test Demo 你 ──────────────┐
│                            │
│            [ Say Hello 你 ]│
│                            │
└────────────────────────────┘
";

			GraphViewTests.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (btn.AutoSize);
			btn.Text = "Say Hello 你 changed";
			Assert.True (btn.AutoSize);
			Application.Refresh ();
			expected = @"
┌ Test Demo 你 ──────────────┐
│                            │
│    [ Say Hello 你 changed ]│
│                            │
└────────────────────────────┘
";

			GraphViewTests.AssertDriverContentsWithFrameAre (expected, output);
		}
	}
}
