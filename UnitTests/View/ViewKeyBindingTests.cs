using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ViewKeyBindingTests
{
    private readonly ITestOutputHelper _output;
    public ViewKeyBindingTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void Focus_KeyBinding ()
    {
        var view = new ScopedKeyBindingView ();
        var invoked = false;
        view.InvokingKeyBindings += (s, e) => invoked = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.OnKeyDown (Key.A);
        Assert.True (invoked);

        invoked = false;
        Application.OnKeyDown (Key.H);
        Assert.True (invoked);

        invoked = false;
        Assert.False (view.HasFocus);
        Application.OnKeyDown (Key.F);
        Assert.False (invoked);
        Assert.False (view.FocusedCommand);

        invoked = false;
        view.CanFocus = true;
        view.SetFocus ();
        Assert.True (view.HasFocus);
        Application.OnKeyDown (Key.F);
        Assert.True (invoked);

        Assert.True (view.ApplicationCommand);
        Assert.True (view.HotKeyCommand);
        Assert.True (view.FocusedCommand);
    }

    [Fact]
    [AutoInitShutdown]
    public void Focus_KeyBinding_Negative ()
    {
        var view = new ScopedKeyBindingView ();
        var invoked = false;
        view.InvokingKeyBindings += (s, e) => invoked = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.OnKeyDown (Key.Z);
        Assert.False (invoked);
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);

        invoked = false;
        Assert.False (view.HasFocus);
        Application.OnKeyDown (Key.F);
        Assert.False (invoked);
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_KeyBinding ()
    {
        var view = new ScopedKeyBindingView ();
        var invoked = false;
        view.InvokingKeyBindings += (s, e) => invoked = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        invoked = false;
        Application.OnKeyDown (Key.H);
        Assert.True (invoked);
        Assert.True (view.HotKeyCommand);

        view.HotKey = KeyCode.Z;
        invoked = false;
        view.HotKeyCommand = false;
        Application.OnKeyDown (Key.H); // old hot key
        Assert.False (invoked);
        Assert.False (view.HotKeyCommand);

        Application.OnKeyDown (Key.Z); // new hot key
        Assert.True (invoked);
        Assert.True (view.HotKeyCommand);
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_KeyBinding_Negative ()
    {
        var view = new ScopedKeyBindingView ();
        var invoked = false;
        view.InvokingKeyBindings += (s, e) => invoked = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.OnKeyDown (Key.Z);
        Assert.False (invoked);
        Assert.False (view.HotKeyCommand);

        invoked = false;
        Application.OnKeyDown (Key.F);
        Assert.False (view.HotKeyCommand);
    }

    // tests that test KeyBindingScope.Focus and KeyBindingScope.HotKey (tests for KeyBindingScope.Application are in Application/KeyboardTests.cs)

    public class ScopedKeyBindingView : View
    {
        public ScopedKeyBindingView ()
        {
            AddCommand (Command.Save, () => ApplicationCommand = true);
            AddCommand (Command.HotKey, () => HotKeyCommand = true);
            AddCommand (Command.Left, () => FocusedCommand = true);

            KeyBindings.Add (Key.A, KeyBindingScope.Application, Command.Save);
            HotKey = KeyCode.H;
            KeyBindings.Add (Key.F, KeyBindingScope.Focused, Command.Left);
        }

        public bool ApplicationCommand { get; set; }
        public bool FocusedCommand { get; set; }
        public bool HotKeyCommand { get; set; }
    }
}
