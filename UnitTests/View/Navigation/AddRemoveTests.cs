using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AddRemoveNavigationTests (ITestOutputHelper _output) : TestsAllViews
{
    [Fact]
    public void Add_First_Subview_Gets_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        top.SetFocus ();
        Assert.True (top.HasFocus);

        View subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };

        top.Add (subView);

        Assert.True (top.HasFocus);
        Assert.Equal (subView, top.Focused);
        Assert.True (subView.HasFocus);
    }

    [Fact]
    public void Add_Subsequent_Subview_Gets_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        top.SetFocus ();
        Assert.True (top.HasFocus);

        View subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };

        top.Add (subView);

        Assert.True (subView.HasFocus);

        View subView2 = new View ()
        {
            Id = "subView2",
            CanFocus = true
        };

        top.Add (subView2);

        Assert.True (subView2.HasFocus);


    }

    [Fact]
    public void Add_Nested_Subviews_Deepest_Gets_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        top.SetFocus ();
        Assert.True (top.HasFocus);

        View subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };

        View subSubView = new View ()
        {
            Id = "subSubView",
            CanFocus = true
        };

        subView.Add (subSubView);

        top.Add (subView);

        Assert.True (top.HasFocus);
        Assert.Equal (subView, top.Focused);
        Assert.True (subView.HasFocus);
        Assert.True (subSubView.HasFocus);
    }

    [Fact]
    public void Remove_Focused_Subview_Keeps_Focus_And_SubView_Looses_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        View subView = new View ()
        {
            Id = "subView",
            CanFocus = true
        };

        top.Add (subView);

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.Equal (subView, top.Focused);
        Assert.True (subView.HasFocus);

        top.Remove (subView);
        Assert.True (top.HasFocus);
        Assert.Equal (null, top.Focused);
        Assert.False (subView.HasFocus);
    }

    [Fact]
    public void Remove_Focused_Subview_Keeps_Focus_And_SubView_Looses_Focus_And_Next_Gets_Focus ()
    {
        View top = new View ()
        {
            Id = "top",
            CanFocus = true
        };

        View subView1 = new View ()
        {
            Id = "subView1",
            CanFocus = true
        };

        View subView2 = new View ()
        {
            Id = "subView2",
            CanFocus = true
        };
        top.Add (subView1, subView2);

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.Equal (subView1, top.Focused);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        top.Remove (subView1);
        Assert.True (top.HasFocus);
        Assert.True (subView2.HasFocus);
        Assert.Equal (subView2, top.Focused);
        Assert.False (subView1.HasFocus);
    }
}
