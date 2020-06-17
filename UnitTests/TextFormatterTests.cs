using NStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Terminal.Gui;
using Xunit;

// Alais Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui {
	public class TextFormatterTests {
		[Fact]
		public void FindHotKey_Invalid_ReturnsFalse ()
		{
			var text = ustring.Empty;
			Rune hotKeySpecifier = '_';
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
			var text = ustring.Empty;
			Rune hotKeySpecifier = '_';
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
			var text = ustring.Empty;
			Rune hotKeySpecifier = '_';
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
			var text = ustring.Empty;
			Rune hotKeySpecifier = '_';
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

			var text = ustring.Empty;
			Rune hotKeySpecifier = (Rune)0;
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

			var text = ustring.Empty;
			Rune hotKeySpecifier = (Rune)0;
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

		static ustring testHotKeyAtStart = "_K Before";
		static ustring testHotKeyAtSecondPos = "a_K Second";
		static ustring testHotKeyAtLastPos = "Last _K";
		static ustring testHotKeyAfterLastChar = "After K_";
		static ustring testMultiHotKeys = "Multiple _K and _R";
		static ustring testNonEnglish = "Non-english: _Кдать";

		[Fact]
		public void RemoveHotKeySpecifier_InValid_ReturnsOriginal ()
		{
			Rune hotKeySpecifier = '_';

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
			Rune hotKeySpecifier = '_';

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
			Rune hotKeySpecifier = '_';

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
			Assert.Equal (Rect.Empty, TextFormatter.CalcRect (1, 2, ""));
			Assert.Equal (Rect.Empty, TextFormatter.CalcRect (-1, -2, ""));
		}

		[Fact]
		public void CalcRect_SingleLine_Returns_1High ()
		{
			var text = ustring.Empty;

			text = "test";
			Assert.Equal (new Rect (0, 0, text.RuneCount, 1), TextFormatter.CalcRect (0, 0, text));
		}

		[Fact]
		public void CalcRect_MultiLine_Returns_nHigh ()
		{
			var text = ustring.Empty;
			var lines = 0;

			text = "line1\nline2";
			lines = 2;
			Assert.Equal (new Rect (0, 0, 5, lines), TextFormatter.CalcRect (0, 0, text));

			text = "line1\nline2\nline3long!";
			lines = 3;
			Assert.Equal (new Rect (0, 0, 10, lines), TextFormatter.CalcRect (0, 0, text));

			text = "line1\nline2\n\n";
			lines = 4;
			Assert.Equal (new Rect (0, 0, 5, lines), TextFormatter.CalcRect (0, 0, text));

			text = "line1\r\nline2";
			lines = 2;
			Assert.Equal (new Rect (0, 0, 5, lines), TextFormatter.CalcRect (0, 0, text));
		}

		[Fact]
		public void ClipAndJustify_Invalid_Returns_Original ()
		{
			var text = ustring.Empty;

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

			var text = ustring.Empty;
			var justifiedText = ustring.Empty;
			int width = 0;

			text = "test";
			width = 0;
			Assert.Equal (ustring.Empty, justifiedText = TextFormatter.ClipAndJustify (text, width, align));

			text = "test";
			width = 2;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));

			text = "test";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A\tsentence\thas\twords.";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A\tsentence\thas\twords.";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "line1\nline2\nline3long!";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "line1\nline2\nline3long!";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = " ~  s  gui.cs   master ↑10";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);
		}

		[Fact]
		public void ClipAndJustify_Valid_Right ()
		{
			var align = TextAlignment.Right;

			var text = ustring.Empty;
			var justifiedText = ustring.Empty;
			int width = 0;

			text = "test";
			width = 0;
			Assert.Equal (ustring.Empty, justifiedText = TextFormatter.ClipAndJustify (text, width, align));

			text = "test";
			width = 2;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));

			text = "test";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A\tsentence\thas\twords.";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A\tsentence\thas\twords.";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "line1\nline2\nline3long!";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "line1\nline2\nline3long!";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = " ~  s  gui.cs   master ↑10";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);
		}

		[Fact]
		public void ClipAndJustify_Valid_Centered ()
		{
			var align = TextAlignment.Centered;

			var text = ustring.Empty;
			var justifiedText = ustring.Empty;
			int width = 0;

			text = "test";
			width = 0;
			Assert.Equal (ustring.Empty, justifiedText = TextFormatter.ClipAndJustify (text, width, align));

			text = "test";
			width = 2;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));

			text = "test";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A\tsentence\thas\twords.";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "A\tsentence\thas\twords.";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "line1\nline2\nline3long!";
			width = int.MaxValue;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "line1\nline2\nline3long!";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = " ~  s  gui.cs   master ↑10";
			width = 10;
			Assert.Equal (text [0, width], justifiedText = TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (justifiedText.RuneCount <= width);

			text = "";
			width = text.RuneCount;
			Assert.Equal (text, justifiedText = TextFormatter.ClipAndJustify (text, width, align)); ;
			Assert.True (justifiedText.RuneCount <= width);
		}


		[Fact]
		public void ClipAndJustify_Valid_Justified ()
		{
			var text = ustring.Empty;
			int width = 0;
			var align = TextAlignment.Justified;

			text = "test";
			width = 0;
			Assert.Equal (ustring.Empty, TextFormatter.ClipAndJustify (text, width, align));

			text = "test";
			width = 2;
			Assert.Equal (text [0, width], TextFormatter.ClipAndJustify (text, width, align));

			text = "test";
			width = int.MaxValue;
			Assert.Equal (text, TextFormatter.ClipAndJustify (text, width, align));
			Assert.True (text.RuneCount <= width);

			// see Justify_ tests below

		}

		[Fact]
		public void Justify_Invalid ()
		{
			var text = ustring.Empty;
			Assert.Equal (text, TextFormatter.Justify (text, 0));

			text = null;
			Assert.Equal (text, TextFormatter.Justify (text, 0));

			text = "test";
			Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.Justify (text, -1));
		}

		[Fact]
		public void Justify_SingleWord ()
		{
			var text = ustring.Empty;
			var justifiedText = ustring.Empty;
			int width = 0;
			char fillChar = '+';

			text = "word";
			justifiedText = "word";
			width = text.RuneCount;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());

			text = "word";
			justifiedText = "word";
			width = text.RuneCount + 1;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());

			text = "word";
			justifiedText = "word";
			width = text.RuneCount + 2;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());

			text = "word";
			justifiedText = "word";
			width = text.RuneCount + 10;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());

			text = "word";
			justifiedText = "word";
			width = text.RuneCount + 11;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
		}

		[Fact]
		public void Justify_Sentence ()
		{
			var text = ustring.Empty;
			var justifiedText = ustring.Empty;
			int width = 0;
			char fillChar = '+';

			text = "A sentence has words.";
			justifiedText = "A+sentence+has+words.";
			width = text.RuneCount;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			justifiedText = "A+sentence+has+words.";
			width = text.RuneCount + 1;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			justifiedText = "A+sentence+has+words.";
			width = text.RuneCount + 2;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			justifiedText = "A++sentence++has++words.";
			width = text.RuneCount + 3;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			justifiedText = "A++sentence++has++words.";
			width = text.RuneCount + 4;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			justifiedText = "A++sentence++has++words.";
			width = text.RuneCount + 5;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			justifiedText = "A+++sentence+++has+++words.";
			width = text.RuneCount + 6;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			justifiedText = "A+++++++sentence+++++++has+++++++words.";
			width = text.RuneCount + 20;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
			Assert.True (justifiedText.RuneCount <= width);

			text = "A sentence has words.";
			justifiedText = "A++++++++sentence++++++++has++++++++words.";
			width = text.RuneCount + 23;
			Assert.Equal (justifiedText.ToString (), TextFormatter.Justify (text, width, fillChar).ToString ());
			Assert.True (justifiedText.RuneCount <= width);

			//TODO: Unicode
		}

		[Fact]
		public void WordWrap_Invalid ()
		{
			var text = ustring.Empty;
			int width = 0;

			Assert.Empty (TextFormatter.WordWrap (null, width));
			Assert.Empty (TextFormatter.WordWrap (text, width));
			Assert.Throws<ArgumentOutOfRangeException> (() => TextFormatter.WordWrap (text, -1));
		}

		[Fact]
		public void WordWrap_SingleWordLine ()
		{
			var text = ustring.Empty;
			int width = 0;
			List<ustring> wrappedLines;

			text = "Constantinople";
			width = text.RuneCount;
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.True (wrappedLines.Count == 1);

			width = text.RuneCount - 1;
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal (text [0, text.RuneCount - 1].ToString (), wrappedLines [0].ToString ());
			Assert.Equal ("e", wrappedLines [1].ToString ());

			width = text.RuneCount - 2;
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal (text [0, text.RuneCount - 2].ToString (), wrappedLines [0].ToString ());

			width = text.RuneCount - 5;
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (2, wrappedLines.Count);

			width = (int)Math.Ceiling ((double)(text.RuneCount / 2F));
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal ("Constan", wrappedLines [0].ToString ());
			Assert.Equal ("tinople", wrappedLines [1].ToString ());

			width = (int)Math.Ceiling ((double)(text.RuneCount / 3F));
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (3, wrappedLines.Count);
			Assert.Equal ("Const", wrappedLines [0].ToString ());
			Assert.Equal ("antin", wrappedLines [1].ToString ());
			Assert.Equal ("ople", wrappedLines [2].ToString ());

			width = (int)Math.Ceiling ((double)(text.RuneCount / 4F));
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (4, wrappedLines.Count);

			width = (int)Math.Ceiling ((double)text.RuneCount / text.RuneCount);
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (text.RuneCount, wrappedLines.Count);
			Assert.Equal ("C", wrappedLines [0].ToString ());
			Assert.Equal ("o", wrappedLines [1].ToString ());
			Assert.Equal ("n", wrappedLines [2].ToString ());
			Assert.Equal ("s", wrappedLines [3].ToString ());
			Assert.Equal ("t", wrappedLines [4].ToString ());
			Assert.Equal ("a", wrappedLines [5].ToString ());
			Assert.Equal ("n", wrappedLines [6].ToString ());
			Assert.Equal ("t", wrappedLines [7].ToString ());
			Assert.Equal ("i", wrappedLines [8].ToString ());
			Assert.Equal ("n", wrappedLines [9].ToString ());
			Assert.Equal ("o", wrappedLines [10].ToString ());
			Assert.Equal ("p", wrappedLines [11].ToString ());
			Assert.Equal ("l", wrappedLines [12].ToString ());
			Assert.Equal ("e", wrappedLines [13].ToString ());
		}

		[Fact]
		public void WordWrap_NoNewLines ()
		{
			var text = ustring.Empty;
			int width = 0;
			List<ustring> wrappedLines;

			text = "A sentence has words.";
			width = text.RuneCount;
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.True (wrappedLines.Count == 1);

			width = text.RuneCount - 1;
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal ("A sentence has", wrappedLines [0].ToString ());
			Assert.Equal ("words.", wrappedLines [1].ToString ());

			width = text.RuneCount - "words.".Length;
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal ("A sentence has", wrappedLines [0].ToString ());
			Assert.Equal ("words.", wrappedLines [1].ToString ());

			width = text.RuneCount - " words.".Length;
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal ("A sentence has", wrappedLines [0].ToString ());
			Assert.Equal ("words.", wrappedLines [1].ToString ());

			width = text.RuneCount - "s words.".Length;
			wrappedLines = TextFormatter.WordWrap (text, width);
			Assert.Equal (2, wrappedLines.Count);
			Assert.Equal ("A sentence", wrappedLines [0].ToString ());
			Assert.Equal ("has words.", wrappedLines [1].ToString ());

			// Unicode 
			// TODO: Lots of bugs
			//text = "A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has words.";
			//width = text.RuneCount;
			//wrappedLines = TextFormatter.WordWrap (text, width);
			//Assert.True (wrappedLines.Count == 1);

			//width = text.RuneCount - 1;
			//wrappedLines = TextFormatter.WordWrap (text, width);
			//Assert.Equal (2, wrappedLines.Count);
			//Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", wrappedLines [0].ToString ());
			//Assert.Equal ("words.", wrappedLines [1].ToString ());

			//width = text.RuneCount - "words.".Length;
			//wrappedLines = TextFormatter.WordWrap (text, width);
			//Assert.Equal (2, wrappedLines.Count);
			//Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", wrappedLines [0].ToString ());
			//Assert.Equal ("words.", wrappedLines [1].ToString ());

			//width = text.RuneCount - " words.".Length;
			//wrappedLines = TextFormatter.WordWrap (text, width);
			//Assert.Equal (2, wrappedLines.Count);
			//Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ) has", wrappedLines [0].ToString ());
			//Assert.Equal ("words.", wrappedLines [1].ToString ());

			//width = text.RuneCount - "s words.".Length;
			//wrappedLines = TextFormatter.WordWrap (text, width);
			//Assert.Equal (2, wrappedLines.Count);
			//Assert.Equal ("A Unicode sentence (Ð¿ÑÐ¸Ð²ÐµÑ)", wrappedLines [0].ToString ());
			//Assert.Equal ("has words.", wrappedLines [1].ToString ());

			//width = text.RuneCount - "Ð²ÐµÑ) has words.".Length;
			//wrappedLines = TextFormatter.WordWrap (text, width);
			//Assert.Equal (2, wrappedLines.Count);
			//Assert.Equal ("A Unicode sentence", wrappedLines [0].ToString ());
			//Assert.Equal ("(Ð¿ÑÐ¸Ð²ÐµÑ) has words.", wrappedLines [1].ToString ());

		}

		[Fact]
		public void ReplaceHotKeyWithTag ()
		{
			ustring text = "test";
			int hotPos = 0;
			uint tag = 0x100000 | 't';

			Assert.Equal (ustring.Make (new Rune [] { tag, 'e', 's', 't' }), TextFormatter.ReplaceHotKeyWithTag (text, hotPos));

			tag = 0x100000 | 'e';
			hotPos = 1;
			Assert.Equal (ustring.Make (new Rune [] { 't', tag, 's', 't' }), TextFormatter.ReplaceHotKeyWithTag (text, hotPos));

			var result = TextFormatter.ReplaceHotKeyWithTag (text, hotPos);
			Assert.Equal ('e', (uint)(result.ToRunes () [1] & ~0x100000));

			text = "Ok";
			tag = 0x100000 | 'O';
			hotPos = 0;
			Assert.Equal (ustring.Make (new Rune [] { tag, 'k' }), result = TextFormatter.ReplaceHotKeyWithTag (text, hotPos));
			Assert.Equal ('O', (uint)(result.ToRunes () [0] & ~0x100000));

			text = "[◦ Ok ◦]";
			text = ustring.Make(new Rune [] { '[', '◦', ' ', 'O', 'k', ' ', '◦', ']' });
			var runes = text.ToRuneList ();
			Assert.Equal (text.RuneCount, runes.Count);
			Assert.Equal (text, ustring.Make(runes));
			tag = 0x100000 | 'O';
			hotPos = 3;
			Assert.Equal (ustring.Make (new Rune [] { '[', '◦', ' ', tag, 'k', ' ', '◦', ']' }), result = TextFormatter.ReplaceHotKeyWithTag (text, hotPos));
			Assert.Equal ('O', (uint)(result.ToRunes () [3] & ~0x100000));

			text = "^k";
			tag = '^';
			hotPos = 0;
			Assert.Equal (ustring.Make (new Rune [] { tag, 'k' }), result = TextFormatter.ReplaceHotKeyWithTag (text, hotPos));
			Assert.Equal ('^', (uint)(result.ToRunes () [0] & ~0x100000));

		}
	}
}