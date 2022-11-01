﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;
using Terminal.Gui;
using Rune = System.Rune;
using Attribute = Terminal.Gui.Attribute;
using System.Text.RegularExpressions;
using System.Reflection;


// This class enables test functions annotated with the [AutoInitShutdown] attribute to 
// automatically call Application.Init before called and Application.Shutdown after
// 
// This is necessary because a) Application is a singleton and Init/Shutdown must be called
// as a pair, and b) all unit test functions should be atomic.
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AutoInitShutdownAttribute : Xunit.Sdk.BeforeAfterTestAttribute {

	static bool _init = false;
	public override void Before (MethodInfo methodUnderTest)
	{
		if (_init) {
			throw new InvalidOperationException ("After did not run.");
		}

		Application.Init (new FakeDriver (), new FakeMainLoop (() => FakeConsole.ReadKey (true)));
		_init = true;
	}

	public override void After (MethodInfo methodUnderTest)
	{
		Application.Shutdown ();
		_init = false;
	}
}

class TestHelpers {


#pragma warning disable xUnit1013 // Public method should be marked as test
	public static void AssertDriverContentsAre (string expectedLook, ITestOutputHelper output)
	{
#pragma warning restore xUnit1013 // Public method should be marked as test

		var sb = new StringBuilder ();
		var driver = ((FakeDriver)Application.Driver);

		var contents = driver.Contents;

		for (int r = 0; r < driver.Rows; r++) {
			for (int c = 0; c < driver.Cols; c++) {
				sb.Append ((char)contents [r, c, 0]);
			}
			sb.AppendLine ();
		}

		var actualLook = sb.ToString ();

		if (!string.Equals (expectedLook, actualLook)) {

			// ignore trailing whitespace on each line
			var trailingWhitespace = new Regex (@"\s+$", RegexOptions.Multiline);

			// get rid of trailing whitespace on each line (and leading/trailing whitespace of start/end of full string)
			expectedLook = trailingWhitespace.Replace (expectedLook, "").Trim ();
			actualLook = trailingWhitespace.Replace (actualLook, "").Trim ();

			// standardize line endings for the comparison
			expectedLook = expectedLook.Replace ("\r\n", "\n");
			actualLook = actualLook.Replace ("\r\n", "\n");

			output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
			output?.WriteLine ("But Was:" + Environment.NewLine + actualLook);

			Assert.Equal (expectedLook, actualLook);
		}
	}

	public static Rect AssertDriverContentsWithFrameAre (string expectedLook, ITestOutputHelper output)
	{
		var lines = new List<List<char>> ();
		var sb = new StringBuilder ();
		var driver = ((FakeDriver)Application.Driver);
		var x = -1;
		var y = -1;
		int w = -1;
		int h = -1;

		var contents = driver.Contents;

		for (int r = 0; r < driver.Rows; r++) {
			var runes = new List<char> ();
			for (int c = 0; c < driver.Cols; c++) {
				var rune = (char)contents [r, c, 0];
				if (rune != ' ') {
					if (x == -1) {
						x = c;
						y = r;
						for (int i = 0; i < c; i++) {
							runes.InsertRange (i, new List<char> () { ' ' });
						}
					}
					if (Rune.ColumnWidth (rune) > 1) {
						c++;
					}
					if (c + 1 > w) {
						w = c + 1;
					}
					h = r - y + 1;
				}
				if (x > -1) {
					runes.Add (rune);
				}
			}
			if (runes.Count > 0) {
				lines.Add (runes);
			}
		}

		// Remove unnecessary empty lines
		if (lines.Count > 0) {
			for (int r = lines.Count - 1; r > h - 1; r--) {
				lines.RemoveAt (r);
			}
		}

		// Remove trailing whitespace on each line
		for (int r = 0; r < lines.Count; r++) {
			List<char> row = lines [r];
			for (int c = row.Count - 1; c >= 0; c--) {
				var rune = row [c];
				if (rune != ' ' || (row.Sum (x => Rune.ColumnWidth (x)) == w)) {
					break;
				}
				row.RemoveAt (c);
			}
		}

		// Convert char list to string
		for (int r = 0; r < lines.Count; r++) {
			var line = new string (lines [r].ToArray ());
			if (r == lines.Count - 1) {
				sb.Append (line);
			} else {
				sb.AppendLine (line);
			}
		}

		var actualLook = sb.ToString ();

		if (!string.Equals (expectedLook, actualLook)) {

			// standardize line endings for the comparison
			expectedLook = expectedLook.Replace ("\r\n", "\n");
			actualLook = actualLook.Replace ("\r\n", "\n");

			// Remove the first and the last line ending from the expectedLook
			if (expectedLook.StartsWith ("\n")) {
				expectedLook = expectedLook [1..];
			}
			if (expectedLook.EndsWith ("\n")) {
				expectedLook = expectedLook [..^1];
			}

			output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
			output?.WriteLine ("But Was:" + Environment.NewLine + actualLook);

			Assert.Equal (expectedLook, actualLook);
		}
		return new Rect (x > -1 ? x : 0, y > -1 ? y : 0, w > -1 ? w : 0, h > -1 ? h : 0);
	}

#pragma warning disable xUnit1013 // Public method should be marked as test
	/// <summary>
	/// Verifies the console was rendered using the given <paramref name="expectedColors"/> at the given locations.
	/// Pass a bitmap of indexes into <paramref name="expectedColors"/> as <paramref name="expectedLook"/> and the
	/// test method will verify those colors were used in the row/col of the console during rendering
	/// </summary>
	/// <param name="expectedLook">Numbers between 0 and 9 for each row/col of the console.  Must be valid indexes of <paramref name="expectedColors"/></param>
	/// <param name="expectedColors"></param>
	public static void AssertDriverColorsAre (string expectedLook, Attribute [] expectedColors)
	{
#pragma warning restore xUnit1013 // Public method should be marked as test

		if (expectedColors.Length > 10) {
			throw new ArgumentException ("This method only works for UIs that use at most 10 colors");
		}

		expectedLook = expectedLook.Trim ();
		var driver = ((FakeDriver)Application.Driver);

		var contents = driver.Contents;

		int r = 0;
		foreach (var line in expectedLook.Split ('\n').Select (l => l.Trim ())) {

			for (int c = 0; c < line.Length; c++) {

				int val = contents [r, c, 1];

				var match = expectedColors.Where (e => e.Value == val).ToList ();
				if (match.Count == 0) {
					throw new Exception ($"Unexpected color {DescribeColor (val)} was used at row {r} and col {c} (indexes start at 0).  Color value was {val} (expected colors were {string.Join (",", expectedColors.Select (c => c.Value))})");
				} else if (match.Count > 1) {
					throw new ArgumentException ($"Bad value for expectedColors, {match.Count} Attributes had the same Value");
				}

				var colorUsed = Array.IndexOf (expectedColors, match [0]).ToString () [0];
				var userExpected = line [c];

				if (colorUsed != userExpected) {
					throw new Exception ($"Colors used did not match expected at row {r} and col {c} (indexes start at 0).  Color index used was {DescribeColor (colorUsed)} but test expected {DescribeColor (userExpected)} (these are indexes into the expectedColors array)");
				}
			}

			r++;
		}
	}

	private static object DescribeColor (int userExpected)
	{
		var a = new Attribute (userExpected);
		return $"{a.Foreground},{a.Background}";
	}
}

