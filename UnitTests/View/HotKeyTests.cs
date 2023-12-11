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
	[InlineData (Key.A | Key.ShiftMask)]
	[InlineData (Key.D1)]
	[InlineData (Key.D1 | Key.ShiftMask)]
	[InlineData ((Key)'!')]
	[InlineData ((Key)'х')]  // Cyrillic x
	[InlineData ((Key)'你')] // Chinese ni
	public void Set_SupportsKeys (Key key)
	{
		var view = new View ();
		view.HotKey = key;
		Assert.Equal (key, view.HotKey);
	}

	[Theory]
	[InlineData (Key.A)]
	[InlineData (Key.A | Key.ShiftMask)]
	[InlineData (Key.D1)]
	[InlineData (Key.D1 | Key.ShiftMask)] // '!'
	[InlineData ((Key)'х')]  // Cyrillic x
	[InlineData ((Key)'你')] // Chinese ni
	public void Set_SetsKeyBindings (Key key)
	{
		var view = new View ();
		view.HotKey = key;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (key, view.HotKey);

		// Verify key bindings were set

		// As passed
		var commands = view.GetKeyBinding (key);
		Assert.Contains (Command.Accept, commands);
		commands = view.GetKeyBinding (key | Key.AltMask);
		Assert.Contains (Command.Accept, commands);

		var baseKey = key & ~Key.ShiftMask;
		// If A...Z, with and without shift
		if (baseKey is >= Key.A and <= Key.Z) {
			commands = view.GetKeyBinding (key | Key.ShiftMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.GetKeyBinding (key & ~Key.ShiftMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.GetKeyBinding (key | Key.AltMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.GetKeyBinding (key & ~Key.ShiftMask | Key.AltMask);
			Assert.Contains (Command.Accept, commands);
		} else {
			// Non A..Z keys should not have shift bindings
			if (key.HasFlag (Key.ShiftMask)) {
				commands = view.GetKeyBinding (key & ~Key.ShiftMask);
				Assert.Empty (commands);
			} else {
				commands = view.GetKeyBinding (key | Key.ShiftMask);
				Assert.Empty (commands);
			}
		}
	}

	[Fact]
	public void Set_RemovesOldKeyBindings ()
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

		commands = view.GetKeyBinding (Key.A | Key.ShiftMask | Key.AltMask);
		Assert.Contains (Command.Accept, commands);

		// Now set again
		view.HotKey = Key.B;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (Key.B, view.HotKey);

		commands = view.GetKeyBinding (Key.A);
		Assert.DoesNotContain (Command.Accept, commands);

		commands = view.GetKeyBinding (Key.A | Key.ShiftMask);
		Assert.DoesNotContain (Command.Accept, commands);

		commands = view.GetKeyBinding (Key.A | Key.AltMask);
		Assert.DoesNotContain (Command.Accept, commands);

		commands = view.GetKeyBinding (Key.A | Key.ShiftMask | Key.AltMask);
		Assert.DoesNotContain (Command.Accept, commands);
	}

	[Fact]
	public void Set_Throws_If_Modifiers_Are_Included ()
	{
		var view = new View ();
		// A..Z must be naked (Alt is assumed)
		view.HotKey = Key.A | Key.AltMask;
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.A | Key.CtrlMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.A | Key.ShiftMask | Key.AltMask | Key.CtrlMask);

		// All others must not have Ctrl (Alt is assumed)
		view.HotKey = Key.D1 | Key.AltMask;
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.D1 | Key.CtrlMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = Key.D1 | Key.ShiftMask | Key.AltMask | Key.CtrlMask);

		// Shift is ok (e.g. this is '!')
		view.HotKey = Key.D1 | Key.ShiftMask;
	}

	[Theory]
	[InlineData (Key.Delete)]
	[InlineData (Key.Backspace)]
	[InlineData (Key.Tab)]
	[InlineData (Key.Enter)]
	[InlineData (Key.Esc)]
	[InlineData (Key.Space)]
	[InlineData (Key.CursorLeft)]
	[InlineData (Key.F1)]
	public void Set_Throws_With_Invalid_HotKeys (Key key)
	{
		var view = new View ();
		Assert.Throws<ArgumentException> (() => view.HotKey = key);
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

	[Fact]
	public void Text_Sets_HotKey_To_KeyNull ()
	{
		var view = new View () {
			HotKeySpecifier = (Rune)'^',
			Text = "^Test"
		};

		Assert.Equal ("^Test", view.Text);
		Assert.Equal (Key.T, view.HotKey);

		view.Text = string.Empty;
		Assert.Equal ("", view.Text);
		Assert.Equal (Key.Null, view.HotKey);

		view.Text = "Te^st";
		Assert.Equal ("Te^st", view.Text);
		Assert.Equal (Key.S, view.HotKey);
	}

	// BUGBUG: Default command is currently Accept. Should be Default.
	[Theory]
	[InlineData (Key.Null, true)] // non-shift
	[InlineData (Key.ShiftMask, true)]
	[InlineData (Key.AltMask, true)]
	[InlineData (Key.ShiftMask | Key.AltMask, true)]
	[InlineData (Key.CtrlMask, false)]
	[InlineData (Key.ShiftMask | Key.CtrlMask, false)]
	public void KeyPress_Runs_Default_HotKey_Command (Key mask, bool expected)
	{
		var view = new View () {
			HotKeySpecifier = (Rune)'^',
			Text = "^Test"
		};
		view.CanFocus = true;
		Assert.False (view.HasFocus);
		view.ProcessKeyDown (new (Key.T | mask));
		Assert.Equal (expected, view.HasFocus);
	}

	// BUGBUG: Default command is currently Accept. Should be Default.
	[Fact]
	public void KeyPress_Runs_Default_HotKey_Command_With_SuperView ()
	{
		var view = new View () {
			HotKeySpecifier = (Rune)'^',
			Text = "^Test"
		};

		var superView = new View ();
		superView.Add (view);

		view.CanFocus = true;
		Assert.False (view.HasFocus);

		superView.ProcessKeyDown (new (Key.T));
		Assert.True (view.HasFocus);


	}



	public class HotKeyTestView : View {
		public bool HotKeyCommandWasCalled { get; set; }
		public HotKeyTestView ()
		{
			HotKeySpecifier = (Rune)'^';
			Text = "^Test";
			//AddCommand (Command.Accept, () => {
			//	HotKeyCommandWasCalled = true;
			//	return true;
			//});
		}
	}

}