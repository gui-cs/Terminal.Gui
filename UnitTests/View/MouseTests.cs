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

        testView.OnMouseEvent (new () { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked });
        Assert.True (superView.HasFocus);
        Assert.Equal (expectedHasFocus, testView.HasFocus);
    }

    // TODO: Add more tests that ensure the above test works with positive adornments

    // Test drag to move
    [Theory]
    [InlineData (0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 4, false)]
    [InlineData (1, 0, 0, 4, true)]
    [InlineData (0, 1, 0, 4, true)]
    [InlineData (0, 0, 1, 4, false)]

    [InlineData (1, 1, 0, 3, false)]
    [InlineData (1, 1, 0, 4, true)]
    [InlineData (1, 1, 0, 5, true)]
    [InlineData (1, 1, 0, 6, false)]


    [InlineData (1, 1, 0, 11, false)]
    [InlineData (1, 1, 0, 12, true)]
    [InlineData (1, 1, 0, 13, true)]
    [InlineData (1, 1, 0, 14, false)]
    [AutoInitShutdown]
    public void ButtonPressed_In_Margin_Or_Border_Starts_Drag (int marginThickness, int borderThickness, int paddingThickness, int xy, bool expectedMoved)
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
        Application.OnMouseEvent (new (new () { X = xy, Y = xy, Flags = MouseFlags.Button1Pressed }));

        Assert.False (Application.MouseGrabView is { } && (Application.MouseGrabView != testView.Margin && Application.MouseGrabView != testView.Border));

        Application.OnMouseEvent (new (new () { X = xy + 1, Y = xy + 1, Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition }));

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

        view.OnMouseEvent (new MouseEvent () { Flags = mouseFlags });
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
            Width  = 1,
            CanFocus = true,
        };

        view.AutoSize = false;
        view.X = Pos.Right(otherView);
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

    [Theory]
    [MemberData (nameof (AllViews))]
    public void AllViews_OnMouseEvent_Enabled_False_Does_Not_Set_Handled (View view, string viewName)
    {
        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewName} - It's a Generic");
            return;
        }

        view.Enabled = false;
        var me = new MouseEvent ();
        view.OnMouseEvent (me);
        Assert.False (me.Handled);
        view.Dispose ();
    }

    [Theory]
    [MemberData (nameof (AllViews))]
    public void AllViews_OnMouseClick_Enabled_False_Does_Not_Set_Handled (View view, string viewName)
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
        view.OnMouseEvent (me);
        Assert.False (me.Handled);
        view.Dispose ();
    }
}
