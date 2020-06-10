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
	public class PosTests {
		[Fact]
		public void New_Works ()
		{
			var pos = new Pos ();
			Assert.Equal ("Terminal.Gui.Pos", pos.ToString ());
		}

		[Fact]
		public void AnchorEnd_SetsValue ()
		{
			var n = 0;
			var pos = Pos.AnchorEnd ();
			Assert.Equal ($"Pos.AnchorEnd(margin={n})", pos.ToString ());

			n = 5;
			pos = Pos.AnchorEnd (n);
			Assert.Equal ($"Pos.AnchorEnd(margin={n})", pos.ToString ());
		}

		[Fact]
		public void AnchorEnd_Equal ()
		{
			var n1 = 0;
			var n2 = 0;

			var pos1 = Pos.AnchorEnd (n1);
			var pos2 = Pos.AnchorEnd (n2);
			Assert.Equal (pos1, pos2);

			// Test inequality
			n2 = 5;
			pos2 = Pos.AnchorEnd (n2);
			Assert.NotEqual (pos1, pos2);
		}

		[Fact]
		public void At_SetsValue ()
		{
			var pos = Pos.At (0);
			Assert.Equal ("Pos.Absolute(0)", pos.ToString ());

			pos = Pos.At (5);
			Assert.Equal ("Pos.Absolute(5)", pos.ToString ());

			//Assert.Throws<ArgumentException> (() => pos = Pos.At (-1));
		}

		[Fact]
		public void At_Equal ()
		{
			var n1 = 0;
			var n2 = 0;

			var pos1 = Pos.At (n1);
			var pos2 = Pos.At (n2);
			Assert.Equal (pos1, pos2);
		}

		[Fact]
		public void Left_SetsValue ()
		{
			var pos = Pos.Left (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			var testVal = Rect.Empty;
			pos = Pos.Left (new View ());
			Assert.Equal ($"Pos.View(side=x, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());

			pos = Pos.Left (new View (testVal));
			Assert.Equal ($"Pos.View(side=x, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());

			testVal = new Rect (1, 2, 3, 4);
			pos = Pos.Left (new View (testVal));
			Assert.Equal ($"Pos.View(side=x, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());
		}

		// TODO: Test Left, Top, Right bottom Equal

		[Fact]
		public void Top_SetsValue ()
		{
			var pos = Pos.Top (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			var testVal = Rect.Empty;
			pos = Pos.Top (new View ());
			Assert.Equal ($"Pos.View(side=y, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());

			testVal = new Rect (1, 2, 3, 4);
			pos = Pos.Top (new View (testVal));
			Assert.Equal ($"Pos.View(side=y, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());
		}

		[Fact]
		public void Right_SetsValue ()
		{
			var pos = Pos.Right (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			var testVal = Rect.Empty;
			pos = Pos.Right (new View ());
			Assert.Equal ($"Pos.View(side=right, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());

			testVal = Rect.Empty;
			pos = Pos.Right (new View (testVal));
			Assert.Equal ($"Pos.View(side=right, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());

			testVal = new Rect (1, 2, 3, 4);
			pos = Pos.Right (new View (testVal));
			Assert.Equal ($"Pos.View(side=right, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());
		}

		[Fact]
		public void Bottom_SetsValue ()
		{
			var pos = Pos.Bottom (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			var testVal = Rect.Empty;
			pos = Pos.Bottom (new View ());
			Assert.Equal ($"Pos.View(side=bottom, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());

			testVal = Rect.Empty;
			pos = Pos.Bottom (new View (testVal));
			Assert.Equal ($"Pos.View(side=bottom, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());

			testVal = new Rect (1, 2, 3, 4);
			pos = Pos.Bottom (new View (testVal));
			Assert.Equal ($"Pos.View(side=bottom, target=View()({{X={testVal.X},Y={testVal.Y},Width={testVal.Width},Height={testVal.Height}}}))", pos.ToString ());

			//Assert.Throws<ArgumentException> (() => pos = Pos.Bottom (new View (new Rect (0, 0, -3, -4))));
		}

		[Fact]
		public void Center_SetsValue ()
		{
			var pos = Pos.Center ();
			Assert.Equal ("Pos.Center", pos.ToString ());
		}

		[Fact]
		public void Percent_SetsValue ()
		{
			float f = 0;
			var pos = Pos.Percent (f);
			Assert.Equal ($"Pos.Factor({f / 100:0.###})", pos.ToString ());
			f = 0.5F;
			pos = Pos.Percent (f);
			Assert.Equal ($"Pos.Factor({f / 100:0.###})", pos.ToString ());
			f = 100;
			pos = Pos.Percent (f);
			Assert.Equal ($"Pos.Factor({f / 100:0.###})", pos.ToString ());
		}

		[Fact]
		public void Percent_Equal ()
		{
			var n1 = 0;
			var n2 = 0;
			var pos1 = Pos.Percent (n1);
			var pos2 = Pos.Percent (n2);
			// BUGBUG: Pos.Percent should support equality 
			Assert.NotEqual (pos1, pos2);
		}

		[Fact]
		public void Percent_ThrowsOnIvalid ()
		{
			var pos = Pos.Percent (0);
			Assert.Throws<ArgumentException> (() => pos = Pos.Percent (-1));
			Assert.Throws<ArgumentException> (() => pos = Pos.Percent (101));
			Assert.Throws<ArgumentException> (() => pos = Pos.Percent (100.0001F));
			Assert.Throws<ArgumentException> (() => pos = Pos.Percent (1000001));
		}

		// TODO: Test operators
	}
}
