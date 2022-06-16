using System;
using Xunit;
using Xunit.Abstractions;
using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.Core {
	public class WindowTests {
		readonly ITestOutputHelper output;

		public WindowTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void New_Initializes ()
		{
			// Parameterless
			var r = new Window ();
			Assert.NotNull (r);
			Assert.Null (r.Title);
			Assert.Equal (LayoutStyle.Computed, r.LayoutStyle);
			Assert.Equal ("Window()({X=0,Y=0,Width=0,Height=0})", r.ToString ());
			Assert.True (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
			Assert.Null (r.Focused);
			Assert.NotNull (r.ColorScheme);
			Assert.Equal (Dim.Fill (0), r.Width);
			Assert.Equal (Dim.Fill (0), r.Height);
			// FIXED: Pos needs equality implemented
			Assert.Equal (Pos.At (0), r.X);
			Assert.Equal (Pos.At (0), r.Y);
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.NotEmpty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

			// Empty Rect
			r = new Window (Rect.Empty, "title");
			Assert.NotNull (r);
			Assert.Equal ("title", r.Title);
			Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
			Assert.Equal ("Window()({X=0,Y=0,Width=0,Height=0})", r.ToString ());
			Assert.True (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
			Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
			Assert.Null (r.Focused);
			Assert.NotNull (r.ColorScheme);
			Assert.NotNull (r.Width);       // All view Dim are initialized now,
			Assert.NotNull (r.Height);      // avoiding Dim errors.
			Assert.NotNull (r.X);           // All view Pos are initialized now,
			Assert.NotNull (r.Y);           // avoiding Pos errors.
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.NotEmpty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);

			// Rect with values
			r = new Window (new Rect (1, 2, 3, 4), "title");
			Assert.Equal ("title", r.Title);
			Assert.NotNull (r);
			Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
			Assert.Equal ("Window()({X=1,Y=2,Width=3,Height=4})", r.ToString ());
			Assert.True (r.CanFocus);
			Assert.False (r.HasFocus);
			Assert.Equal (new Rect (0, 0, 3, 4), r.Bounds);
			Assert.Equal (new Rect (1, 2, 3, 4), r.Frame);
			Assert.Null (r.Focused);
			Assert.NotNull (r.ColorScheme);
			Assert.NotNull (r.Width);
			Assert.NotNull (r.Height);
			Assert.NotNull (r.X);
			Assert.NotNull (r.Y);
			Assert.False (r.IsCurrentTop);
			Assert.Empty (r.Id);
			Assert.NotEmpty (r.Subviews);
			Assert.False (r.WantContinuousButtonPressed);
			Assert.False (r.WantMousePositionReports);
			Assert.Null (r.SuperView);
			Assert.Null (r.MostFocused);
			Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
			r.Dispose();
		}

		[Fact]
		public void Set_Title_Fires_TitleChanging ()
		{
			var r = new Window ();
			Assert.Null (r.Title);

			string expectedAfter = null;
			string expectedDuring = null;
			bool cancel = false;
			r.TitleChanging += (args) => {
				Assert.Equal (expectedDuring, args.NewTitle);
				args.Cancel = cancel;
			};

			r.Title = expectedDuring = expectedAfter = "title";
			Assert.Equal (expectedAfter, r.Title.ToString());

			r.Title = expectedDuring = expectedAfter = "a different title";
			Assert.Equal (expectedAfter, r.Title.ToString ());

			// Now setup cancelling the change and change it back to "title"
			cancel = true;
			r.Title = expectedDuring = "title";
			Assert.Equal (expectedAfter, r.Title.ToString ());
			r.Dispose ();

		}

		[Fact]
		public void Set_Title_Fires_TitleChanged ()
		{
			var r = new Window ();
			Assert.Null (r.Title);

			string expected = null;
			r.TitleChanged += (args) => {
				Assert.Equal (r.Title, args.NewTitle);
			};

			expected = "title";
			r.Title = expected;
			Assert.Equal (expected, r.Title.ToString ());

			expected = "another title";
			r.Title = expected;
			Assert.Equal (expected, r.Title.ToString ());
			r.Dispose ();
		}
	}
}
