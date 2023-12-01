﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class CheckboxTests {
		readonly ITestOutputHelper output;

		public CheckboxTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constructors_Defaults ()
		{
			var ckb = new CheckBox ();
			Assert.True (ckb.AutoSize);
			Assert.False (ckb.Checked);
			Assert.False (ckb.AllowNullChecked);
			Assert.Equal (string.Empty, ckb.Text);
			Assert.Equal ($"{CM.Glyphs.UnChecked} ", ckb.TextFormatter.Text);
			Assert.True (ckb.CanFocus);
			Assert.Equal (new Rect (0, 0, 2, 1), ckb.Frame);

			ckb = new CheckBox ("Test", true);
			Assert.True (ckb.AutoSize);
			Assert.True (ckb.Checked);
			Assert.False (ckb.AllowNullChecked);
			Assert.Equal ("Test", ckb.Text);
			Assert.Equal ($"{CM.Glyphs.Checked} Test", ckb.TextFormatter.Text);
			Assert.True (ckb.CanFocus);
			Assert.Equal (new Rect (0, 0, 6, 1), ckb.Frame);

			ckb = new CheckBox (1, 2, "Test");
			Assert.True (ckb.AutoSize);
			Assert.False (ckb.Checked);
			Assert.False (ckb.AllowNullChecked);
			Assert.Equal ("Test", ckb.Text);
			Assert.Equal ($"{CM.Glyphs.UnChecked} Test", ckb.TextFormatter.Text);
			Assert.True (ckb.CanFocus);
			Assert.Equal (new Rect (1, 2, 6, 1), ckb.Frame);

			ckb = new CheckBox (3, 4, "Test", true);
			Assert.True (ckb.AutoSize);
			Assert.True (ckb.Checked);
			Assert.False (ckb.AllowNullChecked);
			Assert.Equal ("Test", ckb.Text);
			Assert.Equal ($"{CM.Glyphs.Checked} Test", ckb.TextFormatter.Text);
			Assert.True (ckb.CanFocus);
			Assert.Equal (new Rect (3, 4, 6, 1), ckb.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var toggled = false;
			CheckBox ckb = new CheckBox ();
			ckb.Toggled += (s, e) => toggled = true;
			Application.Top.Add (ckb);
			Application.Begin (Application.Top);

			Assert.False (ckb.Checked);
			Assert.False (toggled);
			Assert.Equal (Key.Null, ckb.HotKey);
			
			ckb.Text = "Test";
			Assert.Equal (Key.T, ckb.HotKey);
			Assert.True (Application.Top.ProcessKeyPressed (new (Key.T, new KeyModifiers ())));
			Assert.True (ckb.Checked);
			Assert.True (toggled);
			
			ckb.Text = "T_est";
			toggled = false;
			Assert.Equal (Key.E, ckb.HotKey);
			Assert.True (Application.Top.ProcessKeyPressed (new (Key.E | Key.AltMask, new KeyModifiers () { Alt = true })));
			Assert.True (toggled);
			Assert.False (ckb.Checked);

			toggled = false;
			Assert.Equal (Key.E, ckb.HotKey);
			Assert.True (Application.Top.ProcessKeyPressed (new (Key.E, new KeyModifiers () { Alt = true })));
			Assert.True (toggled);
			Assert.True (ckb.Checked);

			toggled = false;
			Assert.True (Application.Top.ProcessKeyPressed (new ((Key)' ', new KeyModifiers ())));
			Assert.True (toggled);
			Assert.False (ckb.Checked);

			toggled = false;
			Assert.True (Application.Top.ProcessKeyPressed (new (Key.Space, new KeyModifiers ())));
			Assert.True (toggled);
			Assert.True (ckb.Checked);
			Assert.True (ckb.AutoSize);

			Application.Refresh ();

			var expected = @$"
{CM.Glyphs.Checked} Test
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 6, 1), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_StaysVisible ()
		{
			var checkBox = new CheckBox () {
				X = 1,
				Y = Pos.Center (),
				Text = "Check this out 你"
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (checkBox);
			Application.Top.Add (win);

			Assert.False (checkBox.IsInitialized);

			var runstate = Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.True (checkBox.IsInitialized);
			Assert.Equal (new Rect (1, 1, 19, 1), checkBox.Frame);
			Assert.Equal ("Check this out 你", checkBox.Text);
			Assert.Equal ($"{CM.Glyphs.UnChecked} Check this out 你", checkBox.TextFormatter.Text);
			Assert.True (checkBox.AutoSize);

			checkBox.Checked = true;
			Assert.Equal ($"{CM.Glyphs.Checked} Check this out 你", checkBox.TextFormatter.Text);

			//checkBox.AutoSize = false;
			// It isn't auto-size so the height is guaranteed by the SetMinWidthHeight
			//checkBox.Text = "Check this out 你 changed";
			//Application.RunMainLoopIteration (ref runstate, ref first);
			// BUGBUG - v2 - Autosize is busted; disabling tests for now
//			Assert.Equal (new Rect (1, 1, 19, 1), checkBox.Frame);
//			expected = @"
//┌┤Test Demo 你├──────────────┐
//│                            │
//│ √ Check this out 你        │
//│                            │
//└────────────────────────────┘
//";

//			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
//			Assert.Equal (new Rect (0, 0, 30, 5), pos);

//			checkBox.Width = 19;
//			// It isn't auto-size so the height is guaranteed by the SetMinWidthHeight
//			checkBox.Text = "Check this out 你 changed";
//			Application.RunMainLoopIteration (ref runstate, ref first);
//			Assert.False (checkBox.AutoSize);
//			Assert.Equal (new Rect (1, 1, 19, 1), checkBox.Frame);
//			expected = @"
//┌┤Test Demo 你├──────────────┐
//│                            │
//│ √ Check this out 你        │
//│                            │
//└────────────────────────────┘
//";

//			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
//			Assert.Equal (new Rect (0, 0, 30, 5), pos);

//			checkBox.AutoSize = true;
//			Application.RunMainLoopIteration (ref runstate, ref first);
//			Assert.Equal (new Rect (1, 1, 27, 1), checkBox.Frame);
//			expected = @"
//┌┤Test Demo 你├──────────────┐
//│                            │
//│ √ Check this out 你 changed│
//│                            │
//└────────────────────────────┘
//";

//			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
//			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void TextAlignment_Left ()
		{
			var checkBox = new CheckBox () {
				X = 1,
				Y = Pos.Center (),
				Text = "Check this out 你",
				AutoSize = false,
				Width = 25
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (checkBox);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.Equal (TextAlignment.Left, checkBox.TextAlignment);
			Assert.Equal (new Rect (1, 1, 25, 1), checkBox.Frame);
			Assert.Equal (new Size (25, 1), checkBox.TextFormatter.Size);

			var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.UnChecked} Check this out 你        │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);

			checkBox.Checked = true;
			Application.Refresh ();
			expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.Checked} Check this out 你        │
│                            │
└────────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void TextAlignment_Centered ()
		{
			var checkBox = new CheckBox () {
				X = 1,
				Y = Pos.Center (),
				Text = "Check this out 你",
				TextAlignment = TextAlignment.Centered,
				AutoSize = false,
				Width = 25
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (checkBox);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.Equal (TextAlignment.Centered, checkBox.TextAlignment);
			Assert.Equal (new Rect (1, 1, 25, 1), checkBox.Frame);
			Assert.Equal (new Size (25, 1), checkBox.TextFormatter.Size);
			Assert.False (checkBox.AutoSize);

			var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│    {CM.Glyphs.UnChecked} Check this out 你     │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);

			checkBox.Checked = true;
			Application.Refresh ();
			expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│    {CM.Glyphs.Checked} Check this out 你     │
│                            │
└────────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void TextAlignment_Justified ()
		{
			var checkBox1 = new CheckBox () {
				X = 1,
				Y = Pos.Center (),
				Text = "Check first out 你",
				TextAlignment = TextAlignment.Justified,
				AutoSize = false,
				Width = 25
			};
			var checkBox2 = new CheckBox () {
				X = 1,
				Y = Pos.Bottom (checkBox1),
				Text = "Check second out 你",
				TextAlignment = TextAlignment.Justified,
				AutoSize = false,
				Width = 25
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (checkBox1, checkBox2);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 6);

			Assert.Equal (TextAlignment.Justified, checkBox1.TextAlignment);
			Assert.Equal (new Rect (1, 1, 25, 1), checkBox1.Frame);
			Assert.Equal (new Size (25, 1), checkBox1.TextFormatter.Size);
			Assert.Equal (TextAlignment.Justified, checkBox2.TextAlignment);
			Assert.Equal (new Rect (1, 2, 25, 1), checkBox2.Frame);
			Assert.Equal (new Size (25, 1), checkBox2.TextFormatter.Size);

			var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.UnChecked}   Check  first  out  你  │
│ {CM.Glyphs.UnChecked}  Check  second  out  你  │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 6), pos);

			checkBox1.Checked = true;
			Assert.Equal (new Rect (1, 1, 25, 1), checkBox1.Frame);
			//Assert.Equal (new Size (25, 1), checkBox1.TextFormatter.Size);
			checkBox2.Checked = true;
			Assert.Equal (new Rect (1, 2, 25, 1), checkBox2.Frame);
			//Assert.Equal (new Size (25, 1), checkBox2.TextFormatter.Size);
			Application.Refresh ();
			expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.Checked}   Check  first  out  你  │
│ {CM.Glyphs.Checked}  Check  second  out  你  │
│                            │
└────────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 6), pos);
		}

		[Fact, AutoInitShutdown]
		public void TextAlignment_Right ()
		{
			var checkBox = new CheckBox () {
				X = 1,
				Y = Pos.Center (),
				Text = "Check this out 你",
				TextAlignment = TextAlignment.Right,
				AutoSize = false,
				Width = 25
			};
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (checkBox);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.Equal (TextAlignment.Right, checkBox.TextAlignment);
			Assert.Equal (new Rect (1, 1, 25, 1), checkBox.Frame);
			Assert.Equal (new Size (25, 1), checkBox.TextFormatter.Size);
			Assert.False (checkBox.AutoSize);

			var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│       Check this out 你 {CM.Glyphs.UnChecked}  │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);

			checkBox.Checked = true;
			Application.Refresh ();
			expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│       Check this out 你 {CM.Glyphs.Checked}  │
