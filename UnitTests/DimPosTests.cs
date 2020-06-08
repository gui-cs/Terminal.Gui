using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Terminal.Gui;
using Xunit;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.MockConsole;

namespace Terminal.Gui {
	public class DimPosTests {
		[Fact]
		public void TestNew ()
		{
			var pos = new Pos ();
			Assert.Equal ("Terminal.Gui.Pos", pos.ToString ());
		}

		[Fact]
		public void TestAnchorEnd ()
		{
			var pos = Pos.AnchorEnd ();
			Assert.Equal ("Pos.AnchorEnd(margin=0)", pos.ToString ());

			pos = Pos.AnchorEnd (5);
			Assert.Equal ("Pos.AnchorEnd(margin=5)", pos.ToString ());
		}

		[Fact]
		public void TestAt ()
		{
			var pos = Pos.At (0);
			Assert.Equal ("Pos.Absolute(0)", pos.ToString ());

			pos = Pos.At (5);
			Assert.Equal ("Pos.Absolute(5)", pos.ToString ());

			//Assert.Throws<ArgumentException> (() => pos = Pos.At (-1));
		}

		[Fact]
		public void TestLeft ()
		{
			var pos = Pos.Left (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			pos = Pos.Left (new View ());
			Assert.Equal ("Pos.View(side=x, target=View()({X=0,Y=0,Width=0,Height=0})", pos.ToString ());

			pos = Pos.Left (new View (new Rect (1, 2, 3, 4)));
			Assert.Equal ("Pos.View(side=x, target=View()({X=1,Y=2,Width=3,Height=4})", pos.ToString ());
		}

		[Fact]
		public void TestTop ()
		{
			var pos = Pos.Top (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			pos = Pos.Top (new View ());
			Assert.Equal ("Pos.View(side=y, target=View()({X=0,Y=0,Width=0,Height=0})", pos.ToString ());

			pos = Pos.Top (new View (new Rect (1, 2, 3, 4)));
			Assert.Equal ("Pos.View(side=y, target=View()({X=1,Y=2,Width=3,Height=4})", pos.ToString ());
		}

		[Fact]
		public void TestRight ()
		{
			var pos = Pos.Right (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			pos = Pos.Right (new View ());
			Assert.Equal ("Pos.View(side=right, target=View()({X=0,Y=0,Width=0,Height=0})", pos.ToString ());

			pos = Pos.Right (new View (new Rect (1, 2, 3, 4)));
			Assert.Equal ("Pos.View(side=right, target=View()({X=1,Y=2,Width=3,Height=4})", pos.ToString ());
		}

		[Fact]
		public void TestBottom ()
		{
			var pos = Pos.Bottom (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			pos = Pos.Bottom (new View ());
			Assert.Equal ("Pos.View(side=bottom, target=View()({X=0,Y=0,Width=0,Height=0})", pos.ToString ());

			pos = Pos.Bottom (new View (new Rect (1, 2, 3, 4)));
			Assert.Equal ("Pos.View(side=bottom, target=View()({X=1,Y=2,Width=3,Height=4})", pos.ToString ());

			//Assert.Throws<ArgumentException> (() => pos = Pos.Bottom (new View (new Rect (0, 0, -3, -4))));

		}

		[Fact]
		public void TestCenter ()
		{
			var pos = Pos.Center ();
			Assert.Equal ("Pos.Center", pos.ToString ());
		}

		[Fact]
		public void TestPercent ()
		{
			var pos = Pos.Percent (0);
			Assert.Equal ("Pos.Factor(0)", pos.ToString ());

			pos = Pos.Percent (0.5F);
			Assert.Equal ("Pos.Factor(0.005)", pos.ToString ());

			pos = Pos.Percent (100);
			Assert.Equal ("Pos.Factor(1)", pos.ToString ());

			Assert.Throws<ArgumentException> (() => pos = Pos.Percent (-1));
			Assert.Throws<ArgumentException> (() => pos = Pos.Percent (101));
			Assert.Throws<ArgumentException> (() => pos = Pos.Percent (100.0001F));
			Assert.Throws<ArgumentException> (() => pos = Pos.Percent (1000001));
		}

		// TODO: Test operators

		// TODO: Test Dim
	}
}
