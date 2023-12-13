using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

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
		var keyBindings = new KeyBindings ();
		Assert.Throws<InvalidOperationException> (() => keyBindings.GetKeyFromCommands (Command.Accept));
	}

	[Fact]
	public void Add_Single_Adds ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (ConsoleDriverKey.A, Command.Default);
		var resultCommands = keyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Contains (Command.Default, resultCommands);

		keyBindings.Add (ConsoleDriverKey.B, Command.Default);
		resultCommands = keyBindings.GetCommands (ConsoleDriverKey.B);
		Assert.Contains (Command.Default, resultCommands);
	}

	[Fact]
	public void Add_Multiple_Adds ()
	{
		var keyBindings = new KeyBindings ();
		var commands = new Command [] {
			Command.Right,
			Command.Left
		};

		keyBindings.Add (ConsoleDriverKey.A, commands);
		var resultCommands = keyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);

		keyBindings.Add (ConsoleDriverKey.B, commands);
		resultCommands = keyBindings.GetCommands (ConsoleDriverKey.B);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
	}

	[Fact]
	public void Add_Empty_Throws ()
	{
		var keyBindings = new KeyBindings ();
		var commands = new List<Command> ();
		Assert.Throws<ArgumentException> (() => keyBindings.Add (ConsoleDriverKey.A, commands.ToArray ()));
	}

	// Add with scope does the right things
	[Theory]
	[InlineData (KeyBindingScope.Focused)]
	[InlineData (KeyBindingScope.HotKey)]
	[InlineData (KeyBindingScope.Global)]
	public void Scope_Add_Adds (KeyBindingScope scope)
	{
		var keyBindings = new KeyBindings ();
		var commands = new Command [] {
			Command.Right,
			Command.Left
		};

		keyBindings.Add (ConsoleDriverKey.A, scope, commands);
		var binding = keyBindings.Get (ConsoleDriverKey.A);
		Assert.Contains (Command.Right, binding.Commands);
		Assert.Contains (Command.Left, binding.Commands);

		binding = keyBindings.Get (ConsoleDriverKey.A, scope);
		Assert.Contains (Command.Right, binding.Commands);
		Assert.Contains (Command.Left, binding.Commands);

		var resultCommands = keyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
	}

	[Theory]
	[InlineData (KeyBindingScope.Focused)]
	[InlineData (KeyBindingScope.HotKey)]
	[InlineData (KeyBindingScope.Global)]
	public void Scope_Get_Filters (KeyBindingScope scope)
	{
		var keyBindings = new KeyBindings ();
		var commands = new Command [] {
			Command.Right,
			Command.Left
		};

		keyBindings.Add (ConsoleDriverKey.A, scope, commands);
		var binding = keyBindings.Get (ConsoleDriverKey.A);
		Assert.Contains (Command.Right, binding.Commands);
		Assert.Contains (Command.Left, binding.Commands);

		binding = keyBindings.Get (ConsoleDriverKey.A, scope);
		Assert.Contains (Command.Right, binding.Commands);
		Assert.Contains (Command.Left, binding.Commands);

		// negative test
		binding = keyBindings.Get (ConsoleDriverKey.A, (KeyBindingScope)(int)-1);
		Assert.Null (binding);

		var resultCommands = keyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
	}

	// Clear
	[Fact]
	public void Clear_Clears ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (ConsoleDriverKey.A, Command.Default);
		keyBindings.Add (ConsoleDriverKey.B, Command.Default);
		keyBindings.Clear ();
		var resultCommands = keyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Empty (resultCommands);
		resultCommands = keyBindings.GetCommands (ConsoleDriverKey.B);
		Assert.Empty (resultCommands);
	}

	// GetCommands
	[Fact]
	public void GetCommands_Unknown_ReturnsEmpty ()
	{
		var keyBindings = new KeyBindings ();
		var resultCommands = keyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Empty (resultCommands);
	}

	[Fact]
	public void GetCommands_WithCommands_ReturnsCommands ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (ConsoleDriverKey.A, Command.Default);
		var resultCommands = keyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Contains (Command.Default, resultCommands);
	}

	[Fact]
	public void GetCommands_WithMultipleCommands_ReturnsCommands ()
	{
		var keyBindings = new KeyBindings ();
		var commands = new Command [] {
			Command.Right,
			Command.Left
		};
		keyBindings.Add (ConsoleDriverKey.A, commands);
		var resultCommands = keyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
	}

	[Fact]
	public void GetCommands_WithMultipleBindings_ReturnsCommands ()
	{
		var keyBindings = new KeyBindings ();
		var commands = new Command [] {
			Command.Right,
			Command.Left
		};
		keyBindings.Add (ConsoleDriverKey.A, commands);
		keyBindings.Add (ConsoleDriverKey.B, commands);
		var resultCommands = keyBindings.GetCommands (ConsoleDriverKey.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
		resultCommands = keyBindings.GetCommands (ConsoleDriverKey.B);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
	}

	// GetKeyFromCommands
	[Fact]
	public void GetKeyFromCommands_Unknown_Throws_InvalidOperationException ()
	{
		var keyBindings = new KeyBindings ();
		Assert.Throws<InvalidOperationException> (() => keyBindings.GetKeyFromCommands (Command.Accept));
	}

	[Fact]
	public void GetKeyFromCommands_WithCommands_ReturnsKey ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (ConsoleDriverKey.A, Command.Default);
		var resultKey = keyBindings.GetKeyFromCommands (Command.Default);
		Assert.Equal (ConsoleDriverKey.A, resultKey);
	}

	[Fact]
	public void Replace_Key ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (ConsoleDriverKey.A, Command.Default);
		keyBindings.Add (ConsoleDriverKey.B, Command.Default);
		keyBindings.Add (ConsoleDriverKey.C, Command.Default);
		keyBindings.Add (ConsoleDriverKey.D, Command.Default);

		keyBindings.Replace (ConsoleDriverKey.A, ConsoleDriverKey.E);
		Assert.Empty (keyBindings.GetCommands (ConsoleDriverKey.A));
		Assert.Contains (Command.Default, keyBindings.GetCommands (ConsoleDriverKey.E));

		keyBindings.Replace (ConsoleDriverKey.B, ConsoleDriverKey.E);
		Assert.Empty (keyBindings.GetCommands (ConsoleDriverKey.B));
		Assert.Contains (Command.Default, keyBindings.GetCommands (ConsoleDriverKey.E));

		keyBindings.Replace (ConsoleDriverKey.C, ConsoleDriverKey.E);
		Assert.Empty (keyBindings.GetCommands (ConsoleDriverKey.C));
		Assert.Contains (Command.Default, keyBindings.GetCommands (ConsoleDriverKey.E));

		keyBindings.Replace (ConsoleDriverKey.D, ConsoleDriverKey.E);
		Assert.Empty (keyBindings.GetCommands (ConsoleDriverKey.D));
		Assert.Contains (Command.Default, keyBindings.GetCommands (ConsoleDriverKey.E));
	}

	// TryGet
	[Fact]
	public void TryGet_Unknown_ReturnsFalse ()
	{
		var keyBindings = new KeyBindings ();
		var result = keyBindings.TryGet (ConsoleDriverKey.A, out var commands);
		Assert.False (result);
	}

	[Fact]
	public void TryGet_WithCommands_ReturnsTrue ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (ConsoleDriverKey.A, Command.Default);
		var result = keyBindings.TryGet (ConsoleDriverKey.A, out var bindings);
		Assert.True (result);
		Assert.Contains (Command.Default, bindings.Commands);
	}


	[Fact]
	public void GetKeyFromCommands_OneCommand ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (ConsoleDriverKey.A, Command.Right);

		var key = keyBindings.GetKeyFromCommands (Command.Right);
		Assert.Equal (ConsoleDriverKey.A, key);

		// Negative case
		Assert.Throws<InvalidOperationException> (() => key = keyBindings.GetKeyFromCommands (Command.Left));
	}


	[Fact]
	public void GetKeyFromCommands_MultipleCommands ()
	{
		var keyBindings = new KeyBindings ();
		var commands1 = new Command [] {
			Command.Right,
			Command.Left
		};
		keyBindings.Add (ConsoleDriverKey.A, commands1);

		var commands2 = new Command [] {
			Command.LineUp,
			Command.LineDown
		};
		keyBindings.Add (ConsoleDriverKey.B, commands2);

		var key = keyBindings.GetKeyFromCommands (commands1);
		Assert.Equal (ConsoleDriverKey.A, key);

		key = keyBindings.GetKeyFromCommands (commands2);
		Assert.Equal (ConsoleDriverKey.B, key);

		// Negative case
		Assert.Throws<InvalidOperationException> (() => key = keyBindings.GetKeyFromCommands (Command.EndOfLine));
	}
}