using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.InputTests;

public class KeyTests {
	readonly ITestOutputHelper _output;

	public KeyTests (ITestOutputHelper output) => _output = output;

	[Fact]
	public void Constructor_Default_ShouldSetKeyToNull ()
	{
		var eventArgs = new Key ();
		Assert.Equal (KeyCode.Null, eventArgs.KeyCode);
	}

	[Theory]
	[InlineData (KeyCode.Enter)]
	[InlineData (KeyCode.Esc)]
	[InlineData (KeyCode.A)]
	public void Constructor_WithKey_ShouldSetCorrectKey (KeyCode key)
	{
		var eventArgs = new Key (key);
		Assert.Equal (key, eventArgs.KeyCode);
	}

	[Theory]
	[InlineData ('a', KeyCode.A)]
	[InlineData ('A', KeyCode.A | KeyCode.ShiftMask)]
	[InlineData ('z', KeyCode.Z)]
	[InlineData ('Z', KeyCode.Z | KeyCode.ShiftMask)]
	[InlineData (' ', KeyCode.Space)]
	[InlineData ('1', KeyCode.D1)]
	[InlineData ('!', (KeyCode)'!')]
	[InlineData ('\n', KeyCode.Enter)]
	[InlineData ('\t', KeyCode.Tab)]
	[InlineData ('\r', (KeyCode)13)]
	[InlineData ('ó', (KeyCode)'ó')]
	[InlineData ('Ó', (KeyCode)'Ó')]
	[InlineData ('❿', (KeyCode)'❿')]
	[InlineData ('☑', (KeyCode)'☑')]
	[InlineData ('英', (KeyCode)'英')]
	[InlineData ('{', (KeyCode)'{')]
	[InlineData ('\'', (KeyCode)'\'')]
	[InlineData ('\xFFFF', (KeyCode)0xFFFF)]
	[InlineData ('\x0', (KeyCode)0x0)]
	public void Constructor_Char (char ch, KeyCode expectedKeyCode)
	{
		var key = new Key (ch);
		Assert.Equal (expectedKeyCode, key.KeyCode);
	}


	// TryParse
	[Theory]
	[InlineData ("a", KeyCode.A)]
	[InlineData ("Ctrl+A", KeyCode.A | KeyCode.CtrlMask)]
	[InlineData ("Alt+A", KeyCode.A | KeyCode.AltMask)]
	[InlineData ("Shift+A", KeyCode.A | KeyCode.ShiftMask)]
	[InlineData ("A", KeyCode.A | KeyCode.ShiftMask)]
	[InlineData ("â", (KeyCode)'â')]
	[InlineData ("Shift+â", (KeyCode)'â' | KeyCode.ShiftMask)]
	[InlineData ("Shift+Â", (KeyCode)'Â' | KeyCode.ShiftMask)]
	[InlineData ("Ctrl+Shift+CursorUp", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.CursorUp)]
	[InlineData ("Ctrl+Alt+Shift+CursorUp", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp)]
	[InlineData ("ctrl+alt+shift+cursorup", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp)]
	[InlineData ("CTRL+ALT+SHIFT+CURSORUP", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp)]
	[InlineData ("Ctrl+Alt+Shift+Delete", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Delete)]
	[InlineData ("Ctrl+Alt+Shift+Enter", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Enter)]
	[InlineData ("Tab", KeyCode.Tab)]
	[InlineData ("Shift+Tab", KeyCode.Tab | KeyCode.ShiftMask)]
	[InlineData ("Ctrl+Tab", KeyCode.Tab | KeyCode.CtrlMask)]
	[InlineData ("Alt+Tab", KeyCode.Tab | KeyCode.AltMask)]
	[InlineData ("Ctrl+Shift+Tab", KeyCode.Tab | KeyCode.ShiftMask | KeyCode.CtrlMask)]
	[InlineData ("Ctrl+Alt+Tab", KeyCode.Tab | KeyCode.AltMask | KeyCode.CtrlMask)]
	[InlineData ("", KeyCode.Null)]
	[InlineData (" ", KeyCode.Space)]
	[InlineData ("Shift+ ", KeyCode.Space | KeyCode.ShiftMask)]
	[InlineData ("Ctrl+ ", KeyCode.Space | KeyCode.CtrlMask)]
	[InlineData ("Alt+ ", KeyCode.Space | KeyCode.AltMask)]
	[InlineData ("F1", KeyCode.F1)]
	[InlineData ("0", KeyCode.D0)]
	[InlineData ("9", KeyCode.D9)]
	[InlineData ("D0", KeyCode.D0)]
	[InlineData ("65", KeyCode.A | KeyCode.ShiftMask)]
	[InlineData ("97", KeyCode.A)]
	[InlineData ("Shift", KeyCode.ShiftKey)]
	[InlineData ("Ctrl", KeyCode.CtrlKey)]
	[InlineData ("Ctrl-A", KeyCode.A | KeyCode.CtrlMask)]
	[InlineData ("Alt-A", KeyCode.A | KeyCode.AltMask)]
	[InlineData ("A-Ctrl", KeyCode.A | KeyCode.CtrlMask)]
	[InlineData ("Alt-A-Ctrl", KeyCode.A | KeyCode.CtrlMask | KeyCode.AltMask)]
	public void Constructor_String_Valid (string keyString, Key expected)
	{
		Key key = new Key (keyString);
		Assert.Equal (((Key)expected).ToString (), key.ToString ());
	}

