using Xunit.Abstractions;

namespace Terminal.Gui.InputTests;

public class KeyBindingTests
{
    public KeyBindingTests (ITestOutputHelper output) { _output = output; }
    private readonly ITestOutputHelper _output;

    [Fact]
    public void Add_Invalid_Key_Throws ()
    {
        var keyBindings = new KeyBindings ();
        List<Command> commands = new ();
        Assert.Throws<ArgumentException> (() => keyBindings.Add (Key.Empty, KeyBindingScope.HotKey, Command.Accept));
    }

    [Fact]
    public void Add_Multiple_Adds ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };

        keyBindings.Add (Key.A, KeyBindingScope.Application, commands);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);

        keyBindings.Add (Key.B, KeyBindingScope.Application, commands);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void Add_No_Commands_Throws ()
    {
        var keyBindings = new KeyBindings ();
        List<Command> commands = new ();
        Assert.Throws<ArgumentException> (() => keyBindings.Add (Key.A, commands.ToArray ()));
    }

    [Fact]
    public void Add_Single_Adds ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, KeyBindingScope.Application, Command.HotKey);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);

        keyBindings.Add (Key.B, KeyBindingScope.Application, Command.HotKey);
        resultCommands = keyBindings.GetCommands (Key.B);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    // Add should not allow duplicates
    [Fact]
    public void Add_Throws_If_Exists ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, KeyBindingScope.Application, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => keyBindings.Add (Key.A, KeyBindingScope.Application, Command.Accept));

        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);

        keyBindings = new ();
        keyBindings.Add (Key.A, KeyBindingScope.Focused, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => keyBindings.Add (Key.A, KeyBindingScope.Focused, Command.Accept));

        resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);

        keyBindings = new ();
        keyBindings.Add (Key.A, KeyBindingScope.HotKey, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => keyBindings.Add (Key.A, KeyBindingScope.Focused, Command.Accept));

        resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);

        keyBindings = new ();
        keyBindings.Add (Key.A, new KeyBinding (new [] { Command.HotKey }, KeyBindingScope.HotKey));
        Assert.Throws<InvalidOperationException> (() => keyBindings.Add (Key.A, new KeyBinding (new [] { Command.Accept }, KeyBindingScope.HotKey)));

        resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    // Clear
    [Fact]
    public void Clear_Clears ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.B, KeyBindingScope.Application, Command.HotKey);
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
        Assert.Empty (keyBindings.Bindings);
        Assert.Null (keyBindings.GetKeyFromCommands (Command.Accept));
        Assert.Null (keyBindings.BoundView);
    }

    [Fact]
    public void Get_Binding_Not_Found_Throws ()
    {
        var keyBindings = new KeyBindings ();
        Assert.Throws<InvalidOperationException> (() => keyBindings.Get (Key.A));
        Assert.Throws<InvalidOperationException> (() => keyBindings.Get (Key.B, KeyBindingScope.Application));
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
        keyBindings.Add (Key.A, KeyBindingScope.Application, Command.HotKey);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    [Fact]
    public void GetCommands_WithMultipleBindings_ReturnsCommands ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };
        keyBindings.Add (Key.A, KeyBindingScope.Application, commands);
        keyBindings.Add (Key.B, KeyBindingScope.Application, commands);
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
        keyBindings.Add (Key.A, KeyBindingScope.Application, commands);
        Command [] resultCommands = keyBindings.GetCommands (Key.A);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void GetKeyFromCommands_MultipleCommands ()
    {
        var keyBindings = new KeyBindings ();
        Command [] commands1 = { Command.Right, Command.Left };
        keyBindings.Add (Key.A, KeyBindingScope.Application, commands1);

        Command [] commands2 = { Command.Up, Command.Down };
        keyBindings.Add (Key.B, KeyBindingScope.Application, commands2);

        Key key = keyBindings.GetKeyFromCommands (commands1);
        Assert.Equal (Key.A, key);

        key = keyBindings.GetKeyFromCommands (commands2);
        Assert.Equal (Key.B, key);
    }

    [Fact]
    public void GetKeyFromCommands_OneCommand ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, KeyBindingScope.Application, Command.Right);

        Key key = keyBindings.GetKeyFromCommands (Command.Right);
        Assert.Equal (Key.A, key);
    }

    // GetKeyFromCommands
    [Fact]
    public void GetKeyFromCommands_Unknown_Returns_Key_Empty ()
    {
        var keyBindings = new KeyBindings ();
        Assert.Null (keyBindings.GetKeyFromCommands (Command.Accept));
    }

    [Fact]
    public void GetKeyFromCommands_WithCommands_ReturnsKey ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, KeyBindingScope.Application, Command.HotKey);
        Key resultKey = keyBindings.GetKeyFromCommands (Command.HotKey);
        Assert.Equal (Key.A, resultKey);
    }

    [Fact]
    public void ReplaceKey_Replaces ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, KeyBindingScope.Application, Command.HotKey);
        keyBindings.Add (Key.B, KeyBindingScope.Application, Command.HotKey);
        keyBindings.Add (Key.C, KeyBindingScope.Application, Command.HotKey);
        keyBindings.Add (Key.D, KeyBindingScope.Application, Command.HotKey);

        keyBindings.ReplaceKey (Key.A, Key.E);
        Assert.Empty (keyBindings.GetCommands (Key.A));
        Assert.Contains (Command.HotKey, keyBindings.GetCommands (Key.E));

        keyBindings.ReplaceKey (Key.B, Key.F);
        Assert.Empty (keyBindings.GetCommands (Key.B));
        Assert.Contains (Command.HotKey, keyBindings.GetCommands (Key.F));

        keyBindings.ReplaceKey (Key.C, Key.G);
        Assert.Empty (keyBindings.GetCommands (Key.C));
        Assert.Contains (Command.HotKey, keyBindings.GetCommands (Key.G));

        keyBindings.ReplaceKey (Key.D, Key.H);
        Assert.Empty (keyBindings.GetCommands (Key.D));
        Assert.Contains (Command.HotKey, keyBindings.GetCommands (Key.H));
    }

    [Fact]
    public void ReplaceKey_Replaces_Leaves_Old_Binding ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, KeyBindingScope.Application, Command.Accept);
        keyBindings.Add (Key.B, KeyBindingScope.Application, Command.HotKey);

        keyBindings.ReplaceKey (keyBindings.GetKeyFromCommands (Command.Accept), Key.C);
        Assert.Empty (keyBindings.GetCommands (Key.A));
        Assert.Contains (Command.Accept, keyBindings.GetCommands (Key.C));
    }

    [Fact]
    public void ReplaceKey_Throws_If_DoesNotContain_Old ()
    {
        var keyBindings = new KeyBindings ();
        Assert.Throws<InvalidOperationException> (() => keyBindings.ReplaceKey (Key.A, Key.B));
    }

    [Fact]
    public void ReplaceKey_Throws_If_New_Is_Empty ()
    {
        var keyBindings = new KeyBindings ();
        keyBindings.Add (Key.A, KeyBindingScope.Application, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => keyBindings.ReplaceKey (Key.A, Key.Empty));
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
    }

    [Theory]
    [InlineData (KeyBindingScope.Focused)]
    [InlineData (KeyBindingScope.HotKey)]
    [InlineData (KeyBindingScope.Application)]
    public void Scope_TryGet_Filters (KeyBindingScope scope)
    {
        var keyBindings = new KeyBindings ();
        Command [] commands = { Command.Right, Command.Left };

        var key = new Key (Key.A);
        keyBindings.Add (key, scope, commands);
        bool success = keyBindings.TryGet (key, out KeyBinding binding);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        success = keyBindings.TryGet (key, scope, out binding);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        // negative test
        success = keyBindings.TryGet (key, 0, out binding);
        Assert.False (success);

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
        keyBindings.Add (Key.A, KeyBindingScope.Application, Command.HotKey);
        bool result = keyBindings.TryGet (Key.A, out KeyBinding bindings);
        Assert.True (result);
        Assert.Contains (Command.HotKey, bindings.Commands);
    }
}
