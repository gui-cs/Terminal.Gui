using System;
using System.Collections;
using System.Collections.Generic;
using Terminal.Gui;
using Terminal.Gui.ConsoleDrivers;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.InputTests;

public class KeyTests {
	readonly ITestOutputHelper _output;

	public KeyTests (ITestOutputHelper output)
	{
		_output = output;
	}

	enum SimpleEnum { Zero, One, Two, Three, Four, Five }

	[Flags]
	enum FlaggedEnum { Zero, One, Two, Three, Four, Five }

	enum SimpleHighValueEnum { Zero, One, Two, Three, Four, Last = 0x40000000 }

	[Flags]
	enum FlaggedHighValueEnum { Zero, One, Two, Three, Four, Last = 0x40000000 }

	[Fact]
	public void SimpleEnum_And_FlagedEnum ()
	{
		var simple = SimpleEnum.Three | SimpleEnum.Five;

		// Nothing will not be well compared here.
		Assert.True (simple.HasFlag (SimpleEnum.Zero | SimpleEnum.Five));
		Assert.True (simple.HasFlag (SimpleEnum.One | SimpleEnum.Five));
		Assert.True (simple.HasFlag (SimpleEnum.Two | SimpleEnum.Five));
		Assert.True (simple.HasFlag (SimpleEnum.Three | SimpleEnum.Five));
		Assert.True (simple.HasFlag (SimpleEnum.Four | SimpleEnum.Five));
		Assert.True ((simple & (SimpleEnum.Zero | SimpleEnum.Five)) != 0);
		Assert.True ((simple & (SimpleEnum.One | SimpleEnum.Five)) != 0);
		Assert.True ((simple & (SimpleEnum.Two | SimpleEnum.Five)) != 0);
		Assert.True ((simple & (SimpleEnum.Three | SimpleEnum.Five)) != 0);
		Assert.True ((simple & (SimpleEnum.Four | SimpleEnum.Five)) != 0);
		Assert.Equal (7, (int)simple); // As it is not flagged only shows as number.
		Assert.Equal ("7", simple.ToString ());
		Assert.False (simple == (SimpleEnum.Zero | SimpleEnum.Five));
		Assert.False (simple == (SimpleEnum.One | SimpleEnum.Five));
		Assert.True (simple == (SimpleEnum.Two | SimpleEnum.Five));
		Assert.True (simple == (SimpleEnum.Three | SimpleEnum.Five));
		Assert.False (simple == (SimpleEnum.Four | SimpleEnum.Five));

		var flagged = FlaggedEnum.Three | FlaggedEnum.Five;

		// Nothing will not be well compared here.
		Assert.True (flagged.HasFlag (FlaggedEnum.Zero | FlaggedEnum.Five));
		Assert.True (flagged.HasFlag (FlaggedEnum.One | FlaggedEnum.Five));
		Assert.True (flagged.HasFlag (FlaggedEnum.Two | FlaggedEnum.Five));
		Assert.True (flagged.HasFlag (FlaggedEnum.Three | FlaggedEnum.Five));
		Assert.True (flagged.HasFlag (FlaggedEnum.Four | FlaggedEnum.Five));
		Assert.True ((flagged & (FlaggedEnum.Zero | FlaggedEnum.Five)) != 0);
		Assert.True ((flagged & (FlaggedEnum.One | FlaggedEnum.Five)) != 0);
		Assert.True ((flagged & (FlaggedEnum.Two | FlaggedEnum.Five)) != 0);
		Assert.True ((flagged & (FlaggedEnum.Three | FlaggedEnum.Five)) != 0);
		Assert.True ((flagged & (FlaggedEnum.Four | FlaggedEnum.Five)) != 0);
		Assert.Equal (FlaggedEnum.Two | FlaggedEnum.Five, flagged); // As it is flagged shows as bitwise.
		Assert.Equal ("Two, Five", flagged.ToString ());
		Assert.False (flagged == (FlaggedEnum.Zero | FlaggedEnum.Five));
		Assert.False (flagged == (FlaggedEnum.One | FlaggedEnum.Five));
		Assert.True (flagged == (FlaggedEnum.Two | FlaggedEnum.Five));
		Assert.True (flagged == (FlaggedEnum.Three | FlaggedEnum.Five));
		Assert.False (flagged == (FlaggedEnum.Four | FlaggedEnum.Five));
	}

