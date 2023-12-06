using System.Collections.Generic;
using System;
using System.ComponentModel.Design;
using Xunit;
using Xunit.Abstractions;
using System.Windows.Input;
using UICatalog.Scenarios;

namespace Terminal.Gui.ViewTests;

public class KeyBindingTests {
	readonly ITestOutputHelper _output;

	public KeyBindingTests (ITestOutputHelper output)
	{
		this._output = output;
	}


	[Fact]
	public void Defaults ()
	{
		var view = new View ();
		// BUGBUG: This should be Command.Default
		Assert.Contains(Command.Accept, view.GetSupportedCommands ());
		Assert.Throws<InvalidOperationException> (() => view.GetKeyFromCommands (Command.Accept));
	}

	// public virtual bool OnInvokeKeyBindings (KeyEventArgs keyEvent)
	// public event EventHandler<KeyEventArgs> InvokingKeyBindings;
	// protected bool? InvokeKeyBindings (KeyEventArgs keyEvent)
	// public bool? InvokeCommand (Command command)
	// public void AddKeyBinding (Key key, params Command [] commands)

	[Fact]
	public void AddKeyBinding_Single_Adds ()
	{
		var view = new View ();
		view.AddKeyBinding(Key.A, Command.Default);
		var resultCommands = view.GetKeyBinding (Key.A);
		Assert.Contains (Command.Default, resultCommands);

		view.AddKeyBinding (Key.B, Command.Default);
		resultCommands = view.GetKeyBinding (Key.B);
		Assert.Contains (Command.Default, resultCommands);

		// Verify default is there too still
		Assert.Contains (Command.Accept, view.GetSupportedCommands ());
	}

	[Fact]
	public void AddKeyBinding_Multiple_Adds ()
	{
		var view = new View ();
		var commands = new Command [] {
			Command.Right,
			Command.Left
		};

		view.AddKeyBinding (Key.A, commands);
		var resultCommands = view.GetKeyBinding (Key.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);

		view.AddKeyBinding (Key.B, commands);
		resultCommands = view.GetKeyBinding (Key.B);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);

		// Verify default is there too still
		Assert.Contains (Command.Accept, view.GetSupportedCommands ());
	}


	[Fact]
	public void AddKeyBinding_Adds_Empty_Throws ()
	{
		var view = new View ();
		var commands = new List<Command> ();
		Assert.Throws<ArgumentException> (() => view.AddKeyBinding (Key.A, commands.ToArray ()));
	}
	
	// protected void ReplaceKeyBinding (Key fromKey, Key toKey)

	// public bool TryGetKeyBindings (Key key, out Command [] commands)
	[Fact]
	public void TryKeyBindings_Shift ()
	{
		// Test shift, alt, etc...

	}

	// public Command [] GetKeyBindings (Key key)
	// public void ClearKeyBindings ()
	// public void ClearKeyBinding (Key key)
	// public void ClearKeyBinding (params Command [] command)
	// protected void AddCommand (Command command, Func<bool?> f)
	// public IEnumerable<Command> GetSupportedCommands ()
	
	// public Key GetKeyFromCommand (params Command [] command)

	[Fact]
	public void GetKeyFromCommands_OneCommand ()
	{
		var view = new View ();
		view.AddKeyBinding (Key.A, Command.Right);

		var key = view.GetKeyFromCommands (Command.Right);
		Assert.Equal (Key.A, key);

		// Negative case
		Assert.Throws<InvalidOperationException> (() => key = view.GetKeyFromCommands (Command.Left));
	}


	[Fact]
	public void GetKeyFromCommands_MultipleCommands ()
	{
		var view = new View ();
		var commands1 = new Command [] {
			Command.Right,
			Command.Left
		};
		view.AddKeyBinding (Key.A, commands1);

		var commands2 = new Command [] {
			Command.LineUp,
			Command.LineDown
		};
		view.AddKeyBinding (Key.B, commands2);

		var key = view.GetKeyFromCommands (commands1);
		Assert.Equal (Key.A, key);

		key = view.GetKeyFromCommands (commands2);
		Assert.Equal (Key.B, key);

		// Negative case
		Assert.Throws<InvalidOperationException> (() => key = view.GetKeyFromCommands (Command.EndOfLine));
	}

	//[Fact]
	//[AutoInitShutdown]
	//public void KeyBindingExample ()
	//{
	//	int pressed = 0;
	//	var btn = new Button ("Press Me");
	//	btn.Clicked += (s, e) => pressed++;

	//	// The Button class supports the Accept command
	//	Assert.Contains (Command.Accept, btn.GetSupportedCommands ());

	//	Application.Top.Add (btn);
	//	Application.Begin (Application.Top);

	//	// default keybinding is Enter which results in keypress
	//	Application.OnKeyPressed (new ((Key)' '));
	//	Assert.Equal (1, pressed);

	//	// remove the default keybinding (Enter)
	//	btn.ClearKeyBinding (Command.Accept);

	//	// After clearing the default keystroke the Enter button no longer does anything for the Button
	//	Application.OnKeyPressed (new ((Key)' '));
	//	Assert.Equal (1, pressed);

	//	// Set a new binding of b for the click (Accept) event
	//	btn.AddKeyBinding (Key.B, Command.Accept);

	//	// now pressing B should call the button click event
	//	Application.OnKeyPressed (new (Key.B));
	//	Assert.Equal (2, pressed);

	//	// now pressing Shift-B should call the button click event
	//	Application.OnKeyPressed (new (Key.ShiftMask | Key.B));
	//	Assert.Equal (3, pressed);

	//	// now pressing Alt-B should call the button click event
	//	Application.OnKeyPressed (new (Key.AltMask | Key.B));
	//	Assert.Equal (4, pressed);

	//	// now pressing Shift-Alt-B should call the button click event
	//	Application.OnKeyPressed (new (Key.ShiftMask | Key.AltMask | Key.B));
	//	Assert.Equal (4, pressed);

	//}

}