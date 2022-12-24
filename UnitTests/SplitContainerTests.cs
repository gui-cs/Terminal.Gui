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
