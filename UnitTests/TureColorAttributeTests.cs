using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ConsoleDrivers {

	public class TrueColorAttributeTests {

		[Fact]
		public void Constuctors_Constuct ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });

			// Test foreground, background
			var fg = new TrueColor (255, 0, 0);
			var bg = new TrueColor (0, 255, 0);

			var attr = new TrueColorAttribute (fg, bg);

			Assert.Equal (fg, attr.TrueColorForeground);
			Assert.Equal (bg, attr.TrueColorBackground);

			// Test unified color
			attr = new TrueColorAttribute (fg);
			Assert.Equal (fg, attr.TrueColorForeground);
			Assert.Equal (fg, attr.TrueColorBackground);

			attr = new TrueColorAttribute (bg);
			Assert.Equal (bg, attr.TrueColorForeground);
			Assert.Equal (bg, attr.TrueColorBackground);

			driver.End ();
			Application.Shutdown ();
		}

		[Fact]
		public void Basic_Colors_Fallback ()
		{
			var driver = new FakeDriver ();
			Application.Init (driver, new FakeMainLoop (() => FakeConsole.ReadKey (true)));
			driver.Init (() => { });

			// Test bright basic colors
			var attr = new TrueColorAttribute (new TrueColor (128, 0, 0), new TrueColor (0, 128, 0));
			Assert.Equal (Color.Red, attr.Foreground);
			Assert.Equal (Color.Green, attr.Background);

			attr = new TrueColorAttribute (new TrueColor (128, 128, 0), new TrueColor (0, 0, 128));
			Assert.Equal (Color.Brown, attr.Foreground);
			Assert.Equal (Color.Blue, attr.Background);

			attr = new TrueColorAttribute (new TrueColor (128, 0, 128), new TrueColor (0, 128, 128));
			Assert.Equal (Color.Magenta, attr.Foreground);
			Assert.Equal (Color.Cyan, attr.Background);

			// Test basic colors
			attr = new TrueColorAttribute (new TrueColor (255, 0, 0), new TrueColor (0, 255, 0));
			Assert.Equal (Color.BrightRed, attr.Foreground);
			Assert.Equal (Color.BrightGreen, attr.Background);

			attr = new TrueColorAttribute (new TrueColor (255, 255, 0), new TrueColor (0, 0, 255));
			Assert.Equal (Color.BrightYellow, attr.Foreground);
			Assert.Equal (Color.BrightBlue, attr.Background);

			attr = new TrueColorAttribute (new TrueColor (255, 0, 255), new TrueColor (0, 255, 255));
			Assert.Equal (Color.BrightMagenta, attr.Foreground);
			Assert.Equal (Color.BrightCyan, attr.Background);

			// Test gray basic colors
			attr = new TrueColorAttribute (new TrueColor (128, 128, 128), new TrueColor (255, 255, 255));
			Assert.Equal (Color.DarkGray, attr.Foreground);
			Assert.Equal (Color.White, attr.Background);

			attr = new TrueColorAttribute (new TrueColor (192, 192, 192), new TrueColor (0, 0, 0));
			Assert.Equal (Color.Gray, attr.Foreground);
			Assert.Equal (Color.Black, attr.Background);

			driver.End ();
			Application.Shutdown ();
		}
	}
}