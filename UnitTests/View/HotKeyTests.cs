using System;
using Xunit;
using Xunit.Abstractions;
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
		Assert.Equal (KeyCode.Null, view.HotKey);

		// Verify key bindings were set
		var commands = view.KeyBindings.GetCommands (KeyCode.Null);
		Assert.Empty (commands);
	}

	[Theory]
	[InlineData (KeyCode.A)]
	[InlineData ((KeyCode)'a')]
	[InlineData (KeyCode.A | KeyCode.ShiftMask)]
	[InlineData (KeyCode.D1)]
	[InlineData (KeyCode.D1 | KeyCode.ShiftMask)]
	[InlineData ((KeyCode)'!')]
	[InlineData ((KeyCode)'х')]  // Cyrillic x
	[InlineData ((KeyCode)'你')] // Chinese ni
	[InlineData ((KeyCode)'ö')] // German o umlaut
	[InlineData (KeyCode.Null)]
	public void Set_Sets_WithValidKey (KeyCode key)
	{
		var view = new View ();
		view.HotKey = key;
		Assert.Equal (key, view.HotKey);
	}

	[Theory]
	[InlineData (KeyCode.A)]
	[InlineData (KeyCode.A | KeyCode.ShiftMask)]
	[InlineData (KeyCode.D1)]
	[InlineData (KeyCode.D1 | KeyCode.ShiftMask)] // '!'
	[InlineData ((KeyCode)'х')]  // Cyrillic x
	[InlineData ((KeyCode)'你')] // Chinese ni
	[InlineData ((KeyCode)'ö')] // German o umlaut
	public void Set_SetsKeyBindings (KeyCode key)
	{
		var view = new View ();
		view.HotKey = (Key)key;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal ((Key)key, view.HotKey);

		// Verify key bindings were set

		// As passed
		var commands = view.KeyBindings.GetCommands ((Key)key);
		Assert.Contains (Command.Accept, commands);

		var baseKey = ((Key)key).NoShift;
		// If A...Z, with and without shift
		if (baseKey.IsKeyCodeAtoZ) {
			commands = view.KeyBindings.GetCommands (((Key)key).WithShift);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (((Key)key).NoShift);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (((Key)key).WithAlt);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (((Key)key).NoShift.WithAlt);
			Assert.Contains (Command.Accept, commands);
		} else {
			// Non A..Z keys should not have shift bindings
			if (((Key)key).IsShift) {
				commands = view.KeyBindings.GetCommands (((Key)key).NoShift);
				Assert.Empty (commands);
			} else {
				commands = view.KeyBindings.GetCommands (((Key)key).WithShift);
				Assert.Empty (commands);
			}
		}
	}

	[Fact]
	public void Set_RemovesOldKeyBindings ()
	{
		var view = new View ();
		view.HotKey = KeyCode.A;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (KeyCode.A, view.HotKey);

		// Verify key bindings were set
		var commands = view.KeyBindings.GetCommands (KeyCode.A);
		Assert.Contains (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.ShiftMask);
		Assert.Contains (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.AltMask);
		Assert.Contains (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask);
		Assert.Contains (Command.Accept, commands);

		// Now set again
		view.HotKey = KeyCode.B;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (KeyCode.B, view.HotKey);

		commands = view.KeyBindings.GetCommands (KeyCode.A);
		Assert.DoesNotContain (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.ShiftMask);
		Assert.DoesNotContain (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.AltMask);
		Assert.DoesNotContain (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask);
		Assert.DoesNotContain (Command.Accept, commands);
	}

	[Fact]
	public void Set_Throws_If_Modifiers_Are_Included ()
	{
		var view = new View ();
		// A..Z must be naked (Alt is assumed)
		view.HotKey = KeyCode.A | KeyCode.AltMask;
		Assert.Throws<ArgumentException> (() => view.HotKey = KeyCode.A | KeyCode.CtrlMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = KeyCode.A | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask);

		// All others must not have Ctrl (Alt is assumed)
		view.HotKey = KeyCode.D1 | KeyCode.AltMask;
		Assert.Throws<ArgumentException> (() => view.HotKey = KeyCode.D1 | KeyCode.CtrlMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = KeyCode.D1 | KeyCode.ShiftMask | KeyCode.AltMask | KeyCode.CtrlMask);

		// Shift is ok (e.g. this is '!')
		view.HotKey = KeyCode.D1 | KeyCode.ShiftMask;
	}

	[Theory]
	[InlineData (KeyCode.A)]
	[InlineData (KeyCode.A | KeyCode.ShiftMask)]
	[InlineData (KeyCode.D1)]
	[InlineData (KeyCode.D1 | KeyCode.ShiftMask)] // '!'
	[InlineData ((KeyCode)'х')]  // Cyrillic x
	[InlineData ((KeyCode)'你')] // Chinese ni
	public void AddKeyBindingsForHotKey_Sets (KeyCode key)
	{
		var view = new View ();
		view.HotKey = KeyCode.Z;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (KeyCode.Z, view.HotKey);

		view.AddKeyBindingsForHotKey (KeyCode.Null, key);

		// Verify key bindings were set

		// As passed
		var commands = view.KeyBindings.GetCommands (key);
		Assert.Contains (Command.Accept, commands);
		commands = view.KeyBindings.GetCommands (key | KeyCode.AltMask);
		Assert.Contains (Command.Accept, commands);

		var baseKey = key & ~KeyCode.ShiftMask;
		// If A...Z, with and without shift
		if (baseKey is >= KeyCode.A and <= KeyCode.Z) {
			commands = view.KeyBindings.GetCommands (key | KeyCode.ShiftMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (key & ~KeyCode.ShiftMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (key | KeyCode.AltMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (key & ~KeyCode.ShiftMask | KeyCode.AltMask);
			Assert.Contains (Command.Accept, commands);
		} else {
			// Non A..Z keys should not have shift bindings
			if (key.HasFlag (KeyCode.ShiftMask)) {
				commands = view.KeyBindings.GetCommands (key & ~KeyCode.ShiftMask);
				Assert.Empty (commands);
			} else {
				commands = view.KeyBindings.GetCommands (key | KeyCode.ShiftMask);
				Assert.Empty (commands);
			}
		}
	}

	[Theory]
	[InlineData (KeyCode.Delete)]
	[InlineData (KeyCode.Backspace)]
	[InlineData (KeyCode.Tab)]
	[InlineData (KeyCode.Enter)]
	[InlineData (KeyCode.Esc)]
	[InlineData (KeyCode.Space)]
	[InlineData (KeyCode.CursorLeft)]
	[InlineData (KeyCode.F1)]
	[InlineData (KeyCode.Null | KeyCode.ShiftMask)]
	public void Set_Throws_With_Invalid_Key (KeyCode key)
	{
		var view = new View ();
		Assert.Throws<ArgumentException> (() => view.HotKey = key);
	}

	[Theory]
	[InlineData ("Test", KeyCode.Null)]
	[InlineData ("^Test", KeyCode.T)]
	[InlineData ("T^est", KeyCode.E)]
	[InlineData ("Te^st", KeyCode.S)]
	[InlineData ("Tes^t", KeyCode.T)]
	[InlineData ("other", KeyCode.Null)]
	[InlineData ("oTher", KeyCode.Null)]
	[InlineData ("^Öther", (KeyCode)'Ö')]
	[InlineData ("^öther", (KeyCode)'ö')]
	// BUGBUG: '!' should be supported. Line 968 of TextFormatter filters on char.IsLetterOrDigit 
	//[InlineData ("Test^!", (Key)'!')]
	public void Text_Change_Sets_HotKey (string text, KeyCode expectedHotKey)
	{
		var view = new View () {
			HotKeySpecifier = new Rune ('^'),
			Text = "^Hello"
		};
		Assert.Equal (KeyCode.H, view.HotKey);
		
		view.Text = text;
		Assert.Equal (expectedHotKey, view.HotKey);

	}

	[Theory]
	[InlineData("^Test")]
	public void Text_Empty_Sets_HotKey_To_Null (string text)
	{
		var view = new View () {
			HotKeySpecifier = (Rune)'^',
			Text = text
		};

		Assert.Equal (text, view.Text);
		Assert.Equal (KeyCode.T, view.HotKey);

		view.Text = string.Empty;
		Assert.Equal ("", view.Text);
		Assert.Equal (KeyCode.Null, view.HotKey);
	}

	[Theory]
	[InlineData (KeyCode.Null, true)] // non-shift
	[InlineData (KeyCode.ShiftMask, true)]
	[InlineData (KeyCode.AltMask, true)]
	[InlineData (KeyCode.ShiftMask | KeyCode.AltMask, true)]
	[InlineData (KeyCode.CtrlMask, false)]
	[InlineData (KeyCode.ShiftMask | KeyCode.CtrlMask, false)]
	public void KeyPress_Runs_Default_HotKey_Command (KeyCode mask, bool expected)
	{
		var view = new View () {
			HotKeySpecifier = (Rune)'^',
			Text = "^Test"
		};
		view.CanFocus = true;
		Assert.False (view.HasFocus);
		view.NewKeyDownEvent (new (KeyCode.T | mask));
		Assert.Equal (expected, view.HasFocus);
	}

	[Fact]
	public void ProcessKeyDown_Invokes_HotKey_Command_With_SuperView ()
	{
		var view = new View () {
			HotKeySpecifier = (Rune)'^',
			Text = "^Test"
		};

		var superView = new View ();
		superView.Add (view);

		view.CanFocus = true;
		Assert.False (view.HasFocus);

		var ke = new Key (KeyCode.T);
		superView.NewKeyDownEvent (ke);
		Assert.True (view.HasFocus);

	}


	[Fact]
	public void ProcessKeyDown_Ignores_KeyBindings_Out_Of_Scope_SuperView ()
	{
		var view = new View ();
		view.KeyBindings.Add (KeyCode.A, Command.Default);
		view.InvokingKeyBindings += (s, e) => {
			Assert.Fail ();
		};
		
		var superView = new View ();
		superView.Add (view);

		var ke = new Key (KeyCode.A);
		superView.NewKeyDownEvent (ke);
	}
}