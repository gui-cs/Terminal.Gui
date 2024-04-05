using Terminal.Gui.ViewsTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class MouseTests (ITestOutputHelper output)
{
    [Theory]
    [InlineData (false, false, false)]
    [InlineData (true, false, true)]
    [InlineData (true, true, true)]
    public void MouseClick_SetsFocus_If_CanFocus (bool canFocus, bool setFocus, bool expectedHasFocus)
    {
        var superView = new View { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View { CanFocus = true, Width = 1, Height = 1 };
        var testView = new View { CanFocus = canFocus, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, testView);

        focusedView.SetFocus ();

        Assert.True (superView.HasFocus);
        Assert.True (focusedView.HasFocus);
        Assert.False (testView.HasFocus);

        if (setFocus)
        {
            testView.SetFocus ();
        }

        testView.NewMouseEvent (new () { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked });
        Assert.True (superView.HasFocus);
        Assert.Equal (expectedHasFocus, testView.HasFocus);
    }

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
        Application.Begin (top);

        Assert.Equal (new Point (4, 4), testView.Frame.Location);
        Application.OnMouseEvent (new () { X = xy, Y = xy, Flags = MouseFlags.Button1Pressed });

        Application.OnMouseEvent (new () { X = xy + 1, Y = xy + 1, Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition });

        Assert.Equal (expectedMoved, new Point (5, 5) == testView.Frame.Location);
    }

    [Theory]
    [InlineData (MouseFlags.WheeledUp | MouseFlags.ButtonCtrl, MouseFlags.WheeledLeft)]
    [InlineData (MouseFlags.WheeledDown | MouseFlags.ButtonCtrl, MouseFlags.WheeledRight)]
    public void WheeledLeft_WheeledRight (MouseFlags mouseFlags, MouseFlags expectedMouseFlagsFromEvent)
    {
        MouseFlags mouseFlagsFromEvent = MouseFlags.None;
        var view = new View ();
        view.MouseEvent += (s, e) => mouseFlagsFromEvent = e.MouseEvent.Flags;

        view.NewMouseEvent (new MouseEvent () { Flags = mouseFlags });
        Assert.Equal (mouseFlagsFromEvent, expectedMouseFlagsFromEvent);
    }

    public static TheoryData<View, string> AllViews => TestHelpers.GetAllViewsTheoryData ();


    [Theory]
    [MemberData (nameof (AllViews))]

    public void AllViews_Enter_Leave_Events (View view, string viewName)
    {
        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewName} - It's a Generic");
            return;
        }

        if (!view.CanFocus)
        {
            output.WriteLine ($"Ignoring {viewName} - It can't focus.");

            return;
        }

        if (view is Toplevel && ((Toplevel)view).Modal)
        {
            output.WriteLine ($"Ignoring {viewName} - It's a Modal Toplevel");

            return;
        }

        Application.Init (new FakeDriver ());

        Toplevel top = new ()
        {
            Height = 10,
            Width = 10
        };

        View otherView = new ()
        {
            X = 0, Y = 0,
            Height = 1,
            Width = 1,
            CanFocus = true,
        };

        view.AutoSize = false;
        view.X = Pos.Right (otherView);
        view.Y = 0;
        view.Width = 10;
        view.Height = 1;

        var nEnter = 0;
        var nLeave = 0;

        view.Enter += (s, e) => nEnter++;
        view.Leave += (s, e) => nLeave++;

        top.Add (view, otherView);
        Application.Begin (top);

        // Start with the focus on our test view
        view.SetFocus ();

        Assert.Equal (1, nEnter);
        Assert.Equal (0, nLeave);

        // Use keyboard to navigate to next view (otherView). 
        if (view is TextView)
        {
            top.NewKeyDownEvent (Key.Tab.WithCtrl);
        }
        else if (view is DatePicker)
        {
            for (var i = 0; i < 4; i++)
            {
                top.NewKeyDownEvent (Key.Tab.WithCtrl);
            }
        }
        else
        {
            top.NewKeyDownEvent (Key.Tab);
        }

        Assert.Equal (1, nEnter);
        Assert.Equal (1, nLeave);

        top.NewKeyDownEvent (Key.Tab);

        Assert.Equal (2, nEnter);
        Assert.Equal (1, nLeave);

        top.Dispose ();
        Application.Shutdown ();
    }


    [Theory]
    [MemberData (nameof (AllViews))]

    public void AllViews_Enter_Leave_Events_Visible_False (View view, string viewName)
    {
        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewName} - It's a Generic");
            return;
        }

        if (!view.CanFocus)
        {
            output.WriteLine ($"Ignoring {viewName} - It can't focus.");

            return;
        }

        if (view is Toplevel && ((Toplevel)view).Modal)
        {
            output.WriteLine ($"Ignoring {viewName} - It's a Modal Toplevel");

            return;
        }

        Application.Init (new FakeDriver ());

        Toplevel top = new ()
        {
            Height = 10,
            Width = 10
        };

        View otherView = new ()
        {
            X = 0, Y = 0,
            Height = 1,
            Width = 1,
            CanFocus = true,
        };

        view.Visible = false;
        view.AutoSize = false;
        view.X = Pos.Right (otherView);
        view.Y = 0;
        view.Width = 10;
        view.Height = 1;

        var nEnter = 0;
        var nLeave = 0;

        view.Enter += (s, e) => nEnter++;
        view.Leave += (s, e) => nLeave++;

        top.Add (view, otherView);
        Application.Begin (top);

        // Start with the focus on our test view
        view.SetFocus ();

        Assert.Equal (0, nEnter);
        Assert.Equal (0, nLeave);

        // Use keyboard to navigate to next view (otherView). 
        if (view is TextView)
        {
            top.NewKeyDownEvent (Key.Tab.WithCtrl);
        }
        else if (view is DatePicker)
        {
            for (var i = 0; i < 4; i++)
            {
                top.NewKeyDownEvent (Key.Tab.WithCtrl);
            }
        }
        else
        {
            top.NewKeyDownEvent (Key.Tab);
        }

        Assert.Equal (0, nEnter);
        Assert.Equal (0, nLeave);

        top.NewKeyDownEvent (Key.Tab);

        Assert.Equal (0, nEnter);
        Assert.Equal (0, nLeave);

        top.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    public void NewMouseEvent_Invokes_MouseEvent_Properly ()
    {
        View view = new ()
        {
            Width = 1,
            Height = 1,
        };
        bool mouseEventInvoked = false;
        view.MouseEvent += (s, e) =>
                           {
                               mouseEventInvoked = true;
                               e.Handled = true;
                           };

        MouseEvent me = new ();
        view.NewMouseEvent (me);
        Assert.True (mouseEventInvoked);
        Assert.True (me.Handled);

        view.Dispose ();
    }

    [Theory]
    [MemberData (nameof (AllViews))]
    public void AllViews_NewMouseEvent_Enabled_False_Does_Not_Set_Handled (View view, string viewName)
    {
        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewName} - It's a Generic");
            return;
        }

        view.Enabled = false;
        var me = new MouseEvent ();
        view.NewMouseEvent (me);
        Assert.False (me.Handled);
        view.Dispose ();
    }

    [Theory]
    [MemberData (nameof (AllViews))]
    public void AllViews_NewMouseEvent_Clicked_Enabled_False_Does_Not_Set_Handled (View view, string viewName)
    {
        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewName} - It's a Generic");
            return;
        }

        view.Enabled = false;
        var me = new MouseEvent ()
        {
            Flags = MouseFlags.Button1Clicked
        };
        view.NewMouseEvent (me);
        Assert.False (me.Handled);
        view.Dispose ();
    }

    [Theory]
    [InlineData (MouseFlags.Button1Pressed, MouseFlags.Button1Released, MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Pressed, MouseFlags.Button2Released, MouseFlags.Button2Clicked)]
    [InlineData (MouseFlags.Button3Pressed, MouseFlags.Button3Released, MouseFlags.Button3Clicked)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released, MouseFlags.Button4Clicked)]
    public void WantContinuousButtonPressed_False_Button_Press_Release_DoesNotClick (MouseFlags pressed, MouseFlags released, MouseFlags clicked)
    {
        var me = new MouseEvent ();

        var view = new View ()
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

        me.Flags =clicked;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);

        view.Dispose ();
    }

    [Theory]
    [InlineData (MouseFlags.Button1Pressed, MouseFlags.Button1Released, MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Pressed, MouseFlags.Button2Released, MouseFlags.Button2Clicked)]
    [InlineData (MouseFlags.Button3Pressed, MouseFlags.Button3Released, MouseFlags.Button3Clicked)]
    [InlineData (MouseFlags.Button4Pressed, MouseFlags.Button4Released, MouseFlags.Button4Clicked)]
    public void WantContinuousButtonPressed_True_Button_Press_Release_Clicks_Repeatedly (MouseFlags pressed, MouseFlags released, MouseFlags clicked)
    {
        var me = new MouseEvent ();

        var view = new View ()
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true
        };

        var clickedCount = 0;

        view.MouseClick += (s, e) => clickedCount++;

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);
        me.Handled = false;

        me.Flags = pressed;
        view.NewMouseEvent (me);
        Assert.Equal (2, clickedCount);
        me.Handled = false;

        me.Flags = released;
        view.NewMouseEvent (me);
        Assert.Equal (2, clickedCount);
        me.Handled = false;

        me.Flags = clicked;
        view.NewMouseEvent (me);
        Assert.Equal (2, clickedCount);

        view.Dispose ();
    }

    [Fact]
    public void WantContinuousButtonPressed_True_Move_InViewport_OutOfViewport_Keeps_Counting ()
    {
        var me = new MouseEvent ();

        var view = new View ()
        {
            Width = 1,
            Height = 1,
            WantContinuousButtonPressed = true
        };

        var clickedCount = 0;

        view.MouseClick += (s, e) => clickedCount++;

        // Start in Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.X = 0;
        view.NewMouseEvent (me);
        Assert.Equal (1, clickedCount);
        me.Handled = false;

        // Move out of Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.X = 1;
        view.NewMouseEvent (me);
        Assert.Equal (2, clickedCount);
        me.Handled = false;

        // Move into Viewport
        me.Flags = MouseFlags.Button1Pressed;
        me.X = 0;
        view.NewMouseEvent (me);
        Assert.Equal (3, clickedCount);
        me.Handled = false;

        view.Dispose ();
    }

    [Theory]
    [InlineData (HighlightStyle.None, 0, 0)]
    [InlineData (HighlightStyle.Pressed | HighlightStyle.PressedOutside, 1, 1)]
    public void HighlightOnPress_Fires_Events_And_Highlights (HighlightStyle highlightOnPress, int expectedEnabling, int expectedDisabling)
    {
        var view = new View ()
        {
            HighlightStyle = highlightOnPress,
            Height = 1,
            Width = 1
        };

        int enablingHighlight = 0;
        int disablingHighlight = 0;
        view.Highlight += View_Highlight;
        view.ColorScheme = new ColorScheme (new Attribute (ColorName.Red, ColorName.Blue));
        ColorScheme originalColorScheme = view.ColorScheme;

        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed, });

        if (highlightOnPress != HighlightStyle.None)
        {
            Assert.NotEqual (originalColorScheme, view.ColorScheme);
        }
        else
        {
            Assert.Equal (originalColorScheme, view.ColorScheme);
        }

        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Released, });
        Assert.Equal (originalColorScheme, view.ColorScheme);
        Assert.Equal (expectedEnabling, enablingHighlight);
        Assert.Equal (expectedDisabling, disablingHighlight);

        view.Dispose ();

        return;

        void View_Highlight (object sender, HighlightEventArgs e)
        {
            if (e.HighlightStyle == HighlightStyle.None)
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
        var view = new View ()
        {
            HighlightStyle = HighlightStyle.Pressed | HighlightStyle.PressedOutside,
            Height = 1,
            Width = 1
        };
        int enablingHighlight = 0;
        int disablingHighlight = 0;
        view.Highlight += View_Highlight;
        bool inViewport = view.Bounds.Contains (x, 0);

        // Start at 0,0 ; in viewport
        view.NewMouseEvent (new () { X = 0, Flags = MouseFlags.Button1Pressed });
        Assert.Equal (1, enablingHighlight);
        Assert.Equal (0, disablingHighlight);

        // Move to x,0 
        view.NewMouseEvent (new () { X = x, Flags = MouseFlags.Button1Pressed });

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
        view.NewMouseEvent (new () { X = 0, Flags = MouseFlags.Button1Pressed });
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

        return;

        void View_Highlight (object sender, HighlightEventArgs e)
        {
            if (e.HighlightStyle == HighlightStyle.None)
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
