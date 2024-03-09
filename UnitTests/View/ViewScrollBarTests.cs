public class ViewScrollBarTests
{
    [Fact]
    public void EnableScrollBars_Defaults ()
    {
        var view = new View ();
        Assert.False (view.EnableScrollBars);
        Assert.Empty (view.Subviews);
        Assert.False (view.AutoHideScrollBars);
        Assert.False (view.KeepContentAlwaysInContentArea);
        Assert.False (view.ShowVerticalScrollBar);
        Assert.False (view.ShowHorizontalScrollBar);

        view.EnableScrollBars = true;
        Assert.Equal (3, view.Subviews.Count);
        Assert.True (view.AutoHideScrollBars);
        Assert.True (view.KeepContentAlwaysInContentArea);
        Assert.True (view.ShowVerticalScrollBar);
        Assert.True (view.ShowHorizontalScrollBar);

        view.EnableScrollBars = false;
        Assert.Empty (view.Subviews);
        Assert.False (view.AutoHideScrollBars);
        Assert.False (view.KeepContentAlwaysInContentArea);
        Assert.False (view.ShowVerticalScrollBar);
        Assert.False (view.ShowHorizontalScrollBar);
    }
}
