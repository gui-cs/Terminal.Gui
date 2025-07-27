using Moq;
using UnitTests;

namespace Terminal.Gui.ViewMouseTests;

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
        var testView = new View
        {
            CanFocus = true,
            X = 4,
            Y = 4,
            Width = 10,
            Height = 10,
            Arrangement = ViewArrangement.Movable
        };
        testView.Margin.Thickness = new (marginThickness);
        testView.Border.Thickness = new (borderThickness);
        testView.Padding.Thickness = new (paddingThickness);

        var top = new Toplevel ();
        top.Add (testView);

        RunState rs = Application.Begin (top);
        Assert.Equal (4, testView.Frame.X);

        Assert.Equal (new (4, 4), testView.Frame.Location);
        Application.RaiseMouseEvent (new () { ScreenPosition = new (xy, xy), Flags = MouseFlags.Button1Pressed });

        Application.RaiseMouseEvent (new () { ScreenPosition = new (xy + 1, xy + 1), Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition });
        Application.RunIteration (ref rs);

        Assert.Equal (expectedMoved, new Point (5, 5) == testView.Frame.Location);
        top.Dispose ();
    }

    [Theory]
    [InlineData (MouseFlags.Button1Pressed, MouseFlags.Button1Released, MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Pressed, MouseFlags.Button2Released, MouseFlags.Button2Clicked)]
    [InlineData (MouseFlags.Button3Pressed, MouseFlags.Button3Released, MouseFlags.Button3Clicked)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released, MouseFlags.Button4Clicked)]
    public void WantContinuousButtonPressed_False_Button_Press_Release_DoesNotClick (MouseFlags pressed, MouseFlags released, MouseFlags clicked)
    {
        var me = new MouseEventArgs ();

        var view = new View
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = false
        };

        var clickedCount = 0;

        view.MouseClick += (s, e) => clickedCount++;

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

        // Button1Pressed, Button1Released cause Application.MouseGrabHandler.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Clicked)]
    [InlineData (MouseFlags.Button3Clicked)]
    [InlineData (MouseFlags.Button4Clicked)]
    public void WantContinuousButtonPressed_True_Button_Clicked_Raises_MouseClick (MouseFlags clicked)
    {
        var me = new MouseEventArgs ();

        var view = new View
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true
        };

        var clickedCount = 0;

        view.MouseClick += (s, e) => clickedCount++;

        me.Flags = clicked;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);

        view.Dispose ();

        // Button1Pressed, Button1Released cause Application.MouseGrabHandler.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Clicked)]
    [InlineData (MouseFlags.Button3Clicked)]
    [InlineData (MouseFlags.Button4Clicked)]
    public void WantContinuousButtonPressed_True_Button_Clicked_Raises_Selecting (MouseFlags clicked)
    {
        var me = new MouseEventArgs ();

        var view = new View
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true
        };

        var selectingCount = 0;

        view.Selecting += (s, e) => selectingCount++;

        me.Flags = clicked;
        view.NewMouseEvent (me);
        Assert.Equal (1, selectingCount);

        view.Dispose ();

        // Button1Pressed, Button1Released cause Application.MouseGrabHandler.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (MouseFlags.Button1Pressed, MouseFlags.Button1Released)]
    [InlineData (MouseFlags.Button2Pressed, MouseFlags.Button2Released)]
    [InlineData (MouseFlags.Button3Pressed, MouseFlags.Button3Released)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released)]
    public void WantContinuousButtonPressed_True_And_WantMousePositionReports_True_Button_Press_Release_Clicks (MouseFlags pressed, MouseFlags released)
    {
        var me = new MouseEventArgs ();

        var view = new View
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true,
            WantMousePositionReports = true
        };

        // Setup components for mouse held down
        var timed = new TimedEvents ();
        var grab = new MouseGrabHandler ();
        view.MouseHeldDown = new MouseHeldDown (view, timed, grab);

        // Register callback for what to do when the mouse is held down
        var clickedCount = 0;
        view.MouseHeldDown.MouseIsHeldDownTick += (_, _) => clickedCount++;

        // Mouse is currently not held down so should be no timers running
        Assert.Empty(timed.Timeouts);

        // When mouse is held down
        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        // A timer should begin
        var t = Assert.Single (timed.Timeouts);

        // Invoke the timer
        t.Value.Callback.Invoke ();

        // Event should have been raised
        Assert.Equal (1, clickedCount);
        Assert.NotEmpty(timed.Timeouts);

        // When mouse is released
        me.Flags = released;
        view.NewMouseEvent (me);

        // timer should stop
        Assert.Empty (timed.Timeouts);
        Assert.Equal (1, clickedCount);

        view.Dispose ();
    }

    [Theory]
    [InlineData (MouseFlags.Button1Pressed, MouseFlags.Button1Released, MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Pressed, MouseFlags.Button2Released, MouseFlags.Button2Clicked)]
    [InlineData (MouseFlags.Button3Pressed, MouseFlags.Button3Released, MouseFlags.Button3Clicked)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released, MouseFlags.Button4Clicked)]
    public void WantContinuousButtonPressed_True_And_WantMousePositionReports_True_Button_Press_Release_Clicks_Repeatedly (
        MouseFlags pressed,
        MouseFlags released,
        MouseFlags clicked
    )
    {
        var me = new MouseEventArgs ();

        var view = new View
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true,
            WantMousePositionReports = true
        };

        // Setup components for mouse held down
        var timed = new TimedEvents ();
        var grab = new MouseGrabHandler ();
        view.MouseHeldDown = new MouseHeldDown (view, timed, grab);

        // Register callback for what to do when the mouse is held down
        var clickedCount = 0;
        view.MouseHeldDown.MouseIsHeldDownTick += (_, _) => clickedCount++;

        Assert.Empty (timed.Timeouts);

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        Assert.NotEmpty(timed.Timeouts);
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
    public void WantContinuousButtonPressed_True_And_WantMousePositionReports_True_Move_InViewport_OutOfViewport_Keeps_Counting ()
    {
        var me = new MouseEventArgs ();

        var view = new View
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true,
            WantMousePositionReports = true
        };

        // Setup components for mouse held down
        var timed = new TimedEvents ();
        var grab = new MouseGrabHandler ();
        view.MouseHeldDown = new MouseHeldDown (view, timed, grab);

        // Register callback for what to do when the mouse is held down
        var clickedCount = 0;
        view.MouseHeldDown.MouseIsHeldDownTick += (_, _) => clickedCount++;

        // Start in Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.Position = me.Position with { X = 0 };
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        // Mouse is held down so timer should be ticking
        Assert.NotEmpty (timed.Timeouts);
        Assert.Equal (clickedCount,0);

        // Don't wait, just force it to expire
        Assert.Single (timed.Timeouts).Value.Callback.Invoke ();
        Assert.Equal (clickedCount, 1);

        // Move out of Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.Position = me.Position with { X = 1 };
        view.NewMouseEvent (me);

        Assert.Single (timed.Timeouts).Value.Callback.Invoke ();
        Assert.Equal (clickedCount, 2);

        me.Handled = false;

        // Move into Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.Position = me.Position with { X = 0 };
        view.NewMouseEvent (me);

        Assert.NotEmpty (timed.Timeouts);
        Assert.Equal (2, clickedCount);
        me.Handled = false;

        // Stay in Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.Position = me.Position with { X = 0 };
        view.NewMouseEvent (me);

        Assert.Single (timed.Timeouts).Value.Callback.Invoke ();

        Assert.Equal (3, clickedCount);
        me.Handled = false;

        view.Dispose ();
    }

    //[Theory]
    //[InlineData (true, MouseState.None, 0, 0, 0, 0)]
    //[InlineData (true, MouseState.In, 0, 0, 0, 0)]
    //[InlineData (true, MouseState.Pressed, 0, 0, 1, 0)]
    //[InlineData (true, MouseState.PressedOutside, 0, 0, 0, 1)]
    //public void MouseState_Button1_Pressed_Then_Released_Outside (bool inViewport, MouseState highlightFlags, int noneCount, int expectedInCount, int expectedPressedCount, int expectedPressedOutsideCount)
    //{
    //    var testView = new MouseEventTestView
    //    {
    //        HighlightStates = highlightFlags
    //    };

    //    Assert.Equal (0, testView.MouseStateInCount);
    //    Assert.Equal (0, testView.MouseStatePressedCount);
    //    Assert.Equal (0, testView.MouseStatePressedOutsideCount);
    //    Assert.Equal (0, testView.MouseStateNoneCount);

    //    testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed, Position = new (inViewport ? 0 : 1, 0) });
    //    Assert.Equal (expectedInCount, testView.MouseStateInCount);
    //    Assert.Equal (expectedPressedCount, testView.MouseStatePressedCount);
    //    Assert.Equal (expectedPressedOutsideCount, testView.MouseStatePressedOutsideCount);
    //    Assert.Equal (noneCount, testView.MouseStateNoneCount);

    //    testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Released, Position = new (inViewport ? 0 : 1, 0) });
    //    Assert.Equal (expectedInCount, testView.MouseStateInCount);
    //    Assert.Equal (expectedPressedCount, testView.MouseStatePressedCount);
    //    Assert.Equal (expectedPressedOutsideCount, testView.MouseStatePressedOutsideCount);
    //    Assert.Equal (noneCount, testView.MouseStateNoneCount);

    //    testView.Dispose ();

    //    // Button1Pressed, Button1Released cause Application.MouseGrabHandler.MouseGrabView to be set
    //    Application.ResetState (true);

    //}

    // TODO: Add tests for each combination of HighlightFlags

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_None_Button1_Pressed_Move_No_Changes (int x)
    {
        var testView = new MouseEventTestView
        {
            HighlightStates = MouseState.None
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (0, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0 
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed, Position = new (x, 0) });

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
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });

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

        // Button1Pressed, Button1Released cause Application.MouseGrabHandler.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_Pressed_Button1_Pressed_Move_Keeps_Pressed (int x)
    {
        var testView = new MouseEventTestView
        {
            HighlightStates = MouseState.Pressed
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (1, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0 
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed, Position = new (x, 0) });

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
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });

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

        // Button1Pressed, Button1Released cause Application.MouseGrabHandler.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_PressedOutside_Button1_Pressed_Move_Raises_PressedOutside (int x)
    {
        var testView = new MouseEventTestView
        {
            HighlightStates = MouseState.PressedOutside,
            WantContinuousButtonPressed = false
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (0, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0 
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed, Position = new (x, 0) });

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
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });

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

        // Button1Pressed, Button1Released cause Application.MouseGrabHandler.MouseGrabView to be set
        Application.ResetState (true);
    }


    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_PressedOutside_Button1_Pressed_Move_Raises_PressedOutside_WantContinuousButtonPressed (int x)
    {
        var testView = new MouseEventTestView
        {
            HighlightStates = MouseState.PressedOutside,
            WantContinuousButtonPressed = true
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (0, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0 
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed, Position = new (x, 0) });

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
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });

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

        // Button1Pressed, Button1Released cause Application.MouseGrabHandler.MouseGrabView to be set
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
