using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests;

public class NavigationTests
{
    private readonly ITestOutputHelper _output;
    public NavigationTests (ITestOutputHelper output) { _output = output; }

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
    public void CanFocus_Container_ToFalse_Turns_All_Subviews_ToFalse_Too ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();

        var w = new Window ();
        var f = new FrameView ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        f.Add (v1, v2);
        w.Add (f);
        t.Add (w);

        t.Ready += (s, e) =>
                   {
                       Assert.True (t.CanFocus);
                       Assert.True (w.CanFocus);
                       Assert.True (f.CanFocus);
                       Assert.True (v1.CanFocus);
                       Assert.True (v2.CanFocus);

                       w.CanFocus = false;
                       Assert.False (w.CanFocus);
                       Assert.False (f.CanFocus);
                       Assert.False (v1.CanFocus);
                       Assert.False (v2.CanFocus);
                   };

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    public void CanFocus_Container_Toggling_All_Subviews_To_Old_Value_When_Is_True ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();

        var w = new Window ();
        var f = new FrameView ();
        var v1 = new View ();
        var v2 = new View { CanFocus = true };
        f.Add (v1, v2);
        w.Add (f);
        t.Add (w);

        t.Ready += (s, e) =>
                   {
                       Assert.True (t.CanFocus);
                       Assert.True (w.CanFocus);
                       Assert.True (f.CanFocus);
                       Assert.False (v1.CanFocus);
                       Assert.True (v2.CanFocus);

                       w.CanFocus = false;
                       Assert.False (w.CanFocus);
                       Assert.False (f.CanFocus);
                       Assert.False (v1.CanFocus);
                       Assert.False (v2.CanFocus);

                       w.CanFocus = true;
                       Assert.True (w.CanFocus);
                       Assert.True (f.CanFocus);
                       Assert.False (v1.CanFocus);
                       Assert.True (v2.CanFocus);
                   };

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    [AutoInitShutdown]
    public void CanFocus_Faced_With_Container ()
    {
        var t = new Toplevel ();
        var w = new Window ();
        var f = new FrameView ();
        var v = new View { CanFocus = true };
        f.Add (v);
        w.Add (f);
        t.Add (w);

        Assert.True (t.CanFocus);
        Assert.True (w.CanFocus);
        Assert.True (f.CanFocus);
        Assert.True (v.CanFocus);

        f.CanFocus = false;
        Assert.False (f.CanFocus);
        Assert.True (v.CanFocus);

        v.CanFocus = false;
        Assert.False (f.CanFocus);
        Assert.False (v.CanFocus);

        v.CanFocus = true;
        Assert.False (f.CanFocus);
        Assert.True (v.CanFocus);
        t.Dispose ();
    }

    [Fact]
    public void CanFocus_Faced_With_Container_After_Run ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();

        var w = new Window ();
        var f = new FrameView ();
        var v = new View { CanFocus = true };
        f.Add (v);
        w.Add (f);
        t.Add (w);

        t.Ready += (s, e) =>
                   {
                       Assert.True (t.CanFocus);
                       Assert.True (w.CanFocus);
                       Assert.True (f.CanFocus);
                       Assert.True (v.CanFocus);

                       f.CanFocus = false;
                       Assert.False (f.CanFocus);
                       Assert.False (v.CanFocus);

                       v.CanFocus = false;
                       Assert.False (f.CanFocus);
                       Assert.False (v.CanFocus);

                       Assert.Throws<InvalidOperationException> (() => v.CanFocus = true);
                       Assert.False (f.CanFocus);
                       Assert.False (v.CanFocus);

                       f.CanFocus = true;
                       Assert.True (f.CanFocus);
                       Assert.True (v.CanFocus);
                   };

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    public void CanFocus_Faced_With_Container_Before_Run ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();

        var w = new Window ();
        var f = new FrameView ();
        var v = new View { CanFocus = true };
        f.Add (v);
        w.Add (f);
        t.Add (w);

        Assert.True (t.CanFocus);
        Assert.True (w.CanFocus);
        Assert.True (f.CanFocus);
        Assert.True (v.CanFocus);

        f.CanFocus = false;
        Assert.False (f.CanFocus);
        Assert.True (v.CanFocus);