	[Fact]
	public void SimpleHighValueEnum_And_FlaggedHighValueEnum ()
	{
		var simple = SimpleHighValueEnum.Three | SimpleHighValueEnum.Last;

		// This will not be well compared.
		Assert.True (simple.HasFlag (SimpleHighValueEnum.Zero | SimpleHighValueEnum.Last));
		Assert.True (simple.HasFlag (SimpleHighValueEnum.One | SimpleHighValueEnum.Last));
		Assert.True (simple.HasFlag (SimpleHighValueEnum.Two | SimpleHighValueEnum.Last));
		Assert.True (simple.HasFlag (SimpleHighValueEnum.Three | SimpleHighValueEnum.Last));
		Assert.False (simple.HasFlag (SimpleHighValueEnum.Four | SimpleHighValueEnum.Last));
		Assert.True ((simple & (SimpleHighValueEnum.Zero | SimpleHighValueEnum.Last)) != 0);
		Assert.True ((simple & (SimpleHighValueEnum.One | SimpleHighValueEnum.Last)) != 0);
		Assert.True ((simple & (SimpleHighValueEnum.Two | SimpleHighValueEnum.Last)) != 0);
		Assert.True ((simple & (SimpleHighValueEnum.Three | SimpleHighValueEnum.Last)) != 0);
		Assert.True ((simple & (SimpleHighValueEnum.Four | SimpleHighValueEnum.Last)) != 0);

		// This will be well compared, because the SimpleHighValueEnum.Last have a high value.
		Assert.Equal (1073741827, (int)simple); // As it is not flagged only shows as number.
		Assert.Equal ("1073741827", simple.ToString ()); // As it is not flagged only shows as number.
		Assert.False (simple == (SimpleHighValueEnum.Zero | SimpleHighValueEnum.Last));
		Assert.False (simple == (SimpleHighValueEnum.One | SimpleHighValueEnum.Last));
		Assert.False (simple == (SimpleHighValueEnum.Two | SimpleHighValueEnum.Last));
		Assert.True (simple == (SimpleHighValueEnum.Three | SimpleHighValueEnum.Last));
		Assert.False (simple == (SimpleHighValueEnum.Four | SimpleHighValueEnum.Last));

		var flagged = FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last;

		// This will not be well compared.
		Assert.True (flagged.HasFlag (FlaggedHighValueEnum.Zero | FlaggedHighValueEnum.Last));
		Assert.True (flagged.HasFlag (FlaggedHighValueEnum.One | FlaggedHighValueEnum.Last));
		Assert.True (flagged.HasFlag (FlaggedHighValueEnum.Two | FlaggedHighValueEnum.Last));
		Assert.True (flagged.HasFlag (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last));
		Assert.False (flagged.HasFlag (FlaggedHighValueEnum.Four | FlaggedHighValueEnum.Last));
		Assert.True ((flagged & (FlaggedHighValueEnum.Zero | FlaggedHighValueEnum.Last)) != 0);
		Assert.True ((flagged & (FlaggedHighValueEnum.One | FlaggedHighValueEnum.Last)) != 0);
		Assert.True ((flagged & (FlaggedHighValueEnum.Two | FlaggedHighValueEnum.Last)) != 0);
		Assert.True ((flagged & (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last)) != 0);
		Assert.True ((flagged & (FlaggedHighValueEnum.Four | FlaggedHighValueEnum.Last)) != 0);

		// This will be well compared, because the SimpleHighValueEnum.Last have a high value.
		Assert.Equal (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last, flagged); // As it is flagged shows as bitwise.
		Assert.Equal ("Three, Last", flagged.ToString ()); // As it is flagged shows as bitwise.
		Assert.False (flagged == (FlaggedHighValueEnum.Zero | FlaggedHighValueEnum.Last));
		Assert.False (flagged == (FlaggedHighValueEnum.One | FlaggedHighValueEnum.Last));
		Assert.False (flagged == (FlaggedHighValueEnum.Two | FlaggedHighValueEnum.Last));
		Assert.True (flagged == (FlaggedHighValueEnum.Three | FlaggedHighValueEnum.Last));
		Assert.False (flagged == (FlaggedHighValueEnum.Four | FlaggedHighValueEnum.Last));
	}

