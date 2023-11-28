﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests {
	public class RadioGroupTests {
		readonly ITestOutputHelper output;

		public RadioGroupTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constructors_Defaults ()
		{
			var rg = new RadioGroup ();
			Assert.True (rg.CanFocus);
			Assert.Empty (rg.RadioLabels);
			Assert.Null (rg.X);
			Assert.Null (rg.Y);
			Assert.Null (rg.Width);
			Assert.Null (rg.Height);
			Assert.Equal (Rect.Empty, rg.Frame);
			Assert.Equal (0, rg.SelectedItem);

			rg = new RadioGroup (new string [] { "Test" });
			Assert.True (rg.CanFocus);
			Assert.Single (rg.RadioLabels);
			Assert.Null (rg.X);
			Assert.Null (rg.Y);
			Assert.Null (rg.Width);
			Assert.Null (rg.Height);
			Assert.Equal (new Rect (0, 0, 0, 0), rg.Frame);
			Assert.Equal (0, rg.SelectedItem);

			rg = new RadioGroup (new Rect (1, 2, 20, 5), new string [] { "Test" });
			Assert.True (rg.CanFocus);
			Assert.Single (rg.RadioLabels);
			Assert.Equal (LayoutStyle.Absolute, rg.LayoutStyle);
			Assert.Null (rg.X);
			Assert.Null (rg.Y);
			Assert.Null (rg.Width);
			Assert.Null (rg.Height);
			Assert.Equal (new Rect (1, 2, 20, 5), rg.Frame);
			Assert.Equal (0, rg.SelectedItem);

			rg = new RadioGroup (1, 2, new string [] { "Test" });
			Assert.True (rg.CanFocus);
			Assert.Single (rg.RadioLabels);
			Assert.Equal (LayoutStyle.Absolute, rg.LayoutStyle);
			Assert.Null (rg.X);
			Assert.Null (rg.Y);
			Assert.Null (rg.Width);
			Assert.Null (rg.Height);
			Assert.Equal (new Rect (1, 2, 6, 1), rg.Frame);
			Assert.Equal (0, rg.SelectedItem);
		}

		[Fact]
		public void Initialize_SelectedItem_With_Minus_One ()
		{
			var rg = new RadioGroup (new string [] { "Test" }, -1);
			Assert.Equal (-1, rg.SelectedItem);
			Assert.True (rg.ProcessKeyPressed (new (Key.Space, new KeyModifiers ())));
			Assert.Equal (0, rg.SelectedItem);
		}

		[Fact, AutoInitShutdown]
		public void DisplayMode_Width_Height_Vertical_Horizontal_Space ()
		{
			var rg = new RadioGroup (new string [] { "Test", "New Test 你" });
			var win = new Window () {
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};
			win.Add (rg);
			Application.Top.Add (win);

			Application.Begin (Application.Top);
			((FakeDriver)Application.Driver).SetBufferSize (30, 5);

			Assert.Equal (DisplayModeLayout.Vertical, rg.DisplayMode);
			Assert.Equal (2, rg.RadioLabels.Length);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (13, rg.Frame.Width);
			Assert.Equal (2, rg.Frame.Height);
			var expected = @$"
┌────────────────────────────┐
│{CM.Glyphs.Selected} Test                      │
│{CM.Glyphs.UnSelected} New Test 你               │
│                            │
└────────────────────────────┘
";

			var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);

			rg.DisplayMode = DisplayModeLayout.Horizontal;
			Application.Refresh ();

			Assert.Equal (DisplayModeLayout.Horizontal, rg.DisplayMode);
			Assert.Equal (2, rg.HorizontalSpace);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (21, rg.Width);
			Assert.Equal (1, rg.Height);

			expected = @$"
┌────────────────────────────┐
│{CM.Glyphs.Selected} Test  {CM.Glyphs.UnSelected} New Test 你       │
│                            │
│                            │
└────────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);

			rg.HorizontalSpace = 4;
			Application.Refresh ();

			Assert.Equal (DisplayModeLayout.Horizontal, rg.DisplayMode);
			Assert.Equal (4, rg.HorizontalSpace);
			Assert.Equal (0, rg.X);
			Assert.Equal (0, rg.Y);
			Assert.Equal (23, rg.Width);
			Assert.Equal (1, rg.Height);
			expected = @$"
┌────────────────────────────┐
│{CM.Glyphs.Selected} Test    {CM.Glyphs.UnSelected} New Test 你     │
│                            │
│                            │
└────────────────────────────┘
";

			pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
			Assert.Equal (new Rect (0, 0, 30, 5), pos);
		}

		[Fact]
		public void SelectedItemChanged_Event ()
		{
			var previousSelectedItem = -1;
			var selectedItem = -1;
			var rg = new RadioGroup (new string [] { "Test", "New Test" });
			rg.SelectedItemChanged += (s,e) => {
				previousSelectedItem = e.PreviousSelectedItem;
				selectedItem = e.SelectedItem;
			};

			rg.SelectedItem = 1;
			Assert.Equal (0, previousSelectedItem);
			Assert.Equal (selectedItem, rg.SelectedItem);
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			var rg = new RadioGroup (new string [] { "Test", "New Test" });

			Assert.True (rg.ProcessKeyPressed (new (Key.CursorUp, new KeyModifiers ())));
			Assert.True (rg.ProcessKeyPressed (new (Key.CursorDown, new KeyModifiers ())));
			Assert.True (rg.ProcessKeyPressed (new (Key.Home, new KeyModifiers ())));
			Assert.True (rg.ProcessKeyPressed (new (Key.End, new KeyModifiers ())));
			Assert.True (rg.ProcessKeyPressed (new (Key.Space, new KeyModifiers ())));
			Assert.Equal (1, rg.SelectedItem);
		}

		[Fact]
		public void KeyBindings_HotKeys ()
		{
			var rg = new RadioGroup (new string [] { "Left", "Right", "Cen_tered", "Justified" });

			Assert.True (rg.ProcessKeyPressed (new (Key.T, new KeyModifiers ())));
			Assert.Equal (2, rg.SelectedItem);
			Assert.True (rg.ProcessKeyPressed (new (Key.L, new KeyModifiers ())));
			Assert.Equal (0, rg.SelectedItem);
			Assert.True (rg.ProcessKeyPressed (new (Key.J, new KeyModifiers ())));
			Assert.Equal (3, rg.SelectedItem);
			Assert.True (rg.ProcessKeyPressed (new (Key.R, new KeyModifiers ())));
			Assert.Equal (1, rg.SelectedItem);
		}
	}
}