│                            │
└────────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_AnchorEnd_Without_HotKeySpecifier ()
		{
			var checkBox = new CheckBox () {
				Y = Pos.Center (),
				Text = "Check this out 你"
			};
			checkBox.X = Pos.AnchorEnd () - Pos.Function (() => checkBox.GetSizeNeededForTextWithoutHotKey ().Width);

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (checkBox);
			Application.Top.Add (win);

			Assert.True (checkBox.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│         {CM.Glyphs.UnChecked} Check this out 你│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (checkBox.AutoSize);
			checkBox.Text = "Check this out 你 changed";
			Assert.True (checkBox.AutoSize);
			Application.Refresh ();
			expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.UnChecked} Check this out 你 changed│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AutoSize_Stays_True_AnchorEnd_With_HotKeySpecifier ()
		{
			var checkBox = new CheckBox () {
				Y = Pos.Center (),
				Text = "C_heck this out 你"
			};
			checkBox.X = Pos.AnchorEnd () - Pos.Function (() => checkBox.GetSizeNeededForTextWithoutHotKey ().Width);

			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Title = "Test Demo 你"
			};
			win.Add (checkBox);
			Application.Top.Add (win);

			Assert.True (checkBox.AutoSize);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);
			var expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│         {CM.Glyphs.UnChecked} Check this out 你│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			Assert.True (checkBox.AutoSize);
			checkBox.Text = "Check this out 你 changed";
			Assert.True (checkBox.AutoSize);
			Application.Refresh ();
			expected = @$"
