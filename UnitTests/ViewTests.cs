using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Terminal.Gui;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
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
			Assert.Equal (Dim.Sized (0), r.Width);
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
			Assert.False (r.OnEnter (new View ()));
			Assert.False (r.OnLeave (new View ()));

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

		[Fact]
		public void Added_Removed ()
		{
			var v = new View (new Rect (0, 0, 10, 24));
			var t = new View ();

			v.Added += (View e) => {
				Assert.True (v.SuperView == e);
			};

			v.Removed += (View e) => {
				Assert.True (v.SuperView == null);
			};

			t.Add (v);
			Assert.True (t.Subviews.Count == 1);

			t.Remove (v);
			Assert.True (t.Subviews.Count == 0);
		}

		[Fact]
		public void Subviews_TabIndexes_AreEqual ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.Subviews.IndexOf (v2) == 1);
			Assert.True (r.Subviews.IndexOf (v3) == 2);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);

			Assert.Equal (r.Subviews.IndexOf (v1), r.TabIndexes.IndexOf (v1));
			Assert.Equal (r.Subviews.IndexOf (v2), r.TabIndexes.IndexOf (v2));
			Assert.Equal (r.Subviews.IndexOf (v3), r.TabIndexes.IndexOf (v3));
		}

		[Fact]
		public void BringSubviewToFront_Subviews_vs_TabIndexes ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.BringSubviewToFront (v1);
			Assert.True (r.Subviews.IndexOf (v1) == 2);
			Assert.True (r.Subviews.IndexOf (v2) == 0);
			Assert.True (r.Subviews.IndexOf (v3) == 1);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);
		}

		[Fact]
		public void BringSubviewForward_Subviews_vs_TabIndexes ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.BringSubviewForward (v1);
			Assert.True (r.Subviews.IndexOf (v1) == 1);
			Assert.True (r.Subviews.IndexOf (v2) == 0);
			Assert.True (r.Subviews.IndexOf (v3) == 2);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);
		}

		[Fact]
		public void SendSubviewToBack_Subviews_vs_TabIndexes ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.SendSubviewToBack (v3);
			Assert.True (r.Subviews.IndexOf (v1) == 1);
			Assert.True (r.Subviews.IndexOf (v2) == 2);
			Assert.True (r.Subviews.IndexOf (v3) == 0);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);
		}

		[Fact]
		public void SendSubviewBackwards_Subviews_vs_TabIndexes ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.SendSubviewBackwards (v3);
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.Subviews.IndexOf (v2) == 2);
			Assert.True (r.Subviews.IndexOf (v3) == 1);

			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v2) == 1);
			Assert.True (r.TabIndexes.IndexOf (v3) == 2);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_ValidValues ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.TabIndex = 1;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 1);

			v1.TabIndex = 2;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 2);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_HigherValues ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.TabIndex = 3;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 2);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_LowerValues ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.TabIndex = -1;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_False ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.CanFocus = false;
			v1.TabIndex = 0;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 0);
			Assert.Equal (-1, v1.TabIndex);
		}

		[Fact]
		public void TabIndex_Set_CanFocus_False_To_True ()
		{
			var r = new View ();
			var v1 = new View ();
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			v1.CanFocus = true;
			v1.TabIndex = 1;
			Assert.True (r.Subviews.IndexOf (v1) == 0);
			Assert.True (r.TabIndexes.IndexOf (v1) == 1);
		}

		[Fact]
		public void TabStop_And_CanFocus_Are_All_True ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true };
			var v2 = new View () { CanFocus = true };
			var v3 = new View () { CanFocus = true };

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.True (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.True (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.True (v3.HasFocus);
		}

		[Fact]
		public void TabStop_Are_All_True_And_CanFocus_Are_All_False ()
		{
			var r = new View ();
			var v1 = new View ();
			var v2 = new View ();
			var v3 = new View ();

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
		}

		[Fact]
		public void TabStop_Are_All_False_And_CanFocus_Are_All_True ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true, TabStop = false };
			var v2 = new View () { CanFocus = true, TabStop = false };
			var v3 = new View () { CanFocus = true, TabStop = false };

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
		}

		[Fact]
		public void TabStop_And_CanFocus_Mixed_And_BothFalse ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true, TabStop = false };
			var v2 = new View () { CanFocus = false, TabStop = true };
			var v3 = new View () { CanFocus = false, TabStop = false };

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
		}

		[Fact]
		public void TabStop_All_True_And_Changing_CanFocus_Later ()
		{
			var r = new View ();
			var v1 = new View ();
			var v2 = new View ();
			var v3 = new View ();

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);

			v1.CanFocus = true;
			r.FocusNext ();
			Assert.True (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			v2.CanFocus = true;
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.True (v2.HasFocus);
			Assert.False (v3.HasFocus);
			v3.CanFocus = true;
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.True (v3.HasFocus);
		}

		[Fact]
		public void TabStop_All_False_And_All_True_And_Changing_TabStop_Later ()
		{
			var r = new View ();
			var v1 = new View () { CanFocus = true, TabStop = false };
			var v2 = new View () { CanFocus = true, TabStop = false };
			var v3 = new View () { CanFocus = true, TabStop = false };

			r.Add (v1, v2, v3);

			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);

			v1.TabStop = true;
			r.FocusNext ();
			Assert.True (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.False (v3.HasFocus);
			v2.TabStop = true;
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.True (v2.HasFocus);
			Assert.False (v3.HasFocus);
			v3.TabStop = true;
			r.FocusNext ();
			Assert.False (v1.HasFocus);
			Assert.False (v2.HasFocus);
			Assert.True (v3.HasFocus);
		}

		[Fact]
		public void CanFocus_Set_Changes_TabIndex_And_TabStop ()
		{
			var r = new View ();
			var v1 = new View ("1");
			var v2 = new View ("2");
			var v3 = new View ("3");

			r.Add (v1, v2, v3);

			v2.CanFocus = true;
			Assert.Equal (r.TabIndexes.IndexOf (v2), v2.TabIndex);
			Assert.Equal (0, v2.TabIndex);
			Assert.True (v2.TabStop);

			v1.CanFocus = true;
			Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
			Assert.Equal (1, v1.TabIndex);
			Assert.True (v1.TabStop);

			v1.TabIndex = 2;
			Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
			Assert.Equal (1, v1.TabIndex);
			v3.CanFocus = true;
			Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
			Assert.Equal (1, v1.TabIndex);
			Assert.Equal (r.TabIndexes.IndexOf (v3), v3.TabIndex);
			Assert.Equal (2, v3.TabIndex);
			Assert.True (v3.TabStop);

			v2.CanFocus = false;
			Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
			Assert.Equal (1, v1.TabIndex);
			Assert.True (v1.TabStop);
			Assert.NotEqual (r.TabIndexes.IndexOf (v2), v2.TabIndex);
			Assert.Equal (-1, v2.TabIndex);
			Assert.False (v2.TabStop);
			Assert.Equal (r.TabIndexes.IndexOf (v3), v3.TabIndex);
			Assert.Equal (2, v3.TabIndex);
			Assert.True (v3.TabStop);
		}

		[Fact]
		public void Load_Event ()
		{
			Application.Init (new FakeDriver (), new NetMainLoop (() => FakeConsole.ReadKey (true)));

			var t = new Toplevel ();

			var r = new View () { Width = Dim.Fill (), Height = Dim.Fill () };
			var v1 = new View () { Width = Dim.Fill (), Height = Dim.Fill () };
			var v2 = new View () { Width = Dim.Fill (), Height = Dim.Fill () };
			var sv1 = new View () { Width = Dim.Fill (), Height = Dim.Fill () };

			int rc = 0, v1c = 0, v2c = 0, sv1c = 0;

			r.Added += (e) => {
				Assert.Equal (0, r.Frame.Width);
				Assert.Equal (0, r.Frame.Height);
			};
			v1.Added += (e) => {
				Assert.Equal (0, v1.Frame.Width);
				Assert.Equal (0, v1.Frame.Height);
			};
			v2.Added += (e) => {
				Assert.Equal (0, v2.Frame.Width);
				Assert.Equal (0, v2.Frame.Height);
			};
			sv1.Added += (e) => {
				Assert.Equal (0, sv1.Frame.Width);
				Assert.Equal (0, sv1.Frame.Height);
			};

			r.Load += () => {
				rc++;
				Assert.Equal (t.Frame.Width, r.Frame.Width);
				Assert.Equal (t.Frame.Height, r.Frame.Height);
			};
			v1.Load += () => {
				v1c++;
				Assert.Equal (t.Frame.Width, v1.Frame.Width);
				Assert.Equal (t.Frame.Height, v1.Frame.Height);
			};
			v2.Load += () => {
				v2c++;
				Assert.Equal (t.Frame.Width, v2.Frame.Width);
				Assert.Equal (t.Frame.Height, v2.Frame.Height);
			};
			sv1.Load += () => {
				sv1c++;
				Assert.Equal (t.Frame.Width, sv1.Frame.Width);
				Assert.Equal (t.Frame.Height, sv1.Frame.Height);
			};


			v2.Add (sv1);
			r.Add (v1, v2);
			t.Add (r);

			Application.Iteration = () => {
				Application.Refresh ();
				t.Running = false;
			};

			Application.Run (t, true);
			Application.Shutdown (true);

			Assert.Equal (1, rc);
			Assert.Equal (1, v1c);
			Assert.Equal (1, v2c);
			Assert.Equal (1, sv1c);
		}
	}
}
