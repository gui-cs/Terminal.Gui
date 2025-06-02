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

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
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

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
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

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (MouseFlags.Button1Pressed, MouseFlags.Button1Released)]
    [InlineData (MouseFlags.Button2Pressed, MouseFlags.Button2Released)]
    [InlineData (MouseFlags.Button3Pressed, MouseFlags.Button3Released)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released)]
    public void WantContinuousButtonPressed_True_And_WantMousePositionReports_True_Button_Press_Release_Clicks (MouseFlags pressed, MouseFlags released)
    {
        Application.Init (new FakeDriver ());
        var me = new MouseEventArgs ();

        var view = new View
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true,
            WantMousePositionReports = true
        };

        var clickedCount = 0;

        view.MouseClick += (s, e) => clickedCount++;

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);
        me.Handled = false;

        me.Flags = released;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);

        view.Dispose ();

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
        Application.ResetState (true);
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
        Application.Init (new FakeDriver ());
        var me = new MouseEventArgs ();

        var view = new View
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true,
            WantMousePositionReports = true
        };

        var clickedCount = 0;

        view.MouseClick += (s, e) => clickedCount++;

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);
        me.Handled = false;

        me.Flags = released;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);
        me.Handled = false;

        me.Flags = clicked;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);

        view.Dispose ();

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Fact]
    public void WantContinuousButtonPressed_True_And_WantMousePositionReports_True_Move_InViewport_OutOfViewport_Keeps_Counting ()
    {
        Application.Init (new FakeDriver ());
        var me = new MouseEventArgs ();

        var view = new View
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true,
            WantMousePositionReports = true
        };

        var clickedCount = 0;

        view.MouseClick += (s, e) => clickedCount++;

        // Start in Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.Position = me.Position with { X = 0 };
        view.NewMouseEvent (me);
        Assert.Equal (0, clickedCount);
        me.Handled = false;

        // Move out of Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.Position = me.Position with { X = 1 };
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);
        me.Handled = false;

        // Move into Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.Position = me.Position with { X = 0 };
        view.NewMouseEvent (me);
        Assert.Equal (2, clickedCount);
        me.Handled = false;

        // Stay in Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.Position = me.Position with { X = 0 };
        view.NewMouseEvent (me);
        Assert.Equal (3, clickedCount);
        me.Handled = false;

        view.Dispose ();

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Theory (Skip = "This test needs to be redone.")]
    [InlineData (MouseState.None, 0, 0)]
    [InlineData (MouseState.Pressed | MouseState.PressedOutside, 1, 1)]
    public void HighlightOnPress_Fires_Events_And_Highlights (MouseState highlightOnPress, int expectedEnabling, int expectedDisabling)
    {
        var view = new View
        {
            CanFocus = true,
            HighlightStyle = highlightOnPress,
            Height = 1,
            Width = 1
        };

        var enablingHighlight = 0;
        var disablingHighlight = 0;
        view.MouseStateChanging += ViewHighlight;
        view.SetScheme (new (new Attribute (ColorName16.Red, ColorName16.Blue)));
        Scheme originalScheme = view.GetScheme ();

        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });

        if (highlightOnPress != MouseState.None)
        {
            Assert.NotEqual (originalScheme, view.GetScheme ());
        }
        else
        {
            Assert.Equal (originalScheme, view.GetScheme ());
        }

        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Released });
        Assert.Equal (originalScheme, view.GetScheme ());
        Assert.Equal (expectedEnabling, enablingHighlight);
        Assert.Equal (expectedDisabling, disablingHighlight);

        view.Dispose ();

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
        Application.ResetState (true);

        return;

        void ViewHighlight (object sender, CancelEventArgs<MouseState> e)
        {
            if (e.Result == MouseState.None)
            {
                disablingHighlight++;
            }
            else
            {
                enablingHighlight++;
            }
        }
    }

    // TODO: Add tests for each combination of HighlightFlags

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_None_Button1_Pressed_Move_No_Changes (int x)
    {
        var testView = new MouseEventTestView
        {
            HighlightStyle = MouseState.None
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (0, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0 
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

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
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
            HighlightStyle = MouseState.Pressed
        };

        bool inViewport = testView.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        testView.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (0, testView.MouseStateInCount);
        Assert.Equal (1, testView.MouseStatePressedCount);
        Assert.Equal (0, testView.MouseStatePressedOutsideCount);
        Assert.Equal (0, testView.MouseStateNoneCount);

        // Move to x,0 
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

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Theory]
    [InlineData (0)]
    [InlineData (1)]
    [InlineData (10)]
    public void MouseState_PressedOutside_Button1_Pressed_Move_Rises_PressedOutside (int x)
    {
        var testView = new MouseEventTestView
        {
            HighlightStyle = MouseState.PressedOutside,
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
            Assert.Equal (0, testView.MouseStateNoneCount);
        }

        testView.Dispose ();

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
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
        protected override bool OnMouseStateChanging (CancelEventArgs<MouseState> args)
        {
            switch (args.Result)
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

            return base.OnMouseStateChanging (args);
        }
    }
}
