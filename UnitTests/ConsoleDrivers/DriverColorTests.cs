using System;
using Xunit;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests {
	public class DriverColorTests {
		public DriverColorTests ()
		{
			ConsoleDriver.RunningUnitTests = true;
		}
		
		[Theory]
		[InlineData (typeof (FakeDriver))]
		[InlineData (typeof (NetDriver))]
		[InlineData (typeof (CursesDriver))]
		[InlineData (typeof (WindowsDriver))]
		public void SetColors_Changes_Colors (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			driver.Init ();

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


		[Theory]
		[InlineData (typeof (FakeDriver), false)]
		[InlineData (typeof (NetDriver), true)]
		[InlineData (typeof (CursesDriver), false)]
		[InlineData (typeof (WindowsDriver), true)] // Because we're not Windows Terminal
		public void SupportsTrueColor_Defaults (Type driverType, bool expectedSetting)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			driver.Init ();

			Assert.Equal (expectedSetting, driver.SupportsTrueColor);

			driver.End ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		[InlineData (typeof (NetDriver))]
		[InlineData (typeof (CursesDriver))]
		[InlineData (typeof (WindowsDriver))]
		public void Force16Colors_Sets (Type driverType)
		{
			var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
			driver.Init ();

			driver.Force16Colors = true;
			Assert.True (driver.Force16Colors);

			driver.End ();
		}
	}
}