        v.CanFocus = false;
        Assert.False (f.CanFocus);
        Assert.False (v.CanFocus);

        v.CanFocus = true;
        Assert.False (f.CanFocus);
        Assert.True (v.CanFocus);

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    public void CanFocus_Set_Changes_TabIndex_And_TabStop ()
    {
        var r = new View ();
        var v1 = new View { Text = "1" };
        var v2 = new View { Text = "2" };
        var v3 = new View { Text = "3" };

        r.Add (v1, v2, v3);

        v2.CanFocus = true;
        Assert.Equal (r.TabIndexes.IndexOf (v2), v2.TabIndex);
        Assert.Equal (0, v2.TabIndex);
        Assert.True (v2.TabStop);

        v1.CanFocus = true;
        Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
        Assert.Equal (1, v1.TabIndex);
        Assert.True (v1.TabStop);

        v1.TabIndex = 2;
        Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
        Assert.Equal (1, v1.TabIndex);
        v3.CanFocus = true;
        Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
        Assert.Equal (1, v1.TabIndex);
        Assert.Equal (r.TabIndexes.IndexOf (v3), v3.TabIndex);
        Assert.Equal (2, v3.TabIndex);
        Assert.True (v3.TabStop);

        v2.CanFocus = false;
        Assert.Equal (r.TabIndexes.IndexOf (v1), v1.TabIndex);
        Assert.Equal (1, v1.TabIndex);
        Assert.True (v1.TabStop);
        Assert.NotEqual (r.TabIndexes.IndexOf (v2), v2.TabIndex);
        Assert.Equal (-1, v2.TabIndex);
        Assert.False (v2.TabStop);
        Assert.Equal (r.TabIndexes.IndexOf (v3), v3.TabIndex);
        Assert.Equal (2, v3.TabIndex);
        Assert.True (v3.TabStop);
        r.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void CanFocus_Sets_To_False_Does_Not_Sets_HasFocus_To_True ()
    {
        var view = new View { CanFocus = true };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);

        Assert.True (view.CanFocus);
        Assert.True (view.HasFocus);

        view.CanFocus = false;
        Assert.False (view.CanFocus);
        Assert.False (view.HasFocus);
        Assert.Null (Application.Current.Focused);
        Assert.Null (Application.Current.MostFocused);
    }

    [Fact]
    [AutoInitShutdown]
    public void CanFocus_Sets_To_False_On_Single_View_Focus_View_On_Another_Toplevel ()
    {
        var view1 = new View { Id = "view1", Width = 10, Height = 1, CanFocus = true };
        var win1 = new Window { Id = "win1", Width = Dim.Percent (50), Height = Dim.Fill () };
        win1.Add (view1);
        var view2 = new View { Id = "view2", Width = 20, Height = 2, CanFocus = true };
        var win2 = new Window { Id = "win2", X = Pos.Right (win1), Width = Dim.Fill (), Height = Dim.Fill () };
        win2.Add (view2);
        var top = new Toplevel ();
        top.Add (win1, win2);
        Application.Begin (top);

        Assert.True (view1.CanFocus);
        Assert.True (view1.HasFocus);
        Assert.True (view2.CanFocus);
        Assert.False (view2.HasFocus); // Only one of the most focused toplevels view can have focus

        Assert.True (top.NewKeyDownEvent (Key.Tab));
        Assert.True (view1.CanFocus);
        Assert.False (view1.HasFocus); // Only one of the most focused toplevels view can have focus
        Assert.True (view2.CanFocus);
        Assert.True (view2.HasFocus);

        Assert.True (top.NewKeyDownEvent (Key.Tab));
        Assert.True (view1.CanFocus);
        Assert.True (view1.HasFocus);
        Assert.True (view2.CanFocus);
        Assert.False (view2.HasFocus); // Only one of the most focused toplevels view can have focus

        view1.CanFocus = false;
        Assert.False (view1.CanFocus);
        Assert.False (view1.HasFocus);
        Assert.True (view2.CanFocus);
        Assert.True (view2.HasFocus);
        Assert.Equal (win2, Application.Current.Focused);
        Assert.Equal (view2, Application.Current.MostFocused);
    }

