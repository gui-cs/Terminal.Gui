using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Xunit;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.TextTests {
	public class TextFormatterTests {
		readonly ITestOutputHelper output;

		public TextFormatterTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Basic_Usage ()
		{
			var testText = "test";
			var expectedSize = new Size ();
			var testBounds = new Rect (0, 0, 100, 1);
			var tf = new TextFormatter ();

			tf.Text = testText;
			expectedSize = new Size (testText.Length, 1);
			Assert.Equal (testText, tf.Text);
			Assert.Equal (TextAlignment.Left, tf.Alignment);
			Assert.Equal (expectedSize, tf.Size);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.Equal (expectedSize, tf.Size);
			Assert.NotEmpty (tf.Lines);

			tf.Alignment = TextAlignment.Right;
			expectedSize = new Size (testText.Length, 1);
			Assert.Equal (testText, tf.Text);
			Assert.Equal (TextAlignment.Right, tf.Alignment);
			Assert.Equal (expectedSize, tf.Size);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.Equal (expectedSize, tf.Size);
			Assert.NotEmpty (tf.Lines);

			tf.Alignment = TextAlignment.Right;
			expectedSize = new Size (testText.Length * 2, 1);
			tf.Size = expectedSize;
			Assert.Equal (testText, tf.Text);
			Assert.Equal (TextAlignment.Right, tf.Alignment);
			Assert.Equal (expectedSize, tf.Size);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.Equal (expectedSize, tf.Size);
			Assert.NotEmpty (tf.Lines);

			tf.Alignment = TextAlignment.Centered;
			expectedSize = new Size (testText.Length * 2, 1);
			tf.Size = expectedSize;
			Assert.Equal (testText, tf.Text);
			Assert.Equal (TextAlignment.Centered, tf.Alignment);
			Assert.Equal (expectedSize, tf.Size);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.Equal (expectedSize, tf.Size);
			Assert.NotEmpty (tf.Lines);
		}

		[Fact]
		public void TestSize_TextChange ()
		{
			var tf = new TextFormatter () { Text = "你" };
			Assert.Equal (2, tf.Size.Width);
			tf.Text = "你你";
			Assert.Equal (4, tf.Size.Width);
		}

		[Fact]
		public void NeedsFormat_Sets ()
		{
			var testText = "test";
			var testBounds = new Rect (0, 0, 100, 1);
			var tf = new TextFormatter ();

			tf.Text = "test";
			Assert.True (tf.NeedsFormat); // get_Lines causes a Format
			Assert.NotEmpty (tf.Lines);
			Assert.False (tf.NeedsFormat); // get_Lines causes a Format
			Assert.Equal (testText, tf.Text);
			tf.Draw (testBounds, new Attribute (), new Attribute ());
			Assert.False (tf.NeedsFormat);

			tf.Size = new Size (1, 1);
			Assert.True (tf.NeedsFormat);
			Assert.NotEmpty (tf.Lines);
			Assert.False (tf.NeedsFormat); // get_Lines causes a Format

			tf.Alignment = TextAlignment.Centered;
			Assert.True (tf.NeedsFormat);
			Assert.NotEmpty (tf.Lines);
			Assert.False (tf.NeedsFormat); // get_Lines causes a Format
		}

		[Fact]
		public void FindHotKey_Invalid_ReturnsFalse ()
		{
			var text = string.Empty;
			Rune hotKeySpecifier = (Rune)'_';
			bool supportFirstUpperCase = false;
			int hotPos = 0;
			Key hotKey = Key.Unknown;
			bool result = false;

			text = null;
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "no hotkey";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "No hotkey, Upper Case";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "Non-english: Сохранить";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);
		}

		[Fact]
		public void FindHotKey_AlphaUpperCase_Succeeds ()
		{
			var text = string.Empty;
			Rune hotKeySpecifier = (Rune)'_';
			bool supportFirstUpperCase = false;
			int hotPos = 0;
			Key hotKey = Key.Unknown;
			bool result = false;

			text = "_K Before";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (0, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "a_K Second";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (1, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "Last _K";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (5, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "After K_";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "Multiple _K and _R";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (9, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			// Cryllic K (К)
			text = "Non-english: _Кдать";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (13, hotPos);
			Assert.Equal ((Key)'К', hotKey);

			// Turn on FirstUpperCase and verify same results
			supportFirstUpperCase = true;
			text = "_K Before";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (0, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "a_K Second";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (1, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "Last _K";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (5, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "After K_";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "Multiple _K and _R";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (9, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			// Cryllic K (К)
			text = "Non-english: _Кдать";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (13, hotPos);
			Assert.Equal ((Key)'К', hotKey);
		}
		[Fact]
		public void FindHotKey_AlphaLowerCase_Succeeds ()
		{
			var text = string.Empty;
			Rune hotKeySpecifier = (Rune)'_';
			bool supportFirstUpperCase = false;
			int hotPos = 0;
			Key hotKey = Key.Unknown;
			bool result = false;

			// lower case should return uppercase Hotkey
			text = "_k Before";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (0, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "a_k Second";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (1, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "Last _k";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (5, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "After k_";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "Multiple _k and _R";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (9, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			// Lower case Cryllic K (к)
			text = "Non-english: _кдать";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (13, hotPos);
			Assert.Equal ((Key)'К', hotKey);

			// Turn on FirstUpperCase and verify same results
			supportFirstUpperCase = true;
			text = "_k Before";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (0, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "a_k Second";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (1, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "Last _k";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (5, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "After k_";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "Multiple _k and _R";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (9, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			// Lower case Cryllic K (к)
			text = "Non-english: _кдать";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (13, hotPos);
			Assert.Equal ((Key)'К', hotKey);
		}

		[Fact]
		public void FindHotKey_Numeric_Succeeds ()
		{
			var text = string.Empty;
			Rune hotKeySpecifier = (Rune)'_';
			bool supportFirstUpperCase = false;
			int hotPos = 0;
			Key hotKey = Key.Unknown;
			bool result = false;
			// Digits 
			text = "_1 Before";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (0, hotPos);
			Assert.Equal ((Key)'1', hotKey);

			text = "a_1 Second";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (1, hotPos);
			Assert.Equal ((Key)'1', hotKey);

			text = "Last _1";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (5, hotPos);
			Assert.Equal ((Key)'1', hotKey);

			text = "After 1_";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "Multiple _1 and _2";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (9, hotPos);
			Assert.Equal ((Key)'1', hotKey);

			// Turn on FirstUpperCase and verify same results
			supportFirstUpperCase = true;
			text = "_1 Before";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (0, hotPos);
			Assert.Equal ((Key)'1', hotKey);

			text = "a_1 Second";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (1, hotPos);
			Assert.Equal ((Key)'1', hotKey);

			text = "Last _1";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (5, hotPos);
			Assert.Equal ((Key)'1', hotKey);

			text = "After 1_";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "Multiple _1 and _2";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (9, hotPos);
			Assert.Equal ((Key)'1', hotKey);
		}

		[Fact]
		public void FindHotKey_Legacy_FirstUpperCase_Succeeds ()
		{
			bool supportFirstUpperCase = true;

			var text = string.Empty;
			var hotKeySpecifier = (Rune)0;
			int hotPos = 0;
			Key hotKey = Key.Unknown;
			bool result = false;

			text = "K Before";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (0, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "aK Second";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (1, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "last K";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (5, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			text = "multiple K and R";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (9, hotPos);
			Assert.Equal ((Key)'K', hotKey);

			// Cryllic K (К)
			text = "non-english: Кдать";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.True (result);
			Assert.Equal (13, hotPos);
			Assert.Equal ((Key)'К', hotKey);
		}

		[Fact]
		public void FindHotKey_Legacy_FirstUpperCase_NotFound_Returns_False ()
		{
			bool supportFirstUpperCase = true;

			var text = string.Empty;
			var hotKeySpecifier = (Rune)0;
			int hotPos = 0;
			Key hotKey = Key.Unknown;
			bool result = false;

			text = "k before";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "ak second";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "last k";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "multiple k and r";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			text = "12345";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			// punctuation
			text = "`~!@#$%^&*()-_=+[{]}\\|;:'\",<.>/?";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			// ~IsLetterOrDigit + Unicode
			text = " ~  s  gui.cs   master ↑10";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);

			// Lower case Cryllic K (к)
			text = "non-english: кдать";
			result = TextFormatter.FindHotKey (text, hotKeySpecifier, supportFirstUpperCase, out hotPos, out hotKey);
			Assert.False (result);
			Assert.Equal (-1, hotPos);
			Assert.Equal (Key.Unknown, hotKey);
		}

		static string testHotKeyAtStart = "_K Before";
		static string testHotKeyAtSecondPos = "a_K Second";
		static string testHotKeyAtLastPos = "Last _K";
		static string testHotKeyAfterLastChar = "After K_";
		static string testMultiHotKeys = "Multiple _K and _R";
		static string testNonEnglish = "Non-english: _Кдать";

		[Fact]
		public void RemoveHotKeySpecifier_InValid_ReturnsOriginal ()
		{
			Rune hotKeySpecifier = (Rune)'_';

			Assert.Null (TextFormatter.RemoveHotKeySpecifier (null, 0, hotKeySpecifier));
			Assert.Equal ("", TextFormatter.RemoveHotKeySpecifier ("", 0, hotKeySpecifier));
			Assert.Equal ("", TextFormatter.RemoveHotKeySpecifier ("", -1, hotKeySpecifier));
			Assert.Equal ("", TextFormatter.RemoveHotKeySpecifier ("", 100, hotKeySpecifier));

			Assert.Equal ("a", TextFormatter.RemoveHotKeySpecifier ("a", -1, hotKeySpecifier));
			Assert.Equal ("a", TextFormatter.RemoveHotKeySpecifier ("a", 100, hotKeySpecifier));
		}

		[Fact]
		public void RemoveHotKeySpecifier_Valid_ReturnsStripped ()
		{
			Rune hotKeySpecifier = (Rune)'_';

			Assert.Equal ("K Before", TextFormatter.RemoveHotKeySpecifier ("_K Before", 0, hotKeySpecifier));
			Assert.Equal ("aK Second", TextFormatter.RemoveHotKeySpecifier ("a_K Second", 1, hotKeySpecifier));
			Assert.Equal ("Last K", TextFormatter.RemoveHotKeySpecifier ("Last _K", 5, hotKeySpecifier));
			Assert.Equal ("After K", TextFormatter.RemoveHotKeySpecifier ("After K_", 7, hotKeySpecifier));
			Assert.Equal ("Multiple K and _R", TextFormatter.RemoveHotKeySpecifier ("Multiple _K and _R", 9, hotKeySpecifier));
			Assert.Equal ("Non-english: Кдать", TextFormatter.RemoveHotKeySpecifier ("Non-english: _Кдать", 13, hotKeySpecifier));
		}

		[Fact]
		public void RemoveHotKeySpecifier_Valid_Legacy_ReturnsOriginal ()
		{
			Rune hotKeySpecifier = (Rune)'_';

			Assert.Equal ("all lower case", TextFormatter.RemoveHotKeySpecifier ("all lower case", 0, hotKeySpecifier));
			Assert.Equal ("K Before", TextFormatter.RemoveHotKeySpecifier ("K Before", 0, hotKeySpecifier));
			Assert.Equal ("aK Second", TextFormatter.RemoveHotKeySpecifier ("aK Second", 1, hotKeySpecifier));
			Assert.Equal ("Last K", TextFormatter.RemoveHotKeySpecifier ("Last K", 5, hotKeySpecifier));
			Assert.Equal ("After K", TextFormatter.RemoveHotKeySpecifier ("After K", 7, hotKeySpecifier));
			Assert.Equal ("Multiple K and R", TextFormatter.RemoveHotKeySpecifier ("Multiple K and R", 9, hotKeySpecifier));
			Assert.Equal ("Non-english: Кдать", TextFormatter.RemoveHotKeySpecifier ("Non-english: Кдать", 13, hotKeySpecifier));
		}

		[Fact]
		public void CalcRect_Invalid_Returns_Empty ()
		{
			Assert.Equal (Rect.Empty, TextFormatter.CalcRect (0, 0, null));
			Assert.Equal (Rect.Empty, TextFormatter.CalcRect (0, 0, ""));
			Assert.Equal (new Rect (new Point (1, 2), Size.Empty), TextFormatter.CalcRect (1, 2, ""));
			Assert.Equal (new Rect (new Point (-1, -2), Size.Empty), TextFormatter.CalcRect (-1, -2, ""));
		}

		[Fact]
		public void CalcRect_SingleLine_Returns_1High ()
		{
			var text = string.Empty;

			text = "test";
			Assert.Equal (new Rect (0, 0, text.RuneCount (), 1), TextFormatter.CalcRect (0, 0, text));
			Assert.Equal (new Rect (0, 0, text.ConsoleWidth (), 1), TextFormatter.CalcRect (0, 0, text));

			text = " ~  s  gui.cs   master ↑10";
			Assert.Equal (new Rect (0, 0, text.RuneCount (), 1), TextFormatter.CalcRect (0, 0, text));
			Assert.Equal (new Rect (0, 0, text.ConsoleWidth (), 1), TextFormatter.CalcRect (0, 0, text));
		}

		[Fact]
		public void CalcRect_MultiLine_Returns_nHigh ()
		{
			var text = string.Empty;
			var lines = 0;

			text = "line1\nline2";
			lines = 2;
			Assert.Equal (new Rect (0, 0, 5, lines), TextFormatter.CalcRect (0, 0, text));

			text = "\nline2";
			lines = 2;
			Assert.Equal (new Rect (0, 0, 5, lines), TextFormatter.CalcRect (0, 0, text));

			text = "\n\n";
			lines = 3;
			Assert.Equal (new Rect (0, 0, 0, lines), TextFormatter.CalcRect (0, 0, text));

			text = "\n\n\n";
			lines = 4;
			Assert.Equal (new Rect (0, 0, 0, lines), TextFormatter.CalcRect (0, 0, text));

			text = "line1\nline2\nline3long!";
			lines = 3;
			Assert.Equal (new Rect (0, 0, 10, lines), TextFormatter.CalcRect (0, 0, text));

			text = "line1\nline2\n\n";
			lines = 4;
			Assert.Equal (new Rect (0, 0, 5, lines), TextFormatter.CalcRect (0, 0, text));

			text = "line1\r\nline2";
			lines = 2;
			Assert.Equal (new Rect (0, 0, 5, lines), TextFormatter.CalcRect (0, 0, text));

			text = " ~  s  gui.cs   master ↑10\n";
			lines = 2;
			Assert.Equal (new Rect (0, 0, text.RuneCount () - 1, lines), TextFormatter.CalcRect (0, 0, text));
			Assert.Equal (new Rect (0, 0, text.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 0)), lines), TextFormatter.CalcRect (0, 0, text));

			text = "\n ~  s  gui.cs   master ↑10";
			lines = 2;
			Assert.Equal (new Rect (0, 0, text.RuneCount () - 1, lines), TextFormatter.CalcRect (0, 0, text));
			Assert.Equal (new Rect (0, 0, text.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 0)), lines), TextFormatter.CalcRect (0, 0, text));

			text = " ~  s  gui.cs   master\n↑10";
			lines = 2;
			Assert.Equal (new Rect (0, 0, " ~  s  gui.cs   master\n".RuneCount () - 1, lines), TextFormatter.CalcRect (0, 0, text));
			Assert.Equal (new Rect (0, 0, " ~  s  gui.cs   master\n".ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 0)), lines), TextFormatter.CalcRect (0, 0, text));
		}

		[Fact]
		public void ClipAndJustify_Invalid_Returns_Original ()
		{
			var text = string.Empty;

			Assert.Equal (text, TextFormatter.ClipAndJustify (text, 0, TextAlignment.Left));

			text = null;
			Assert.Equal (text, TextFormatter.ClipAndJustify (text, 0, TextAlignment.Left));

			text = "test";
			Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.ClipAndJustify (text, -1, TextAlignment.Left));
		}

		[Fact]
		public void ClipAndJustify_Valid_Left ()
		{
			var align = TextAlignment.Left;

			var text = string.Empty;
			var justifiedText = string.Empty;
			int maxWidth = 0;
			int expectedClippedWidth = 0;

			text = "test";
			maxWidth = 0;
			Assert.Equal (string.Empty, justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));

			text = "test";
			maxWidth = 2;
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..maxWidth]), justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));

			text = "test";
			maxWidth = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));
			Assert.True (justifiedText.RuneCount () <= maxWidth);
			Assert.True (justifiedText.ConsoleWidth () <= maxWidth);

			text = "A sentence has words.";
			// should fit
			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "A\tsentence\thas\twords.";
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "A\tsentence\thas\twords.";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "line1\nline2\nline3long!";
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "line1\nline2\nline3long!";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Unicode
			text = " ~  s  gui.cs   master ↑10";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// should fit
			text = "Ð ÑÐ";
			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);
		}

		[Fact]
		public void ClipAndJustify_Valid_Right ()
		{
			var align = TextAlignment.Right;

			var text = string.Empty;
			var justifiedText = string.Empty;
			int maxWidth = 0;
			int expectedClippedWidth = 0;

			text = "test";
			maxWidth = 0;
			Assert.Equal (string.Empty, justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));

			text = "test";
			maxWidth = 2;
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..maxWidth]), justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));

			text = "test";
			maxWidth = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));
			Assert.True (justifiedText.RuneCount () <= maxWidth);
			Assert.True (justifiedText.ConsoleWidth () <= maxWidth);

			text = "A sentence has words.";
			// should fit
			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "A\tsentence\thas\twords.";
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "A\tsentence\thas\twords.";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "line1\nline2\nline3long!";
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "line1\nline2\nline3long!";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Unicode
			text = " ~  s  gui.cs   master ↑10";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// should fit
			text = "Ð ÑÐ";
			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);
		}

		[Fact]
		public void ClipAndJustify_Valid_Centered ()
		{
			var align = TextAlignment.Centered;

			var text = string.Empty;
			var justifiedText = string.Empty;
			int maxWidth = 0;
			int expectedClippedWidth = 0;

			text = "test";
			maxWidth = 0;
			Assert.Equal (string.Empty, justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));

			text = "test";
			maxWidth = 2;
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..maxWidth]), justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));

			text = "test";
			maxWidth = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));
			Assert.True (justifiedText.RuneCount () <= maxWidth);
			Assert.True (justifiedText.ConsoleWidth () <= maxWidth);

			text = "A sentence has words.";
			// should fit
			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "A\tsentence\thas\twords.";
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "A\tsentence\thas\twords.";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "line1\nline2\nline3long!";
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "line1\nline2\nline3long!";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Unicode
			text = " ~  s  gui.cs   master ↑10";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// should fit
			text = "Ð ÑÐ";
			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);
		}

		[Fact]
		public void ClipAndJustify_Valid_Justified ()
		{
			var align = TextAlignment.Justified;

			var text = string.Empty;
			var justifiedText = string.Empty;
			int maxWidth = 0;
			int expectedClippedWidth = 0;

			text = "test";
			maxWidth = 0;
			Assert.Equal (string.Empty, justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));

			text = "test";
			maxWidth = 2;
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..maxWidth]), justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));

			text = "test";
			maxWidth = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align));
			Assert.True (justifiedText.RuneCount () <= maxWidth);

			text = "A sentence has words.";
			// should fit
			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Max (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Throws<ArgumentOutOfRangeException> (() => StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]));

			// Should fit.
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			//Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = 500;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			//Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.True (expectedClippedWidth <= maxWidth);
			//Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			//Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			//Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "A\tsentence\thas\twords.";
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			//Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "A\tsentence\thas\twords.";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "line1\nline2\nline3long!";
			maxWidth = int.MaxValue;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			text = "line1\nline2\nline3long!";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)));
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Unicode
			text = " ~  s  gui.cs   master ↑10";
			maxWidth = 10;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// should fit
			text = "Ð ÑÐ";
			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			//Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.True (expectedClippedWidth <= maxWidth);
			//Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should fit.
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// Should not fit
			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			justifiedText = TextFormatter.ClipAndJustify (text, maxWidth, align);
			Assert.Equal (expectedClippedWidth, justifiedText.RuneCount ());
			Assert.Equal (expectedClippedWidth, justifiedText.ConsoleWidth ());
			Assert.True (expectedClippedWidth <= maxWidth);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), justifiedText);

			// see Justify_ tests below
		}

		[Fact]
		public void Justify_Invalid ()
		{
			var text = string.Empty;
			Assert.Equal (text, TextFormatter.Justify (text, 0));

			text = null;
			Assert.Equal (text, TextFormatter.Justify (text, 0));

			text = "test";
			Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.Justify (text, -1));
		}

		[Fact]
		public void Justify_SingleWord ()
		{
			var text = string.Empty;
			var justifiedText = string.Empty;
			int width = 0;
			char fillChar = '+';

			// Even # of chars
			text = "word";
			justifiedText = text;

			width = text.RuneCount ();
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 1;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 2;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 10;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 11;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));

			// Odd # of chars
			text = "word.";
			justifiedText = text;

			width = text.RuneCount ();
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 1;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 2;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 10;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 11;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));

			// Unicode (even #)
			text = "Ð¿ÑÐ¸Ð²ÐµÑ";
			justifiedText = text;

			width = text.RuneCount ();
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 1;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 2;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 10;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 11;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));

			// Unicode (odd # of chars)
			text = "Ð¿ÑÐ¸Ð²ÐµÑ.";
			justifiedText = text;

			width = text.RuneCount ();
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 1;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 2;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 10;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
			width = text.RuneCount () + 11;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, width, fillChar));
		}

		[Fact]
		public void Justify_Sentence ()
		{
			var text = string.Empty;
			var justifiedText = string.Empty;
			int forceToWidth = 0;
			char fillChar = '+';

			// Even # of spaces
			//      0123456789
			text = "012 456 89";

			forceToWidth = text.RuneCount ();
			justifiedText = text.Replace (" ", "+");
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012++456+89";
			forceToWidth = text.RuneCount () + 1;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "++");
			forceToWidth = text.RuneCount () + 2;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012+++456++89";
			forceToWidth = text.RuneCount () + 3;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "+++");
			forceToWidth = text.RuneCount () + 4;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012++++456+++89";
			forceToWidth = text.RuneCount () + 5;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "++++");
			forceToWidth = text.RuneCount () + 6;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "+++++++++++");
			forceToWidth = text.RuneCount () + 20;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012+++++++++++++456++++++++++++89";
			forceToWidth = text.RuneCount () + 23;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			// Odd # of spaces
			//      0123456789
			text = "012 456 89 end";

			forceToWidth = text.RuneCount ();
			justifiedText = text.Replace (" ", "+");
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012++456+89+end";
			forceToWidth = text.RuneCount () + 1;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012++456++89+end";
			forceToWidth = text.RuneCount () + 2;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "++");
			forceToWidth = text.RuneCount () + 3;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012+++456++89++end";
			forceToWidth = text.RuneCount () + 4;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012+++456+++89++end";
			forceToWidth = text.RuneCount () + 5;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "+++");
			forceToWidth = text.RuneCount () + 6;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012++++++++456++++++++89+++++++end";
			forceToWidth = text.RuneCount () + 20;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "012+++++++++456+++++++++89++++++++end";
			forceToWidth = text.RuneCount () + 23;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			// Unicode
			// Even # of chars
			//      0123456789
			text = "Ð¿ÑÐ Ð²Ð Ñ";

			forceToWidth = text.RuneCount ();
			justifiedText = text.Replace (" ", "+");
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð¿ÑÐ++Ð²Ð+Ñ";
			forceToWidth = text.RuneCount () + 1;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "++");
			forceToWidth = text.RuneCount () + 2;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð¿ÑÐ+++Ð²Ð++Ñ";
			forceToWidth = text.RuneCount () + 3;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "+++");
			forceToWidth = text.RuneCount () + 4;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð¿ÑÐ++++Ð²Ð+++Ñ";
			forceToWidth = text.RuneCount () + 5;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "++++");
			forceToWidth = text.RuneCount () + 6;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "+++++++++++");
			forceToWidth = text.RuneCount () + 20;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð¿ÑÐ+++++++++++++Ð²Ð++++++++++++Ñ";
			forceToWidth = text.RuneCount () + 23;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			// Unicode
			// Odd # of chars
			//      0123456789
			text = "Ð ÑÐ Ð²Ð Ñ";

			forceToWidth = text.RuneCount ();
			justifiedText = text.Replace (" ", "+");
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð++ÑÐ+Ð²Ð+Ñ";
			forceToWidth = text.RuneCount () + 1;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð++ÑÐ++Ð²Ð+Ñ";
			forceToWidth = text.RuneCount () + 2;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "++");
			forceToWidth = text.RuneCount () + 3;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð+++ÑÐ++Ð²Ð++Ñ";
			forceToWidth = text.RuneCount () + 4;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð+++ÑÐ+++Ð²Ð++Ñ";
			forceToWidth = text.RuneCount () + 5;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = text.Replace (" ", "+++");
			forceToWidth = text.RuneCount () + 6;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð++++++++ÑÐ++++++++Ð²Ð+++++++Ñ";
			forceToWidth = text.RuneCount () + 20;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));

			justifiedText = "Ð+++++++++ÑÐ+++++++++Ð²Ð++++++++Ñ";
			forceToWidth = text.RuneCount () + 23;
			Assert.Equal (justifiedText, TextFormatter.Justify (text, forceToWidth, fillChar));
			Assert.True (Math.Abs (forceToWidth - justifiedText.RuneCount ()) < text.Count (s => s == ' '));
			Assert.True (Math.Abs (forceToWidth - justifiedText.ConsoleWidth ()) < text.Count (s => s == ' '));
		}

		[Fact]
		public void WordWrap_Invalid ()
		{
			var text = string.Empty;
			int width = 0;

			Assert.Empty (TextFormatter.WordWrapText (null, width));
			Assert.Empty (TextFormatter.WordWrapText (text, width));
			Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.WordWrapText (text, -1));
		}

		[Fact]
		public void WordWrap_BigWidth ()
		{
			List<string> wrappedLines;

			var text = "Constantinople";
			wrappedLines = TextFormatter.WordWrapText (text, 100);
			Assert.True (wrappedLines.Count == 1);
			Assert.Equal ("Constantinople", wrappedLines [0]);
		}

		[Fact]
		public void WordWrap_SingleWordLine ()
		{
			var text = string.Empty;
			int width = 0;
			List<string> wrappedLines;

			text = "Constantinople";
			width = text.RuneCount ();
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.True (wrappedLines.Count == 1);

			width = text.RuneCount () - 1;
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal (text [..(text.RuneCount () - 1)], wrappedLines [0]);
			Assert.Equal ("e", wrappedLines [1]);

			width = text.RuneCount () - 2;
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal (text [..(text.RuneCount () - 2)], wrappedLines [0]);

			width = text.RuneCount () - 5;
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);

			width = (int)Math.Ceiling ((double)(text.RuneCount () / 2F));
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal ("Constan", wrappedLines [0]);
			Assert.Equal ("tinople", wrappedLines [1]);

			width = (int)Math.Ceiling ((double)(text.RuneCount () / 3F));
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (3, wrappedLines.Count);
			Assert.Equal ("Const", wrappedLines [0]);
			Assert.Equal ("antin", wrappedLines [1]);
			Assert.Equal ("ople", wrappedLines [2]);

			width = (int)Math.Ceiling ((double)(text.RuneCount () / 4F));
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (4, wrappedLines.Count);

			width = (int)Math.Ceiling ((double)text.RuneCount () / text.RuneCount ());
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (text.RuneCount (), wrappedLines.Count);
			Assert.Equal (text.ConsoleWidth (), wrappedLines.Count);
			Assert.Equal ("C", wrappedLines [0]);
			Assert.Equal ("o", wrappedLines [1]);
			Assert.Equal ("n", wrappedLines [2]);
			Assert.Equal ("s", wrappedLines [3]);
			Assert.Equal ("t", wrappedLines [4]);
			Assert.Equal ("a", wrappedLines [5]);
			Assert.Equal ("n", wrappedLines [6]);
			Assert.Equal ("t", wrappedLines [7]);
			Assert.Equal ("i", wrappedLines [8]);
			Assert.Equal ("n", wrappedLines [9]);
			Assert.Equal ("o", wrappedLines [10]);
			Assert.Equal ("p", wrappedLines [11]);
			Assert.Equal ("l", wrappedLines [12]);
			Assert.Equal ("e", wrappedLines [13]);
		}

		[Fact]
		public void WordWrap_Unicode_SingleWordLine ()
		{
			var text = string.Empty;
			int width = 0;
			List<string> wrappedLines;

			text = "กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ";
			width = text.RuneCount ();
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.True (wrappedLines.Count == 1);

			width = text.RuneCount () - 1;
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..(text.RuneCount () - 1)]), wrappedLines [0]);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..(text.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)) - 1)]), wrappedLines [0]);
			Assert.Equal ("ำ", wrappedLines [1]);

			width = text.RuneCount () - 2;
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..(text.RuneCount () - 2)]), wrappedLines [0]);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..(text.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)) - 2)]), wrappedLines [0]);

			width = text.RuneCount () - 5;
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);

			width = (int)Math.Ceiling ((double)(text.RuneCount () / 2F));
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal ("กขฃคฅฆงจฉชซฌญฎฏฐฑฒณดตถทธนบ", wrappedLines [0]);
			Assert.Equal ("ปผฝพฟภมยรฤลฦวศษสหฬอฮฯะัาำ", wrappedLines [1]);

			width = (int)Math.Ceiling ((double)(text.RuneCount () / 3F));
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (3, wrappedLines.Count);
			Assert.Equal ("กขฃคฅฆงจฉชซฌญฎฏฐฑ", wrappedLines [0]);
			Assert.Equal ("ฒณดตถทธนบปผฝพฟภมย", wrappedLines [1]);
			Assert.Equal ("รฤลฦวศษสหฬอฮฯะัาำ", wrappedLines [2]);

			width = (int)Math.Ceiling ((double)(text.RuneCount () / 4F));
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (4, wrappedLines.Count);

			width = (int)Math.Ceiling ((double)text.RuneCount () / text.RuneCount ());
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (text.RuneCount (), wrappedLines.Count);
			Assert.Equal (text.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)), wrappedLines.Count);
			Assert.Equal ("ก", wrappedLines [0]);
			Assert.Equal ("ข", wrappedLines [1]);
			Assert.Equal ("ฃ", wrappedLines [2]);
			Assert.Equal ("ำ", wrappedLines [^1]);
		}

		[Fact]
		public void WordWrap_Unicode_LineWithNonBreakingSpace ()
		{
			var text = string.Empty;
			int width = 0;
			List<string> wrappedLines;

			text = "This\u00A0is\u00A0a\u00A0sentence.";
			width = text.RuneCount ();
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.True (wrappedLines.Count == 1);

			width = text.RuneCount () - 1;
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..(text.RuneCount () - 1)]), wrappedLines [0]);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..(text.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)) - 1)]), wrappedLines [0]);
			Assert.Equal (".", wrappedLines [1]);

			width = text.RuneCount () - 2;
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..(text.RuneCount () - 2)]), wrappedLines [0]);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..(text.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)) - 2)]), wrappedLines [0]);

			width = text.RuneCount () - 5;
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);

			width = (int)Math.Ceiling ((double)(text.RuneCount () / 2F));
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal ("This\u00A0is\u00A0a\u00A0", wrappedLines [0]);
			Assert.Equal ("sentence.", wrappedLines [1]);

			width = (int)Math.Ceiling ((double)(text.RuneCount () / 3F));
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (3, wrappedLines.Count);
			Assert.Equal ("This\u00A0is", wrappedLines [0]);
			Assert.Equal ("\u00a0a\u00a0sent", wrappedLines [1]);
			Assert.Equal ("ence.", wrappedLines [2]);

			width = (int)Math.Ceiling ((double)(text.RuneCount () / 4F));
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (4, wrappedLines.Count);

			width = (int)Math.Ceiling ((double)text.RuneCount () / text.RuneCount ());
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (text.RuneCount (), wrappedLines.Count);
			Assert.Equal (text.ToRuneList ().Sum (r => Math.Max (r.ColumnWidth (), 1)), wrappedLines.Count);
			Assert.Equal ("T", wrappedLines [0]);
			Assert.Equal ("h", wrappedLines [1]);
			Assert.Equal ("i", wrappedLines [2]);
			Assert.Equal (".", wrappedLines [^1]);
		}

		[Fact]
		public void WordWrap_Unicode_2LinesWithNonBreakingSpace ()
		{
			var text = string.Empty;
			int width = 0;
			List<string> wrappedLines;

			text = "This\u00A0is\n\u00A0a\u00A0sentence.";
			width = text.RuneCount ();
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.True (wrappedLines.Count == 1);

			width = text.RuneCount () - 1;
			wrappedLines = TextFormatter.WordWrapText (text, width);
#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
			Assert.Equal (1, wrappedLines.Count);
#pragma warning restore xUnit2013 // Do not use equality check to check for collection size.
			Assert.Equal ("This\u00A0is\u00A0a\u00A0sentence.", wrappedLines [0]);

			text = "\u00A0\u00A0\u00A0\u00A0\u00A0test\u00A0sentence.";
			width = text.RuneCount ();
			wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.True (wrappedLines.Count == 1);
		}

		[Fact]
		public void WordWrap_NoNewLines_Default ()
		{
			// Calls WordWrapText (text, width) and thus preserveTrailingSpaces defaults to false
			var text = string.Empty;
			int maxWidth = 0;
			int expectedClippedWidth = 0;

			List<string> wrappedLines;

			text = "A sentence has words.";
			maxWidth = text.RuneCount ();
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.True (wrappedLines.Count == 1);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A sentence has words.", wrappedLines [0]);

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A sentence has", wrappedLines [0]);
			Assert.Equal ("words.", wrappedLines [1]);

			maxWidth = text.RuneCount () - "words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A sentence has", wrappedLines [0]);
			Assert.Equal ("words.", wrappedLines [1]);

			maxWidth = text.RuneCount () - " words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A sentence has", wrappedLines [0]);
			Assert.Equal ("words.", wrappedLines [1]);

			maxWidth = text.RuneCount () - "s words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A sentence", wrappedLines [0]);
			Assert.Equal ("has words.", wrappedLines [1]);

			// Unicode 
			text = "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.";
			maxWidth = text.RuneCount ();
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.True (wrappedLines.Count == 1);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.", wrappedLines [0]);

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", wrappedLines [0]);
			Assert.Equal ("words.", wrappedLines [1]);

			maxWidth = text.RuneCount () - "words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", wrappedLines [0]);
			Assert.Equal ("words.", wrappedLines [1]);

			maxWidth = text.RuneCount () - " words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", wrappedLines [0]);
			Assert.Equal ("words.", wrappedLines [1]);

			maxWidth = text.RuneCount () - "s words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ)", wrappedLines [0]);
			Assert.Equal ("has words.", wrappedLines [1]);

			maxWidth = text.RuneCount () - "Ð²ÐµÑ) has words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence", wrappedLines [0]);
			Assert.Equal ("(Ð¿ÑÐ¸Ð²ÐµÑ) has words.", wrappedLines [1]);
		}

		/// <summary>
		/// WordWrap strips CRLF
		/// </summary>
		[Fact]
		public void WordWrap_WithNewLines ()
		{
			var text = string.Empty;
			int maxWidth = 0;
			int expectedClippedWidth = 0;

			List<string> wrappedLines;

			text = "A sentence has words.\nA paragraph has lines.";
			maxWidth = text.RuneCount ();
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
			Assert.Equal (1, wrappedLines.Count);
#pragma warning restore xUnit2013 // Do not use equality check to check for collection size.
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.Equal ("A sentence has words.A paragraph has lines.", wrappedLines [0]);

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
			Assert.Equal (1, wrappedLines.Count);
#pragma warning restore xUnit2013 // Do not use equality check to check for collection size.
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A sentence has words.A paragraph has lines.", wrappedLines [0]);

			maxWidth = text.RuneCount () - "words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A sentence has words.A paragraph has", wrappedLines [0]);
			Assert.Equal ("lines.", wrappedLines [1]);

			// Unicode 
			text = "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has Линии.";
			maxWidth = text.RuneCount ();
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.True (wrappedLines.Count == 1);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has Линии.", wrappedLines [0]);

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has", wrappedLines [0]);
			Assert.Equal ("Линии.", wrappedLines [1]);

			maxWidth = text.RuneCount () - "words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт has", wrappedLines [0]);
			Assert.Equal ("Линии.", wrappedLines [1]);

			maxWidth = text.RuneCount () - "s words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode Пункт", wrappedLines [0]);
			Assert.Equal ("has Линии.", wrappedLines [1]);

			maxWidth = text.RuneCount () - "Ð²ÐµÑ) has words.".Length;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.Equal (2, wrappedLines.Count);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.A Unicode", wrappedLines [0]);
			Assert.Equal ("Пункт has Линии.", wrappedLines [1]);
		}

		[Fact]
		public void WordWrap_Narrow_Default ()
		{
			// Calls WordWrapText (text, width) and thus preserveTrailingSpaces defaults to false
			var text = string.Empty;
			int maxWidth = 1;
			int expectedClippedWidth = 1;

			List<string> wrappedLines;

			text = "A sentence has words.";
			maxWidth = 3;
			expectedClippedWidth = 3;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A", wrappedLines [0]);
			Assert.Equal ("sen", wrappedLines [1]);
			Assert.Equal ("ten", wrappedLines [2]);
			Assert.Equal ("ce", wrappedLines [3]);
			Assert.Equal ("has", wrappedLines [4]);
			Assert.Equal ("wor", wrappedLines [5]);
			Assert.Equal ("ds.", wrappedLines [6]);

			maxWidth = 2;
			expectedClippedWidth = 2;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A", wrappedLines [0]);
			Assert.Equal ("se", wrappedLines [1]);
			Assert.Equal ("nt", wrappedLines [2]);
			Assert.Equal ("en", wrappedLines [3]);
			Assert.Equal ("s.", wrappedLines [^1]);

			maxWidth = 1;
			expectedClippedWidth = 1;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A", wrappedLines [0]);
			Assert.Equal ("s", wrappedLines [1]);
			Assert.Equal ("e", wrappedLines [2]);
			Assert.Equal ("n", wrappedLines [3]);
			Assert.Equal (".", wrappedLines [^1]);
		}

		[Fact]
		public void WordWrap_PreserveTrailingSpaces_True ()
		{
			var text = string.Empty;
			int maxWidth = 1;
			int expectedClippedWidth = 1;

			List<string> wrappedLines;

			text = "A sentence has words.";
			maxWidth = 14;
			expectedClippedWidth = 14;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A sentence ", wrappedLines [0]);
			Assert.Equal ("has words.", wrappedLines [1]);
			Assert.True (wrappedLines.Count == 2);

			maxWidth = 8;
			expectedClippedWidth = 8;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A ", wrappedLines [0]);
			Assert.Equal ("sentence", wrappedLines [1]);
			Assert.Equal (" has ", wrappedLines [2]);
			Assert.Equal ("words.", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 4);

			maxWidth = 6;
			expectedClippedWidth = 6;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A ", wrappedLines [0]);
			Assert.Equal ("senten", wrappedLines [1]);
			Assert.Equal ("ce ", wrappedLines [2]);
			Assert.Equal ("has ", wrappedLines [3]);
			Assert.Equal ("words.", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 5);

			maxWidth = 3;
			expectedClippedWidth = 3;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A ", wrappedLines [0]);
			Assert.Equal ("sen", wrappedLines [1]);
			Assert.Equal ("ten", wrappedLines [2]);
			Assert.Equal ("ce ", wrappedLines [3]);
			Assert.Equal ("has", wrappedLines [4]);
			Assert.Equal (" ", wrappedLines [5]);
			Assert.Equal ("wor", wrappedLines [6]);
			Assert.Equal ("ds.", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 8);

			maxWidth = 2;
			expectedClippedWidth = 2;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A ", wrappedLines [0]);
			Assert.Equal ("se", wrappedLines [1]);
			Assert.Equal ("nt", wrappedLines [2]);
			Assert.Equal ("en", wrappedLines [3]);
			Assert.Equal ("ce", wrappedLines [4]);
			Assert.Equal (" ", wrappedLines [5]);
			Assert.Equal ("ha", wrappedLines [6]);
			Assert.Equal ("s ", wrappedLines [7]);
			Assert.Equal ("wo", wrappedLines [8]);
			Assert.Equal ("rd", wrappedLines [9]);
			Assert.Equal ("s.", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 11);

			maxWidth = 1;
			expectedClippedWidth = 1;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("A", wrappedLines [0]);
			Assert.Equal (" ", wrappedLines [1]);
			Assert.Equal ("s", wrappedLines [2]);
			Assert.Equal ("e", wrappedLines [3]);
			Assert.Equal ("n", wrappedLines [4]);
			Assert.Equal ("t", wrappedLines [5]);
			Assert.Equal ("e", wrappedLines [6]);
			Assert.Equal ("n", wrappedLines [7]);
			Assert.Equal ("c", wrappedLines [8]);
			Assert.Equal ("e", wrappedLines [9]);
			Assert.Equal (" ", wrappedLines [10]);
			Assert.Equal (".", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == text.Length);
		}

		[Fact]
		public void WordWrap_PreserveTrailingSpaces_True_Wide_Runes ()
		{
			var text = string.Empty;
			int maxWidth = 1;
			int expectedClippedWidth = 1;

			List<string> wrappedLines;

			text = "文に は言葉 があり ます。";
			maxWidth = 14;
			expectedClippedWidth = 14;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("文に は言葉 ", wrappedLines [0]);
			Assert.Equal ("があり ます。", wrappedLines [1]);
			Assert.True (wrappedLines.Count == 2);

			maxWidth = 3;
			expectedClippedWidth = 3;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("文", wrappedLines [0]);
			Assert.Equal ("に ", wrappedLines [1]);
			Assert.Equal ("は", wrappedLines [2]);
			Assert.Equal ("言", wrappedLines [3]);
			Assert.Equal ("葉 ", wrappedLines [4]);
			Assert.Equal ("が", wrappedLines [5]);
			Assert.Equal ("あ", wrappedLines [6]);
			Assert.Equal ("り ", wrappedLines [7]);
			Assert.Equal ("ま", wrappedLines [8]);
			Assert.Equal ("す", wrappedLines [9]);
			Assert.Equal ("。", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 11);

			maxWidth = 2;
			expectedClippedWidth = 2;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("文", wrappedLines [0]);
			Assert.Equal ("に", wrappedLines [1]);
			Assert.Equal (" ", wrappedLines [2]);
			Assert.Equal ("は", wrappedLines [3]);
			Assert.Equal ("言", wrappedLines [4]);
			Assert.Equal ("葉", wrappedLines [5]);
			Assert.Equal (" ", wrappedLines [6]);
			Assert.Equal ("が", wrappedLines [7]);
			Assert.Equal ("あ", wrappedLines [8]);
			Assert.Equal ("り", wrappedLines [9]);
			Assert.Equal (" ", wrappedLines [10]);
			Assert.Equal ("ま", wrappedLines [11]);
			Assert.Equal ("す", wrappedLines [12]);
			Assert.Equal ("。", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 14);

			maxWidth = 1;
			expectedClippedWidth = 0;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.Empty (wrappedLines);
			Assert.False (wrappedLines.Count == text.Length);
			Assert.False (wrappedLines.Count == text.RuneCount ());
			Assert.False (wrappedLines.Count == text.ConsoleWidth ());
			Assert.Equal (25, text.ConsoleWidth ());
			Assert.Equal (25, TextFormatter.GetTextWidth (text));
		}

		[Fact]
		public void WordWrap_PreserveTrailingSpaces_False_Wide_Runes ()
		{
			var text = string.Empty;
			int maxWidth = 1;
			int expectedClippedWidth = 1;

			List<string> wrappedLines;

			text = "文に は言葉 があり ます。";
			maxWidth = 14;
			expectedClippedWidth = 14;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("文に は言葉 ", wrappedLines [0]);
			Assert.Equal ("があり ます。", wrappedLines [1]);
			Assert.True (wrappedLines.Count == 2);

			maxWidth = 3;
			expectedClippedWidth = 3;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("文", wrappedLines [0]);
			Assert.Equal ("に ", wrappedLines [1]);
			Assert.Equal ("は", wrappedLines [2]);
			Assert.Equal ("言", wrappedLines [3]);
			Assert.Equal ("葉 ", wrappedLines [4]);
			Assert.Equal ("が", wrappedLines [5]);
			Assert.Equal ("あ", wrappedLines [6]);
			Assert.Equal ("り ", wrappedLines [7]);
			Assert.Equal ("ま", wrappedLines [8]);
			Assert.Equal ("す", wrappedLines [9]);
			Assert.Equal ("。", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 11);

			maxWidth = 2;
			expectedClippedWidth = 2;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			Assert.Equal ("文", wrappedLines [0]);
			Assert.Equal ("に", wrappedLines [1]);
			Assert.Equal (" ", wrappedLines [2]);
			Assert.Equal ("は", wrappedLines [3]);
			Assert.Equal ("言", wrappedLines [4]);
			Assert.Equal ("葉", wrappedLines [5]);
			Assert.Equal (" ", wrappedLines [6]);
			Assert.Equal ("が", wrappedLines [7]);
			Assert.Equal ("あ", wrappedLines [8]);
			Assert.Equal ("り", wrappedLines [9]);
			Assert.Equal (" ", wrappedLines [10]);
			Assert.Equal ("ま", wrappedLines [11]);
			Assert.Equal ("す", wrappedLines [12]);
			Assert.Equal ("。", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 14);

			maxWidth = 1;
			expectedClippedWidth = 0;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true);
			Assert.Empty (wrappedLines);
			Assert.False (wrappedLines.Count == text.Length);
			Assert.False (wrappedLines.Count == text.RuneCount ());
			Assert.False (wrappedLines.Count == text.ConsoleWidth ());
			Assert.Equal (25, text.ConsoleWidth ());
			Assert.Equal (25, TextFormatter.GetTextWidth (text));
			//var text = string.Empty;
			//int maxWidth = 1;
			//int expectedClippedWidth = 1;

			//List<string> wrappedLines;

			//text = "文に は言葉 があり ます。";
			//maxWidth = 14;
			//expectedClippedWidth = 14;
			//wrappedLines = TextFormatter.WordWrapText (text, maxWidth, false);
			//Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			//Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			//Assert.Equal ("文に は言葉", wrappedLines [0]);
			//Assert.Equal ("があり ます。", wrappedLines [1]);
			//Assert.True (wrappedLines.Count == 2);

			//maxWidth = 3;
			//expectedClippedWidth = 3;
			//wrappedLines = TextFormatter.WordWrapText (text, maxWidth, false);
			//Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			//Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			//Assert.Equal ("文 ", wrappedLines [0]);
			//Assert.Equal ("に ", wrappedLines [1]);
			//Assert.Equal ("は ", wrappedLines [2]);
			//Assert.Equal ("言 ", wrappedLines [3]);
			//Assert.Equal ("葉 ", wrappedLines [4]);
			//Assert.Equal ("が ", wrappedLines [5]);
			//Assert.Equal ("あ ", wrappedLines [6]);
			//Assert.Equal ("り ", wrappedLines [7]);
			//Assert.Equal ("ま ", wrappedLines [8]);
			//Assert.Equal ("す ", wrappedLines [9]);
			//Assert.Equal ("。", wrappedLines [^1]);
			//Assert.Equal (11, wrappedLines.Count);

			//maxWidth = 2;
			//expectedClippedWidth = 2;
			//wrappedLines = TextFormatter.WordWrapText (text, maxWidth, false);
			//Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			//Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.ConsoleWidth ()));
			//Assert.Equal ("文", wrappedLines [0]);
			//Assert.Equal ("に", wrappedLines [1]);
			//Assert.Equal ("は", wrappedLines [2]);
			//Assert.Equal ("言", wrappedLines [3]);
			//Assert.Equal ("葉", wrappedLines [4]);
			//Assert.Equal ("が", wrappedLines [5]);
			//Assert.Equal ("あ", wrappedLines [6]);
			//Assert.Equal ("り", wrappedLines [7]);
			//Assert.Equal ("ま", wrappedLines [8]);
			//Assert.Equal ("す", wrappedLines [9]);
			//Assert.Equal ("。", wrappedLines [^1]);
			//Assert.Equal (11, wrappedLines.Count);

			//maxWidth = 1;
			//expectedClippedWidth = 0;
			//wrappedLines = TextFormatter.WordWrapText (text, maxWidth, false);
			//Assert.Empty (wrappedLines);
		}

		[Fact]
		public void WordWrap_PreserveTrailingSpaces_True_With_Simple_Runes_Width_3 ()
		{
			var text = "A sentence has words. ";
			var width = 3;
			var wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: true);
			var breakLines = "";
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			var expected = "A " + Environment.NewLine +
					"sen" + Environment.NewLine +
					"ten" + Environment.NewLine +
					"ce " + Environment.NewLine +
					"has" + Environment.NewLine +
					" " + Environment.NewLine +
					"wor" + Environment.NewLine +
					"ds." + Environment.NewLine +
					" " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double space Complex example - this is how VS 2022 does it
			//text = "A  sentence      has words.  ";
			//breakLines = "";
			//wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: true);
			//foreach (var line in wrappedLines) {
			//	breakLines += $"{line}{Environment.NewLine}";
			//}
			//expected = "A  " + Environment.NewLine +
			//	" se" + Environment.NewLine +
			//	" nt" + Environment.NewLine +
			//	" en" + Environment.NewLine +
			//	" ce" + Environment.NewLine +
			//	"  " + Environment.NewLine +
			//	"  " + Environment.NewLine +
			//	"  " + Environment.NewLine +
			//	" ha" + Environment.NewLine +
			//	" s " + Environment.NewLine +
			//	" wo" + Environment.NewLine +
			//	" rd" + Environment.NewLine +
			//	" s." + Environment.NewLine;
			//Assert.Equal (expected, breakLines);
		}

		[Fact]
		public void WordWrap_PreserveTrailingSpaces_False_With_Simple_Runes_Width_1 ()
		{
			// Empty input
			string text = null;
			var width = 1;
			var breakLines = "";
			//var wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			//foreach (var line in wrappedLines) {
			//	breakLines += $"{line}{Environment.NewLine}";
			//}
			var expected = string.Empty;
			//Assert.Equal (expected, breakLines);

			// Single Spaces
			text = "1 34";
			breakLines = "";
			var wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine +
				"3" + Environment.NewLine +
				"4" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = string.Empty;
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = string.Empty;
			Assert.Equal (expected, breakLines);

			// Short input
			text = "1";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "12";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine +
				"2" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "123";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine +
				"2" + Environment.NewLine +
				"3" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// No spaces
			text = "123456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine +
				"2" + Environment.NewLine +
				"3" + Environment.NewLine +
				"4" + Environment.NewLine +
				"5" + Environment.NewLine +
				"6" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Just Spaces; should result in a single space
			text = " ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "  ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "   ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine +
				" " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "    ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine +
				" " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Single Spaces
			text = "12 456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine +
				"2" + Environment.NewLine +
				"4" + Environment.NewLine +
				"5" + Environment.NewLine +
				"6" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Leading spaces should be preserved.
			text = " 2 456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine +
				"2" + Environment.NewLine +
				"4" + Environment.NewLine +
				"5" + Environment.NewLine +
				"6" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = " 2 456 8";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine +
				"2" + Environment.NewLine +
				"4" + Environment.NewLine +
				"5" + Environment.NewLine +
				"6" + Environment.NewLine +
				"8" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Complex example
			text = "A sentence has words. ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "A" + Environment.NewLine +
				"s" + Environment.NewLine +
				"e" + Environment.NewLine +
				"n" + Environment.NewLine +
				"t" + Environment.NewLine +
				"e" + Environment.NewLine +
				"n" + Environment.NewLine +
				"c" + Environment.NewLine +
				"e" + Environment.NewLine +
				"h" + Environment.NewLine +
				"a" + Environment.NewLine +
				"s" + Environment.NewLine +
				"w" + Environment.NewLine +
				"o" + Environment.NewLine +
				"r" + Environment.NewLine +
				"d" + Environment.NewLine +
				"s" + Environment.NewLine +
				"." + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double Spaces
			text = "12 456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine +
				"2" + Environment.NewLine +
				"4" + Environment.NewLine +
				"5" + Environment.NewLine +
				"6" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double Leading spaces should be preserved.
			text = "  3 567";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine +
				"3" + Environment.NewLine +
				"5" + Environment.NewLine +
				"6" + Environment.NewLine +
				"7" + Environment.NewLine; Assert.Equal (expected, breakLines);
			Assert.Equal (expected, breakLines);

			text = "  3  678  1";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine +
				"3" + Environment.NewLine +
				" " + Environment.NewLine +
				"6" + Environment.NewLine +
				"7" + Environment.NewLine +
				"8" + Environment.NewLine +
				" " + Environment.NewLine +
				"1" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "1  456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine +
				" " + Environment.NewLine +
				"4" + Environment.NewLine +
				"5" + Environment.NewLine +
				"6" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double space Complex example
			text = "A  sentence   has words.  ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "A" + Environment.NewLine +
				" " + Environment.NewLine +
				"s" + Environment.NewLine +
				"e" + Environment.NewLine +
				"n" + Environment.NewLine +
				"t" + Environment.NewLine +
				"e" + Environment.NewLine +
				"n" + Environment.NewLine +
				"c" + Environment.NewLine +
				"e" + Environment.NewLine +
				" " + Environment.NewLine +
				"h" + Environment.NewLine +
				"a" + Environment.NewLine +
				"s" + Environment.NewLine +
				"w" + Environment.NewLine +
				"o" + Environment.NewLine +
				"r" + Environment.NewLine +
				"d" + Environment.NewLine +
				"s" + Environment.NewLine +
				"." + Environment.NewLine +
				" " + Environment.NewLine;
			Assert.Equal (expected, breakLines);
		}

		[Fact]
		public void WordWrap_PreserveTrailingSpaces_False_With_Simple_Runes_Width_3 ()
		{
			// Empty input
			string text = null;
			var width = 3;
			var breakLines = "";
			var wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			var expected = string.Empty;
			Assert.Equal (expected, breakLines);

			text = string.Empty;
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = string.Empty;
			Assert.Equal (expected, breakLines);

			// Short input
			text = "1";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "12";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "12" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "123";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "123" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// No spaces
			text = "123456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "123" + Environment.NewLine +
					"456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// No spaces
			text = "1234567";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "123" + Environment.NewLine +
				"456" + Environment.NewLine +
				"7" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Just Spaces; should result in a single space
			text = " ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "  ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "  " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "   ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "   " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "    ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "   " + Environment.NewLine;
			//" " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Single Spaces
			text = "12 456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "12" + Environment.NewLine +
				"456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Leading spaces should be preserved.
			text = " 2 456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " 2" + Environment.NewLine +
				"456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = " 2 456 8";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " 2" + Environment.NewLine +
				"456" + Environment.NewLine +
				"8" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Complex example
			text = "A sentence has words. ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "A" + Environment.NewLine +
					"sen" + Environment.NewLine +
					"ten" + Environment.NewLine +
					"ce" + Environment.NewLine +
					"has" + Environment.NewLine +
					"wor" + Environment.NewLine +
					"ds." + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double Spaces
			text = "12 456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "12" + Environment.NewLine +
				"456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double Leading spaces should be preserved.
			text = "  3 567";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "  3" + Environment.NewLine +
				"567" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "  3  678  1";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "  3" + Environment.NewLine +
				" 67" + Environment.NewLine +
				"8 " + Environment.NewLine + // BUGBUG: looks like a trailing space to me!
				"1" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "1  456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1 " + Environment.NewLine + // BUGBUG: looks like a trailing space to me!
				"456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double space Complex example
			text = "A  sentence      has words.  ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "A " + Environment.NewLine +
					"sen" + Environment.NewLine +
					"ten" + Environment.NewLine +
					"ce " + Environment.NewLine +
					"   " + Environment.NewLine +
					"has" + Environment.NewLine +
					"wor" + Environment.NewLine +
					"ds." + Environment.NewLine +
					" " + Environment.NewLine;
			Assert.Equal (expected, breakLines);
		}

		[Fact]
		public void WordWrap_PreserveTrailingSpaces_False_With_Simple_Runes_Width_50 ()
		{
			// Empty input
			string text = null;
			var width = 50;
			var breakLines = "";
			var wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			var expected = string.Empty;
			Assert.Equal (expected, breakLines);

			text = string.Empty;
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = string.Empty;
			Assert.Equal (expected, breakLines);

			// Short input
			text = "1";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "12";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "12" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "123";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "123" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// No spaces
			text = "123456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "123456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// No spaces
			text = "1234567";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1234567" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Just Spaces; should result in a single space
			text = " ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "  ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "  " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "   ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "   " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "    ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "    " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Single Spaces
			text = "12 456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "12 456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Leading spaces should be preserved.
			text = " 2 456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " 2 456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = " 2 456 8";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = " 2 456 8" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Complex example
			text = "A sentence has words. ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "A sentence has words. " + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double Spaces
			text = "12 456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "12 456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double Leading spaces should be preserved.
			text = "  3 567";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "  3 567" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "  3  678  1";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "  3  678  1" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			text = "1  456";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "1  456" + Environment.NewLine;
			Assert.Equal (expected, breakLines);

			// Double space Complex example
			text = "A  sentence      has words.  ";
			breakLines = "";
			wrappedLines = TextFormatter.WordWrapText (text, width, preserveTrailingSpaces: false);
			foreach (var line in wrappedLines) {
				breakLines += $"{line}{Environment.NewLine}";
			}
			expected = "A  sentence      has words.  " + Environment.NewLine;
			Assert.Equal (expected, breakLines);
		}

		[Fact]
		public void WordWrap_PreserveTrailingSpaces_True_With_Tab ()
		{
			var text = string.Empty;
			int maxWidth = 1;
			int expectedClippedWidth = 1;

			List<string> wrappedLines;

			text = "A sentence\t\t\t has words.";
			var tabWidth = 4;
			maxWidth = 14;
			expectedClippedWidth = 11;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true, tabWidth);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.Equal ("A sentence\t", wrappedLines [0]);
			Assert.Equal ("\t\t has ", wrappedLines [1]);
			Assert.Equal ("words.", wrappedLines [2]);
			Assert.True (wrappedLines.Count == 3);

			maxWidth = 8;
			expectedClippedWidth = 8;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true, tabWidth);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.Equal ("A ", wrappedLines [0]);
			Assert.Equal ("sentence", wrappedLines [1]);
			Assert.Equal ("\t\t", wrappedLines [2]);
			Assert.Equal ("\t ", wrappedLines [3]);
			Assert.Equal ("has ", wrappedLines [4]);
			Assert.Equal ("words.", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 6);

			maxWidth = 3;
			expectedClippedWidth = 3;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true, tabWidth);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.Equal ("A ", wrappedLines [0]);
			Assert.Equal ("sen", wrappedLines [1]);
			Assert.Equal ("ten", wrappedLines [2]);
			Assert.Equal ("ce", wrappedLines [3]);
			Assert.Equal ("\t", wrappedLines [4]);
			Assert.Equal ("\t", wrappedLines [5]);
			Assert.Equal ("\t", wrappedLines [6]);
			Assert.Equal (" ", wrappedLines [7]);
			Assert.Equal ("has", wrappedLines [8]);
			Assert.Equal (" ", wrappedLines [9]);
			Assert.Equal ("wor", wrappedLines [10]);
			Assert.Equal ("ds.", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 12);

			maxWidth = 2;
			expectedClippedWidth = 2;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true, tabWidth);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.Equal ("A ", wrappedLines [0]);
			Assert.Equal ("se", wrappedLines [1]);
			Assert.Equal ("nt", wrappedLines [2]);
			Assert.Equal ("en", wrappedLines [3]);
			Assert.Equal ("ce", wrappedLines [4]);
			Assert.Equal ("\t", wrappedLines [5]);
			Assert.Equal ("\t", wrappedLines [6]);
			Assert.Equal ("\t", wrappedLines [7]);
			Assert.Equal (" ", wrappedLines [8]);
			Assert.Equal ("ha", wrappedLines [9]);
			Assert.Equal ("s ", wrappedLines [10]);
			Assert.Equal ("wo", wrappedLines [11]);
			Assert.Equal ("rd", wrappedLines [12]);
			Assert.Equal ("s.", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == 14);

			maxWidth = 1;
			expectedClippedWidth = 1;
			wrappedLines = TextFormatter.WordWrapText (text, maxWidth, true, tabWidth);
			Assert.True (expectedClippedWidth >= wrappedLines.Max (l => l.RuneCount ()));
			Assert.Equal ("A", wrappedLines [0]);
			Assert.Equal (" ", wrappedLines [1]);
			Assert.Equal ("s", wrappedLines [2]);
			Assert.Equal ("e", wrappedLines [3]);
			Assert.Equal ("n", wrappedLines [4]);
			Assert.Equal ("t", wrappedLines [5]);
			Assert.Equal ("e", wrappedLines [6]);
			Assert.Equal ("n", wrappedLines [7]);
			Assert.Equal ("c", wrappedLines [8]);
			Assert.Equal ("e", wrappedLines [9]);
			Assert.Equal ("\t", wrappedLines [10]);
			Assert.Equal ("\t", wrappedLines [11]);
			Assert.Equal ("\t", wrappedLines [12]);
			Assert.Equal (" ", wrappedLines [13]);
			Assert.Equal (".", wrappedLines [^1]);
			Assert.True (wrappedLines.Count == text.Length);
		}

		[Fact]
		public void WordWrap_Unicode_Wide_Runes ()
		{
			string text = "これが最初の行です。 こんにちは世界。 これが2行目です。";
			var width = text.RuneCount ();
			var wrappedLines = TextFormatter.WordWrapText (text, width);
			Assert.Equal (3, wrappedLines.Count);
			Assert.Equal ("これが最初の行です。", wrappedLines [0]);
			Assert.Equal ("こんにちは世界。", wrappedLines [1]);
			Assert.Equal ("これが2行目です。", wrappedLines [^1]);
		}

		[Fact]
		public void ReplaceHotKeyWithTag ()
		{
			var tf = new TextFormatter ();
			string text = "test";
			int hotPos = 0;
			uint tag = 't';

			Assert.Equal (StringExtensions.Make (new Rune [] { (Rune)tag, (Rune)'e', (Rune)'s', (Rune)'t' }), tf.ReplaceHotKeyWithTag (text, hotPos));

			tag = 'e';
			hotPos = 1;
			Assert.Equal (StringExtensions.Make (new Rune [] { (Rune)'t', (Rune)tag, (Rune)'s', (Rune)'t' }), tf.ReplaceHotKeyWithTag (text, hotPos));

			var result = tf.ReplaceHotKeyWithTag (text, hotPos);
			Assert.Equal ((Rune)'e', result.ToRunes () [1]);

			text = "Ok";
			tag = 'O';
			hotPos = 0;
			Assert.Equal (StringExtensions.Make (new Rune [] { (Rune)tag, (Rune)'k' }), result = tf.ReplaceHotKeyWithTag (text, hotPos));
			Assert.Equal ((Rune)'O', result.ToRunes () [0]);

			text = "[◦ Ok ◦]";
			text = StringExtensions.Make (new Rune [] { (Rune)'[', (Rune)'◦', (Rune)' ', (Rune)'O', (Rune)'k', (Rune)' ', (Rune)'◦', (Rune)']' });
			var runes = text.ToRuneList ();
			Assert.Equal (text.RuneCount (), runes.Count);
			Assert.Equal (text, StringExtensions.Make (runes));
			tag = 'O';
			hotPos = 3;
			Assert.Equal (StringExtensions.Make (new Rune [] { (Rune)'[', (Rune)'◦', (Rune)' ', (Rune)tag, (Rune)'k', (Rune)' ', (Rune)'◦', (Rune)']' }), result = tf.ReplaceHotKeyWithTag (text, hotPos));
			Assert.Equal ((Rune)'O', result.ToRunes () [3]);

			text = "^k";
			tag = '^';
			hotPos = 0;
			Assert.Equal (StringExtensions.Make (new Rune [] { (Rune)tag, (Rune)'k' }), result = tf.ReplaceHotKeyWithTag (text, hotPos));
			Assert.Equal ((Rune)'^', result.ToRunes () [0]);
		}

		[Fact]
		public void Reformat_Invalid ()
		{
			var text = string.Empty;
			var list = new List<string> ();

			Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.Format (text, -1, TextAlignment.Left, false));

			list = TextFormatter.Format (text, 0, TextAlignment.Left, false);
			Assert.NotEmpty (list);
			Assert.True (list.Count == 1);
			Assert.Equal (string.Empty, list [0]);

			text = null;
			list = TextFormatter.Format (text, 0, TextAlignment.Left, false);
			Assert.NotEmpty (list);
			Assert.True (list.Count == 1);
			Assert.Equal (string.Empty, list [0]);

			list = TextFormatter.Format (text, 0, TextAlignment.Left, true);
			Assert.NotEmpty (list);
			Assert.True (list.Count == 1);
			Assert.Equal (string.Empty, list [0]);
		}

		[Fact]
		public void Reformat_NoWordrap_SingleLine ()
		{
			var text = string.Empty;
			var list = new List<string> ();
			var maxWidth = 0;
			var expectedClippedWidth = 0;
			var wrap = false;

			maxWidth = 0;
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);

			maxWidth = 1;
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);

			text = "A sentence has words.";
			maxWidth = 0;
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (string.Empty, list [0]);

			maxWidth = 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);

			maxWidth = 5;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);

			// no clip
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);

			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);
		}

		[Fact]
		public void Reformat_NoWordrap_NewLines ()
		{
			var text = string.Empty;
			var list = new List<string> ();
			var maxWidth = 0;
			var expectedClippedWidth = 0;
			var wrap = false;

			text = "A sentence has words.\nLine 2.";
			maxWidth = 0;
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (string.Empty, list [0]);

			maxWidth = 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);

			maxWidth = 5;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]).Replace ("\n", " "), list [0]);

			// no clip
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]).Replace ("\n", " "), list [0]);

			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]).Replace ("\n", " "), list [0]);

			text = "A sentence has words.\r\nLine 2.";
			maxWidth = 0;
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (string.Empty, list [0]);

			maxWidth = 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);

			maxWidth = 5;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth) + 1;
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]).Replace ("\r\n", " "), list [0]);

			// no clip
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]).Replace ("\r\n", " "), list [0]);

			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]).Replace ("\r\n", " "), list [0]);
		}

		[Fact]
		public void Reformat_Wrap_Spaces_No_NewLines ()
		{
			var text = string.Empty;
			var list = new List<string> ();
			var maxWidth = 0;
			var expectedClippedWidth = 0;
			var wrap = true;
			var preserveTrailingSpaces = true;

			// Even # of spaces
			//      0123456789
			text = "012 456 89";

			// See WordWrap BUGBUGs above.
			maxWidth = 0;
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal (string.Empty, list [0]);

			maxWidth = 1;
			// remove 3 whitespace chars
			expectedClippedWidth = text.RuneCount () - text.Sum (r => r == ' ' ? 1 : 0);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.Equal (expectedClippedWidth, list.Count);
			Assert.Equal ("01245689", string.Join ("", list.ToArray ()));

			maxWidth = 1;
			// keep 3 whitespace chars
			expectedClippedWidth = text.RuneCount ();
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap, preserveTrailingSpaces);
			Assert.Equal (expectedClippedWidth, list.Count);
			Assert.Equal (text, string.Join ("", list.ToArray ()));

			maxWidth = 5;
			// remove 3 whitespace chars
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth - text.Sum (r => r == ' ' ? 1 : 0));
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.Equal (expectedClippedWidth, list.Count);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);
			Assert.Equal ("01245689", string.Join ("", list.ToArray ()));

			maxWidth = 5;
			// keep 3 whitespace chars
			expectedClippedWidth = Math.Min (text.RuneCount (), (int)Math.Ceiling ((double)(text.RuneCount () / 3F)));
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap, preserveTrailingSpaces);
			Assert.Equal (expectedClippedWidth - (maxWidth - expectedClippedWidth), list.Count);
			Assert.Equal (StringExtensions.Make (text.ToRunes () [0..expectedClippedWidth]), list [0]);
			Assert.Equal ("012 456 89", string.Join ("", list.ToArray ()));

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 2);
			Assert.Equal ("012 456", list [0]);
			Assert.Equal ("89", list [1]);

			// no clip
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal ("012 456 89", list [0]);

			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal ("012 456 89", list [0]);

			// Odd # of spaces
			//      0123456789
			text = "012 456 89 end";
			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 2);
			Assert.Equal ("012 456 89", list [0]);
			Assert.Equal ("end", list [1]);

			// no clip
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal ("012 456 89 end", list [0]);

			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal ("012 456 89 end", list [0]);
		}

		[Fact]
		public void Reformat_Unicode_Wrap_Spaces_No_NewLines ()
		{
			var text = string.Empty;
			var list = new List<string> ();
			var maxWidth = 0;
			var expectedClippedWidth = 0;
			var wrap = true;

			// Unicode
			// Even # of chars
			//      0123456789
			text = "\u2660Ð¿ÑÐ Ð²Ð Ñ";

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 2);
			Assert.Equal ("\u2660Ð¿ÑÐ Ð²Ð", list [0]);
			Assert.Equal ("Ñ", list [1]);

			// no clip
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal ("\u2660Ð¿ÑÐ Ð²Ð Ñ", list [0]);

			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal ("\u2660Ð¿ÑÐ Ð²Ð Ñ", list [0]);

			// Unicode
			// Odd # of chars
			//      0123456789
			text = "\u2660 ÑÐ Ð²Ð Ñ";

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 2);
			Assert.Equal ("\u2660 ÑÐ Ð²Ð", list [0]);
			Assert.Equal ("Ñ", list [1]);

			// no clip
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal ("\u2660 ÑÐ Ð²Ð Ñ", list [0]);

			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.True (list.Count == 1);
			Assert.Equal ("\u2660 ÑÐ Ð²Ð Ñ", list [0]);
		}

		[Fact]
		public void Reformat_Unicode_Wrap_Spaces_NewLines ()
		{
			var text = string.Empty;
			var list = new List<string> ();
			var maxWidth = 0;
			var expectedClippedWidth = 0;
			var wrap = true;

			// Unicode
			text = "\u2460\u2461\u2462\n\u2460\u2461\u2462\u2463\u2464";

			maxWidth = text.RuneCount () - 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.Equal (2, list.Count);
			Assert.Equal ("\u2460\u2461\u2462", list [0]);
			Assert.Equal ("\u2460\u2461\u2462\u2463\u2464", list [1]);

			// no clip
			maxWidth = text.RuneCount () + 0;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.Equal (2, list.Count);
			Assert.Equal ("\u2460\u2461\u2462", list [0]);
			Assert.Equal ("\u2460\u2461\u2462\u2463\u2464", list [1]);

			maxWidth = text.RuneCount () + 1;
			expectedClippedWidth = Math.Min (text.RuneCount (), maxWidth);
			list = TextFormatter.Format (text, maxWidth, TextAlignment.Left, wrap);
			Assert.Equal (2, list.Count);
			Assert.Equal ("\u2460\u2461\u2462", list [0]);
			Assert.Equal ("\u2460\u2461\u2462\u2463\u2464", list [1]);
		}

		[Fact]
		public void Format_WordWrap_PreserveTrailingSpaces ()
		{
			string text = " A sentence has words. \n This is the second Line - 2. ";

			// With preserveTrailingSpaces = false by default.
			var list1 = TextFormatter.Format (text, 4, TextAlignment.Left, true);
			string wrappedText1 = string.Empty;
			Assert.Equal (" A", list1 [0]);
			Assert.Equal ("sent", list1 [1]);
			Assert.Equal ("ence", list1 [2]);
			Assert.Equal ("has", list1 [3]);
			Assert.Equal ("word", list1 [4]);
			Assert.Equal ("s. ", list1 [5]);
			Assert.Equal (" Thi", list1 [6]);
			Assert.Equal ("s is", list1 [7]);
			Assert.Equal ("the", list1 [8]);
			Assert.Equal ("seco", list1 [9]);
			Assert.Equal ("nd", list1 [10]);
			Assert.Equal ("Line", list1 [11]);
			Assert.Equal ("- 2.", list1 [^1]);
			foreach (var txt in list1) wrappedText1 += txt;
			Assert.Equal (" Asentencehaswords.  This isthesecondLine- 2.", wrappedText1);

			// With preserveTrailingSpaces = true.
			var list2 = TextFormatter.Format (text, 4, TextAlignment.Left, true, true);
			string wrappedText2 = string.Empty;
			Assert.Equal (" A ", list2 [0]);
			Assert.Equal ("sent", list2 [1]);
			Assert.Equal ("ence", list2 [2]);
			Assert.Equal (" ", list2 [3]);
			Assert.Equal ("has ", list2 [4]);
			Assert.Equal ("word", list2 [5]);
			Assert.Equal ("s. ", list2 [6]);
			Assert.Equal (" ", list2 [7]);
			Assert.Equal ("This", list2 [8]);
			Assert.Equal (" is ", list2 [9]);
			Assert.Equal ("the ", list2 [10]);
			Assert.Equal ("seco", list2 [11]);
			Assert.Equal ("nd ", list2 [12]);
			Assert.Equal ("Line", list2 [13]);
			Assert.Equal (" - ", list2 [14]);
			Assert.Equal ("2. ", list2 [^1]);
			foreach (var txt in list2) wrappedText2 += txt;
			Assert.Equal (" A sentence has words.  This is the second Line - 2. ", wrappedText2);
		}

		[Fact]
		public void Format_Dont_Throw_ArgumentException_With_WordWrap_As_False_And_Keep_End_Spaces_As_True ()
		{
			var exception = Record.Exception (() => TextFormatter.Format ("Some text", 4, TextAlignment.Left, false, true));
			Assert.Null (exception);
		}


		[Fact]
		public void Format_Justified_Always_Returns_Text_Width_Equal_To_Passed_Width_Horizontal ()
		{
			string text = "Hello world, how are you today? Pretty neat!";

			Assert.Equal (44, text.RuneCount ());

			for (int i = 44; i < 80; i++) {
				var fmtText = TextFormatter.Format (text, i, TextAlignment.Justified, false, true) [0];
				Assert.Equal (i, fmtText.RuneCount ());
				var c = (char)fmtText [^1];
				Assert.Equal ('!', c);
			}
		}

		[Fact]
		public void Format_Justified_Always_Returns_Text_Width_Equal_To_Passed_Width_Vertical ()
		{
			string text = "Hello world, how are you today? Pretty neat!";

			Assert.Equal (44, text.RuneCount ());

			for (int i = 44; i < 80; i++) {
				var fmtText = TextFormatter.Format (text, i, TextAlignment.Justified, false, true, 0, TextDirection.TopBottom_LeftRight) [0];
				Assert.Equal (i, fmtText.RuneCount ());
				var c = (char)fmtText [^1];
				Assert.Equal ('!', c);
			}
		}

		[Fact]
		public void TestClipOrPad_ShortWord ()
		{
			// word is short but we want it to fill 6 so it should be padded
			Assert.Equal ("fff   ", TextFormatter.ClipOrPad ("fff", 6));
		}

		[Fact]
		public void TestClipOrPad_LongWord ()
		{
			// word is long but we want it to fill 3 space only
			Assert.Equal ("123", TextFormatter.ClipOrPad ("123456789", 3));
		}

		[Fact]
		public void Internal_Tests ()
		{
			var tf = new TextFormatter ();
			Assert.Equal (Key.Null, tf.HotKey);
			tf.HotKey = Key.CtrlMask | Key.Q;
			Assert.Equal (Key.CtrlMask | Key.Q, tf.HotKey);
		}

		[Fact]
		public void GetTextWidth_Simple_And_Wide_Runes ()
		{
			string text = "Hello World";
			Assert.Equal (11, TextFormatter.GetTextWidth (text));
			text = "こんにちは世界";
			Assert.Equal (14, TextFormatter.GetTextWidth (text));
		}

		[Fact]
		public void GetSumMaxCharWidth_Simple_And_Wide_Runes ()
		{
			string text = "Hello World";
			Assert.Equal (11, TextFormatter.GetSumMaxCharWidth (text));
			Assert.Equal (1, TextFormatter.GetSumMaxCharWidth (text, 6, 1));
			text = "こんにちは 世界";
			Assert.Equal (15, TextFormatter.GetSumMaxCharWidth (text));
			Assert.Equal (2, TextFormatter.GetSumMaxCharWidth (text, 6, 1));
		}

		[Fact]
		public void GetSumMaxCharWidth_List_Simple_And_Wide_Runes ()
		{
			var text = new List<string> () { "Hello", "World" };
			Assert.Equal (2, TextFormatter.GetSumMaxCharWidth (text));
			Assert.Equal (1, TextFormatter.GetSumMaxCharWidth (text, 1, 1));
			text = new List<string> () { "こんにちは", "世界" };
			Assert.Equal (4, TextFormatter.GetSumMaxCharWidth (text));
			Assert.Equal (2, TextFormatter.GetSumMaxCharWidth (text, 1, 1));
		}

		[Fact]
		public void GetLengthThatFits_runelist ()
		{
			int columns = 3;
			string text = "test";
			var runes = text.ToRuneList ();

			Assert.Equal (3, TextFormatter.GetLengthThatFits (runes, columns));

			columns = 4;
			Assert.Equal (4, TextFormatter.GetLengthThatFits (runes, columns));

			columns = 10;
			Assert.Equal (4, TextFormatter.GetLengthThatFits (runes, columns));
		}

		[Fact]
		public void GetLengthThatFits_ustring ()
		{
			int columns = 3;
			string text = "test";

			Assert.Equal (3, TextFormatter.GetLengthThatFits (text, columns));

			columns = 4;
			Assert.Equal (4, TextFormatter.GetLengthThatFits (text, columns));

			columns = 10;
			Assert.Equal (4, TextFormatter.GetLengthThatFits (text, columns));

			columns = 1;
			Assert.Equal (1, TextFormatter.GetLengthThatFits (text, columns));

			columns = 0;
			Assert.Equal (0, TextFormatter.GetLengthThatFits (text, columns));

			columns = -1;
			Assert.Equal (0, TextFormatter.GetLengthThatFits (text, columns));

			text = null;
			Assert.Equal (0, TextFormatter.GetLengthThatFits (text, columns));

			text = string.Empty;
			Assert.Equal (0, TextFormatter.GetLengthThatFits (text, columns));
		}

		[Fact]
		public void GetLengthThatFits_Simple_And_Wide_Runes ()
		{
			string text = "Hello World";
			Assert.Equal (6, TextFormatter.GetLengthThatFits (text, 6));
			text = "こんにちは 世界";
			Assert.Equal (3, TextFormatter.GetLengthThatFits (text, 6));
		}

		[Fact]
		public void GetLengthThatFits_List_Simple_And_Wide_Runes ()
		{
			var runes = "Hello World".ToRuneList ();
			Assert.Equal (6, TextFormatter.GetLengthThatFits (runes, 6));
			runes = "こんにちは 世界".ToRuneList ();
			Assert.Equal (3, TextFormatter.GetLengthThatFits (runes, 6));
			runes = $"{CM.Glyphs.LeftBracket} Say Hello 你 {CM.Glyphs.RightBracket}".ToRuneList ();
			Assert.Equal (15, TextFormatter.GetLengthThatFits (runes, 16));
		}

		[Fact]
		public void Format_Truncate_Simple_And_Wide_Runes ()
		{
			var text = "Truncate";
			var list = TextFormatter.Format (text, 3, false, false);
			Assert.Equal ("Tru", list [^1]);

			text = "デモエムポンズ";
			list = TextFormatter.Format (text, 3, false, false);
			Assert.Equal ("デ", list [^1]);
		}


		[Fact]
		public void Format_With_PreserveTrailingSpaces_And_Without_PreserveTrailingSpaces ()
		{
			var text = $"Line1{Environment.NewLine}Line2{Environment.NewLine}Line3{Environment.NewLine}";
			var width = 60;
			var preserveTrailingSpaces = false;
			var formated = TextFormatter.Format (text, width, false, true, preserveTrailingSpaces);
			Assert.Equal ("Line1", formated [0]);
			Assert.Equal ("Line2", formated [1]);
			Assert.Equal ("Line3", formated [^1]);

			preserveTrailingSpaces = true;
			formated = TextFormatter.Format (text, width, false, true, preserveTrailingSpaces);
			Assert.Equal ("Line1", formated [0]);
			Assert.Equal ("Line2", formated [1]);
			Assert.Equal ("Line3", formated [^1]);
		}

		[Fact]
		public void SplitNewLine_Ending_Without_NewLine_Probably_CRLF ()
		{
			var text = $"First Line 界{Environment.NewLine}Second Line 界{Environment.NewLine}Third Line 界";
			var splited = TextFormatter.SplitNewLine (text);
			Assert.Equal ("First Line 界", splited [0]);
			Assert.Equal ("Second Line 界", splited [1]);
			Assert.Equal ("Third Line 界", splited [^1]);
		}

		[Fact]
		public void SplitNewLine_Ending_With_NewLine_Probably_CRLF ()
		{
			var text = $"First Line 界{Environment.NewLine}Second Line 界{Environment.NewLine}Third Line 界{Environment.NewLine}";
			var splited = TextFormatter.SplitNewLine (text);
			Assert.Equal ("First Line 界", splited [0]);
			Assert.Equal ("Second Line 界", splited [1]);
			Assert.Equal ("Third Line 界", splited [2]);
			Assert.Equal ("", splited [^1]);
		}

		[Fact]
		public void SplitNewLine_Ending_Without_NewLine_Only_LF ()
		{
			var text = $"First Line 界\nSecond Line 界\nThird Line 界";
			var splited = TextFormatter.SplitNewLine (text);
			Assert.Equal ("First Line 界", splited [0]);
			Assert.Equal ("Second Line 界", splited [1]);
			Assert.Equal ("Third Line 界", splited [^1]);
		}

		[Fact]
		public void SplitNewLine_Ending_With_NewLine_Only_LF ()
		{
			var text = $"First Line 界\nSecond Line 界\nThird Line 界\n";
			var splited = TextFormatter.SplitNewLine (text);
			Assert.Equal ("First Line 界", splited [0]);
			Assert.Equal ("Second Line 界", splited [1]);
			Assert.Equal ("Third Line 界", splited [2]);
			Assert.Equal ("", splited [^1]);
		}

		[Fact]
		public void MaxWidthLine_With_And_Without_Newlines ()
		{
			var text = "Single Line 界";
			Assert.Equal (14, TextFormatter.MaxWidthLine (text));

			text = $"First Line 界\nSecond Line 界\nThird Line 界\n";
			Assert.Equal (14, TextFormatter.MaxWidthLine (text));
		}

		[Fact]
		public void String_Array_Is_Not_Always_Equal_ToRunes_Array ()
		{
			string s = "New Test 你";
			// Rune array length is equal to string array
			var usToRunes = s.ToRunes ();
			Assert.Equal (10, usToRunes.Length);
			Assert.Equal (10, s.Length);
			Assert.Equal (20320, usToRunes [9].Value);
			Assert.Equal (20320, s [9]);
			Assert.Equal ("你", usToRunes [9].ToString ());
			Assert.Equal ("你", s [9].ToString ());

			s = "New Test \U0001d539";
			// Rune array length isn't equal to string array
			usToRunes = s.ToRunes ();
			Assert.Equal (10, usToRunes.Length);
			Assert.Equal (11, s.Length);
			Assert.Equal (120121, usToRunes [9].Value);
			Assert.Equal (55349, s [9]);
			Assert.Equal ("𝔹", usToRunes [9].ToString ());
			Assert.Equal ("𝔹", new string (new char [] { s [9], s [10] }));
		}
	}
}