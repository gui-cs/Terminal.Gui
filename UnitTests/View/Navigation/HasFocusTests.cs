using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class HasFocusTests (ITestOutputHelper _output) : TestsAllViews
{

    [Fact]
    public void HasFocus_False ()
    {
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };

        view.SetFocus ();
        Assert.True (view.HasFocus);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
    }

    [Fact]
    public void HasFocus_False_WithSuperView_Does_Not_Change_SuperView ()
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

        subview.HasFocus = false;
        Assert.False (subview.HasFocus);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void HasFocus_False_WithSubview_Leaves_All ()
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
        Assert.Equal (subview, view.Focused);

        view.HasFocus = false;
        Assert.Null (view.Focused);
        Assert.False (view.HasFocus);
        Assert.False (subview.HasFocus);
    }



    [Fact]
    public void Enabled_False_Sets_HasFocus_To_False ()
    {
        var wasClicked = false;
        var view = new Button { Text = "Click Me" };
        view.Accept += (s, e) => wasClicked = !wasClicked;

        view.NewKeyDownEvent (Key.Space);
        Assert.True (wasClicked);
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked });
        Assert.False (wasClicked);
        Assert.True (view.Enabled);
        Assert.True (view.CanFocus);
        Assert.True (view.HasFocus);

        view.Enabled = false;
        view.NewKeyDownEvent (Key.Space);
        Assert.False (wasClicked);
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked });
        Assert.False (wasClicked);
        Assert.False (view.Enabled);
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);
        view.SetFocus ();
        Assert.False (view.HasFocus);
    }



    [Fact]
    public void HasFocus_False_CompoundSubView_Leaves_All ()
    {
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };

        var subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };

        var subViewSubView1 = new View ()
        {
            Id = "subViewSubView1",
            CanFocus = false
        };

        var subViewSubView2 = new View ()
        {
            Id = "subViewSubView2",
            CanFocus = true
        };

        var subViewSubView3 = new View ()
        {
            Id = "subViewSubView3",
            CanFocus = false
        };

        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.True (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);
    }
}
