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
	public class DimTests {
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
			Application.Init (new FakeDriver (), new NetMainLoop (() => FakeConsole.ReadKey (true)));

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
			Application.Init (new FakeDriver (), new NetMainLoop (() => FakeConsole.ReadKey (true)));

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
			Application.Init (new FakeDriver (), new NetMainLoop (() => FakeConsole.ReadKey (true)));

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


		// TODO: Test operators
	}
}
