using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit.Abstractions;
using Xunit;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Diagnostics;

using Attribute = Terminal.Gui.Attribute;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Sdk;

namespace Terminal.Gui;
// This class enables test functions annotated with the [AutoInitShutdown] attribute to 
// automatically call Application.Init at start of the test and Application.Shutdown after the
// test exits. 
// 
// This is necessary because a) Application is a singleton and Init/Shutdown must be called
// as a pair, and b) all unit test functions should be atomic..
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AutoInitShutdownAttribute : Xunit.Sdk.BeforeAfterTestAttribute {
	/// <summary>
	/// Initializes a [AutoInitShutdown] attribute, which determines if/how Application.Init and
	/// Application.Shutdown are automatically called Before/After a test runs.
	/// </summary>
	/// <param name="autoInit">If true, Application.Init will be called Before the test runs.</param>
	/// <param name="autoShutdown">If true, Application.Shutdown will be called After the test runs.</param>
	/// <param name="consoleDriverType">Determines which ConsoleDriver (FakeDriver, WindowsDriver, 
	/// CursesDriver, NetDriver) will be used when Application.Init is called. If null FakeDriver will be used.
	/// Only valid if <paramref name="autoInit"/> is true.</param>
	/// <param name="useFakeClipboard">If true, will force the use of <see cref="FakeDriver.FakeClipboard"/>. 
	/// Only valid if <see cref="ConsoleDriver"/> == <see cref="FakeDriver"/> and <paramref name="autoInit"/> is true.</param>
	/// <param name="fakeClipboardAlwaysThrowsNotSupportedException">Only valid if <paramref name="autoInit"/> is true.
	/// Only valid if <see cref="ConsoleDriver"/> == <see cref="FakeDriver"/> and <paramref name="autoInit"/> is true.</param>
	/// <param name="fakeClipboardIsSupportedAlwaysTrue">Only valid if <paramref name="autoInit"/> is true.
	/// Only valid if <see cref="ConsoleDriver"/> == <see cref="FakeDriver"/> and <paramref name="autoInit"/> is true.</param>
	/// <param name="configLocation">Determines what config file locations <see cref="ConfigurationManager"/> will 
	/// load from.</param>
	public AutoInitShutdownAttribute (bool autoInit = true,
		Type consoleDriverType = null,
		bool useFakeClipboard = true,
		bool fakeClipboardAlwaysThrowsNotSupportedException = false,
		bool fakeClipboardIsSupportedAlwaysTrue = false,
		ConfigurationManager.ConfigLocations configLocation = ConfigurationManager.ConfigLocations.DefaultOnly)
	{
		//Assert.True (autoInit == false && consoleDriverType == null);

		AutoInit = autoInit;
		_driverType = consoleDriverType ?? typeof (FakeDriver);
		FakeDriver.FakeBehaviors.UseFakeClipboard = useFakeClipboard;
		FakeDriver.FakeBehaviors.FakeClipboardAlwaysThrowsNotSupportedException = fakeClipboardAlwaysThrowsNotSupportedException;
		FakeDriver.FakeBehaviors.FakeClipboardIsSupportedAlwaysFalse = fakeClipboardIsSupportedAlwaysTrue;
		ConfigurationManager.Locations = configLocation;
	}

	static bool _init = false;
	bool AutoInit { get; }
	Type _driverType;

	public override void Before (MethodInfo methodUnderTest)
	{
		Debug.WriteLine ($"Before: {methodUnderTest.Name}");
		if (AutoInit) {
			Application.Init ((ConsoleDriver)Activator.CreateInstance (_driverType));
			_init = true;
		}
	}
	
	public override void After (MethodInfo methodUnderTest)
	{
		Debug.WriteLine ($"After: {methodUnderTest.Name}");
		if (AutoInit) {
			Application.Shutdown ();
			_init = false;
		}
	}
}

