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
		Assert.Equal (ConsoleDriverKey.Null, view.HotKey);

		// Verify key bindings were set
		var commands = view.KeyBindings.GetCommands (ConsoleDriverKey.Null);
		Assert.Empty (commands);
	}

	[Theory]
	[InlineData (ConsoleDriverKey.A)]
	[InlineData ((ConsoleDriverKey)'a')]
	[InlineData (ConsoleDriverKey.A | ConsoleDriverKey.ShiftMask)]
	[InlineData (ConsoleDriverKey.D1)]
	[InlineData (ConsoleDriverKey.D1 | ConsoleDriverKey.ShiftMask)]
	[InlineData ((ConsoleDriverKey)'!')]
	[InlineData ((ConsoleDriverKey)'х')]  // Cyrillic x
	[InlineData ((ConsoleDriverKey)'你')] // Chinese ni
	[InlineData ((ConsoleDriverKey)'ö')] // German o umlaut
	public void Set_SupportsKeys (ConsoleDriverKey key)
	{
		var view = new View ();
		view.HotKey = key;
		Assert.Equal (key, view.HotKey);
	}

	[Theory]
	[InlineData (ConsoleDriverKey.A)]
	[InlineData (ConsoleDriverKey.A | ConsoleDriverKey.ShiftMask)]
	[InlineData (ConsoleDriverKey.D1)]
	[InlineData (ConsoleDriverKey.D1 | ConsoleDriverKey.ShiftMask)] // '!'
	[InlineData ((ConsoleDriverKey)'х')]  // Cyrillic x
	[InlineData ((ConsoleDriverKey)'你')] // Chinese ni
	[InlineData ((ConsoleDriverKey)'ö')] // German o umlaut
	public void Set_SetsKeyBindings (ConsoleDriverKey key)
	{
		var view = new View ();
		view.HotKey = key;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (key, view.HotKey);

		// Verify key bindings were set

		// As passed
		var commands = view.KeyBindings.GetCommands (key);
		Assert.Contains (Command.Accept, commands);
		commands = view.KeyBindings.GetCommands (key | ConsoleDriverKey.AltMask);
		Assert.Contains (Command.Accept, commands);

		var baseKey = key & ~ConsoleDriverKey.ShiftMask;
		// If A...Z, with and without shift
		if (baseKey is >= ConsoleDriverKey.A and <= ConsoleDriverKey.Z) {
			commands = view.KeyBindings.GetCommands (key | ConsoleDriverKey.ShiftMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (key & ~ConsoleDriverKey.ShiftMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (key | ConsoleDriverKey.AltMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (key & ~ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask);
			Assert.Contains (Command.Accept, commands);
		} else {
			// Non A..Z keys should not have shift bindings
			if (key.HasFlag (ConsoleDriverKey.ShiftMask)) {
				commands = view.KeyBindings.GetCommands (key & ~ConsoleDriverKey.ShiftMask);
				Assert.Empty (commands);
			} else {
				commands = view.KeyBindings.GetCommands (key | ConsoleDriverKey.ShiftMask);
				Assert.Empty (commands);
			}
		}
	}

	[Fact]
	public void Set_RemovesOldKeyBindings ()
	{
		var view = new View ();
		view.HotKey = ConsoleDriverKey.A;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (ConsoleDriverKey.A, view.HotKey);

		// Verify key bindings were set
		var commands = view.KeyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Contains (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (ConsoleDriverKey.A | ConsoleDriverKey.ShiftMask);
		Assert.Contains (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (ConsoleDriverKey.A | ConsoleDriverKey.AltMask);
		Assert.Contains (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (ConsoleDriverKey.A | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask);
		Assert.Contains (Command.Accept, commands);

		// Now set again
		view.HotKey = ConsoleDriverKey.B;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (ConsoleDriverKey.B, view.HotKey);

		commands = view.KeyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.DoesNotContain (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (ConsoleDriverKey.A | ConsoleDriverKey.ShiftMask);
		Assert.DoesNotContain (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (ConsoleDriverKey.A | ConsoleDriverKey.AltMask);
		Assert.DoesNotContain (Command.Accept, commands);

		commands = view.KeyBindings.GetCommands (ConsoleDriverKey.A | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask);
		Assert.DoesNotContain (Command.Accept, commands);
	}

	[Fact]
	public void Set_Throws_If_Modifiers_Are_Included ()
	{
		var view = new View ();
		// A..Z must be naked (Alt is assumed)
		view.HotKey = ConsoleDriverKey.A | ConsoleDriverKey.AltMask;
		Assert.Throws<ArgumentException> (() => view.HotKey = ConsoleDriverKey.A | ConsoleDriverKey.CtrlMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = ConsoleDriverKey.A | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask);

		// All others must not have Ctrl (Alt is assumed)
		view.HotKey = ConsoleDriverKey.D1 | ConsoleDriverKey.AltMask;
		Assert.Throws<ArgumentException> (() => view.HotKey = ConsoleDriverKey.D1 | ConsoleDriverKey.CtrlMask);
		Assert.Throws<ArgumentException> (() => view.HotKey = ConsoleDriverKey.D1 | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask);

		// Shift is ok (e.g. this is '!')
		view.HotKey = ConsoleDriverKey.D1 | ConsoleDriverKey.ShiftMask;
	}

	[Theory]
	[InlineData (ConsoleDriverKey.A)]
	[InlineData (ConsoleDriverKey.A | ConsoleDriverKey.ShiftMask)]
	[InlineData (ConsoleDriverKey.D1)]
	[InlineData (ConsoleDriverKey.D1 | ConsoleDriverKey.ShiftMask)] // '!'
	[InlineData ((ConsoleDriverKey)'х')]  // Cyrillic x
	[InlineData ((ConsoleDriverKey)'你')] // Chinese ni
	public void AddKeyBindingsForHotKey_Sets (ConsoleDriverKey key)
	{
		var view = new View ();
		view.HotKey = ConsoleDriverKey.Z;
		Assert.Equal (string.Empty, view.Title);
		Assert.Equal (ConsoleDriverKey.Z, view.HotKey);

		view.AddKeyBindingsForHotKey (ConsoleDriverKey.Null, key);

		// Verify key bindings were set

		// As passed
		var commands = view.KeyBindings.GetCommands (key);
		Assert.Contains (Command.Accept, commands);
		commands = view.KeyBindings.GetCommands (key | ConsoleDriverKey.AltMask);
		Assert.Contains (Command.Accept, commands);

		var baseKey = key & ~ConsoleDriverKey.ShiftMask;
		// If A...Z, with and without shift
		if (baseKey is >= ConsoleDriverKey.A and <= ConsoleDriverKey.Z) {
			commands = view.KeyBindings.GetCommands (key | ConsoleDriverKey.ShiftMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (key & ~ConsoleDriverKey.ShiftMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (key | ConsoleDriverKey.AltMask);
			Assert.Contains (Command.Accept, commands);
			commands = view.KeyBindings.GetCommands (key & ~ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask);
			Assert.Contains (Command.Accept, commands);
		} else {
			// Non A..Z keys should not have shift bindings
			if (key.HasFlag (ConsoleDriverKey.ShiftMask)) {
				commands = view.KeyBindings.GetCommands (key & ~ConsoleDriverKey.ShiftMask);
				Assert.Empty (commands);
			} else {
				commands = view.KeyBindings.GetCommands (key | ConsoleDriverKey.ShiftMask);
				Assert.Empty (commands);
			}
		}
	}

	[Theory]
	[InlineData (ConsoleDriverKey.Delete)]
	[InlineData (ConsoleDriverKey.Backspace)]
	[InlineData (ConsoleDriverKey.Tab)]
	[InlineData (ConsoleDriverKey.Enter)]
	[InlineData (ConsoleDriverKey.Esc)]
	[InlineData (ConsoleDriverKey.Space)]
	[InlineData (ConsoleDriverKey.CursorLeft)]
	[InlineData (ConsoleDriverKey.F1)]
	public void Set_Throws_With_Invalid_HotKeys (ConsoleDriverKey key)
	{
		var view = new View ();
		Assert.Throws<ArgumentException> (() => view.HotKey = key);
	}

	[Theory]
	[InlineData ("Test", ConsoleDriverKey.T)]
	[InlineData ("^Test", ConsoleDriverKey.T)]
	[InlineData ("T^est", ConsoleDriverKey.E)]
	[InlineData ("Te^st", ConsoleDriverKey.S)]
	[InlineData ("Tes^t", ConsoleDriverKey.T)]
	[InlineData ("other", ConsoleDriverKey.Null)]
	[InlineData ("oTher", ConsoleDriverKey.T)]
	[InlineData ("^Öther", (ConsoleDriverKey)'Ö')]
	[InlineData ("^öther", (ConsoleDriverKey)'ö')]
	// BUGBUG: '!' should be supported. Line 968 of TextFormatter filters on char.IsLetterOrDigit 
	//[InlineData ("Test^!", (Key)'!')]
	public void Text_Change_Sets_HotKey (string text, ConsoleDriverKey expectedHotKey)
	{
		var view = new View () {
			HotKeySpecifier = new Rune ('^'),
			Text = "^Hello"
		};
		Assert.Equal (ConsoleDriverKey.H, view.HotKey);
		
		view.Text = text;
		Assert.Equal (expectedHotKey, view.HotKey);

	}

	[Theory]
	[InlineData("^Test")]
	public void Text_Sets_HotKey_To_KeyNull (string text)
	{
		var view = new View () {
			HotKeySpecifier = (Rune)'^',
			Text = text
		};

		Assert.Equal (text, view.Text);
		Assert.Equal (ConsoleDriverKey.T, view.HotKey);

		view.Text = string.Empty;
		Assert.Equal ("", view.Text);
		Assert.Equal (ConsoleDriverKey.Null, view.HotKey);
	}

	[Theory]
	[InlineData (ConsoleDriverKey.Null, true)] // non-shift
	[InlineData (ConsoleDriverKey.ShiftMask, true)]
	[InlineData (ConsoleDriverKey.AltMask, true)]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask, true)]
	[InlineData (ConsoleDriverKey.CtrlMask, false)]
	[InlineData (ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask, false)]
	public void KeyPress_Runs_Default_HotKey_Command (ConsoleDriverKey mask, bool expected)
	{
		var view = new View () {
			HotKeySpecifier = (Rune)'^',
			Text = "^Test"
		};
		view.CanFocus = true;
		Assert.False (view.HasFocus);
		view.ProcessKeyDown (new (ConsoleDriverKey.T | mask));
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

		var ke = new KeyEventArgs (ConsoleDriverKey.T);
		superView.ProcessKeyDown (ke);
		Assert.True (view.HasFocus);

	}


	[Fact]
	public void ProcessKeyDown_Ignores_KeyBindings_Out_Of_Scope_SuperView ()
	{
		var view = new View ();
		view.KeyBindings.Add (ConsoleDriverKey.A, Command.Default);
		view.InvokingKeyBindings += (s, e) => {
			Assert.Fail ();
		};
		
		var superView = new View ();
		superView.Add (view);

		var ke = new KeyEventArgs (ConsoleDriverKey.A);
		superView.ProcessKeyDown (ke);
	}
}