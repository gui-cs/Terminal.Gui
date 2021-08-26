using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.Views {
	public class PanelViewTests {
		[Fact]
		public void Constructor_Defaults ()
		{
			var pv = new PanelView ();

			Assert.False (pv.CanFocus);
			Assert.False (pv.Visible);
			Assert.False (pv.UsePanelFrame);
			Assert.Null (pv.Child);

			pv = new PanelView (new Label ("This is a test."));

			Assert.False (pv.CanFocus);
			Assert.True (pv.Visible);
			Assert.False (pv.UsePanelFrame);
			Assert.NotNull (pv.Child);
			Assert.NotNull (pv.Border);
			Assert.Null (pv.Child.Border);
		}

		[Fact]
		public void Child_Sets_To_Null_Remove_From_Subviews_PanelView ()
		{
			var pv = new PanelView (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews[0].Subviews.Count);

			pv.Child = null;
			Assert.Null (pv.Child);
			Assert.Equal (0, pv.Subviews [0].Subviews.Count);
		}

		[Fact]
		public void Add_View_Also_Sets_Child ()
		{
			var pv = new PanelView ();
			Assert.Null (pv.Child);
			Assert.Equal (0, pv.Subviews [0].Subviews.Count);

			pv.Add (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);
		}

		[Fact]
		public void Add_More_Views_Remove_Last_Child_Before__Only_One_Is_Allowed ()
		{
			var pv = new PanelView (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);
			Assert.IsType<Label> (pv.Child);

			pv.Add (new TextField ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);
			Assert.IsNotType<Label> (pv.Child);
			Assert.IsType<TextField> (pv.Child);
		}

		[Fact]
		public void Remove_RemoveAll_View_Also_Sets_Child_To_Null ()
		{
			var pv = new PanelView (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);

			pv.Remove (pv.Child);
			Assert.Null (pv.Child);
			Assert.Equal (0, pv.Subviews [0].Subviews.Count);

			pv = new PanelView (new Label ("This is a test."));
			Assert.NotNull (pv.Child);
			Assert.Equal (1, pv.Subviews [0].Subviews.Count);

			pv.RemoveAll ();
			Assert.Null (pv.Child);
			Assert.Equal (0, pv.Subviews [0].Subviews.Count);
		}

		[Fact]
		[AutoInitShutdown]
		public void AdjustContainer_Without_Border ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new Label ("This is a test."));
			win.Add (pv);
			top.Add (win);

			Application.Begin (top);

			Assert.Equal (new Rect (0, 0, 15, 1), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void AdjustContainer_With_Border_Absolute_Values ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new Label ("This is a test.") {
				Border = new Border () {
					BorderStyle = BorderStyle.Double,
					BorderThickness = new Thickness (1, 2, 3, 4),
					Padding = new Thickness (1, 2, 3, 4)
				}
			});
			win.Add (pv);
			top.Add (win);

			Application.Begin (top);

			Assert.Equal (new Rect (0, 0, 25, 15), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void AdjustContainer_With_Border_Computed_Values ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new TextView () {
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Border = new Border () {
					BorderStyle = BorderStyle.Double,
					BorderThickness = new Thickness (1, 2, 3, 4),
					Padding = new Thickness (1, 2, 3, 4)
				}
			});

			var pv1 = new PanelView (new TextView () {
				Width = Dim.Fill (1),
				Height = Dim.Fill (1),
				Border = new Border () {
					BorderStyle = BorderStyle.Double,
					BorderThickness = new Thickness (1, 2, 3, 4),
					Padding = new Thickness (1, 2, 3, 4)
				}
			});

			var pv2 = new PanelView (new TextView () {
				Width = Dim.Fill (2),
				Height = Dim.Fill (2),
				Border = new Border () {
					BorderStyle = BorderStyle.Double,
					BorderThickness = new Thickness (1, 2, 3, 4),
					Padding = new Thickness (1, 2, 3, 4)
				}
			});

			win.Add (pv, pv1, pv2);
			top.Add (win);

			Application.Begin (top);

			Assert.Equal (new Rect (0, 0, 78, 23), pv.Frame);
			Assert.Equal (new Rect (0, 0, 68, 9), pv.Child.Frame);
			Assert.Equal (new Rect (0, 0, 77, 22), pv1.Frame);
			Assert.Equal (new Rect (0, 0, 65, 6), pv1.Child.Frame);
			Assert.Equal (new Rect (0, 0, 76, 21), pv2.Frame);
			Assert.Equal (new Rect (0, 0, 62, 3), pv2.Child.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void UsePanelFrame_False_PanelView_Always_Respect_The_Child_Upper_Left_Corner_Position_And_Size ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new Label ("This is a test.")) {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10
			};
			var pv1 = new PanelView (new TextField (3, 4, 15, "This is a test.")) {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10
			};
			var pv2 = new PanelView (new TextView () {
				X = 5,
				Y = 6,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			}) {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10
			};

			win.Add (pv, pv1, pv2);
			top.Add (win);

			Application.Begin (top);

			Assert.Equal (new Rect (0, 0, 15, 1), pv.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv.Child.Frame);
			Assert.Equal (new Rect (3, 4, 15, 1), pv1.Frame);
			Assert.Equal (new Rect (0, 0, 15, 1), pv1.Child.Frame);
			Assert.Equal (new Rect (5, 6, 73, 17), pv2.Frame);
			Assert.Equal (new Rect (0, 0, 73, 17), pv2.Child.Frame);
		}

		[Fact]
		[AutoInitShutdown]
		public void UsePanelFrame_True_PanelView_Position_And_Size_Are_Used ()
		{
			var top = Application.Top;
			var win = new Window ();
			var pv = new PanelView (new TextView () {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10
			}) {
				X = 5,
				Y = 6,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				UsePanelFrame = true
			};
			var pv1 = new PanelView (new TextView () {
				X = 5,
				Y = 6,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			}) {
				X = 2,
				Y = 4,
				Width = 20,
				Height = 10,
				UsePanelFrame = true
			};

			win.Add (pv, pv1);
			top.Add (win);

			Application.Begin (top);

			Assert.Equal (new Rect (5, 6, 73, 17), pv.Frame);
			Assert.Equal (new Rect (0, 0, 20, 10), pv.Child.Frame);
			Assert.Equal (new Rect (2, 4, 20, 10), pv1.Frame);
			Assert.Equal (new Rect (0, 0, 20, 10), pv1.Child.Frame);
		}
	}
}
