#nullable enable
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
        Assert.True (view.ApplicationCommand);

        keyWasHandled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H);
        Assert.True (view.HotKeyCommand);
        Assert.False (keyWasHandled);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (keyWasHandled);
        Assert.False (view.FocusedCommand);

        keyWasHandled = false;
        view.CanFocus = true;
        view.SetFocus ();
        Assert.True (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.True (view.FocusedCommand);
        Assert.False (keyWasHandled); // Command was invoked, but wasn't handled

        Assert.True (view.ApplicationCommand);
        Assert.True (view.HotKeyCommand);
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
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);
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
        Assert.True (view.HotKeyCommand);
        Assert.False (keyWasHandled);

        view.HotKey = KeyCode.Z;
        keyWasHandled = false;
        view.HotKeyCommand = false;
        app.Keyboard.RaiseKeyDownEvent (Key.H); // old hot key
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommand);

        app.Keyboard.RaiseKeyDownEvent (Key.Z); // new hot key
        Assert.True (view.HotKeyCommand);
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
        Assert.False (view.HotKeyCommand);

        keyWasHandled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (view.HotKeyCommand);
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
        Assert.False (view.HotKeyCommand);

        keyWasHandled = false;
        view.Enabled = false;
        app.Keyboard.RaiseKeyDownEvent (Key.F);
        Assert.False (view.HotKeyCommand);
    }

    [Fact]
    public void HotKey_Raises_HotKeyCommand ()
    {
        IApplication? app = Application.Create ();
        app.Begin (new Runnable<bool> ());
        var hotKeyRaised = false;
        var acceptRaised = false;
        var selectRaised = false;

        var view = new View
        {
            CanFocus = true,
            HotKeySpecifier = new ('_'),
            Title = "_Test"
        };
        app!.TopRunnableView!.Add (view);

        view.HandlingHotKey += (s, e) => hotKeyRaised = true;
        view.Accepting += (s, e) => acceptRaised = true;
        view.Selecting += (s, e) => selectRaised = true;

        Assert.Equal (KeyCode.T, view.HotKey);
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.T));
        Assert.True (hotKeyRaised);
        Assert.False (acceptRaised);
        Assert.False (selectRaised);

        hotKeyRaised = false;
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.T.WithAlt));
        Assert.True (hotKeyRaised);
        Assert.False (acceptRaised);
        Assert.False (selectRaised);

        hotKeyRaised = false;
        view.HotKey = KeyCode.E;
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.E.WithAlt));
        Assert.True (hotKeyRaised);
        Assert.False (acceptRaised);
        Assert.False (selectRaised);
    }

    // tests that test KeyBindingScope.Focus and KeyBindingScope.HotKey (tests for KeyBindingScope.Application are in Application/KeyboardTests.cs)

    public class ScopedKeyBindingView : View
    {
        /// <inheritdoc/>
        public override void EndInit ()
        {
            base.EndInit ();
            AddCommand (Command.Save, () => ApplicationCommand = true);
            AddCommand (Command.HotKey, () => HotKeyCommand = true);
            AddCommand (Command.Left, () => FocusedCommand = true);

            App!.Keyboard.KeyBindings.Add (Key.A, this, Command.Save);
            HotKey = KeyCode.H;
            KeyBindings.Add (Key.F, Command.Left);
        }

        public bool ApplicationCommand { get; set; }
        public bool FocusedCommand { get; set; }
        public bool HotKeyCommand { get; set; }
    }
}
