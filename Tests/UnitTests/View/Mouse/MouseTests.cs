using Timeout = Terminal.Gui.App.Timeout;

namespace UnitTests.ViewBaseTests.MouseTests;

[Trait ("Category", "Input")]
public class MouseTests : TestsAllViews
{
    // TODO: Add more tests that ensure the above test works with positive adornments

    // Test drag to move
    [Theory]
    [InlineData (0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 4, false)]
    [InlineData (1, 0, 0, 4, false)]
    [InlineData (0, 1, 0, 4, true)]
    [InlineData (0, 0, 1, 4, false)]
    [InlineData (1, 1, 0, 3, false)]
    [InlineData (1, 1, 0, 4, false)]
    [InlineData (1, 1, 0, 5, true)]
    [InlineData (1, 1, 0, 6, false)]
    [InlineData (1, 1, 0, 11, false)]
    [InlineData (1, 1, 0, 12, true)]
    [InlineData (1, 1, 0, 13, false)]
    [InlineData (1, 1, 0, 14, false)]
    [AutoInitShutdown]
    public void ButtonPressed_In_Border_Starts_Drag (int marginThickness, int borderThickness, int paddingThickness, int xy, bool expectedMoved)
    {
        View testView = new ()
        {
            CanFocus = true,
            X = 4,
            Y = 4,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable
        };
        testView.Border.LineStyle = LineStyle.None; // Calls EnsureView
        testView.Margin.Thickness = new (marginThickness);
        testView.Border!.Thickness = new (borderThickness);
        testView.Padding!.Thickness = new (paddingThickness);

        Runnable top = new ();
        top.Add (testView);

        SessionToken rs = Application.Begin (top);
        Assert.Equal (4, testView.Frame.X);

        Assert.Equal (new (4, 4), testView.Frame.Location);
        Application.RaiseMouseEvent (new () { ScreenPosition = new (xy, xy), Flags = MouseFlags.LeftButtonPressed });

        Application.RaiseMouseEvent (new () { ScreenPosition = new (xy + 1, xy + 1), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport });
        AutoInitShutdownAttribute.RunIteration ();
        Assert.Equal (expectedMoved, new Point (5, 5) == testView.Frame.Location);
        // The above grabbed the mouse. Need to ungrab.
        Application.Mouse.UngrabMouse ();

        top.Dispose ();
    }

