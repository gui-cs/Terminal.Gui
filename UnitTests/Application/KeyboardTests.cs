using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests;

public class KeyboardTests
{
    private readonly ITestOutputHelper _output;

    public KeyboardTests (ITestOutputHelper output)
    {
        _output = output;
#if DEBUG_IDISPOSABLE
        Responder.Instances.Clear ();
        RunState.Instances.Clear ();
#endif
    }

    [Fact]
    public void AlternateForwardKey_AlternateBackwardKey_Tests ()
    {
        Application.Init (new FakeDriver ());

        Toplevel top = new ();
        var w1 = new Window ();
        var v1 = new TextField ();
        var v2 = new TextView ();
        w1.Add (v1, v2);

        var w2 = new Window ();
        var v3 = new CheckBox ();
        var v4 = new Button ();
        w2.Add (v3, v4);

        top.Add (w1, w2);

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.True (v1.HasFocus);

                                     // Using default keys.
                                     top.NewKeyDownEvent (Key.Tab.WithCtrl);
                                     Assert.True (v2.HasFocus);
                                     top.NewKeyDownEvent (Key.Tab.WithCtrl);
                                     Assert.True (v3.HasFocus);
                                     top.NewKeyDownEvent (Key.Tab.WithCtrl);
                                     Assert.True (v4.HasFocus);
                                     top.NewKeyDownEvent (Key.Tab.WithCtrl);
                                     Assert.True (v1.HasFocus);

                                     top.NewKeyDownEvent (Key.Tab.WithShift.WithCtrl);
                                     Assert.True (v4.HasFocus);
                                     top.NewKeyDownEvent (Key.Tab.WithShift.WithCtrl);
                                     Assert.True (v3.HasFocus);
                                     top.NewKeyDownEvent (Key.Tab.WithShift.WithCtrl);
                                     Assert.True (v2.HasFocus);
                                     top.NewKeyDownEvent (Key.Tab.WithShift.WithCtrl);
                                     Assert.True (v1.HasFocus);

                                     top.NewKeyDownEvent (Key.PageDown.WithCtrl);
                                     Assert.True (v2.HasFocus);
                                     top.NewKeyDownEvent (Key.PageDown.WithCtrl);
                                     Assert.True (v3.HasFocus);
                                     top.NewKeyDownEvent (Key.PageDown.WithCtrl);
                                     Assert.True (v4.HasFocus);
                                     top.NewKeyDownEvent (Key.PageDown.WithCtrl);
                                     Assert.True (v1.HasFocus);

                                     top.NewKeyDownEvent (Key.PageUp.WithCtrl);
                                     Assert.True (v4.HasFocus);
                                     top.NewKeyDownEvent (Key.PageUp.WithCtrl);
                                     Assert.True (v3.HasFocus);
                                     top.NewKeyDownEvent (Key.PageUp.WithCtrl);
                                     Assert.True (v2.HasFocus);
                                     top.NewKeyDownEvent (Key.PageUp.WithCtrl);
                                     Assert.True (v1.HasFocus);

                                     // Using another's alternate keys.
                                     Application.AlternateForwardKey = Key.F7;
                                     Application.AlternateBackwardKey = Key.F6;

                                     top.NewKeyDownEvent (Key.F7);
                                     Assert.True (v2.HasFocus);
                                     top.NewKeyDownEvent (Key.F7);
                                     Assert.True (v3.HasFocus);
                                     top.NewKeyDownEvent (Key.F7);
                                     Assert.True (v4.HasFocus);
                                     top.NewKeyDownEvent (Key.F7);
                                     Assert.True (v1.HasFocus);

                                     top.NewKeyDownEvent (Key.F6);
                                     Assert.True (v4.HasFocus);
                                     top.NewKeyDownEvent (Key.F6);
                                     Assert.True (v3.HasFocus);
                                     top.NewKeyDownEvent (Key.F6);
                                     Assert.True (v2.HasFocus);
                                     top.NewKeyDownEvent (Key.F6);
                                     Assert.True (v1.HasFocus);

                                     Application.RequestStop ();
                                 };

        Application.Run (top);

