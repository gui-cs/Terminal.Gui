using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui.ConsoleDrivers;
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
		Assert.Equal (Key.Unknown, eventArgs.Key);
	}

	[Theory]
	[InlineData (Key.Enter)]
	[InlineData (Key.Esc)]
	[InlineData (Key.A)]
	public void Constructor_WithKey_ShouldSetCorrectKey (Key key)
	{
		var eventArgs = new KeyEventArgs (key);
		Assert.Equal (key, eventArgs.Key);
	}

	[Fact]
	public void HandledProperty_ShouldBeFalseByDefault ()
	{
		var eventArgs = new KeyEventArgs ();
		Assert.False (eventArgs.Handled);
	}

	[Theory]
	[InlineData (Key.A, true)]
	[InlineData (Key.A | Key.ShiftMask, true)]
	[InlineData (Key.F, true)]
	[InlineData (Key.F | Key.ShiftMask, true)]
	[InlineData (Key.A | Key.CtrlMask, false)]
	[InlineData (Key.A | Key.AltMask, false)]
	[InlineData (Key.D0, false)]
	[InlineData (Key.Esc, false)]
	[InlineData (Key.Tab, false)]
	public void IsAlpha (Key key, bool expected)
	{
		var eventArgs = new KeyEventArgs (key);
		Assert.Equal (expected, eventArgs.IsAlpha);
	}

	[Theory]
	[InlineData ((Key)'❿', '❿')]
	[InlineData ((Key)'☑', '☑')]
	[InlineData ((Key)'英', '英')]
	[InlineData ((Key)'{', '{')]
	[InlineData ((Key)'\'', '\'')]
	[InlineData ((Key)'\r', '\r')]
	[InlineData ((Key)'ó', 'ó')]
	[InlineData ((Key)'ó' | Key.ShiftMask, 'ó')]
	[InlineData ((Key)'Ó', 'Ó')]
	[InlineData ((Key)'ç' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, default)]

	[InlineData ((Key)'a', 97)] // 97 or Key.Space | Key.A
	[InlineData ((Key)'A', 97)] // 65 or equivalent to Key.A, but A-Z are mapped to lower case by drivers
	[InlineData (Key.A, 97)] // 65 equivalent to (Key)'A', but A-Z are mapped to lower case by drivers
	[InlineData (Key.ShiftMask | Key.A, 65)]
	[InlineData (Key.CtrlMask | Key.A, default)]
	[InlineData (Key.AltMask | Key.A, default)]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.A, default)]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.A, default)]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.A, default)]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.A, default)]
	
	[InlineData ((Key)'z', 'z')]
	[InlineData ((Key)'Z', 'z')]
	[InlineData (Key.ShiftMask | Key.Z, 'Z')]

	[InlineData ((Key)'1', '1')]
	[InlineData (Key.ShiftMask | Key.D1, '1')]
	[InlineData (Key.CtrlMask | Key.D1, default)]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.D1, default)]

	[InlineData (Key.F1, default)]
	[InlineData (Key.ShiftMask | Key.F1, default)]
	[InlineData (Key.CtrlMask | Key.F1, default)]

	[InlineData (Key.Enter, '\n')]
	[InlineData (Key.Tab, '\t')]
	[InlineData (Key.Esc, 0x1b)]
	[InlineData (Key.Space, ' ')]

	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.Enter, default)]

	[InlineData (Key.Null, default)]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.Null, default)]

	[InlineData (Key.CharMask, default)]
	[InlineData (Key.SpecialMask, default)]
	public void AsRune_ShouldReturnCorrectIntValue (Key key, Rune expected)
	{
		var eventArgs = new KeyEventArgs (key);
		Assert.Equal (expected, eventArgs.AsRune);
	}

	[Theory]
	[InlineData (Key.AltMask, true)]
	[InlineData (Key.A, false)]
	public void IsAlt_ShouldReturnCorrectValue (Key key, bool expected)
	{
		var eventArgs = new KeyEventArgs (key);
		Assert.Equal (expected, eventArgs.IsAlt);
	}

	// Similar tests for IsShift and IsCtrl
	[Fact]
	public void ToString_ShouldReturnReadableString ()
	{
		var eventArgs = new KeyEventArgs (Key.CtrlMask | Key.A);
		Assert.Equal ("Ctrl+A", eventArgs.ToString ());
	}

	[Theory]
	[InlineData (Key.CtrlMask | Key.A, '+', "Ctrl+A")]
	[InlineData (Key.AltMask | Key.B, '-', "Alt-B")]
	public void ToStringWithSeparator_ShouldReturnFormattedString (Key key, Rune separator, string expected)
	{
		Assert.Equal (expected, KeyEventArgs.ToString (key, separator));
	}

	[Theory]
	[InlineData ((Key)'☑', "☑")]
	[InlineData ((Key)'英', "英")]
	[InlineData ((Key)'{', "{")]
	[InlineData ((Key)'\'', "\'")]
	[InlineData ((Key)'ó', "ó")]
	[InlineData ((Key)'ó' | Key.ShiftMask, "Shift+ó")] // is this right???
	[InlineData ((Key)'Ó', "Ó")]
	[InlineData ((Key)'ç' | Key.ShiftMask | Key.AltMask | Key.CtrlMask, "Ctrl+Alt+Shift+ç")]

	[InlineData ((Key)'a', "a")] // 97 or Key.Space | Key.A
	[InlineData ((Key)'A', "a")] // 65 or equivalent to Key.A, but A-Z are mapped to lower case by drivers
	//[InlineData (Key.A, "a")] // 65 equivalent to (Key)'A', but A-Z are mapped to lower case by drivers
	[InlineData (Key.ShiftMask | Key.A, "Shift+A")]
	[InlineData (Key.CtrlMask | Key.A, "Ctrl+A")]
	[InlineData (Key.AltMask | Key.A, "Alt+A")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.A, "Ctrl+Shift+A")]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.A, "Alt+Shift+A")]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.A, "Ctrl+Alt+A")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.A, "Ctrl+Alt+Shift+A")]

	[InlineData ((Key)'z', "z")]
	[InlineData ((Key)'Z', "z")]
	[InlineData (Key.ShiftMask | Key.Z, "Shift+Z")]
	[InlineData (Key.CtrlMask | Key.Z, "Ctrl+Z")]
	[InlineData (Key.AltMask | Key.Z, "Alt+Z")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.Z, "Ctrl+Shift+Z")]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.Z, "Alt+Shift+Z")]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.Z, "Ctrl+Alt+Z")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.Z, "Ctrl+Alt+Shift+Z")]

	[InlineData ((Key)'1', "1")]
	[InlineData (Key.ShiftMask | Key.D1, "Shift+1")]
	[InlineData (Key.CtrlMask | Key.D1, "Ctrl+1")]
	[InlineData (Key.AltMask | Key.D1, "Alt+1")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.D1, "Ctrl+Shift+1")]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.D1, "Alt+Shift+1")]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.D1, "Ctrl+Alt+1")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.D1, "Ctrl+Alt+Shift+1")]

	[InlineData (Key.F1, "F1")]
	[InlineData (Key.ShiftMask | Key.F1, "Shift+F1")]
	[InlineData (Key.CtrlMask | Key.F1, "Ctrl+F1")]
	[InlineData (Key.AltMask | Key.F1, "Alt+F1")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.F1, "Ctrl+Shift+F1")]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.F1, "Alt+Shift+F1")]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.F1, "Ctrl+Alt+F1")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.F1, "Ctrl+Alt+Shift+F1")]

	[InlineData (Key.Enter, "Enter")]
	[InlineData (Key.ShiftMask | Key.Enter, "Shift+Enter")]
	[InlineData (Key.CtrlMask | Key.Enter, "Ctrl+Enter")]
	[InlineData (Key.AltMask | Key.Enter, "Alt+Enter")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.Enter, "Ctrl+Shift+Enter")]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.Enter, "Alt+Shift+Enter")]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.Enter, "Ctrl+Alt+Enter")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.Enter, "Ctrl+Alt+Shift+Enter")]

	[InlineData (Key.Delete, "Delete")]
	[InlineData (Key.ShiftMask | Key.Delete, "Shift+Delete")]
	[InlineData (Key.CtrlMask | Key.Delete, "Ctrl+Delete")]
	[InlineData (Key.AltMask | Key.Delete, "Alt+Delete")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.Delete, "Ctrl+Shift+Delete")]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.Delete, "Alt+Shift+Delete")]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.Delete, "Ctrl+Alt+Delete")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.Delete, "Ctrl+Alt+Shift+Delete")]

	[InlineData (Key.CursorUp, "CursorUp")]
	[InlineData (Key.ShiftMask | Key.CursorUp, "Shift+CursorUp")]
	[InlineData (Key.CtrlMask | Key.CursorUp, "Ctrl+CursorUp")]
	[InlineData (Key.AltMask | Key.CursorUp, "Alt+CursorUp")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.CursorUp, "Ctrl+Shift+CursorUp")]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.CursorUp, "Alt+Shift+CursorUp")]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.CursorUp, "Ctrl+Alt+CursorUp")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.CursorUp, "Ctrl+Alt+Shift+CursorUp")]

	[InlineData (Key.Unknown, "Unknown")]
	[InlineData (Key.ShiftMask | Key.Unknown, "Shift+Unknown")]
	[InlineData (Key.CtrlMask | Key.Unknown, "Ctrl+Unknown")]
	[InlineData (Key.AltMask | Key.Unknown, "Alt+Unknown")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.Unknown, "Ctrl+Shift+Unknown")]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.Unknown, "Alt+Shift+Unknown")]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.Unknown, "Ctrl+Alt+Unknown")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.Unknown, "Ctrl+Alt+Shift+Unknown")]
	
	[InlineData (Key.Null, "")]
	[InlineData (Key.ShiftMask | Key.Null, "Shift")]
	[InlineData (Key.CtrlMask | Key.Null, "Ctrl")]
	[InlineData (Key.AltMask | Key.Null, "Alt")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.Null, "Ctrl+Shift")]
	[InlineData (Key.ShiftMask | Key.AltMask | Key.Null, "Alt+Shift")]
	[InlineData (Key.AltMask | Key.CtrlMask | Key.Null, "Ctrl+Alt")]
	[InlineData (Key.ShiftMask | Key.CtrlMask | Key.AltMask | Key.Null, "Ctrl+Alt+Shift")]

	[InlineData (Key.CharMask, "CharMask")]
	[InlineData (Key.SpecialMask, "Ctrl+Alt+Shift")]

	public void ToString_ShouldReturnFormattedString (Key key, string expected)
	{
		Assert.Equal (expected, KeyEventArgs.ToString(key));
	}
}