	[Theory]
	[InlineData("Barf")]
	public void Constructor_String_Invalid_Throws (string keyString)
	{
		Assert.Throws<ArgumentException> (() => new Key (keyString));
	}

	[Theory]
	[InlineData ('a', KeyCode.A)]
	[InlineData ('A', KeyCode.A | KeyCode.ShiftMask)]
	[InlineData ('z', KeyCode.Z)]
	[InlineData ('Z', KeyCode.Z | KeyCode.ShiftMask)]
	[InlineData (' ', KeyCode.Space)]
	[InlineData ('1', KeyCode.D1)]
	[InlineData ('!', (KeyCode)'!')]
	[InlineData ('\n', KeyCode.Enter)]
	[InlineData ('\t', KeyCode.Tab)]
	[InlineData ('\r', (KeyCode)13)]
	[InlineData ('ó', (KeyCode)'ó')]
	[InlineData ('Ó', (KeyCode)'Ó')]
	[InlineData ('❿', (KeyCode)'❿')]
	[InlineData ('☑', (KeyCode)'☑')]
	[InlineData ('英', (KeyCode)'英')]
	[InlineData ('{', (KeyCode)'{')]
	[InlineData ('\'', (KeyCode)'\'')]
	[InlineData ('\xFFFF', (KeyCode)0xFFFF)]
	[InlineData ('\x0', (KeyCode)0x0)]
	public void Cast_Char_To_Key (char ch, KeyCode expectedKeyCode)
	{
		var key = (Key)ch;
		Assert.Equal (expectedKeyCode, key.KeyCode);
	}

