using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class NavigationTests (ITestOutputHelper _output) : TestsAllViews
{
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_AtLeastOneNavKey_Leaves (Type viewType)
    {
        View view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (!view.CanFocus)
        {
            _output.WriteLine ($"Ignoring {viewType} - It can't focus.");

            return;
        }

        Application.Init (new FakeDriver ());

        Toplevel top = new ();

        View otherView = new ()
        {
            Id = "otherView",
            CanFocus = true,
            TabStop = view.TabStop
        };

        top.Add (view, otherView);
        Application.Begin (top);

        // Start with the focus on our test view
        view.SetFocus ();

        Key [] navKeys = { Key.Tab, Key.Tab.WithShift, Key.CursorUp, Key.CursorDown, Key.CursorLeft, Key.CursorRight };

        if (view.TabStop == TabBehavior.TabGroup)
        {
            navKeys = new [] { Key.F6, Key.F6.WithShift };
        }

        var left = false;

        foreach (Key key in navKeys)
        {
            switch (view.TabStop)
            {
                case TabBehavior.TabStop:
                case TabBehavior.NoStop:
                case TabBehavior.TabGroup:
                    Application.OnKeyDown (key);

                    break;
                default:
                    Application.OnKeyDown (Key.Tab);

                    break;
            }

            if (!view.HasFocus)
            {
                left = true;
                _output.WriteLine ($"{view.GetType ().Name} - {key} Left.");
                view.SetFocus ();
            }
            else
            {
                _output.WriteLine ($"{view.GetType ().Name} - {key} did not Leave.");
            }
        }

        top.Dispose ();
        Application.Shutdown ();

        Assert.True (left);
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Enter_Leave_Events (Type viewType)
    {
        View view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (!view.CanFocus)
        {
            _output.WriteLine ($"Ignoring {viewType} - It can't focus.");

            return;
        }

        if (view is Toplevel && ((Toplevel)view).Modal)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Modal Toplevel");

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
            Id = "otherView",
            X = 0, Y = 0,
            Height = 1,
            Width = 1,
            CanFocus = true,
            TabStop = view.TabStop
        };

        view.X = Pos.Right (otherView);
        view.Y = 0;
        view.Width = 10;
        view.Height = 1;

        var nEnter = 0;
        var nLeave = 0;

        view.Enter += (s, e) => nEnter++;
        view.Leave += (s, e) => nLeave++;

        top.Add (view, otherView);
        Assert.False (view.HasFocus);
        Assert.False (otherView.HasFocus);

        Application.Begin (top);
        Assert.True (Application.Current!.HasFocus);
        Assert.True (top.HasFocus);

        // Start with the focus on our test view
        Assert.True (view.HasFocus);

        Assert.Equal (1, nEnter);
        Assert.Equal (0, nLeave);

        // Use keyboard to navigate to next view (otherView).
        if (view is TextView)
        {
            Application.OnKeyDown (Key.F6);
        }
        else
        {
            var tries = 0;

            while (view.HasFocus)
            {
                if (++tries > 10)
                {
                    Assert.Fail ($"{view} is not leaving.");
                }

                switch (view.TabStop)
                {
                    case TabBehavior.NoStop:
                        Application.OnKeyDown (Key.Tab);

                        break;
                    case TabBehavior.TabStop:
                        Application.OnKeyDown (Key.Tab);

                        break;
                    case TabBehavior.TabGroup:
                        Application.OnKeyDown (Key.F6);

                        break;
                    case null:
                        Application.OnKeyDown (Key.Tab);

                        break;
                    default:
                        throw new ArgumentOutOfRangeException ();
                }
            }
        }

        Assert.Equal (1, nEnter);
        Assert.Equal (1, nLeave);

        Assert.False (view.HasFocus);
        Assert.True (otherView.HasFocus);

        // Now navigate back to our test view
        switch (view.TabStop)
        {
            case TabBehavior.NoStop:
                view.SetFocus ();

                break;
            case TabBehavior.TabStop:
                Application.OnKeyDown (Key.Tab);

                break;
            case TabBehavior.TabGroup:
                Application.OnKeyDown (Key.F6);

                break;
            case null:
                Application.OnKeyDown (Key.Tab);

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        Assert.Equal (2, nEnter);
        Assert.Equal (1, nLeave);

        Assert.True (view.HasFocus);
        Assert.False (otherView.HasFocus);

        // Cache state because Shutdown has side effects.
        // Also ensures other tests can continue running if there's a fail
        bool otherViewHasFocus = otherView.HasFocus;
        bool viewHasFocus = view.HasFocus;

        int enterCount = nEnter;
        int leaveCount = nLeave;

        top.Dispose ();
        Application.Shutdown ();

        Assert.False (otherViewHasFocus);
        Assert.True (viewHasFocus);

        Assert.Equal (2, enterCount);
        Assert.Equal (1, leaveCount);
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Enter_Leave_Events_Visible_False (Type viewType)
    {
        View view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (!view.CanFocus)
        {
            _output.WriteLine ($"Ignoring {viewType} - It can't focus.");

            return;
        }

        if (view is Toplevel && ((Toplevel)view).Modal)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Modal Toplevel");

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
            CanFocus = true
        };

        view.Visible = false;
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
            Application.OnKeyDown (Key.F6);
        }
        else if (view is DatePicker)
        {
            for (var i = 0; i < 4; i++)
            {
                Application.OnKeyDown (Key.F6);
            }
        }
        else
        {
            Application.OnKeyDown (Key.Tab);
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
    public void BringSubviewForward_Subviews_vs_TabIndexes ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        r.BringSubviewForward (v1);
        Assert.True (r.Subviews.IndexOf (v1) == 1);
        Assert.True (r.Subviews.IndexOf (v2) == 0);
        Assert.True (r.Subviews.IndexOf (v3) == 2);

        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v2) == 1);
        Assert.True (r.TabIndexes.IndexOf (v3) == 2);
        r.Dispose ();
    }

    [Fact]
    public void BringSubviewToFront_Subviews_vs_TabIndexes ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        r.BringSubviewToFront (v1);
        Assert.True (r.Subviews.IndexOf (v1) == 2);
        Assert.True (r.Subviews.IndexOf (v2) == 0);
        Assert.True (r.Subviews.IndexOf (v3) == 1);

        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v2) == 1);
        Assert.True (r.TabIndexes.IndexOf (v3) == 2);
        r.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Enabled_Sets_Also_Sets_Subviews ()
    {
        var wasClicked = false;
        var button = new Button { Text = "Click Me" };
        button.IsDefault = true;
        button.Accept += (s, e) => wasClicked = !wasClicked;
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (button);
        var top = new Toplevel ();
        top.Add (win);

        var iterations = 0;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     win.NewKeyDownEvent (Key.Enter);
                                     Assert.True (wasClicked);
                                     button.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked });
                                     Assert.False (wasClicked);
                                     Assert.True (button.Enabled);
                                     Assert.True (button.CanFocus);
                                     Assert.True (button.HasFocus);
                                     Assert.True (win.Enabled);
                                     Assert.True (win.CanFocus);
                                     Assert.True (win.HasFocus);

                                     win.Enabled = false;
                                     button.NewKeyDownEvent (Key.Enter);
                                     Assert.False (wasClicked);
                                     button.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked });
                                     Assert.False (wasClicked);
                                     Assert.False (button.Enabled);
                                     Assert.True (button.CanFocus);
                                     Assert.False (button.HasFocus);
                                     Assert.False (win.Enabled);
                                     Assert.True (win.CanFocus);
                                     Assert.False (win.HasFocus);
                                     button.SetFocus ();
                                     Assert.False (button.HasFocus);
                                     Assert.False (win.HasFocus);
                                     win.SetFocus ();
                                     Assert.False (button.HasFocus);
                                     Assert.False (win.HasFocus);

                                     win.Enabled = true;
                                     win.FocusDeepest (null, NavigationDirection.Forward);
                                     Assert.True (button.HasFocus);
                                     Assert.True (win.HasFocus);

                                     Application.RequestStop ();
                                 };

        Application.Run (top);

        Assert.Equal (1, iterations);
        top.Dispose ();
    }

    // View.Focused & View.MostFocused tests

    // View.Focused - No subviews
    [Fact]
    [Trait ("BUGBUG", "Fix in Issue #3444")]
    public void Focused_NoSubviews ()
    {
        var view = new View ();
        Assert.Null (view.GetFocused ());

        view.CanFocus = true;
        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Null (view.GetFocused ()); // BUGBUG: Should be view
    }

    [Fact]
    [AutoInitShutdown]
    public void FocusNearestView_Ensure_Focus_Ordered ()
    {
        var top = new Toplevel ();

        var win = new Window ();
        var winSubview = new View { CanFocus = true, Text = "WindowSubview" };
        win.Add (winSubview);
        top.Add (win);

        var frm = new FrameView ();
        var frmSubview = new View { CanFocus = true, Text = "FrameSubview" };
        frm.Add (frmSubview);
        top.Add (frm);

        Application.Begin (top);
        Assert.Equal ("WindowSubview", top.GetMostFocused ().Text);

        Application.OnKeyDown (Key.Tab);
        Assert.Equal ("WindowSubview", top.GetMostFocused ().Text);

        Application.OnKeyDown (Key.F6);
        Assert.Equal ("FrameSubview", top.GetMostFocused ().Text);

        Application.OnKeyDown (Key.Tab);
        Assert.Equal ("FrameSubview", top.GetMostFocused ().Text);

        Application.OnKeyDown (Key.F6);
        Assert.Equal ("WindowSubview", top.GetMostFocused ().Text);

        Application.OnKeyDown (Key.F6.WithShift);
        Assert.Equal ("FrameSubview", top.GetMostFocused ().Text);

        Application.OnKeyDown (Key.F6.WithShift);
        Assert.Equal ("WindowSubview", top.GetMostFocused ().Text);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void FocusNext_Does_Not_Throws_If_A_View_Was_Removed_From_The_Collection ()
    {
        Toplevel top1 = new ();
        var view1 = new View { Id = "view1", Width = 10, Height = 5, CanFocus = true };
        var top2 = new Toplevel { Id = "top2", Y = 1, Width = 10, Height = 5 };

        var view2 = new View
        {
            Id = "view2",
            Y = 1,
            Width = 10,
            Height = 5,
            CanFocus = true
        };
        View view3 = null;
        var removed = false;

        view2.Enter += (s, e) =>
                       {
                           if (!removed)
                           {
                               removed = true;
                               view3 = new () { Id = "view3", Y = 1, Width = 10, Height = 5 };
                               Application.Current.Add (view3);
                               Application.Current.BringSubviewToFront (view3);
                               Assert.False (view3.HasFocus);
                           }
                       };

        view2.Leave += (s, e) =>
                       {
                           Application.Current.Remove (view3);
                           view3.Dispose ();
                           view3 = null;
                       };
        top2.Add (view2);
        top1.Add (view1, top2);
        Application.Begin (top1);

        Assert.True (top1.HasFocus);
        Assert.True (view1.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.False (removed);
        Assert.Null (view3);

        Assert.True (Application.OnKeyDown (Key.F6));
        Assert.True (top1.HasFocus);
        Assert.False (view1.HasFocus);
        Assert.True (view2.HasFocus);
        Assert.True (removed);
        Assert.NotNull (view3);

        Exception exception = Record.Exception (() => Application.OnKeyDown (Key.F6));
        Assert.Null (exception);
        Assert.True (removed);
        //Assert.Null (view3);
        top1.Dispose ();
    }

    [Fact]
    public void GetMostFocused_NoSubviews_Returns_Null ()
    {
        var view = new View ();
        Assert.Null (view.GetFocused ());

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Null (view.GetMostFocused ());
    }

    [Fact]
    public void GetMostFocused_Returns_Most ()
    {
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };

        var subview = new View ()
        {
            Id = "subview",
            CanFocus = true
        };

        view.Add (subview);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subview.HasFocus);
        Assert.Equal (subview, view.GetMostFocused ());

        var subview2 = new View ()
        {
            Id = "subview2",
            CanFocus = true
        };

        view.Add (subview2);
        Assert.Equal (subview2, view.GetMostFocused ());
    }

    //    [Fact]
    //    [AutoInitShutdown]
    //    public void HotKey_Will_Invoke_KeyPressed_Only_For_The_MostFocused_With_Top_KeyPress_Event ()
    //    {
    //        var sbQuiting = false;
    //        var tfQuiting = false;
    //        var topQuiting = false;

    //        var sb = new StatusBar (
    //                                new Shortcut []
    //                                {
    //                                    new (
    //                                         KeyCode.CtrlMask | KeyCode.Q,
    //                                         "Quit",
    //                                         () => sbQuiting = true
    //                                        )
    //                                }
    //                               );
    //        var tf = new TextField ();
    //        tf.KeyDown += Tf_KeyPressed;

    //        void Tf_KeyPressed (object sender, Key obj)
    //        {
    //            if (obj.KeyCode == (KeyCode.Q | KeyCode.CtrlMask))
    //            {
    //                obj.Handled = tfQuiting = true;
    //            }
    //        }

    //        var win = new Window ();
    //        win.Add (sb, tf);
    //        Toplevel top = new ();
    //        top.KeyDown += Top_KeyPress;

    //        void Top_KeyPress (object sender, Key obj)
    //        {
    //            if (obj.KeyCode == (KeyCode.Q | KeyCode.CtrlMask))
    //            {
    //                obj.Handled = topQuiting = true;
    //            }
    //        }

    //        top.Add (win);
    //        Application.Begin (top);

    //        Assert.False (sbQuiting);
    //        Assert.False (tfQuiting);
    //        Assert.False (topQuiting);

    //        Application.Driver?.SendKeys ('Q', ConsoleKey.Q, false, false, true);
    //        Assert.False (sbQuiting);
    //        Assert.True (tfQuiting);
    //        Assert.False (topQuiting);

    //#if BROKE_WITH_2927
    //        tf.KeyPressed -= Tf_KeyPress;
    //        tfQuiting = false;
    //        Application.Driver?.SendKeys ('q', ConsoleKey.Q, false, false, true);
    //        Application.MainLoop.RunIteration ();
    //        Assert.True (sbQuiting);
    //        Assert.False (tfQuiting);
    //        Assert.False (topQuiting);

    //        sb.RemoveItem (0);
    //        sbQuiting = false;
    //        Application.Driver?.SendKeys ('q', ConsoleKey.Q, false, false, true);
    //        Application.MainLoop.RunIteration ();
    //        Assert.False (sbQuiting);
    //        Assert.False (tfQuiting);

    //// This test is now invalid because `win` is focused, so it will receive the keypress
    //        Assert.True (topQuiting);
    //#endif
    //        top.Dispose ();
    //    }

    //    [Fact]
    //    [AutoInitShutdown]
    //    public void HotKey_Will_Invoke_KeyPressed_Only_For_The_MostFocused_Without_Top_KeyPress_Event ()
    //    {
    //        var sbQuiting = false;
    //        var tfQuiting = false;

    //        var sb = new StatusBar (
    //                                new Shortcut []
    //                                {
    //                                    new (
    //                                         KeyCode.CtrlMask | KeyCode.Q,
    //                                         "~^Q~ Quit",
    //                                         () => sbQuiting = true
    //                                        )
    //                                }
    //                               );
    //        var tf = new TextField ();
    //        tf.KeyDown += Tf_KeyPressed;

    //        void Tf_KeyPressed (object sender, Key obj)
    //        {
    //            if (obj.KeyCode == (KeyCode.Q | KeyCode.CtrlMask))
    //            {
    //                obj.Handled = tfQuiting = true;
    //            }
    //        }

    //        var win = new Window ();
    //        win.Add (sb, tf);
    //        Toplevel top = new ();
    //        top.Add (win);
    //        Application.Begin (top);

    //        Assert.False (sbQuiting);
    //        Assert.False (tfQuiting);

    //        Application.Driver?.SendKeys ('Q', ConsoleKey.Q, false, false, true);
    //        Assert.False (sbQuiting);
    //        Assert.True (tfQuiting);

    //        tf.KeyDown -= Tf_KeyPressed;
    //        tfQuiting = false;
    //        Application.Driver?.SendKeys ('Q', ConsoleKey.Q, false, false, true);
    //        Application.MainLoop.RunIteration ();
    //#if BROKE_WITH_2927
    //        Assert.True (sbQuiting);
    //        Assert.False (tfQuiting);
    //#endif
    //        top.Dispose ();
    //    }

    [Fact]
    [SetupFakeDriver]
    public void Navigation_With_Null_Focused_View ()
    {
        // Non-regression test for #882 (NullReferenceException during keyboard navigation when Focused is null)

        Application.Init (new FakeDriver ());

        var top = new Toplevel ();
        top.Ready += (s, e) => { Assert.Null (top.GetFocused ()); };

        // Keyboard navigation with tab
        FakeConsole.MockKeyPresses.Push (new ('\t', ConsoleKey.Tab, false, false, false));

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
    }

