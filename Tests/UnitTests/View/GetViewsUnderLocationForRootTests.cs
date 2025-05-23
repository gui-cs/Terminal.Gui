#nullable enable

namespace Terminal.Gui.ViewMouseTests;

[Trait ("Category", "Input")]
public class GetViewsUnderLocationForRootTests
{
    [Fact]
    public void ReturnsRoot_WhenPointInsideRoot_NoSubviews ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (5, 5), false);
        Assert.Contains (top, result);
    }

    [Fact]
    public void ReturnsEmpty_WhenPointOutsideRoot ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (20, 20), false);
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsSubview_WhenPointInsideSubview ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };

        View sub = new ()
        {
            X = 2, Y = 2, Width = 5, Height = 5
        };
        top.Add (sub);
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (3, 3), false);
        Assert.Contains (top, result);
        Assert.Contains (sub, result);
        Assert.Equal (sub, result.Last ());
    }

    [Fact]
    public void ReturnsAdornment_WhenPointInMargin ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        top.Margin.Thickness = new (1);
        top.Margin.ViewportSettings = ViewportSettings.None;
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (0, 0), false);
        Assert.Contains(top, result);
        Assert.Contains (top.Margin, result);
    }

    [Fact]
    public void ReturnsAdornment_WhenPointIn_TransparentToMouse_Margin ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        top.Margin.Thickness = new (1);
        top.Margin.ViewportSettings = ViewportSettings.TransparentMouse;
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (0, 0), false);
        Assert.Contains (top, result);
        Assert.DoesNotContain (top.Margin, result);
    }

    [Fact]
    public void ReturnsAdornment_WhenPointInBorder ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        top.Border.Thickness = new (1);
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (0, 0), false);
        Assert.Contains (top, result);
        Assert.Contains (top.Border, result);
    }

    [Fact]
    public void ReturnsAdornment_WhenPointInPadding ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        top.Border.Thickness = new (1);
        top.Padding.Thickness = new (1);
        top.Layout ();
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (1, 1), false);
        Assert.Contains (top, result);
        Assert.Contains (top.Padding, result);
    }


    [Fact]
    public void HonorsTransparentMouseSetting ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10),
            ViewportSettings = ViewportSettings.TransparentMouse
        };
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (5, 5), false);
        Assert.Empty (result);
    }

    [Fact]
    public void ReturnsDeepestSubview_WhenNested ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };

        View sub1 = new ()
        {
            X = 1, Y = 1, Width = 8, Height = 8
        };

        View sub2 = new ()
        {
            X = 1, Y = 1, Width = 6, Height = 6
        };
        sub1.Add (sub2);
        top.Add (sub1);
        List<View?> result = View.GetViewsUnderLocationForRoot (top, new (3, 3), false);
        Assert.Contains (sub2, result);
        Assert.Equal (sub2, result.Last ());
    }
}
