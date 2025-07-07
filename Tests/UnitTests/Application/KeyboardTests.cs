using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests;

/// <summary>
///     Application tests for keyboard support.
/// </summary>
public class KeyboardTests
{
    public KeyboardTests (ITestOutputHelper output)
    {
        _output = output;
#if DEBUG_IDISPOSABLE
        View.Instances.Clear ();
        RunState.Instances.Clear ();
#endif
    }

    private readonly ITestOutputHelper _output;

    private object _timeoutLock;

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Add_Adds ()
    {
        Application.KeyBindings.Add (Key.A, Command.Accept);
        Application.KeyBindings.Add (Key.B, Command.Accept);

        Assert.True (Application.KeyBindings.TryGet (Key.A, out KeyBinding binding));
        Assert.Null (binding.Target);
        Assert.True (Application.KeyBindings.TryGet (Key.B, out binding));
        Assert.Null (binding.Target);
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Remove_Removes ()
    {
        Application.KeyBindings.Add (Key.A, Command.Accept);

        Assert.True (Application.KeyBindings.TryGet (Key.A, out _));

        Application.KeyBindings.Remove (Key.A);
        Assert.False (Application.KeyBindings.TryGet (Key.A, out _));
    }

    [Fact]
    public void KeyBindings_OnKeyDown ()
    {
        Application.Top = new ();
        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

        Application.Top.Add (view);

        Application.RaiseKeyDownEvent (Key.A);
        Assert.False (keyWasHandled);
        Assert.True (view.ApplicationCommand);

        keyWasHandled = false;
        view.ApplicationCommand = false;
        Application.KeyBindings.Remove (KeyCode.A);
        Application.RaiseKeyDownEvent (Key.A); // old
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommand);
        Application.KeyBindings.Add (Key.A.WithCtrl, view, Command.Save);
        Application.RaiseKeyDownEvent (Key.A); // old
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommand);
        Application.RaiseKeyDownEvent (Key.A.WithCtrl); // new
        Assert.False (keyWasHandled);
        Assert.True (view.ApplicationCommand);

        keyWasHandled = false;
        Application.RaiseKeyDownEvent (Key.H);
        Assert.False (keyWasHandled);
        Assert.True (view.HotKeyCommand);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        Application.RaiseKeyDownEvent (Key.F);
        Assert.False (keyWasHandled);

        Assert.True (view.ApplicationCommand);
        Assert.True (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);
        Application.Top.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_OnKeyDown_Negative ()
    {
        var view = new ScopedKeyBindingView ();
        var keyWasHandled = false;
        view.KeyDownNotHandled += (s, e) => keyWasHandled = true;

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Application.RaiseKeyDownEvent (Key.A.WithCtrl);
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);

