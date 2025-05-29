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

        view.Dispose ();

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
        Application.ResetState (true);
    }

    [Theory (Skip = "This test needs to be redone.")]
    [InlineData (HighlightStyle.None, 0, 0)]
    [InlineData (HighlightStyle.Pressed | HighlightStyle.PressedOutside, 1, 1)]
    public void HighlightOnPress_Fires_Events_And_Highlights (HighlightStyle highlightOnPress, int expectedEnabling, int expectedDisabling)
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
        view.Highlight += ViewHighlight;
        view.SetScheme (new (new Attribute (ColorName16.Red, ColorName16.Blue)));
        Scheme originalScheme = view.GetScheme ();

        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });

        if (highlightOnPress != HighlightStyle.None)
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

        void ViewHighlight (object sender, CancelEventArgs<HighlightStyle> e)
        {
            if (e.NewValue == HighlightStyle.None)
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
    public void HighlightOnPress_Move_Keeps_Highlight (int x)
    {
        var view = new View
        {
            CanFocus = true,
            HighlightStyle = HighlightStyle.Pressed | HighlightStyle.PressedOutside,
            Height = 1,
            Width = 1
        };
        var enablingHighlight = 0;
        var disablingHighlight = 0;
        view.Highlight += ViewHighlight;
        bool inViewport = view.Viewport.Contains (x, 0);

        // Start at 0,0 ; in viewport
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });
        Assert.Equal (1, enablingHighlight);
        Assert.Equal (0, disablingHighlight);

        // Move to x,0 
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });

        if (inViewport)
        {
            Assert.Equal (2, enablingHighlight);
            Assert.Equal (0, disablingHighlight);
        }
        else
        {
            Assert.Equal (2, enablingHighlight);
            Assert.Equal (0, disablingHighlight);
        }

        // Move backto 0,0 ; in viewport
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed });

        if (inViewport)
        {
            Assert.Equal (3, enablingHighlight);
            Assert.Equal (0, disablingHighlight);
        }
        else
        {
            Assert.Equal (3, enablingHighlight);
            Assert.Equal (0, disablingHighlight);
        }

        view.Dispose ();

        // Button1Pressed, Button1Released cause Application.MouseGrabView to be set
        Application.ResetState (true);

        return;

        void ViewHighlight (object sender, CancelEventArgs<HighlightStyle> e)
        {
            if (e.NewValue == HighlightStyle.None)
            {
                disablingHighlight++;
            }
            else
            {
                enablingHighlight++;
            }
        }
    }
}