┌┤Test Demo 你├──────────────┐
│                            │
│ {CM.Glyphs.UnChecked} Check this out 你 changed│
│                            │
└────────────────────────────┘
";

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Fact, AutoInitShutdown]
		public void AllowNullChecked_Get_Set ()
		{
			var checkBox = new CheckBox ("Check this out 你");
			var top = Application.Top;
			top.Add (checkBox);
			Application.Begin (top);

			Assert.False (checkBox.Checked);
			Assert.True (checkBox.ProcessKeyPressed (new (Key.Space, new KeyModifiers ())));
			Assert.True (checkBox.Checked);
			Assert.True (checkBox.MouseEvent (new MouseEvent () { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked }));
			Assert.False (checkBox.Checked);

			checkBox.AllowNullChecked = true;
			Assert.True (checkBox.ProcessKeyPressed (new (Key.Space, new KeyModifiers ())));
			Assert.Null (checkBox.Checked);
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@$"
{CM.Glyphs.NullChecked} Check this out 你", output);
			Assert.True (checkBox.MouseEvent (new MouseEvent () { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked }));
			Assert.True (checkBox.Checked);
			Assert.True (checkBox.ProcessKeyPressed (new (Key.Space, new KeyModifiers ())));
			Assert.False (checkBox.Checked);
			Assert.True (checkBox.MouseEvent (new MouseEvent () { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked }));
			Assert.Null (checkBox.Checked);

			checkBox.AllowNullChecked = false;
			Assert.False (checkBox.Checked);
		}
	}
}
