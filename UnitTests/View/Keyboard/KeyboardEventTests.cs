using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ViewTests;

public class KeyboardEventTests (ITestOutputHelper output) : TestsAllViews
{
    /// <summary>
    ///     This tests that when a new key down event is sent to the view  will fire the key-down related
    ///     events: KeyDown and KeyDownNotHandled. Note that KeyUp is independent.
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

        var keyDownNotHandled = false;

        view.KeyDownNotHandled += (s, a) =>
                               {
                                   a.Handled = true;
                                   keyDownNotHandled = true;
                               };

        // Key.Empty is invalid, but it's used here to test that the event is fired
        Assert.True (view.NewKeyDownEvent (Key.Empty)); // this will be true because the ProcessKeyDown event handled it
        Assert.True (keyDown);
        Assert.True (keyDownNotHandled);
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
        var keyDownNotHandled = false;
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
        view.KeyDownNotHandled += (s, e) => { keyDownNotHandled = true; };

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

        view.NewKeyDownEvent (
                              new (
                                   KeyCode.Null
                                   | (shift ? KeyCode.ShiftMask : 0)
                                   | (alt ? KeyCode.AltMask : 0)
                                   | (control ? KeyCode.CtrlMask : 0)
                                  )
                             );
        Assert.True (keyDownNotHandled);
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
    public void NewKeyDownEvent_Handled_True_Stops_Processing ()
    {
        var keyDown = false;
        var keyDownNotHandled = false;

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


        view.KeyDownNotHandled += (s, e) =>
                               {
                                   Assert.Equal (KeyCode.A, e.KeyCode);
                                   Assert.False (keyDownNotHandled);
                                   Assert.False (view.OnProcessKeyDownCalled);
                                   e.Handled = true;
                                   keyDownNotHandled = true;
                               };

        view.NewKeyDownEvent (Key.A);
        Assert.True (keyDown);
        Assert.False (keyDownNotHandled);

        Assert.True (view.OnKeyDownCalled);
        Assert.False (view.OnProcessKeyDownCalled);
    }

    [Fact]
    public void NewKeyDownEvent_KeyDown_Handled_Stops_Processing ()
    {
        var view = new View ();
        var keyDownNotHandled = false;
        var setHandledTo = false;

        view.KeyDown += (s, e) =>
                        {
                            e.Handled = setHandledTo;
                            Assert.Equal (setHandledTo, e.Handled);
                            Assert.Equal (KeyCode.N, e.KeyCode);
                        };

        view.KeyDownNotHandled += (s, e) =>
                               {
                                   keyDownNotHandled = true;
                                   Assert.False (e.Handled);
                                   Assert.Equal (KeyCode.N, e.KeyCode);
                               };

        view.NewKeyDownEvent (Key.N);
        Assert.True (keyDownNotHandled);

        keyDownNotHandled = false;
        setHandledTo = true;
        view.NewKeyDownEvent (Key.N);
        Assert.False (keyDownNotHandled);
    }

    [Fact]
    public void NewKeyDownEvent_ProcessKeyDown_Handled_Stops_Processing ()
    {
        var keyDown = false;
        var keyDownNotHandled = false;

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

        view.KeyDownNotHandled += (s, e) =>
                               {
                                   Assert.Equal (KeyCode.A, e.KeyCode);
                                   Assert.False (keyDownNotHandled);
                                   Assert.True (view.OnProcessKeyDownCalled);
                                   e.Handled = true;
                                   keyDownNotHandled = true;
                               };

        view.NewKeyDownEvent (Key.A);
        Assert.True (keyDown);
        Assert.True (keyDownNotHandled);

        Assert.True (view.OnKeyDownCalled);
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
        Assert.False (view.OnProcessKeyDownCalled);
    }

    [Theory]
    [InlineData (null, null)]
    [InlineData (true, true)]
    [InlineData (false, false)]
    public void InvokeCommandsBoundToKey_Returns_Nullable_Properly (bool? toReturn, bool? expected)
    {
        var view = new KeyBindingsTestView ();
        view.CommandReturns = toReturn;

        bool? result = view.InvokeCommandsBoundToKey (Key.A);
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
        public bool OnKeyDownCalled { get; set; }
        public bool OnProcessKeyDownCalled { get; set; }
        public bool OnKeyUpCalled { get; set; }
        public override string Text { get; set; }

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
