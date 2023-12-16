using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.InputTests;

public class KeyTests {
	readonly ITestOutputHelper _output;

	public KeyTests (ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void Constructor_Default_ShouldSetKeyToUnknown ()
	{
		var eventArgs = new Key ();
		Assert.Equal (KeyCode.Unknown, eventArgs.KeyCode);
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
		Key key = (Key)cdk;
		Assert.Equal (expected.ToString (), key.ToString ());

		// implicit
		key = cdk;
		Assert.Equal (expected.ToString (), key.ToString ());
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
	public void ToStringWithSeparator_ShouldReturnFormattedString (KeyCode key, char separator, string expected)
	{
		Assert.Equal (expected, Key.ToString (key, (Rune)separator));
	}

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
	[InlineData (KeyCode.Unknown, "Unknown")]
	[InlineData (KeyCode.ShiftMask | KeyCode.Unknown, "Shift+Unknown")]
	[InlineData (KeyCode.CtrlMask | KeyCode.Unknown, "Ctrl+Unknown")]
	[InlineData (KeyCode.AltMask | KeyCode.Unknown, "Alt+Unknown")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Unknown, "Ctrl+Shift+Unknown")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Unknown, "Alt+Shift+Unknown")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Unknown, "Ctrl+Alt+Unknown")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Unknown, "Ctrl+Alt+Shift+Unknown")]
	[InlineData (KeyCode.Null, "")]
	[InlineData (KeyCode.ShiftMask | KeyCode.Null, "Shift")]
	[InlineData (KeyCode.CtrlMask | KeyCode.Null, "Ctrl")]
	[InlineData (KeyCode.AltMask | KeyCode.Null, "Alt")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.Null, "Ctrl+Shift")]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.Null, "Alt+Shift")]
	[InlineData (KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Null, "Ctrl+Alt")]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask | KeyCode.AltMask | KeyCode.Null, "Ctrl+Alt+Shift")]
	[InlineData (KeyCode.CharMask, "CharMask")]
	[InlineData (KeyCode.SpecialMask, "Ctrl+Alt+Shift")]
	public void ToString_ShouldReturnFormattedString (KeyCode key, string expected)
	{
		Assert.Equal (expected, Key.ToString (key));
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
	public void TryParse_ShouldReturnFalse_On_InvalidKey (string keyString)
	{
		Assert.False (Key.TryParse (keyString, out var _));
	}
}