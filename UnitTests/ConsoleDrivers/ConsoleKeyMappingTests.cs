using System;
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
}
