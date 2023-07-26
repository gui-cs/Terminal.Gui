using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests {
	public class ConsoleScrollingTests {
		readonly ITestOutputHelper output;

		public ConsoleScrollingTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void Left_And_Top_Is_Always_Zero (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			driver.SetWindowPosition (5, 5);
			Assert.Equal (0, Console.WindowLeft);
			Assert.Equal (0, Console.WindowTop);

			Application.Shutdown ();
		}
		
	}
}
