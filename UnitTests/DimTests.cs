using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Terminal.Gui;
using Xunit;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.Core {
	public class DimTests {
		public DimTests ()
		{
			Console.OutputEncoding = System.Text.Encoding.Default;
			// Change current culture
			CultureInfo culture = CultureInfo.CreateSpecificCulture ("en-US");
			Thread.CurrentThread.CurrentCulture = culture;
			Thread.CurrentThread.CurrentUICulture = culture;
		}

		[Fact]
		public void New_Works ()
		{
			var dim = new Dim ();
			Assert.Equal ("Terminal.Gui.Dim", dim.ToString ());
		}

		[Fact]
		public void Sized_SetsValue ()
		{
			var dim = Dim.Sized (0);
			Assert.Equal ("Dim.Absolute(0)", dim.ToString ());

			int testVal = 5;
			dim = Dim.Sized (testVal);
			Assert.Equal ($"Dim.Absolute({testVal})", dim.ToString ());

			testVal = -1;
			dim = Dim.Sized (testVal);
			Assert.Equal ($"Dim.Absolute({testVal})", dim.ToString ());
		}

		[Fact]
		public void Sized_Equals ()
		{
			int n1 = 0;
			int n2 = 0;
			var dim1 = Dim.Sized (n1);
			var dim2 = Dim.Sized (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 1;
			dim1 = Dim.Sized (n1);
			dim2 = Dim.Sized (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = -1;
			dim1 = Dim.Sized (n1);
			dim2 = Dim.Sized (n2);
			Assert.Equal (dim1, dim2);

			n1 = 0;
			n2 = 1;
			dim1 = Dim.Sized (n1);
			dim2 = Dim.Sized (n2);
			Assert.NotEqual (dim1, dim2);
		}

		[Fact]
		public void Width_SetsValue ()
		{
			var dim = Dim.Width (null);
			Assert.Throws<NullReferenceException> (() => dim.ToString ());

			var testVal = Rect.Empty;
			testVal = Rect.Empty;
			dim = Dim.Width (new View (testVal));
			Assert.Equal ($"DimView(side=Width, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", dim.ToString ());

			testVal = new Rect (1, 2, 3, 4);
			dim = Dim.Width (new View (testVal));
			Assert.Equal ($"DimView(side=Width, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", dim.ToString ());
		}

		[Fact]
		public void Width_Equals ()
		{
			var testRect1 = Rect.Empty;
			var view1 = new View (testRect1);
			var testRect2 = Rect.Empty;
			var view2 = new View (testRect2);

			var dim1 = Dim.Width (view1);
			var dim2 = Dim.Width (view1);
			// FIXED: Dim.Width should support Equals() and this should change to Equal.
			Assert.Equal (dim1, dim2);

			dim2 = Dim.Width (view2);
			Assert.NotEqual (dim1, dim2);

			testRect1 = new Rect (0, 1, 2, 3);
			view1 = new View (testRect1);
			testRect2 = new Rect (0, 1, 2, 3);
			dim1 = Dim.Width (view1);
			dim2 = Dim.Width (view1);
			// FIXED: Dim.Width should support Equals() and this should change to Equal.
			Assert.Equal (dim1, dim2);

			Assert.Throws<ArgumentException> (() => new Rect (0, -1, -2, -3));
			testRect1 = new Rect (0, -1, 2, 3);
			view1 = new View (testRect1);
			testRect2 = new Rect (0, -1, 2, 3);
			dim1 = Dim.Width (view1);
			dim2 = Dim.Width (view1);
			// FIXED: Dim.Width should support Equals() and this should change to Equal.
			Assert.Equal (dim1, dim2);

			testRect1 = new Rect (0, -1, 2, 3);
			view1 = new View (testRect1);
			testRect2 = Rect.Empty;
			view2 = new View (testRect2);
			dim1 = Dim.Width (view1);
			dim2 = Dim.Width (view2);
			Assert.NotEqual (dim1, dim2);
		}

		[Fact]
		public void Height_SetsValue ()
		{
			var dim = Dim.Height (null);
			Assert.Throws<NullReferenceException> (() => dim.ToString ());

			var testVal = Rect.Empty;
			testVal = Rect.Empty;
			dim = Dim.Height (new View (testVal));
			Assert.Equal ($"DimView(side=Height, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", dim.ToString ());

			testVal = new Rect (1, 2, 3, 4);
			dim = Dim.Height (new View (testVal));
			Assert.Equal ($"DimView(side=Height, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", dim.ToString ());
		}

		// TODO: Other Dim.Height tests (e.g. Equal?)

		[Fact]
		public void Fill_SetsValue ()
		{
			var testMargin = 0;
			var dim = Dim.Fill ();
			Assert.Equal ($"Dim.Fill(margin={testMargin})", dim.ToString ());

			testMargin = 0;
			dim = Dim.Fill (testMargin);
			Assert.Equal ($"Dim.Fill(margin={testMargin})", dim.ToString ());

			testMargin = 5;
			dim = Dim.Fill (testMargin);
			Assert.Equal ($"Dim.Fill(margin={testMargin})", dim.ToString ());
		}


		[Fact]
		public void Fill_Equal ()
		{
			var margin1 = 0;
			var margin2 = 0;
			var dim1 = Dim.Fill (margin1);
			var dim2 = Dim.Fill (margin2);
			Assert.Equal (dim1, dim2);
		}

		[Fact]
		public void Percent_SetsValue ()
		{
			float f = 0;
			var dim = Dim.Percent (f);
			Assert.Equal ($"Dim.Factor(factor={f / 100:0.###}, remaining={false})", dim.ToString ());
			f = 0.5F;
			dim = Dim.Percent (f);
			Assert.Equal ($"Dim.Factor(factor={f / 100:0.###}, remaining={false})", dim.ToString ());
			f = 100;
			dim = Dim.Percent (f);
			Assert.Equal ($"Dim.Factor(factor={f / 100:0.###}, remaining={false})", dim.ToString ());
		}

		[Fact]
		public void Percent_Equals ()
		{
			float n1 = 0;
			float n2 = 0;
			var dim1 = Dim.Percent (n1);
			var dim2 = Dim.Percent (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 1;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 0.5f;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 100f;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 0.3f;
			dim1 = Dim.Percent (n1, true);
			dim2 = Dim.Percent (n2, true);
			Assert.Equal (dim1, dim2);

			n1 = n2 = 0.3f;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2, true);
			Assert.NotEqual (dim1, dim2);

			n1 = 0;
			n2 = 1;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.NotEqual (dim1, dim2);

			n1 = 0.5f;
			n2 = 1.5f;
			dim1 = Dim.Percent (n1);
			dim2 = Dim.Percent (n2);
			Assert.NotEqual (dim1, dim2);
		}

		[Fact]
		public void Percent_ThrowsOnIvalid ()
		{
			var dim = Dim.Percent (0);
			Assert.Throws<ArgumentException> (() => dim = Dim.Percent (-1));
			Assert.Throws<ArgumentException> (() => dim = Dim.Percent (101));
			Assert.Throws<ArgumentException> (() => dim = Dim.Percent (100.0001F));
			Assert.Throws<ArgumentException> (() => dim = Dim.Percent (1000001));
		}

		[Fact]
		public void Dim_Validation_Throws_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Another_Type ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var t = Application.Top;

			var w = new Window ("w") {
				Width = Dim.Fill (0),
				Height = Dim.Sized (10)
			};
			var v = new View ("v") {
				Width = Dim.Width (w) - 2,
				Height = Dim.Percent (10)
			};

			w.Add (v);
			t.Add (w);

			t.Ready += () => {
				Assert.Equal (2, w.Width = 2);
				Assert.Equal (2, w.Height = 2);
				Assert.Throws<ArgumentException> (() => v.Width = 2);
				Assert.Throws<ArgumentException> (() => v.Height = 2);
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}

		[Fact]
		public void Dim_Validation_Do_Not_Throws_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Null ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var t = Application.Top;

			var w = new Window (new Rect (1, 2, 4, 5), "w");
			t.Add (w);

			t.Ready += () => {
				Assert.Equal (3, w.Width = 3);
				Assert.Equal (4, w.Height = 4);
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}

		[Fact]
		public void Dim_Validation_Do_Not_Throws_If_NewValue_Is_DimAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var t = Application.Top;

			var w = new Window ("w") {
				Width = Dim.Fill (0),
				Height = Dim.Sized (10)
			};
			var v = new View ("v") {
				Width = Dim.Width (w) - 2,
				Height = Dim.Percent (10)
			};

			w.Add (v);
			t.Add (w);

			t.Ready += () => {
				v.LayoutStyle = LayoutStyle.Absolute;
				Assert.Equal (2, v.Width = 2);
				Assert.Equal (2, v.Height = 2);
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}

		[Fact]
		public void Only_DimAbsolute_And_DimFactor_As_A_Different_Procedure_For_Assigning_Value_To_Width_Or_Height ()
		{
			// Testing with the Button because it properly handles the Dim class.

			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var t = Application.Top;

			var w = new Window ("w") {
				Width = 100,
				Height = 100
			};

			var f1 = new FrameView ("f1") {
				X = 0,
				Y = 0,
				Width = Dim.Percent (50),
				Height = 5
			};

			var f2 = new FrameView ("f2") {
				X = Pos.Right (f1),
				Y = 0,
				Width = Dim.Fill (),
				Height = 5
			};

			var v1 = new Button ("v1") {
				X = Pos.X (f1) + 2,
				Y = Pos.Bottom (f1) + 2,
				Width = Dim.Width (f1) - 2,
				Height = Dim.Fill () - 2
			};

			var v2 = new Button ("v2") {
				X = Pos.X (f2) + 2,
				Y = Pos.Bottom (f2) + 2,
				Width = Dim.Width (f2) - 2,
				Height = Dim.Fill () - 2
			};

			var v3 = new Button ("v3") {
				Width = Dim.Percent (10),
				Height = Dim.Percent (10)
			};

			var v4 = new Button ("v4") {
				Width = Dim.Sized (50),
				Height = Dim.Sized (50)
			};

			var v5 = new Button ("v5") {
				Width = Dim.Width (v1) - Dim.Width (v3),
				Height = Dim.Height (v1) - Dim.Height (v3)
			};

			var v6 = new Button ("v6") {
				X = Pos.X (f2),
				Y = Pos.Bottom (f2) + 2,
				Width = Dim.Percent (20, true),
				Height = Dim.Percent (20, true)
			};

			w.Add (f1, f2, v1, v2, v3, v4, v5, v6);
			t.Add (w);

			t.Ready += () => {
				Assert.Equal ("Dim.Absolute(100)", w.Width.ToString ());
				Assert.Equal ("Dim.Absolute(100)", w.Height.ToString ());
				Assert.Equal (100, w.Frame.Width);
				Assert.Equal (100, w.Frame.Height);

				Assert.Equal ("Dim.Factor(factor=0.5, remaining=False)", f1.Width.ToString ());
				Assert.Equal ("Dim.Absolute(5)", f1.Height.ToString ());
				Assert.Equal (49, f1.Frame.Width); // 50-1=49
				Assert.Equal (5, f1.Frame.Height);

				Assert.Equal ("Dim.Fill(margin=0)", f2.Width.ToString ());
				Assert.Equal ("Dim.Absolute(5)", f2.Height.ToString ());
				Assert.Equal (49, f2.Frame.Width); // 50-1=49
				Assert.Equal (5, f2.Frame.Height);

				Assert.Equal ("Dim.Combine(DimView(side=Width, target=FrameView()({X=0,Y=0,Width=49,Height=5}))-Dim.Absolute(2))", v1.Width.ToString ());
				Assert.Equal ("Dim.Combine(Dim.Fill(margin=0)-Dim.Absolute(2))", v1.Height.ToString ());
				Assert.Equal (47, v1.Frame.Width); // 49-2=47
				Assert.Equal (89, v1.Frame.Height); // 98-5-2-2=89


				Assert.Equal ("Dim.Combine(DimView(side=Width, target=FrameView()({X=49,Y=0,Width=49,Height=5}))-Dim.Absolute(2))", v2.Width.ToString ());
				Assert.Equal ("Dim.Combine(Dim.Fill(margin=0)-Dim.Absolute(2))", v2.Height.ToString ());
				Assert.Equal (47, v2.Frame.Width); // 49-2=47
				Assert.Equal (89, v2.Frame.Height); // 98-5-2-2=89

				Assert.Equal ("Dim.Factor(factor=0.1, remaining=False)", v3.Width.ToString ());
				Assert.Equal ("Dim.Factor(factor=0.1, remaining=False)", v3.Height.ToString ());
				Assert.Equal (9, v3.Frame.Width); // 98*10%=9
				Assert.Equal (9, v3.Frame.Height); // 98*10%=9

				Assert.Equal ("Dim.Absolute(50)", v4.Width.ToString ());
				Assert.Equal ("Dim.Absolute(50)", v4.Height.ToString ());
				Assert.Equal (50, v4.Frame.Width);
				Assert.Equal (50, v4.Frame.Height);

				Assert.Equal ("Dim.Combine(DimView(side=Width, target=Button()({X=2,Y=7,Width=47,Height=89}))-DimView(side=Width, target=Button()({X=0,Y=0,Width=9,Height=9})))", v5.Width.ToString ());
				Assert.Equal ("Dim.Combine(DimView(side=Height, target=Button()({X=2,Y=7,Width=47,Height=89}))-DimView(side=Height, target=Button()({X=0,Y=0,Width=9,Height=9})))", v5.Height.ToString ());
				Assert.Equal (38, v5.Frame.Width); // 47-9=38
				Assert.Equal (80, v5.Frame.Height); // 89-9=80

				Assert.Equal ("Dim.Factor(factor=0.2, remaining=True)", v6.Width.ToString ());
				Assert.Equal ("Dim.Factor(factor=0.2, remaining=True)", v6.Height.ToString ());
				Assert.Equal (9, v6.Frame.Width); // 47*20%=9
				Assert.Equal (18, v6.Frame.Height); // 89*20%=18


				w.Width = 200;
				w.Height = 200;
				t.LayoutSubviews ();

				Assert.Equal ("Dim.Absolute(200)", w.Width.ToString ());
				Assert.Equal ("Dim.Absolute(200)", w.Height.ToString ());
				Assert.Equal (200, w.Frame.Width);
				Assert.Equal (200, w.Frame.Height);

				f1.Text = "Frame1";
				Assert.Equal ("Dim.Factor(factor=0.5, remaining=False)", f1.Width.ToString ());
				Assert.Equal ("Dim.Absolute(5)", f1.Height.ToString ());
				Assert.Equal (99, f1.Frame.Width); // 100-1=99
				Assert.Equal (5, f1.Frame.Height);

				f2.Text = "Frame2";
				Assert.Equal ("Dim.Fill(margin=0)", f2.Width.ToString ());
				Assert.Equal ("Dim.Absolute(5)", f2.Height.ToString ());
				Assert.Equal (99, f2.Frame.Width); // 100-1=99
				Assert.Equal (5, f2.Frame.Height);

				v1.Text = "Button1";
				Assert.Equal ("Dim.Combine(DimView(side=Width, target=FrameView()({X=0,Y=0,Width=99,Height=5}))-Dim.Absolute(2))", v1.Width.ToString ());
				Assert.Equal ("Dim.Absolute(1)", v1.Height.ToString ());
				Assert.Equal (97, v1.Frame.Width); // 99-2=97
				Assert.Equal (1, v1.Frame.Height); // 1 because is Dim.DimAbsolute

				v2.Text = "Button2";
				Assert.Equal ("Dim.Combine(DimView(side=Width, target=FrameView()({X=99,Y=0,Width=99,Height=5}))-Dim.Absolute(2))", v2.Width.ToString ());
				Assert.Equal ("Dim.Absolute(1)", v2.Height.ToString ());
				Assert.Equal (97, v2.Frame.Width); // 99-2=97
				Assert.Equal (1, v2.Frame.Height); // 1 because is Dim.DimAbsolute

				v3.Text = "Button3";
				Assert.Equal ("Dim.Factor(factor=0.1, remaining=False)", v3.Width.ToString ());
				Assert.Equal ("Dim.Absolute(1)", v3.Height.ToString ());
				Assert.Equal (19, v3.Frame.Width); // 198*10%=19 * Percent is related to the super-view if it isn't null otherwise the view width
				Assert.Equal (1, v3.Frame.Height); // 1 because is Dim.DimAbsolute

				v4.Text = "Button4";
				v4.AutoSize = false;
				Assert.Equal ("Dim.Absolute(50)", v4.Width.ToString ());
				Assert.Equal ("Dim.Absolute(1)", v4.Height.ToString ());
				v4.AutoSize = true;
				Assert.Equal ("Dim.Absolute(11)", v4.Width.ToString ());
				Assert.Equal ("Dim.Absolute(1)", v4.Height.ToString ());
				Assert.Equal (11, v4.Frame.Width); // 11 is the text length and because is Dim.DimAbsolute
				Assert.Equal (1, v4.Frame.Height); // 1 because is Dim.DimAbsolute

				v5.Text = "Button5";
				Assert.Equal ("Dim.Combine(DimView(side=Width, target=Button()({X=2,Y=7,Width=97,Height=1}))-DimView(side=Width, target=Button()({X=0,Y=0,Width=19,Height=1})))", v5.Width.ToString ());
				Assert.Equal ("Dim.Absolute(1)", v5.Height.ToString ());
				Assert.Equal (78, v5.Frame.Width); // 97-19=78
				Assert.Equal (1, v5.Frame.Height); // 1 because is Dim.DimAbsolute

				v6.Text = "Button6";
				Assert.Equal ("Dim.Factor(factor=0.2, remaining=True)", v6.Width.ToString ());
				Assert.Equal ("Dim.Absolute(1)", v6.Height.ToString ());
				Assert.Equal (19, v6.Frame.Width); // 99*20%=19
				Assert.Equal (1, v6.Frame.Height); // 1 because is Dim.DimAbsolute
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}

		// DONE: Test operators
		[Fact]
		public void DimCombine_Do_Not_Throws ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var t = Application.Top;

			var w = new Window ("w") {
				Width = Dim.Width (t) - 2,
				Height = Dim.Height (t) - 2
			};
			var f = new FrameView ("f");
			var v1 = new View ("v1") {
				Width = Dim.Width (w) - 2,
				Height = Dim.Height (w) - 2
			};
			var v2 = new View ("v2") {
				Width = Dim.Width (v1) - 2,
				Height = Dim.Height (v1) - 2
			};

			f.Add (v1, v2);
			w.Add (f);
			t.Add (w);

			f.Width = Dim.Width (t) - Dim.Width (v2);
			f.Height = Dim.Height (t) - Dim.Height (v2);

			t.Ready += () => {
				Assert.Equal (80, t.Frame.Width);
				Assert.Equal (25, t.Frame.Height);
				Assert.Equal (78, w.Frame.Width);
				Assert.Equal (23, w.Frame.Height);
				Assert.Equal (6, f.Frame.Width);
				Assert.Equal (6, f.Frame.Height);
				Assert.Equal (76, v1.Frame.Width);
				Assert.Equal (21, v1.Frame.Height);
				Assert.Equal (74, v2.Frame.Width);
				Assert.Equal (19, v2.Frame.Height);
			};

			Application.Iteration += () => Application.RequestStop ();

			Application.Run ();
			Application.Shutdown ();
		}

		[Fact]
		public void PosCombine_Will_Throws ()
		{
			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var t = Application.Top;

			var w = new Window ("w") {
				Width = Dim.Width (t) - 2,
				Height = Dim.Height (t) - 2
			};
			var f = new FrameView ("f");
			var v1 = new View ("v1") {
				Width = Dim.Width (w) - 2,
				Height = Dim.Height (w) - 2
			};
			var v2 = new View ("v2") {
				Width = Dim.Width (v1) - 2,
				Height = Dim.Height (v1) - 2
			};

			f.Add (v1); // v2 not added
			w.Add (f);
			t.Add (w);

			f.Width = Dim.Width (t) - Dim.Width (v2);
			f.Height = Dim.Height (t) - Dim.Height (v2);

			Assert.Throws<InvalidOperationException> (() => Application.Run ());
			Application.Shutdown ();
		}


		[Fact]
		public void Dim_Add_Operator ()
		{

			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var top = Application.Top;

			var view = new View () { X = 0, Y = 0, Width = 20, Height = 0 };
			var field = new TextField () { X = 0, Y = Pos.Bottom (view), Width = 20 };
			var count = 0;

			field.KeyDown += (k) => {
				if (k.KeyEvent.Key == Key.Enter) {
					field.Text = $"Label {count}";
					var label = new Label (field.Text) { X = 0, Y = view.Bounds.Height, Width = 20 };
					view.Add (label);
					Assert.Equal ($"Label {count}", label.Text);
					Assert.Equal ($"Pos.Absolute({count})", label.Y.ToString ());

					Assert.Equal ($"Dim.Absolute({count})", view.Height.ToString ());
					view.Height += 1;
					count++;
					Assert.Equal ($"Dim.Absolute({count})", view.Height.ToString ());
				}
			};

			Application.Iteration += () => {
				while (count < 20) {
					field.OnKeyDown (new KeyEvent (Key.Enter, new KeyModifiers ()));
				}

				Application.RequestStop ();
			};

			var win = new Window ();
			win.Add (view);
			win.Add (field);

			top.Add (win);

			Application.Run (top);

			Assert.Equal (20, count);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

		[Fact]
		public void Dim_Subtract_Operator ()
		{

			Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));

			var top = Application.Top;

			var view = new View () { X = 0, Y = 0, Width = 20, Height = 0 };
			var field = new TextField () { X = 0, Y = Pos.Bottom (view), Width = 20 };
			var count = 20;
			var listLabels = new List<Label> ();

			for (int i = 0; i < count; i++) {
				field.Text = $"Label {i}";
				var label = new Label (field.Text) { X = 0, Y = view.Bounds.Height, Width = 20 };
				view.Add (label);
				Assert.Equal ($"Label {i}", label.Text);
				Assert.Equal ($"Pos.Absolute({i})", label.Y.ToString ());
				listLabels.Add (label);

				Assert.Equal ($"Dim.Absolute({i})", view.Height.ToString ());
				view.Height += 1;
				Assert.Equal ($"Dim.Absolute({i + 1})", view.Height.ToString ());
			}

			field.KeyDown += (k) => {
				if (k.KeyEvent.Key == Key.Enter) {
					Assert.Equal ($"Label {count - 1}", listLabels [count - 1].Text);
					view.Remove (listLabels [count - 1]);

					Assert.Equal ($"Dim.Absolute({count})", view.Height.ToString ());
					view.Height -= 1;
					count--;
					Assert.Equal ($"Dim.Absolute({count})", view.Height.ToString ());
				}
			};

			Application.Iteration += () => {
				while (count > 0) {
					field.OnKeyDown (new KeyEvent (Key.Enter, new KeyModifiers ()));
				}

				Application.RequestStop ();
			};

			var win = new Window ();
			win.Add (view);
			win.Add (field);

			top.Add (win);

			Application.Run (top);

			Assert.Equal (0, count);

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();

		}
	}
}
