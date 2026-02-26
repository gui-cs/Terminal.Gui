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
    ///     Replicates issue #4759: A View several levels down has a HotKeyBinding for Key.Space.
    ///     A *different* focused view (plain View with default Space→Activate binding) swallows
    ///     the key via DefaultActivateHandler returning true, preventing the HotKey from firing.
    ///     This test mirrors @mrazza's scenario: the HotKey should fire regardless of which
    ///     view is focused, unless that view explicitly handles the key.
    /// </summary>
    [Fact]
    public void Space_HotKey_Fires_When_Sibling_View_Is_Focused ()
    {
        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        // The view with the Space HotKeyBinding — a few levels down, like mrazza's scenario
        View container = new () { CanFocus = false };
        View innerContainer = new () { CanFocus = false };
        View hotKeyView = new () { CanFocus = false };
        var hotKeyFired = false;
        hotKeyView.HandlingHotKey += (_, _) => hotKeyFired = true;
        hotKeyView.HotKeyBindings.Add (Key.Space, Command.HotKey);

        container.Add (innerContainer);
        innerContainer.Add (hotKeyView);
        app!.TopRunnableView!.Add (container);

        // A separate focusable view (sibling) — plain View with default Space→Activate binding
        View focusableView = new () { CanFocus = true };
        app!.TopRunnableView!.Add (focusableView);

        // With no view focused, Space HotKey should fire
        hotKeyFired = false;
        app.Keyboard.RaiseKeyDownEvent (Key.Space);
        Assert.True (hotKeyFired);

        // Focus the sibling view — it has default Space→Command.Activate from SetupKeyboard
        focusableView.SetFocus ();
        Assert.True (focusableView.HasFocus);

        // Space HotKey should STILL fire — the focused view's Activate is unhandled/default
        // BUG: DefaultActivateHandler returns true unconditionally, swallowing Space
        // before InvokeCommandsBoundToHotKey runs on the SuperView hierarchy
        hotKeyFired = false;
        app.Keyboard.RaiseKeyDownEvent (Key.Space);
        Assert.True (hotKeyFired);
    }

    // Claude - Opus 4.5
    /// <summary>
    ///     Verifies that Enter (Command.Accept) as a HotKeyBinding works even when another
    ///     view is focused. This is the asymmetry noted in #4759: DefaultAcceptHandler returns
    ///     false for plain Views, so Enter leaks through to HotKey dispatch, while Space doesn't.
    /// </summary>
    [Fact]
    public void Enter_HotKey_Fires_When_Sibling_View_Is_Focused ()
    {
        IApplication app = Application.Create ();
        app.Begin (new Runnable<bool> { CanFocus = true });

        // View with Enter HotKeyBinding
        View hotKeyView = new () { CanFocus = false };
        var hotKeyFired = false;
        hotKeyView.HandlingHotKey += (_, _) => hotKeyFired = true;
        hotKeyView.HotKeyBindings.Add (Key.Enter, Command.HotKey);
        app!.TopRunnableView!.Add (hotKeyView);

        // Separate focusable view with default Enter→Command.Accept
        View focusableView = new () { CanFocus = true };
        app!.TopRunnableView!.Add (focusableView);

        focusableView.SetFocus ();
        Assert.True (focusableView.HasFocus);

        // Enter HotKey fires because DefaultAcceptHandler returns false for plain Views
        hotKeyFired = false;
        app.Keyboard.RaiseKeyDownEvent (Key.Enter);
        Assert.True (hotKeyFired);
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
