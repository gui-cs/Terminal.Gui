using System;
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

			var attr = new Attribute (fg, bg);

			Assert.Equal (fg, attr.TrueColorForeground);
			Assert.Equal (bg, attr.TrueColorBackground);

			// Test unified color
			attr = new Attribute (fg,fg);
			Assert.Equal (fg, attr.TrueColorForeground);
			Assert.Equal (fg, attr.TrueColorBackground);

			attr = new Attribute (bg,bg);
			Assert.Equal (bg, attr.TrueColorForeground);
			Assert.Equal (bg, attr.TrueColorBackground);

			driver.End ();
			Application.Shutdown ();
		}

		[InlineData (new object [] { 127, 0, 0, Color.Red })]
		[InlineData(new object [] { 128, 0, 0, Color.Red})]
		[InlineData (new object [] { 130, 0, 0, Color.Red })]
		[InlineData (new object [] { 200, 0, 0, Color.BrightRed })]
		[InlineData (new object [] { 245, 0, 0, Color.BrightRed })]
		[InlineData (new object [] { 255, 0, 0, Color.BrightRed })]

		[InlineData (new object [] { 0, 128, 0, Color.Green })]
		//[InlineData (new object [] { 128, 128, 0, Color.Brown })] // TODO : This was an original test in the PR I have kept it but current it does not map to Brown (it maps to BrightYellow)
		[InlineData (new object [] { 0, 0, 128, Color.Blue })]
		[InlineData (new object [] { 128, 0, 128, Color.Magenta })]
		[InlineData (new object [] { 0, 128, 128, Color.Cyan })]
		[InlineData (new object [] { 0, 255, 0, Color.BrightGreen })]
		[InlineData (new object [] { 255, 255, 0, Color.BrightYellow })]
		[InlineData (new object [] { 0, 0, 255, Color.BrightBlue })]
		[InlineData (new object [] { 255, 0, 255, Color.BrightMagenta })]
		[InlineData (new object [] { 0, 255, 255, Color.BrightCyan })]
		[InlineData (new object [] { 128, 128, 128, Color.DarkGray })]
		[InlineData (new object [] { 255, 255, 255, Color.White })]
		[InlineData (new object [] { 192, 192, 192, Color.Gray })]
		[InlineData (new object [] { 0, 0, 0, Color.Black })]
		[Theory, AutoInitShutdown]
		public void Basic_Colors_Fallback (int r, int g, int b, Color expectedColor)
		{
			// Test foreground color property
			var attr = new Attribute (new TrueColor (r, g, b), new TrueColor (0,0,0));
			Assert.Equal (expectedColor, attr.Foreground);
			Assert.Equal (Color.Black, attr.Background);
			
			// Test background color property
			attr = new Attribute (new TrueColor (0, 0, 0), new TrueColor (r, g, b));
			Assert.Equal (Color.Black, attr.Foreground);
			Assert.Equal (expectedColor, attr.Background);

			// Test 5 up
			attr = new Attribute (new TrueColor (Math.Min (255,r+5), Math.Min(255,g+5), Math.Min (255, b+5)), new TrueColor (0, 0, 0));
			Assert.Equal (expectedColor, attr.Foreground);
			Assert.Equal (Color.Black, attr.Background);

			// Test 5 down
			attr = new Attribute (new TrueColor (Math.Max (0, r - 5), Math.Max (0, g - 5), Math.Max (0, b - 5)), new TrueColor (0, 0, 0));
			Assert.Equal (expectedColor, attr.Foreground);
			Assert.Equal (Color.Black, attr.Background);
		}
	}
}