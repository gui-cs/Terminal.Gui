using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests;
public class ContentsTests {
	readonly ITestOutputHelper output;

	public ContentsTests (ITestOutputHelper output)
	{
		this.output = output;
	}

	[Theory]
	[InlineData (typeof (FakeDriver))]
	//[InlineData (typeof (NetDriver))]
	//[InlineData (typeof (CursesDriver))]
	//[InlineData (typeof (WindowsDriver))]
	public void AddStr_With_Combining_Characters (Type driverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		Application.Init (driver);
		// driver.Init (null);

		var acuteaccent = new System.Text.Rune (0x0301); // Combining acute accent (é)
		var combined = "e" + acuteaccent;
		var expected = "é";

		driver.AddStr (combined);
		TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

#if false // Disabled Until #2616 is fixed

		// 3 char combine
		// a + ogonek + acute = <U+0061, U+0328, U+0301> ( ǫ́ )
		var ogonek = new System.Text.Rune (0x0328); // Combining ogonek (a small hook or comma shape)
		combined = "a" + ogonek + acuteaccent;
		expected = "ǫ́";

		driver.Move (0, 0);
		driver.AddStr (combined);
		TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

#endif
		
		// Shutdown must be called to safely clean up Application if Init has been called
		Application.Shutdown ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver))]
	//[InlineData (typeof (NetDriver))]
	//[InlineData (typeof (CursesDriver))]
	//[InlineData (typeof (WindowsDriver))]
	public void Move_Bad_Coordinates (Type driverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		Application.Init (driver);

		Assert.Equal (0, driver.Col);
		Assert.Equal (0, driver.Row);

		driver.Move (-1, 0);
		Assert.Equal (-1, driver.Col);
		Assert.Equal (0, driver.Row);

		driver.Move (0, -1);
		Assert.Equal (0, driver.Col);
		Assert.Equal (-1, driver.Row);

		driver.Move (driver.Cols, 0);
		Assert.Equal (driver.Cols, driver.Col);
		Assert.Equal (0, driver.Row);

		driver.Move (0, driver.Rows);
		Assert.Equal (0, driver.Col);
		Assert.Equal (driver.Rows, driver.Row);

		driver.Move (500, 500);
		Assert.Equal (500, driver.Col);
		Assert.Equal (500, driver.Row);

		// Shutdown must be called to safely clean up Application if Init has been called
		Application.Shutdown ();
	}

	// TODO: Add these unit tests
	
	// AddRune moves correctly

	// AddRune with wide characters are handled correctly

	// AddRune with wide characters and Col < 0 are handled correctly

	// AddRune with wide characters and Col == Cols - 1 are handled correctly

	// AddRune with wide characters and Col == Cols are handled correctly

	// AddStr moves correctly

	// AddStr with wide characters moves correctly

	// AddStr where Col is negative works

	// AddStr where Col is negative and characters include wide / combining characters works

	// AddStr where Col is near Cols and characters include wide / combining characters works

	// Clipping works correctly

	// Clipping works correctly with wide characters

	// Clipping works correctly with combining characters

	// Clipping works correctly with combining characters and wide characters

	// ResizeScreen works correctly

	// Refresh works correctly

	// IsDirty tests
}