    [Fact]
    [AutoInitShutdown]
    public void CanFocus_Sets_To_False_On_Toplevel_Focus_View_On_Another_Toplevel ()
    {
        var view1 = new View { Id = "view1", Width = 10, Height = 1, CanFocus = true };
        var win1 = new Window { Id = "win1", Width = Dim.Percent (50), Height = Dim.Fill () };
        win1.Add (view1);
        var view2 = new View { Id = "view2", Width = 20, Height = 2, CanFocus = true };
        var win2 = new Window { Id = "win2", X = Pos.Right (win1), Width = Dim.Fill (), Height = Dim.Fill () };
        win2.Add (view2);
        var top = new Toplevel ();
        top.Add (win1, win2);
        Application.Begin (top);

        Assert.True (view1.CanFocus);
        Assert.True (view1.HasFocus);
        Assert.True (view2.CanFocus);
        Assert.False (view2.HasFocus); // Only one of the most focused toplevels view can have focus

        Assert.True (top.NewKeyDownEvent (Key.Tab.WithCtrl));
        Assert.True (view1.CanFocus);
        Assert.False (view1.HasFocus); // Only one of the most focused toplevels view can have focus
        Assert.True (view2.CanFocus);
        Assert.True (view2.HasFocus);

        Assert.True (top.NewKeyDownEvent (Key.Tab.WithCtrl));
        Assert.True (view1.CanFocus);
        Assert.True (view1.HasFocus);
        Assert.True (view2.CanFocus);
        Assert.False (view2.HasFocus); // Only one of the most focused toplevels view can have focus

        win1.CanFocus = false;
        Assert.False (view1.CanFocus);
        Assert.False (view1.HasFocus);
        Assert.False (win1.CanFocus);
        Assert.False (win1.HasFocus);
        Assert.True (view2.CanFocus);
        Assert.True (view2.HasFocus);
        Assert.Equal (win2, Application.Current.Focused);
        Assert.Equal (view2, Application.Current.MostFocused);
    }

    [Fact]
    [AutoInitShutdown]
    public void CanFocus_Sets_To_False_With_Two_Views_Focus_Another_View_On_The_Same_Toplevel ()
    {
        var view1 = new View { Id = "view1", Width = 10, Height = 1, CanFocus = true };

        var view12 = new View
        {
            Id = "view12",
            Y = 5,
            Width = 10,
            Height = 1,
            CanFocus = true
        };
        var win1 = new Window { Id = "win1", Width = Dim.Percent (50), Height = Dim.Fill () };
        win1.Add (view1, view12);
        var view2 = new View { Id = "view2", Width = 20, Height = 2, CanFocus = true };
        var win2 = new Window { Id = "win2", X = Pos.Right (win1), Width = Dim.Fill (), Height = Dim.Fill () };
        win2.Add (view2);
        var top = new Toplevel ();
        top.Add (win1, win2);
        Application.Begin (top);

        Assert.True (view1.CanFocus);
        Assert.True (view1.HasFocus);
        Assert.True (view2.CanFocus);
        Assert.False (view2.HasFocus); // Only one of the most focused toplevels view can have focus

        Assert.True (top.NewKeyDownEvent (Key.Tab.WithCtrl));
        Assert.True (top.NewKeyDownEvent (Key.Tab.WithCtrl));
        Assert.True (view1.CanFocus);
        Assert.False (view1.HasFocus); // Only one of the most focused toplevels view can have focus
        Assert.True (view2.CanFocus);
        Assert.True (view2.HasFocus);

        Assert.True (top.NewKeyDownEvent (Key.Tab.WithCtrl));
        Assert.True (view1.CanFocus);
        Assert.True (view1.HasFocus);
        Assert.True (view2.CanFocus);
        Assert.False (view2.HasFocus); // Only one of the most focused toplevels view can have focus

        view1.CanFocus = false;
        Assert.False (view1.CanFocus);
        Assert.False (view1.HasFocus);
        Assert.True (view2.CanFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (win1, Application.Current.Focused);
        Assert.Equal (view12, Application.Current.MostFocused);
    }

    [Fact]
    [AutoInitShutdown]
    public void Enabled_False_Sets_HasFocus_To_False ()
    {
        var wasClicked = false;
        var view = new Button { Text = "Click Me" };
        view.Accept += (s, e) => wasClicked = !wasClicked;

        view.NewKeyDownEvent (Key.Space);
        Assert.True (wasClicked);
        view.OnMouseEvent (new MouseEvent { Flags = MouseFlags.Button1Clicked });
        Assert.False (wasClicked);
        Assert.True (view.Enabled);
        Assert.True (view.CanFocus);
        Assert.True (view.HasFocus);

        view.Enabled = false;
        view.NewKeyDownEvent (Key.Space);
        Assert.False (wasClicked);
        view.OnMouseEvent (new MouseEvent { Flags = MouseFlags.Button1Clicked });
        Assert.False (wasClicked);
        Assert.False (view.Enabled);
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);
        view.SetFocus ();
        Assert.False (view.HasFocus);
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
                                     button.OnMouseEvent (new MouseEvent { Flags = MouseFlags.Button1Clicked });
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
                                     button.OnMouseEvent (new MouseEvent { Flags = MouseFlags.Button1Clicked });
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
                                     win.FocusFirst ();
                                     Assert.True (button.HasFocus);
                                     Assert.True (win.HasFocus);

                                     Application.RequestStop ();
                                 };

