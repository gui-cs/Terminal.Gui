using System;
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


		[Fact,AutoInitShutdown]
		public void TestSplitContainer_Vertical()
		{
			var splitContainer = Get11By3SplitContainer ();
			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"
11111│22222
     │
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect if it is not focused
			splitContainer.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}
		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Vertical_Focused ()
		{
			var splitContainer = Get11By3SplitContainer ();
			splitContainer.EnsureFocus ();
			splitContainer.FocusFirst ();
			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"
11111│22222
     ◊
     │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now while focused move the splitter 1 unit right
			splitContainer.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
111111│2222
      ◊
      │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);


			// and 2 to the left
			splitContainer.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			splitContainer.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);

			looksLike =
@"
1111│222222
    ◊
    │     ";
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Horizontal ()
		{
			var splitContainer = Get11By3SplitContainer ();
			splitContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"    
11111111111
───────────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Keyboard movement on splitter should have no effect if it is not focused
			splitContainer.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			splitContainer.SetNeedsDisplay ();
			splitContainer.Redraw (splitContainer.Bounds);
			TestHelpers.AssertDriverContentsAre (looksLike, output);
		}

		[Fact, AutoInitShutdown]
		public void TestSplitContainer_Horizontal_Focused ()
		{
			var splitContainer = Get11By3SplitContainer ();

			splitContainer.Orientation = Terminal.Gui.Graphs.Orientation.Horizontal;
			splitContainer.EnsureFocus();
			splitContainer.FocusFirst();

			splitContainer.Redraw (splitContainer.Bounds);

			string looksLike =
@"    
11111111111
─────◊─────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// Now move splitter line down
			splitContainer.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);
			looksLike =
@"    
11111111111

─────◊─────";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

			// And 2 up
			splitContainer.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			splitContainer.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ()));
			splitContainer.Redraw (splitContainer.Bounds);
			looksLike =
@"    
─────◊─────
22222222222";
			TestHelpers.AssertDriverContentsAre (looksLike, output);

		}


		private SplitContainer Get11By3SplitContainer ()
		{
			var container = new SplitContainer () {
				Width = 11,
				Height = 3,
			};
			
			container.Panel1.Add (new Label (new string ('1', 100)));
			container.Panel2.Add (new Label (new string ('2', 100)));

			Application.Top.Add (container);
			container.ColorScheme = new ColorScheme ();
			container.LayoutSubviews ();

			return container;
		}
	}
}
