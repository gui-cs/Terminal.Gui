#nullable enable
using Terminal.Gui.App;

namespace UnitTests_Parallelizable.ApplicationTests;

/// <summary>
///     Parallelizable tests for keyboard handling.
///     These tests use isolated instances of <see cref="IKeyboard"/> to avoid static state dependencies.
/// </summary>
public class KeyboardTests
{
    [Fact]
    public void Constructor_InitializesKeyBindings ()
    {
        // Arrange & Act
        var keyboard = new Keyboard ();

        // Assert
        Assert.NotNull (keyboard.KeyBindings);
        // Verify that some default bindings exist
        Assert.True (keyboard.KeyBindings.TryGet (keyboard.QuitKey, out _));
    }

    [Fact]
    public void QuitKey_DefaultValue_IsEsc ()
    {
        // Arrange
        var keyboard = new Keyboard ();

        // Assert
        Assert.Equal (Key.Esc, keyboard.QuitKey);
    }

    [Fact]
    public void QuitKey_SetValue_UpdatesKeyBindings ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key newQuitKey = Key.Q.WithCtrl;

        // Act
        keyboard.QuitKey = newQuitKey;

        // Assert
        Assert.Equal (newQuitKey, keyboard.QuitKey);
        Assert.True (keyboard.KeyBindings.TryGet (newQuitKey, out KeyBinding binding));
        Assert.Contains (Command.Quit, binding.Commands);
    }

    [Fact]
    public void ArrangeKey_DefaultValue_IsCtrlF5 ()
    {
        // Arrange
        var keyboard = new Keyboard ();

        // Assert
        Assert.Equal (Key.F5.WithCtrl, keyboard.ArrangeKey);
    }

    [Fact]
    public void NextTabKey_DefaultValue_IsTab ()
    {
        // Arrange
        var keyboard = new Keyboard ();

        // Assert
        Assert.Equal (Key.Tab, keyboard.NextTabKey);
    }

    [Fact]
    public void PrevTabKey_DefaultValue_IsShiftTab ()
    {
        // Arrange
        var keyboard = new Keyboard ();

        // Assert
        Assert.Equal (Key.Tab.WithShift, keyboard.PrevTabKey);
    }

    [Fact]
    public void NextTabGroupKey_DefaultValue_IsF6 ()
    {
        // Arrange
        var keyboard = new Keyboard ();

        // Assert
        Assert.Equal (Key.F6, keyboard.NextTabGroupKey);
    }

    [Fact]
    public void PrevTabGroupKey_DefaultValue_IsShiftF6 ()
    {
        // Arrange
        var keyboard = new Keyboard ();

        // Assert
        Assert.Equal (Key.F6.WithShift, keyboard.PrevTabGroupKey);
    }

    [Fact]
    public void KeyBindings_Add_CanAddCustomBinding ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key customKey = Key.K.WithCtrl;

        // Act
        keyboard.KeyBindings.Add (customKey, Command.Accept);

        // Assert
        Assert.True (keyboard.KeyBindings.TryGet (customKey, out KeyBinding binding));
        Assert.Contains (Command.Accept, binding.Commands);
    }

    [Fact]
    public void KeyBindings_Remove_CanRemoveBinding ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key customKey = Key.K.WithCtrl;
        keyboard.KeyBindings.Add (customKey, Command.Accept);

        // Act
        keyboard.KeyBindings.Remove (customKey);

        // Assert
        Assert.False (keyboard.KeyBindings.TryGet (customKey, out _));
    }

    [Fact]
    public void KeyDown_Event_CanBeSubscribed ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        bool eventRaised = false;

        // Act
        keyboard.KeyDown += (sender, key) =>
        {
            eventRaised = true;
        };

        // Assert - event subscription doesn't throw
        Assert.False (eventRaised); // Event hasn't been raised yet
    }

    [Fact]
    public void KeyUp_Event_CanBeSubscribed ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        bool eventRaised = false;

        // Act
        keyboard.KeyUp += (sender, key) =>
        {
            eventRaised = true;
        };

        // Assert - event subscription doesn't throw
        Assert.False (eventRaised); // Event hasn't been raised yet
    }

    [Fact]
    public void InvokeCommand_WithInvalidCommand_ThrowsNotSupportedException ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        // Pick a command that isn't registered
        Command invalidCommand = (Command)9999;
        Key testKey = Key.A;
        var binding = new KeyBinding ([invalidCommand]);

        // Act & Assert
        Assert.Throws<NotSupportedException> (() => keyboard.InvokeCommand (invalidCommand, testKey, binding));
    }

    [Fact]
    public void Multiple_Keyboards_CanExistIndependently ()
    {
        // Arrange & Act
        var keyboard1 = new Keyboard ();
        var keyboard2 = new Keyboard ();

        keyboard1.QuitKey = Key.Q.WithCtrl;
        keyboard2.QuitKey = Key.X.WithCtrl;

        // Assert - each keyboard maintains independent state
        Assert.Equal (Key.Q.WithCtrl, keyboard1.QuitKey);
        Assert.Equal (Key.X.WithCtrl, keyboard2.QuitKey);
        Assert.NotEqual (keyboard1.QuitKey, keyboard2.QuitKey);
    }

    [Fact]
    public void KeyBindings_Replace_UpdatesExistingBinding ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key oldKey = Key.Esc;
        Key newKey = Key.Q.WithCtrl;

        // Verify initial state
        Assert.True (keyboard.KeyBindings.TryGet (oldKey, out KeyBinding oldBinding));
        Assert.Contains (Command.Quit, oldBinding.Commands);

        // Act
        keyboard.KeyBindings.Replace (oldKey, newKey);

        // Assert - old key should no longer have the binding
        Assert.False (keyboard.KeyBindings.TryGet (oldKey, out _));
        // New key should have the binding
        Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding newBinding));
        Assert.Contains (Command.Quit, newBinding.Commands);
    }

    [Fact]
    public void KeyBindings_Clear_RemovesAllBindings ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        // Verify initial state has bindings
        Assert.True (keyboard.KeyBindings.TryGet (keyboard.QuitKey, out _));

        // Act
        keyboard.KeyBindings.Clear ();

        // Assert - previously existing binding is gone
        Assert.False (keyboard.KeyBindings.TryGet (keyboard.QuitKey, out _));
    }

    [Fact]
    public void AddKeyBindings_PopulatesDefaultBindings ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        keyboard.KeyBindings.Clear ();
        Assert.False (keyboard.KeyBindings.TryGet (keyboard.QuitKey, out _));

        // Act
        keyboard.AddKeyBindings ();

        // Assert
        Assert.True (keyboard.KeyBindings.TryGet (keyboard.QuitKey, out KeyBinding binding));
        Assert.Contains (Command.Quit, binding.Commands);
    }

    // Migrated from UnitTests/Application/KeyboardTests.cs
    
    [Fact]
    public void KeyBindings_Add_Adds ()
    {
        // Arrange
        var keyboard = new Keyboard ();

        // Act
        keyboard.KeyBindings.Add (Key.A, Command.Accept);
        keyboard.KeyBindings.Add (Key.B, Command.Accept);

        // Assert
        Assert.True (keyboard.KeyBindings.TryGet (Key.A, out KeyBinding binding));
        Assert.Null (binding.Target);
        Assert.True (keyboard.KeyBindings.TryGet (Key.B, out binding));
        Assert.Null (binding.Target);
    }

    [Fact]
    public void KeyBindings_Remove_Removes ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        keyboard.KeyBindings.Add (Key.A, Command.Accept);
        Assert.True (keyboard.KeyBindings.TryGet (Key.A, out _));

        // Act
        keyboard.KeyBindings.Remove (Key.A);

        // Assert
        Assert.False (keyboard.KeyBindings.TryGet (Key.A, out _));
    }

    [Fact]
    public void QuitKey_Default_Is_Esc ()
    {
        // Arrange & Act
        var keyboard = new Keyboard ();

        // Assert
        Assert.Equal (Key.Esc, keyboard.QuitKey);
    }

    [Fact]
    public void QuitKey_Setter_UpdatesBindings ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key prevKey = keyboard.QuitKey;

        // Act - Change QuitKey
        keyboard.QuitKey = Key.C.WithCtrl;

        // Assert - Old key should no longer trigger quit
        Assert.False (keyboard.KeyBindings.TryGet (prevKey, out _));
        // New key should trigger quit
        Assert.True (keyboard.KeyBindings.TryGet (Key.C.WithCtrl, out KeyBinding binding));
        Assert.Contains (Command.Quit, binding.Commands);
    }

    [Fact]
    public void NextTabKey_Setter_UpdatesBindings ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key prevKey = keyboard.NextTabKey;
        Key newKey = Key.N.WithCtrl;

        // Act
        keyboard.NextTabKey = newKey;

        // Assert
        Assert.Equal (newKey, keyboard.NextTabKey);
        Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
        Assert.Contains (Command.NextTabStop, binding.Commands);
    }

    [Fact]
    public void PrevTabKey_Setter_UpdatesBindings ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key newKey = Key.P.WithCtrl;

        // Act
        keyboard.PrevTabKey = newKey;

        // Assert
        Assert.Equal (newKey, keyboard.PrevTabKey);
        Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
        Assert.Contains (Command.PreviousTabStop, binding.Commands);
    }

    [Fact]
    public void NextTabGroupKey_Setter_UpdatesBindings ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key newKey = Key.PageDown.WithCtrl;

        // Act
        keyboard.NextTabGroupKey = newKey;

        // Assert
        Assert.Equal (newKey, keyboard.NextTabGroupKey);
        Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, keyboard.NextTabGroupKey.KeyCode);
        Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
        Assert.Contains (Command.NextTabGroup, binding.Commands);
    }

    [Fact]
    public void PrevTabGroupKey_Setter_UpdatesBindings ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key newKey = Key.PageUp.WithCtrl;

        // Act
        keyboard.PrevTabGroupKey = newKey;

        // Assert
        Assert.Equal (newKey, keyboard.PrevTabGroupKey);
        Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, keyboard.PrevTabGroupKey.KeyCode);
        Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
        Assert.Contains (Command.PreviousTabGroup, binding.Commands);
    }

    [Fact]
    public void ArrangeKey_Setter_UpdatesBindings ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key newKey = Key.A.WithCtrl;

        // Act
        keyboard.ArrangeKey = newKey;

        // Assert
        Assert.Equal (newKey, keyboard.ArrangeKey);
        Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
        Assert.Contains (Command.Arrange, binding.Commands);
    }

    [Fact]
    public void KeyBindings_AddWithTarget_StoresTarget ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        var view = new View ();

        // Act
        keyboard.KeyBindings.Add (Key.A.WithCtrl, view, Command.Accept);

        // Assert
        Assert.True (keyboard.KeyBindings.TryGet (Key.A.WithCtrl, out KeyBinding binding));
        Assert.Equal (view, binding.Target);
        Assert.Contains (Command.Accept, binding.Commands);

        view.Dispose ();
    }

    [Fact]
    public void InvokeCommandsBoundToKey_ReturnsNull_WhenNoBindingExists ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key unboundKey = Key.Z.WithAlt.WithCtrl;

        // Act
        bool? result = keyboard.InvokeCommandsBoundToKey (unboundKey);

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void InvokeCommandsBoundToKey_InvokesCommand_WhenBindingExists ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        // QuitKey has a bound command by default

        // Act
        bool? result = keyboard.InvokeCommandsBoundToKey (keyboard.QuitKey);

        // Assert
        // Command.Quit would normally call Application.RequestStop, 
        // but in isolation it should return true (handled)
        Assert.NotNull (result);
    }

    [Fact]
    public void Multiple_Keyboards_Independent_KeyBindings ()
    {
        // Arrange
        var keyboard1 = new Keyboard ();
        var keyboard2 = new Keyboard ();

        // Act
        keyboard1.KeyBindings.Add (Key.X, Command.Accept);
        keyboard2.KeyBindings.Add (Key.Y, Command.Cancel);

        // Assert
        Assert.True (keyboard1.KeyBindings.TryGet (Key.X, out _));
        Assert.False (keyboard1.KeyBindings.TryGet (Key.Y, out _));

        Assert.True (keyboard2.KeyBindings.TryGet (Key.Y, out _));
        Assert.False (keyboard2.KeyBindings.TryGet (Key.X, out _));
    }

    [Fact]
    public void KeyBindings_Replace_PreservesCommandsForNewKey ()
    {
        // Arrange
        var keyboard = new Keyboard ();
        Key oldKey = Key.Esc;
        Key newKey = Key.Q.WithCtrl;

        // Get the commands from the old binding
        Assert.True (keyboard.KeyBindings.TryGet (oldKey, out KeyBinding oldBinding));
        Command[] oldCommands = oldBinding.Commands.ToArray ();

        // Act
        keyboard.KeyBindings.Replace (oldKey, newKey);

        // Assert - new key should have the same commands
        Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding newBinding));
        Assert.Equal (oldCommands, newBinding.Commands);
    }
}
