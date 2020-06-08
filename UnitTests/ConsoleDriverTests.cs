using System;
using Terminal.Gui;
using Xunit;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.MockConsole;

namespace Terminal.Gui {
	public class ConsoleDriverTests {
		[Fact]
		public void TestInit ()
		{
			var driver = new MockDriver ();
			driver.Init (() => { });

			Assert.Equal (80, Console.BufferWidth);
			Assert.Equal (25, Console.BufferHeight);

			// MockDriver is always 80x25
			Assert.Equal (Console.BufferWidth, driver.Cols);
			Assert.Equal (Console.BufferHeight, driver.Rows);
			driver.End ();
		}

		[Fact]
		public void TestEnd ()
		{
			var driver = new MockDriver ();
			driver.Init (() => { });

			MockConsole.ForegroundColor = ConsoleColor.Red;
			Assert.Equal (ConsoleColor.Red, Console.ForegroundColor);

			MockConsole.BackgroundColor = ConsoleColor.Green;
			Assert.Equal (ConsoleColor.Green, Console.BackgroundColor);
			driver.Move (2, 3);
			Assert.Equal (2, Console.CursorLeft);
			Assert.Equal (3, Console.CursorTop);

			driver.End ();
			Assert.Equal (0, Console.CursorLeft);
			Assert.Equal (0, Console.CursorTop);
			Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
			Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);
		}

		[Fact]
		public void TestSetColors ()
		{
			var driver = new MockDriver ();
			driver.Init (() => { });
			Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
			Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);

			Console.ForegroundColor = ConsoleColor.Red;
			Assert.Equal (ConsoleColor.Red, Console.ForegroundColor);

			Console.BackgroundColor = ConsoleColor.Green;
			Assert.Equal (ConsoleColor.Green, Console.BackgroundColor);

			Console.ResetColor ();
			Assert.Equal (ConsoleColor.Gray, Console.ForegroundColor);
			Assert.Equal (ConsoleColor.Black, Console.BackgroundColor);
			driver.End ();
		}
	}
}
