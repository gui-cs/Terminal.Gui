using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
///     Tests for View.KeyBindings
/// </summary>
public class KeyBindingsTests ()
{
    [Fact]
    [AutoInitShutdown]
    public void Focused_HotKey_Application_All_Work ()
    {
        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.RaiseKeyDownEvent (Key.A);
        Assert.False (keyWasHandled);
        Assert.True (view.ApplicationCommand);

        keyWasHandled = false;
        Application.RaiseKeyDownEvent (Key.H);
        Assert.True (view.HotKeyCommand);
        Assert.False (keyWasHandled);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        Application.RaiseKeyDownEvent (Key.F);
        Assert.False (keyWasHandled);
        Assert.False (view.FocusedCommand);

        keyWasHandled = false;
        view.CanFocus = true;
        view.SetFocus ();
        Assert.True (view.HasFocus);
        Application.RaiseKeyDownEvent (Key.F);
        Assert.True (view.FocusedCommand);
        Assert.False (keyWasHandled); // Command was invoked, but wasn't handled

        Assert.True (view.ApplicationCommand);
        Assert.True (view.HotKeyCommand);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBinding_Negative ()
    {
        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        Application.RaiseKeyDownEvent (Key.F);
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_KeyBinding ()
    {
        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        keyWasHandled = false;
        Application.RaiseKeyDownEvent (Key.H);
        Assert.True (view.HotKeyCommand);
        Assert.False (keyWasHandled);

        view.HotKey = KeyCode.Z;
        keyWasHandled = false;
        view.HotKeyCommand = false;
        Application.RaiseKeyDownEvent (Key.H); // old hot key
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommand);

        Application.RaiseKeyDownEvent (Key.Z); // new hot key
        Assert.True (view.HotKeyCommand);
        Assert.False (keyWasHandled);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_KeyBinding_Negative ()
    {
        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommand);

        keyWasHandled = false;
        Application.RaiseKeyDownEvent (Key.F);
        Assert.False (view.HotKeyCommand);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_Enabled_False_Does_Not_Invoke ()
    {
        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.HotKeyCommand);

        keyWasHandled = false;
        view.Enabled = false;
        Application.RaiseKeyDownEvent (Key.F);
        Assert.False (view.HotKeyCommand);
        top.Dispose ();
    }


    [Fact]
    public void HotKey_Raises_HotKeyCommand ()
    {
        var hotKeyRaised = false;
        var acceptRaised = false;
        var selectRaised = false;
        Application.Top = new Toplevel ();
        var view = new View
        {
            CanFocus = true,
            HotKeySpecifier = new Rune ('_'),
            Title = "_Test"
        };
        Application.Top.Add (view);
        view.HandlingHotKey += (s, e) => hotKeyRaised = true;
        view.Accepting += (s, e) => acceptRaised = true;
        view.Selecting += (s, e) => selectRaised = true;

        Assert.Equal (KeyCode.T, view.HotKey);
        Assert.True (Application.RaiseKeyDownEvent (Key.T));
        Assert.True (hotKeyRaised);
        Assert.False (acceptRaised);
        Assert.False (selectRaised);

        hotKeyRaised = false;
        Assert.True (Application.RaiseKeyDownEvent (Key.T.WithAlt));
        Assert.True (hotKeyRaised);
        Assert.False (acceptRaised);
        Assert.False (selectRaised);

        hotKeyRaised = false;
        view.HotKey = KeyCode.E;
        Assert.True (Application.RaiseKeyDownEvent (Key.E.WithAlt));
        Assert.True (hotKeyRaised);
        Assert.False (acceptRaised);
        Assert.False (selectRaised);

        Application.Top.Dispose ();
        Application.ResetState (true);
    }
    // tests that test KeyBindingScope.Focus and KeyBindingScope.HotKey (tests for KeyBindingScope.Application are in Application/KeyboardTests.cs)

    public class ScopedKeyBindingView : View
    {
        public ScopedKeyBindingView ()
        {
            AddCommand (Command.Save, () => ApplicationCommand = true);
            AddCommand (Command.HotKey, () => HotKeyCommand = true);
            AddCommand (Command.Left, () => FocusedCommand = true);

            Application.KeyBindings.Add (Key.A, this, Command.Save);
            HotKey = KeyCode.H;
            KeyBindings.Add (Key.F, Command.Left);
        }

        public bool ApplicationCommand { get; set; }
        public bool FocusedCommand { get; set; }
        public bool HotKeyCommand { get; set; }
    }
}
