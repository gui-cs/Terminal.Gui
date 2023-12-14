using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.InputTests;

public class KeyEventArgsTests {
	readonly ITestOutputHelper _output;

	public KeyEventArgsTests (ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void Constructor_Default_ShouldSetKeyToUnknown ()
	{
		var eventArgs = new KeyEventArgs ();
		Assert.Equal (ConsoleDriverKey.Unknown, eventArgs.ConsoleDriverKey);
	}

	[Theory]
	[InlineData (ConsoleDriverKey.Enter)]
	[InlineData (ConsoleDriverKey.Esc)]
	[InlineData (ConsoleDriverKey.A)]
	public void Constructor_WithKey_ShouldSetCorrectKey (ConsoleDriverKey key)
	{
		var eventArgs = new KeyEventArgs (key);
		Assert.Equal (key, eventArgs.ConsoleDriverKey);
	}

	[Fact]
	public void HandledProperty_ShouldBeFalseByDefault ()
	{
		var eventArgs = new KeyEventArgs ();
		Assert.False (eventArgs.Handled);
	}

	[Theory]
	[InlineData (ConsoleDriverKey.A, true)]
	[InlineData (ConsoleDriverKey.A | ConsoleDriverKey.ShiftMask, true)]
	[InlineData (ConsoleDriverKey.F, true)]
	[InlineData (ConsoleDriverKey.F | ConsoleDriverKey.ShiftMask, true)]
	[InlineData (ConsoleDriverKey.A | ConsoleDriverKey.CtrlMask, false)]
	[InlineData (ConsoleDriverKey.A | ConsoleDriverKey.AltMask, false)]
	[InlineData (ConsoleDriverKey.D0, false)]
	[InlineData (ConsoleDriverKey.Esc, false)]
	[InlineData (ConsoleDriverKey.Tab, false)]
	public void IsLowerCaseAtoZ (ConsoleDriverKey key, bool expected)
	{
		var eventArgs = new KeyEventArgs (key);
		Assert.Equal (expected, eventArgs.IsLowerCaseAtoZ);
	}

	[Theory]
	[InlineData ((ConsoleDriverKey)'❿', '❿')]
	[InlineData ((ConsoleDriverKey)'☑', '☑')]
	[InlineData ((ConsoleDriverKey)'英', '英')]
	[InlineData ((ConsoleDriverKey)'{', '{')]
	[InlineData ((ConsoleDriverKey)'\'', '\'')]
	[InlineData ((ConsoleDriverKey)'\r', '\r')]
	[InlineData ((ConsoleDriverKey)'ó', 'ó')]
	[InlineData ((ConsoleDriverKey)'ó' | ConsoleDriverKey.ShiftMask, 'ó')]
	[InlineData ((ConsoleDriverKey)'Ó', 'Ó')]
	[InlineData ((ConsoleDriverKey)'ç' | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask, '\0')]
	[InlineData ((ConsoleDriverKey)'a', 97)] // 97 or Key.Space | Key.A
	[InlineData ((ConsoleDriverKey)'A', 97)] // 65 or equivalent to Key.A, but A-Z are mapped to lower case by drivers
	//[InlineData (Key.A, 97)] // 65 equivalent to (Key)'A', but A-Z are mapped to lower case by drivers
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.A, 65)]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.A, '\0')]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.A, '\0')]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.A, '\0')]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.A, '\0')]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.A, '\0')]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.A, '\0')]
	[InlineData ((ConsoleDriverKey)'z', 'z')]
	[InlineData ((ConsoleDriverKey)'Z', 'z')]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.Z, 'Z')]
	[InlineData ((ConsoleDriverKey)'1', '1')]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.D1, '1')]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.D1, '\0')]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.D1, '\0')]
	[InlineData (ConsoleDriverKey.F1, '\0')]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.F1, '\0')]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.F1, '\0')]
	[InlineData (ConsoleDriverKey.Enter, '\n')]
	[InlineData (ConsoleDriverKey.Tab, '\t')]
	[InlineData (ConsoleDriverKey.Esc, 0x1b)]
	[InlineData (ConsoleDriverKey.Space, ' ')]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Enter, '\0')]
	[InlineData (ConsoleDriverKey.Null, '\0')]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Null, '\0')]
	[InlineData (ConsoleDriverKey.CharMask, '\0')]
	[InlineData (ConsoleDriverKey.SpecialMask, '\0')]
	public void AsRune_ShouldReturnCorrectIntValue (ConsoleDriverKey key, Rune expected)
	{
		var eventArgs = new KeyEventArgs (key);
		Assert.Equal (expected, eventArgs.AsRune);
	}

	[Theory]
	[InlineData (ConsoleDriverKey.AltMask, true)]
	[InlineData (ConsoleDriverKey.A, false)]
	public void IsAlt_ShouldReturnCorrectValue (ConsoleDriverKey key, bool expected)
	{
		var eventArgs = new KeyEventArgs (key);
		Assert.Equal (expected, eventArgs.IsAlt);
	}

	// Similar tests for IsShift and IsCtrl
	[Fact]
	public void ToString_ShouldReturnReadableString ()
	{
		var eventArgs = new KeyEventArgs (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.A);
		Assert.Equal ("Ctrl+A", eventArgs.ToString ());
	}

	[Theory]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.A, '+', "Ctrl+A")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.B, '-', "Alt-B")]
	public void ToStringWithSeparator_ShouldReturnFormattedString (ConsoleDriverKey key, char separator, string expected)
	{
		Assert.Equal (expected, KeyEventArgs.ToString (key, (Rune)separator));
	}

	[Theory]
	[InlineData ((ConsoleDriverKey)'☑', "☑")]
	[InlineData ((ConsoleDriverKey)'英', "英")]
	[InlineData ((ConsoleDriverKey)'{', "{")]
	[InlineData ((ConsoleDriverKey)'\'', "\'")]
	[InlineData ((ConsoleDriverKey)'ó', "ó")]
	[InlineData ((ConsoleDriverKey)'ó' | ConsoleDriverKey.ShiftMask, "Shift+ó")] // is this right???
	[InlineData ((ConsoleDriverKey)'Ó', "Ó")]
	[InlineData ((ConsoleDriverKey)'ç' | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask, "Ctrl+Alt+Shift+ç")]
	[InlineData ((ConsoleDriverKey)'a', "a")] // 97 or Key.Space | Key.A
	[InlineData ((ConsoleDriverKey)'A', "a")] // 65 or equivalent to Key.A, but A-Z are mapped to lower case by drivers
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.A, "Shift+A")]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.A, "Ctrl+A")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.A, "Alt+A")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.A, "Ctrl+Shift+A")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.A, "Alt+Shift+A")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.A, "Ctrl+Alt+A")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.A, "Ctrl+Alt+Shift+A")]
	[InlineData ((ConsoleDriverKey)'z', "z")]
	[InlineData ((ConsoleDriverKey)'Z', "z")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.Z, "Shift+Z")]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Z, "Ctrl+Z")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.Z, "Alt+Z")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Z, "Ctrl+Shift+Z")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Z, "Alt+Shift+Z")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Z, "Ctrl+Alt+Z")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Z, "Ctrl+Alt+Shift+Z")]
	[InlineData ((ConsoleDriverKey)'1', "1")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.D1, "Shift+1")]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.D1, "Ctrl+1")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.D1, "Alt+1")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.D1, "Ctrl+Shift+1")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.D1, "Alt+Shift+1")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.D1, "Ctrl+Alt+1")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.D1, "Ctrl+Alt+Shift+1")]
	[InlineData (ConsoleDriverKey.F1, "F1")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.F1, "Shift+F1")]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.F1, "Ctrl+F1")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.F1, "Alt+F1")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.F1, "Ctrl+Shift+F1")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.F1, "Alt+Shift+F1")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.F1, "Ctrl+Alt+F1")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.F1, "Ctrl+Alt+Shift+F1")]
	[InlineData (ConsoleDriverKey.Enter, "Enter")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.Enter, "Shift+Enter")]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Enter, "Ctrl+Enter")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.Enter, "Alt+Enter")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Enter, "Ctrl+Shift+Enter")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Enter, "Alt+Shift+Enter")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Enter, "Ctrl+Alt+Enter")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Enter, "Ctrl+Alt+Shift+Enter")]
	[InlineData (ConsoleDriverKey.Delete, "Delete")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.Delete, "Shift+Delete")]
	[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Delete, "Ctrl+Delete")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.Delete, "Alt+Delete")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Delete, "Ctrl+Shift+Delete")]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Delete, "Alt+Shift+Delete")]
	[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Delete, "Ctrl+Alt+Delete")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Delete, "Ctrl+Alt+Shift+Delete")]
	//[InlineData (ConsoleDriverKey.CursorUp, "CursorUp")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CursorUp, "Shift+CursorUp")]
	//[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.CursorUp, "Ctrl+CursorUp")]
	//[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CursorUp, "Alt+CursorUp")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.CursorUp, "Ctrl+Shift+CursorUp")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.CursorUp, "Alt+Shift+CursorUp")]
	//[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.CursorUp, "Ctrl+Alt+CursorUp")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.CursorUp, "Ctrl+Alt+Shift+CursorUp")]
	//[InlineData (ConsoleDriverKey.Unknown, "Unknown")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.Unknown, "Shift+Unknown")]
	//[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Unknown, "Ctrl+Unknown")]
	//[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.Unknown, "Alt+Unknown")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Unknown, "Ctrl+Shift+Unknown")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Unknown, "Alt+Shift+Unknown")]
	//[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Unknown, "Ctrl+Alt+Unknown")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Unknown, "Ctrl+Alt+Shift+Unknown")]
	//[InlineData (ConsoleDriverKey.Null, "")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.Null, "Shift")]
	//[InlineData (ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Null, "Ctrl")]
	//[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.Null, "Alt")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Null, "Ctrl+Shift")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Null, "Alt+Shift")]
	//[InlineData (ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Null, "Ctrl+Alt")]
	//[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Null, "Ctrl+Alt+Shift")]
	//[InlineData (ConsoleDriverKey.CharMask, "CharMask")]
	//[InlineData (ConsoleDriverKey.SpecialMask, "Ctrl+Alt+Shift")]
	public void ToString_ShouldReturnFormattedString (ConsoleDriverKey key, string expected)
	{
		Assert.Equal (expected, KeyEventArgs.ToString (key));
	}
}