using UICatalog.Scenarios;
using Xunit.Abstractions;

namespace Terminal.Gui.InputTests;

public class KeyBindingTests
{
    private readonly ITestOutputHelper _output;
    public KeyBindingTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void Add_Empty_Throws ()
    {
        var keyBindings = new KeyBindings ();
        List<Command> commands = new ();
        Assert.Throws<ArgumentException> (() => keyBindings.Add (Key.A, commands.ToArray ()));
    }

    [Fact]
    public void Add_Multiple_Adds ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };

        keyBindings.Add (Key.A, commands);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);

        keyBindings.Add (Key.B, commands);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void Add_Single_Adds ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.Default);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Default, resultCommands);

        keyBindings.Add (Key.B, Command.Default);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Contains (Command.Default, resultCommands);
    }

    // Clear
    [Fact]
    public void Clear_Clears ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.Default);
        keyBindings.Add (Key.B, Command.Default);
        keyBindings.Clear ();
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Empty (resultCommands);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Empty (resultCommands);
    }

    [Fact]
    public void Defaults ()
    {
        var keyBindings = new KeyBindings ();
        Assert.Throws<InvalidOperationException> (() => keyBindings.GetKeyFromCommands (Command.Accept));
    }

    // GetCommands
    [Fact]
    public void GetCommands_Unknown_ReturnsEmpty ()
    {
        var keyBindings = new KeyBindings ();
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Empty (resultCommands);
    }

    [Fact]
    public void GetCommands_WithCommands_ReturnsCommands ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.Default);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Default, resultCommands);
    }

    [Fact]
    public void GetCommands_WithMultipleBindings_ReturnsCommands ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };
        keyBindings.Add (Key.A, commands);
        keyBindings.Add (Key.B, commands);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void GetCommands_WithMultipleCommands_ReturnsCommands ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };
        keyBindings.Add (Key.A, commands);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void GetKeyFromCommands_MultipleCommands ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands1 = { Command.Right, Command.Left };
        keyBindings.Add (Key.A, commands1);

        Command [] commands2 = { Command.LineUp, Command.LineDown };
        keyBindings.Add (Key.B, commands2);

        Key key = keyBindings.GetKeyFromCommands (commands1);
        Assert.Equal (Key.A, key);

        key = keyBindings.GetKeyFromCommands (commands2);
        Assert.Equal (Key.B, key);

        // Negative case
        Assert.Throws<InvalidOperationException> (() => key = keyBindings.GetKeyFromCommands (Command.EndOfLine));
    }

    [Fact]
    public void GetKeyFromCommands_OneCommand ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.Right);

        Key key = keyBindings.GetKeyFromCommands (Command.Right);
        Assert.Equal (Key.A, key);

        // Negative case
        Assert.Throws<InvalidOperationException> (() => key = keyBindings.GetKeyFromCommands (Command.Left));
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
        Key resultKey = keyBindings.GetKeyFromCommands (Command.Default);
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

    // Add with scope does the right things
    [Theory]
    [InlineData (KeyBindingScope.Focused)]
    [InlineData (KeyBindingScope.HotKey)]
    [InlineData (KeyBindingScope.Application)]
    public void Scope_Add_Adds (KeyBindingScope scope)
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };

        var key = new Key (Key.A);
        keyBindings.Add (Key.A, scope, commands);
        KeyBinding binding = keyBindings.Get (key);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        binding = keyBindings.Get (key, scope);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        Command [] resultCommands = keyBindings.GetCommands (key);
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
        Command [] commands = { Command.Right, Command.Left };

        var key = new Key (Key.A);
        keyBindings.Add (key, scope, commands);
        KeyBinding binding = keyBindings.Get (key);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        binding = keyBindings.Get (key, scope);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        // negative test
        binding = keyBindings.Get (key, (KeyBindingScope)(-1));
        Assert.Null (binding);

        Command [] resultCommands = keyBindings.GetCommands (key);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    // TryGet
    [Fact]
    public void TryGet_Unknown_ReturnsFalse ()
    {
        var keyBindings = new KeyBindings ();
        bool result = keyBindings.TryGet (Key.A, out KeyBinding _);
        Assert.False (result);
    }

    [Fact]
    public void TryGet_WithCommands_ReturnsTrue ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, Command.Default);
        bool result = keyBindings.TryGet (Key.A, out KeyBinding bindings);
        Assert.True (result);
        Assert.Contains (Command.Default, bindings.Commands);
    }
}
