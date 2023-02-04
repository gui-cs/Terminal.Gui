using System;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.UI.FakeConsole;

namespace Terminal.UI.DriverTests {
	public class ColorTests {

		[Theory]
		[InlineData (typeof (FakeDriver))]
		//[InlineData (typeof (NetDriver))]
		//[InlineData (typeof (CursesDriver))]
		//[InlineData (typeof (WindowsDriver))]
		public void SetColors_Changes_Colors (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);
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

			// Shutdown must be called to safely clean up Application if Init has been called
			Application.Shutdown ();
		}

	}
}