#if V2_NEW_FOCUS_IMPL // bogus test - Depends on auto setting of CanFocus
    [Fact]
    [AutoInitShutdown]
    public void Remove_Does_Not_Change_Focus ()
    {
        var top = new Toplevel ();
        Assert.True (top.CanFocus);
        Assert.False (top.HasFocus);

        var container = new View { Width = 10, Height = 10 };
        var leave = false;
        container.Leave += (s, e) => leave = true;
        Assert.False (container.CanFocus);
        var child = new View { Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
        container.Add (child);

        Assert.True (container.CanFocus);
        Assert.False (container.HasFocus);
        Assert.True (child.CanFocus);
        Assert.False (child.HasFocus);

        top.Add (container);
        Application.Begin (top);

        Assert.True (top.CanFocus);
        Assert.True (top.HasFocus);
        Assert.True (container.CanFocus);
        Assert.True (container.HasFocus);
        Assert.True (child.CanFocus);
        Assert.True (child.HasFocus);

        container.Remove (child);
        child.Dispose ();
        child = null;
        Assert.True (top.HasFocus);
        Assert.True (container.CanFocus);
        Assert.True (container.HasFocus);
        Assert.Null (child);
        Assert.False (leave);
        top.Dispose ();
    }
#endif

    [Fact]
    [AutoInitShutdown]
    public void ScreenToView_ViewToScreen_FindDeepestView_Full_Top ()
    {
        Toplevel top = new ();
        top.BorderStyle = LineStyle.Single;

        var view = new View
        {
            X = 3,
            Y = 2,
            Width = 10,
            Height = 1,
            Text = "0123456789"
        };
        top.Add (view);

        Application.Begin (top);

        Assert.Equal (Application.Current, top);
        Assert.Equal (new (0, 0, 80, 25), new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows));
        Assert.Equal (new (0, 0, View.Driver.Cols, View.Driver.Rows), top.Frame);
        Assert.Equal (new (0, 0, 80, 25), top.Frame);

        ((FakeDriver)Application.Driver!).SetBufferSize (20, 10);
        Assert.Equal (new (0, 0, View.Driver.Cols, View.Driver.Rows), top.Frame);
        Assert.Equal (new (0, 0, 20, 10), top.Frame);

        _ = TestHelpers.AssertDriverContentsWithFrameAre (
                                                          @"
┌──────────────────┐
│                  │
│                  │
│   0123456789     │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘",
                                                          _output
                                                         );

        // top
        Assert.Equal (Point.Empty, top.ScreenToFrame (new (0, 0)));
        Point screen = top.Margin.ViewportToScreen (new Point (0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = top.Border.ViewportToScreen (new Point (0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = top.Padding.ViewportToScreen (new Point (0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = top.ViewportToScreen (new Point (0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = top.ViewportToScreen (new Point (-1, -1));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        var found = View.FindDeepestView (top, new (0, 0));
        Assert.Equal (top.Border, found);

        Assert.Equal (0, found.Frame.X);
        Assert.Equal (0, found.Frame.Y);
        Assert.Equal (new (3, 2), top.ScreenToFrame (new (3, 2)));
        screen = top.ViewportToScreen (new Point (3, 2));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, new (screen.X, screen.Y));
        Assert.Equal (view, found);

        //Assert.Equal (0, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        found = View.FindDeepestView (top, new (3, 2));
        Assert.Equal (top, found);

        //Assert.Equal (3, found.FrameToScreen ().X);
        //Assert.Equal (2, found.FrameToScreen ().Y);
        Assert.Equal (new (13, 2), top.ScreenToFrame (new (13, 2)));
        screen = top.ViewportToScreen (new Point (12, 2));
        Assert.Equal (13, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, new (screen.X, screen.Y));
        Assert.Equal (view, found);

        //Assert.Equal (9, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        screen = top.ViewportToScreen (new Point (13, 2));
        Assert.Equal (14, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, new (13, 2));
        Assert.Equal (top, found);

        //Assert.Equal (13, found.FrameToScreen ().X);
        //Assert.Equal (2, found.FrameToScreen ().Y);
        Assert.Equal (new (14, 3), top.ScreenToFrame (new (14, 3)));
        screen = top.ViewportToScreen (new Point (14, 3));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        found = View.FindDeepestView (top, new (14, 3));
        Assert.Equal (top, found);

        //Assert.Equal (14, found.FrameToScreen ().X);
        //Assert.Equal (3, found.FrameToScreen ().Y);

        // view
        Assert.Equal (new (-4, -3), view.ScreenToFrame (new (0, 0)));
        screen = view.Margin.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Border.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Padding.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.ViewportToScreen (new Point (-4, -3));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        found = View.FindDeepestView (top, new (0, 0));
        Assert.Equal (top.Border, found);

        Assert.Equal (new (-1, -1), view.ScreenToFrame (new (3, 2)));
        screen = view.ViewportToScreen (new Point (0, 0));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, new (4, 3));
        Assert.Equal (view, found);

        Assert.Equal (new (9, -1), view.ScreenToFrame (new (13, 2)));
        screen = view.ViewportToScreen (new Point (10, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, new (14, 3));
        Assert.Equal (top, found);

        Assert.Equal (new (10, 0), view.ScreenToFrame (new (14, 3)));
        screen = view.ViewportToScreen (new Point (11, 1));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        found = View.FindDeepestView (top, new (15, 4));
        Assert.Equal (top, found);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ScreenToView_ViewToScreen_FindDeepestView_Smaller_Top ()
    {
        var top = new Toplevel
        {
            X = 3,
            Y = 2,
            Width = 20,
            Height = 10,
            BorderStyle = LineStyle.Single
        };

        var view = new View
        {
            X = 3,
            Y = 2,
            Width = 10,
            Height = 1,
            Text = "0123456789"
        };
        top.Add (view);

        Application.Begin (top);

        Assert.Equal (Application.Current, top);
        Assert.Equal (new (0, 0, 80, 25), new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows));
        Assert.NotEqual (new (0, 0, View.Driver.Cols, View.Driver.Rows), top.Frame);
        Assert.Equal (new (3, 2, 20, 10), top.Frame);

        ((FakeDriver)Application.Driver!).SetBufferSize (30, 20);
        Assert.Equal (new (0, 0, 30, 20), new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows));
        Assert.NotEqual (new (0, 0, View.Driver.Cols, View.Driver.Rows), top.Frame);
        Assert.Equal (new (3, 2, 20, 10), top.Frame);

        Rectangle frame = TestHelpers.AssertDriverContentsWithFrameAre (
                                                                        @"
   ┌──────────────────┐
   │                  │
   │                  │
   │   0123456789     │
   │                  │
   │                  │
   │                  │
   │                  │
   │                  │
   └──────────────────┘",
                                                                        _output
                                                                       );

        // mean the output started at col 3 and line 2
        // which result with a width of 23 and a height of 10 on the output
        Assert.Equal (new (3, 2, 23, 10), frame);

        // top
        Assert.Equal (new (-3, -2), top.ScreenToFrame (new (0, 0)));
        Point screen = top.Margin.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = top.Border.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = top.Padding.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = top.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = top.ViewportToScreen (new Point (-4, -3));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        var found = View.FindDeepestView (top, new (-4, -3));
        Assert.Null (found);
        Assert.Equal (Point.Empty, top.ScreenToFrame (new (3, 2)));
        screen = top.ViewportToScreen (new Point (0, 0));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        Assert.Equal (top.Border, View.FindDeepestView (top, new (3, 2)));

        //Assert.Equal (0, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        Assert.Equal (new (10, 0), top.ScreenToFrame (new (13, 2)));
        screen = top.ViewportToScreen (new Point (10, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (3, screen.Y);
        Assert.Equal (top.Border, View.FindDeepestView (top, new (13, 2)));

        //Assert.Equal (10, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        Assert.Equal (new (11, 1), top.ScreenToFrame (new (14, 3)));
        screen = top.ViewportToScreen (new Point (11, 1));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        Assert.Equal (top, View.FindDeepestView (top, new (14, 3)));

        // view
        Assert.Equal (new (-7, -5), view.ScreenToFrame (new (0, 0)));
        screen = view.Margin.ViewportToScreen (new Point (-6, -4));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Border.ViewportToScreen (new Point (-6, -4));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Padding.ViewportToScreen (new Point (-6, -4));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.ViewportToScreen (new Point (-6, -4));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        Assert.Null (View.FindDeepestView (top, new (1, 1)));
        Assert.Equal (new (-4, -3), view.ScreenToFrame (new (3, 2)));
        screen = view.ViewportToScreen (new Point (-3, -2));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        Assert.Equal (top, View.FindDeepestView (top, new (4, 3)));
        Assert.Equal (new (-1, -1), view.ScreenToFrame (new (6, 4)));
        screen = view.ViewportToScreen (new Point (0, 0));
        Assert.Equal (7, screen.X);
        Assert.Equal (5, screen.Y);
        Assert.Equal (view, View.FindDeepestView (top, new (7, 5)));
        Assert.Equal (new (6, -1), view.ScreenToFrame (new (13, 4)));
        screen = view.ViewportToScreen (new Point (7, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (5, screen.Y);
        Assert.Equal (view, View.FindDeepestView (top, new (14, 5)));
        Assert.Equal (new (7, -2), view.ScreenToFrame (new (14, 3)));
        screen = view.ViewportToScreen (new Point (8, -1));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        Assert.Equal (top, View.FindDeepestView (top, new (15, 4)));
        Assert.Equal (new (16, -2), view.ScreenToFrame (new (23, 3)));
        screen = view.ViewportToScreen (new Point (17, -1));
        Assert.Equal (24, screen.X);
        Assert.Equal (4, screen.Y);
        Assert.Null (View.FindDeepestView (top, new (24, 4)));
        top.Dispose ();
    }
}
