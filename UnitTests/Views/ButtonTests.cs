﻿using System;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
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
			var rs = Application.Begin (Application.Top);

			Assert.Equal ($"{CM.Glyphs.LeftBracket}  {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
			Assert.False (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier.Value);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (0, 0, 4, 1), btn.Bounds);
			Assert.Equal (new Rect (0, 0, 4, 1), btn.Frame);

			Assert.Equal (string.Empty, btn.Title);
			Assert.Equal (Key.Null, btn.HotKey);

			var expected = @$"
{CM.Glyphs.LeftBracket}  {CM.Glyphs.RightBracket}
";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Application.End (rs);

			btn = new Button ("ARGS", true) { Text = "Test" };
			Assert.Equal ("Test", btn.Text);
			Application.Top.Add (btn);
			rs = Application.Begin (Application.Top);

			Assert.Equal ($"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} Test {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
			Assert.True (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier.Value);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (0, 0, 10, 1), btn.Bounds);
			Assert.Equal (new Rect (0, 0, 10, 1), btn.Frame);
			Assert.Equal (Key.T, btn.HotKey);
			Application.End (rs);

			btn = new Button (3, 4, "Test", true);
			Assert.Equal ("Test", btn.Text);
			Application.Top.Add (btn);
			rs = Application.Begin (Application.Top);

			Assert.Equal ($"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} Test {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
			Assert.True (btn.IsDefault);
			Assert.Equal (TextAlignment.Centered, btn.TextAlignment);
			Assert.Equal ('_', btn.HotKeySpecifier.Value);
			Assert.True (btn.CanFocus);
			Assert.Equal (new Rect (0, 0, 10, 1), btn.Bounds);
			Assert.Equal (new Rect (3, 4, 10, 1), btn.Frame);
			Assert.Equal (Key.T, btn.HotKey);

			Application.End (rs);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var clicked = false;
			Button btn = new Button ("_Test");
			btn.Clicked += (s, e) => clicked = true;
			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			Assert.Equal (Key.T, btn.HotKey);
			Assert.True (btn.ProcessKeyDown (new (Key.T)));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKeyDown (new (Key.T | Key.AltMask)));
			Assert.True (clicked);
			clicked = false;
			Assert.False (btn.IsDefault);
			Assert.False (btn.ProcessKeyDown (new (Key.Enter)));
			Assert.False (clicked);
			btn.IsDefault = true;
			Assert.False (btn.ProcessKeyDown (new (Key.Enter)));
			Assert.True (Application.Top.ProcessKeyDown (new (Key.Enter)));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKeyDown (new (Key.AltMask | Key.T)));
			Assert.True (clicked);
			clicked = false;
			Assert.True (Application.Top.ProcessKeyDown (new (Key.Enter)));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKeyDown (new (Key.Space)));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKeyDown (new ((Key)'T')));
			Assert.True (clicked);
			clicked = false;
			Assert.True (btn.ProcessKeyDown (new (btn.HotKey)));
			Assert.True (clicked);
			btn.Text = "Te_st";
			clicked = false;
			Assert.True (btn.ProcessKeyDown (new (btn.HotKey)));
			Assert.True (clicked);
		}

		[Fact]
		[AutoInitShutdown]
		public void HotKeyChange_Works ()
		{
			var clicked = false;
			Button btn = new Button ("Test");
			btn.Clicked += (s, e) => clicked = true;
			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			Assert.Equal (Key.T, btn.HotKey);
			Assert.True (btn.ProcessKeyDown (new (Key.T)));
			Assert.True (clicked);

			clicked = false;
			Assert.True (btn.ProcessKeyDown (new (Key.T | Key.AltMask)));
			Assert.True (clicked);

			clicked = false;
			btn.HotKey = Key.E;
			Assert.True (btn.ProcessKeyDown (new (Key.E | Key.AltMask)));
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

			btn.Clicked += (s, e) => pressed++;

			// The Button class supports the Default and Accept command
			Assert.Contains (Command.Default, btn.GetSupportedCommands ());
			Assert.Contains (Command.Accept, btn.GetSupportedCommands ());

			Application.Top.Add (btn);
			Application.Begin (Application.Top);

			// default keybinding is Space which results in keypress
			Application.OnKeyDown (new ((Key)' '));
			Assert.Equal (1, pressed);

			// remove the default keybinding (Space)
			btn.KeyBindings.Clear (Command.Default, Command.Accept);

			// After clearing the default keystroke the Space button no longer does anything for the Button
			Application.OnKeyDown (new ((Key)' '));
			Assert.Equal (1, pressed);

			// Set a new binding of b for the click (Accept) event
			btn.KeyBindings.Add (Key.B, Command.Default, Command.Accept);

			// now pressing B should call the button click event
			Application.OnKeyDown (new (Key.B));
			Assert.Equal (2, pressed);

			// now pressing Shift-B should NOT call the button click event
			Application.OnKeyDown (new (Key.ShiftMask | Key.B));
			Assert.Equal (2, pressed);

			// now pressing Alt-B should NOT call the button click event
			Application.OnKeyDown (new (Key.AltMask | Key.B));
			Assert.Equal (2, pressed);

			// now pressing Shift-Alt-B should NOT call the button click event
			Application.OnKeyDown (new (Key.ShiftMask | Key.AltMask | Key.B));
			Assert.Equal (2, pressed);
		}

		[Fact]
		public void TestAssignTextToButton ()
		{
			View b = new Button () { Text = "heya" };
			Assert.Equal ("heya", b.Text);
			Assert.Contains ("heya", b.TextFormatter.Text);
			b.Text = "heyb";
			Assert.Equal ("heyb", b.Text);
			Assert.Contains ("heyb", b.TextFormatter.Text);

			// with cast
			Assert.Equal ("heyb", ((Button)b).Text);
		}

		[Fact]
		public void Setting_Empty_Text_Sets_HoKey_To_KeyNull ()
		{
			var super = new View ();
			var btn = new Button ("Test");
			super.Add (btn);
			super.BeginInit ();
			super.EndInit ();

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
			var win = new Window () {
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
			Assert.Equal ($"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
			Assert.Equal (new Rect (0, 0, 16, 1), btn.Bounds);
			var btnTxt = $"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}";
			var expected = @$"
┌────────────────────────────┐
│                            │
│      {btnTxt}      │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
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
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.False (btn.IsInitialized);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.True (btn.IsInitialized);
			Assert.Equal ("Say Hello 你", btn.Text);
			Assert.Equal ($"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}", btn.TextFormatter.Text);
			Assert.Equal (new Rect (0, 0, 16, 1), btn.Bounds);
			var btnTxt = $"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}";
			var expected = @$"
┌────────────────────────────┐
│                            │
│      {btnTxt}      │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_With_EmptyText ()
		{
			var btn = new Button () {
				X = Pos.Center (),
				Y = Pos.Center (),
				AutoSize = true
			};

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.True (btn.AutoSize);

			btn.Text = "Say Hello 你";

			Assert.True (btn.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @$"
┌────────────────────────────┐
│                            │
│      {CM.Glyphs.LeftBracket} Say Hello 你 {CM.Glyphs.RightBracket}      │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_Center ()
		{
			var btn = new Button () {
				X = Pos.Center (),
				Y = Pos.Center (),
				Text = "Say Hello 你"
			};

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.True (btn.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @$"
┌────────────────────────────┐
│                            │
│      {CM.Glyphs.LeftBracket} Say Hello 你 {CM.Glyphs.RightBracket}      │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (btn.AutoSize);
			btn.Text = "Say Hello 你 changed";
			Assert.True (btn.AutoSize);
			Application.Refresh ();
			expected = @$"
┌────────────────────────────┐
│                            │
│  {CM.Glyphs.LeftBracket} Say Hello 你 changed {CM.Glyphs.RightBracket}  │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_AnchorEnd ()
		{
			var btn = new Button () {
				Y = Pos.Center (),
				Text = "Say Hello 你",
				AutoSize = true
			};
			var btnTxt = $"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}";

			btn.X = Pos.AnchorEnd () - Pos.Function (() => btn.TextFormatter.Text.GetColumns ());

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			win.Add (btn);
			Application.Top.Add (win);

			Assert.True (btn.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @$"
┌────────────────────────────┐
│                            │
│            {btnTxt}│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (btn.AutoSize);
			btn.Text = "Say Hello 你 changed";
			btnTxt = $"{CM.Glyphs.LeftBracket} {btn.Text} {CM.Glyphs.RightBracket}";
			Assert.True (btn.AutoSize);
			Application.Refresh ();
			expected = @$"
┌────────────────────────────┐
│                            │
│    {btnTxt}│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_False_With_Fixed_Width ()
		{
			var tab = new View ();

			var lblWidth = 8;

			var label = new Label ("Find:") {
				Y = 1,
				Width = lblWidth,
				TextAlignment = TextAlignment.Right,
				AutoSize = false
			};
			tab.Add (label);

			var txtToFind = new TextField ("Testing buttons.") {
				X = Pos.Right (label) + 1,
				Y = Pos.Top (label),
				Width = 20
			};
			tab.Add (txtToFind);

			var btnFindNext = new Button ("Find _Next") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (label),
				Width = 20,
				Enabled = !string.IsNullOrEmpty (txtToFind.Text),
				TextAlignment = TextAlignment.Centered,
				IsDefault = true,
				AutoSize = false
			};
			tab.Add (btnFindNext);

			var btnFindPrevious = new Button ("Find _Previous") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindNext) + 1,
				Width = 20,
				Enabled = !string.IsNullOrEmpty (txtToFind.Text),
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			};
			tab.Add (btnFindPrevious);

			var btnCancel = new Button ("Cancel") {
				X = Pos.Right (txtToFind) + 1,
				Y = Pos.Top (btnFindPrevious) + 2,
				Width = 20,
				TextAlignment = TextAlignment.Centered,
				AutoSize = false
			};
			tab.Add (btnCancel);

			var ckbMatchCase = new CheckBox ("Match c_ase") {
				X = 0,
				Y = Pos.Top (txtToFind) + 2,
				Checked = true
			};
			tab.Add (ckbMatchCase);

			var ckbMatchWholeWord = new CheckBox ("Match _whole word") {
				X = 0,
				Y = Pos.Top (ckbMatchCase) + 1,
				Checked = false
			};
			tab.Add (ckbMatchWholeWord);

			var tabView = new TabView () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			tabView.AddTab (new Tab ("Find", tab), true);

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			tab.Width = label.Width + txtToFind.Width + btnFindNext.Width + 2;
			tab.Height = btnFindNext.Height + btnFindPrevious.Height + btnCancel.Height + 4;

			win.Add (tabView);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (54, 11);

			Assert.Equal (new Rect (0, 0, 54, 11), win.Frame);
			Assert.Equal (new Rect (0, 0, 52, 9), tabView.Frame);
			Assert.Equal (new Rect (0, 0, 50, 7), tab.Frame);
			Assert.Equal (new Rect (0, 1, 8, 1), label.Frame);
			Assert.Equal (new Rect (9, 1, 20, 1), txtToFind.Frame);

			Assert.Equal (0, txtToFind.ScrollOffset);
			Assert.Equal (16, txtToFind.CursorPosition);

			Assert.Equal (new Rect (30, 1, 20, 1), btnFindNext.Frame);
			Assert.Equal (new Rect (30, 2, 20, 1), btnFindPrevious.Frame);
			Assert.Equal (new Rect (30, 4, 20, 1), btnCancel.Frame);
			Assert.Equal (new Rect (0, 3, 12, 1), ckbMatchCase.Frame);
			Assert.Equal (new Rect (0, 4, 18, 1), ckbMatchWholeWord.Frame);
			var btn1 = $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} Find Next {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";
			var btn2 = $"{CM.Glyphs.LeftBracket} Find Previous {CM.Glyphs.RightBracket}";
			var btn3 = $"{CM.Glyphs.LeftBracket} Cancel {CM.Glyphs.RightBracket}";
			var expected = @$"
┌────────────────────────────────────────────────────┐
│┌────┐                                              │
││Find│                                              │
││    └─────────────────────────────────────────────┐│
││                                                  ││
││   Find: Testing buttons.       {btn1}   ││
││                               {btn2}  ││
││{CM.Glyphs.Checked} Match case                                      ││
││{CM.Glyphs.UnChecked} Match whole word                 {btn3}     ││
│└──────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void Pos_Center_Layout_AutoSize_True ()
		{
			var button = new Button ("Process keys") {
				X = Pos.Center (),
				Y = Pos.Center (),
				IsDefault = true
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (button);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			Assert.True (button.AutoSize);
			Assert.Equal (new Rect (5, 1, 18, 1), button.Frame);
			var btn = $"{CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} Process keys {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}";

			var expected = @$"
┌────────────────────────────┐
│                            │
│     {btn}     │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void Pos_Center_Layout_AutoSize_False ()
		{
			var button = new Button ("Process keys") {
				X = Pos.Center (),
				Y = Pos.Center (),
				Width = 20,
				IsDefault = true,
				AutoSize = false
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (button);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			Assert.False (button.AutoSize);
			Assert.Equal (new Rect (4, 1, 20, 1), button.Frame);
			var expected = @$"
┌────────────────────────────┐
│                            │
│     {CM.Glyphs.LeftBracket}{CM.Glyphs.LeftDefaultIndicator} Process keys {CM.Glyphs.RightDefaultIndicator}{CM.Glyphs.RightBracket}     │
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}
		[Fact, AutoInitShutdown]
		public void Button_HotKeyChanged_EventFires ()
		{
			var btn = new Button ("Yar");

			object sender = null;
			KeyChangedEventArgs args = null;

			btn.HotKeyChanged += (s, e) => {
				sender = s;
				args = e;

			};

			btn.HotKey = Key.R;
			Assert.Same (btn, sender);
			Assert.Equal (Key.Y, args.OldKey);
			Assert.Equal (Key.R, args.NewKey);

		}
		[Fact, AutoInitShutdown]
		public void Button_HotKeyChanged_EventFires_WithNone ()
		{
			var btn = new Button ();

			object sender = null;
			KeyChangedEventArgs args = null;

			btn.HotKeyChanged += (s, e) => {
				sender = s;
				args = e;

			};

			btn.HotKey = Key.R;
			Assert.Same (btn, sender);
			Assert.Equal (Key.Null, args.OldKey);
			Assert.Equal (Key.R, args.NewKey);

		}
	}
}
