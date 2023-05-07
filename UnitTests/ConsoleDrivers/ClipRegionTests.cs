using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		public void ClearClipRegion_Means_Screen (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Assert.Empty (driver.ClipRegion);
			Assert.Equal (new Rect (0, 0, driver.Cols, driver.Rows), driver.Clip);

			// Define a clip rectangle
			driver.Clip = new Rect (5, 5, 5, 5);
			Assert.NotEmpty (driver.ClipRegion);
			Assert.Equal (new Rect (5, 5, 5, 5), driver.Clip);

			driver.ClearClipRegion ();
			Assert.Empty (driver.ClipRegion);
			Assert.Equal (new Rect (0, 0, driver.Cols, driver.Rows), driver.Clip);

			// Define a clip rectangle
			driver.Clip = new Rect (5, 5, 5, 5);
			Assert.NotEmpty (driver.ClipRegion);
			Assert.Equal (new Rect (5, 5, 5, 5), driver.Clip);

			driver.ClearClipRegion ();
			Assert.Empty (driver.ClipRegion);
			Assert.Equal (new Rect (0, 0, driver.Cols, driver.Rows), driver.Clip);

			Application.Shutdown ();
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
			Assert.NotEmpty (driver.ClipRegion);

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
		public void AddClipRegion_Expands (Type driverType)
		{
			var driver = (FakeDriver)Activator.CreateInstance (driverType);
			Application.Init (driver);

			Assert.Empty (driver.ClipRegion);

			// Setup the region with a single rectangle
			driver.AddToClipRegion (new Rect (5, 5, 5, 5));
			Assert.NotEmpty (driver.ClipRegion);
			Assert.Equal (new Rect (5, 5, 5, 5), driver.Clip);

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

			// Add a second, disjoint, rectangle to the region. This should expand the region
			// to include both rectangles. maxX before was 9. The far right side of the new
			// rect is 9 + 5 = 14. 
			driver.AddToClipRegion (new Rect (10, 10, 5, 5));
			Assert.NotEmpty (driver.ClipRegion);
			Assert.Equal (new Rect (5, 5, 10, 10), driver.Clip);

			// positive
			Assert.True (driver.IsValidLocation (5, 5));
			Assert.True (driver.IsValidLocation (9, 9));
			Assert.True (driver.IsValidLocation (10, 10));
			Assert.True (driver.IsValidLocation (14, 14));
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

			// add a third, contiguous with first, rectangle
			driver.AddToClipRegion (new Rect (0, 0, 6, 6));
			Assert.NotEmpty (driver.ClipRegion);
			Assert.Equal (new Rect (0, 0, 15, 15), driver.Clip);

			// positive
			Assert.True (driver.IsValidLocation (0, 5));
			Assert.True (driver.IsValidLocation (4, 4));
			Assert.True (driver.IsValidLocation (5, 5));
			Assert.True (driver.IsValidLocation (9, 9));
			Assert.True (driver.IsValidLocation (10, 10));
			Assert.True (driver.IsValidLocation (14, 14));
			Assert.True (driver.IsValidLocation (7, 7));
			// negative
			Assert.False (driver.IsValidLocation (4, 10));
			Assert.False (driver.IsValidLocation (10, 9));
			Assert.False (driver.IsValidLocation (9, 10));

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
			Assert.Equal ('x', driver.Contents [0, 0, 0]);

			driver.Move (5, 5);
			driver.AddRune ('x');
			Assert.Equal ('x', driver.Contents [5, 5, 0]);

			// Clear the contents
			driver.FillRect (new Rect (0, 0, driver.Rows, driver.Cols), ' ');
			Assert.Equal (' ', driver.Contents [0, 0, 0]);

			// Setup the region with a single rectangle, fill screen with 'x'
			driver.Clip = new Rect (5, 5, 5, 5);
			driver.FillRect (new Rect (0, 0, driver.Rows, driver.Cols), 'x');
			Assert.Equal (' ', (char)driver.Contents [0, 0, 0]);
			Assert.Equal (' ', (char)driver.Contents [4, 9, 0]);
			Assert.Equal ('x', (char)driver.Contents [5, 5, 0]);
			Assert.Equal ('x', (char)driver.Contents [9, 9, 0]);
			Assert.Equal (' ', (char)driver.Contents [10, 10, 0]);

			// Add a second, disjoint, rectangle, fill screen with 'y'
			driver.AddToClipRegion (new Rect (10, 10, 5, 5));
			driver.FillRect (new Rect (0, 0, driver.Rows, driver.Cols), 'y');
			Assert.Equal (' ', (char)driver.Contents [4, 9, 0]);
			Assert.Equal ('y', (char)driver.Contents [5, 9, 0]);
			Assert.Equal ('y', (char)driver.Contents [5, 5, 0]);// in 1st rect
			Assert.Equal ('y', (char)driver.Contents [9, 9, 0]);
			Assert.Equal (' ', (char)driver.Contents [0, 0, 0]);
			Assert.Equal ('y', (char)driver.Contents [10, 10, 0]);
			Assert.Equal ('y', (char)driver.Contents [14, 14, 0]);
			Assert.Equal (' ', (char)driver.Contents [15, 15, 0]);

			// add a third, contiguous with first, rectangle, fill screen with 'z'
			driver.AddToClipRegion (new Rect (0, 0, 5, 5));
			driver.FillRect (new Rect (0, 0, driver.Rows, driver.Cols), 'z');
			Assert.Equal ('z', (char)driver.Contents [5, 5, 0]); // in 1st rect
			Assert.Equal ('z', (char)driver.Contents [0, 0, 0]); // in 3rd rect
			Assert.Equal ('z', (char)driver.Contents [4, 4, 0]); // in 3rd rect
			Assert.Equal (' ', (char)driver.Contents [4, 9, 0]);
			Assert.Equal ('z', (char)driver.Contents [5, 5, 0]);
			Assert.Equal ('z', (char)driver.Contents [9, 9, 0]); // in 2nd rect
			Assert.Equal ('z', (char)driver.Contents [10, 10, 0]);
			Assert.Equal ('z', (char)driver.Contents [14, 14, 0]);
			Assert.Equal (' ', (char)driver.Contents [15, 15, 0]);

			Application.Shutdown ();
		}
	}
}