        keyWasHandled = false;
        Assert.False (view.HasFocus);
        Application.RaiseKeyDownEvent (Key.Z);
        Assert.False (keyWasHandled);
        Assert.False (view.ApplicationCommand);
        Assert.False (view.HotKeyCommand);
        Assert.False (view.FocusedCommand);
        top.Dispose ();
    }

    [Fact]
    public void NextTabGroupKey_Moves_Focus_To_TabStop_In_Next_TabGroup ()
    {
        // Arrange
        Application.Navigation = new ();
        var top = new Toplevel ();

        var view1 = new View
        {
            Id = "view1",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        view1.Add (subView1);

        var view2 = new View
        {
            Id = "view2",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        var subView2 = new View
        {
            Id = "subView2",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };
        view2.Add (subView2);

        top.Add (view1, view2);
        Application.Top = top;
        view1.SetFocus ();
        Assert.True (view1.HasFocus);
        Assert.True (subView1.HasFocus);

        // Act
        Application.RaiseKeyDownEvent (Application.NextTabGroupKey);

        // Assert
        Assert.True (view2.HasFocus);
        Assert.True (subView2.HasFocus);

        top.Dispose ();
        Application.Navigation = null;
    }

    [Fact]
    public void NextTabGroupKey_PrevTabGroupKey_Tests ()
    {
        Application.Init (new FakeDriver ());

        Toplevel top = new (); // TabGroup
        var w1 = new Window (); // TabGroup
        var v1 = new TextField (); // TabStop
        var v2 = new TextView (); // TabStop
        w1.Add (v1, v2);

        var w2 = new Window (); // TabGroup
        var v3 = new CheckBox (); // TabStop
        var v4 = new Button (); // TabStop
        w2.Add (v3, v4);

        top.Add (w1, w2);

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.True (v1.HasFocus);

                                     // Across TabGroups
                                     Application.RaiseKeyDownEvent (Key.F6);
                                     Assert.True (v3.HasFocus);
                                     Application.RaiseKeyDownEvent (Key.F6);
                                     Assert.True (v1.HasFocus);

                                     Application.RaiseKeyDownEvent (Key.F6.WithShift);
                                     Assert.True (v3.HasFocus);
                                     Application.RaiseKeyDownEvent (Key.F6.WithShift);
                                     Assert.True (v1.HasFocus);

                                     // Restore?
                                     Application.RaiseKeyDownEvent (Key.Tab);
                                     Assert.True (v2.HasFocus);

                                     Application.RaiseKeyDownEvent (Key.F6);
                                     Assert.True (v3.HasFocus);

                                     Application.RaiseKeyDownEvent (Key.F6);
                                     Assert.True (v2.HasFocus); // previously focused view was preserved

                                     Application.RequestStop ();
                                 };

        Application.Run (top);

        // Replacing the defaults keys to avoid errors on others unit tests that are using it.
        Application.NextTabGroupKey = Key.PageDown.WithCtrl;
        Application.PrevTabGroupKey = Key.PageUp.WithCtrl;
        Application.QuitKey = Key.Q.WithCtrl;

        Assert.Equal (KeyCode.PageDown | KeyCode.CtrlMask, Application.NextTabGroupKey.KeyCode);
        Assert.Equal (KeyCode.PageUp | KeyCode.CtrlMask, Application.PrevTabGroupKey.KeyCode);
        Assert.Equal (KeyCode.Q | KeyCode.CtrlMask, Application.QuitKey.KeyCode);

        top.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    public void NextTabKey_Moves_Focus_To_Next_TabStop ()
    {
        // Arrange
        Application.Navigation = new ();
        var top = new Toplevel ();
        var view1 = new View { Id = "view1", CanFocus = true };
        var view2 = new View { Id = "view2", CanFocus = true };
        top.Add (view1, view2);
        Application.Top = top;
        view1.SetFocus ();

        // Act
        Application.RaiseKeyDownEvent (Application.NextTabKey);

        // Assert
        Assert.True (view2.HasFocus);

        top.Dispose ();
        Application.Navigation = null;
    }

    [Fact]
    public void PrevTabGroupKey_Moves_Focus_To_TabStop_In_Prev_TabGroup ()
    {
        // Arrange
        Application.Navigation = new ();
        var top = new Toplevel ();

        var view1 = new View
        {
            Id = "view1",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        view1.Add (subView1);

        var view2 = new View
        {
            Id = "view2",
            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        var subView2 = new View
        {
            Id = "subView2",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };
        view2.Add (subView2);

        top.Add (view1, view2);
        Application.Top = top;
        view1.SetFocus ();
        Assert.True (view1.HasFocus);
        Assert.True (subView1.HasFocus);

        // Act
        Application.RaiseKeyDownEvent (Application.PrevTabGroupKey);

        // Assert
        Assert.True (view2.HasFocus);
        Assert.True (subView2.HasFocus);

        top.Dispose ();
        Application.Navigation = null;
    }

    [Fact]
    public void PrevTabKey_Moves_Focus_To_Prev_TabStop ()
    {
        // Arrange
        Application.Navigation = new ();
        var top = new Toplevel ();
        var view1 = new View { Id = "view1", CanFocus = true };
        var view2 = new View { Id = "view2", CanFocus = true };
        top.Add (view1, view2);
        Application.Top = top;
        view1.SetFocus ();

        // Act
        Application.RaiseKeyDownEvent (Application.NextTabKey);

        // Assert
        Assert.True (view2.HasFocus);

        top.Dispose ();
        Application.Navigation = null;
    }

    [Fact]
    public void QuitKey_Default_Is_Esc ()
    {
        Application.ResetState (true);

        // Before Init
        Assert.Equal (Key.Esc, Application.QuitKey);

        Application.Init (new FakeDriver ());

        // After Init
        Assert.Equal (Key.Esc, Application.QuitKey);

        Application.Shutdown ();
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

        Key prevKey = Application.QuitKey;

        Application.RaiseKeyDownEvent (Application.QuitKey);
        Assert.True (isQuiting);

        isQuiting = false;
        Application.RaiseKeyDownEvent (Application.QuitKey);
        Assert.True (isQuiting);

        isQuiting = false;
        Application.QuitKey = Key.C.WithCtrl;
        Application.RaiseKeyDownEvent (prevKey); // Should not quit
        Assert.False (isQuiting);
        Application.RaiseKeyDownEvent (Key.Q.WithCtrl); // Should not quit
        Assert.False (isQuiting);

        Application.RaiseKeyDownEvent (Application.QuitKey);
        Assert.True (isQuiting);

        // Reset the QuitKey to avoid throws errors on another tests
        Application.QuitKey = prevKey;
        top.Dispose ();
    }

    [Fact]
    public void QuitKey_Quits ()
    {
        Assert.Null (_timeoutLock);
        _timeoutLock = new ();

        uint abortTime = 500;
        var initialized = false;
        var iteration = 0;
        var shutdown = false;
        object timeout = null;

        Application.InitializedChanged += OnApplicationOnInitializedChanged;

        Application.Init (new FakeDriver ());
        Assert.True (initialized);
        Assert.False (shutdown);

        _output.WriteLine ("Application.Run<Toplevel> ().Dispose ()..");
        Application.Run<Toplevel> ().Dispose ();
        _output.WriteLine ("Back from Application.Run<Toplevel> ().Dispose ()");

        Assert.True (initialized);
        Assert.False (shutdown);

        Assert.Equal (1, iteration);

        Application.Shutdown ();

        Application.InitializedChanged -= OnApplicationOnInitializedChanged;

        lock (_timeoutLock)
        {
            if (timeout is { })
            {
                Application.RemoveTimeout (timeout);
                timeout = null;
            }
        }

        Assert.True (initialized);
        Assert.True (shutdown);

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif
        lock (_timeoutLock)
        {
            _timeoutLock = null;
        }

        return;

        void OnApplicationOnInitializedChanged (object s, EventArgs<bool> a)
        {
            _output.WriteLine ("OnApplicationOnInitializedChanged: {0}", a.Value);

            if (a.Value)
            {
                Application.Iteration += OnApplicationOnIteration;
                initialized = true;

                lock (_timeoutLock)
                {
                    timeout = Application.AddTimeout (TimeSpan.FromMilliseconds (abortTime), ForceCloseCallback);
                }
            }
            else
            {
                Application.Iteration -= OnApplicationOnIteration;
                shutdown = true;
            }
        }

        bool ForceCloseCallback ()
        {
            lock (_timeoutLock)
            {
                _output.WriteLine ($"ForceCloseCallback. iteration: {iteration}");

                if (timeout is { })
                {
                    timeout = null;
                }
            }

            Application.ResetState (true);
            Assert.Fail ($"Failed to Quit with {Application.QuitKey} after {abortTime}ms. Force quit.");

            return false;
        }

        void OnApplicationOnIteration (object s, IterationEventArgs a)
        {
            _output.WriteLine ("Iteration: {0}", iteration);
            iteration++;
            Assert.True (iteration < 2, "Too many iterations, something is wrong.");

            if (Application.Initialized)
            {
                _output.WriteLine ("  Pressing QuitKey");
                Application.RaiseKeyDownEvent (Application.QuitKey);
            }
        }
    }

    // Test View for testing Application key Bindings
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
