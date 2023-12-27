﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.DriverTests;
public class ContentsTests {
	readonly ITestOutputHelper output;

	public ContentsTests (ITestOutputHelper output)
	{
		ConsoleDriver.RunningUnitTests = true;
		this.output = output;
	}


	[Theory]
	[InlineData (typeof (FakeDriver))]
	[InlineData (typeof (NetDriver))]
	//[InlineData (typeof (ANSIDriver))]
	//[InlineData (typeof (CursesDriver))] // TODO: Uncomment when #2796 and #2615 are fixed
	//[InlineData (typeof (WindowsDriver))] // TODO: Uncomment when #2610 is fixed
	public void AddStr_Combining_Character_1st_Column (Type driverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		driver.Init ();
		var expected = "\u0301!";
		driver.AddStr ("\u0301!"); // acute accent + exclamation mark
		TestHelpers.AssertDriverContentsAre (expected, output, driver);

		driver.End ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver))]
	[InlineData (typeof (NetDriver))]
	//[InlineData (typeof (ANSIDriver))]
	//[InlineData (typeof (CursesDriver))] // TODO: Uncomment when #2796 and #2615 are fixed
	//[InlineData (typeof (WindowsDriver))] // TODO: Uncomment when #2610 is fixed
	public void AddStr_With_Combining_Characters (Type driverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		driver.Init ();

		var acuteaccent = new System.Text.Rune (0x0301); // Combining acute accent (é)
		var combined = "e" + acuteaccent;
		var expected = "é";

		driver.AddStr (combined);
		TestHelpers.AssertDriverContentsAre (expected, output, driver);

		// 3 char combine
		// a + ogonek + acute = <U+0061, U+0328, U+0301> ( ą́ )
		var ogonek = new System.Text.Rune (0x0328); // Combining ogonek (a small hook or comma shape)
		combined = "a" + ogonek + acuteaccent;
		expected = ("a" + ogonek).Normalize(NormalizationForm.FormC); // See Issue #2616

		driver.Move (0, 0);
		driver.AddStr (combined);
		TestHelpers.AssertDriverContentsAre (expected, output, driver);

		// e + ogonek + acute = <U+0061, U+0328, U+0301> ( ę́́ )
		combined = "e" + ogonek + acuteaccent;
		expected = ("e" + ogonek).Normalize (NormalizationForm.FormC); // See Issue #2616

		driver.Move (0, 0);
		driver.AddStr (combined);
		TestHelpers.AssertDriverContentsAre (expected, output, driver);

		// i + ogonek + acute = <U+0061, U+0328, U+0301> ( į́́́ )
		combined = "i" + ogonek + acuteaccent;
		expected = ("i" + ogonek).Normalize (NormalizationForm.FormC); // See Issue #2616

		driver.Move (0, 0);
		driver.AddStr (combined);
		TestHelpers.AssertDriverContentsAre (expected, output, driver);

		// u + ogonek + acute = <U+0061, U+0328, U+0301> ( ų́́́́ )
		combined = "u" + ogonek + acuteaccent;
		expected = ("u" + ogonek).Normalize (NormalizationForm.FormC); // See Issue #2616

		driver.Move (0, 0);
		driver.AddStr (combined);
		TestHelpers.AssertDriverContentsAre (expected, output, driver);

		driver.End ();
	}

	[Theory]
	[InlineData (typeof (FakeDriver))]
	[InlineData (typeof (NetDriver))]
	//[InlineData (typeof (ANSIDriver))]
	[InlineData (typeof (WindowsDriver))]
	[InlineData (typeof (CursesDriver))]
	public void Move_Bad_Coordinates (Type driverType)
	{
		var driver = (ConsoleDriver)Activator.CreateInstance (driverType);
		driver.Init ();

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
		driver.End ();
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