[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class SetupFakeDriverAttribute : Xunit.Sdk.BeforeAfterTestAttribute {
	/// <summary>
	/// Enables test functions annotated with the [SetupFakeDriver] attribute to 
	/// set Application.Driver to new FakeDriver(). 
	/// </summary>
	public SetupFakeDriverAttribute ()
	{
	}

	public override void Before (MethodInfo methodUnderTest)
	{
		Debug.WriteLine ($"Before: {methodUnderTest.Name}");
		Assert.Null (Application.Driver);
		Application.Driver = new FakeDriver ();
	}

	public override void After (MethodInfo methodUnderTest)
	{
		Debug.WriteLine ($"After: {methodUnderTest.Name}");
		Application.Driver = null;
	}
}

partial class TestHelpers {
	[GeneratedRegex ("\\s+$", RegexOptions.Multiline)]
	private static partial Regex TrailingWhiteSpaceRegEx ();
	[GeneratedRegex ("^\\s+", RegexOptions.Multiline)]
	private static partial Regex LeadingWhitespaceRegEx ();

#pragma warning disable xUnit1013 // Public method should be marked as test
	public static void AssertDriverContentsAre (string expectedLook, ITestOutputHelper output, bool ignoreLeadingWhitespace = false)
	{
#pragma warning restore xUnit1013 // Public method should be marked as test

		var sb = new StringBuilder ();
		var driver = (FakeDriver)Application.Driver;

		var contents = driver.Contents;

		for (int r = 0; r < driver.Rows; r++) {
			for (int c = 0; c < driver.Cols; c++) {
				Rune rune = (Rune)contents [r, c, 0];
				if (rune.DecodeSurrogatePair (out char [] spair)) {
					sb.Append (spair);
				} else {
					sb.Append ((char)rune.Value);
				}
				if (rune.GetColumns () > 1) {
					c++;
				}
			}
			sb.AppendLine ();
		}

		var actualLook = sb.ToString ();

		if (string.Equals (expectedLook, actualLook)) return;
		
		// get rid of trailing whitespace on each line (and leading/trailing whitespace of start/end of full string)
		expectedLook = TrailingWhiteSpaceRegEx ().Replace (expectedLook, "").Trim ();
		actualLook = TrailingWhiteSpaceRegEx ().Replace (actualLook, "").Trim ();

		if (ignoreLeadingWhitespace) {
			expectedLook = LeadingWhitespaceRegEx().Replace (expectedLook, "").Trim ();
			actualLook = LeadingWhitespaceRegEx().Replace (actualLook, "").Trim ();
		}

		// standardize line endings for the comparison
		expectedLook = expectedLook.Replace ("\r\n", "\n");
		actualLook = actualLook.Replace ("\r\n", "\n");

		// If test is about to fail show user what things looked like
		if (!string.Equals (expectedLook, actualLook)) {
			output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
			output?.WriteLine ("But Was:" + Environment.NewLine + actualLook);
		}

		Assert.Equal (expectedLook, actualLook);
	}

	public static Rect AssertDriverContentsWithFrameAre (string expectedLook, ITestOutputHelper output)
	{
		var lines = new List<List<Rune>> ();
		var sb = new StringBuilder ();
		var driver = Application.Driver;
		var x = -1;
		var y = -1;
		var w = -1;
		var h = -1;
		
		var contents = driver.Contents;

		for (var r = 0; r < driver.Rows; r++) {
			var runes = new List<Rune> ();
			for (var c = 0; c < driver.Cols; c++) {
				var rune = (Rune)contents [r, c, 0];
				if (rune != (Rune)' ') {
					if (x == -1) {
						x = c;
						y = r;
						for (int i = 0; i < c; i++) {
							runes.InsertRange (i, new List<Rune> () { (Rune)' ' });
						}
					}
					if (rune.GetColumns () > 1) {
						c++;
					}
					if (c + 1 > w) {
						w = c + 1;
					}
					h = r - y + 1;
				}
				if (x > -1) runes.Add (rune);
			}
			if (runes.Count > 0) lines.Add (runes);
		}

		// Remove unnecessary empty lines
		if (lines.Count > 0) {
			for (var r = lines.Count - 1; r > h - 1; r--) lines.RemoveAt (r);
		}

		// Remove trailing whitespace on each line
		foreach (var row in lines) {
			for (var c = row.Count - 1; c >= 0; c--) {
				var rune = row [c];
				if (rune != (Rune)' ' || (row.Sum (x => x.GetColumns ()) == w)) {
					break;
				}
				row.RemoveAt (c);
			}
		}

		// Convert Rune list to string
		for (int r = 0; r < lines.Count; r++) {
			var line = Terminal.Gui.StringExtensions.ToString (lines [r]).ToString ();
			if (r == lines.Count - 1) {
				sb.Append (line);
			} else {
				sb.AppendLine (line);
			}
		}

		var actualLook = sb.ToString ();

		if (string.Equals (expectedLook, actualLook)) {
			return new Rect (x > -1 ? x : 0, y > -1 ? y : 0, w > -1 ? w : 0, h > -1 ? h : 0);
		}
		
		// standardize line endings for the comparison
		expectedLook = expectedLook.Replace ("\r\n", "\n");
		actualLook = actualLook.Replace ("\r\n", "\n");

		// Remove the first and the last line ending from the expectedLook
		if (expectedLook.StartsWith ("\n")) expectedLook = expectedLook [1..];
		if (expectedLook.EndsWith ("\n")) expectedLook = expectedLook [..^1];

		output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
		output?.WriteLine ("But Was:" + Environment.NewLine + actualLook);

		Assert.Equal (expectedLook, actualLook);
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
	public static void AssertDriverColorsAre (string expectedLook, params Attribute [] expectedColors)
	{
#pragma warning restore xUnit1013 // Public method should be marked as test

		if (expectedColors.Length > 10) throw new ArgumentException ("This method only works for UIs that use at most 10 colors");

		expectedLook = expectedLook.Trim ();
		var driver = (FakeDriver)Application.Driver;
		
		var contents = driver.Contents;

		var r = 0;
		foreach (var line in expectedLook.Split ('\n').Select (l => l.Trim ())) {

			for (var c = 0; c < line.Length; c++) {

				Attribute val = new Attribute( contents [r, c, 1]);

				var match = expectedColors.Where (e => e == val).ToList ();
				switch (match.Count) {
				case 0:
					throw new Exception ($"Unexpected color {val} was used at row {r} and col {c} (indexes start at 0).  Color value was {val} (expected colors were {string.Join (",", expectedColors.Select (c => c.Value.ToString()))})");
				case > 1:
					throw new ArgumentException ($"Bad value for expectedColors, {match.Count} Attributes had the same Value");
				}

				var colorUsed = Array.IndexOf (expectedColors, match [0]).ToString () [0];
				var userExpected = line [c];

				if (colorUsed != userExpected) throw new Exception ($"Colors used did not match expected at row {r} and col {c} (indexes start at 0).  Color index used was {colorUsed} ({val}) but test expected {userExpected} ({expectedColors [int.Parse (userExpected.ToString ())].Value}) (these are indexes into the expectedColors array)");
			}

			r++;
		}
	}
	/// <summary>
	/// Verifies the console used all the <paramref name="expectedColors"/> when rendering.
	/// If one or more of the expected colors are not used then the failure will output both
	/// the colors that were found to be used and which of your expectations was not met.
	/// </summary>
	/// <param name="expectedColors"></param>
	internal static void AssertDriverUsedColors (params Attribute [] expectedColors)
	{
		var driver = (FakeDriver)Application.Driver;

		var contents = driver.Contents;

		var toFind = expectedColors.ToList ();

		// Contents 3rd column is an Attribute
		var colorsUsed = new HashSet<Attribute> ();

		for (var r = 0; r < driver.Rows; r++) {
			for (var c = 0; c < driver.Cols; c++) {
				var val = new Attribute(contents [r, c, 1]);

				colorsUsed.Add (val);

				var match = toFind.FirstOrDefault (e => e == val);

				// need to check twice because Attribute is a struct and therefore cannot be null
				if (toFind.Any (e => e == val)) {
					toFind.Remove (match);
				}
			}}

		if (!toFind.Any ()) {
			return;
		}
		var sb = new StringBuilder ();
		sb.AppendLine ("The following colors were not used:" + string.Join ("; ", toFind.Select (a => a.ToString())));
		sb.AppendLine ("Colors used were:" + string.Join ("; ", colorsUsed.Select (a => a.ToString())));
		throw new Exception (sb.ToString ());
	}

#pragma warning disable xUnit1013 // Public method should be marked as test
	/// <summary>
	/// Verifies two strings are equivalent. If the assert fails, output will be generated to standard 
	/// output showing the expected and actual look.
	/// </summary>
	/// <param name="output"></param>
	/// <param name="expectedLook">A string containing the expected look. Newlines should be specified as "\r\n" as
	/// they will be converted to <see cref="Environment.NewLine"/> to make tests platform independent.</param>
	/// <param name="actualLook"></param>
	public static void AssertEqual (ITestOutputHelper output, string expectedLook, string actualLook)
	{
		// Convert newlines to platform-specific newlines
		expectedLook = ReplaceNewLinesToPlatformSpecific (expectedLook);

		// If test is about to fail show user what things looked like
		if (!string.Equals (expectedLook, actualLook)) {
			output?.WriteLine ("Expected:" + Environment.NewLine + expectedLook);
			output?.WriteLine ("But Was:" + Environment.NewLine + actualLook);
		}

		Assert.Equal (expectedLook, actualLook);
	}
#pragma warning restore xUnit1013 // Public method should be marked as test

	private static string ReplaceNewLinesToPlatformSpecific (string toReplace)
	{
		var replaced = toReplace;

		replaced = Environment.NewLine.Length switch {
			2 when !replaced.Contains ("\r\n") => replaced.Replace ("\n", Environment.NewLine),
			1 => replaced.Replace ("\r\n", Environment.NewLine),
			var _ => replaced
		};

		return replaced;
	}
}
