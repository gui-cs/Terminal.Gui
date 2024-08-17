using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class HasFocusTests (ITestOutputHelper _output) : TestsAllViews
{

    [Fact]
    public void HasFocus_False_Leaves ()
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
    public void HasFocus_False_WithSuperView_Leaves_All ()
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
        Assert.False (view.HasFocus);
        Assert.False (subview.HasFocus);
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
        Assert.Equal (subview, view.GetFocused ());

        view.HasFocus = false;
        Assert.Null (view.GetFocused ());
        Assert.False (view.HasFocus);
        Assert.False (subview.HasFocus);
    }


    [Fact]
    public void HasFocus_False_Leave_Invoked ()
    {
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        int leaveInvoked = 0;

        view.Leave += (s, e) => leaveInvoked++;

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (0, leaveInvoked);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.Equal (1, leaveInvoked);
    }

    [Fact]
    public void HasFocus_False_Leave_Invoked_ForAllSubViews ()
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

        int leaveInvoked = 0;

        view.Leave += (s, e) => leaveInvoked++;
        subview.Leave += (s, e) => leaveInvoked++;

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (0, leaveInvoked);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.False (subview.HasFocus);
        Assert.Equal (2, leaveInvoked);
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

}
