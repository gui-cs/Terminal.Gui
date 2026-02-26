using System.Text;

namespace ViewBaseTests.Keyboard;

/// <summary>
///     Tests for View.KeyBindings
/// </summary>
public class KeyBindingsTests
{
    [Fact]
    public void Focused_HotKey_Application_All_Work ()
    {
        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        ScopedKeyBindingView view = new ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        app!.TopRunnableView!.Add (view);

        app.Keyboard.RaiseKeyDownEvent (Key.A);
        Assert.False (keyWasHandled);
        Assert.True (view.ApplicationCommandInvoked);

        keyWasHandled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.True (view.HotKeyCommandInvoked);
        Assert.False (keyWasHandled);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (keyWasHandled);
        Assert.False (view.FocusedCommandInvoked);

        keyWasHandled = false;
        view.CanFocus = true;
        view.SetFocus ();
        Assert.True (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.True (view.FocusedCommandInvoked);
        Assert.False (keyWasHandled); // Command was invoked, but wasn't handled

        Assert.True (view.ApplicationCommandInvoked);
        Assert.True (view.HotKeyCommandInvoked);
    }

    [Fact]
    public void KeyBinding_Negative ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        ScopedKeyBindingView view = new ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        app.Keyboard.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommandInvoked);
        Assert.False (view.HotKeyCommandInvoked);
        Assert.False (view.FocusedCommandInvoked);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommandInvoked);
        Assert.False (view.HotKeyCommandInvoked);
        Assert.False (view.FocusedCommandInvoked);
    }

    [Fact]
    public void HotKey_KeyBinding ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        ScopedKeyBindingView view = new ();
        app!.TopRunnableView!.Add (view);

        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        keyWasHandled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.True (view.HotKeyCommandInvoked);
        Assert.False (keyWasHandled);

        view.HotKey = KeyCode.Z;
        keyWasHandled = false;
        view.HotKeyCommandInvoked = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H); // old hot key
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommandInvoked);

        app.Keyboard.RaiseKeyDownEvent (Key.Z); // new hot key
        Assert.True (view.HotKeyCommandInvoked);
        Assert.False (keyWasHandled);
    }

    [Fact]
    public void HotKey_KeyBinding_Negative ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        ScopedKeyBindingView view = new ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        app.Keyboard.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommandInvoked);

        keyWasHandled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (view.HotKeyCommandInvoked);
    }

    [Fact]
    public void HotKey_Enabled_False_Does_Not_Invoke ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> ());

        ScopedKeyBindingView view = new ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (_, _) => keyWasHandled = true;

        app!.TopRunnableView!.Add (view);

        app.Keyboard.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommandInvoked);

        keyWasHandled = false;
        view.Enabled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (view.HotKeyCommandInvoked);
    }

    [Fact]
    public void HotKey_Raises_HandlingHotKey ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> ());
        var hotKeyRaised = 0;
        var acceptRaised = 0;
        var activateRaised = 0;

        View view = new () { CanFocus = true, HotKeySpecifier = new Rune ('_'), Title = "_Test" };
        app!.TopRunnableView!.Add (view);

        view.HandlingHotKey += (_, _) => hotKeyRaised++;
        view.Accepting += (_, _) => acceptRaised++;
        view.Activating += (_, _) => activateRaised++;

        Assert.Equal (KeyCode.T, view.HotKey);
        app.Keyboard.RaiseKeyDownEvent (Key.T);
        Assert.Equal (1, hotKeyRaised);
        Assert.Equal (0, acceptRaised);
        Assert.Equal (1, activateRaised);

        app.Keyboard.RaiseKeyDownEvent (Key.T.WithAlt);
        Assert.Equal (2, hotKeyRaised);
        Assert.Equal (0, acceptRaised);
        Assert.Equal (2, activateRaised);

        view.HotKey = KeyCode.E;
        app.Keyboard.RaiseKeyDownEvent (Key.E.WithAlt);
        Assert.Equal (3, hotKeyRaised);
        Assert.Equal (0, acceptRaised);
        Assert.Equal (3, activateRaised);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Demonstrates that Space as a HotKeyBinding is swallowed by the focused view's
    ///     Command.Activate KeyBinding (bound to Space in SetupKeyboard), so the HotKey never fires.
    ///     This is the bug reported in issue #4759.
    /// </summary>
    [Fact]
    public void Focused_Space_HotKey_Is_Swallowed_By_Focused_View ()
    {
        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        ScopedKeyBindingView view = new () { ExtraHotKey = Key.Space };
        app!.TopRunnableView!.Add (view);

        // Verify Key.A (application-scoped) still works
        app.Keyboard.RaiseKeyDownEvent (Key.A);
        Assert.True (view.ApplicationCommandInvoked);

        // Verify Key.H (title-derived hotkey) still works
        view.HotKeyCommandInvoked = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.True (view.HotKeyCommandInvoked);

        // Space HotKey should fire when no view is focused that binds Space
        view.HotKeyCommandInvoked = false;
        Assert.False (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.Space);
        Assert.True (view.HotKeyCommandInvoked);

        // Now focus the view - Space should STILL fire the HotKey, but currently doesn't
        // because DefaultActivateHandler (Command.Activate bound to Space) returns true,
        // swallowing the key before HotKey dispatch runs.
        view.HotKeyCommandInvoked = false;
        view.CanFocus = true;
        view.SetFocus ();
        Assert.True (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.Space);

        // BUG: This fails - Space is consumed by Command.Activate on the focused view
        Assert.True (view.HotKeyCommandInvoked);
    }

    // tests that test KeyBindingScope.Focus and KeyBindingScope.HotKey (tests for KeyBindingScope.Application are in Application/KeyboardTests.cs)

    public class ScopedKeyBindingView : View
    {
        /// <summary>
        ///     If set, an additional HotKeyBinding is added via <see cref="View.HotKeyBindings"/> for this key.
        ///     This bypasses <see cref="View.AddKeyBindingsForHotKey"/> validation, allowing keys like Space.
        /// </summary>
        public Key? ExtraHotKey { get; set; }

        /// <inheritdoc/>
        public override void EndInit ()
        {
            base.EndInit ();
            AddCommand (Command.Save, () => ApplicationCommandInvoked = true);
            AddCommand (Command.HotKey, () => HotKeyCommandInvoked = true);
            AddCommand (Command.Left, () => FocusedCommandInvoked = true);

            App!.Keyboard.KeyBindings.AddApp (Key.A, this, Command.Save);
            HotKey = KeyCode.H;
            KeyBindings.Add (Key.F, Command.Left);

            if (ExtraHotKey is { })
            {
                HotKeyBindings.Add (ExtraHotKey, Command.HotKey);
            }
        }

        public bool ApplicationCommandInvoked { get; set; }
        public bool FocusedCommandInvoked { get; set; }
        public bool HotKeyCommandInvoked { get; set; }
    }
}
