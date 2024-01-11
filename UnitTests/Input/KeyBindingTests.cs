using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.InputTests;

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
		keyBindings.Add (Key.A, Command.Default);
		var resultCommands = keyBindings.GetCommands (Key.A);
		Assert.Contains (Command.Default, resultCommands);

		keyBindings.Add (Key.B, Command.Default);
		resultCommands = keyBindings.GetCommands (Key.B);
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

		keyBindings.Add (Key.A, commands);
		var resultCommands = keyBindings.GetCommands (Key.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);

		keyBindings.Add (Key.B, commands);
		resultCommands = keyBindings.GetCommands (Key.B);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
	}

	[Fact]
	public void Add_Empty_Throws ()
	{
		var keyBindings = new KeyBindings ();
		var commands = new List<Command> ();
		Assert.Throws<ArgumentException> (() => keyBindings.Add (Key.A, commands.ToArray ()));
	}

	// Add with scope does the right things
	[Theory]
	[InlineData (KeyBindingScope.Focused)]
	[InlineData (KeyBindingScope.HotKey)]
	[InlineData (KeyBindingScope.Application)]
	public void Scope_Add_Adds (KeyBindingScope scope)
	{
		var keyBindings = new KeyBindings ();
		var commands = new Command [] {
			Command.Right,
			Command.Left
		};

		keyBindings.Add (Key.A, scope, commands);
		var binding = keyBindings.Get (Key.A);
		Assert.Contains (Command.Right, binding.Commands);
		Assert.Contains (Command.Left, binding.Commands);

		binding = keyBindings.Get (Key.A, scope);
		Assert.Contains (Command.Right, binding.Commands);
		Assert.Contains (Command.Left, binding.Commands);

		var resultCommands = keyBindings.GetCommands (Key.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
	}

	[Theory]
	[InlineData (KeyBindingScope.Focused)]
	[InlineData (KeyBindingScope.HotKey)]
	[InlineData (KeyBindingScope.Application)]
	public void Scope_Get_Filters (KeyBindingScope scope)
	{
		var keyBindings = new KeyBindings ();
		var commands = new Command [] {
			Command.Right,
			Command.Left
		};

		keyBindings.Add (Key.A, scope, commands);
		var binding = keyBindings.Get (Key.A);
		Assert.Contains (Command.Right, binding.Commands);
		Assert.Contains (Command.Left, binding.Commands);

		binding = keyBindings.Get (Key.A, scope);
		Assert.Contains (Command.Right, binding.Commands);
		Assert.Contains (Command.Left, binding.Commands);

		// negative test
		binding = keyBindings.Get (Key.A, (KeyBindingScope)(int)-1);
		Assert.Null (binding);

		var resultCommands = keyBindings.GetCommands (Key.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
	}

	// Clear
	[Fact]
	public void Clear_Clears ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (Key.A, Command.Default);
		keyBindings.Add (Key.B, Command.Default);
		keyBindings.Clear ();
		var resultCommands = keyBindings.GetCommands (Key.A);
		Assert.Empty (resultCommands);
		resultCommands = keyBindings.GetCommands (Key.B);
		Assert.Empty (resultCommands);
	}

	// GetCommands
	[Fact]
	public void GetCommands_Unknown_ReturnsEmpty ()
	{
		var keyBindings = new KeyBindings ();
		var resultCommands = keyBindings.GetCommands (Key.A);
		Assert.Empty (resultCommands);
	}

	[Fact]
	public void GetCommands_WithCommands_ReturnsCommands ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (Key.A, Command.Default);
		var resultCommands = keyBindings.GetCommands (Key.A);
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
		keyBindings.Add (Key.A, commands);
		var resultCommands = keyBindings.GetCommands (Key.A);
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
		keyBindings.Add (Key.A, commands);
		keyBindings.Add (Key.B, commands);
		var resultCommands = keyBindings.GetCommands (Key.A);
		Assert.Contains (Command.Right, resultCommands);
		Assert.Contains (Command.Left, resultCommands);
		resultCommands = keyBindings.GetCommands (Key.B);
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
		keyBindings.Add (Key.A, Command.Default);
		var resultKey = keyBindings.GetKeyFromCommands (Command.Default);
		Assert.Equal (Key.A, resultKey);
	}

	[Fact]
	public void Replace_Key ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (Key.A, Command.Default);
		keyBindings.Add (Key.B, Command.Default);
		keyBindings.Add (Key.C, Command.Default);
		keyBindings.Add (Key.D, Command.Default);

		keyBindings.Replace (Key.A, Key.E);
		Assert.Empty (keyBindings.GetCommands (Key.A));
		Assert.Contains (Command.Default, keyBindings.GetCommands (Key.E));

		keyBindings.Replace (Key.B, Key.E);
		Assert.Empty (keyBindings.GetCommands (Key.B));
		Assert.Contains (Command.Default, keyBindings.GetCommands (Key.E));

		keyBindings.Replace (Key.C, Key.E);
		Assert.Empty (keyBindings.GetCommands (Key.C));
		Assert.Contains (Command.Default, keyBindings.GetCommands (Key.E));

		keyBindings.Replace (Key.D, Key.E);
		Assert.Empty (keyBindings.GetCommands (Key.D));
		Assert.Contains (Command.Default, keyBindings.GetCommands (Key.E));
	}

	// TryGet
	[Fact]
	public void TryGet_Unknown_ReturnsFalse ()
	{
		var keyBindings = new KeyBindings ();
		var result = keyBindings.TryGet (Key.A, out var _);
		Assert.False (result);
	}

	[Fact]
	public void TryGet_WithCommands_ReturnsTrue ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (Key.A, Command.Default);
		var result = keyBindings.TryGet (Key.A, out var bindings);
		Assert.True (result);
		Assert.Contains (Command.Default, bindings.Commands);
	}


	[Fact]
	public void GetKeyFromCommands_OneCommand ()
	{
		var keyBindings = new KeyBindings ();
		keyBindings.Add (Key.A, Command.Right);

		var key = keyBindings.GetKeyFromCommands (Command.Right);
		Assert.Equal (Key.A, key);

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
		keyBindings.Add (Key.A, commands1);

		var commands2 = new Command [] {
			Command.LineUp,
			Command.LineDown
		};
		keyBindings.Add (Key.B, commands2);

		var key = keyBindings.GetKeyFromCommands (commands1);
		Assert.Equal (Key.A, key);

		key = keyBindings.GetKeyFromCommands (commands2);
		Assert.Equal (Key.B, key);

		// Negative case
		Assert.Throws<InvalidOperationException> (() => key = keyBindings.GetKeyFromCommands (Command.EndOfLine));
	}
}