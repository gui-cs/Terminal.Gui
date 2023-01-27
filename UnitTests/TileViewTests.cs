using System;
using System.Linq;
using Terminal.Gui;
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
			tileView.SetSplitterPos(0,Pos.Percent (50));
			Assert.IsType<Pos.PosFactor> (tileView.SplitterDistances.ElementAt(0));
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
			Assert.IsType<Pos.PosFactor> (tileView.SplitterDistances.ElementAt(0));


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
			tileView.Tiles.ElementAt(0).MinSize = 6;

			// distance is too small (below 6)
			tileView.SetSplitterPos(0, 2);

			// Should bound the value to the minimum distance
			Assert.Equal (6, tileView.SplitterDistances.ElementAt (0));

			tileView.Redraw (tileView.Bounds);

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
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter right if we want
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
1111111│222
1111111◊222
       │     ";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}


		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_View1MinSize_Absolute_WithBorder ()
		{
			var tileView = Get11By3TileView (out var line,true);
			SetInputFocusLine (tileView);
			tileView.Tiles.ElementAt(0).MinSize = 5;

			// distance is too small (below 5)
			tileView.SetSplitterPos(0,2);

			// Should bound the value to the minimum distance
			Assert.Equal (6, tileView.SplitterDistances.ElementAt(0));

			tileView.Redraw (tileView.Bounds);

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
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter right if we want
			line.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌──────┬──┐
│111111◊22│
└──────┴──┘";

			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_View2MinSize_Absolute ()
		{
			var tileView = Get11By3TileView (out var line);
			SetInputFocusLine (tileView);
			tileView.Tiles.ElementAt(1).MinSize = 6;

			// distance leaves too little space for view2 (less than 6 would remain)
			tileView.SetSplitterPos(0,8);

			// Should bound the value to the minimum distance
			Assert.Equal (4, tileView.SplitterDistances.ElementAt(0));

			tileView.Redraw (tileView.Bounds);

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
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter left if we want
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
111│2222222
111◊2222222
   │     ";

			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestTileView_Vertical_View2MinSize_Absolute_WithBorder ()
		{
			var tileView = Get11By3TileView (out var line, true);
			SetInputFocusLine (tileView);
			tileView.Tiles.ElementAt(1).MinSize = 5;

			// distance leaves too little space for view2 (less than 5 would remain)
			tileView.SetSplitterPos(0,8);

			// Should bound the value to the minimum distance
			Assert.Equal (4, tileView.SplitterDistances.ElementAt(0));

			tileView.Redraw (tileView.Bounds);

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
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// but we can continue to move the splitter left if we want
			line.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			tileView.SetNeedsDisplay ();
			tileView.Redraw (tileView.Bounds);

			looksLike =
@"
┌──┬──────┐
│11◊222222│
└──┴──────┘";

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
		public void TestTileView_InsertPanelMiddle()
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
			tileView.Tiles.ElementAt(0).MinSize = 1;

			// 0 should not be allowed because it brings us below minimum size of View1
			tileView.SetSplitterPos(0,0);
			Assert.Equal ((Pos)1, tileView.SplitterDistances.ElementAt(0));

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

			var ex = Assert.Throws<ArgumentException> (() => tileView.SetSplitterPos(0,Pos.Right (tileView)));
			Assert.Equal ("Only Percent and Absolute values are supported.  Passed value was PosCombine", ex.Message);


			ex = Assert.Throws<ArgumentException> (() => tileView.SetSplitterPos(0,Pos.Function (() => 1)));
			Assert.Equal ("Only Percent and Absolute values are supported.  Passed value was PosFunc", ex.Message);

			// Also not allowed because this results in a PosCombine
			ex = Assert.Throws<ArgumentException> (() => tileView.SetSplitterPos(0, Pos.Percent (50) - 1));
			Assert.Equal ("Only Percent and Absolute values are supported.  Passed value was PosCombine", ex.Message);
		}

		[Fact,AutoInitShutdown]
		public void TestNestedContainer2LeftAnd1Right_RendersNicely()
		{
			var tileView = GetNestedContainer2Left1Right (false);

			Assert.Equal (20,tileView.Frame.Width);
			Assert.Equal (10, tileView.Tiles.ElementAt(0).View.Frame.Width);
			Assert.Equal (9, tileView.Tiles.ElementAt (1).View.Frame.Width);

			Assert.IsType<TileView> (tileView.Tiles.ElementAt (0).View);
			var left = (TileView)tileView.Tiles.ElementAt (0).View;
			Assert.Same (left.SuperView, tileView);


			Assert.Equal(2, left.Tiles.ElementAt (0).View.Subviews.Count);
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


		[Fact,AutoInitShutdown]
		public void TestNestedContainer3RightAnd1Down_RendersNicely()
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
			Assert.Equal(5,tileView.Subviews.Count);


			// Check X and Widths of Tiles
			Assert.Equal(0,tileView.Tiles.ElementAt(0).View.Frame.X);
			Assert.Equal(6,tileView.Tiles.ElementAt(0).View.Frame.Width);

			Assert.Equal(7,tileView.Tiles.ElementAt(1).View.Frame.X);
			Assert.Equal(6,tileView.Tiles.ElementAt(1).View.Frame.Width);

			Assert.Equal(14,tileView.Tiles.ElementAt(2).View.Frame.X);
			Assert.Equal(6,tileView.Tiles.ElementAt(2).View.Frame.Width);
			

			// Check Y and Heights of Tiles
			Assert.Equal(0,tileView.Tiles.ElementAt(0).View.Frame.Y);
			Assert.Equal(10,tileView.Tiles.ElementAt(0).View.Frame.Height);

			Assert.Equal(0,tileView.Tiles.ElementAt(1).View.Frame.Y);
			Assert.Equal(10,tileView.Tiles.ElementAt(1).View.Frame.Height);

			Assert.Equal(0,tileView.Tiles.ElementAt(2).View.Frame.Y);
			Assert.Equal(10,tileView.Tiles.ElementAt(2).View.Frame.Height);
			
			// Check Sub containers in last panel
			var subSplit = (TileView)tileView.Tiles.ElementAt(2).View;
			Assert.Equal(0,subSplit.Tiles.ElementAt(0).View.Frame.X);
			Assert.Equal(6,subSplit.Tiles.ElementAt(0).View.Frame.Width);
			Assert.Equal(0,subSplit.Tiles.ElementAt(0).View.Frame.Y);
			Assert.Equal(5,subSplit.Tiles.ElementAt(0).View.Frame.Height);
			Assert.IsType<TextView>(subSplit.Tiles.ElementAt(0).View.Subviews.Single());

			Assert.Equal(0,subSplit.Tiles.ElementAt(1).View.Frame.X);
			Assert.Equal(6,subSplit.Tiles.ElementAt(1).View.Frame.Width);
			Assert.Equal(6,subSplit.Tiles.ElementAt(1).View.Frame.Y);
			Assert.Equal(4,subSplit.Tiles.ElementAt(1).View.Frame.Height);
			Assert.IsType<TextView>(subSplit.Tiles.ElementAt(1).View.Subviews.Single());
		}

		[Fact,AutoInitShutdown]
		public void TestNestedContainer3RightAnd1Down_WithBorder_RendersNicely()
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
			Assert.Equal(5,tileView.Subviews.Count);

			// Check X and Widths of Tiles
			Assert.Equal(1,tileView.Tiles.ElementAt(0).View.Frame.X);
			Assert.Equal(5,tileView.Tiles.ElementAt(0).View.Frame.Width);

			Assert.Equal(7,tileView.Tiles.ElementAt(1).View.Frame.X);
			Assert.Equal(6,tileView.Tiles.ElementAt(1).View.Frame.Width);

			Assert.Equal(14,tileView.Tiles.ElementAt(2).View.Frame.X);
			Assert.Equal(5,tileView.Tiles.ElementAt(2).View.Frame.Width);
			

			// Check Y and Heights of Tiles
			Assert.Equal(1,tileView.Tiles.ElementAt(0).View.Frame.Y);
			Assert.Equal(8,tileView.Tiles.ElementAt(0).View.Frame.Height);

			Assert.Equal(1,tileView.Tiles.ElementAt(1).View.Frame.Y);
			Assert.Equal(8,tileView.Tiles.ElementAt(1).View.Frame.Height);

			Assert.Equal(1,tileView.Tiles.ElementAt(2).View.Frame.Y);
			Assert.Equal(8,tileView.Tiles.ElementAt(2).View.Frame.Height);
			
			// Check Sub containers in last panel
			var subSplit = (TileView)tileView.Tiles.ElementAt(2).View;
			Assert.Equal(0,subSplit.Tiles.ElementAt(0).View.Frame.X);
			Assert.Equal(5,subSplit.Tiles.ElementAt(0).View.Frame.Width);
			Assert.Equal(0,subSplit.Tiles.ElementAt(0).View.Frame.Y);
			Assert.Equal(4,subSplit.Tiles.ElementAt(0).View.Frame.Height);
			Assert.IsType<TextView>(subSplit.Tiles.ElementAt(0).View.Subviews.Single());

			Assert.Equal(0,subSplit.Tiles.ElementAt(1).View.Frame.X);
			Assert.Equal(5,subSplit.Tiles.ElementAt(1).View.Frame.Width);
			Assert.Equal(5,subSplit.Tiles.ElementAt(1).View.Frame.Y);
			Assert.Equal(3,subSplit.Tiles.ElementAt(1).View.Frame.Height);
			Assert.IsType<TextView>(subSplit.Tiles.ElementAt(1).View.Subviews.Single());
		}


		/// <summary>
		/// Creates a vertical orientation root container with left pane split into
		/// two (with horizontal splitter line).
		/// </summary>
		/// <param name="withBorder"></param>
		/// <returns></returns>
		private TileView GetNestedContainer2Left1Right(bool withBorder)
		{
			var container = GetTileView (20, 10,withBorder);
			Assert.True (container.TrySplitTile (0,2, out var newContainer));
			
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
		private TileView GetNestedContainer3Right1Down(bool withBorder)
		{
			var container = 
			new TileView (3)
			{
				Width = 20,
				Height = 10,
				IntegratedBorder = withBorder ? BorderStyle.Single : BorderStyle.None
			};

			Assert.True (container.TrySplitTile (2,2, out var newContainer));
			
			newContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			
			int i=0;
			foreach(var tile in container.Tiles.Take(2).Union(newContainer.Tiles))
			{
				i++;

				tile.View.Add(new TextView{
					Width = Dim.Fill(),
					Height = Dim.Fill(),
					Text = 
						string.Join('\n',
						Enumerable.Repeat(
							new string(i.ToString()[0],100)
							,10).ToArray()),
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

		private TileView Get11By3TileView(out LineView line, bool withBorder = false)
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
