using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Terminal.Gui;
using Xunit;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui {
	public class ViewTests {
		[Fact]
		public void New_Initializes ()
		{
			// Parameterless
			var r = new View ();
			Assert.NotNull (r);
			Assert.Equal (LayoutStyle.Computed, r.LayoutStyle);
			Assert.Equal ("View()({X=0,Y=0,Width=0,Height=0})", r.ToString ());
			Assert.False (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
			Assert.Null (r.Focused);
			Assert.Null (r.ColorScheme);
			Assert.Equal (Dim.Sized (0), r.Height);
			Assert.Equal (Dim.Sized (0), r.Height);
			// BUGBUG: Pos needs eqality implemented
			//Assert.Equal (Pos.At (0), r.X);
			//Assert.Equal (Pos.At (0), r.Y);
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.Empty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.GetEnumerator().Current);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);

			// Empty Rect
			r = new View (Rect.Empty);
			Assert.NotNull (r);
			Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
			Assert.Equal ("View()({X=0,Y=0,Width=0,Height=0})", r.ToString ());
			Assert.False (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
			Assert.Null (r.Focused);
			Assert.Null (r.ColorScheme);
			Assert.Null (r.Height);
			Assert.Null (r.Height);
			Assert.Null (r.X);
			Assert.Null (r.Y);
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.Empty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.GetEnumerator ().Current);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);

			// Rect with values
			r = new View (new Rect(1, 2, 3, 4));
			Assert.NotNull (r);
			Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
			Assert.Equal ("View()({X=1,Y=2,Width=3,Height=4})", r.ToString ());
			Assert.False (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 3, 4), r.Bounds);
			Assert.Equal (new Rect (1, 2, 3, 4), r.Frame);
			Assert.Null (r.Focused);
			Assert.Null (r.ColorScheme);
			Assert.Null (r.Height);
			Assert.Null (r.Height);
			Assert.Null (r.X);
			Assert.Null (r.Y);
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.Empty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.GetEnumerator ().Current);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);

		}

		[Fact]
		public void New_Methods_Return_False ()
		{
			var r = new View ();

			Assert.False (r.ProcessKey (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.ProcessHotKey (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.ProcessColdKey (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.OnKeyDown (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.OnKeyUp (new KeyEvent () { Key = Key.Unknown }));
			Assert.False (r.MouseEvent (new MouseEvent () { Flags = MouseFlags.AllEvents }));
			Assert.False (r.OnMouseEnter (new MouseEvent () { Flags = MouseFlags.AllEvents }));
			Assert.False (r.OnMouseLeave (new MouseEvent () { Flags = MouseFlags.AllEvents }));
			Assert.False (r.OnEnter ());
			Assert.False (r.OnLeave ());

			// TODO: Add more
		}

		[Fact]
		public void TopologicalSort_Missing_Add ()
		{
			var root = new View ();
			var sub1 = new View ();
			root.Add (sub1);
			var sub2 = new View ();
			sub1.Width = Dim.Width(sub2);

			Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());

			sub2.Width = Dim.Width (sub1);

			Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());
		}

		[Fact]
		public void TopologicalSort_Recursive_Ref ()
		{
			var root = new View ();
			var sub1 = new View ();
			root.Add (sub1);
			var sub2 = new View ();
			root.Add (sub2);
			sub2.Width = Dim.Width (sub2);
			Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());
		}
	}
}
