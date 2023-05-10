using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

namespace Terminal.Gui.TextTests {
	public class RuneTests {
		[Fact]
		public void TestColumnWidth ()
		{
			Rune a = (Rune)'a';
			Rune b = (Rune)'b';
			Rune c = (Rune)123;
			Rune d = (Rune)'\u1150';  // 0x1150	ᅐ	Unicode Technical Report #11
			Rune e = (Rune)'\u1161';  // 0x1161	ᅡ	Unicode Hangul Jamo for join with column width equal to 0 alone.
			Rune f = (Rune)31;    // non printable character
			Rune g = (Rune)127;   // non printable character
			string h = "\U0001fa01";
			string i = "\U000e0fe1";
			Rune j = (Rune)'\u20D0';
			Rune k = (Rune)'\u25a0';
			Rune l = (Rune)'\u25a1';
			Rune m = (Rune)'\uf61e';
			byte [] n = new byte [4] { 0xf0, 0x9f, 0x8d, 0x95 }; // UTF-8 Encoding
			Rune o = new Rune ('\ud83c', '\udf55'); // UTF-16 Encoding;
			string p = "\U0001F355"; // UTF-32 Encoding
			Rune q = (Rune)'\u2103';
			Rune r = (Rune)'\u1100';
			Rune s = (Rune)'\u2501';

			Assert.Equal (1, a.ColumnWidth ());
			Assert.Equal ("a", a.ToString ());
			Assert.Equal (1, a.ToString ().Length);
			Assert.Equal (1, a.Utf8SequenceLength);
			Assert.Equal (1, b.ColumnWidth ());
			Assert.Equal ("b", b.ToString ());
			Assert.Equal (1, b.ToString ().Length);
			Assert.Equal (1, b.Utf8SequenceLength);
			var rl = a < b;
			Assert.True (rl);
			Assert.Equal (1, c.ColumnWidth ());
			Assert.Equal ("{", c.ToString ());
			Assert.Equal (1, c.ToString ().Length);
			Assert.Equal (1, c.Utf8SequenceLength);
			Assert.Equal (2, d.ColumnWidth ());
			Assert.Equal ("ᅐ", d.ToString ());
			Assert.Equal (1, d.ToString ().Length);
			Assert.Equal (3, d.Utf8SequenceLength);
			Assert.Equal (0, e.ColumnWidth ());
			string join = "\u1104\u1161";
			Assert.Equal ("따", join);
			Assert.Equal (2, join.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (OperationStatus.Done, Rune.DecodeFromUtf16 (join.ToCharArray (), out Rune result, out int charsConsumed));
			Assert.False (join.DecodeSurrogatePair (out char [] spair));
			Assert.Equal (2, join.RuneCount ());
			Assert.Equal (2, join.Length);
			Assert.Equal ("ᅡ", e.ToString ());
			Assert.Equal (1, e.ToString ().Length);
			Assert.Equal (3, e.Utf8SequenceLength);
			string joinNormalize = join.Normalize ();
			Assert.Equal ("따", joinNormalize);
			Assert.Equal (2, joinNormalize.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (OperationStatus.Done, Rune.DecodeFromUtf16 (joinNormalize.ToCharArray (), out result, out charsConsumed));
			Assert.False (joinNormalize.DecodeSurrogatePair (out spair));
			Assert.Equal (1, joinNormalize.RuneCount ());
			Assert.Equal (1, joinNormalize.Length);
			Assert.Equal (-1, f.ColumnWidth ());
			Assert.Equal (1, f.ToString ().Length);
			Assert.Equal (1, f.Utf8SequenceLength);
			Assert.Equal (-1, g.ColumnWidth ());
			Assert.Equal (1, g.ToString ().Length);
			Assert.Equal (1, g.Utf8SequenceLength);
			var uh = h;
			(var runeh, var sizeh) = uh.DecodeRune ();
			Assert.Equal (1, runeh.ColumnWidth ());
			Assert.Equal ("🨁", h);
			Assert.Equal (2, runeh.ToString ().Length);
			Assert.Equal (4, runeh.Utf8SequenceLength);
			Assert.Equal (sizeh, runeh.Utf8SequenceLength);
			for (int x = 0; x < uh.Length - 1; x++) {
				Assert.Equal (0x1FA01, char.ConvertToUtf32 (uh [x], uh [x + 1]));
				Assert.True (RuneExtensions.EncodeSurrogatePair (uh [x], uh [x + 1], out result));
				Assert.Equal (0x1FA01, result.Value);
			}
			Assert.True (Rune.IsValid (runeh.Value));
			Assert.True (RuneExtensions.IsValid (uh.ToByteArray ()));
			//Assert.True (Rune.FullRune (uh.ToByteArray ()));
			Assert.Equal (1, uh.RuneCount ());
			(var runelh, var sizelh) = uh.DecodeLastRune ();

			Assert.Equal (1, runelh.ColumnWidth ());
			Assert.Equal (2, runelh.ToString ().Length);
			Assert.Equal (4, runelh.Utf8SequenceLength);
			Assert.Equal (sizelh, runelh.Utf8SequenceLength);
			Assert.True (Rune.IsValid (runelh.Value));

			var ui = i;
			(var runei, var sizei) = ui.DecodeRune ();
			Assert.Equal (1, runei.ColumnWidth ());
			Assert.Equal ("󠿡", i);
			Assert.Equal (2, runei.ToString ().Length);
			Assert.Equal (4, runei.Utf8SequenceLength);
			Assert.Equal (sizei, runei.Utf8SequenceLength);
			for (int x = 0; x < ui.Length - 1; x++) {
				Assert.Equal (0xE0FE1, char.ConvertToUtf32 (ui [x], ui [x + 1]));
				Assert.True (RuneExtensions.EncodeSurrogatePair (ui [x], ui [x + 1], out result));
				Assert.Equal (0xE0FE1, result.Value);
			}
			Assert.True (Rune.IsValid (runei.Value));
			Assert.True (RuneExtensions.IsValid (ui.ToByteArray ()));
			//Assert.True (Rune.FullRune (ui.ToByteArray ()));
			(var runeli, var sizeli) = ui.DecodeLastRune ();
			Assert.Equal (1, runeli.ColumnWidth ());
			Assert.Equal (2, runeli.ToString ().Length);
			Assert.Equal (4, runeli.Utf8SequenceLength);
			Assert.Equal (sizeli, runeli.Utf8SequenceLength);
			Assert.True (Rune.IsValid (runeli.Value));

			Assert.Equal (runeh.ColumnWidth (), runei.ColumnWidth ());
			Assert.NotEqual (h, i);
			Assert.Equal (runeh.ToString ().Length, runei.ToString ().Length);
			Assert.Equal (runeh.Utf8SequenceLength, runei.Utf8SequenceLength);
			var uj = j.ToString ();
			(var runej, var sizej) = uj.DecodeRune ();
			Assert.Equal (0, j.ColumnWidth ());
			Assert.Equal (0, ((Rune)uj [0]).ColumnWidth ());
			Assert.Equal (j, (Rune)uj [0]);
			Assert.Equal ("⃐", j.ToString ());
			Assert.Equal ("⃐", uj);
			Assert.Equal (1, j.ToString ().Length);
			Assert.Equal (1, runej.ToString ().Length);
			Assert.Equal (3, j.Utf8SequenceLength);
			Assert.Equal (sizej, runej.Utf8SequenceLength);
			Assert.Equal (1, k.ColumnWidth ());
			Assert.Equal ("■", k.ToString ());
			Assert.Equal (1, k.ToString ().Length);
			Assert.Equal (3, k.Utf8SequenceLength);
			Assert.Equal (1, l.ColumnWidth ());
			Assert.Equal ("□", l.ToString ());
			Assert.Equal (1, l.ToString ().Length);
			Assert.Equal (3, l.Utf8SequenceLength);
			Assert.Equal (1, m.ColumnWidth ());
			Assert.Equal ("", m.ToString ());
			Assert.Equal (1, m.ToString ().Length);
			Assert.Equal (3, m.Utf8SequenceLength);
			var rn = StringExtensions.Make (n).DecodeRune ().Rune;
			Assert.Equal (2, rn.ColumnWidth ());
			Assert.Equal ("🍕", rn.ToString ());
			Assert.Equal (2, rn.ToString ().Length);
			Assert.Equal (4, rn.Utf8SequenceLength);
			Assert.Equal (2, o.ColumnWidth ());
			Assert.Equal ("🍕", o.ToString ());
			Assert.Equal (2, o.ToString ().Length);
			Assert.Equal (4, o.Utf8SequenceLength);
			var rp = p.DecodeRune ().Rune;
			Assert.Equal (2, rp.ColumnWidth ());
			Assert.Equal ("🍕", p);
			Assert.Equal (2, p.Length);
			Assert.Equal (4, rp.Utf8SequenceLength);
			Assert.Equal (1, q.ColumnWidth ());
			Assert.Equal ("℃", q.ToString ());
			Assert.Equal (1, q.ToString ().Length);
			Assert.Equal (3, q.Utf8SequenceLength);
			var rq = q.ToString ().DecodeRune ().Rune;
			Assert.Equal (1, rq.ColumnWidth ());
			Assert.Equal ("℃", rq.ToString ());
			Assert.Equal (1, rq.ToString ().Length);
			Assert.Equal (3, rq.Utf8SequenceLength);
			Assert.Equal (2, r.ColumnWidth ());
			Assert.Equal ("ᄀ", r.ToString ());
			Assert.Equal (1, r.ToString ().Length);
			Assert.Equal (3, r.Utf8SequenceLength);
			Assert.Equal (1, s.ColumnWidth ());
			Assert.Equal ("━", s.ToString ());
			Assert.Equal (1, s.ToString ().Length);
			Assert.Equal (3, s.Utf8SequenceLength);
			var buff = new byte [4];
			var sb = ((Rune)'\u2503').EncodeRune (buff);
			Assert.Equal (1, ((Rune)'\u2503').ColumnWidth ());
			(var rune, var size) = StringExtensions.Make ('\u2503').DecodeRune ();
			Assert.Equal (sb, size);
			Assert.Equal ('\u2503', (uint)rune.Value);
			var scb = char.ConvertToUtf32 ("℃", 0);
			var scr = '℃'.ToString ().Length;
			Assert.Equal (scr, ((Rune)(uint)scb).ColumnWidth ());
			buff = new byte [4];
			sb = ((Rune)'\u1100').EncodeRune (buff);
			Assert.Equal (2, ((Rune)'\u1100').ColumnWidth ());
			Assert.Equal (2, StringExtensions.Make ((Rune)'\u1100').ConsoleWidth ());
			Assert.Equal (1, '\u1100'.ToString ().Length); // Length as string returns 1 but in reality it occupies 2 columns.
			(rune, size) = StringExtensions.Make ((Rune)'\u1100').DecodeRune ();
			Assert.Equal (sb, size);
			Assert.Equal ('\u1100', (uint)rune.Value);
			string str = "\u2615";
			Assert.Equal ("☕", str);
			Assert.Equal (2, str.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (2, str.ConsoleWidth ());
			Assert.Equal (1, str.RuneCount ());
			Assert.Equal (1, str.Length);
			str = "\u2615\ufe0f"; // Identical but \ufe0f forces it to be rendered as a colorful image as compared to a monochrome text variant.
			Assert.Equal ("☕️", str);
			Assert.Equal (2, str.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (2, str.ConsoleWidth ());
			Assert.Equal (2, str.RuneCount ());
			Assert.Equal (2, str.Length);
			str = "\u231a";
			Assert.Equal ("⌚", str);
			Assert.Equal (2, str.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (2, str.ConsoleWidth ());
			Assert.Equal (1, str.RuneCount ());
			Assert.Equal (1, str.Length);
			str = "\u231b";
			Assert.Equal ("⌛", str);
			Assert.Equal (2, str.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (2, str.ConsoleWidth ());
			Assert.Equal (1, str.RuneCount ());
			Assert.Equal (1, str.Length);
			str = "\u231c";
			Assert.Equal ("⌜", str);
			Assert.Equal (1, str.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (1, str.ConsoleWidth ());
			Assert.Equal (1, str.RuneCount ());
			Assert.Equal (1, str.Length);
			str = "\u1dc0";
			Assert.Equal ("᷀", str);
			Assert.Equal (0, str.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (0, str.ConsoleWidth ());
			Assert.Equal (1, str.RuneCount ());
			Assert.Equal (1, str.Length);
			str = "\ud83e\udd16";
			Assert.Equal ("🤖", str);
			Assert.Equal (2, str.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (2, str.ConsoleWidth ());
			Assert.Equal (1, str.RuneCount ()); // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
			Assert.Equal (2, str.Length); // String always preserves the originals values of each surrogate pair
			str = "\U0001f9e0";
			Assert.Equal ("🧠", str);
			Assert.Equal (2, str.EnumerateRunes ().Sum (x => x.ColumnWidth ()));
			Assert.Equal (2, str.ConsoleWidth ());
			Assert.Equal (1, str.RuneCount ());
			Assert.Equal (2, str.Length);
		}

		[Fact]
		public void TestRune ()
		{
			Rune a = new Rune ('a');
			Assert.Equal (1, a.ColumnWidth ());
			Assert.Equal (1, a.ToString ().Length);
			Assert.Equal ("a", a.ToString ());
			Rune b = new Rune (0x0061);
			Assert.Equal (1, b.ColumnWidth ());
			Assert.Equal (1, b.ToString ().Length);
			Assert.Equal ("a", b.ToString ());
			Rune c = new Rune ('\u0061');
			Assert.Equal (1, c.ColumnWidth ());
			Assert.Equal (1, c.ToString ().Length);
			Assert.Equal ("a", c.ToString ());
			Rune d = new Rune (0x10421);
			Assert.Equal (1, d.ColumnWidth ()); // Many surrogate pairs only occupies 1 column
			Assert.Equal (2, d.ToString ().Length);
			Assert.Equal ("𐐡", d.ToString ());
			Assert.False (RuneExtensions.EncodeSurrogatePair ('\ud799', '\udc21', out Rune rune));
			Assert.Throws<ArgumentOutOfRangeException> (() => new Rune ('\ud799', '\udc21'));
			Rune e = new Rune ('\ud801', '\udc21');
			Assert.Equal (1, e.ColumnWidth ());
			Assert.Equal (2, e.ToString ().Length);
			Assert.Equal ("𐐡", e.ToString ());
			Assert.Throws<ArgumentOutOfRangeException> (() => Assert.False (Rune.IsValid (new Rune ('\ud801').Value)));
			Rune f = new Rune ('\ud83c', '\udf39');
			Assert.Equal (2, f.ColumnWidth ());
			Assert.Equal (2, f.ToString ().Length);
			Assert.Equal ("🌹", f.ToString ());
			var exception = Record.Exception (() => new Rune (0x10ffff));
			Assert.Null (exception);
			Rune g = new Rune (0x10ffff);
			string s = "\U0010ffff";
			Assert.Equal (1, g.ColumnWidth ());
			Assert.Equal (1, s.ConsoleWidth ());
			Assert.Equal (2, g.ToString ().Length);
			Assert.Equal (2, s.Length);
			Assert.Equal ("􏿿", g.ToString ());
			Assert.Equal ("􏿿", s);
			Assert.Equal (g.ToString (), s);
			Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (0x12345678));
			var h = new Rune ('\u1150');
			Assert.Equal (2, h.ColumnWidth ());
			Assert.Equal (1, h.ToString ().Length);
			Assert.Equal ("ᅐ", h.ToString ());
			var i = new Rune ('\u4F60');
			Assert.Equal (2, i.ColumnWidth ());
			Assert.Equal (1, i.ToString ().Length);
			Assert.Equal ("你", i.ToString ());
			var j = new Rune ('\u597D');
			Assert.Equal (2, j.ColumnWidth ());
			Assert.Equal (1, j.ToString ().Length);
			Assert.Equal ("好", j.ToString ());
			var k = new Rune ('\ud83d', '\udc02');
			Assert.Equal (2, k.ColumnWidth ());
			Assert.Equal (2, k.ToString ().Length);
			Assert.Equal ("🐂", k.ToString ());
			var l = new Rune ('\ud801', '\udcbb');
			Assert.Equal (1, l.ColumnWidth ());
			Assert.Equal (2, l.ToString ().Length);
			Assert.Equal ("𐒻", l.ToString ());
			var m = new Rune ('\ud801', '\udccf');
			Assert.Equal (1, m.ColumnWidth ());
			Assert.Equal (2, m.ToString ().Length);
			Assert.Equal ("𐓏", m.ToString ());
			var n = new Rune ('\u00e1');
			Assert.Equal (1, n.ColumnWidth ());
			Assert.Equal (1, n.ToString ().Length);
			Assert.Equal ("á", n.ToString ());
			var o = new Rune ('\ud83d', '\udd2e');
			Assert.Equal (2, o.ColumnWidth ());
			Assert.Equal (2, o.ToString ().Length);
			Assert.Equal ("🔮", o.ToString ());
			var p = new Rune ('\u2329');
			Assert.Equal (2, p.ColumnWidth ());
			Assert.Equal (1, p.ToString ().Length);
			Assert.Equal ("〈", p.ToString ());
			var q = new Rune ('\u232a');
			Assert.Equal (2, q.ColumnWidth ());
			Assert.Equal (1, q.ToString ().Length);
			Assert.Equal ("〉", q.ToString ());
			var r = "\U0000232a".DecodeRune ().Rune;
			Assert.Equal (2, r.ColumnWidth ());
			Assert.Equal (1, r.ToString ().Length);
			Assert.Equal ("〉", r.ToString ());

			PrintTextElementCount ('\u00e1'.ToString (), "á", 1, 1, 1, 1);
			PrintTextElementCount (new string (new char [] { '\u0061', '\u0301' }), "á", 1, 2, 2, 1);
			PrintTextElementCount (StringExtensions.Make ('\u0061', '\u0301'), "á", 1, 2, 2, 1);
			PrintTextElementCount (StringExtensions.Make ('\u0065', '\u0301'), "é", 1, 2, 2, 1);
			PrintTextElementCount (StringExtensions.Make (new Rune [] { new Rune (0x1f469), new Rune (0x1f3fd), new Rune ('\u200d'), new Rune (0x1f692) }),
				"👩🏽‍🚒", 6, 4, 7, 1);
			PrintTextElementCount (StringExtensions.Make (new Rune [] { new Rune (0x1f469), new Rune (0x1f3fd), new Rune ('\u200d'), new Rune (0x1f692) }),
				"\U0001f469\U0001f3fd\u200d\U0001f692", 6, 4, 7, 1);
			PrintTextElementCount (StringExtensions.Make (new Rune ('\ud801', '\udccf')),
				"𐓏", 1, 1, 2, 1);
		}

		void PrintTextElementCount (string us, string s, int consoleWidth, int runeCount, int stringCount, int txtElementCount)
		{
			Assert.Equal (us.Length, s.Length);
			Assert.Equal (us, s);
			Assert.Equal (consoleWidth, us.ConsoleWidth ());
			Assert.Equal (runeCount, us.RuneCount ());
			Assert.Equal (stringCount, s.Length);

			TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator (s);

			int textElementCount = 0;
			while (enumerator.MoveNext ()) {
				textElementCount++; // For versions prior to Net5.0 the StringInfo class might handle some grapheme clusters incorrectly.
			}

			Assert.Equal (txtElementCount, textElementCount);
		}

		[Fact]
		public void TestRuneIsLetter ()
		{
			Assert.Equal (5, CountLettersInString ("Hello"));
			Assert.Equal (8, CountLettersInString ("𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟"));
		}

		int CountLettersInString (string s)
		{
			int letterCount = 0;
			foreach (Rune rune in s.EnumerateRunes ()) {
				if (Rune.IsLetter (rune)) { letterCount++; }
			}

			return letterCount;
		}

		[Fact]
		public void Test_SurrogatePair_From_String ()
		{
			Assert.True (ProcessTestStringUseChar ("𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟"));
			Assert.Throws<Exception> (() => ProcessTestStringUseChar ("\ud801"));

			Assert.True (ProcessStringUseRune ("𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟"));
			Assert.Throws<Exception> (() => ProcessStringUseRune ("\ud801"));
		}

		bool ProcessTestStringUseChar (string s)
		{
			char surrogateChar = default;
			for (int i = 0; i < s.Length; i++) {
				Rune r;
				if (char.IsSurrogate (s [i])) {
					if (surrogateChar != default && char.IsSurrogate (surrogateChar)) {
						r = new Rune (surrogateChar, s [i]);
						Assert.True (r.IsSurrogatePair ());
						int codePoint = char.ConvertToUtf32 (surrogateChar, s [i]);
						RuneExtensions.EncodeSurrogatePair (surrogateChar, s [i], out Rune rune);
						Assert.Equal (codePoint, rune.Value);
						string sp = new string (new char [] { surrogateChar, s [i] });
						r = (Rune)codePoint;
						Assert.Equal (sp, r.ToString ());
						Assert.True (r.IsSurrogatePair ());

						surrogateChar = default;
					} else if (i < s.Length - 1) {
						surrogateChar = s [i];
						continue;
					} else {
						Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (s [i]));
						throw new Exception ("String was not well-formed UTF-16.");
					}
				} else {
					r = new Rune (s [i]);
					var buff = new byte [4];
					((Rune)s [i]).EncodeRune (buff);
					Assert.Equal ((int)s [i], buff [0]);
					Assert.Equal (s [i], r.Value);
					Assert.True (Rune.IsValid (r.Value));
					Assert.False (r.IsSurrogatePair ());
				}
			}
			return true;
		}

		bool ProcessStringUseRune (string s)
		{
			var us = s;
			string rs = "";
			Rune codePoint;
			List<Rune> runes = new List<Rune> ();
			int colWidth = 0;

			for (int i = 0; i < s.Length; i++) {
				Rune rune = default;
				if (Rune.IsValid (s [i])) {
					rune = new Rune (s [i]);
					Assert.True (Rune.IsValid (rune.Value));
					runes.Add (rune);
					Assert.Equal (s [i], rune.Value);
					Assert.False (rune.IsSurrogatePair ());
				} else if (i + 1 < s.Length && (RuneExtensions.EncodeSurrogatePair (s [i], s [i + 1], out codePoint))) {
					Assert.Equal (0, rune.Value);
					Assert.False (Rune.IsValid (s [i]));
					rune = codePoint;
					runes.Add (rune);
					string sp = new string (new char [] { s [i], s [i + 1] });
					Assert.Equal (sp, codePoint.ToString ());
					Assert.True (codePoint.IsSurrogatePair ());
					i++; // Increment the iterator by the number of surrogate pair
				} else {
					Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (s [i]));
					throw new Exception ("String was not well-formed UTF-16.");
				}
				colWidth += rune.ColumnWidth (); // Increment the column width of this Rune
				rs += rune.ToString ();
			}
			Assert.Equal (us.ConsoleWidth (), colWidth);
			Assert.Equal (s, rs);
			Assert.Equal (s, StringExtensions.Make (runes));
			return true;
		}

		[Fact]
		public void TestSplit ()
		{
			string inputString = "🐂, 🐄, 🐆";
			string [] splitOnSpace = inputString.Split (' ');
			string [] splitOnComma = inputString.Split (',');
			Assert.Equal (3, splitOnSpace.Length);
			Assert.Equal (3, splitOnComma.Length);
		}

		[Fact]
		public void TestValidRune ()
		{
			Assert.True (Rune.IsValid (new Rune ('\u1100').Value));
			Assert.True (Rune.IsValid (new Rune ('\ud83c', '\udf39').Value));
			Assert.False (Rune.IsValid ('\ud801'));
			Assert.False (Rune.IsValid ((uint)'\ud801'));
			Assert.Throws<ArgumentOutOfRangeException> (() => Assert.False (Rune.IsValid (((Rune)'\ud801').Value)));
		}

		[Fact]
		public void TestValid ()
		{
			var rune1 = new Rune ('\ud83c', '\udf39');
			var buff1 = new byte [4];
			Assert.Equal (4, rune1.EncodeRune (buff1));
			Assert.True (RuneExtensions.IsValid (buff1));
			Assert.Equal (2, rune1.ToString ().Length);
			Assert.Equal (4, rune1.Utf8SequenceLength);
			var c = '\ud801';
			var buff2 = Encoding.UTF8.GetBytes (new char [] { c });
			//var buff2 = new byte [4];
			Assert.Equal (3, buff2.Length);
			var str = Encoding.UTF8.GetString (buff2);
			Assert.Equal ("�", str);
			Assert.False (RuneExtensions.IsValid (buff2));
			Assert.Equal (1, str.Length);
			Assert.Throws<ArgumentOutOfRangeException> (() => (Rune)'\ud801');
			Assert.Equal (new byte [] { 0xef, 0xbf, 0xbd }, buff2);
			Assert.Equal (Rune.ReplacementChar.ToString (), str);
		}

		[Fact]
		public void Test_IsNonSpacingChar ()
		{
			Rune l = (Rune)'\u0370';
			Assert.False (l.IsNonSpacingChar ());
			Assert.Equal (1, l.ColumnWidth ());
			Assert.Equal (1, StringExtensions.Make (l).ConsoleWidth ());
			Rune ns = (Rune)'\u302a';
			Assert.False (ns.IsNonSpacingChar ());
			Assert.Equal (2, ns.ColumnWidth ());
			Assert.Equal (2, StringExtensions.Make (ns).ConsoleWidth ());
			l = (Rune)'\u006f';
			ns = (Rune)'\u0302';
			var s = "\u006f\u0302";
			Assert.Equal (1, l.ColumnWidth ());
			Assert.Equal (0, ns.ColumnWidth ());
			var ul = StringExtensions.Make (l);
			Assert.Equal ("o", ul);
			var uns = StringExtensions.Make (ns);
			Assert.Equal ("̂", uns);
			var f = $"{l}{ns}";
			Assert.Equal ("ô", f);
			Assert.Equal (f, s);
			Assert.Equal (1, f.ConsoleWidth ());
			Assert.Equal (1, s.EnumerateRunes ().Sum (c => c.ColumnWidth ()));
			Assert.Equal (2, s.Length);
			(var rune, var size) = f.DecodeRune ();
			Assert.Equal (rune, l);
			Assert.Equal (1, size);
			l = (Rune)'\u0041';
			ns = (Rune)'\u0305';
			s = "\u0041\u0305";
			Assert.Equal (1, l.ColumnWidth ());
			Assert.Equal (0, ns.ColumnWidth ());
			ul = StringExtensions.Make (l);
			Assert.Equal ("A", ul);
			uns = StringExtensions.Make (ns);
			Assert.Equal ("̅", uns);
			f = $"{l}{ns}";
			Assert.Equal ("A̅", f);
			Assert.Equal (f, s);
			Assert.Equal (1, f.ConsoleWidth ());
			Assert.Equal (1, s.EnumerateRunes ().Sum (c => c.ColumnWidth ()));
			Assert.Equal (2, s.Length);
			(rune, size) = f.DecodeRune ();
			Assert.Equal (rune, l);
			Assert.Equal (1, size);
			l = (Rune)'\u0061';
			ns = (Rune)'\u0308';
			s = "\u0061\u0308";
			Assert.Equal (1, l.ColumnWidth ());
			Assert.Equal (0, ns.ColumnWidth ());
			ul = StringExtensions.Make (l);
			Assert.Equal ("a", ul);
			uns = StringExtensions.Make (ns);
			Assert.Equal ("̈", uns);
			f = $"{l}{ns}";
			Assert.Equal ("ä", f);
			Assert.Equal (f, s);
			Assert.Equal (1, f.ConsoleWidth ());
			Assert.Equal (1, s.EnumerateRunes ().Sum (c => c.ColumnWidth ()));
			Assert.Equal (2, s.Length);
			(rune, size) = f.DecodeRune ();
			Assert.Equal (rune, l);
			Assert.Equal (1, size);
			l = (Rune)'\u4f00';
			ns = (Rune)'\u302a';
			s = "\u4f00\u302a";
			Assert.Equal (2, l.ColumnWidth ());
			Assert.Equal (2, ns.ColumnWidth ());
			ul = StringExtensions.Make (l);
			Assert.Equal ("伀", ul);
			uns = StringExtensions.Make (ns);
			Assert.Equal ("〪", uns);
			f = $"{l}{ns}";
			Assert.Equal ("伀〪", f); // Occupies 4 columns.
			Assert.Equal (f, s);
			Assert.Equal (4, f.ConsoleWidth ());
			Assert.Equal (4, s.EnumerateRunes ().Sum (c => c.ColumnWidth ()));
			Assert.Equal (2, s.Length);
			(rune, size) = f.DecodeRune ();
			Assert.Equal (rune, l);
			Assert.Equal (3, size);
		}

		[Fact]
		public void Test_IsWideChar ()
		{
			Assert.True (((Rune)0x115e).IsWideChar ());
			Assert.Equal (2, ((Rune)0x115e).ColumnWidth ());
			Assert.False (((Rune)0x116f).IsWideChar ());
		}

		[Fact]
		public void Test_MaxRune ()
		{
			Assert.Throws<ArgumentOutOfRangeException> (() => new Rune (500000000));
			Assert.Throws<ArgumentOutOfRangeException> (() => new Rune ((char)0xf801, (char)0xdfff));
		}

		[Fact]
		public void Sum_Of_ColumnWidth_Is_Not_Always_Equal_To_ConsoleWidth ()
		{
			const int start = 0x000000;
			const int end = 0x10ffff;

			for (int i = start; i <= end; i++) {
				if (char.IsSurrogate ((char)i)) {
					continue;
				}
				Rune r = new Rune ((uint)i);
				string us = StringExtensions.Make (r);
				string hex = i.ToString ("x6");
				int v = int.Parse (hex, System.Globalization.NumberStyles.HexNumber);
				string s = char.ConvertFromUtf32 (v);

				if (!r.IsSurrogatePair ()) {
					Assert.Equal (r.ToString (), us);
					Assert.Equal (us, s);
					if (r.ColumnWidth () < 0) {
						Assert.NotEqual (r.ColumnWidth (), us.ConsoleWidth ());
						Assert.NotEqual (s.EnumerateRunes ().Sum (c => c.ColumnWidth ()), us.ConsoleWidth ());
					} else {
						Assert.Equal (r.ColumnWidth (), us.ConsoleWidth ());
						Assert.Equal (s.EnumerateRunes ().Sum (c => c.ColumnWidth ()), us.ConsoleWidth ());
					}
					Assert.Equal (us.RuneCount (), s.Length);
				} else {
					Assert.Equal (r.ToString (), us);
					Assert.Equal (us, s);
					Assert.Equal (r.ColumnWidth (), us.ConsoleWidth ());
					Assert.Equal (s.ConsoleWidth (), us.ConsoleWidth ());
					Assert.Equal (1, us.RuneCount ()); // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
					Assert.Equal (2, s.Length); // String always preserves the originals values of each surrogate pair
				}
			}
		}

		[Fact]
		public void Test_Right_To_Left_Runes ()
		{
			Rune r0 = (Rune)0x020000;
			Rune r7 = (Rune)0x020007;
			Rune r1b = (Rune)0x02001b;
			Rune r9b = (Rune)0x02009b;

			Assert.Equal (2, r0.ColumnWidth ());
			Assert.Equal (2, r7.ColumnWidth ());
			Assert.Equal (2, r1b.ColumnWidth ());
			Assert.Equal (2, r9b.ColumnWidth ());

			"𠀀".DecodeSurrogatePair (out char [] chars);
			var rtl = new Rune (chars [0], chars [1]);
			var rtlp = new Rune ('\ud840', '\udc00');
			var s = "\U00020000";

			Assert.Equal (2, rtl.ColumnWidth ());
			Assert.Equal (2, rtlp.ColumnWidth ());
			Assert.Equal (2, s.Length);
		}

		[Theory]
		[InlineData (0x20D0, 0x20EF)]
		[InlineData (0x2310, 0x231F)]
		[InlineData (0x1D800, 0x1D80F)]
		public void Test_Range (int start, int end)
		{
			for (int i = start; i <= end; i++) {
				Rune r = new Rune ((uint)i);
				string us = StringExtensions.Make (r);
				string hex = i.ToString ("x6");
				int v = int.Parse (hex, System.Globalization.NumberStyles.HexNumber);
				string s = char.ConvertFromUtf32 (v);

				if (!r.IsSurrogatePair ()) {
					Assert.Equal (r.ToString (), us);
					Assert.Equal (us, s);
					Assert.Equal (r.ColumnWidth (), us.ConsoleWidth ());
					Assert.Equal (us.RuneCount (), s.Length); // For not surrogate pairs string.RuneCount is always equal to String.Length
				} else {
					Assert.Equal (r.ToString (), us);
					Assert.Equal (us, s);
					Assert.Equal (r.ColumnWidth (), us.ConsoleWidth ());
					Assert.Equal (1, us.RuneCount ()); // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
					Assert.Equal (2, s.Length); // String always preserves the originals values of each surrogate pair
				}
				Assert.Equal (s.ConsoleWidth (), us.ConsoleWidth ());
			}
		}

		[Fact]
		public void Test_IsSurrogate ()
		{
			char c = '\ue0fd';
			Assert.False (char.IsSurrogate (c.ToString (), 0));
			Rune r = (Rune)0x927C0;
			Assert.True (r.IsSurrogatePair ());
			Assert.True (r.IsSurrogatePair ());
			Assert.True (char.IsSurrogate (r.ToString (), 0));
			Assert.True (char.IsSurrogate (r.ToString (), 1));
			Assert.True (char.IsSurrogatePair (r.ToString (), 0));

			c = '\ud800';
			Assert.True (char.IsSurrogate (c.ToString (), 0));
			c = '\udfff';
			Assert.True (char.IsSurrogate (c.ToString (), 0));
		}

		[Fact]
		public void Test_EncodeSurrogatePair ()
		{
			Assert.False (RuneExtensions.EncodeSurrogatePair (unchecked((char)0x40D7C0), (char)0xDC20, out Rune rune));
			Assert.Equal (0, rune.Value);
			Assert.False (RuneExtensions.EncodeSurrogatePair ((char)0x0065, (char)0x0301, out rune));
			Assert.Equal (0, rune.Value);
			Assert.True (RuneExtensions.EncodeSurrogatePair ('\ud83c', '\udf56', out rune));
			Assert.Equal (0x1F356, rune.Value);
			Assert.Equal ("🍖", rune.ToString ());
		}

		[Fact]
		public void Test_DecodeSurrogatePair ()
		{
			Assert.False (((Rune)'\uea85').DecodeSurrogatePair (out char [] chars));
			Assert.Null (chars);
			Assert.True (((Rune)0x1F356).DecodeSurrogatePair (out chars));
			Assert.Equal (2, chars.Length);
			Assert.Equal ('\ud83c', chars [0]);
			Assert.Equal ('\udf56', chars [1]);
			Assert.Equal ("🍖", new Rune (chars [0], chars [1]).ToString ());
		}

		[Fact]
		public void Test_Surrogate_Pairs_Range ()
		{
			for (uint h = 0xd800; h <= 0xdbff; h++) {
				for (uint l = 0xdc00; l <= 0xdfff; l++) {
					Rune r = new Rune ((char)h, (char)l);
					string us = StringExtensions.Make (r);
					string hex = r.Value.ToString ("x6");
					int v = int.Parse (hex, System.Globalization.NumberStyles.HexNumber);
					string s = char.ConvertFromUtf32 (v);

					Assert.True (v >= 0x10000 && v <= RuneExtensions.MaxRune.Value);
					Assert.Equal (r.ToString (), us);
					Assert.Equal (us, s);
					Assert.Equal (r.ColumnWidth (), us.ConsoleWidth ());
					Assert.Equal (s.ConsoleWidth (), us.ConsoleWidth ());
					Assert.Equal (1, us.RuneCount ()); // Here returns 1 because is a valid surrogate pair resulting in only rune >=U+10000..U+10FFFF
					Assert.Equal (2, s.Length); // String always preserves the originals values of each surrogate pair
				}
			}
		}

		[Fact]
		public void Test_DecodeRune_Extension ()
		{
			string us = "Hello, 世界";
			List<Rune> runes = new List<Rune> ();
			int tSize = 0;
			for (int i = 0; i < us.RuneCount (); i++) {
				(Rune rune, int size) = us.DecodeRune (i);
				runes.Add (rune);
				tSize += size;
			}
			string result = StringExtensions.Make (runes);
			Assert.Equal ("Hello, 世界", result);
			Assert.Equal (13, tSize);
			Assert.Equal (11, result.ConsoleWidth ());
		}

		[Fact]
		public void Test_DecodeRune_With_Surrogate_Pairs ()
		{
			string us = "Hello, 𝔹𝕆𝔹";
			List<Rune> runes = new List<Rune> ();
			int tSize = 0;
			for (int i = 0; i < us.RuneCount (); i++) {
				(Rune rune, int size) = us.DecodeRune (i);
				runes.Add (rune);
				tSize += size;
			}
			string result = StringExtensions.Make (runes);
			Assert.Equal ("Hello, 𝔹𝕆𝔹", result);
			Assert.Equal (19, tSize);
			Assert.Equal (13, result.ConsoleWidth ());
		}

		[Fact]
		public void Test_DecodeLastRune_Extension ()
		{
			string us = "Hello, 世界";
			List<Rune> runes = new List<Rune> ();
			int tSize = 0;
			for (int i = us.RuneCount () - 1; i >= 0; i--) {
				(Rune rune, int size) = us.DecodeLastRune (i);
				runes.Add (rune);
				tSize += size;
			}
			string result = StringExtensions.Make (runes);
			Assert.Equal ("界世 ,olleH", result);
			Assert.Equal (13, tSize);
			Assert.Equal (11, result.ConsoleWidth ());
		}

		[Fact]
		public void Test_DecodeLastRune_With_Surrogate_Pairs ()
		{
			string us = "Hello, 𝔹𝕆𝔹";
			List<Rune> runes = new List<Rune> ();
			int tSize = 0;
			for (int i = us.RuneCount () - 1; i >= 0; i--) {
				(Rune rune, int size) = us.DecodeLastRune (i);
				runes.Add (rune);
				tSize += size;
			}
			string result = StringExtensions.Make (runes);
			Assert.Equal ("𝔹𝕆𝔹 ,olleH", result);
			Assert.Equal (19, tSize);
			Assert.Equal (13, result.ConsoleWidth ());
		}

		[Fact]
		public void Test_Valid_Extension ()
		{
			string us = "Hello, 世界";
			Assert.True (RuneExtensions.IsValid (us.ToByteArray ()));
			us = StringExtensions.Make (new byte [] { 0xff, 0xfe, 0xfd });
			Assert.False (RuneExtensions.IsValid (us.ToByteArray ()));
		}

		[Fact]
		public void Equals_ToRuneList ()
		{
			var a = new List<List<Rune>> () { "First line.".ToRuneList () };
			var b = new List<List<Rune>> () { "First line.".ToRuneList (), "Second line.".ToRuneList () };
			var c = new List<Rune> (a [0]);
			var d = a [0];

			Assert.Equal (a [0], b [0]);
			// Not the same reference
			Assert.False (a [0] == b [0]);
			Assert.NotEqual (a [0], b [1]);
			Assert.False (a [0] == b [1]);

			Assert.Equal (c, a [0]);
			Assert.False (c == a [0]);
			Assert.Equal (c, b [0]);
			Assert.False (c == b [0]);
			Assert.NotEqual (c, b [1]);
			Assert.False (c == b [1]);

			Assert.Equal (d, a [0]);
			// Is the same reference
			Assert.True (d == a [0]);
			Assert.Equal (d, b [0]);
			Assert.False (d == b [0]);
			Assert.NotEqual (d, b [1]);
			Assert.False (d == b [1]);

			Assert.True (a [0].SequenceEqual (b [0]));
			Assert.False (a [0].SequenceEqual (b [1]));

			Assert.True (c.SequenceEqual (a [0]));
			Assert.True (c.SequenceEqual (b [0]));
			Assert.False (c.SequenceEqual (b [1]));

			Assert.True (d.SequenceEqual (a [0]));
			Assert.True (d.SequenceEqual (b [0]));
			Assert.False (d.SequenceEqual (b [1]));
		}

		[Fact]
		public void Rune_ColumnWidth_Versus_Ustring_ConsoleWidth_With_Non_Printable_Characters ()
		{
			int sumRuneWidth = 0;
			int sumConsoleWidth = 0;
			for (uint i = 0; i < 32; i++) {
				sumRuneWidth += ((Rune)i).ColumnWidth ();
				sumConsoleWidth += StringExtensions.Make (i).ConsoleWidth ();
			}

			Assert.Equal (-32, sumRuneWidth);
			Assert.Equal (0, sumConsoleWidth);
		}

		[Fact]
		public void Rune_ColumnWidth_Versus_Ustring_ConsoleWidth ()
		{
			string us = "01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";
			Assert.Equal (200, us.Length);
			Assert.Equal (200, us.RuneCount ());
			Assert.Equal (200, us.ConsoleWidth ());
			int sumRuneWidth = us.EnumerateRunes ().Sum (x => x.ColumnWidth ());
			Assert.Equal (200, sumRuneWidth);

			us = "01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789\n";
			Assert.Equal (201, us.Length);
			Assert.Equal (201, us.RuneCount ());
			Assert.Equal (200, us.ConsoleWidth ());
			sumRuneWidth = us.EnumerateRunes ().Sum (x => x.ColumnWidth ());
			Assert.Equal (199, sumRuneWidth);
		}

		[Fact]
		public void Rune_IsHighSurrogate_IsLowSurrogate ()
		{
			char c = '\ud800';
			Assert.True (char.IsHighSurrogate (c));

			c = '\udbff';
			Assert.True (char.IsHighSurrogate (c));

			c = '\udc00';
			Assert.True (char.IsLowSurrogate (c));

			c = '\udfff';
			Assert.True (char.IsLowSurrogate (c));
		}

		[Fact]
		public void Rune_ToRunes ()
		{
			var str = "First line.";
			var runes = str.ToRunes ();
			for (int i = 0; i < runes.Length; i++) {
				Assert.Equal (str [i], runes [i].Value);
			}
		}

		[Fact]
		public void System_Rune_ColumnWidth ()
		{
			var r = new Rune ('a');
			Assert.Equal (1, r.ColumnWidth ());
			Assert.Equal (1, r.ToString ().ConsoleWidth ());
			Assert.Equal (1, r.ToString ().Length);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);

			r = new Rune ('b');
			Assert.Equal (1, r.ColumnWidth ());
			Assert.Equal (1, r.ToString ().ConsoleWidth ());
			Assert.Equal (1, r.ToString ().Length);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);

			r = new Rune (123);
			Assert.Equal (1, r.ColumnWidth ());
			Assert.Equal (1, r.ToString ().ConsoleWidth ());
			Assert.Equal (1, r.ToString ().Length);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);

			r = new Rune ('\u1150');
			Assert.Equal (2, r.ColumnWidth ());      // 0x1150	ᅐ	Unicode Technical Report #11
			Assert.Equal (2, r.ToString ().ConsoleWidth ());
			Assert.Equal (1, r.ToString ().Length);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (3, r.Utf8SequenceLength);

			r = new Rune ('\u1161');
			Assert.Equal (0, r.ColumnWidth ());      // 0x1161	ᅡ	column width of 0
			Assert.Equal (0, r.ToString ().ConsoleWidth ());
			Assert.Equal (1, r.ToString ().Length);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (3, r.Utf8SequenceLength);

			r = new Rune (31);
			Assert.Equal (-1, r.ColumnWidth ());        // non printable character
			Assert.Equal (0, r.ToString ().ConsoleWidth ());// ConsoleWidth only returns zero or greater than zero
			Assert.Equal (1, r.ToString ().Length);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);

			r = new Rune (127);
			Assert.Equal (-1, r.ColumnWidth ());       // non printable character
			Assert.Equal (0, r.ToString ().ConsoleWidth ());
			Assert.Equal (1, r.ToString ().Length);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);

			r = new Rune (0x16fe0);
			Assert.Equal (2, r.ColumnWidth ());       // non printable character
			Assert.Equal (2, r.ToString ().ConsoleWidth ());
			Assert.Equal (2, r.ToString ().Length);
			Assert.Equal (2, r.Utf16SequenceLength);
			Assert.Equal (4, r.Utf8SequenceLength);
		}

		[Fact]
		public void System_Text_Rune ()
		{
			var r = new System.Text.Rune ('a');
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);

			r = new System.Text.Rune ('b');
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);

			r = new System.Text.Rune (123);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);

			r = new System.Text.Rune ('\u1150');
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (3, r.Utf8SequenceLength);         // 0x1150	ᅐ	Unicode Technical Report #11

			r = new System.Text.Rune ('\u1161');
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (3, r.Utf8SequenceLength);         // 0x1161	ᅡ	column width of 0

			r = new System.Text.Rune (31);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);         // non printable character

			r = new System.Text.Rune (127);
			Assert.Equal (1, r.Utf16SequenceLength);
			Assert.Equal (1, r.Utf8SequenceLength);         // non printable character

			r = new System.Text.Rune (0x16fe0);
			Assert.Equal (2, r.Utf16SequenceLength);
			Assert.Equal (4, r.Utf8SequenceLength);
		}
	}
}
