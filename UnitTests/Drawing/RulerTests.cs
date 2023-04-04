using Terminal.Gui;
using NStack;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Terminal.Gui.Graphs;
using Xunit;
using Xunit.Abstractions;
//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DrawingTests {
	public class RulerTests {

		readonly ITestOutputHelper output;

		public RulerTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact ()]
		public void Constructor_Defaults ()
		{
			var r = new Ruler ();
			Assert.Equal (0, r.Length);
			Assert.Equal ("0123456789", r.Template);
			Assert.Equal (Orientation.Horizontal, r.Orientation);
			Assert.Equal (default, r.Attribute);
		}


		[Fact ()]
		public void Orientation_set ()
		{
			var r = new Ruler ();
			Assert.Equal (Orientation.Horizontal, r.Orientation);
			r.Orientation = Orientation.Vertical;
			Assert.Equal (Orientation.Vertical, r.Orientation);
		}
		
		[Fact ()]
		public void Length_set ()
		{
			var r = new Ruler ();
			Assert.Equal (0, r.Length);
			r.Length = 42;
			Assert.Equal (42, r.Length);
		}

		[Fact ()]
		public void Template_set ()
		{
			var newTemplate = ")!@#$$%^&*(";

			var r = new Ruler ();
			Assert.Equal ("0123456789", r.Template);
			r.Template = newTemplate;
			Assert.Equal (newTemplate, r.Template);
		}

		[Fact ()]
		public void Attribute_set ()
		{
			var newAttribute = new Attribute (Color.Red, Color.Green);

			var r = new Ruler ();
			Assert.Equal (default, r.Attribute);
			r.Attribute = newAttribute;
			Assert.Equal (newAttribute, r.Attribute);
		}

		[Fact (), AutoInitShutdown]
		public void Draw_Default ()
		{
			((FakeDriver)Application.Driver).SetBufferSize (25, 25);

			var r = new Ruler ();
			r.Draw (new Point (0, 0));
			TestHelpers.AssertDriverContentsWithFrameAre (@"", output);
		}

		[Fact (), AutoInitShutdown]
		public void Draw_Horizontal ()
		{
			var len = 15;
			((FakeDriver)Application.Driver).SetBufferSize (len + 5, 5);

			// Add a frame so we can see the ruler
			var f = new FrameView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};
			Application.Top.Add (f);
			Application.Begin (Application.Top);
			Assert.Equal (new Rect (0, 0, len + 5, 5), f.Frame);
			
			var r = new Ruler ();
			Assert.Equal (Orientation.Horizontal, r.Orientation);

			r.Length = len;
			r.Draw (new Point(0,0));
			TestHelpers.AssertDriverContentsWithFrameAre (@"012345678901234", output);

			// Postive offset
			((FakeDriver)Application.Driver).FillRect (new Rect (0, 0, len * 2, len * 2), ' ');
			r.Draw (new Point (1, 1));
			TestHelpers.AssertDriverContentsWithFrameAre (@"

012345678901234", output);
		}

		[Fact (), AutoInitShutdown]
		public void Draw_Vertical ()
		{
			var len = 25;
			((FakeDriver)Application.Driver).SetBufferSize (len * 2, len * 2);

			var r = new Ruler ();
			r.Orientation = Orientation.Vertical;
			Assert.Equal (Orientation.Vertical, r.Orientation);

			r.Length = len;
			r.Draw (new Point (0, 0));
			TestHelpers.AssertDriverContentsWithFrameAre (@"
0
1
2
3
4
5
6
7
8
9
0
1
2
3
4
5
6
7
8
9
0
1
2
3
4", output);
		}
		
	}
}


