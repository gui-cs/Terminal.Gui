using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.Views {
	public class ScrollViewTests {
		readonly ITestOutputHelper output;

		public ScrollViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constructors_Defaults ()
		{
			var sv = new ScrollView ();
			Assert.Equal (LayoutStyle.Computed, sv.LayoutStyle);
			Assert.True (sv.CanFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), sv.Frame);
			Assert.Equal (Rect.Empty, sv.Frame);
			Assert.Null (sv.X);
			Assert.Null (sv.Y);
			Assert.Null (sv.Width);
			Assert.Null (sv.Height);
			Assert.Equal (Point.Empty, sv.ContentOffset);
			Assert.Equal (Size.Empty, sv.ContentSize);
			Assert.True (sv.AutoHideScrollBars);
			Assert.True (sv.KeepContentAlwaysInViewport);

			sv = new ScrollView (new Rect (1, 2, 20, 10));
			Assert.Equal (LayoutStyle.Absolute, sv.LayoutStyle);
			Assert.True (sv.CanFocus);
			Assert.Equal (new Rect (1, 2, 20, 10), sv.Frame);
			Assert.Null (sv.X);
			Assert.Null (sv.Y);
			Assert.Null (sv.Width);
			Assert.Null (sv.Height);
			Assert.Equal (Point.Empty, sv.ContentOffset);
			Assert.Equal (Size.Empty, sv.ContentSize);
			Assert.True (sv.AutoHideScrollBars);
			Assert.True (sv.KeepContentAlwaysInViewport);
		}

		[Fact]
		public void Adding_Views ()
		{
			var sv = new ScrollView (new Rect (0, 0, 20, 10)) {
				ContentSize = new Size (30, 20)
			};
			sv.Add (new View () { Width = 10, Height = 5 },
				new View () { X = 12, Y = 7, Width = 10, Height = 5 });

			Assert.Equal (new Size (30, 20), sv.ContentSize);
			Assert.Equal (2, sv.Subviews [0].Subviews.Count);
		}

		[Fact]
		public void KeyBindings_Command ()
		{
			var sv = new ScrollView (new Rect (0, 0, 20, 10)) {
				ContentSize = new Size (40, 20)
			};
			sv.Add (new View () { Width = 20, Height = 5 },
				new View () { X = 22, Y = 7, Width = 10, Height = 5 });

			Assert.True (sv.KeepContentAlwaysInViewport);
			Assert.True (sv.AutoHideScrollBars);
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -1), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent ((Key)'v' | Key.AltMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.V | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (-1, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageUp | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-20, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-20, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-20, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);

			sv.KeepContentAlwaysInViewport = false;
			Assert.False (sv.KeepContentAlwaysInViewport);
			Assert.True (sv.AutoHideScrollBars);
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -1), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageUp, new KeyModifiers ())));
			Assert.Equal (new Point (0, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -10), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent ((Key)'v' | Key.AltMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -9), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.V | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (-1, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageUp | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-20, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.PageDown | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.PageUp | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-19, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-19, 0), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.Home, new KeyModifiers ())));
			Assert.Equal (new Point (-19, 0), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (new Point (-19, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.End, new KeyModifiers ())));
			Assert.Equal (new Point (-19, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.Home | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (0, -19), sv.ContentOffset);
			Assert.True (sv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
			Assert.False (sv.ProcessKey (new KeyEvent (Key.End | Key.CtrlMask, new KeyModifiers ())));
			Assert.Equal (new Point (-39, -19), sv.ContentOffset);
		}

		[Fact, AutoInitShutdown]
		public void AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
		{
			var sv = new ScrollView {
				Width = 10,
				Height = 10
			};

			Application.Top.Add (sv);
			Application.Begin (Application.Top);

			Assert.True (sv.AutoHideScrollBars);
			Assert.False (sv.ShowHorizontalScrollIndicator);
			Assert.False (sv.ShowVerticalScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre ("", output);

			sv.AutoHideScrollBars = false;
			sv.ShowHorizontalScrollIndicator = true;
			sv.ShowVerticalScrollIndicator = true;
			sv.Redraw (sv.Bounds);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
         ▲
         ┬
         │
         │
         │
         │
         │
         ┴
         ▼
◄├─────┤► 
", output);
		}

		[Fact, AutoInitShutdown]
		public void ContentSize_AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
		{
			var sv = new ScrollView {
				Width = 10,
				Height = 10,
				ContentSize = new Size (50, 50)
			};

			Application.Top.Add (sv);
			Application.Begin (Application.Top);

			Assert.Equal (50, sv.ContentSize.Width);
			Assert.Equal (50, sv.ContentSize.Height);
			Assert.True (sv.AutoHideScrollBars);
			Assert.True (sv.ShowHorizontalScrollIndicator);
			Assert.True (sv.ShowVerticalScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
         ▲
         ┬
         ┴
         ░
         ░
         ░
         ░
         ░
         ▼
◄├┤░░░░░► 
", output);
		}

		[Fact, AutoInitShutdown]
		public void ContentOffset_ContentSize_AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
		{
			var sv = new ScrollView {
				Width = 10,
				Height = 10,
				ContentSize = new Size (50, 50),
				ContentOffset = new Point (25, 25)
			};

			Application.Top.Add (sv);
			Application.Begin (Application.Top);

			Assert.Equal (-25, sv.ContentOffset.X);
			Assert.Equal (-25, sv.ContentOffset.Y);
			Assert.Equal (50, sv.ContentSize.Width);
			Assert.Equal (50, sv.ContentSize.Height);
			Assert.True (sv.AutoHideScrollBars);
			Assert.True (sv.ShowHorizontalScrollIndicator);
			Assert.True (sv.ShowVerticalScrollIndicator);
			TestHelpers.AssertDriverContentsWithFrameAre (@"
         ▲
         ░
         ░
         ░
         ┬
         │
         ┴
         ░
         ▼
◄░░░├─┤░► 
", output);
		}
	}
}
