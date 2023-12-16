using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Terminal.Gui.ConsoleDrivers;
public class ConsoleKeyMappingTests {
	[Theory]
	[InlineData ((KeyCode)'a' | KeyCode.ShiftMask, ConsoleKey.A, KeyCode.A, 'A')]
	[InlineData ((KeyCode)'A', ConsoleKey.A, (KeyCode)'a', 'a')]
	[InlineData ((KeyCode)'à' | KeyCode.ShiftMask, ConsoleKey.A, (KeyCode)'À', 'À')]
	[InlineData ((KeyCode)'À', ConsoleKey.A, (KeyCode)'à', 'à')]
	[InlineData ((KeyCode)'ü' | KeyCode.ShiftMask, ConsoleKey.U, (KeyCode)'Ü', 'Ü')]
	[InlineData ((KeyCode)'Ü', ConsoleKey.U, (KeyCode)'ü', 'ü')]
	[InlineData ((KeyCode)'ý' | KeyCode.ShiftMask, ConsoleKey.Y, (KeyCode)'Ý', 'Ý')]
	[InlineData ((KeyCode)'Ý', ConsoleKey.Y, (KeyCode)'ý', 'ý')]
	[InlineData ((KeyCode)'!' | KeyCode.ShiftMask, ConsoleKey.D1, (KeyCode)'!', '!')]
	[InlineData (KeyCode.D1, ConsoleKey.D1, KeyCode.D1, '1')]
	[InlineData ((KeyCode)'/' | KeyCode.ShiftMask, ConsoleKey.D7, (KeyCode)'/', '/')]
	[InlineData (KeyCode.D7, ConsoleKey.D7, KeyCode.D7, '7')]
	[InlineData (KeyCode.PageDown | KeyCode.ShiftMask, ConsoleKey.PageDown, KeyCode.Null, '\0')]
	[InlineData (KeyCode.PageDown, ConsoleKey.PageDown, KeyCode.Null, '\0')]

	public void TestIfEqual (KeyCode key, ConsoleKey expectedConsoleKey, KeyCode expectedKey, char expectedChar)
	{
		var consoleKeyInfo = ConsoleKeyMapping.GetConsoleKeyFromKey (key);
		Assert.Equal (consoleKeyInfo.Key, expectedConsoleKey);
		Assert.Equal ((char)expectedKey, expectedChar);
		Assert.Equal (consoleKeyInfo.KeyChar, expectedChar);
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
				Assert.Equal (Key.ToString (expectedRemapping), Key.ToString (e.KeyCode));
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