	[Fact]
	public void Key_Enum_Ambiguity_Check ()
	{
		var key = KeyCode.Y | KeyCode.CtrlMask;

		// This will not be well compared.
		Assert.True (key.HasFlag (KeyCode.Q | KeyCode.CtrlMask));
		Assert.True ((key & (KeyCode.Q | KeyCode.CtrlMask)) != 0);
		Assert.Equal (KeyCode.Y | KeyCode.CtrlMask, key);
		Assert.Equal ("Y, CtrlMask", key.ToString ());

		// This will be well compared, because the Key.CtrlMask have a high value.
		Assert.False (key == Application.QuitKey);
		switch (key) {
		case KeyCode.Q | KeyCode.CtrlMask:
			// Never goes here.
			break;
		case KeyCode.Y | KeyCode.CtrlMask:
			Assert.True (key == (KeyCode.Y | KeyCode.CtrlMask));
			break;
		default:
			// Never goes here.
			break;
		}
	}

	[Fact]
	public void KeyEnum_ShouldHaveCorrectValues ()
	{
		Assert.Equal (0, (int)KeyCode.Null);
		Assert.Equal (8, (int)KeyCode.Backspace);
		Assert.Equal (9, (int)KeyCode.Tab);
		// Continue for other keys...
	}

	[Fact]
	public void Key_ToString ()
	{
		var k = KeyCode.Y | KeyCode.CtrlMask;
		Assert.Equal ("Y, CtrlMask", k.ToString ());

		k = KeyCode.CtrlMask | KeyCode.Y;
		Assert.Equal ("Y, CtrlMask", k.ToString ());

		k = KeyCode.Space;
		Assert.Equal ("Space", k.ToString ());

		k = KeyCode.D;
		Assert.Equal ("D", k.ToString ());

		k = (KeyCode)'d';
		Assert.Equal ("d", ((char)k).ToString ());

		k = KeyCode.D;
		Assert.Equal ("D", k.ToString ());

		// In a console this will always returns Key.D
		k = KeyCode.D | KeyCode.ShiftMask;
		Assert.Equal ("D, ShiftMask", k.ToString ());
	}

	static object packetLock = new object ();

	/// <summary>
	/// Sometimes when using remote tools EventKeyRecord sends 'virtual keystrokes'.
	/// These are indicated with the wVirtualKeyCode of 231. When we see this code
	/// then we need to look to the unicode character (UnicodeChar) instead of the key
	/// when telling the rest of the framework what button was pressed. For full details
	/// see: https://github.com/gui-cs/Terminal.Gui/issues/2008
	/// </summary>
	[Theory]
	[AutoInitShutdown]
	[ClassData (typeof (PacketTest))]
	public void TestVKPacket (uint unicodeCharacter, bool shift, bool alt, bool control, uint initialVirtualKey,
				uint initialScanCode, KeyCode expectedRemapping, uint expectedVirtualKey, uint expectedScanCode)
	{
		lock (packetLock) {
			Application._forceFakeConsole = true;
			Application.Init ();

			var modifiers = new ConsoleModifiers ();
			if (shift) {
				modifiers |= ConsoleModifiers.Shift;
			}
			if (alt) {
				modifiers |= ConsoleModifiers.Alt;
			}
			if (control) {
				modifiers |= ConsoleModifiers.Control;
			}
			ConsoleKeyInfo consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyFromKey (unicodeCharacter, modifiers, out uint scanCode);

			Assert.Equal ((uint)consoleKeyInfo.Key, initialVirtualKey);


			//if (scanCode > 0 && consoleKey == keyChar && consoleKey > 48 && consoleKey > 57 && consoleKey < 65 && consoleKey > 91) {
			if (scanCode > 0 && consoleKeyInfo.KeyChar == 0) {
				Assert.Equal (0, (double)consoleKeyInfo.KeyChar);
			} else {
				Assert.Equal (consoleKeyInfo.KeyChar, unicodeCharacter);
			}
			Assert.Equal ((uint)consoleKeyInfo.Key, expectedVirtualKey);
			Assert.Equal (scanCode, initialScanCode);
			Assert.Equal (scanCode, expectedScanCode);

			var top = Application.Top;

			top.KeyDown += (s, e) => {
				Assert.Equal (KeyEventArgs.ToString (expectedRemapping), KeyEventArgs.ToString (e.KeyCode));
				e.Handled = true;
				Application.RequestStop ();
			};

			int iterations = -1;

			Application.Iteration += (s, a) => {
				iterations++;
				if (iterations == 0) {
					Application.Driver.SendKeys (consoleKeyInfo.KeyChar, ConsoleKey.Packet, shift, alt, control);
				}
			};
			Application.Run ();
			Application.Shutdown ();
		}
	}