	// string cast operators
	[Fact]
	public void Cast_String_To_Key ()
	{
		var key = (Key)"Ctrl+Q";
		Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, key.KeyCode);
	}

	[Fact]
	public void Cast_Key_ToString ()
	{
		var str = (string)Key.Q.WithCtrl;
		Assert.Equal ("Ctrl+Q", str);
	}

	// IsValid
	[Theory]
	[InlineData (KeyCode.A, true)]
	[InlineData (KeyCode.B, true)]
	[InlineData (KeyCode.F1 | KeyCode.ShiftMask, true)]
	[InlineData (KeyCode.Null, false)]
	[InlineData (KeyCode.ShiftMask, false)]
	[InlineData (KeyCode.CtrlMask, false)]
	[InlineData (KeyCode.AltMask, false)]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask, false)]
	public void IsValid (Key key, bool expected) => Assert.Equal (expected, key.IsValid);

	[Fact]
	public void HandledProperty_ShouldBeFalseByDefault ()
	{
		var eventArgs = new Key ();
		Assert.False (eventArgs.Handled);
	}

	[Theory]
	[InlineData (KeyCode.Enter, KeyCode.Enter)]
	[InlineData (KeyCode.Esc, KeyCode.Esc)]
	[InlineData (KeyCode.A, (KeyCode)'a')]
	[InlineData (KeyCode.A | KeyCode.ShiftMask, KeyCode.A | KeyCode.ShiftMask)]
	[InlineData (KeyCode.Z, (KeyCode)'z')]
	[InlineData (KeyCode.Space, KeyCode.Space)]
	public void Cast_KeyCode_To_Key (KeyCode cdk, Key expected)
	{
		// explicit
		var key = (Key)cdk;
		Assert.Equal (expected.ToString (), key.ToString ());

		// implicit
		key = cdk;
		Assert.Equal (expected.ToString (), key.ToString ());
	}

	[Fact]
	public void Standard_Keys_Always_New ()
	{
		// Make two local keys, and grab Key.A, which is a reference to a singleton.
		Key aKey1 = Key.A;
		Key aKey2 = Key.A;

		// Assert the starting state that is expected
		Assert.False (aKey1.Handled);
		Assert.False (aKey2.Handled);
		Assert.False (Key.A.Handled);

		// Now set Handled true on one of our local keys
		aKey1.Handled = true;

		// Assert the newly-expected case
		// The last two assertions will fail, because we have actually modified a singleton
		Assert.True (aKey1.Handled);
		Assert.False (aKey2.Handled);
		Assert.False (Key.A.Handled);
	}

	[Theory]
	[InlineData ((KeyCode)'a', true)]
	[InlineData ((KeyCode)'a' | KeyCode.ShiftMask, true)]
	[InlineData (KeyCode.A, true)]
	[InlineData (KeyCode.A | KeyCode.ShiftMask, true)]
	[InlineData (KeyCode.F, true)]
	[InlineData (KeyCode.F | KeyCode.ShiftMask, true)]
	// these have alt or ctrl modifiers or are not a..z
	[InlineData (KeyCode.A | KeyCode.CtrlMask, false)]
	[InlineData (KeyCode.A | KeyCode.AltMask, false)]
	[InlineData (KeyCode.D0, false)]
	[InlineData (KeyCode.Esc, false)]
	[InlineData (KeyCode.Tab, false)]
	public void IsKeyCodeAtoZ (KeyCode key, bool expected)
	{
		var eventArgs = new Key (key);
		Assert.Equal (expected, eventArgs.IsKeyCodeAtoZ);
	}

	[Theory]
	[InlineData ((KeyCode)'❿', '❿')]
	[InlineData ((KeyCode)'☑', '☑')]
	[InlineData ((KeyCode)'英', '英')]
	[InlineData ((KeyCode)'{', '{')]
	[InlineData ((KeyCode)'\'', '\'')]
	[InlineData ((KeyCode)'\r', '\r')]
	[InlineData ((KeyCode)'ó', 'ó')]
	[InlineData ((KeyCode)'ó' | KeyCode.ShiftMask, 'ó')]
	[InlineData ((KeyCode)'Ó', 'Ó')]
	[InlineData ((KeyCode)'ç' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, '\0')]
	[InlineData ((KeyCode)'a', 97)] // 97 or Key.Space | Key.A
	[InlineData ((KeyCode)'A', 97)] // 65 or equivalent to Key.A, but A-Z are mapped to lower case by drivers
					//[InlineData (Key.A, 97)] // 65 equivalent to (Key)'A', but A-Z are mapped to lower case by drivers
	[InlineData (KeyCode.ShiftMask | KeyCode.A, 65)]
	[InlineData (KeyCode.CtrlMask | KeyCode.A, '\0')]
	[InlineData (KeyCode.AltMask | KeyCode.A, '\0')]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.A, '\0')]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.A, '\0')]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.A, '\0')]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.A, '\0')]
	[InlineData ((KeyCode)'z', 'z')]
	[InlineData ((KeyCode)'Z', 'z')]
	[InlineData (KeyCode.ShiftMask | KeyCode.Z, 'Z')]
	[InlineData ((KeyCode)'1', '1')]
	[InlineData (KeyCode.ShiftMask | KeyCode.D1, '1')]
	[InlineData (KeyCode.CtrlMask | KeyCode.D1, '\0')]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.D1, '\0')]
	[InlineData (KeyCode.F1, '\0')]
	[InlineData (KeyCode.ShiftMask | KeyCode.F1, '\0')]
	[InlineData (KeyCode.CtrlMask | KeyCode.F1, '\0')]
	[InlineData (KeyCode.Enter, '\n')]
	[InlineData (KeyCode.Tab, '\t')]
	[InlineData (KeyCode.Esc, 0x1b)]
	[InlineData (KeyCode.Space, ' ')]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Enter, '\0')]
	[InlineData (KeyCode.Null, '\0')]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Null, '\0')]
	[InlineData (KeyCode.CharMask, '\0')]
	[InlineData (KeyCode.SpecialMask, '\0')]
	public void AsRune_ShouldReturnCorrectIntValue (KeyCode key, Rune expected)
	{
		var eventArgs = new Key (key);
		Assert.Equal (expected, eventArgs.AsRune);
	}

	[Theory]
	[InlineData (KeyCode.AltMask, true)]
	[InlineData (KeyCode.A, false)]
	public void IsAlt_ShouldReturnCorrectValue (KeyCode key, bool expected)
	{
		var eventArgs = new Key (key);
		Assert.Equal (expected, eventArgs.IsAlt);
	}

	[Fact]
	public void WithShift_ShouldReturnCorrectValue ()
	{
		var a = new Key (KeyCode.A);
		Assert.Equal (KeyCode.A | KeyCode.ShiftMask, a.WithShift);

		var CAD = Key.Delete.WithCtrl.WithAlt;
		Assert.Equal (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.AltMask, CAD);
	}

	[Fact]
	public void NoShift_ShouldReturnCorrectValue ()
	{
		var CAD = Key.Delete.WithCtrl.WithAlt;
		Assert.Equal (KeyCode.Delete | KeyCode.CtrlMask | KeyCode.AltMask, CAD);

		Assert.Equal (KeyCode.Delete | KeyCode.AltMask, CAD.NoCtrl);

		var a = new Key (KeyCode.A).WithCtrl.WithAlt.WithShift;
		Assert.Equal (KeyCode.A, a.NoCtrl.NoShift.NoAlt);
		Assert.Equal (KeyCode.A, a.NoAlt.NoShift.NoCtrl);
		Assert.Equal (KeyCode.A, a.NoAlt.NoShift.NoCtrl.NoCtrl.NoAlt.NoShift);

		Assert.Equal (Key.Delete, Key.Delete.WithCtrl.NoCtrl);

		Assert.Equal ((KeyCode)Key.Delete | KeyCode.CtrlMask, Key.Delete.NoCtrl.WithCtrl);
	}

	[Fact]
	public void Standard_Keys_Should_Equal_KeyCode ()
	{
		Assert.Equal (KeyCode.A, Key.A);
		Assert.Equal (KeyCode.Delete, Key.Delete);
	}

	// TODO: Create equality operator for KeyCode
	//Assert.Equal (KeyCode.Delete, Key.Delete);

	// Similar tests for IsShift and IsCtrl
	[Fact]
	public void ToString_ShouldReturnReadableString ()
	{
		var eventArgs = new Key (KeyCode.CtrlMask | KeyCode.A);
		Assert.Equal ("Ctrl+A", eventArgs.ToString ());
	}

	[Theory]
	[InlineData (KeyCode.CtrlMask | KeyCode.A, '+', "Ctrl+A")]
	[InlineData (KeyCode.AltMask | KeyCode.B, '-', "Alt-B")]
	public void ToStringWithSeparator_ShouldReturnFormattedString (KeyCode key, char separator, string expected) => Assert.Equal (expected, Key.ToString (key, (Rune)separator));

	[Theory]
	[InlineData ((KeyCode)'☑', "☑")]
	//[InlineData ((ConsoleDriverKey)'英', "英")]
	//[InlineData ((ConsoleDriverKey)'{', "{")]
	[InlineData ((KeyCode)'\'', "\'")]
	[InlineData ((KeyCode)'ó', "ó")]
	[InlineData ((KeyCode)'ó' | KeyCode.ShiftMask, "Shift+ó")] // is this right???
	[InlineData ((KeyCode)'Ó', "Ó")]
	[InlineData ((KeyCode)'ç' | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, "Ctrl+Alt+Shift+ç")]
	[InlineData ((KeyCode)'a', "a")] // 97 or Key.Space | Key.A
	[InlineData ((KeyCode)'A', "a")] // 65 or equivalent to Key.A, but A-Z are mapped to lower case by drivers
	[InlineData (KeyCode.ShiftMask | KeyCode.A, "A")]
	[InlineData ((KeyCode)'a' | KeyCode.ShiftMask, "A")]
	[InlineData (KeyCode.CtrlMask | KeyCode.A, "Ctrl+A")]
	[InlineData (KeyCode.AltMask | KeyCode.A, "Alt+A")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.A, "Ctrl+Shift+A")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.A, "Alt+Shift+A")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.A, "Ctrl+Alt+A")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.A, "Ctrl+Alt+Shift+A")]
	[InlineData (KeyCode.ShiftMask | KeyCode.Z, "Z")]
	[InlineData (KeyCode.CtrlMask | KeyCode.Z, "Ctrl+Z")]
	[InlineData (KeyCode.AltMask | KeyCode.Z, "Alt+Z")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Z, "Ctrl+Shift+Z")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Z, "Alt+Shift+Z")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Z, "Ctrl+Alt+Z")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Z, "Ctrl+Alt+Shift+Z")]
	[InlineData ((KeyCode)'1', "1")]
	[InlineData (KeyCode.ShiftMask | KeyCode.D1, "Shift+1")]
	[InlineData (KeyCode.CtrlMask | KeyCode.D1, "Ctrl+1")]
	[InlineData (KeyCode.AltMask | KeyCode.D1, "Alt+1")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.D1, "Ctrl+Shift+1")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.D1, "Alt+Shift+1")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.D1, "Ctrl+Alt+1")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.D1, "Ctrl+Alt+Shift+1")]
	[InlineData (KeyCode.F1, "F1")]
	[InlineData (KeyCode.ShiftMask | KeyCode.F1, "Shift+F1")]
	[InlineData (KeyCode.CtrlMask | KeyCode.F1, "Ctrl+F1")]
	[InlineData (KeyCode.AltMask | KeyCode.F1, "Alt+F1")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.F1, "Ctrl+Shift+F1")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.F1, "Alt+Shift+F1")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.F1, "Ctrl+Alt+F1")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.F1, "Ctrl+Alt+Shift+F1")]
	[InlineData (KeyCode.Enter, "Enter")]
	[InlineData (KeyCode.ShiftMask | KeyCode.Enter, "Shift+Enter")]
	[InlineData (KeyCode.CtrlMask | KeyCode.Enter, "Ctrl+Enter")]
	[InlineData (KeyCode.AltMask | KeyCode.Enter, "Alt+Enter")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Enter, "Ctrl+Shift+Enter")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Enter, "Alt+Shift+Enter")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Enter, "Ctrl+Alt+Enter")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Enter, "Ctrl+Alt+Shift+Enter")]
	[InlineData (KeyCode.Delete, "Delete")]
	[InlineData (KeyCode.ShiftMask | KeyCode.Delete, "Shift+Delete")]
	[InlineData (KeyCode.CtrlMask | KeyCode.Delete, "Ctrl+Delete")]
	[InlineData (KeyCode.AltMask | KeyCode.Delete, "Alt+Delete")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Delete, "Ctrl+Shift+Delete")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Delete, "Alt+Shift+Delete")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Delete, "Ctrl+Alt+Delete")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Delete, "Ctrl+Alt+Shift+Delete")]
	[InlineData (KeyCode.CursorUp, "CursorUp")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CursorUp, "Shift+CursorUp")]
	[InlineData (KeyCode.CtrlMask | KeyCode.CursorUp, "Ctrl+CursorUp")]
	[InlineData (KeyCode.AltMask | KeyCode.CursorUp, "Alt+CursorUp")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.CursorUp, "Ctrl+Shift+CursorUp")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CursorUp, "Alt+Shift+CursorUp")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.CursorUp, "Ctrl+Alt+CursorUp")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp, "Ctrl+Alt+Shift+CursorUp")]
	[InlineData (KeyCode.Null, "Null")]
	[InlineData (KeyCode.ShiftMask | KeyCode.Null, "Null")]
	[InlineData (KeyCode.CtrlMask | KeyCode.Null, "Null")]
	[InlineData (KeyCode.AltMask | KeyCode.Null, "Null")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Null, "Null")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Null, "Null")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Null, "Null")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Null, "Null")]
	[InlineData (KeyCode.AltKey, "AltKey")]
	[InlineData (KeyCode.CtrlKey, "CtrlKey")]
	[InlineData (KeyCode.ShiftKey, "ShiftKey")]
	[InlineData (KeyCode.CharMask, "CharMask")]
	[InlineData (KeyCode.SpecialMask, "Ctrl+Alt+Shift")]
	public void ToString_ShouldReturnFormattedString (KeyCode key, string expected) => Assert.Equal (expected, Key.ToString (key));

	// TryParse
	[Theory]
	[InlineData ("a", KeyCode.A)]
	[InlineData ("Ctrl+A", KeyCode.A | KeyCode.CtrlMask)]
	[InlineData ("Alt+A", KeyCode.A | KeyCode.AltMask)]
	[InlineData ("Shift+A", KeyCode.A | KeyCode.ShiftMask)]
	[InlineData ("A", KeyCode.A | KeyCode.ShiftMask)]
	[InlineData ("â", (KeyCode)'â')]
	[InlineData ("Shift+â", (KeyCode)'â' | KeyCode.ShiftMask)]
	[InlineData ("Shift+Â", (KeyCode)'Â' | KeyCode.ShiftMask)]
	[InlineData ("Ctrl+Shift+CursorUp", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.CursorUp)]
	[InlineData ("Ctrl+Alt+Shift+CursorUp", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp)]
	[InlineData ("ctrl+alt+shift+cursorup", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp)]
	[InlineData ("CTRL+ALT+SHIFT+CURSORUP", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.CursorUp)]
	[InlineData ("Ctrl+Alt+Shift+Delete", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Delete)]
	[InlineData ("Ctrl+Alt+Shift+Enter", KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Enter)]
	[InlineData ("Tab", KeyCode.Tab)]
	[InlineData ("Shift+Tab", KeyCode.Tab | KeyCode.ShiftMask)]
	[InlineData ("Ctrl+Tab", KeyCode.Tab | KeyCode.CtrlMask)]
	[InlineData ("Alt+Tab", KeyCode.Tab | KeyCode.AltMask)]
	[InlineData ("Ctrl+Shift+Tab", KeyCode.Tab | KeyCode.ShiftMask | KeyCode.CtrlMask)]
	[InlineData ("Ctrl+Alt+Tab", KeyCode.Tab | KeyCode.AltMask | KeyCode.CtrlMask)]
	[InlineData ("", KeyCode.Null)]
	[InlineData (" ", KeyCode.Space)]
	[InlineData ("Shift+ ", KeyCode.Space | KeyCode.ShiftMask)]
	[InlineData ("Ctrl+ ", KeyCode.Space | KeyCode.CtrlMask)]
	[InlineData ("Alt+ ", KeyCode.Space | KeyCode.AltMask)]
	[InlineData ("F1", KeyCode.F1)]
	[InlineData ("0", KeyCode.D0)]
	[InlineData ("9", KeyCode.D9)]
	[InlineData ("D0", KeyCode.D0)]
	[InlineData ("65", KeyCode.A | KeyCode.ShiftMask)]
	[InlineData ("97", KeyCode.A)]
	[InlineData ("Shift", KeyCode.ShiftKey)]
	[InlineData ("Ctrl", KeyCode.CtrlKey)]
	[InlineData ("Ctrl-A", KeyCode.A | KeyCode.CtrlMask)]
	[InlineData ("Alt-A", KeyCode.A | KeyCode.AltMask)]
	[InlineData ("A-Ctrl", KeyCode.A | KeyCode.CtrlMask)]
	[InlineData ("Alt-A-Ctrl", KeyCode.A | KeyCode.CtrlMask | KeyCode.AltMask)]
	public void TryParse_ShouldReturnTrue_WhenValidKey (string keyString, Key expected)
	{
		Key key;
		Assert.True (Key.TryParse (keyString, out key));
		Assert.Equal (((Key)expected).ToString (), key.ToString ());
	}

	[Theory]
	[InlineData ("aa")]
	[InlineData ("-1")]
	[InlineData ("Crtl-A")]
	[InlineData ("Ctrl=A")]
	[InlineData ("Crtl")]
	[InlineData ("99a")]
	[InlineData ("a99")]
	[InlineData ("#99")]
	[InlineData ("x99")]
	[InlineData ("0x99")]
	[InlineData ("Ctrl-Ctrl")]
	public void TryParse_ShouldReturnFalse_On_InvalidKey (string keyString) => Assert.False (Key.TryParse (keyString, out var _));

	[Theory]
	[InlineData (KeyCode.ShiftKey, KeyCode.ShiftMask, true)]
	[InlineData (KeyCode.ShiftKey | KeyCode.A, KeyCode.ShiftMask | KeyCode.A, true)]
	public void IsShift_With_Wrong_And_Right_ShiftMask_NoShift (KeyCode wrongKeyCode, KeyCode rightKeyCode, bool isShift)
	{
		Assert.NotEqual (((Key)wrongKeyCode).IsShift, isShift);
		Assert.NotEqual ((Key)(wrongKeyCode & ~KeyCode.ShiftKey), ((Key)wrongKeyCode).NoShift);
		Assert.Equal (((Key)rightKeyCode).IsShift, isShift);
		Assert.Equal ((Key)(rightKeyCode & ~KeyCode.ShiftMask), ((Key)rightKeyCode).NoShift);
	}

	[Theory]
	[InlineData (KeyCode.AltKey, KeyCode.AltMask, true)]
	[InlineData (KeyCode.AltKey | KeyCode.A, KeyCode.AltMask | KeyCode.A, true)]
	public void IsAlt_With_Wrong_And_Right_AltMask_NoAlt (KeyCode wrongKeyCode, KeyCode rightKeyCode, bool isAlt)
	{
		Assert.NotEqual (((Key)wrongKeyCode).IsAlt, isAlt);
		Assert.NotEqual ((Key)(wrongKeyCode & ~KeyCode.AltKey), ((Key)wrongKeyCode).NoAlt);
		Assert.Equal (((Key)rightKeyCode).IsAlt, isAlt);
		Assert.Equal ((Key)(rightKeyCode & ~KeyCode.AltMask), ((Key)rightKeyCode).NoAlt);
	}

	[Theory]
	[InlineData (KeyCode.CtrlKey, KeyCode.CtrlMask, true)]
	[InlineData (KeyCode.CtrlKey | KeyCode.A, KeyCode.CtrlMask | KeyCode.A, true)]
	public void IsCtrl_With_Wrong_And_Right_CtrlMask_NoCtrl (KeyCode wrongKeyCode, KeyCode rightKeyCode, bool isCtrl)
	{
		Assert.NotEqual (((Key)wrongKeyCode).IsCtrl, isCtrl);
		Assert.NotEqual ((Key)(wrongKeyCode & ~KeyCode.CtrlKey), ((Key)wrongKeyCode).NoCtrl);
		Assert.Equal (((Key)rightKeyCode).IsCtrl, isCtrl);
		Assert.Equal ((Key)(rightKeyCode & ~KeyCode.CtrlMask), ((Key)rightKeyCode).NoCtrl);
	}

	[Theory]
	[InlineData (KeyCode.ShiftMask, true, false, false)]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask, true, true, false)]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask, true, true, true)]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask, true, false, true)]
	[InlineData (KeyCode.AltMask, false, true, false)]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask, false, true, true)]
	[InlineData (KeyCode.CtrlMask, false, false, true)]
	public void IsShift_IsAlt_IsCtrl (KeyCode keyCode, bool isShift, bool isAlt, bool isCtrl)
	{
		Assert.Equal (((Key)keyCode).IsShift, isShift);
		Assert.Equal (((Key)keyCode).IsAlt, isAlt);
		Assert.Equal (((Key)keyCode).IsCtrl, isCtrl);
	}
}