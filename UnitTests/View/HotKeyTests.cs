using System.Collections.Generic;
using System;
using Xunit;
using Xunit.Abstractions;
using UICatalog.Scenarios;
using System.Text;

namespace Terminal.Gui.ViewTests;

public class HotKeyTests {
	readonly ITestOutputHelper _output;

	public HotKeyTests (ITestOutputHelper output)
	{
		this._output = output;
	}

	[Fact]
	public void Defaults ()
	{
		var view = new View ();
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (Key.Null, view.HotKey);

		// Verify key bindings were set
		var commands = view.GetKeyBinding (Key.Null);
		Assert.Empty (commands);
	}

	[Theory]
	[InlineData (Key.A)]
	[InlineData ((Key)'a')]
	[InlineData ((Key)'A')]
	[InlineData (Key.A | Key.ShiftMask)]
	[InlineData (Key.D1)]
	[InlineData (Key.D1 | Key.ShiftMask)]
	[InlineData ((Key)'!')]
	public void Set_SupportsKeys (Key key)
	{
		var view = new View ();
		view.HotKey = key;
		Assert.Equal (key, view.HotKey);
	}

	[Fact]
	public void Set_SetsKeyBindings ()
	{
		var view = new View ();
		view.HotKey = Key.A;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (Key.A, view.HotKey);

		// Verify key bindings were set
		var commands = view.GetKeyBinding (Key.A);
		Assert.Contains (Command.Accept, commands);

		commands = view.GetKeyBinding (Key.A | Key.ShiftMask);
		Assert.Contains (Command.Accept, commands);

		commands = view.GetKeyBinding (Key.A | Key.AltMask);
		Assert.Contains (Command.Accept, commands);
	}

	[Fact]
	public void Set_Throws_If_Modifiers_Are_Included ()
	{
		var view = new View ();
		// A..Z must be naked
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.A | Key.AltMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.A | Key.CtrlMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.A | Key.ShiftMask | Key.AltMask | Key.CtrlMask);

		// All others must not have Ctrl or Alt
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.D1 | Key.AltMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.D1 | Key.CtrlMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.D1 | Key.ShiftMask | Key.AltMask | Key.CtrlMask);

		// Shift is ok (e.g. this is '!')
		view.HotKey = Key.D1 | Key.ShiftMask;
	}

	[Fact]
	public void Text_Change_Sets_HotKey ()
	{
		var view = new View () {
			HotKeySpecifier = new Rune ('_'),
			Text = "_Title!"
		};
		Assert.Equal (Key.T, view.HotKey);

		view.Text = "T_itle!";
		Assert.Equal (Key.I, view.HotKey);

		// BUGBUG: '!' should be supported. Line 968 of TextFormatter filters on char.IsLetterOrDigit 
		//view.Text = "Title_!";
		//Assert.Equal (Key.D1 | Key.ShiftMask, view.HotKey);
	}

}