using Xunit.Abstractions;
using static System.Net.Mime.MediaTypeNames;

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
            TabStop = view.TabStop == TabBehavior.NoStop ? TabBehavior.TabStop : view.TabStop
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
            TabStop = view.TabStop == TabBehavior.NoStop ? TabBehavior.TabStop : view.TabStop
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
        var tries = 0;

        while (view.HasFocus)
        {
            if (++tries > 10)
            {
                Assert.Fail ($"{view} is not leaving.");
            }

            switch (view.TabStop)
            {
                case null:
                case TabBehavior.NoStop:
                case TabBehavior.TabStop:
                    if (Application.OnKeyDown (Key.Tab))
                    {
                        if (view.HasFocus)
                        {
                            // Try another nav key (e.g. for TextView that eats Tab)
                            Application.OnKeyDown (Key.CursorDown);
                        }
                    };
                    break;

                case TabBehavior.TabGroup:
                    Application.OnKeyDown (Key.F6);

                    break;
                default:
                    throw new ArgumentOutOfRangeException ();
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
    public void FocusNearestView_Ensure_Focus_Ordered ()
    {
        Application.Top = Application.Current = new Toplevel ();

        var win = new Window ();
        var winSubview = new View { CanFocus = true, Text = "WindowSubview" };
        win.Add (winSubview);
        Application.Current.Add (win);

        var frm = new FrameView ();
        var frmSubview = new View { CanFocus = true, Text = "FrameSubview" };
        frm.Add (frmSubview);
        Application.Current.Add (frm);
        Application.Current.SetFocus ();

        Assert.Equal (winSubview, Application.Current.GetMostFocused ());

        Application.OnKeyDown (Key.Tab); // Move to the next TabStop. There is none. So we should stay.
        Assert.Equal (winSubview, Application.Current.GetMostFocused ());

        Application.OnKeyDown (Key.F6);
        Assert.Equal (frmSubview, Application.Current.GetMostFocused ());

        Application.OnKeyDown (Key.Tab);
        Assert.Equal (frmSubview, Application.Current.GetMostFocused ());

        Application.OnKeyDown (Key.F6);
        Assert.Equal (winSubview, Application.Current.GetMostFocused ());

        Application.OnKeyDown (Key.F6.WithShift);
        Assert.Equal (frmSubview, Application.Current.GetMostFocused ());

        Application.OnKeyDown (Key.F6.WithShift);
        Assert.Equal (winSubview, Application.Current.GetMostFocused ());

        Application.Current.Dispose ();
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

}