        // Replacing the defaults keys to avoid errors on others unit tests that are using it.
        Application.AlternateForwardKey = Key.PageDown.WithCtrl;
        Application.AlternateBackwardKey = Key.PageUp.WithCtrl;
        Application.QuitKey = Key.Q.WithCtrl;

        Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, Application.AlternateForwardKey.KeyCode);
        Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, Application.AlternateBackwardKey.KeyCode);
        Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, Application.QuitKey.KeyCode);

        top.Dispose ();
        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    [AutoInitShutdown]
    public void EnsuresTopOnFront_CanFocus_False_By_Keyboard ()
    {
        Toplevel top = new ();

        var win = new Window
        {
            Title = "win",
            X = 0,
            Y = 0,
            Width = 20,
            Height = 10
        };
        var tf = new TextField { Width = 10 };
        win.Add (tf);

        var win2 = new Window
        {
            Title = "win2",
            X = 22,
            Y = 0,
            Width = 20,
            Height = 10
        };
        var tf2 = new TextField { Width = 10 };
        win2.Add (tf2);
        top.Add (win, win2);

        Application.Begin (top);

        Assert.True (win.CanFocus);
        Assert.True (win.HasFocus);
        Assert.True (win2.CanFocus);
        Assert.False (win2.HasFocus);
        Assert.Equal ("win2", ((Window)top.Subviews [^1]).Title);

        win.CanFocus = false;
        Assert.False (win.CanFocus);
        Assert.False (win.HasFocus);
        Assert.True (win2.CanFocus);
        Assert.True (win2.HasFocus);
        Assert.Equal ("win2", ((Window)top.Subviews [^1]).Title);

        top.NewKeyDownEvent (Key.Tab.WithCtrl);
        Assert.True (win2.CanFocus);
        Assert.False (win.HasFocus);
        Assert.True (win2.CanFocus);
        Assert.True (win2.HasFocus);
        Assert.Equal ("win2", ((Window)top.Subviews [^1]).Title);

        top.NewKeyDownEvent (Key.Tab.WithCtrl);
        Assert.False (win.CanFocus);
        Assert.False (win.HasFocus);
        Assert.True (win2.CanFocus);
        Assert.True (win2.HasFocus);
        Assert.Equal ("win2", ((Window)top.Subviews [^1]).Title);
    }

    [Fact]
    [AutoInitShutdown]
    public void EnsuresTopOnFront_CanFocus_True_By_Keyboard_ ()
    {
        Toplevel top = new ();

        var win = new Window
        {
            Title = "win",
            X = 0,
            Y = 0,
            Width = 20,
            Height = 10
        };
        var tf = new TextField { Width = 10 };
        win.Add (tf);

        var win2 = new Window
        {
            Title = "win2",
            X = 22,
            Y = 0,
            Width = 20,
            Height = 10
        };
        var tf2 = new TextField { Width = 10 };
        win2.Add (tf2);
        top.Add (win, win2);

        Application.Begin (top);

        Assert.True (win.CanFocus);
        Assert.True (win.HasFocus);
        Assert.True (win2.CanFocus);
        Assert.False (win2.HasFocus);
        Assert.Equal ("win2", ((Window)top.Subviews [^1]).Title);

        top.NewKeyDownEvent (Key.Tab.WithCtrl);
        Assert.True (win.CanFocus);
        Assert.False (win.HasFocus);
        Assert.True (win2.CanFocus);
        Assert.True (win2.HasFocus);
        Assert.Equal ("win2", ((Window)top.Subviews [^1]).Title);

        top.NewKeyDownEvent (Key.Tab.WithCtrl);
        Assert.True (win.CanFocus);
        Assert.True (win.HasFocus);
        Assert.True (win2.CanFocus);
        Assert.False (win2.HasFocus);
        Assert.Equal ("win", ((Window)top.Subviews [^1]).Title);
    }

    [Fact]
    public void KeyUp_Event ()
    {
        Application.Init (new FakeDriver ());

        // Setup some fake keypresses (This)
        var input = "Tests";

        // Put a control-q in at the end
        FakeConsole.MockKeyPresses.Push (new ConsoleKeyInfo ('Q', ConsoleKey.Q, false, false, true));

        foreach (char c in input.Reverse ())
        {
            if (char.IsLetter (c))
            {
                FakeConsole.MockKeyPresses.Push (
                                                 new ConsoleKeyInfo (
                                                                     c,
                                                                     (ConsoleKey)char.ToUpper (c),
                                                                     char.IsUpper (c),
                                                                     false,
                                                                     false
                                                                    )
                                                );
            }
            else
            {
                FakeConsole.MockKeyPresses.Push (
                                                 new ConsoleKeyInfo (
                                                                     c,
                                                                     (ConsoleKey)c,
                                                                     false,
                                                                     false,
                                                                     false
                                                                    )
                                                );
            }
        }

        int stackSize = FakeConsole.MockKeyPresses.Count;

        var iterations = 0;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     // Stop if we run out of control...
                                     if (iterations > 10)
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        var keyUps = 0;
        var output = string.Empty;
        var top = new Toplevel ();

        top.KeyUp += (sender, args) =>
                     {
                         if (args.KeyCode != (KeyCode.CtrlMask | KeyCode.Q))
                         {
                             output += args.AsRune;
                         }

                         keyUps++;
                     };

        Application.Run (top);

        // Input string should match output
        Assert.Equal (input, output);

        // # of key up events should match stack size
        //Assert.Equal (stackSize, keyUps);
        // We can't use numbers variables on the left side of an Assert.Equal/NotEqual,
        // it must be literal (Linux only).
        Assert.Equal (6, keyUps);

        // # of key up events should match # of iterations
        Assert.Equal (stackSize, iterations);

        top.Dispose ();
        Application.Shutdown ();
        Assert.Null (Application.Current);
        Assert.Null (Application.Top);
        Assert.Null (Application.MainLoop);
        Assert.Null (Application.Driver);
    }

    [Fact]
    [AutoInitShutdown]
    public void OnKeyDown_Application_KeyBinding ()
    {
        var view = new ScopedKeyBindingView ();
        var invoked = false;
        view.InvokingKeyBindings += (s, e) => invoked = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.OnKeyDown (Key.A);
        Assert.True (invoked);
        Assert.True (view.ApplicationCommand);

        invoked = false;
        view.ApplicationCommand = false;
        view.KeyBindings.Remove (KeyCode.A);
        Application.OnKeyDown (Key.A); // old
        Assert.False (invoked);
        Assert.False (view.ApplicationCommand);
        view.KeyBindings.Add (Key.A.WithCtrl, KeyBindingScope.Application, Command.Save);
        Application.OnKeyDown (Key.A); // old
        Assert.False (invoked);
        Assert.False (view.ApplicationCommand);
        Application.OnKeyDown (Key.A.WithCtrl); // new
        Assert.True (invoked);
        Assert.True (view.ApplicationCommand);

        invoked = false;
        Application.OnKeyDown (Key.H);
        Assert.True (invoked);

        invoked = false;
        Assert.False (view.HasFocus);
        Application.OnKeyDown (Key.F);
        Assert.False (invoked);

        Assert.True (view.ApplicationCommand);
        Assert.True (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);
    }

    [Fact]
    [AutoInitShutdown]
    public void OnKeyDown_Application_KeyBinding_Negative ()
    {
        var view = new ScopedKeyBindingView ();
        var invoked = false;
        view.InvokingKeyBindings += (s, e) => invoked = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.OnKeyDown (Key.A.WithCtrl);
        Assert.False (invoked);
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);

        invoked = false;
        Assert.False (view.HasFocus);
        Application.OnKeyDown (Key.Z);
        Assert.False (invoked);
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);
    }

    [Fact]
    [AutoInitShutdown]
    public void QuitKey_Getter_Setter ()
    {
        Toplevel top = new ();
        var isQuiting = false;

        top.Closing += (s, e) =>
                       {
                           isQuiting = true;
                           e.Cancel = true;
                       };

        Application.Begin (top);
        top.Running = true;

        Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, Application.QuitKey.KeyCode);
        Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
        Assert.True (isQuiting);

        isQuiting = false;
        Application.OnKeyDown (Key.Q.WithCtrl);
        Assert.True (isQuiting);

        isQuiting = false;
        Application.QuitKey = Key.C.WithCtrl;
        Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
        Assert.False (isQuiting);
        Application.OnKeyDown (Key.Q.WithCtrl);
        Assert.False (isQuiting);

        Application.OnKeyDown (Application.QuitKey);
        Assert.True (isQuiting);

        // Reset the QuitKey to avoid throws errors on another tests
        Application.QuitKey = Key.Q.WithCtrl;
    }

    // test Application key Bindings
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
