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
        List<View?> result = View.GetViewsUnderLocation (top, new (5, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
    }

    [Fact]
    public void ReturnsEmpty_WhenPointOutsideRoot ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        List<View?> result = View.GetViewsUnderLocation (top, new (20, 20), ViewportSettingsFlags.TransparentMouse);
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
        List<View?> result = View.GetViewsUnderLocation (top, new (3, 3), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (sub, result);
        Assert.Equal (sub, result.Last ());
    }

    [Fact]
    public void ReturnsTop_WhenPointInsideSubview_With_TransparentMouse ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };

        View sub = new ()
        {
            X = 2, Y = 2, Width = 5, Height = 5,
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };
        top.Add (sub);
        List<View?> result = View.GetViewsUnderLocation (top, new (3, 3), ViewportSettingsFlags.TransparentMouse);
        Assert.Single (result);
        Assert.Contains (top, result);

        result = View.GetViewsUnderLocation (top, new (3, 3), ViewportSettingsFlags.None);
        Assert.Equal (2, result.Count);
        Assert.Contains (top, result);
        Assert.Contains (sub, result);
    }

    [Fact]
    public void ReturnsAdornment_WhenPointInMargin ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        top.Margin!.Thickness = new (1);
        top.Margin!.ViewportSettings = ViewportSettingsFlags.None;
        List<View?> result = View.GetViewsUnderLocation (top, new (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (top.Margin, result);
    }

    [Fact]
    public void Returns_WhenPointIn_TransparentToMouseMargin_None ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        top.Margin!.Thickness = new (1);
        top.Margin!.ViewportSettings = ViewportSettingsFlags.TransparentMouse;
        List<View?> result = View.GetViewsUnderLocation (top, new (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.DoesNotContain (top, result);
        Assert.DoesNotContain (top.Margin, result);
    }

    [Fact]
    public void Returns_WhenPointIn_NotTransparentToMouseMargin_Top_And_Margin ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        top.Margin!.Thickness = new (1);
        top.Margin!.ViewportSettings = ViewportSettingsFlags.None;
        List<View?> result = View.GetViewsUnderLocation (top, new (0, 0), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (top.Margin, result);
    }

    [Fact]
    public void ReturnsAdornment_WhenPointInBorder ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };
        top.Border!.Thickness = new (1);
        List<View?> result = View.GetViewsUnderLocation (top, new (0, 0), ViewportSettingsFlags.TransparentMouse);
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
        top.Border!.Thickness = new (1);
        top.Padding!.Thickness = new (1);
        top.Layout ();
        List<View?> result = View.GetViewsUnderLocation (top, new (1, 1), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (top.Padding, result);
    }

    [Fact]
    public void HonorsIgnoreTransparentMouseParam ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10),
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };
        List<View?> result = View.GetViewsUnderLocation (top, new (5, 5), ViewportSettingsFlags.TransparentMouse);
        Assert.Empty (result);

        result = View.GetViewsUnderLocation (top, new (5, 5), ViewportSettingsFlags.None);
        Assert.NotEmpty (result);
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
        List<View?> result = View.GetViewsUnderLocation (top, new (3, 3), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (sub2, result);
        Assert.Equal (sub2, result.Last ());
    }

    [Fact]
    public void Returns_Subview_WhenPointIn_TransparentToMouseMargin_Top ()
    {
        Toplevel top = new ()
        {
            Frame = new (0, 0, 20, 20)
        };

        View subView = new ()
        {
            Frame = new (0, 0, 5, 5)
        };
        subView.Margin!.Thickness = new (1);
        subView.Margin!.ViewportSettings = ViewportSettingsFlags.None;
        top.Add (subView);

        Assert.True (subView.Contains (new Point (4, 4)));
        List<View?> result = View.GetViewsUnderLocation (top, new (4, 4), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.Contains (subView.Margin, result);
        Assert.Contains (subView, result);

        subView.Margin!.ViewportSettings = ViewportSettingsFlags.TransparentMouse;

        result = View.GetViewsUnderLocation (top, new (4, 4), ViewportSettingsFlags.TransparentMouse);
        Assert.Contains (top, result);
        Assert.DoesNotContain (subView.Margin, result);
        Assert.DoesNotContain (subView, result);
    }

    [Theory]
    [InlineData ("Margin")]
    [InlineData ("Border")]
    [InlineData ("Padding")]
    public void Returns_Subview_Of_Adornment (string adornmentType)
    {
        // Arrange: top -> subView -> subView.[Adornment] -> adornmentSubView
        Toplevel top = new ()
        {
            Frame = new (0, 0, 10, 10)
        };

        View subView = new ()
        {
            X = 0, Y = 0, Width = 8, Height = 8
        };
        top.Add (subView);

        View? adornment = null;
        switch (adornmentType)
        {
            case "Margin":
                subView.Margin!.Thickness = new (2);
                adornment = subView.Margin;
                break;
            case "Border":
                subView.Border!.Thickness = new (2);
                adornment = subView.Border;
                break;
            case "Padding":
                subView.Padding!.Thickness = new (2);
                adornment = subView.Padding;
                break;
        }

        subView.Layout ();

        // Add a child to the adornment
        View adornmentSubView = new ()
        {
            X = 0, Y = 0, Width = 2, Height = 2
        };
        adornment!.Add (adornmentSubView);

        // Set adornment ViewportSettings to None so it doesn't interfere with the test
        adornment.ViewportSettings = ViewportSettingsFlags.None;

        // Act: Point inside adornmentSubView (which is inside the adornment)
        var result = View.GetViewsUnderLocation (top, new (0, 0), ViewportSettingsFlags.TransparentMouse);

        // Assert: Should contain top, subView, adornment, and adornmentSubView
        Assert.Contains (top, result);
        Assert.Contains (subView, result);
        Assert.Contains (adornment, result);
        Assert.Contains (adornmentSubView, result);
        Assert.Equal (top, result [0]);
        Assert.Equal (adornmentSubView, result [^1]);
    }



    [Theory]
    [InlineData ("Margin")]
    [InlineData ("Border")]
    [InlineData ("Padding")]
    public void Returns_OnlyParentsSuperView_Of_Adornment_If_TransparentMouse (string adornmentType)
    {
        // Arrange: top -> subView -> subView.[Adornment] -> adornmentSubView
        Toplevel top = new ()
        {
            Id = "top",
            Frame = new (0, 0, 10, 10)
        };

        View subView = new ()
        {
            Id = "subView",
            X = 0, Y = 0, Width = 8, Height = 8
        };
        top.Add (subView);

        View? adornment = null;
        switch (adornmentType)
        {
            case "Margin":
                subView.Margin!.Thickness = new (2);
                adornment = subView.Margin;
                break;
            case "Border":
                subView.Border!.Thickness = new (2);
                adornment = subView.Border;
                break;
            case "Padding":
                subView.Padding!.Thickness = new (2);
                adornment = subView.Padding;
                break;
        }

        subView.Layout ();

        // Add a child to the adornment
        View adornmentSubView = new ()
        {
            Id = "adornmentSubView",
            X = 0, Y = 0, Width = 2, Height = 2
        };
        adornment!.Add (adornmentSubView);

        adornment.ViewportSettings = ViewportSettingsFlags.TransparentMouse;

        // Act: Point inside adornmentSubView (which is inside the adornment)
        var result = View.GetViewsUnderLocation (top, new (0, 0), ViewportSettingsFlags.TransparentMouse);

        // Assert: Should contain top, subView, adornment, and adornmentSubView
        Assert.Contains (top, result);
        Assert.DoesNotContain (adornment, result);
        Assert.Contains (adornmentSubView, result);
        Assert.DoesNotContain (subView, result);
        Assert.Equal (top, result [0]);
        Assert.Equal (adornmentSubView, result [^1]);
    }

}
