#nullable enable
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

        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

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

        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

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

        var view = new ScopedKeyBindingView ();
        app!.TopRunnableView!.Add (view);

        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

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

        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

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

        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

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

        var view = new View
        {
            CanFocus = true,
            HotKeySpecifier = new Rune ('_'),
            Title = "_Test"
        };
        app!.TopRunnableView!.Add (view);

        view.HandlingHotKey += (s, e) => hotKeyRaised++;
        view.Accepting += (s, e) => acceptRaised++;
        view.Activating += (s, e) => activateRaised++;

        Assert.Equal (KeyCode.T, view.HotKey);
        app.Keyboard.RaiseKeyDownEvent (Key.T);
        Assert.Equal(1, hotKeyRaised);
        Assert.Equal(0, acceptRaised);
        Assert.Equal(1, activateRaised);

        app.Keyboard.RaiseKeyDownEvent (Key.T.WithAlt);
        Assert.Equal (2, hotKeyRaised);
        Assert.Equal (0, acceptRaised);
        Assert.Equal (2, activateRaised);

        view.HotKey = KeyCode.E;
        app.Keyboard.RaiseKeyDownEvent(Key.E.WithAlt);
        Assert.Equal (3, hotKeyRaised);
        Assert.Equal (0, acceptRaised);
        Assert.Equal (3, activateRaised);
    }

    // tests that test KeyBindingScope.Focus and KeyBindingScope.HotKey (tests for KeyBindingScope.Application are in Application/KeyboardTests.cs)

    public class ScopedKeyBindingView : View
    {
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
        }

        public bool ApplicationCommandInvoked { get; set; }
        public bool FocusedCommandInvoked { get; set; }
        public bool HotKeyCommandInvoked { get; set; }
    }
}
