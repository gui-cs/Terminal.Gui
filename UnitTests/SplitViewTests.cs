using System;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests {
	public class SplitViewTests {

		readonly ITestOutputHelper output;

		public SplitViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}


		[Fact, AutoInitShutdown]
		public void TestSplitView_Vertical ()
		{
			var splitContainer = Get11By3SplitView (out var line);
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
		public void TestSplitView_Vertical_WithBorder ()
		{
			var splitContainer = Get11By3SplitView (out var line, true);
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
		public void TestSplitView_Vertical_Focused ()
		{
			var splitContainer = Get11By3SplitView (out var line);
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
		public void TestSplitView_Vertical_Focused_WithBorder ()
		{
			var splitContainer = Get11By3SplitView (out var line, true);
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
		public void TestSplitView_Vertical_Focused_50PercentSplit ()
		{
			var splitContainer = Get11By3SplitView (out var line);
			SetInputFocusLine (splitContainer);
			splitContainer.SetSplitterPos(0,Pos.Percent (50));
			Assert.IsType<Pos.PosFactor> (splitContainer.SplitterDistances.ElementAt(0));
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
			Assert.IsType<Pos.PosFactor> (splitContainer.SplitterDistances.ElementAt(0));


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
			Assert.IsType<Pos.PosFactor> (splitContainer.SplitterDistances.ElementAt (0));
		}

		[Fact, AutoInitShutdown]
		public void TestSplitView_Horizontal ()
		{
			var splitContainer = Get11By3SplitView (out var line);
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
		public void TestSplitView_Vertical_View1MinSize_Absolute ()
		{
			var splitContainer = Get11By3SplitView (out var line);
			SetInputFocusLine (splitContainer);
			splitContainer.Tiles.ElementAt(0).MinSize = 6;

			// distance is too small (below 6)
			splitContainer.SetSplitterPos(0, 2);

			// Should bound the value to the minimum distance
			Assert.Equal (6, splitContainer.SplitterDistances.ElementAt (0));

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
		public void TestSplitView_Vertical_View1MinSize_Absolute_WithBorder ()
		{
			var splitContainer = Get11By3SplitView (out var line,true);
			SetInputFocusLine (splitContainer);
			splitContainer.Tiles.ElementAt(0).MinSize = 5;

			// distance is too small (below 5)
			splitContainer.SetSplitterPos(0,2);

			// Should bound the value to the minimum distance
			Assert.Equal (6, splitContainer.SplitterDistances.ElementAt(0));

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
		public void TestSplitView_Vertical_View2MinSize_Absolute ()
		{
			var splitContainer = Get11By3SplitView (out var line);
			SetInputFocusLine (splitContainer);
			splitContainer.Tiles.ElementAt(1).MinSize = 6;

			// distance leaves too little space for view2 (less than 6 would remain)
			splitContainer.SetSplitterPos(0,8);

			// Should bound the value to the minimum distance
			Assert.Equal (4, splitContainer.SplitterDistances.ElementAt(0));

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
		public void TestSplitView_Vertical_View2MinSize_Absolute_WithBorder ()
		{
			var splitContainer = Get11By3SplitView (out var line, true);
			SetInputFocusLine (splitContainer);
			splitContainer.Tiles.ElementAt(1).MinSize = 5;

			// distance leaves too little space for view2 (less than 5 would remain)
			splitContainer.SetSplitterPos(0,8);

			// Should bound the value to the minimum distance
			Assert.Equal (4, splitContainer.SplitterDistances.ElementAt(0));

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
		public void TestSplitView_InsertPanelAtStart ()
		{
			var splitContainer = Get11By3SplitView (out var line, true);
			SetInputFocusLine (splitContainer);

			splitContainer.InsertTile (0);

			splitContainer.Redraw (splitContainer.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
┌──┬───┬──┐
│  │111│22│
└──┴───┴──┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestSplitView_Horizontal_Focused ()
		{
			var splitContainer = Get11By3SplitView (out var line);

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
		public void TestSplitView_Horizontal_View1MinSize_Absolute ()
		{
			var splitContainer = Get11By3SplitView (out var line);

			splitContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			SetInputFocusLine (splitContainer);
			splitContainer.Tiles.ElementAt(0).MinSize = 1;

			// 0 should not be allowed because it brings us below minimum size of View1
			splitContainer.SetSplitterPos(0,0);
			Assert.Equal ((Pos)1, splitContainer.SplitterDistances.ElementAt(0));

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

			// And up 2 (only 1 is allowed because of minimum size of 1 on view1)
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
		public void TestSplitView_CannotSetSplitterPosToFuncEtc ()
		{
			var splitContainer = Get11By3SplitView ();

			var ex = Assert.Throws<ArgumentException> (() => splitContainer.SetSplitterPos(0,Pos.Right (splitContainer)));
			Assert.Equal ("Only Percent and Absolute values are supported.  Passed value was PosCombine", ex.Message);


			ex = Assert.Throws<ArgumentException> (() => splitContainer.SetSplitterPos(0,Pos.Function (() => 1)));
			Assert.Equal ("Only Percent and Absolute values are supported.  Passed value was PosFunc", ex.Message);

			// Also not allowed because this results in a PosCombine
			ex = Assert.Throws<ArgumentException> (() => splitContainer.SetSplitterPos(0, Pos.Percent (50) - 1));
			Assert.Equal ("Only Percent and Absolute values are supported.  Passed value was PosCombine", ex.Message);
		}

		[Fact,AutoInitShutdown]
		public void TestNestedContainer2LeftAnd1Right_RendersNicely()
		{
			var splitContainer = GetNestedContainer2Left1Right (false);

			Assert.Equal (20,splitContainer.Frame.Width);
			Assert.Equal (10, splitContainer.Tiles.ElementAt(0).View.Frame.Width);
			Assert.Equal (9, splitContainer.Tiles.ElementAt (1).View.Frame.Width);

			Assert.IsType<SplitView> (splitContainer.Tiles.ElementAt (0).View);
			var left = (SplitView)splitContainer.Tiles.ElementAt (0).View;
			Assert.Same (left.SuperView, splitContainer);


			Assert.Equal(2, left.Tiles.ElementAt (0).View.Subviews.Count);
			Assert.IsType<Label> (left.Tiles.ElementAt (0).View.Subviews [0]);
			Assert.IsType<Label> (left.Tiles.ElementAt (0).View.Subviews [1]);
			var onesTop = (Label)left.Tiles.ElementAt (0).View.Subviews [0];
			var onesBottom = (Label)left.Tiles.ElementAt (0).View.Subviews [1];

			Assert.Same (left.Tiles.ElementAt (0).View, onesTop.SuperView);
			Assert.Same (left.Tiles.ElementAt (0).View, onesBottom.SuperView);

			Assert.Equal (10, onesTop.Frame.Width);
			Assert.Equal (10, onesBottom.Frame.Width);

			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"    
1111111111│222222222
1111111111│222222222
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
		private SplitView GetNestedContainer2Left1Right(bool withBorder)
		{
			var container = GetSplitView (20, 10,withBorder);
			Assert.True (container.TrySplitView (0, out var newContainer));
			
			newContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			newContainer.ColorScheme = new ColorScheme ();
			container.ColorScheme = new ColorScheme ();

			container.LayoutSubviews ();
			return container;
		}

		private LineView GetLine (SplitView splitContainer)
		{
			return splitContainer.Subviews.OfType<LineView> ().Single ();
		}

		private void SetInputFocusLine (SplitView splitContainer)
		{
			var line = GetLine (splitContainer);
			line.SetFocus ();
			Assert.True (line.HasFocus);
		}

		private SplitView Get11By3SplitView(out LineView line, bool withBorder = false)
		{
			var split = Get11By3SplitView (withBorder);
			line = GetLine (split);
			
			return split;
		}
		private SplitView Get11By3SplitView (bool withBorder = false)
		{
			return GetSplitView (11, 3, withBorder);
		}
		private SplitView GetSplitView (int width, int height, bool withBorder = false)
		{
			var container = new SplitView () {
				Width = width,
				Height = height,
			};

			container.IntegratedBorder = withBorder ? BorderStyle.Single : BorderStyle.None;

			container.Tiles.ElementAt(0).View.Add (new Label (new string ('1', 100)) { Width = Dim.Fill(), Height = 1, AutoSize = false});
			container.Tiles.ElementAt (0).View.Add (new Label (new string ('1', 100)) { Width = Dim.Fill (), Height = 1, AutoSize = false,Y = 1});
			container.Tiles.ElementAt (1).View.Add (new Label (new string ('2', 100)) { Width = Dim.Fill (), Height = 1, AutoSize = false });
			container.Tiles.ElementAt (1).View.Add (new Label (new string ('2', 100)) { Width = Dim.Fill (), Height = 1, AutoSize = false,Y = 1});

			container.Tiles.ElementAt (0).MinSize = 0;
			container.Tiles.ElementAt (1).MinSize = 0;

			Application.Top.Add (container);
			container.ColorScheme = new ColorScheme ();
			container.LayoutSubviews ();
			container.BeginInit ();
			container.EndInit ();
			return container;
		}
	}
}
