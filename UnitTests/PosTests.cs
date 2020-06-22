using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
		public void AnchorEnd_Negative_Throws ()
		{
			Pos pos;
			var n = -1;
			Assert.Throws<ArgumentException> (() => pos = Pos.AnchorEnd (n));
		}

		[Fact]
		public void At_SetsValue ()
		{
			var pos = Pos.At (0);
			Assert.Equal ("Pos.Absolute(0)", pos.ToString ());

			pos = Pos.At (5);
			Assert.Equal ("Pos.Absolute(5)", pos.ToString ());

			pos = Pos.At (-1);
			Assert.Equal ("Pos.Absolute(-1)", pos.ToString ());
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
		public void SetSide_Null_Throws ()
		{
			var pos = Pos.Left (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			pos = Pos.X (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			pos = Pos.Top (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			pos = Pos.Y(null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			pos = Pos.Bottom (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());

			pos = Pos.Right (null);
			Assert.Throws<NullReferenceException> (() => pos.ToString ());
		}

		// TODO: Test Left, Top, Right bottom Equal

		/// <summary>
		/// Tests Pos.Left, Pos.X, Pos.Top, Pos.Y, Pos.Right, and Pos.Bottom set operations
		/// </summary>
		[Fact]
		public void PosSide_SetsValue ()
		{
			string side; // used in format string
			var testRect = Rect.Empty;
			var testInt = 0;
			Pos pos; 

			// Pos.Left
			side = "x";
			testInt = 0;
			testRect = Rect.Empty;
			pos = Pos.Left (new View ());
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			pos = Pos.Left (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testRect = new Rect (1, 2, 3, 4);
			pos = Pos.Left (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			// Pos.Left(win) + 0
			pos = Pos.Left (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = 1;
			// Pos.Left(win) +1
			pos = Pos.Left (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = -1;
			// Pos.Left(win) -1
			pos = Pos.Left (new View (testRect)) - testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			// Pos.X
			side = "x";
			testInt = 0;
			testRect = Rect.Empty;
			pos = Pos.X (new View ());
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			pos = Pos.X (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testRect = new Rect (1, 2, 3, 4);
			pos = Pos.X (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			// Pos.X(win) + 0
			pos = Pos.X (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = 1;
			// Pos.X(win) +1
			pos = Pos.X (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = -1;
			// Pos.X(win) -1
			pos = Pos.X (new View (testRect)) - testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			// Pos.Top
			side = "y";
			testInt = 0;
			testRect = Rect.Empty;
			pos = Pos.Top (new View ());
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			pos = Pos.Top (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testRect = new Rect (1, 2, 3, 4);
			pos = Pos.Top (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			// Pos.Top(win) + 0
			pos = Pos.Top (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = 1;
			// Pos.Top(win) +1
			pos = Pos.Top (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = -1;
			// Pos.Top(win) -1
			pos = Pos.Top (new View (testRect)) - testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			// Pos.Y
			side = "y";
			testInt = 0;
			testRect = Rect.Empty;
			pos = Pos.Y (new View ());
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			pos = Pos.Y (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testRect = new Rect (1, 2, 3, 4);
			pos = Pos.Y (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			// Pos.Y(win) + 0
			pos = Pos.Y (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = 1;
			// Pos.Y(win) +1
			pos = Pos.Y (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = -1;
			// Pos.Y(win) -1
			pos = Pos.Y (new View (testRect)) - testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			// Pos.Bottom
			side = "bottom";
			testRect = Rect.Empty;
			testInt = 0;
			pos = Pos.Bottom (new View ());
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			pos = Pos.Bottom (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testRect = new Rect (1, 2, 3, 4);
			pos = Pos.Bottom (new View (testRect));
			Assert.Equal ($"Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}})){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			// Pos.Bottom(win) + 0
			pos = Pos.Bottom (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = 1;
			// Pos.Bottom(win) +1
			pos = Pos.Bottom (new View (testRect)) + testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());

			testInt = -1;
			// Pos.Bottom(win) -1
			pos = Pos.Bottom (new View (testRect)) - testInt;
			Assert.Equal ($"Pos.Combine(Pos.Combine(Pos.View(side={side}, target=View()({{X={testRect.X},Y={testRect.Y},Width={testRect.Width},Height={testRect.Height}}}))+Pos.Absolute(0)){(testInt < 0 ? '-' : '+')}Pos.Absolute({testInt}))", pos.ToString ());
		}

		// See: https://github.com/migueldeicaza/gui.cs/issues/504
		[Fact]
		public void LeftTopBottomRight_Win_ShouldNotThrow ()
		{
			// Setup Fake driver
			(Window win, Button button) setup ()
			{
				Application.Init (new FakeDriver (), new NetMainLoop (() => FakeConsole.ReadKey (true)));
				Application.Iteration = () => {
					Application.RequestStop ();
				};
				var win = new Window ("window") {
					X = 0,
					Y = 0,
					Width = Dim.Fill (),
					Height = Dim.Fill (),
				};
				Application.Top.Add (win);

				var button = new Button ("button") {
					X = Pos.Center (),
				};
				win.Add (button);

				return (win, button);
			}

			void cleanup ()
			{
				// Cleanup
				Application.Shutdown ();
			}

			// Test cases:
			var app = setup ();
			app.button.Y = Pos.Left (app.win);
			Application.Run ();
			cleanup ();

			app = setup ();
			app.button.Y = Pos.X (app.win);
			Application.Run ();
			cleanup ();

			app = setup ();
			app.button.Y = Pos.Top (app.win);
			Application.Run ();
			cleanup ();

			app = setup ();
			app.button.Y = Pos.Y (app.win);
			Application.Run ();
			cleanup ();

			app = setup ();
			app.button.Y = Pos.Bottom (app.win);
			Application.Run ();
			cleanup ();

			app = setup ();
			app.button.Y = Pos.Right (app.win);
			Application.Run ();
			cleanup ();

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

		// TODO: Test PosCombine


		// TODO: Test operators
	}
}
