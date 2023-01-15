using System;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests {
	public class SplitContainerTests {

		readonly ITestOutputHelper output;

		public SplitContainerTests (ITestOutputHelper output)
		{
			this.output = output;
		}


		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical ()
		{
			var splitContainer = Get11By3SplitContainer (out var line);
			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"
11111│22222
11111│22222
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect if it is not focused
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical_WithBorder ()
		{
			var splitContainer = Get11By3SplitContainer (out var line, true);
			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"
┌────┬────┐
│1111│2222│
└────┴────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect if it is not focused
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical_Focused ()
		{
			var splitContainer = Get11By3SplitContainer (out var line);
			SetInputFocusLine (splitContainer);

			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"
11111│22222
11111◊22222
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now while focused move the splitter 1 unit right
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
111111│2222
111111◊2222
      │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);


			// and 2 to the left
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
1111│222222
1111◊222222
    │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical_Focused_WithBorder ()
		{
			var splitContainer = Get11By3SplitContainer (out var line, true);
			SetInputFocusLine (splitContainer);

			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"
┌────┬────┐
│1111◊2222│
└────┴────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now while focused move the splitter 1 unit right
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
┌─────┬───┐
│11111◊222│
└─────┴───┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);


			// and 2 to the left
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
┌───┬─────┐
│111◊22222│
└───┴─────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical_Focused_50PercentSplit ()
		{
			var splitContainer = Get11By3SplitContainer (out var line);
			SetInputFocusLine (splitContainer);
			splitContainer.SplitterDistance = Pos.Percent (50);
			Assert.IsType<Pos.PosFactor> (splitContainer.SplitterDistance);
			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"
11111│22222
11111◊22222
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now while focused move the splitter 1 unit right
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
111111│2222
111111◊2222
      │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Even when moving the splitter location it should stay a Percentage based one
			Assert.IsType<Pos.PosFactor> (splitContainer.SplitterDistance);


			// and 2 to the left
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
1111│222222
1111◊222222
    │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
			// Even when moving the splitter location it should stay a Percentage based one
			Assert.IsType<Pos.PosFactor> (splitContainer.SplitterDistance);
		}

		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Horizontal ()
		{
			var splitContainer = Get11By3SplitContainer (out var line);
			splitContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"    
11111111111
───────────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect if it is not focused
			line.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical_Panel1MinSize_Absolute ()
		{
			var splitContainer = Get11By3SplitContainer (out var line);
			SetInputFocusLine (splitContainer);
			splitContainer.Panel1MinSize = 6;

			// distance is too small (below 6)
			splitContainer.SplitterDistance = 2;

			// Should bound the value to the minimum distance
			Assert.Equal (6, splitContainer.SplitterDistance);

			splitContainer.Redraw (splitContainer.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
111111│2222
111111◊2222
      │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect because it
			// would take us below the minimum splitter size
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter right if we want
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
1111111│222
1111111◊222
       │     ";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical_Panel1MinSize_Absolute_WithBorder ()
		{
			var splitContainer = Get11By3SplitContainer (out var line,true);
			SetInputFocusLine (splitContainer);
			splitContainer.Panel1MinSize = 5;

			// distance is too small (below 5)
			splitContainer.SplitterDistance = 2;

			// Should bound the value to the minimum distance
			Assert.Equal (6, splitContainer.SplitterDistance);

			splitContainer.Redraw (splitContainer.Bounds);

			// so should ignore the 2 distance and stick to 5
			string looksLike =
@"
┌─────┬───┐
│11111◊222│
└─────┴───┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect because it
			// would take us below the minimum splitter size
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter right if we want
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
┌──────┬──┐
│111111◊22│
└──────┴──┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical_Panel2MinSize_Absolute ()
		{
			var splitContainer = Get11By3SplitContainer (out var line);
			SetInputFocusLine (splitContainer);
			splitContainer.Panel2MinSize = 6;

			// distance leaves too little space for panel2 (less than 6 would remain)
			splitContainer.SplitterDistance = 8;

			// Should bound the value to the minimum distance
			Assert.Equal (4, splitContainer.SplitterDistance);

			splitContainer.Redraw (splitContainer.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
1111│222222
1111◊222222
    │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect because it
			// would take us below the minimum splitter size
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter left if we want
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
111│2222222
111◊2222222
   │     ";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical_Panel2MinSize_Absolute_WithBorder ()
		{
			var splitContainer = Get11By3SplitContainer (out var line, true);
			SetInputFocusLine (splitContainer);
			splitContainer.Panel2MinSize = 5;

			// distance leaves too little space for panel2 (less than 5 would remain)
			splitContainer.SplitterDistance = 8;

			// Should bound the value to the minimum distance
			Assert.Equal (4, splitContainer.SplitterDistance);

			splitContainer.Redraw (splitContainer.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
┌───┬─────┐
│111◊22222│
└───┴─────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect because it
			// would take us below the minimum splitter size
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter left if we want
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
┌──┬──────┐
│11◊222222│
└──┴──────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Horizontal_Focused ()
		{
			var splitContainer = Get11By3SplitContainer (out var line);

			splitContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			SetInputFocusLine (splitContainer);

			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"    
11111111111
─────◊─────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now move splitter line down
			line.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);
			looksLike =
@"    
11111111111
11111111111
─────◊─────";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// And 2 up
			line.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);
			looksLike =
@"    
─────◊─────
22222222222
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Horizontal_Panel1MinSize_Absolute ()
		{
			var splitContainer = Get11By3SplitContainer (out var line);

			splitContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			SetInputFocusLine (splitContainer);
			splitContainer.Panel1MinSize = 1;

			// 0 should not be allowed because it brings us below minimum size of Panel1
			splitContainer.SplitterDistance = 0;
			Assert.Equal ((Pos)1, splitContainer.SplitterDistance);

			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"    
11111111111
─────◊─────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now move splitter line down (allowed
			line.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);
			looksLike =
@"    
11111111111
11111111111
─────◊─────";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// And up 2 (only 1 is allowed because of minimum size of 1 on panel1)
			line.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);
			looksLike =
@"    
11111111111
─────◊─────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestSplitContainer_CannotSetSplitterPosToFuncEtc ()
		{
			var splitContainer = Get11By3SplitContainer ();

			var ex = Assert.Throws<ArgumentException> (() => splitContainer.SplitterDistance = Pos.Right (splitContainer));
			Assert.Equal ("Only Percent and Absolute values are supported for SplitterDistance property.  Passed value was PosCombine", ex.Message);


			ex = Assert.Throws<ArgumentException> (() => splitContainer.SplitterDistance = Pos.Function (() => 1));
			Assert.Equal ("Only Percent and Absolute values are supported for SplitterDistance property.  Passed value was PosFunc", ex.Message);

			// Also not allowed because this results in a PosCombine
			ex = Assert.Throws<ArgumentException> (() => splitContainer.SplitterDistance = Pos.Percent (50) - 1);
			Assert.Equal ("Only Percent and Absolute values are supported for SplitterDistance property.  Passed value was PosCombine", ex.Message);
		}

		[Fact,AutoInitShutdown]
		public void TestNestedContainer2LeftAnd1Right_RendersNicely()
		{
			var splitContainer = GetNestedContainer2Left1Right (false);
			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"    
1111111111│22222222
1111111111│22222222
          │
          │
          │
──────────┤
          │
          │
          │
          │";
			TestHelpers.AssertDriverContentsAre (looksLike, output);


		}


		/// <summary>
		/// Creates a vertical orientation root container with left pane split into
		/// two (with horizontal splitter line).
		/// </summary>
		/// <param name="withBorder"></param>
		/// <returns></returns>
		private SplitContainer GetNestedContainer2Left1Right(bool withBorder)
		{
			var container = GetSplitContainer (20, 10,withBorder);
			Assert.True (container.TrySplitPanel1 (out var newContainer));
			
			newContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			newContainer.ColorScheme = new ColorScheme ();
			container.ColorScheme = new ColorScheme ();

			container.LayoutSubviews ();
			return container;
		}

		private LineView GetLine (SplitContainer splitContainer)
		{
			return splitContainer.Subviews.OfType<LineView> ().Single ();
		}

		private void SetInputFocusLine (SplitContainer splitContainer)
		{
			var line = GetLine (splitContainer);
			line.SetFocus ();
			Assert.True (line.HasFocus);
		}

		private SplitContainer Get11By3SplitContainer(out LineView line, bool withBorder = false)
		{
			var split = Get11By3SplitContainer (withBorder);
			line = GetLine (split);
			
			return split;
		}
		private SplitContainer Get11By3SplitContainer (bool withBorder = false)
		{
			return GetSplitContainer (11, 3, withBorder);
		}
		private SplitContainer GetSplitContainer (int width, int height, bool withBorder = false)
		{
			var container = new SplitContainer () {
				Width = width,
				Height = height,
			};

			container.IntegratedBorder = withBorder ? BorderStyle.Single : BorderStyle.None;

			container.Panel1.Add (new Label (new string ('1', 100)));
			container.Panel1.Add (new Label (new string ('1', 100)) { Y = 1});
			container.Panel2.Add (new Label (new string ('2', 100)));
			container.Panel2.Add (new Label (new string ('2', 100)) { Y = 1});

			container.Panel1MinSize = 0;
			container.Panel2MinSize = 0;

			Application.Top.Add (container);
			container.ColorScheme = new ColorScheme ();
			container.LayoutSubviews ();
			container.BeginInit ();
			container.EndInit ();
			return container;
		}
	}
}