        Application.Run (top);

        Assert.Equal (1, iterations);
    }

    [Fact]
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

        top.NewKeyDownEvent (Key.Tab);
        Assert.Equal ("WindowSubview", top.MostFocused.Text);
        top.NewKeyDownEvent (Key.Tab);
        Assert.Equal ("FrameSubview", top.MostFocused.Text);
        top.NewKeyDownEvent (Key.Tab);
        Assert.Equal ("WindowSubview", top.MostFocused.Text);

        top.NewKeyDownEvent (Key.Tab.WithShift);
        Assert.Equal ("FrameSubview", top.MostFocused.Text);
        top.NewKeyDownEvent (Key.Tab.WithShift);
        Assert.Equal ("WindowSubview", top.MostFocused.Text);
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
                               view3 = new View { Id = "view3", Y = 1, Width = 10, Height = 5 };
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

        Assert.True (top1.NewKeyDownEvent (Key.Tab.WithCtrl));
        Assert.True (top1.HasFocus);
        Assert.False (view1.HasFocus);
        Assert.True (view2.HasFocus);
        Assert.True (removed);
        Assert.NotNull (view3);

        Exception exception =
            Record.Exception (() => top1.NewKeyDownEvent (Key.Tab.WithCtrl));
        Assert.Null (exception);
        Assert.True (removed);
        Assert.Null (view3);
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_Will_Invoke_KeyPressed_Only_For_The_MostFocused_With_Top_KeyPress_Event ()
    {
        var sbQuiting = false;
        var tfQuiting = false;
        var topQuiting = false;

        var sb = new StatusBar (
                                new StatusItem []
                                {
                                    new (
                                         KeyCode.CtrlMask | KeyCode.Q,
                                         "~^Q~ Quit",
                                         () => sbQuiting = true
                                        )
                                }
                               );
        var tf = new TextField ();
        tf.KeyDown += Tf_KeyPressed;

        void Tf_KeyPressed (object sender, Key obj)
        {
            if (obj.KeyCode == (KeyCode.Q | KeyCode.CtrlMask))
            {
                obj.Handled = tfQuiting = true;
            }
        }

        var win = new Window ();
        win.Add (sb, tf);
        Toplevel top = new ();
        top.KeyDown += Top_KeyPress;

        void Top_KeyPress (object sender, Key obj)
        {
            if (obj.KeyCode == (KeyCode.Q | KeyCode.CtrlMask))
            {
                obj.Handled = topQuiting = true;
            }
        }

        top.Add (win);
        Application.Begin (top);

        Assert.False (sbQuiting);
        Assert.False (tfQuiting);
        Assert.False (topQuiting);

        Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
        Assert.False (sbQuiting);
        Assert.True (tfQuiting);
        Assert.False (topQuiting);

#if BROKE_WITH_2927
        tf.KeyPressed -= Tf_KeyPress;
        tfQuiting = false;
        Application.Driver.SendKeys ('q', ConsoleKey.Q, false, false, true);
        Application.MainLoop.RunIteration ();
        Assert.True (sbQuiting);
        Assert.False (tfQuiting);
        Assert.False (topQuiting);

        sb.RemoveItem (0);
        sbQuiting = false;
        Application.Driver.SendKeys ('q', ConsoleKey.Q, false, false, true);
        Application.MainLoop.RunIteration ();
        Assert.False (sbQuiting);
        Assert.False (tfQuiting);

// This test is now invalid because `win` is focused, so it will receive the keypress
        Assert.True (topQuiting);
#endif
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_Will_Invoke_KeyPressed_Only_For_The_MostFocused_Without_Top_KeyPress_Event ()
    {
        var sbQuiting = false;
        var tfQuiting = false;

        var sb = new StatusBar (
                                new StatusItem []
                                {
                                    new (
                                         KeyCode.CtrlMask | KeyCode.Q,
                                         "~^Q~ Quit",
                                         () => sbQuiting = true
                                        )
                                }
                               );
        var tf = new TextField ();
        tf.KeyDown += Tf_KeyPressed;

        void Tf_KeyPressed (object sender, Key obj)
        {
            if (obj.KeyCode == (KeyCode.Q | KeyCode.CtrlMask))
            {
                obj.Handled = tfQuiting = true;
            }
        }

        var win = new Window ();
        win.Add (sb, tf);
        Toplevel top = new ();
        top.Add (win);
        Application.Begin (top);

        Assert.False (sbQuiting);
        Assert.False (tfQuiting);

        Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
        Assert.False (sbQuiting);
        Assert.True (tfQuiting);

        tf.KeyDown -= Tf_KeyPressed;
        tfQuiting = false;
        Application.Driver.SendKeys ('Q', ConsoleKey.Q, false, false, true);
        Application.MainLoop.RunIteration ();
#if BROKE_WITH_2927
        Assert.True (sbQuiting);
        Assert.False (tfQuiting);
#endif
    }

    [Fact]
    public void Navigation_With_Null_Focused_View ()
    {
        // Non-regression test for #882 (NullReferenceException during keyboard navigation when Focused is null)

        Application.Init (new FakeDriver ());

        var top = new Toplevel ();
        top.Ready += (s, e) => { Assert.Null (top.Focused); };

        // Keyboard navigation with tab
        Console.MockKeyPresses.Push (new ConsoleKeyInfo ('\t', ConsoleKey.Tab, false, false, false));

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
    }

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
    }

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
        Assert.Equal (new Rectangle (0, 0, 80, 25), new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows));
        Assert.Equal (new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 80, 25), top.Frame);

        ((FakeDriver)Application.Driver).SetBufferSize (20, 10);
        Assert.Equal (new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 20, 10), top.Frame);

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
        Assert.Equal (Point.Empty, top.ScreenToFrame (0, 0));
        Rectangle screen = top.Margin.BoundsToScreen (new (0, 0, 0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = top.Border.BoundsToScreen (new (0, 0, 0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = top.Padding.BoundsToScreen (new (0, 0, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = top.BoundsToScreen (new (0, 0, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = top.BoundsToScreen (new (-1, -1, 0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        var found = View.FindDeepestView (top, 0, 0);
        Assert.Equal (top.Border, found);
 
        Assert.Equal (0, found.Frame.X);
        Assert.Equal (0, found.Frame.Y);
        Assert.Equal (new Point (3, 2), top.ScreenToFrame (3, 2));
        screen = top.BoundsToScreen (new (3, 2, 0, 0));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, screen.X, screen.Y);
        Assert.Equal (view, found);
        //Assert.Equal (0, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        found = View.FindDeepestView (top, 3, 2);
        Assert.Equal (top, found);
        //Assert.Equal (3, found.FrameToScreen ().X);
        //Assert.Equal (2, found.FrameToScreen ().Y);
        Assert.Equal (new Point (13, 2), top.ScreenToFrame (13, 2));
        screen = top.BoundsToScreen (new (12, 2, 0, 0));
        Assert.Equal (13, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, screen.X, screen.Y);
        Assert.Equal (view, found);
        //Assert.Equal (9, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        screen = top.BoundsToScreen (new (13, 2, 0, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, 13, 2);
        Assert.Equal (top, found);
        //Assert.Equal (13, found.FrameToScreen ().X);
        //Assert.Equal (2, found.FrameToScreen ().Y);
        Assert.Equal (new Point (14, 3), top.ScreenToFrame (14, 3));
        screen = top.BoundsToScreen (new (14, 3, 0, 0));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        found = View.FindDeepestView (top, 14, 3);
        Assert.Equal (top, found);
        //Assert.Equal (14, found.FrameToScreen ().X);
        //Assert.Equal (3, found.FrameToScreen ().Y);

        // view
        Assert.Equal (new Point (-4, -3), view.ScreenToFrame (0, 0));
        screen = view.Margin.BoundsToScreen (new (-3, -2, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Border.BoundsToScreen (new (-3, -2, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Padding.BoundsToScreen (new (-3, -2, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.BoundsToScreen (new (-3, -2, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.BoundsToScreen (new (-4, -3, 0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        found = View.FindDeepestView (top, 0, 0);
        Assert.Equal (top.Border, found);

        Assert.Equal (new Point (-1, -1), view.ScreenToFrame (3, 2));
        screen = view.BoundsToScreen (new (0, 0, 0, 0));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, 4, 3);
        Assert.Equal (view, found);
        
        Assert.Equal (new Point (9, -1), view.ScreenToFrame (13, 2));
        screen = view.BoundsToScreen (new (10, 0, 0, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (3, screen.Y);
        found = View.FindDeepestView (top, 14, 3);
        Assert.Equal (top, found);
        
        Assert.Equal (new Point (10, 0), view.ScreenToFrame (14, 3));
        screen = view.BoundsToScreen (new (11, 1, 0, 0));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        found = View.FindDeepestView (top, 15, 4);
        Assert.Equal (top, found);
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
        Assert.Equal (new Rectangle (0, 0, 80, 25), new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows));
        Assert.NotEqual (new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows), top.Frame);
        Assert.Equal (new Rectangle (3, 2, 20, 10), top.Frame);

        ((FakeDriver)Application.Driver).SetBufferSize (30, 20);
        Assert.Equal (new Rectangle (0, 0, 30, 20), new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows));
        Assert.NotEqual (new Rectangle (0, 0, View.Driver.Cols, View.Driver.Rows), top.Frame);
        Assert.Equal (new Rectangle (3, 2, 20, 10), top.Frame);

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
        Assert.Equal (new Rectangle (3, 2, 23, 10), frame);

        // top
        Assert.Equal (new Point (-3, -2), top.ScreenToFrame (0, 0));
        Rectangle screen = top.Margin.BoundsToScreen (new (-3, -2, 0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = top.Border.BoundsToScreen (new (-3, -2, 0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        screen = top.Padding.BoundsToScreen (new (-3, -2, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = top.BoundsToScreen (new (-3, -2, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = top.BoundsToScreen (new (-4, -3, 0, 0));
        Assert.Equal (0, screen.X);
        Assert.Equal (0, screen.Y);
        var found = View.FindDeepestView (top, -4, -3);
        Assert.Null (found);
        Assert.Equal (Point.Empty, top.ScreenToFrame (3, 2));
        screen = top.BoundsToScreen (new (0, 0, 0, 0));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        Assert.Equal (top.Border, View.FindDeepestView (top, 3, 2));
        //Assert.Equal (0, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        Assert.Equal (new Point (10, 0), top.ScreenToFrame (13, 2));
        screen = top.BoundsToScreen (new (10, 0, 0, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (3, screen.Y);
        Assert.Equal (top.Border, View.FindDeepestView (top, 13, 2));
        //Assert.Equal (10, found.FrameToScreen ().X);
        //Assert.Equal (0, found.FrameToScreen ().Y);
        Assert.Equal (new Point (11, 1), top.ScreenToFrame (14, 3));
        screen = top.BoundsToScreen (new (11, 1, 0, 0));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        Assert.Equal (top, View.FindDeepestView (top, 14, 3));
        
        // view
        Assert.Equal (new Point (-7, -5), view.ScreenToFrame (0, 0));
        screen = view.Margin.BoundsToScreen (new (-6, -4, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Border.BoundsToScreen (new (-6, -4, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.Padding.BoundsToScreen (new (-6, -4, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        screen = view.BoundsToScreen (new (-6, -4, 0, 0));
        Assert.Equal (1, screen.X);
        Assert.Equal (1, screen.Y);
        Assert.Null (View.FindDeepestView (top, 1, 1));
        Assert.Equal (new Point (-4, -3), view.ScreenToFrame (3, 2));
        screen = view.BoundsToScreen (new (-3, -2, 0, 0));
        Assert.Equal (4, screen.X);
        Assert.Equal (3, screen.Y);
        Assert.Equal (top, View.FindDeepestView (top, 4, 3));
        Assert.Equal (new Point (-1, -1), view.ScreenToFrame (6, 4));
        screen = view.BoundsToScreen (new (0, 0, 0, 0));
        Assert.Equal (7, screen.X);
        Assert.Equal (5, screen.Y);
        Assert.Equal (view, View.FindDeepestView (top, 7, 5));
        Assert.Equal (new Point (6, -1), view.ScreenToFrame (13, 4));
        screen = view.BoundsToScreen (new (7, 0, 0, 0));
        Assert.Equal (14, screen.X);
        Assert.Equal (5, screen.Y);
        Assert.Equal (view, View.FindDeepestView (top, 14, 5));
        Assert.Equal (new Point (7, -2), view.ScreenToFrame (14, 3));
        screen = view.BoundsToScreen (new (8, -1, 0, 0));
        Assert.Equal (15, screen.X);
        Assert.Equal (4, screen.Y);
        Assert.Equal (top, View.FindDeepestView (top, 15, 4));
        Assert.Equal (new Point (16, -2), view.ScreenToFrame (23, 3));
        screen = view.BoundsToScreen (new (17, -1, 0, 0));
        Assert.Equal (24, screen.X);
        Assert.Equal (4, screen.Y);
        Assert.Null (View.FindDeepestView (top, 24, 4));
    }

    [Fact]
    public void SendSubviewBackwards_Subviews_vs_TabIndexes ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        r.SendSubviewBackwards (v3);
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.Subviews.IndexOf (v2) == 2);
        Assert.True (r.Subviews.IndexOf (v3) == 1);

        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v2) == 1);
        Assert.True (r.TabIndexes.IndexOf (v3) == 2);
        r.Dispose ();
    }

    [Fact]
    public void SendSubviewToBack_Subviews_vs_TabIndexes ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        r.SendSubviewToBack (v3);
        Assert.True (r.Subviews.IndexOf (v1) == 1);
        Assert.True (r.Subviews.IndexOf (v2) == 2);
        Assert.True (r.Subviews.IndexOf (v3) == 0);

        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v2) == 1);
        Assert.True (r.TabIndexes.IndexOf (v3) == 2);
        r.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void SetFocus_View_With_Null_Superview_Does_Not_Throw_Exception ()
    {
        var top = new Toplevel ();
        Assert.True (top.CanFocus);
        Assert.False (top.HasFocus);

        Exception exception = Record.Exception (top.SetFocus);
        Assert.Null (exception);
        Assert.True (top.CanFocus);
        Assert.True (top.HasFocus);
    }

    [Fact]
    [AutoInitShutdown]
    public void SetHasFocus_Do_Not_Throws_If_OnLeave_Remove_Focused_Changing_To_Null ()
    {
        var view1Leave = false;
        var subView1Leave = false;
        var subView1subView1Leave = false;
        Toplevel top = new ();
        var view1 = new View { CanFocus = true };
        var subView1 = new View { CanFocus = true };
        var subView1subView1 = new View { CanFocus = true };
        view1.Leave += (s, e) => { view1Leave = true; };

        subView1.Leave += (s, e) =>
                          {
                              subView1.Remove (subView1subView1);
                              subView1Leave = true;
                          };
        view1.Add (subView1);

        subView1subView1.Leave += (s, e) =>
                                  {
                                      // This is never invoked
                                      subView1subView1Leave = true;
                                  };
        subView1.Add (subView1subView1);
        var view2 = new View { CanFocus = true };
        top.Add (view1, view2);
        RunState rs = Application.Begin (top);

        view2.SetFocus ();
        Assert.True (view1Leave);
        Assert.True (subView1Leave);
        Assert.False (subView1subView1Leave);
        Application.End (rs);
        subView1subView1.Dispose ();
    }

    [Fact]
    public void Subviews_TabIndexes_AreEqual ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.Subviews.IndexOf (v2) == 1);
        Assert.True (r.Subviews.IndexOf (v3) == 2);

        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v2) == 1);
        Assert.True (r.TabIndexes.IndexOf (v3) == 2);

        Assert.Equal (r.Subviews.IndexOf (v1), r.TabIndexes.IndexOf (v1));
        Assert.Equal (r.Subviews.IndexOf (v2), r.TabIndexes.IndexOf (v2));
        Assert.Equal (r.Subviews.IndexOf (v3), r.TabIndexes.IndexOf (v3));
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Set_CanFocus_False ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        v1.CanFocus = false;
        v1.TabIndex = 0;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        Assert.Equal (-1, v1.TabIndex);
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Set_CanFocus_False_To_True ()
    {
        var r = new View ();
        var v1 = new View ();
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        v1.CanFocus = true;
        v1.TabIndex = 1;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 1);
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Set_CanFocus_HigherValues ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        v1.TabIndex = 3;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 2);
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Set_CanFocus_LowerValues ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        v1.TabIndex = -1;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 0);
        r.Dispose ();
    }

    [Fact]
    public void TabIndex_Set_CanFocus_ValidValues ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        v1.TabIndex = 1;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 1);

        v1.TabIndex = 2;
        Assert.True (r.Subviews.IndexOf (v1) == 0);
        Assert.True (r.TabIndexes.IndexOf (v1) == 2);
        r.Dispose ();
    }

    [Fact]
    public void TabStop_All_False_And_All_True_And_Changing_TabStop_Later ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true, TabStop = false };
        var v2 = new View { CanFocus = true, TabStop = false };
        var v3 = new View { CanFocus = true, TabStop = false };

        r.Add (v1, v2, v3);

        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v1.TabStop = true;
        r.FocusNext ();
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        v2.TabStop = true;
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);
        v3.TabStop = true;
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.True (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void TabStop_All_True_And_Changing_CanFocus_Later ()
    {
        var r = new View ();
        var v1 = new View ();
        var v2 = new View ();
        var v3 = new View ();

        r.Add (v1, v2, v3);

        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v1.CanFocus = true;
        r.FocusNext ();
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        v2.CanFocus = true;
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);
        v3.CanFocus = true;
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.True (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void TabStop_And_CanFocus_Are_All_True ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true };
        var v2 = new View { CanFocus = true };
        var v3 = new View { CanFocus = true };

        r.Add (v1, v2, v3);

        r.FocusNext ();
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.True (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void TabStop_And_CanFocus_Mixed_And_BothFalse ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true, TabStop = false };
        var v2 = new View { CanFocus = false, TabStop = true };
        var v3 = new View { CanFocus = false, TabStop = false };

        r.Add (v1, v2, v3);

        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void TabStop_Are_All_False_And_CanFocus_Are_All_True ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true, TabStop = false };
        var v2 = new View { CanFocus = true, TabStop = false };
        var v3 = new View { CanFocus = true, TabStop = false };

        r.Add (v1, v2, v3);

        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void TabStop_Are_All_True_And_CanFocus_Are_All_False ()
    {
        var r = new View ();
        var v1 = new View ();
        var v2 = new View ();
        var v3 = new View ();

        r.Add (v1, v2, v3);

        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.FocusNext ();
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void WindowDispose_CanFocusProblem ()
    {
        // Arrange
        Application.Init ();
        using var top = new Toplevel ();
        using var view = new View { X = 0, Y = 1, Text = nameof (WindowDispose_CanFocusProblem) };
        using var window = new Window ();
        top.Add (window);
        window.Add (view);

        // Act
        RunState rs = Application.Begin (top);
        Application.End (rs);
        top.Dispose ();
        Application.Shutdown ();

        // Assert does Not throw NullReferenceException
        top.SetFocus ();
    }
}
