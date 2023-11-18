using System.Text;
using System;
using Xunit;
using Xunit.Abstractions;
using static Terminal.Gui.View;

namespace Terminal.Gui.ViewsTests {
	public class DrawTests {
		readonly ITestOutputHelper output;

		public DrawTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		// TODO: The tests below that use Label should use View instead.
		[Fact, AutoInitShutdown]
		public void Non_Bmp_ConsoleWidth_ColumnWidth_Equal_Two ()
		{
			string us = "\U0001d539";
			Rune r = (Rune)0x1d539;

			Assert.Equal ("𝔹", us);
			Assert.Equal ("𝔹", r.ToString ());
			Assert.Equal (us, r.ToString ());

			Assert.Equal (1, us.GetColumns ());
			Assert.Equal (1, r.GetColumns ());

			var win = new Window () { Title = us };
			var label = new Label (r.ToString ());
			var tf = new TextField (us) { Y = 1, Width = 3 };
			win.Add (label, tf);
			var top = Application.Top;
			top.Add (win);

			Application.Begin (top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			var expected = @"
┌┤𝔹├─────┐
│𝔹       │
│𝔹       │
└────────┘";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			TestHelpers.AssertDriverContentsAre (expected, output);

			var expectedColors = new Attribute [] {
				// 0
				Colors.Base.Normal,
				// 1
				Colors.Base.Focus,
				// 2
				Colors.Base.HotNormal
			};

			TestHelpers.AssertDriverColorsAre (@"
0020000000
0000000000
0111000000
0000000000", driver: Application.Driver, expectedColors);
		}

		[Fact, AutoInitShutdown]
		public void CJK_Compatibility_Ideographs_ConsoleWidth_ColumnWidth_Equal_Two ()
		{
			string us = "\U0000f900";
			Rune r = (Rune)0xf900;

			Assert.Equal ("豈", us);
			Assert.Equal ("豈", r.ToString ());
			Assert.Equal (us, r.ToString ());

			Assert.Equal (2, us.GetColumns ());
			Assert.Equal (2, r.GetColumns ());

			var win = new Window () { Title = us };
			var label = new Label (r.ToString ());
			var tf = new TextField (us) { Y = 1, Width = 3 };
			win.Add (label, tf);
			var top = Application.Top;
			top.Add (win);

			Application.Begin (top);
			((FakeDriver)Application.Driver).SetBufferSize (10, 4);

			var expected = @"
┌┤豈├────┐
│豈      │
│豈      │
└────────┘";
			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

			TestHelpers.AssertDriverContentsAre (expected, output);

			var expectedColors = new Attribute [] {
				// 0
				Colors.Base.Normal,
				// 1
				Colors.Base.Focus,
				// 2
				Colors.Base.HotNormal
			};

			TestHelpers.AssertDriverColorsAre (@"
0022000000
0000000000
0111000000
0000000000", driver: Application.Driver, expectedColors);
		}

		[Fact, AutoInitShutdown]
		public void Colors_On_TextAlignment_Right_And_Bottom ()
		{
			var labelRight = new Label ("Test") {
				Width = 6,
				Height = 1,
				TextAlignment = TextAlignment.Right,
				ColorScheme = Colors.Base
			};
			var labelBottom = new Label ("Test", TextDirection.TopBottom_LeftRight) {
				Y = 1,
				Width = 1,
				Height = 6,
				VerticalTextAlignment = VerticalTextAlignment.Bottom,
				ColorScheme = Colors.Base
			};
			var top = Application.Top;
			top.Add (labelRight, labelBottom);

			Application.Begin (top);
			((FakeDriver)Application.Driver).SetBufferSize (7, 7);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
  Test
      
      
T     
e     
s     
t     ", output);

			TestHelpers.AssertDriverColorsAre (@"
000000
0
0
0
0
0
0", driver: Application.Driver, new Attribute [] { Colors.Base.Normal });
		}

		[Fact, AutoInitShutdown]
		public void Draw_Negative_Bounds_Horizontal_Without_New_Lines ()
		{
			// BUGBUG: This previously assumed the default height of a View was 1. 
			var subView = new View () { Id = "subView", Y = 1, Width = 7, Height = 1, Text = "subView" };
			var view = new View () { Id = "view", Width = 20, Height = 2, Text = "01234567890123456789" };
			view.Add (subView);
			var content = new View () { Id = "content", Width = 20, Height = 20 };
			content.Add (view);
			var container = new View () { Id = "container", X = 1, Y = 1, Width = 5, Height = 5 };
			container.Add (content);
			var top = Application.Top;
			top.Add (container);
			// BUGBUG: v2 - it's bogus to reference .Frame before BeginInit. And why is the clip being set anyway???

			void Top_LayoutComplete (object sender, LayoutEventArgs e)
			{
				Application.Driver.Clip = container.Frame;
			}
			top.LayoutComplete += Top_LayoutComplete;
			Application.Begin (top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
 01234
 subVi", output);

			content.X = -1;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 12345
 ubVie", output);

			content.Y = -1;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 ubVie", output);

			content.Y = -2;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre ("", output);

			content.X = -20;
			content.Y = 0;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre ("", output);
		}

		[Fact, AutoInitShutdown]
		public void Draw_Negative_Bounds_Horizontal_With_New_Lines ()
		{
			var subView = new View () { Id = "subView", X = 1, Width = 1, Height = 7, Text = "s\nu\nb\nV\ni\ne\nw" };
			var view = new View () { Id = "view", Width = 2, Height = 20, Text = "0\n1\n2\n3\n4\n5\n6\n7\n8\n9\n0\n1\n2\n3\n4\n5\n6\n7\n8\n9" };
			view.Add (subView);
			var content = new View () { Id = "content", Width = 20, Height = 20 };
			content.Add (view);
			var container = new View () { Id = "container", X = 1, Y = 1, Width = 5, Height = 5 };
			container.Add (content);
			var top = Application.Top;
			top.Add (container);
			Application.Driver.Clip = container.Frame;
			Application.Begin (top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
 0s
 1u
 2b
 3V
 4i", output);

			content.X = -1;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 s
 u
 b
 V
 i", output);

			content.X = -2;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"", output);

			content.X = 0;
			content.Y = -1;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 1u
 2b
 3V
 4i
 5e", output);

			content.Y = -6;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 6w
 7 
 8 
 9 
 0 ", output);

			content.Y = -19;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 9", output);

			content.Y = -20;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre ("", output);

			content.X = -2;
			content.Y = 0;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre ("", output);
		}

		[Fact, AutoInitShutdown]
		public void Draw_Negative_Bounds_Vertical ()
		{
			var subView = new View () { Id = "subView", X = 1, Width = 1, Height = 7, Text = "subView", TextDirection = TextDirection.TopBottom_LeftRight };
			var view = new View () { Id = "view", Width = 2, Height = 20, Text = "01234567890123456789", TextDirection = TextDirection.TopBottom_LeftRight };
			view.Add (subView);
			var content = new View () { Id = "content", Width = 20, Height = 20 };
			content.Add (view);
			var container = new View () { Id = "container", X = 1, Y = 1, Width = 5, Height = 5 };
			container.Add (content);
			var top = Application.Top;
			top.Add (container);
			Application.Driver.Clip = container.Frame;
			Application.Begin (top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
 0s
 1u
 2b
 3V
 4i", output);

			content.X = -1;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 s
 u
 b
 V
 i", output);

			content.X = -2;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"", output);

			content.X = 0;
			content.Y = -1;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 1u
 2b
 3V
 4i
 5e", output);

			content.Y = -6;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 6w
 7 
 8 
 9 
 0 ", output);

			content.Y = -19;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
 9", output);

			content.Y = -20;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre ("", output);

			content.X = -2;
			content.Y = 0;
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre ("", output);
		}

		[Fact, AutoInitShutdown]
		public void DrawFrame_Merge ()
		{
			var label = new View () { X = Pos.Center (), Y = Pos.Center (), Text = "test", AutoSize = true };
			var view = new View () { Width = 10, Height = 5 };
			view.DrawContent += (s, e) => view.DrawFrame (view.Bounds, LineStyle.Single);
			view.Add (label);
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│        │
│  test  │
│        │
└────────┘", output);
		}

		[Fact, AutoInitShutdown]
		public void DrawFrame_Without_Merge ()
		{
			var label = new View () { X = Pos.Center (), Y = Pos.Center (), Text = "test", AutoSize = true };
			var view = new View () { Width = 10, Height = 5 };
			view.DrawContentComplete += (s, e) => {
				view.DrawFrame (view.Bounds, LineStyle.Single, null, false);
				view.OnRenderLineCanvas ();
			};
			view.Add (label);
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌────────┐
│        │
│  test  │
│        │
└────────┘", output);
		}

		[Theory, AutoInitShutdown]
		[InlineData (1, Side.Left, 5, Side.Left, @"
┌────────┐
│        │
   test  │
         │
─────────┘")]
		[InlineData (1, Side.Left, 4, Side.Left, @"
┌────────┐
│        │
   test  │
         │
└────────┘")]
		[InlineData (0, Side.Left, 3, Side.Left, @"
┌────────┐
         │
   test  │
│        │
└────────┘")]
		[InlineData (5, Side.Top, -1, Side.Top, @"
│    ────┐
│        │
│  test  │
│        │
└────────┘")]
		[InlineData (5, Side.Top, 0, Side.Top, @"
┌    ────┐
│        │
│  test  │
│        │
└────────┘")]
		[InlineData (6, Side.Top, 1, Side.Top, @"
┌─    ───┐
│        │
│  test  │
│        │
└────────┘")]
		[InlineData (7, Side.Top, 2, Side.Top, @"
┌──    ──┐
│        │
│  test  │
│        │
└────────┘")]
		[InlineData (8, Side.Top, 3, Side.Top, @"
┌───    ─┐
│        │
│  test  │
│        │
└────────┘")]
		[InlineData (9, Side.Top, 4, Side.Top, @"
┌────    ┐
│        │
│  test  │
│        │
└────────┘")]
		[InlineData (10, Side.Top, 5, Side.Top, @"
┌─────   │
│        │
│  test  │
│        │
└────────┘")]
		[InlineData (3, Side.Right, -1, Side.Right, @"
┌─────────
│         
│  test   
│        │
└────────┘")]
		[InlineData (3, Side.Right, 0, Side.Right, @"
┌────────┐
│         
│  test   
│        │
└────────┘")]
		[InlineData (4, Side.Right, 1, Side.Right, @"
┌────────┐
│        │
│  test   
│         
└────────┘")]
		[InlineData (4, Side.Bottom, 10, Side.Bottom, @"
┌────────┐
│        │
│  test  │
│        │
└────    │")]
		[InlineData (4, Side.Bottom, 9, Side.Bottom, @"
┌────────┐
│        │
│  test  │
│        │
└────    ┘")]
		[InlineData (3, Side.Bottom, 8, Side.Bottom, @"
┌────────┐
│        │
│  test  │
│        │
└───    ─┘")]
		[InlineData (2, Side.Bottom, 7, Side.Bottom, @"
┌────────┐
│        │
│  test  │
│        │
└──    ──┘")]
		[InlineData (1, Side.Bottom, 6, Side.Bottom, @"
┌────────┐
│        │
│  test  │
│        │
└─    ───┘")]
		[InlineData (0, Side.Bottom, 5, Side.Bottom, @"
┌────────┐
│        │
│  test  │
│        │
└    ────┘")]
		[InlineData (-1, Side.Bottom, 5, Side.Bottom, @"
┌────────┐
│        │
│  test  │
│        │
│    ────┘")]
		public void DrawIncompleteFrame_All_Sides (int start, Side startSide, int end, Side endSide, string expected)
		{
			View view = GetViewsForDrawFrameTests (start, startSide, end, endSide);
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		private static View GetViewsForDrawFrameTests (int start, Side startSide, int end, Side endSide)
		{
			var label = new View () { X = Pos.Center (), Y = Pos.Center (), Text = "test", AutoSize = true };
			var view = new View () { Width = 10, Height = 5 };
			view.DrawContent += (s, e) =>
				view.DrawIncompleteFrame (new (start, startSide), new (end, endSide), view.Bounds, LineStyle.Single);
			view.Add (label);
			return view;
		}

		[Theory, AutoInitShutdown]
		[InlineData (4, Side.Left, 4, Side.Right, @"
┌────────┐
│        │
│  test  │
│        │
│        │")]
		[InlineData (0, Side.Top, 0, Side.Bottom, @"
─────────┐
         │
   test  │
         │
─────────┘")]
		[InlineData (0, Side.Right, 0, Side.Left, @"
│        │
│        │
│  test  │
│        │
└────────┘")]
		[InlineData (9, Side.Bottom, 9, Side.Top, @"
┌─────────
│         
│  test   
│         
└─────────")]
		public void DrawIncompleteFrame_Three_Full_Sides (int start, Side startSide, int end, Side endSide, string expected)
		{
			View view = GetViewsForDrawFrameTests (start, startSide, end, endSide);
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Theory, AutoInitShutdown]
		[InlineData (4, Side.Left, 10, Side.Top, @"
┌─────────
│         
│  test   
│         
│         ")]
		[InlineData (0, Side.Top, 5, Side.Right, @"
─────────┐
         │
   test  │
         │
         │")]
		[InlineData (0, Side.Right, 0, Side.Bottom, @"
         │
         │
   test  │
         │
─────────┘")]
		[InlineData (9, Side.Bottom, 0, Side.Left, @"
│         
│         
│  test   
│         
└─────────")]
		public void DrawIncompleteFrame_Two_Full_Sides (int start, Side startSide, int end, Side endSide, string expected)
		{
			View view = GetViewsForDrawFrameTests (start, startSide, end, endSide);
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Theory, AutoInitShutdown]
		[InlineData (4, Side.Left, 0, Side.Left, @"
│      
│      
│  test
│      
│      ")]
		[InlineData (0, Side.Top, 9, Side.Top, @"
──────────
          
   test   ")]
		[InlineData (0, Side.Right, 4, Side.Right, @"
         │
         │
   test  │
         │
         │")]
		[InlineData (9, Side.Bottom, 0, Side.Bottom, @"
   test   
          
──────────")]
		public void DrawIncompleteFrame_One_Full_Sides (int start, Side startSide, int end, Side endSide, string expected)
		{
			View view = GetViewsForDrawFrameTests (start, startSide, end, endSide);
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Theory, AutoInitShutdown]
		[InlineData (0, Side.Bottom, 0, Side.Top, @"
┌      
│      
│  test
│      
└      ")]
		[InlineData (0, Side.Left, 0, Side.Right, @"
┌────────┐
          
   test   ")]
		[InlineData (9, Side.Top, 9, Side.Bottom, @"
         ┐
         │
   test  │
         │
         ┘")]
		[InlineData (4, Side.Right, 4, Side.Left, @"
   test   
          
└────────┘")]
		public void DrawIncompleteFrame_One_Full_Sides_With_Corner (int start, Side startSide, int end, Side endSide, string expected)
		{
			View view = GetViewsForDrawFrameTests (start, startSide, end, endSide);
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}

		[Theory, AutoInitShutdown]
		[InlineData (2, Side.Left, 2, Side.Left, @"
│  test")]
		[InlineData (3, Side.Top, 6, Side.Top, @"
   ────
       
   test")]
		[InlineData (2, Side.Right, 2, Side.Right, @"
   test  │")]
		[InlineData (6, Side.Bottom, 3, Side.Bottom, @"
   test
       
   ────")]
		public void DrawIncompleteFrame_One_Part_Sides (int start, Side startSide, int end, Side endSide, string expected)
		{
			View view = GetViewsForDrawFrameTests (start, startSide, end, endSide);
			Application.Top.Add (view);
			Application.Begin (Application.Top);

			TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		}
	}
}
