using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ViewTests;

public class KeyboardEventTests (ITestOutputHelper output) : TestsAllViews
{
    /// <summary>
    ///     This tests that when a new key down event is sent to the view  will fire the 3 key-down related
    ///     events: KeyDown, InvokingKeyBindings, and ProcessKeyDown. Note that KeyUp is independent.
    /// </summary>
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_NewKeyDownEvent_All_EventsFire (Type viewType)
    {
        var view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"ERROR: Skipping generic view: {viewType}");

            return;
        }

        output.WriteLine ($"Testing {viewType}");

        var keyDown = false;

        view.KeyDown += (s, a) =>
                        {
                            a.Handled = false; // don't handle it so the other events are called
                            keyDown = true;
                        };

        var invokingKeyBindings = false;

        view.InvokingKeyBindings += (s, a) =>
                                    {
                                        a.Handled = false; // don't handle it so the other events are called
                                        invokingKeyBindings = true;
                                    };

        var keyDownProcessed = false;

        view.KeyDownNotHandled += (s, a) =>
                               {
                                   a.Handled = true;
                                   keyDownProcessed = true;
                               };

        // Key.Empty is invalid, but it's used here to test that the event is fired
        Assert.True (view.NewKeyDownEvent (Key.Empty)); // this will be true because the ProcessKeyDown event handled it
        Assert.True (keyDown);
        Assert.True (invokingKeyBindings);
        Assert.True (keyDownProcessed);
        view.Dispose ();
    }

    /// <summary>
    ///     This tests that when a new key up event is sent to the view the view will fire the 1 key-up related event:
    ///     KeyUp
    /// </summary>
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_NewKeyUpEvent_All_EventsFire (Type viewType)
    {
        var view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"ERROR: Generic view {viewType}");

            return;
        }

        output.WriteLine ($"Testing {view.GetType ().Name}");

        var keyUp = false;

        view.KeyUp += (s, a) =>
                      {
                          a.Handled = true;
                          keyUp = true;
                      };

        Assert.True (view.NewKeyUpEvent (Key.A)); // this will be true because the KeyUp event handled it
        Assert.True (keyUp);
        view.Dispose ();
    }

    [Theory]
    [InlineData (true, false, false)]
    [InlineData (true, true, false)]
    [InlineData (true, true, true)]
    public void NewKeyDownUpEvents_Events_Are_Raised_With_Only_Key_Modifiers (bool shift, bool alt, bool control)
    {
        var keyDown = false;
        var keyPressed = false;
        var keyUp = false;

        var view = new OnNewKeyTestView ();
        view.CancelVirtualMethods = false;

        view.KeyDown += (s, e) =>
                        {
                            Assert.Equal (KeyCode.Null, e.KeyCode & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask);
                            Assert.Equal (shift, e.IsShift);
                            Assert.Equal (alt, e.IsAlt);
                            Assert.Equal (control, e.IsCtrl);
                            Assert.False (keyDown);
                            Assert.True (view.OnKeyDownCalled);
                            keyDown = true;
                        };
        view.KeyDownNotHandled += (s, e) => { keyPressed = true; };

        view.KeyUp += (s, e) =>
                      {
                          Assert.Equal (KeyCode.Null, e.KeyCode & ~KeyCode.CtrlMask & ~KeyCode.AltMask & ~KeyCode.ShiftMask);
                          Assert.Equal (shift, e.IsShift);
                          Assert.Equal (alt, e.IsAlt);
                          Assert.Equal (control, e.IsCtrl);
                          Assert.False (keyUp);
                          Assert.True (view.OnKeyUpCalled);
                          keyUp = true;
                      };

        //view.ProcessKeyDownEvent (new (Key.Null | (shift ? Key.ShiftMask : 0) | (alt ? Key.AltMask : 0) | (control ? Key.CtrlMask : 0)));
        //Assert.True (keyDown);
        //Assert.True (view.OnKeyDownWasCalled);
        //Assert.True (view.OnProcessKeyDownWasCalled);

        view.NewKeyDownEvent (
                              new (
                                   KeyCode.Null
                                   | (shift ? KeyCode.ShiftMask : 0)
                                   | (alt ? KeyCode.AltMask : 0)
                                   | (control ? KeyCode.CtrlMask : 0)
                                  )
                             );
        Assert.True (keyPressed);
        Assert.True (view.OnKeyDownCalled);
        Assert.True (view.OnProcessKeyDownCalled);

        view.NewKeyUpEvent (
                            new (
                                 KeyCode.Null
                                 | (shift ? KeyCode.ShiftMask : 0)
                                 | (alt ? KeyCode.AltMask : 0)
                                 | (control ? KeyCode.CtrlMask : 0)
                                )
                           );
        Assert.True (keyUp);
        Assert.True (view.OnKeyUpCalled);
    }

    [Fact]
    public void NewKeyDownEvent_InvokingKeyBindings_Handled_Cancels ()
    {
        var view = new View ();
        var keyPressInvoked = false;
        var invokingKeyBindingsInvoked = false;
        var processKeyPressInvoked = false;
        var setHandledTo = false;

        view.KeyDown += (s, e) =>
                        {
                            keyPressInvoked = true;
                            Assert.False (e.Handled);
                            Assert.Equal (KeyCode.N, e.KeyCode);
                        };

        view.InvokingKeyBindings += (s, e) =>
                                    {
                                        invokingKeyBindingsInvoked = true;
                                        e.Handled = setHandledTo;
                                        Assert.Equal (setHandledTo, e.Handled);
                                        Assert.Equal (KeyCode.N, e.KeyCode);
                                    };

        view.KeyDownNotHandled += (s, e) =>
                               {
                                   processKeyPressInvoked = true;
                                   processKeyPressInvoked = true;
                                   Assert.False (e.Handled);
                                   Assert.Equal (KeyCode.N, e.KeyCode);
                               };

        view.NewKeyDownEvent (Key.N);
        Assert.True (keyPressInvoked);
        Assert.True (invokingKeyBindingsInvoked);
        Assert.True (processKeyPressInvoked);

        keyPressInvoked = false;
        invokingKeyBindingsInvoked = false;
        processKeyPressInvoked = false;
        setHandledTo = true;
        view.NewKeyDownEvent (Key.N);
        Assert.True (keyPressInvoked);
        Assert.True (invokingKeyBindingsInvoked);
        Assert.False (processKeyPressInvoked);
    }

    [Fact]
    public void NewKeyDownEvent_InvokingKeyBindings_Handled_True_Stops_Processing ()
    {
        var keyDown = false;
        var invokingKeyBindings = false;
        var keyPressed = false;

        var view = new OnNewKeyTestView ();
        Assert.True (view.CanFocus);
        view.CancelVirtualMethods = false;

        view.KeyDown += (s, e) =>
                        {
                            Assert.Equal (KeyCode.A, e.KeyCode);
                            Assert.False (keyDown);
                            Assert.True (view.OnKeyDownCalled);
                            e.Handled = false;
                            keyDown = true;
                        };

        view.InvokingKeyBindings += (s, e) =>
                                    {
                                        Assert.Equal (KeyCode.A, e.KeyCode);
                                        Assert.False (keyPressed);
                                        Assert.True (view.OnInvokingKeyBindingsCalled);
                                        e.Handled = true;
                                        invokingKeyBindings = true;
                                    };

        view.KeyDownNotHandled += (s, e) =>
                               {
                                   Assert.Equal (KeyCode.A, e.KeyCode);
                                   Assert.False (keyPressed);
                                   Assert.False (view.OnProcessKeyDownCalled);
                                   e.Handled = true;
                                   keyPressed = true;
                               };

        view.NewKeyDownEvent (Key.A);
        Assert.True (keyDown);
        Assert.True (invokingKeyBindings);
        Assert.False (keyPressed);

        Assert.True (view.OnKeyDownCalled);
        Assert.True (view.OnInvokingKeyBindingsCalled);
        Assert.False (view.OnProcessKeyDownCalled);
    }

    [Fact]
    public void NewKeyDownEvent_Handled_True_Stops_Processing ()
    {
        var keyDown = false;
        var invokingKeyBindings = false;
        var keyPressed = false;

        var view = new OnNewKeyTestView ();
        Assert.True (view.CanFocus);
        view.CancelVirtualMethods = false;

        view.KeyDown += (s, e) =>
                        {
                            Assert.Equal (KeyCode.A, e.KeyCode);
                            Assert.False (keyDown);
                            Assert.True (view.OnKeyDownCalled);
                            e.Handled = true;
                            keyDown = true;
                        };

        view.InvokingKeyBindings += (s, e) =>
                                    {
                                        Assert.Equal (KeyCode.A, e.KeyCode);
                                        Assert.False (keyPressed);
                                        Assert.False (view.OnInvokingKeyBindingsCalled);
                                        e.Handled = true;
                                        invokingKeyBindings = true;
                                    };

        view.KeyDownNotHandled += (s, e) =>
                               {
                                   Assert.Equal (KeyCode.A, e.KeyCode);
                                   Assert.False (keyPressed);
                                   Assert.False (view.OnProcessKeyDownCalled);
                                   e.Handled = true;
                                   keyPressed = true;
                               };

        view.NewKeyDownEvent (Key.A);
        Assert.True (keyDown);
        Assert.False (invokingKeyBindings);
        Assert.False (keyPressed);

        Assert.True (view.OnKeyDownCalled);
        Assert.False (view.OnInvokingKeyBindingsCalled);
        Assert.False (view.OnProcessKeyDownCalled);
    }

    [Fact]
    public void NewKeyDownEvent_KeyDown_Handled_Stops_Processing ()
    {
        var view = new View ();
        var invokingKeyBindingsInvoked = false;
        var processKeyPressInvoked = false;
        var setHandledTo = false;

        view.KeyDown += (s, e) =>
                        {
                            e.Handled = setHandledTo;
                            Assert.Equal (setHandledTo, e.Handled);
                            Assert.Equal (KeyCode.N, e.KeyCode);
                        };

        view.InvokingKeyBindings += (s, e) =>
                                    {
                                        invokingKeyBindingsInvoked = true;
                                        Assert.False (e.Handled);
                                        Assert.Equal (KeyCode.N, e.KeyCode);
                                    };

        view.KeyDownNotHandled += (s, e) =>
                               {
                                   processKeyPressInvoked = true;
                                   Assert.False (e.Handled);
                                   Assert.Equal (KeyCode.N, e.KeyCode);
                               };

        view.NewKeyDownEvent (Key.N);
        Assert.True (invokingKeyBindingsInvoked);
        Assert.True (processKeyPressInvoked);

        invokingKeyBindingsInvoked = false;
        processKeyPressInvoked = false;
        setHandledTo = true;
        view.NewKeyDownEvent (Key.N);
        Assert.False (invokingKeyBindingsInvoked);
        Assert.False (processKeyPressInvoked);
    }

    [Fact]
    public void NewKeyDownEvent_ProcessKeyDown_Handled_Stops_Processing ()
    {
        var keyDown = false;
        var invokingKeyBindings = false;
        var processKeyDown = false;

        var view = new OnNewKeyTestView ();
        Assert.True (view.CanFocus);
        view.CancelVirtualMethods = false;

        view.KeyDown += (s, e) =>
                        {
                            Assert.Equal (KeyCode.A, e.KeyCode);
                            Assert.False (keyDown);
                            Assert.True (view.OnKeyDownCalled);
                            e.Handled = false;
                            keyDown = true;
                        };

        view.InvokingKeyBindings += (s, e) =>
                                    {
                                        Assert.Equal (KeyCode.A, e.KeyCode);
                                        Assert.False (processKeyDown);
                                        Assert.True (view.OnInvokingKeyBindingsCalled);
                                        e.Handled = false;
                                        invokingKeyBindings = true;
                                    };

        view.KeyDownNotHandled += (s, e) =>
                               {
                                   Assert.Equal (KeyCode.A, e.KeyCode);
                                   Assert.False (processKeyDown);
                                   Assert.True (view.OnProcessKeyDownCalled);
                                   e.Handled = true;
                                   processKeyDown = true;
                               };

        view.NewKeyDownEvent (Key.A);
        Assert.True (keyDown);
        Assert.True (invokingKeyBindings);
        Assert.True (processKeyDown);

        Assert.True (view.OnKeyDownCalled);
        Assert.True (view.OnInvokingKeyBindingsCalled);
        Assert.True (view.OnProcessKeyDownCalled);
    }

    [Fact]
    public void NewKeyUpEvent_KeyUp_Handled_True_Stops_Processing ()
    {
        var keyUp = false;

        var view = new OnNewKeyTestView ();
        Assert.True (view.CanFocus);
        view.CancelVirtualMethods = false;

        view.KeyUp += (s, e) =>
                      {
                          Assert.Equal (KeyCode.A, e.KeyCode);
                          Assert.False (keyUp);
                          Assert.False (view.OnProcessKeyDownCalled);
                          e.Handled = true;
                          keyUp = true;
                      };

        view.NewKeyUpEvent (Key.A);
        Assert.True (keyUp);

        Assert.True (view.OnKeyUpCalled);
        Assert.False (view.OnKeyDownCalled);
        Assert.False (view.OnInvokingKeyBindingsCalled);
        Assert.False (view.OnProcessKeyDownCalled);
    }

    [Theory]
    [InlineData (null, null)]
    [InlineData (true, true)]
    [InlineData (false, false)]
    public void RaiseInvokingKeyBindingsAndInvokeCommands_Returns_Nullable_Properly (bool? toReturn, bool? expected)
    {
        var view = new KeyBindingsTestView ();
        view.CommandReturns = toReturn;

        bool? result = view.RaiseInvokingKeyBindingsAndInvokeCommands (Key.A);
        Assert.Equal (expected, result);
    }

    /// <summary>A view that overrides the OnKey* methods so we can test that they are called.</summary>
    public class KeyBindingsTestView : View
    {
        public KeyBindingsTestView ()
        {
            CanFocus = true;
            AddCommand (Command.HotKey, () => CommandReturns);
            KeyBindings.Add (Key.A, Command.HotKey);
        }

        public bool? CommandReturns { get; set; }
    }

    /// <summary>A view that overrides the OnKey* methods so we can test that they are called.</summary>
    public class OnNewKeyTestView : View
    {
        public OnNewKeyTestView () { CanFocus = true; }
        public bool CancelVirtualMethods { set; private get; }
        public bool OnInvokingKeyBindingsCalled { get; set; }
        public bool OnKeyDownCalled { get; set; }
        public bool OnProcessKeyDownCalled { get; set; }
        public bool OnKeyUpCalled { get; set; }
        public override string Text { get; set; }

        protected override bool OnInvokingKeyBindings (Key keyEvent, KeyBindingScope scope)
        {

            OnInvokingKeyBindingsCalled = true;

            return CancelVirtualMethods;
        }

        protected override bool OnKeyDown (Key keyEvent)
        {
            OnKeyDownCalled = true;

            return CancelVirtualMethods;
        }

        public override bool OnKeyUp (Key keyEvent)
        {
            OnKeyUpCalled = true;

            return CancelVirtualMethods;
        }

        protected override bool OnKeyDownNotHandled (Key keyEvent)
        {
            OnProcessKeyDownCalled = true;

            return CancelVirtualMethods;
        }
    }
}
