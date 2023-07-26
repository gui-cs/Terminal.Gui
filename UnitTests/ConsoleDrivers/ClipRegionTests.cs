using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests {
	public class ClipRegionTests {
		readonly ITestOutputHelper output;

		public ClipRegionTests (ITestOutputHelper output)
		{
			this.output = output;
		}
		
		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void IsValidLocation (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			// positive
			Assert.True (driver.IsValidLocation (0, 0));
			Assert.True (driver.IsValidLocation (1, 1));
			Assert.True (driver.IsValidLocation (driver.Cols - 1, driver.Rows - 1));
			// negative
			Assert.False (driver.IsValidLocation (-1, 0));
			Assert.False (driver.IsValidLocation (0, -1));
			Assert.False (driver.IsValidLocation (-1, -1));
			Assert.False (driver.IsValidLocation (driver.Cols, driver.Rows - 1));
			Assert.False (driver.IsValidLocation (driver.Cols, driver.Rows - 1));
			Assert.False (driver.IsValidLocation (driver.Cols, driver.Rows));

			// Define a clip rectangle
			driver.Clip = new Rect (5, 5, 5, 5);
			// positive
			Assert.True (driver.IsValidLocation (5, 5));
			Assert.True (driver.IsValidLocation (9, 9));
			// negative
			Assert.False (driver.IsValidLocation (4, 5));
			Assert.False (driver.IsValidLocation (5, 4));
			Assert.False (driver.IsValidLocation (10, 9));
			Assert.False (driver.IsValidLocation (9, 10));
			Assert.False (driver.IsValidLocation (-1, 0));
			Assert.False (driver.IsValidLocation (0, -1));
			Assert.False (driver.IsValidLocation (-1, -1));
			Assert.False (driver.IsValidLocation (driver.Cols, driver.Rows - 1));
			Assert.False (driver.IsValidLocation (driver.Cols, driver.Rows - 1));
			Assert.False (driver.IsValidLocation (driver.Cols, driver.Rows));

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void Clip_Set_To_Empty_AllInvalid (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			// Define a clip rectangle
			driver.Clip = Rect.Empty;

			// negative
			Assert.False (driver.IsValidLocation (4, 5));
			Assert.False (driver.IsValidLocation (5, 4));
			Assert.False (driver.IsValidLocation (10, 9));
			Assert.False (driver.IsValidLocation (9, 10));
			Assert.False (driver.IsValidLocation (-1, 0));
			Assert.False (driver.IsValidLocation (0, -1));
			Assert.False (driver.IsValidLocation (-1, -1));
			Assert.False (driver.IsValidLocation (driver.Cols, driver.Rows - 1));
			Assert.False (driver.IsValidLocation (driver.Cols, driver.Rows - 1));
			Assert.False (driver.IsValidLocation (driver.Cols, driver.Rows));

			Application.Shutdown ();
		}

		[Theory]
		[InlineData (typeof (FakeDriver))]
		public void AddRune_Is_Clipped (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			driver.Move (0, 0);
			driver.AddRune ('x');
			Assert.Equal ((Rune)'x', driver.Contents [0, 0].Runes [0]);

			driver.Move (5, 5);
			driver.AddRune ('x');
			Assert.Equal ((Rune)'x', driver.Contents [5, 5].Runes [0]);

			// Clear the contents
			driver.FillRect (new Rect (0, 0, driver.Rows, driver.Cols), ' ');
			Assert.Equal ((Rune)' ', driver.Contents [0, 0].Runes [0]);

			// Setup the region with a single rectangle, fill screen with 'x'
			driver.Clip = new Rect (5, 5, 5, 5);
			driver.FillRect (new Rect (0, 0, driver.Rows, driver.Cols), 'x');
			Assert.Equal ((Rune)' ', driver.Contents [0, 0].Runes [0]);
			Assert.Equal ((Rune)' ', driver.Contents [4, 9].Runes [0]);
			Assert.Equal ((Rune)'x', driver.Contents [5, 5].Runes [0]);
			Assert.Equal ((Rune)'x', driver.Contents [9, 9].Runes [0]);
			Assert.Equal ((Rune)' ', driver.Contents [10, 10].Runes [0]);

			Application.Shutdown ();
		}
	}
}
