using System;
using System.Linq;
using Terminal.Gui;
using Terminal.Gui.Graphs;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests {
	public class TileViewTests {

		readonly ITestOutputHelper output;

		public TileViewTests (ITestOutputHelper output)
		{
			this.output = output;
		}


		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical ()
		{
			var tileView = Get11By3TileView (out var line);
			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
11111│22222
11111│22222
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect if it is not focused
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_WithBorder ()
		{
			var tileView = Get11By3TileView (out var line, true);
			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
┌────┬────┐
│1111│2222│
└────┴────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect if it is not focused
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_Focused ()
		{
			var tileView = Get11By3TileView (out var line);
			SetInputFocusLine (tileView);

			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
11111│22222
11111◊22222
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now while focused move the splitter 1 unit right
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
111111│2222
111111◊2222
      │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);


			// and 2 to the left
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
1111│222222
1111◊222222
    │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_Focused_WithBorder ()
		{
			var tileView = Get11By3TileView (out var line, true);
			SetInputFocusLine (tileView);

			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
┌────┬────┐
│1111◊2222│
└────┴────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now while focused move the splitter 1 unit right
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌─────┬───┐
│11111◊222│
└─────┴───┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);


			// and 2 to the left
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌───┬─────┐
│111◊22222│
└───┴─────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_Focused_50PercentSplit ()
		{
			var tileView = Get11By3TileView (out var line);
			SetInputFocusLine (tileView);
			tileView.SetSplitterPos (0, Pos.Percent (50));
			Assert.IsType<Pos.PosFactor> (tileView.SplitterDistances.ElementAt (0));
			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
11111│22222
11111◊22222
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now while focused move the splitter 1 unit right
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
111111│2222
111111◊2222
      │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Even when moving the splitter location it should stay a Percentage based one
			Assert.IsType<Pos.PosFactor> (tileView.SplitterDistances.ElementAt (0));


			// and 2 to the left
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
1111│222222
1111◊222222
    │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
			// Even when moving the splitter location it should stay a Percentage based one
			Assert.IsType<Pos.PosFactor> (tileView.SplitterDistances.ElementAt (0));
		}

		[Fact, AutoInitShutdown]
		public void TestTileView_Horizontal ()
		{
			var tileView = Get11By3TileView (out var line);
			tileView.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"    
11111111111
───────────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect if it is not focused
			line.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_View1MinSize_Absolute ()
		{
			var tileView = Get11By3TileView (out var line);
			SetInputFocusLine (tileView);
			tileView.Tiles.ElementAt (0).MinSize = 6;

			// distance is too small (below 6)
			Assert.False(tileView.SetSplitterPos (0, 2));

			// Should stay where it was originally at (50%)
			Assert.Equal (Pos.Percent(50), tileView.SplitterDistances.ElementAt (0));

			tileView.Redraw (tileView.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
11111│22222
11111◊22222
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect because it
			// would take us below the minimum splitter size
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter right if we want
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
111111│2222
111111◊2222
      │     ";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_View1MinSize_Absolute_WithBorder ()
		{
			var tileView = Get11By3TileView (out var line, true);
			SetInputFocusLine (tileView);
			tileView.Tiles.ElementAt (0).MinSize = 5;

			// distance is too small (below 5)
			Assert.False(tileView.SetSplitterPos (0, 2));

			// Should stay where it was originally at (50%)
			Assert.Equal (Pos.Percent(50), tileView.SplitterDistances.ElementAt (0));

			tileView.Redraw (tileView.Bounds);

			// so should ignore the 2 distance and stick to 5
			string looksLike =
@"
┌────┬────┐
│1111◊2222│
└────┴────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect because it
			// would take us below the minimum splitter size
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter right if we want
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌─────┬───┐
│11111◊222│
└─────┴───┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_View2MinSize_Absolute ()
		{
			var tileView = Get11By3TileView (out var line);
			SetInputFocusLine (tileView);
			tileView.Tiles.ElementAt (1).MinSize = 6;

			// distance leaves too little space for view2 (less than 6 would remain)
			Assert.False(tileView.SetSplitterPos (0, 8));

			//  Should stay where it was originally at (50%)
			Assert.Equal (Pos.Percent(50), tileView.SplitterDistances.ElementAt (0));

			tileView.Redraw (tileView.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
11111│22222
11111◊22222
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect because it
			// would take us below the minimum splitter size
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter left if we want
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
1111│222222
1111◊222222
    │    ";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_View2MinSize_Absolute_WithBorder ()
		{
			var tileView = Get11By3TileView (out var line, true);
			SetInputFocusLine (tileView);
			tileView.Tiles.ElementAt (1).MinSize = 5;

			// distance leaves too little space for view2 (less than 5 would remain)
			Assert.False(tileView.SetSplitterPos (0, 8));

			//  Should stay where it was originally at (50%)
			Assert.Equal (Pos.Percent(50), tileView.SplitterDistances.ElementAt (0));

			tileView.Redraw (tileView.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
┌────┬────┐
│1111◊2222│
└────┴────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect because it
			// would take us below the minimum splitter size
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter left if we want
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌───┬─────┐
│111◊22222│
└───┴─────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTileView_InsertPanelAtStart ()
		{
			var tileView = Get11By3TileView (out var line, true);
			SetInputFocusLine (tileView);

			tileView.InsertTile (0);

			tileView.Redraw (tileView.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
┌──┬───┬──┐
│  │111│22│
└──┴───┴──┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTileView_InsertPanelMiddle ()
		{
			var tileView = Get11By3TileView (out var line, true);
			SetInputFocusLine (tileView);

			tileView.InsertTile (1);

			tileView.Redraw (tileView.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
┌──┬───┬──┐
│11│   │22│
└──┴───┴──┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTileView_InsertPanelAtEnd ()
		{
			var tileView = Get11By3TileView (out var line, true);
			SetInputFocusLine (tileView);

			tileView.InsertTile (2);

			tileView.Redraw (tileView.Bounds);

			// so should ignore the 2 distance and stick to 6
			string looksLike =
@"
┌──┬───┬──┐
│11│222│  │
└──┴───┴──┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTileView_Horizontal_Focused ()
		{
			var tileView = Get11By3TileView (out var line);

			tileView.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			SetInputFocusLine (tileView);

			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"    
11111111111
─────◊─────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now move splitter line down
			line.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));

			tileView.Redraw (tileView.Bounds);
			looksLike =
@"    
11111111111
11111111111
─────◊─────";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// And 2 up
			line.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			tileView.Redraw (tileView.Bounds);
			looksLike =
@"    
─────◊─────
22222222222
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Fact, AutoInitShutdown]
		public void TestTileView_Horizontal_View1MinSize_Absolute ()
		{
			var tileView = Get11By3TileView (out var line);

			tileView.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			SetInputFocusLine (tileView);
			tileView.Tiles.ElementAt (0).MinSize = 1;

			// 0 should not be allowed because it brings us below minimum size of View1
			Assert.False(tileView.SetSplitterPos (0, 0));
			// position should remain where it was, at 50%
			Assert.Equal (Pos.Percent(50f), tileView.SplitterDistances.ElementAt (0));

			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"    
11111111111
─────◊─────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now move splitter line down (allowed
			line.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			tileView.Redraw (tileView.Bounds);
			looksLike =
@"    
11111111111
11111111111
─────◊─────";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// And up 2 (only 1 is allowed because of minimum size of 1 on view1)
			line.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			line.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			tileView.Redraw (tileView.Bounds);
			looksLike =
@"    
11111111111
─────◊─────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTileView_CannotSetSplitterPosToFuncEtc ()
		{
			var tileView = Get11By3TileView ();

			var ex = Assert.Throws<ArgumentException> (() => tileView.SetSplitterPos (0, Pos.Right (tileView)));
			Assert.Equal ("Only Percent and Absolute values are supported.  Passed value was PosCombine", ex.Message);


			ex = Assert.Throws<ArgumentException> (() => tileView.SetSplitterPos (0, Pos.Function (() => 1)));
			Assert.Equal ("Only Percent and Absolute values are supported.  Passed value was PosFunc", ex.Message);

			// Also not allowed because this results in a PosCombine
			ex = Assert.Throws<ArgumentException> (() => tileView.SetSplitterPos (0, Pos.Percent (50) - 1));
			Assert.Equal ("Only Percent and Absolute values are supported.  Passed value was PosCombine", ex.Message);
		}

		[Fact, AutoInitShutdown]
		public void TestNestedContainer2LeftAnd1Right_RendersNicely ()
		{
			var tileView = GetNestedContainer2Left1Right (false);

			Assert.Equal (20, tileView.Frame.Width);
			Assert.Equal (10, tileView.Tiles.ElementAt (0).View.Frame.Width);
			Assert.Equal (9, tileView.Tiles.ElementAt (1).View.Frame.Width);

			Assert.IsType<TileView> (tileView.Tiles.ElementAt (0).View);
			var left = (TileView)tileView.Tiles.ElementAt (0).View;
			Assert.Same (left.SuperView, tileView);


			Assert.Equal (2, left.Tiles.ElementAt (0).View.Subviews.Count);
			Assert.IsType<Label> (left.Tiles.ElementAt (0).View.Subviews [0]);
			Assert.IsType<Label> (left.Tiles.ElementAt (0).View.Subviews [1]);
			var onesTop = (Label)left.Tiles.ElementAt (0).View.Subviews [0];
			var onesBottom = (Label)left.Tiles.ElementAt (0).View.Subviews [1];

			Assert.Same (left.Tiles.ElementAt (0).View, onesTop.SuperView);
			Assert.Same (left.Tiles.ElementAt (0).View, onesBottom.SuperView);

			Assert.Equal (10, onesTop.Frame.Width);
			Assert.Equal (10, onesBottom.Frame.Width);

			tileView.Redraw (tileView.Bounds);

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


		[Fact, AutoInitShutdown]
		public void TestNestedContainer3RightAnd1Down_RendersNicely ()
		{
			var tileView = GetNestedContainer3Right1Down (false);

			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222├──────
111111│222222│444444
111111│222222│444444
111111│222222│444444
111111│222222│444444
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// It looks good but lets double check the measurements incase
			// anything is sticking out but drawn over

			// 3 panels + 2 splitters
			Assert.Equal (5, tileView.Subviews.Count);


			// Check X and Widths of Tiles
			Assert.Equal (0, tileView.Tiles.ElementAt (0).View.Frame.X);
			Assert.Equal (6, tileView.Tiles.ElementAt (0).View.Frame.Width);

			Assert.Equal (7, tileView.Tiles.ElementAt (1).View.Frame.X);
			Assert.Equal (6, tileView.Tiles.ElementAt (1).View.Frame.Width);

			Assert.Equal (14, tileView.Tiles.ElementAt (2).View.Frame.X);
			Assert.Equal (6, tileView.Tiles.ElementAt (2).View.Frame.Width);


			// Check Y and Heights of Tiles
			Assert.Equal (0, tileView.Tiles.ElementAt (0).View.Frame.Y);
			Assert.Equal (10, tileView.Tiles.ElementAt (0).View.Frame.Height);

			Assert.Equal (0, tileView.Tiles.ElementAt (1).View.Frame.Y);
			Assert.Equal (10, tileView.Tiles.ElementAt (1).View.Frame.Height);

			Assert.Equal (0, tileView.Tiles.ElementAt (2).View.Frame.Y);
			Assert.Equal (10, tileView.Tiles.ElementAt (2).View.Frame.Height);

			// Check Sub containers in last panel
			var subSplit = (TileView)tileView.Tiles.ElementAt (2).View;
			Assert.Equal (0, subSplit.Tiles.ElementAt (0).View.Frame.X);
			Assert.Equal (6, subSplit.Tiles.ElementAt (0).View.Frame.Width);
			Assert.Equal (0, subSplit.Tiles.ElementAt (0).View.Frame.Y);
			Assert.Equal (5, subSplit.Tiles.ElementAt (0).View.Frame.Height);
			Assert.IsType<TextView> (subSplit.Tiles.ElementAt (0).View.Subviews.Single ());

			Assert.Equal (0, subSplit.Tiles.ElementAt (1).View.Frame.X);
			Assert.Equal (6, subSplit.Tiles.ElementAt (1).View.Frame.Width);
			Assert.Equal (6, subSplit.Tiles.ElementAt (1).View.Frame.Y);
			Assert.Equal (4, subSplit.Tiles.ElementAt (1).View.Frame.Height);
			Assert.IsType<TextView> (subSplit.Tiles.ElementAt (1).View.Subviews.Single ());
		}

		[Fact, AutoInitShutdown]
		public void TestNestedContainer3RightAnd1Down_WithBorder_RendersNicely ()
		{
			var tileView = GetNestedContainer3Right1Down (true);

			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
┌─────┬──────┬─────┐
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222├─────┤
│11111│222222│44444│
│11111│222222│44444│
│11111│222222│44444│
└─────┴──────┴─────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// It looks good but lets double check the measurements incase
			// anything is sticking out but drawn over

			// 3 panels + 2 splitters
			Assert.Equal (5, tileView.Subviews.Count);

			// Check X and Widths of Tiles
			Assert.Equal (1, tileView.Tiles.ElementAt (0).View.Frame.X);
			Assert.Equal (5, tileView.Tiles.ElementAt (0).View.Frame.Width);

			Assert.Equal (7, tileView.Tiles.ElementAt (1).View.Frame.X);
			Assert.Equal (6, tileView.Tiles.ElementAt (1).View.Frame.Width);

			Assert.Equal (14, tileView.Tiles.ElementAt (2).View.Frame.X);
			Assert.Equal (5, tileView.Tiles.ElementAt (2).View.Frame.Width);


			// Check Y and Heights of Tiles
			Assert.Equal (1, tileView.Tiles.ElementAt (0).View.Frame.Y);
			Assert.Equal (8, tileView.Tiles.ElementAt (0).View.Frame.Height);

			Assert.Equal (1, tileView.Tiles.ElementAt (1).View.Frame.Y);
			Assert.Equal (8, tileView.Tiles.ElementAt (1).View.Frame.Height);

			Assert.Equal (1, tileView.Tiles.ElementAt (2).View.Frame.Y);
			Assert.Equal (8, tileView.Tiles.ElementAt (2).View.Frame.Height);

			// Check Sub containers in last panel
			var subSplit = (TileView)tileView.Tiles.ElementAt (2).View;
			Assert.Equal (0, subSplit.Tiles.ElementAt (0).View.Frame.X);
			Assert.Equal (5, subSplit.Tiles.ElementAt (0).View.Frame.Width);
			Assert.Equal (0, subSplit.Tiles.ElementAt (0).View.Frame.Y);
			Assert.Equal (4, subSplit.Tiles.ElementAt (0).View.Frame.Height);
			Assert.IsType<TextView> (subSplit.Tiles.ElementAt (0).View.Subviews.Single ());

			Assert.Equal (0, subSplit.Tiles.ElementAt (1).View.Frame.X);
			Assert.Equal (5, subSplit.Tiles.ElementAt (1).View.Frame.Width);
			Assert.Equal (5, subSplit.Tiles.ElementAt (1).View.Frame.Y);
			Assert.Equal (3, subSplit.Tiles.ElementAt (1).View.Frame.Height);
			Assert.IsType<TextView> (subSplit.Tiles.ElementAt (1).View.Subviews.Single ());
		}

		[Fact, AutoInitShutdown]
		public void TestNestedContainer3RightAnd1Down_WithTitledBorder_RendersNicely ()
		{
			var tileView = GetNestedContainer3Right1Down (true, true);

			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
┌T1───┬T2────┬T3───┐
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222├T4───┤
│11111│222222│44444│
│11111│222222│44444│
│11111│222222│44444│
└─────┴──────┴─────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestNestedContainer3RightAnd1Down_WithBorder_RemovingTiles ()
		{
			var tileView = GetNestedContainer3Right1Down (true);

			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
┌─────┬──────┬─────┐
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222├─────┤
│11111│222222│44444│
│11111│222222│44444│
│11111│222222│44444│
└─────┴──────┴─────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			var toRemove = tileView.Tiles.ElementAt (1);
			var removed = tileView.RemoveTile (1);
			Assert.Same (toRemove, removed);
			Assert.DoesNotContain (removed, tileView.Tiles);


			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌─────────┬────────┐
│111111111│33333333│
│111111111│33333333│
│111111111│33333333│
│111111111│33333333│
│111111111├────────┤
│111111111│44444444│
│111111111│44444444│
│111111111│44444444│
└─────────┴────────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// cannot remove at this index because there is only one horizontal tile left
			Assert.Null (tileView.RemoveTile (2));
			tileView.RemoveTile (0);



			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌──────────────────┐
│333333333333333333│
│333333333333333333│
│333333333333333333│
│333333333333333333│
├──────────────────┤
│444444444444444444│
│444444444444444444│
│444444444444444444│
└──────────────────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			Assert.NotNull (tileView.RemoveTile (0));


			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// cannot remove
			Assert.Null (tileView.RemoveTile (0));
		}

		[Theory, AutoInitShutdown]
		[InlineData (true)]
		[InlineData (false)]
		public void TestTileView_IndexOf (bool recursive)
		{
			var tv = new TileView ();
			var lbl1 = new Label ();
			var lbl2 = new Label ();
			var frame = new FrameView ();
			var sub = new Label ();
			frame.Add (sub);

			// IndexOf returns -1 when view not found
			Assert.Equal (-1, tv.IndexOf (lbl1, recursive));
			Assert.Equal (-1, tv.IndexOf (lbl2, recursive));

			// IndexOf supports looking for Tile.View
			Assert.Equal (0, tv.IndexOf (tv.Tiles.ElementAt (0).View, recursive));
			Assert.Equal (1, tv.IndexOf (tv.Tiles.ElementAt (1).View, recursive));

			// IndexOf supports looking for Tile.View.Subviews
			tv.Tiles.ElementAt (0).View.Add (lbl1);
			Assert.Equal (0, tv.IndexOf (lbl1, recursive));

			tv.Tiles.ElementAt (1).View.Add (lbl2);
			Assert.Equal (1, tv.IndexOf (lbl2, recursive));

			// IndexOf supports looking deep into subviews only when
			// the recursive true value is passed
			tv.Tiles.ElementAt (1).View.Add (frame);
			if (recursive) {
				Assert.Equal (1, tv.IndexOf (sub, recursive));
			} else {
				Assert.Equal (-1, tv.IndexOf (sub, recursive));
			}
		}

		[Fact, AutoInitShutdown]
		public void TestNestedRoots_BothRoots_BothCanHaveBorders ()
		{
			var tv = new TileView { Width = 10, Height = 5, ColorScheme = new ColorScheme (), IntegratedBorder = BorderStyle.Single };
			var tv2 = new TileView {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = new ColorScheme (),
				IntegratedBorder = BorderStyle.Single,
				Orientation = Orientation.Horizontal
			};

			Assert.True (tv.IsRootTileView ());
			tv.Tiles.ElementAt (1).View.Add (tv2);

			Application.Top.Add (tv);
			tv.BeginInit ();
			tv.EndInit ();
			tv.LayoutSubviews ();

			tv.LayoutSubviews ();
			tv.Tiles.ElementAt (1).View.LayoutSubviews ();
			tv2.LayoutSubviews ();

			// tv2 is still considered a root because 
			// it was manually created by API user.  That
			// means it will not have its lines joined to
			// parents and it is permitted to have a border
			Assert.True (tv2.IsRootTileView ());

			tv.Redraw (tv.Bounds);

			var looksLike =
@"
┌────┬───┐
│    │┌─┐│
│    │├─┤│
│    │└─┘│
└────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact,AutoInitShutdown]
		public void Test5Panel_MinSizes_VerticalSplitters_ResizeSplitter1()
		{
			var tv = Get5x1TilesView();

			tv.Tiles.ElementAt(0).MinSize = int.MaxValue;

			tv.Redraw (tv.Bounds);

			var looksLike =
@"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			for(int x=0;x<=5;x++) {
				// All these values would result in tile 0 getting smaller
				// so are not allowed (tile[0] has a min size of Int.Max)
				Assert.False (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
			}

			for (int x = 6; x < 10; x++) {
				// All these values would result in tile 0 getting bigger
				// so are allowed
				Assert.True (tv.SetSplitterPos (0, x),$"Assert failed for x={x}");
			}


			for (int x = 10; x < 100; x++) {
				// These values would result in the first splitter moving past
				// the second splitter so are not allowed
				Assert.False (tv.SetSplitterPos (0, x), $"Assert failed for x={x}");
			}

			tv.Redraw (tv.Bounds);

			looksLike =
@"
┌────────┬┬────┬────┬───┐
│11111111││3333│4444│555│
│        ││    │    │   │
└────────┴┴────┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}

		[Fact, AutoInitShutdown]
		public void Test5Panel_NoMinSizes_VerticalSplitters_ResizeSplitter2_CannotMoveOverNeighbours ()
		{
			var tv = Get5x1TilesView ();

			tv.Redraw (tv.Bounds);

			var looksLike =
@"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			for (int x = 10; x > 5; x--) {
				Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
			}

			for (int x = 5; x > 0; x--) {
				Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
			}

			tv.Redraw (tv.Bounds);

			looksLike =
@"
┌────┬┬────────┬────┬───┐
│1111││33333333│4444│555│
│    ││        │    │   │
└────┴┴────────┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);


			for (int x = 10; x < 15; x++) {
				Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
			}


			for (int x = 15; x < 25; x++) {
				Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
			}

			tv.Redraw (tv.Bounds);

			looksLike =
@"
┌────┬────────┬┬────┬───┐
│1111│22222222││4444│555│
│    │        ││    │   │
└────┴────────┴┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}

		[Fact, AutoInitShutdown]
		public void Test5Panel_MinSizes_VerticalSplitters_ResizeSplitter2 ()
		{
			var tv = Get5x1TilesView ();

			tv.Tiles.ElementAt (1).MinSize = 2;
			tv.Tiles.ElementAt (2).MinSize = 3;

			tv.Redraw (tv.Bounds);

			var looksLike =
@"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			for (int x = 10; x > 7; x--) {
				Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
			}

			for (int x = 7; x > 0; x--) {
				Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
			}

			tv.Redraw (tv.Bounds);

			looksLike =
@"
┌────┬──┬──────┬────┬───┐
│1111│22│333333│4444│555│
│    │  │      │    │   │
└────┴──┴──────┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);


			for (int x = 10; x < 12; x++) {
				Assert.True (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
			}


			for (int x = 12; x < 25; x++) {
				Assert.False (tv.SetSplitterPos (1, x), $"Assert failed for x={x}");
			}

			tv.Redraw (tv.Bounds);

			looksLike =
@"
┌────┬─────┬───┬────┬───┐
│1111│22222│333│4444│555│
│    │     │   │    │   │
└────┴─────┴───┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}


		[Fact, AutoInitShutdown]
		public void Test5Panel_NoMinSizes_VerticalSplitters_ResizeSplitter4_CannotMoveOverNeighbours ()
		{
			var tv = Get5x1TilesView ();

			tv.Redraw (tv.Bounds);

			var looksLike =
@"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			for (int x = 20; x > 15; x--) {
				Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
			}

			for (int x = 15; x > 0; x--) {
				Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
			}

			tv.Redraw (tv.Bounds);

			looksLike =
@"
┌────┬────┬────┬┬───────┐
│1111│2222│3333││5555555│
│    │    │    ││       │
└────┴────┴────┴┴───────┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);


			for (int x = 20; x < 24; x++) {
				Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
			}


			for (int x = 24; x < 100; x++) {
				Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
			}

			tv.Redraw (tv.Bounds);

			looksLike =
@"
┌────┬────┬────┬───────┬┐
│1111│2222│3333│4444444││
│    │    │    │       ││
└────┴────┴────┴───────┴┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}


		[Fact, AutoInitShutdown]
		public void Test5Panel_MinSizes_VerticalSplitters_ResizeSplitter4 ()
		{
			var tv = Get5x1TilesView ();

			tv.Tiles.ElementAt (3).MinSize = 2;
			tv.Tiles.ElementAt (4).MinSize = 1;

			tv.Redraw (tv.Bounds);

			var looksLike =
@"
┌────┬────┬────┬────┬───┐
│1111│2222│3333│4444│555│
│    │    │    │    │   │
└────┴────┴────┴────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			for (int x = 20; x > 17; x--) {
				Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
			}

			for (int x = 17; x > 0; x--) {
				Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
			}

			tv.Redraw (tv.Bounds);

			looksLike =
@"
┌────┬────┬────┬──┬─────┐
│1111│2222│3333│44│55555│
│    │    │    │  │     │
└────┴────┴────┴──┴─────┘

";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			for (int x = 20; x < 23; x++) {
				Assert.True (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
			}


			for (int x = 23; x < 100; x++) {
				Assert.False (tv.SetSplitterPos (3, x), $"Assert failed for x={x}");
			}


			tv.Redraw (tv.Bounds);

			looksLike =
@"
┌────┬────┬────┬──────┬─┐
│1111│2222│3333│444444│5│
│    │    │    │      │ │
└────┴────┴────┴──────┴─┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestNestedNonRoots_OnlyOneRoot_OnlyRootCanHaveBorders ()
		{
			var tv = new TileView { Width = 10, Height = 5, ColorScheme = new ColorScheme (), IntegratedBorder = BorderStyle.Single };

			tv.TrySplitTile (1, 2, out var tv2);
			tv2.ColorScheme = new ColorScheme ();
			tv2.IntegratedBorder = BorderStyle.Single; // will not be respected
			tv2.Orientation = Orientation.Horizontal;

			Assert.True (tv.IsRootTileView ());

			Application.Top.Add (tv);
			tv.BeginInit ();
			tv.EndInit ();
			tv.LayoutSubviews ();

			tv.LayoutSubviews ();
			tv.Tiles.ElementAt (1).View.LayoutSubviews ();
			tv2.LayoutSubviews ();

			// tv2 is not considered a root because 
			// it was created via TrySplitTile so it
			// will have its lines joined to
			// parent and cannot have its own border
			Assert.False (tv2.IsRootTileView ());

			tv.Redraw (tv.Bounds);

			var looksLike =
@"
┌────┬───┐
│    │   │
│    ├───┤
│    │   │
└────┴───┘
";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}



		[Fact, AutoInitShutdown]
		public void TestNestedContainer3RightAnd1Down_TileVisibility_WithBorder ()
		{
			var tileView = GetNestedContainer3Right1Down (true);
			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
┌─────┬──────┬─────┐
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222│33333│
│11111│222222├─────┤
│11111│222222│44444│
│11111│222222│44444│
│11111│222222│44444│
└─────┴──────┴─────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

			tileView.Tiles.ElementAt (0).View.Visible = false;
			tileView.Tiles.ElementAt (1).View.Visible = true;
			tileView.Tiles.ElementAt (2).View.Visible = true;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			 looksLike =
@"
┌────────────┬─────┐
│222222222222│33333│
│222222222222│33333│
│222222222222│33333│
│222222222222│33333│
│222222222222├─────┤
│222222222222│44444│
│222222222222│44444│
│222222222222│44444│
└────────────┴─────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

			tileView.Tiles.ElementAt (0).View.Visible = true;
			tileView.Tiles.ElementAt (1).View.Visible = false;
			tileView.Tiles.ElementAt (2).View.Visible = true;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌────────────┬─────┐
│111111111111│33333│
│111111111111│33333│
│111111111111│33333│
│111111111111│33333│
│111111111111├─────┤
│111111111111│44444│
│111111111111│44444│
│111111111111│44444│
└────────────┴─────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);


			tileView.Tiles.ElementAt (0).View.Visible = true;
			tileView.Tiles.ElementAt (1).View.Visible = true;
			tileView.Tiles.ElementAt (2).View.Visible = false;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌─────┬────────────┐
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
│11111│222222222222│
└─────┴────────────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);


			tileView.Tiles.ElementAt (0).View.Visible = true;
			tileView.Tiles.ElementAt (1).View.Visible = false;
			tileView.Tiles.ElementAt (2).View.Visible = false;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌──────────────────┐
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
│111111111111111111│
└──────────────────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);


			tileView.Tiles.ElementAt (0).View.Visible = false;
			tileView.Tiles.ElementAt (1).View.Visible = true;
			tileView.Tiles.ElementAt (2).View.Visible = false;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌──────────────────┐
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
│222222222222222222│
└──────────────────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

			tileView.Tiles.ElementAt (0).View.Visible = false;
			tileView.Tiles.ElementAt (1).View.Visible = false;
			tileView.Tiles.ElementAt (2).View.Visible = true;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌──────────────────┐
│333333333333333333│
│333333333333333333│
│333333333333333333│
│333333333333333333│
├──────────────────┤
│444444444444444444│
│444444444444444444│
│444444444444444444│
└──────────────────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);



			TestHelpers.AssertDriverContentsAre (looksLike, output);

			tileView.Tiles.ElementAt (0).View.Visible = false;
			tileView.Tiles.ElementAt (1).View.Visible = false;
			tileView.Tiles.ElementAt (2).View.Visible = false;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}



		[Fact, AutoInitShutdown]
		public void TestNestedContainer3RightAnd1Down_TileVisibility_WithoutBorder ()
		{
			var tileView = GetNestedContainer3Right1Down (false);
			tileView.Redraw (tileView.Bounds);

			string looksLike =
@"
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222│333333
111111│222222├──────
111111│222222│444444
111111│222222│444444
111111│222222│444444
111111│222222│444444";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

			tileView.Tiles.ElementAt (0).View.Visible = false;
			tileView.Tiles.ElementAt (1).View.Visible = true;
			tileView.Tiles.ElementAt (2).View.Visible = true;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			 looksLike =
@"
2222222222222│333333
2222222222222│333333
2222222222222│333333
2222222222222│333333
2222222222222│333333
2222222222222├──────
2222222222222│444444
2222222222222│444444
2222222222222│444444
2222222222222│444444";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

			tileView.Tiles.ElementAt (0).View.Visible = true;
			tileView.Tiles.ElementAt (1).View.Visible = false;
			tileView.Tiles.ElementAt (2).View.Visible = true;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
1111111111111│333333
1111111111111│333333
1111111111111│333333
1111111111111│333333
1111111111111│333333
1111111111111├──────
1111111111111│444444
1111111111111│444444
1111111111111│444444
1111111111111│444444";

			TestHelpers.AssertDriverContentsAre (looksLike, output);


			tileView.Tiles.ElementAt (0).View.Visible = true;
			tileView.Tiles.ElementAt (1).View.Visible = true;
			tileView.Tiles.ElementAt (2).View.Visible = false;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222
111111│2222222222222";

			TestHelpers.AssertDriverContentsAre (looksLike, output);


			tileView.Tiles.ElementAt (0).View.Visible = true;
			tileView.Tiles.ElementAt (1).View.Visible = false;
			tileView.Tiles.ElementAt (2).View.Visible = false;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111
11111111111111111111";

			TestHelpers.AssertDriverContentsAre (looksLike, output);


			tileView.Tiles.ElementAt (0).View.Visible = false;
			tileView.Tiles.ElementAt (1).View.Visible = true;
			tileView.Tiles.ElementAt (2).View.Visible = false;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222
22222222222222222222";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

			tileView.Tiles.ElementAt (0).View.Visible = false;
			tileView.Tiles.ElementAt (1).View.Visible = false;
			tileView.Tiles.ElementAt (2).View.Visible = true;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
33333333333333333333
33333333333333333333
33333333333333333333
33333333333333333333
33333333333333333333
────────────────────
44444444444444444444
44444444444444444444
44444444444444444444
44444444444444444444";

			TestHelpers.AssertDriverContentsAre (looksLike, output);



			TestHelpers.AssertDriverContentsAre (looksLike, output);

			tileView.Tiles.ElementAt (0).View.Visible = false;
			tileView.Tiles.ElementAt (1).View.Visible = false;
			tileView.Tiles.ElementAt (2).View.Visible = false;
			tileView.LayoutSubviews ();

			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
 ";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}


		/// <summary>
		/// Creates a vertical orientation root container with left pane split into
		/// two (with horizontal splitter line).
		/// </summary>
		/// <param name="withBorder"></param>
		/// <returns></returns>
		private TileView GetNestedContainer2Left1Right (bool withBorder)
		{
			var container = GetTileView (20, 10, withBorder);
			Assert.True (container.TrySplitTile (0, 2, out var newContainer));

			newContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			newContainer.ColorScheme = new ColorScheme ();
			container.ColorScheme = new ColorScheme ();

			container.LayoutSubviews ();
			return container;
		}

		/// <summary>
		/// Creates a vertical orientation root container with 3 tiles.
		/// The rightmost is split horizontally
		/// </summary>
		/// <param name="withBorder"></param>
		/// <returns></returns>
		private TileView GetNestedContainer3Right1Down (bool withBorder, bool withTitles = false)
		{
			var container =
			new TileView (3) {
				Width = 20,
				Height = 10,
				IntegratedBorder = withBorder ? BorderStyle.Single : BorderStyle.None
			};

			Assert.True (container.TrySplitTile (2, 2, out var newContainer));

			newContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;

			int i = 0;
			foreach (var tile in container.Tiles.Take (2).Union (newContainer.Tiles)) {
				i++;

				if (withTitles) {
					tile.Title = "T" + i;
				}

				tile.View.Add (new TextView {
					Width = Dim.Fill (),
					Height = Dim.Fill (),
					Text =
						string.Join ('\n',
						Enumerable.Repeat (
							new string (i.ToString () [0], 100)
							, 10).ToArray ()),
					WordWrap = false
				});
			}

			newContainer.ColorScheme = new ColorScheme ();
			container.ColorScheme = new ColorScheme ();
			container.LayoutSubviews ();
			return container;
		}

		private LineView GetLine (TileView tileView)
		{
			return tileView.Subviews.OfType<LineView> ().Single ();
		}

		private void SetInputFocusLine (TileView tileView)
		{
			var line = GetLine (tileView);
			line.SetFocus ();
			Assert.True (line.HasFocus);
		}


		private TileView Get5x1TilesView ()
		{
			var tv = new TileView (5){ Width = 25, Height = 4, ColorScheme = new ColorScheme (), IntegratedBorder = BorderStyle.Single };

			tv.Tiles.ElementAt (0).View.Add (new Label(new string('1',100)){AutoSize=false,Width=Dim.Fill(),Height = 1});
			tv.Tiles.ElementAt (1).View.Add (new Label(new string('2',100)){AutoSize=false,Width=Dim.Fill(),Height = 1});
			tv.Tiles.ElementAt (2).View.Add (new Label(new string('3',100)){AutoSize=false,Width=Dim.Fill(),Height = 1});
			tv.Tiles.ElementAt (3).View.Add (new Label(new string('4',100)){AutoSize=false,Width=Dim.Fill(),Height = 1});
			tv.Tiles.ElementAt (4).View.Add (new Label(new string('5',100)){AutoSize=false,Width=Dim.Fill(),Height = 1});

			Application.Top.Add (tv);
			tv.BeginInit ();
			tv.EndInit ();
			tv.LayoutSubviews ();

			return tv;
		}

		private TileView Get11By3TileView (out LineView line, bool withBorder = false)
		{
			var split = Get11By3TileView (withBorder);
			line = GetLine (split);

			return split;
		}
		private TileView Get11By3TileView (bool withBorder = false)
		{
			return GetTileView (11, 3, withBorder);
		}
		private TileView GetTileView (int width, int height, bool withBorder = false)
		{
			var container = new TileView () {
				Width = width,
				Height = height,
			};

			container.IntegratedBorder = withBorder ? BorderStyle.Single : BorderStyle.None;

			container.Tiles.ElementAt (0).View.Add (new Label (new string ('1', 100)) { Width = Dim.Fill (), Height = 1, AutoSize = false });
			container.Tiles.ElementAt (0).View.Add (new Label (new string ('1', 100)) { Width = Dim.Fill (), Height = 1, AutoSize = false, Y = 1 });
			container.Tiles.ElementAt (1).View.Add (new Label (new string ('2', 100)) { Width = Dim.Fill (), Height = 1, AutoSize = false });
			container.Tiles.ElementAt (1).View.Add (new Label (new string ('2', 100)) { Width = Dim.Fill (), Height = 1, AutoSize = false, Y = 1 });

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
