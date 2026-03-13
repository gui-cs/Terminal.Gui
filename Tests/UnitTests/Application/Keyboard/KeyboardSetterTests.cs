// Tests that mutate Application.DefaultKeyBindings (static state).
// These MUST NOT be in UnitTestsParallelizable.

namespace UnitTests.ApplicationTests.Keyboard;

public class KeyboardSetterTests
{
    [Fact]
    public void QuitKey_SetValue_UpdatesKeyBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.Quit];
        var keyboard = new ApplicationKeyboard ();
        Key newQuitKey = Key.Q.WithCtrl;

        try
        {
            // Act — mutate dict then re-add bindings (dict mutation doesn't fire the event)
            Application.DefaultKeyBindings! [Command.Quit] = Bind.All (newQuitKey);
            keyboard.AddKeyBindings ();

            // Assert
            Assert.Equal (newQuitKey, Application.GetDefaultKey (Command.Quit));
            Assert.True (keyboard.KeyBindings.TryGet (newQuitKey, out KeyBinding binding));
            Assert.Contains (Command.Quit, binding.Commands);
        }
        finally
        {
            Application.DefaultKeyBindings! [Command.Quit] = original;
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void NextTabKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.NextTabStop];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.N.WithCtrl;

        try
        {
            // Act
            Application.DefaultKeyBindings! [Command.NextTabStop] = Bind.All (newKey);
            keyboard.AddKeyBindings ();

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.NextTabStop));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.NextTabStop, binding.Commands);
        }
        finally
        {
            Application.DefaultKeyBindings! [Command.NextTabStop] = original;
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void PrevTabKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.PreviousTabStop];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.P.WithCtrl;

        try
        {
            // Act
            Application.DefaultKeyBindings! [Command.PreviousTabStop] = Bind.All (newKey);
            keyboard.AddKeyBindings ();

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.PreviousTabStop));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.PreviousTabStop, binding.Commands);
        }
        finally
        {
            Application.DefaultKeyBindings! [Command.PreviousTabStop] = original;
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void NextTabGroupKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.NextTabGroup];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.PageDown.WithCtrl;

        try
        {
            // Act
            Application.DefaultKeyBindings! [Command.NextTabGroup] = Bind.All (newKey);
            keyboard.AddKeyBindings ();

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.NextTabGroup));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.NextTabGroup, binding.Commands);
        }
        finally
        {
            Application.DefaultKeyBindings! [Command.NextTabGroup] = original;
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void PrevTabGroupKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.PreviousTabGroup];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.PageUp.WithCtrl;

        try
        {
            // Act
            Application.DefaultKeyBindings! [Command.PreviousTabGroup] = Bind.All (newKey);
            keyboard.AddKeyBindings ();

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.PreviousTabGroup));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.PreviousTabGroup, binding.Commands);
        }
        finally
        {
            Application.DefaultKeyBindings! [Command.PreviousTabGroup] = original;
            keyboard.Dispose ();
        }
    }

    [Fact]
    public void ArrangeKey_Setter_UpdatesBindings ()
    {
        // Arrange
        PlatformKeyBinding original = Application.DefaultKeyBindings! [Command.Arrange];
        var keyboard = new ApplicationKeyboard ();
        Key newKey = Key.A.WithCtrl;

        try
        {
            // Act
            Application.DefaultKeyBindings! [Command.Arrange] = Bind.All (newKey);
            keyboard.AddKeyBindings ();

            // Assert
            Assert.Equal (newKey, Application.GetDefaultKey (Command.Arrange));
            Assert.True (keyboard.KeyBindings.TryGet (newKey, out KeyBinding binding));
            Assert.Contains (Command.Arrange, binding.Commands);
        }
        finally
        {
            Application.DefaultKeyBindings! [Command.Arrange] = original;
            keyboard.Dispose ();
        }
    }
}