    [Theory]
    [InlineData (MouseFlags.LeftButtonPressed, MouseFlags.LeftButtonReleased, MouseFlags.LeftButtonClicked)]
    [InlineData (MouseFlags.MiddleButtonPressed, MouseFlags.MiddleButtonReleased, MouseFlags.MiddleButtonClicked)]
    [InlineData (MouseFlags.RightButtonPressed, MouseFlags.RightButtonReleased, MouseFlags.RightButtonClicked)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released, MouseFlags.Button4Clicked)]
    public void MouseHoldRepeat_False_Button_Press_Release_DoesNotClick (MouseFlags pressed, MouseFlags released, MouseFlags clicked)
    {
        Mouse me = new ();

        View view = new ()
        {
            Width = 1,
            Height = 1,
            MouseHoldRepeat = null
        };

        var clickedCount = 0;

        view.MouseEvent += (s, e) => clickedCount += e.IsSingleDoubleOrTripleClicked ? 1 : 0;

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        me.Flags = released;
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        me.Flags = clicked;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);

        view.Dispose ();

        // LeftButtonPressed, LeftButtonReleased cause Application.Mouse.IsGrabbed to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (MouseFlags.LeftButtonPressed, MouseFlags.LeftButtonReleased)]
    [InlineData (MouseFlags.MiddleButtonPressed, MouseFlags.MiddleButtonReleased)]
    [InlineData (MouseFlags.RightButtonPressed, MouseFlags.RightButtonReleased)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released)]
    public void MouseHoldRepeat_True_And_MousePositionTracking_True_Button_Press_Release_Clicks (MouseFlags pressed, MouseFlags released)
    {
        Mouse me = new ();

        View view = new ()
        {
            Width = 1,
            Height = 1,
            MouseHoldRepeat = MouseFlags.LeftButtonReleased,
            MousePositionTracking  = true
        };

        // Setup components for mouse held down
        TimedEvents timed = new ();
        ApplicationMouse grab = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timed, grab);

        // Register callback for what to do when the mouse is held down
        var clickedCount = 0;
        view.MouseHoldRepeater.MouseIsHeldDownTick += (_, _) => clickedCount++;

        // Mouse is currently not held down so should be no timers running
        Assert.Empty (timed.Timeouts);

        // When mouse is held down
        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        // A timer should begin
        KeyValuePair<long, Timeout> t = Assert.Single (timed.Timeouts);

        // Invoke the timer
        t.Value.Callback.Invoke ();

        // Event should have been raised
        Assert.Equal (1, clickedCount);
        Assert.NotEmpty (timed.Timeouts);

        // When mouse is released
        me.Flags = released;
        view.NewMouseEvent (me);

        // timer should stop
        Assert.Empty (timed.Timeouts);
        Assert.Equal (1, clickedCount);

        view.Dispose ();
    }

    [Theory]
    [InlineData (MouseFlags.LeftButtonPressed, MouseFlags.LeftButtonReleased, MouseFlags.LeftButtonClicked)]
    [InlineData (MouseFlags.MiddleButtonPressed, MouseFlags.MiddleButtonReleased, MouseFlags.MiddleButtonClicked)]
    [InlineData (MouseFlags.RightButtonPressed, MouseFlags.RightButtonReleased, MouseFlags.RightButtonClicked)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released, MouseFlags.Button4Clicked)]
    public void MouseHoldRepeat_True_And_MousePositionTracking_True_Button_Press_Release_Clicks_Repeatedly (
        MouseFlags pressed,
        MouseFlags released,
        MouseFlags clicked
    )
    {
        Mouse me = new ();

        View view = new ()
        {
            Width = 1,
            Height = 1,
            MouseHoldRepeat = MouseFlags.LeftButtonReleased,
            MousePositionTracking  = true
        };

        // Setup components for mouse held down
        TimedEvents timed = new ();
        ApplicationMouse grab = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timed, grab);

        // Register callback for what to do when the mouse is held down
        var clickedCount = 0;
        view.MouseHoldRepeater.MouseIsHeldDownTick += (_, _) => clickedCount++;

        Assert.Empty (timed.Timeouts);

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        Assert.NotEmpty (timed.Timeouts);
        Assert.Single (timed.Timeouts).Value.Callback.Invoke ();

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);
        me.Handled = false;

        Assert.NotEmpty (timed.Timeouts);

        me.Flags = released;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);
        me.Handled = false;

        Assert.Empty (timed.Timeouts);

        me.Flags = clicked;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_And_MousePositionTracking_True_Move_InViewport_OutOfViewport_Keeps_Counting ()
    {
        Mouse mouse = new ()
        {
            Position = Point.Empty
        };

        View view = new ()
        {
            Width = 1,
            Height = 1,
            MouseHoldRepeat = MouseFlags.LeftButtonReleased,
            MousePositionTracking  = true
        };

        // Setup components for mouse held down
        TimedEvents timed = new ();
        ApplicationMouse grab = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timed, grab);

        // Register callback for what to do when the mouse is held down
        var clickedCount = 0;
        view.MouseHoldRepeater.MouseIsHeldDownTick += (_, _) => clickedCount++;

        // Start in Viewport
        mouse.Flags = MouseFlags.LeftButtonPressed;
        mouse.Position = mouse.Position!.Value with { X = 0 };
        view.NewMouseEvent (mouse);
        Assert.Equal (0, clickedCount);
        mouse.Handled = false;

        // Mouse is held down so timer should be ticking
        Assert.NotEmpty (timed.Timeouts);
        Assert.Equal (0, clickedCount);

        // Don't wait, just force it to expire
        Assert.Single (timed.Timeouts).Value.Callback.Invoke ();
        Assert.Equal (1, clickedCount);

        // Move out of Viewport
        mouse.Flags = MouseFlags.LeftButtonPressed;
        mouse.Position = mouse.Position!.Value with { X = 1 };
        view.NewMouseEvent (mouse);

        Assert.Single (timed.Timeouts).Value.Callback.Invoke ();
        Assert.Equal (2, clickedCount);

        mouse.Handled = false;

        // Move into Viewport
        mouse.Flags = MouseFlags.LeftButtonPressed;
        mouse.Position = mouse.Position!.Value with { X = 0 };
        view.NewMouseEvent (mouse);

        Assert.NotEmpty (timed.Timeouts);
        Assert.Equal (2, clickedCount);
        mouse.Handled = false;

        // Stay in Viewport
        mouse.Flags = MouseFlags.LeftButtonPressed;
        mouse.Position = mouse.Position!.Value with { X = 0 };
        view.NewMouseEvent (mouse);

        Assert.Single (timed.Timeouts).Value.Callback.Invoke ();

        Assert.Equal (3, clickedCount);
        mouse.Handled = false;

        view.Dispose ();
    }

    //[Theory]
    //[InlineData (true, MouseState.None, 0, 0, 0, 0)]
    //[InlineData (true, MouseState.In, 0, 0, 0, 0)]
    //[InlineData (true, MouseState.Pressed, 0, 0, 1, 0)]
    //[InlineData (true, MouseState.PressedOutside, 0, 0, 0, 1)]
    //public void MouseState_LeftButton_Pressed_Then_Released_Outside (bool inViewport, MouseState highlightFlags, int noneCount, int expectedInCount, int expectedPressedCount, int expectedPressedOutsideCount)
    //{
    //    MouseEventTestView testView = new ()
    //    {
    //        MouseHighlightStates = highlightFlags
    //    };

    //    Assert.Equal (0, testView.MouseStateInCount);
    //    Assert.Equal (0, testView.MouseStatePressedCount);
    //    Assert.Equal (0, testView.MouseStatePressedOutsideCount);
    //    Assert.Equal (0, testView.MouseStateNoneCount);

    //    testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed, Position = new (inViewport ? 0 : 1, 0) });
    //    Assert.Equal (expectedInCount, testView.MouseStateInCount);
    //    Assert.Equal (expectedPressedCount, testView.MouseStatePressedCount);
    //    Assert.Equal (expectedPressedOutsideCount, testView.MouseStatePressedOutsideCount);
    //    Assert.Equal (noneCount, testView.MouseStateNoneCount);

    //    testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonReleased, Position = new (inViewport ? 0 : 1, 0) });
    //    Assert.Equal (expectedInCount, testView.MouseStateInCount);
    //    Assert.Equal (expectedPressedCount, testView.MouseStatePressedCount);
    //    Assert.Equal (expectedPressedOutsideCount, testView.MouseStatePressedOutsideCount);
    //    Assert.Equal (noneCount, testView.MouseStateNoneCount);

    //    testView.Dispose ();

    //    // LeftButtonPressed, LeftButtonReleased cause Application.Mouse.IsGrabbed to be set
    //    Application.ResetState (true);

    //}

    // TODO: Add tests for each combination of HighlightFlags

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_None_LeftButton_Pressed_Move_No_Changes (int x)
    {
        MouseEventTestView testView = new ()
        {
            MouseHighlightStates = MouseState.None
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (0, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed, Position = new (x, 0) });

        if (inViewport)
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }
        else
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }

        // Move back to 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed });

        if (inViewport)
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }
        else
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }

        testView.Dispose ();

        // LeftButtonPressed, LeftButtonReleased cause Application.Mouse.IsGrabbed to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_Pressed_LeftButton_Pressed_Move_Keeps_Pressed (int x)
    {
        MouseEventTestView testView = new ()
        {
            MouseHighlightStates = MouseState.Pressed
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (1, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0 
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed, Position = new (x, 0) });

        if (inViewport)
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (1, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }
        else
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (1, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }

        // Move backto 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed });

        if (inViewport)
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (1, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }
        else
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (1, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }

        testView.Dispose ();

        // LeftButtonPressed, LeftButtonReleased cause Application.Mouse.IsGrabbed to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_PressedOutside_LeftButton_Pressed_Move_Raises_PressedOutside (int x)
    {
        MouseEventTestView testView = new ()
        {
            MouseHighlightStates = MouseState.PressedOutside,
            MouseHoldRepeat = null
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (0, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0 
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed, Position = new (x, 0) });

        if (inViewport)
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }
        else
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (1, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }

        // Move backto 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed });

        if (inViewport)
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }
        else
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (1, testView.MouseStatePressedOutsideCount);
            Assert.Equal (1, testView.MouseStateNoneCount);
        }

        testView.Dispose ();

        // LeftButtonPressed, LeftButtonReleased cause Application.Mouse.IsGrabbed to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_PressedOutside_LeftButton_Pressed_Move_Raises_PressedOutside_MouseHoldRepeat (int x)
    {
        MouseEventTestView testView = new ()
        {
            MouseHighlightStates = MouseState.PressedOutside,
            MouseHoldRepeat = MouseFlags.LeftButtonReleased
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (0, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0 
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed, Position = new (x, 0) });

        if (inViewport)
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }
        else
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }

        // Move backto 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.LeftButtonPressed });

        if (inViewport)
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }
        else
        {
            Assert.Equal (0, testView.MouseStateInCount);
            Assert.Equal (0, testView.MouseStatePressedCount);
            Assert.Equal (0, testView.MouseStatePressedOutsideCount);
            Assert.Equal (0, testView.MouseStateNoneCount);
        }

        testView.Dispose ();

        // LeftButtonPressed, LeftButtonReleased cause Application.Mouse.IsGrabbed to be set
        Application.ResetState (true);
    }

    private class MouseEventTestView : View
    {
        public int MouseEnterCount { get; private set; }
        public int MouseLeaveCount { get; private set; }
        public int MouseStatePressedOutsideCount { get; private set; }
        public int MouseStateInCount { get; private set; }
        public int MouseStatePressedCount { get; private set; }
        public int MouseStateNoneCount { get; private set; }

        public MouseEventTestView ()
        {
            Height = 1;
            Width = 1;
            CanFocus = true;
            Id = "mouseEventTestView";

            MouseLeave += (s, e) => { MouseEnterCount++; };
            MouseEnter += (s, e) => { MouseLeaveCount++; };
        }

        /// <inheritdoc/>
        protected override void OnMouseStateChanged (EventArgs<MouseState> args)
        {
            switch (args.Value)
            {
                case MouseState.None:
                    MouseStateNoneCount++;

                    break;
                case MouseState.In:
                    MouseStateInCount++;

                    break;

                case MouseState.Pressed:
                    MouseStatePressedCount++;

                    break;

                case MouseState.PressedOutside:
                    MouseStatePressedOutsideCount++;

                    break;
            }

            base.OnMouseStateChanged (args);
        }
    }
}