	public class PacketTest : IEnumerable, IEnumerable<object []> {
		public IEnumerator<object []> GetEnumerator ()
		{
			lock (packetLock) {
				// unicodeCharacter, shift, alt, control, initialVirtualKey, initialScanCode, expectedRemapping, expectedVirtualKey, expectedScanCode
				yield return new object [] { 'a', false, false, false, 'A', 30, KeyCode.A, 'A', 30 };
				yield return new object [] { 'A', true, false, false, 'A', 30, KeyCode.A | KeyCode.ShiftMask, 'A', 30 };
				yield return new object [] { 'A', true, true, false, 'A', 30, KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask, 'A', 30 };
				yield return new object [] { 'A', true, true, true, 'A', 30, KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 'A', 30 };
				yield return new object [] { 'z', false, false, false, 'Z', 44, KeyCode.Z, 'Z', 44 };
				yield return new object [] { 'Z', true, false, false, 'Z', 44, KeyCode.Z | KeyCode.ShiftMask, 'Z', 44 };
				yield return new object [] { 'Z', true, true, false, 'Z', 44, KeyCode.Z | KeyCode.ShiftMask | KeyCode.AltMask, 'Z', 44 };
				yield return new object [] { 'Z', true, true, true, 'Z', 44, KeyCode.Z | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 'Z', 44 };
				yield return new object [] { '英', false, false, false, '\0', 0, (KeyCode)'英', '\0', 0 };
				yield return new object [] { '英', true, false, false, '\0', 0, (KeyCode)'英' | KeyCode.ShiftMask, '\0', 0 };
				yield return new object [] { '英', true, true, false, '\0', 0, (KeyCode)'英' | KeyCode.ShiftMask | KeyCode.AltMask, '\0', 0 };
				yield return new object [] { '英', true, true, true, '\0', 0, (KeyCode)'英' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '\0', 0 };
				yield return new object [] { '+', false, false, false, 187, 26, (KeyCode)'+', 187, 26 };
				yield return new object [] { '*', true, false, false, 187, 26, (KeyCode)'*' | KeyCode.ShiftMask, 187, 26 };
				yield return new object [] { '+', true, true, false, 187, 26, (KeyCode)'+' | KeyCode.ShiftMask | KeyCode.AltMask, 187, 26 };
				yield return new object [] { '+', true, true, true, 187, 26, (KeyCode)'+' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 187, 26 };
				yield return new object [] { '1', false, false, false, '1', 2, KeyCode.D1, '1', 2 };
				yield return new object [] { '!', true, false, false, '1', 2, (KeyCode)'!' | KeyCode.ShiftMask, '1', 2 };
				yield return new object [] { '1', true, true, false, '1', 2, KeyCode.D1 | KeyCode.ShiftMask | KeyCode.AltMask, '1', 2 };
				yield return new object [] { '1', true, true, true, '1', 2, KeyCode.D1 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '1', 2 };
				yield return new object [] { '1', false, true, true, '1', 2, KeyCode.D1 | KeyCode.AltMask | KeyCode.CtrlMask, '1', 2 };
				yield return new object [] { '2', false, false, false, '2', 3, KeyCode.D2, '2', 3 };
				yield return new object [] { '"', true, false, false, '2', 3, (KeyCode)'"' | KeyCode.ShiftMask, '2', 3 };
				yield return new object [] { '2', true, true, false, '2', 3, KeyCode.D2 | KeyCode.ShiftMask | KeyCode.AltMask, '2', 3 };
				yield return new object [] { '2', true, true, true, '2', 3, KeyCode.D2 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '2', 3 };
				yield return new object [] { '@', false, true, true, '2', 3, (KeyCode)'@' | KeyCode.AltMask | KeyCode.CtrlMask, '2', 3 };
				yield return new object [] { '3', false, false, false, '3', 4, KeyCode.D3, '3', 4 };
				yield return new object [] { '#', true, false, false, '3', 4, (KeyCode)'#' | KeyCode.ShiftMask, '3', 4 };
				yield return new object [] { '3', true, true, false, '3', 4, KeyCode.D3 | KeyCode.ShiftMask | KeyCode.AltMask, '3', 4 };
				yield return new object [] { '3', true, true, true, '3', 4, KeyCode.D3 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '3', 4 };
				yield return new object [] { '£', false, true, true, '3', 4, (KeyCode)'£' | KeyCode.AltMask | KeyCode.CtrlMask, '3', 4 };
				yield return new object [] { '4', false, false, false, '4', 5, KeyCode.D4, '4', 5 };
				yield return new object [] { '$', true, false, false, '4', 5, (KeyCode)'$' | KeyCode.ShiftMask, '4', 5 };
				yield return new object [] { '4', true, true, false, '4', 5, KeyCode.D4 | KeyCode.ShiftMask | KeyCode.AltMask, '4', 5 };
				yield return new object [] { '4', true, true, true, '4', 5, KeyCode.D4 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '4', 5 };
				yield return new object [] { '§', false, true, true, '4', 5, (KeyCode)'§' | KeyCode.AltMask | KeyCode.CtrlMask, '4', 5 };
				yield return new object [] { '5', false, false, false, '5', 6, KeyCode.D5, '5', 6 };
				yield return new object [] { '%', true, false, false, '5', 6, (KeyCode)'%' | KeyCode.ShiftMask, '5', 6 };
				yield return new object [] { '5', true, true, false, '5', 6, KeyCode.D5 | KeyCode.ShiftMask | KeyCode.AltMask, '5', 6 };
				yield return new object [] { '5', true, true, true, '5', 6, KeyCode.D5 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '5', 6 };
				yield return new object [] { '€', false, true, true, '5', 6, (KeyCode)'€' | KeyCode.AltMask | KeyCode.CtrlMask, '5', 6 };
				yield return new object [] { '6', false, false, false, '6', 7, KeyCode.D6, '6', 7 };
				yield return new object [] { '&', true, false, false, '6', 7, (KeyCode)'&' | KeyCode.ShiftMask, '6', 7 };
				yield return new object [] { '6', true, true, false, '6', 7, KeyCode.D6 | KeyCode.ShiftMask | KeyCode.AltMask, '6', 7 };
				yield return new object [] { '6', true, true, true, '6', 7, KeyCode.D6 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '6', 7 };
				yield return new object [] { '6', false, true, true, '6', 7, KeyCode.D6 | KeyCode.AltMask | KeyCode.CtrlMask, '6', 7 };
				yield return new object [] { '7', false, false, false, '7', 8, KeyCode.D7, '7', 8 };
				yield return new object [] { '/', true, false, false, '7', 8, (KeyCode)'/' | KeyCode.ShiftMask, '7', 8 };
				yield return new object [] { '7', true, true, false, '7', 8, KeyCode.D7 | KeyCode.ShiftMask | KeyCode.AltMask, '7', 8 };
				yield return new object [] { '7', true, true, true, '7', 8, KeyCode.D7 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '7', 8 };
				yield return new object [] { '{', false, true, true, '7', 8, (KeyCode)'{' | KeyCode.AltMask | KeyCode.CtrlMask, '7', 8 };
				yield return new object [] { '8', false, false, false, '8', 9, KeyCode.D8, '8', 9 };
				yield return new object [] { '(', true, false, false, '8', 9, (KeyCode)'(' | KeyCode.ShiftMask, '8', 9 };
				yield return new object [] { '8', true, true, false, '8', 9, KeyCode.D8 | KeyCode.ShiftMask | KeyCode.AltMask, '8', 9 };
				yield return new object [] { '8', true, true, true, '8', 9, KeyCode.D8 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '8', 9 };
				yield return new object [] { '[', false, true, true, '8', 9, (KeyCode)'[' | KeyCode.AltMask | KeyCode.CtrlMask, '8', 9 };
				yield return new object [] { '9', false, false, false, '9', 10, KeyCode.D9, '9', 10 };
				yield return new object [] { ')', true, false, false, '9', 10, (KeyCode)')' | KeyCode.ShiftMask, '9', 10 };
				yield return new object [] { '9', true, true, false, '9', 10, KeyCode.D9 | KeyCode.ShiftMask | KeyCode.AltMask, '9', 10 };
				yield return new object [] { '9', true, true, true, '9', 10, KeyCode.D9 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '9', 10 };
				yield return new object [] { ']', false, true, true, '9', 10, (KeyCode)']' | KeyCode.AltMask | KeyCode.CtrlMask, '9', 10 };
				yield return new object [] { '0', false, false, false, '0', 11, KeyCode.D0, '0', 11 };
				yield return new object [] { '=', true, false, false, '0', 11, (KeyCode)'=' | KeyCode.ShiftMask, '0', 11 };
				yield return new object [] { '0', true, true, false, '0', 11, KeyCode.D0 | KeyCode.ShiftMask | KeyCode.AltMask, '0', 11 };
				yield return new object [] { '0', true, true, true, '0', 11, KeyCode.D0 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '0', 11 };
				yield return new object [] { '}', false, true, true, '0', 11, (KeyCode)'}' | KeyCode.AltMask | KeyCode.CtrlMask, '0', 11 };
				yield return new object [] { '\'', false, false, false, 219, 12, (KeyCode)'\'', 219, 12 };
				yield return new object [] { '?', true, false, false, 219, 12, (KeyCode)'?' | KeyCode.ShiftMask, 219, 12 };
				yield return new object [] { '\'', true, true, false, 219, 12, (KeyCode)'\'' | KeyCode.ShiftMask | KeyCode.AltMask, 219, 12 };
				yield return new object [] { '\'', true, true, true, 219, 12, (KeyCode)'\'' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 219, 12 };
				yield return new object [] { '«', false, false, false, 221, 13, (KeyCode)'«', 221, 13 };
				yield return new object [] { '»', true, false, false, 221, 13, (KeyCode)'»' | KeyCode.ShiftMask, 221, 13 };
				yield return new object [] { '«', true, true, false, 221, 13, (KeyCode)'«' | KeyCode.ShiftMask | KeyCode.AltMask, 221, 13 };
				yield return new object [] { '«', true, true, true, 221, 13, (KeyCode)'«' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 221, 13 };
				yield return new object [] { 'á', false, false, false, 'A', 30, (KeyCode)'á', 'A', 30 };
				yield return new object [] { 'Á', true, false, false, 'A', 30, (KeyCode)'Á' | KeyCode.ShiftMask, 'A', 30 };
				yield return new object [] { 'à', false, false, false, 'A', 30, (KeyCode)'à', 'A', 30 };
				yield return new object [] { 'À', true, false, false, 'A', 30, (KeyCode)'À' | KeyCode.ShiftMask, 'A', 30 };
				yield return new object [] { 'é', false, false, false, 'E', 18, (KeyCode)'é', 'E', 18 };
				yield return new object [] { 'É', true, false, false, 'E', 18, (KeyCode)'É' | KeyCode.ShiftMask, 'E', 18 };
				yield return new object [] { 'è', false, false, false, 'E', 18, (KeyCode)'è', 'E', 18 };
				yield return new object [] { 'È', true, false, false, 'E', 18, (KeyCode)'È' | KeyCode.ShiftMask, 'E', 18 };
				yield return new object [] { 'í', false, false, false, 'I', 23, (KeyCode)'í', 'I', 23 };
				yield return new object [] { 'Í', true, false, false, 'I', 23, (KeyCode)'Í' | KeyCode.ShiftMask, 'I', 23 };
				yield return new object [] { 'ì', false, false, false, 'I', 23, (KeyCode)'ì', 'I', 23 };
				yield return new object [] { 'Ì', true, false, false, 'I', 23, (KeyCode)'Ì' | KeyCode.ShiftMask, 'I', 23 };
				yield return new object [] { 'ó', false, false, false, 'O', 24, (KeyCode)'ó', 'O', 24 };
				yield return new object [] { 'Ó', true, false, false, 'O', 24, (KeyCode)'Ó' | KeyCode.ShiftMask, 'O', 24 };
				yield return new object [] { 'ò', false, false, false, 'O', 24, (KeyCode)'ò', 'O', 24 };
				yield return new object [] { 'Ò', true, false, false, 'O', 24, (KeyCode)'Ò' | KeyCode.ShiftMask, 'O', 24 };
				yield return new object [] { 'ú', false, false, false, 'U', 22, (KeyCode)'ú', 'U', 22 };
				yield return new object [] { 'Ú', true, false, false, 'U', 22, (KeyCode)'Ú' | KeyCode.ShiftMask, 'U', 22 };
				yield return new object [] { 'ù', false, false, false, 'U', 22, (KeyCode)'ù', 'U', 22 };
				yield return new object [] { 'Ù', true, false, false, 'U', 22, (KeyCode)'Ù' | KeyCode.ShiftMask, 'U', 22 };
				yield return new object [] { 'ö', false, false, false, 'O', 24, (KeyCode)'ö', 'O', 24 };
				yield return new object [] { 'Ö', true, false, false, 'O', 24, (KeyCode)'Ö' | KeyCode.ShiftMask, 'O', 24 };
				yield return new object [] { '<', false, false, false, 226, 86, (KeyCode)'<', 226, 86 };
				yield return new object [] { '>', true, false, false, 226, 86, (KeyCode)'>' | KeyCode.ShiftMask, 226, 86 };
				yield return new object [] { '<', true, true, false, 226, 86, (KeyCode)'<' | KeyCode.ShiftMask | KeyCode.AltMask, 226, 86 };
				yield return new object [] { '<', true, true, true, 226, 86, (KeyCode)'<' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 226, 86 };
				yield return new object [] { 'ç', false, false, false, 192, 39, (KeyCode)'ç', 192, 39 };
				yield return new object [] { 'Ç', true, false, false, 192, 39, (KeyCode)'Ç' | KeyCode.ShiftMask, 192, 39 };
				yield return new object [] { 'ç', true, true, false, 192, 39, (KeyCode)'ç' | KeyCode.ShiftMask | KeyCode.AltMask, 192, 39 };
				yield return new object [] { 'ç', true, true, true, 192, 39, (KeyCode)'ç' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 192, 39 };
				yield return new object [] { '¨', false, true, true, 187, 26, (KeyCode)'¨' | KeyCode.AltMask | KeyCode.CtrlMask, 187, 26 };
				yield return new object [] { KeyCode.PageUp, false, false, false, 33, 73, KeyCode.Null, 33, 73 };
				yield return new object [] { KeyCode.PageUp, true, false, false, 33, 73, KeyCode.Null | KeyCode.ShiftMask, 33, 73 };
				yield return new object [] { KeyCode.PageUp, true, true, false, 33, 73, KeyCode.Null | KeyCode.ShiftMask | KeyCode.AltMask, 33, 73 };
				yield return new object [] { KeyCode.PageUp, true, true, true, 33, 73, KeyCode.Null | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, 33, 73 };
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
	}
}