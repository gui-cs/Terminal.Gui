using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class EnterLeaveTests (ITestOutputHelper _output) : TestsAllViews
{
    [Fact]
    public void SetFocus_FiresEnter ()
    {
        int nEnter = 0;
        int nLeave = 0;
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };
        view.Enter += (s, e) => nEnter++;
        view.Leave += (s, e) => nLeave++;

        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (1, nEnter);
        Assert.Equal (0, nLeave);
    }

    [Fact]
    public void RemoveFocus_FiresLeave ()
    {
        int nEnter = 0;
        int nLeave = 0;
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };
        view.Enter += (s, e) => nEnter++;
        view.Leave += (s, e) => nLeave++;

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
    public void SetFocus_SubView_SetFocus_FiresEnter ()
    {
        int viewEnterCount = 0;
        int viewLeaveCount = 0;
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };
        view.Enter += (s, e) => viewEnterCount++;
        view.Leave += (s, e) => viewLeaveCount++;

        int subviewEnterCount = 0;
        int subviewLeaveCount = 0;
        var subview = new View ()
        {
            Id = "subview",
            CanFocus = true
        };
        subview.Enter += (s, e) => subviewEnterCount++;
        subview.Leave += (s, e) => subviewLeaveCount++;

        view.Add (subview);

        view.SetFocus ();

        Assert.Equal (1, viewEnterCount);
        Assert.Equal (0, viewLeaveCount);

        Assert.Equal (1, subviewEnterCount);
        Assert.Equal (0, subviewLeaveCount);
    }

    [Fact]
    public void RemoveFocus_SubView_FiresLeave ()
    {
        int viewEnterCount = 0;
        int viewLeaveCount = 0;
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };
        view.Enter += (s, e) => viewEnterCount++;
        view.Leave += (s, e) => viewLeaveCount++;

        int subviewEnterCount = 0;
        int subviewLeaveCount = 0;
        var subview = new View ()
        {
            Id = "subview",
            CanFocus = true
        };
        subview.Enter += (s, e) => subviewEnterCount++;
        subview.Leave += (s, e) => subviewLeaveCount++;

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
    public void SetFocus_CompoundSubView_FiresEnter ()
    {
        int viewEnterCount = 0;
        int viewLeaveCount = 0;
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };
        view.Enter += (s, e) => viewEnterCount++;
        view.Leave += (s, e) => viewLeaveCount++;

        int subViewEnterCount = 0;
        int subViewLeaveCount = 0;
        var subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };
        subView.Enter += (s, e) => subViewEnterCount++;
        subView.Leave += (s, e) => subViewLeaveCount++;

        int subviewSubView1EnterCount = 0;
        int subviewSubView1LeaveCount = 0;
        var subViewSubView1 = new View ()
        {
            Id = "subViewSubView1",
            CanFocus = false
        };
        subViewSubView1.Enter += (s, e) => subviewSubView1EnterCount++;
        subViewSubView1.Leave += (s, e) => subviewSubView1LeaveCount++;

        int subviewSubView2EnterCount = 0;
        int subviewSubView2LeaveCount = 0;
        var subViewSubView2 = new View ()
        {
            Id = "subViewSubView2",
            CanFocus = true
        };
        subViewSubView2.Enter += (s, e) => subviewSubView2EnterCount++;
        subViewSubView2.Leave += (s, e) => subviewSubView2LeaveCount++;

        int subviewSubView3EnterCount = 0;
        int subviewSubView3LeaveCount = 0;
        var subViewSubView3 = new View ()
        {
            Id = "subViewSubView3",
            CanFocus = false
        };
        subViewSubView3.Enter += (s, e) => subviewSubView3EnterCount++;
        subViewSubView3.Leave += (s, e) => subviewSubView3LeaveCount++;

        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True(view.HasFocus);
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
    public void RemoveFocus_CompoundSubView_FiresLeave ()
    {
        int viewEnterCount = 0;
        int viewLeaveCount = 0;
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };
        view.Enter += (s, e) => viewEnterCount++;
        view.Leave += (s, e) => viewLeaveCount++;

        int subViewEnterCount = 0;
        int subViewLeaveCount = 0;
        var subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };
        subView.Enter += (s, e) => subViewEnterCount++;
        subView.Leave += (s, e) => subViewLeaveCount++;

        int subviewSubView1EnterCount = 0;
        int subviewSubView1LeaveCount = 0;
        var subViewSubView1 = new View ()
        {
            Id = "subViewSubView1",
            CanFocus = false
        };
        subViewSubView1.Enter += (s, e) => subviewSubView1EnterCount++;
        subViewSubView1.Leave += (s, e) => subviewSubView1LeaveCount++;

        int subviewSubView2EnterCount = 0;
        int subviewSubView2LeaveCount = 0;
        var subViewSubView2 = new View ()
        {
            Id = "subViewSubView2",
            CanFocus = true
        };
        subViewSubView2.Enter += (s, e) => subviewSubView2EnterCount++;
        subViewSubView2.Leave += (s, e) => subviewSubView2LeaveCount++;

        int subviewSubView3EnterCount = 0;
        int subviewSubView3LeaveCount = 0;
        var subViewSubView3 = new View ()
        {
            Id = "subViewSubView3",
            CanFocus = false
        };
        subViewSubView3.Enter += (s, e) => subviewSubView3EnterCount++;
        subViewSubView3.Leave += (s, e) => subviewSubView3LeaveCount++;

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
    public void SetFocus_Peer_LeavesOther ()
    {
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };

        var subview1 = new View ()
        {
            Id = "subview1",
            CanFocus = true
        };

        var subview2 = new View ()
        {
            Id = "subview2",
            CanFocus = true
        };
        view.Add (subview1, subview2);

        view.SetFocus ();
        Assert.Equal (subview1, view.Focused);
        Assert.True (subview1.HasFocus);
        Assert.False (subview2.HasFocus);

        subview2.SetFocus ();
        Assert.Equal (subview2, view.Focused);
        Assert.True (subview2.HasFocus);
        Assert.False (subview1.HasFocus);
    }

    [Fact]
    public void SetFocus_Peer_LeavesOthers_Subviews ()
    {
        var top = new View
        {
            Id = "top",
            CanFocus = true
        };
        var view1 = new View
        {
            Id = "view1",
            CanFocus = true
        };

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true
        };

        view1.Add (subView1);

        var subView1SubView1 = new View
        {
            Id = "subView1subView1",
            CanFocus = true
        };

        subView1.Add (subView1SubView1);

        var view2 = new View
        {
            Id = "view2",
            CanFocus = true
        };

        top.Add (view1, view2);
        Assert.False (view1.HasFocus);
        Assert.False (view2.HasFocus);

        view1.SetFocus ();
        Assert.True (view1.HasFocus);
        Assert.True (subView1.HasFocus);
        Assert.True (subView1SubView1.HasFocus);
        Assert.Equal (subView1, view1.Focused);
        Assert.Equal (subView1SubView1, subView1.Focused);

        view2.SetFocus ();
        Assert.False (view1.HasFocus);
        Assert.True (view2.HasFocus);
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

}
