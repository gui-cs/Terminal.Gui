using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class EnabledTests () : TestsAllViews
{
    [Fact]
    public void Enabled_False_Leaves ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.SetFocus ();
        Assert.True (view.HasFocus);

        view.Enabled = false;
        Assert.False (view.HasFocus);
    }

    [Fact]
    public void Enabled_False_Leaves_Subview ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);

        view.Enabled = false;
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);
    }

    [Fact]
    public void Enabled_False_Leaves_Subview2 ()
    {
        var view = new Window
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);

        view.Enabled = false;
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);
    }

    [Fact]
    public void Enabled_False_On_Subview_Leaves_Just_Subview ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);

        subView.Enabled = false;
        Assert.True (view.HasFocus);
        Assert.False (subView.HasFocus);
    }

    [Fact]
    public void Enabled_False_Focuses_Deepest_Focusable_Subview ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        var subViewSubView1 = new View
        {
            Id = "subViewSubView1",
            CanFocus = false
        };

        var subViewSubView2 = new View
        {
            Id = "subViewSubView2",
            CanFocus = true
        };

        var subViewSubView3 = new View
        {
            Id = "subViewSubView3",
            CanFocus = true // This is the one that will be focused
        };
        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.Equal (subViewSubView2, subView.Focused);

        subViewSubView2.Enabled = false;
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.Equal (subViewSubView3, subView.Focused);
        Assert.True (subViewSubView3.HasFocus);
    }

    [Fact]
    public void Enabled_True_Subview_Focuses_SubView ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true,
            Enabled = false
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);

        view.Enabled = true;
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
    }

    [Fact]
    public void Enabled_True_On_Subview_Focuses ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true,
            Enabled = false
        };

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.False (subView.HasFocus);

        subView.Enabled = true;
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
    }

    [Fact]
    public void Enabled_True_Focuses_Deepest_Focusable_Subview ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View
        {
            Id = "subView",
            CanFocus = true,
            Enabled = false
        };

        var subViewSubView1 = new View
        {
            Id = "subViewSubView1",
            CanFocus = false
        };

        var subViewSubView2 = new View
        {
            Id = "subViewSubView2",
            CanFocus = true // This is the one that will be focused
        };

        var subViewSubView3 = new View
        {
            Id = "subViewSubView3",
            CanFocus = true
        };
        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.False (subView.HasFocus);
        Assert.False (subViewSubView2.HasFocus);

        subView.Enabled = true;
        Assert.True (subView.HasFocus);
        Assert.Equal (subView, view.Focused);
        Assert.Equal (subViewSubView2, subView.Focused);
        Assert.True (subViewSubView2.HasFocus);
    }

    [Fact]
    [AutoInitShutdown]
    public void _Enabled_Sets_Also_Sets_Subviews ()
    {
        var wasClicked = false;
        var button = new Button { Text = "Click Me" };
        button.IsDefault = true;
        button.Accepting += (s, e) => wasClicked = !wasClicked;
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

                                     Assert.True (button.HasFocus);
                                     win.Enabled = false;
                                     Assert.False (button.HasFocus);
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
                                     win.FocusDeepest (NavigationDirection.Forward, null);
                                     Assert.True (button.HasFocus);
                                     Assert.True (win.HasFocus);

                                     Application.RequestStop ();
                                 };

        Application.Run (top);

        Assert.Equal (1, iterations);
        top.Dispose ();
    }
}
