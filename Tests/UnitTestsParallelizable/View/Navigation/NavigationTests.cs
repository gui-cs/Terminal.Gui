using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class NavigationTests
{
 
    // View.Focused & View.MostFocused tests

    // View.Focused - No subviews
    [Fact]
    public void Focused_NoSubViews ()
    {
        var view = new View ();
        Assert.Null (view.Focused);

        view.CanFocus = true;
        view.SetFocus ();
    }

    [Fact]
    public void GetMostFocused_NoSubViews_Returns_Null ()
    {
        var view = new View ();
        Assert.Null (view.Focused);

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Null (view.MostFocused);
    }

    [Fact]
    public void GetMostFocused_Returns_Most ()
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

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subview.HasFocus);
        Assert.Equal (subview, view.MostFocused);

        var subview2 = new View
        {
            Id = "subview2",
            CanFocus = true
        };

        view.Add (subview2);
        Assert.Equal (subview2, view.MostFocused);
    }
}
