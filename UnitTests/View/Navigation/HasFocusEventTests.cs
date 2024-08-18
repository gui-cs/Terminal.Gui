using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class HasFocusEventTests (ITestOutputHelper _output) : TestsAllViews
{
    [Fact]
    public void SetFocus_Raises_HasFocusChanging ()
    {
        var nEnter = 0;
        var nLeave = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };
        view.HasFocusChanging += (s, e) => nEnter++;
        view.HasFocusChanged += (s, e) => nLeave++;

        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (1, nEnter);
        Assert.Equal (0, nLeave);
    }

    [Fact]
    public void RemoveFocus_Raises_HasFocusChanged ()
    {
        var nEnter = 0;
        var nLeave = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };
        view.HasFocusChanging += (s, e) => nEnter++;
        view.HasFocusChanged += (s, e) => nLeave++;

        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (1, nEnter);
        Assert.Equal (0, nLeave);

        view.HasFocus = false;
        Assert.Equal (1, nEnter);
        Assert.Equal (1, nLeave);
    }

    [Fact]
    public void SetFocus_SubView_SetFocus_Raises_HasFocusChanging ()
    {
        var viewEnterCount = 0;
        var viewLeaveCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };
        view.HasFocusChanging += (s, e) => viewEnterCount++;
        view.HasFocusChanged += (s, e) => viewLeaveCount++;

        var subviewEnterCount = 0;
        var subviewLeaveCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };
        subview.HasFocusChanging += (s, e) => subviewEnterCount++;
        subview.HasFocusChanged += (s, e) => subviewLeaveCount++;

        view.Add (subview);

        view.SetFocus ();

        Assert.Equal (1, viewEnterCount);
        Assert.Equal (0, viewLeaveCount);

        Assert.Equal (1, subviewEnterCount);
        Assert.Equal (0, subviewLeaveCount);
    }

    [Fact]
    public void RemoveFocus_SubView_Raises_HasFocusChanged ()
    {
        var viewEnterCount = 0;
        var viewLeaveCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };
        view.HasFocusChanging += (s, e) => viewEnterCount++;
        view.HasFocusChanged += (s, e) => viewLeaveCount++;

        var subviewEnterCount = 0;
        var subviewLeaveCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };
        subview.HasFocusChanging += (s, e) => subviewEnterCount++;
        subview.HasFocusChanged += (s, e) => subviewLeaveCount++;

        view.Add (subview);

        view.SetFocus ();

        view.HasFocus = false;

        Assert.Equal (1, viewEnterCount);
        Assert.Equal (1, viewLeaveCount);

        Assert.Equal (1, subviewEnterCount);
        Assert.Equal (1, subviewLeaveCount);

        view.SetFocus ();

        Assert.Equal (2, viewEnterCount);
        Assert.Equal (1, viewLeaveCount);

        Assert.Equal (2, subviewEnterCount);
        Assert.Equal (1, subviewLeaveCount);

        subview.HasFocus = false;

        Assert.Equal (2, viewEnterCount);
        Assert.Equal (2, viewLeaveCount);

        Assert.Equal (2, subviewEnterCount);
        Assert.Equal (2, subviewLeaveCount);
    }

    [Fact]
    public void SetFocus_CompoundSubView_Raises_HasFocusChanging ()
    {
        var viewEnterCount = 0;
        var viewLeaveCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };
        view.HasFocusChanging += (s, e) => viewEnterCount++;
        view.HasFocusChanged += (s, e) => viewLeaveCount++;

        var subViewEnterCount = 0;
        var subViewLeaveCount = 0;

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };
        subView.HasFocusChanging += (s, e) => subViewEnterCount++;
        subView.HasFocusChanged += (s, e) => subViewLeaveCount++;

        var subviewSubView1EnterCount = 0;
        var subviewSubView1LeaveCount = 0;

        var subViewSubView1 = new View
        {
            Id = "subViewSubView1",
            CanFocus = false
        };
        subViewSubView1.HasFocusChanging += (s, e) => subviewSubView1EnterCount++;
        subViewSubView1.HasFocusChanged += (s, e) => subviewSubView1LeaveCount++;

        var subviewSubView2EnterCount = 0;
        var subviewSubView2LeaveCount = 0;

        var subViewSubView2 = new View
        {
            Id = "subViewSubView2",
            CanFocus = true
        };
        subViewSubView2.HasFocusChanging += (s, e) => subviewSubView2EnterCount++;
        subViewSubView2.HasFocusChanged += (s, e) => subviewSubView2LeaveCount++;

        var subviewSubView3EnterCount = 0;
        var subviewSubView3LeaveCount = 0;

        var subViewSubView3 = new View
        {
            Id = "subViewSubView3",
            CanFocus = false
        };
        subViewSubView3.HasFocusChanging += (s, e) => subviewSubView3EnterCount++;
        subViewSubView3.HasFocusChanged += (s, e) => subviewSubView3LeaveCount++;

        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.True (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        Assert.Equal (1, viewEnterCount);
        Assert.Equal (0, viewLeaveCount);

        Assert.Equal (1, subViewEnterCount);
        Assert.Equal (0, subViewLeaveCount);

        Assert.Equal (0, subviewSubView1EnterCount);
        Assert.Equal (0, subviewSubView1LeaveCount);

        Assert.Equal (1, subviewSubView2EnterCount);
        Assert.Equal (0, subviewSubView2LeaveCount);

        Assert.Equal (0, subviewSubView3EnterCount);
        Assert.Equal (0, subviewSubView3LeaveCount);
    }

    [Fact]
    public void RemoveFocus_CompoundSubView_Raises_HasFocusChanged ()
    {
        var viewEnterCount = 0;
        var viewLeaveCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };
        view.HasFocusChanging += (s, e) => viewEnterCount++;
        view.HasFocusChanged += (s, e) => viewLeaveCount++;

        var subViewEnterCount = 0;
        var subViewLeaveCount = 0;

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };
        subView.HasFocusChanging += (s, e) => subViewEnterCount++;
        subView.HasFocusChanged += (s, e) => subViewLeaveCount++;

        var subviewSubView1EnterCount = 0;
        var subviewSubView1LeaveCount = 0;

        var subViewSubView1 = new View
        {
            Id = "subViewSubView1",
            CanFocus = false
        };
        subViewSubView1.HasFocusChanging += (s, e) => subviewSubView1EnterCount++;
        subViewSubView1.HasFocusChanged += (s, e) => subviewSubView1LeaveCount++;

        var subviewSubView2EnterCount = 0;
        var subviewSubView2LeaveCount = 0;

        var subViewSubView2 = new View
        {
            Id = "subViewSubView2",
            CanFocus = true
        };
        subViewSubView2.HasFocusChanging += (s, e) => subviewSubView2EnterCount++;
        subViewSubView2.HasFocusChanged += (s, e) => subviewSubView2LeaveCount++;

        var subviewSubView3EnterCount = 0;
        var subviewSubView3LeaveCount = 0;

        var subViewSubView3 = new View
        {
            Id = "subViewSubView3",
            CanFocus = false
        };
        subViewSubView3.HasFocusChanging += (s, e) => subviewSubView3EnterCount++;
        subViewSubView3.HasFocusChanged += (s, e) => subviewSubView3LeaveCount++;

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

        Assert.Equal (1, viewEnterCount);
        Assert.Equal (1, viewLeaveCount);

        Assert.Equal (1, subViewEnterCount);
        Assert.Equal (1, subViewLeaveCount);

        Assert.Equal (0, subviewSubView1EnterCount);
        Assert.Equal (0, subviewSubView1LeaveCount);

        Assert.Equal (1, subviewSubView2EnterCount);
        Assert.Equal (1, subviewSubView2LeaveCount);

        Assert.Equal (0, subviewSubView3EnterCount);
        Assert.Equal (0, subviewSubView3LeaveCount);
    }

    [Fact]
    public void HasFocus_False_Leave_Raised ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };
        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        var leaveInvoked = 0;

        view.HasFocusChanged += (s, e) => leaveInvoked++;

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (0, leaveInvoked);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.Equal (1, leaveInvoked);
    }

    [Fact]
    public void HasFocus_False_Leave_Raised_ForAllSubViews ()
    {
        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };
        view.Add (subview);

        var leaveInvoked = 0;

        view.HasFocusChanged += (s, e) => leaveInvoked++;
        subview.HasFocusChanged += (s, e) => leaveInvoked++;

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (0, leaveInvoked);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.False (subview.HasFocus);
        Assert.Equal (2, leaveInvoked);
    }
}
