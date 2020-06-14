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
			// BUGBUG: Dim.Width should support Equals() and this should change to Euqal.
			Assert.NotEqual (dim1, dim2);

			dim2 = Dim.Width (view2);
			Assert.NotEqual (dim1, dim2);

			testRect1 = new Rect (0, 1, 2, 3);
			view1 = new View (testRect1);
			testRect2 = new Rect (0, 1, 2, 3);
			dim1 = Dim.Width (view1);
			dim2 = Dim.Width (view1);
			// BUGBUG: Dim.Width should support Equals() and this should change to Euqal.
			Assert.NotEqual (dim1, dim2);

			testRect1 = new Rect (0, -1, -2, -3);
			view1 = new View (testRect1);
			testRect2 = new Rect (0, -1, -2, -3);
			dim1 = Dim.Width (view1);
			dim2 = Dim.Width (view1);
			// BUGBUG: Dim.Width should support Equals() and this should change to Euqal.
			Assert.NotEqual (dim1, dim2);

			testRect1 = new Rect (0, -1, -2, -3);
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
			Assert.Equal ($"Dim.Factor({f / 100:0.###})", dim.ToString ());
			f = 0.5F;
			dim = Dim.Percent (f);
			Assert.Equal ($"Dim.Factor({f / 100:0.###})", dim.ToString ());
			f = 100;
			dim = Dim.Percent (f);
			Assert.Equal ($"Dim.Factor({f / 100:0.###})", dim.ToString ());
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

		// TODO: Test operators
	}
}
