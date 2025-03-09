#nullable enable
namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class NeedsDrawTests
{
    [Fact]
    public void NeedsDraw_False_If_Width_Height_Zero ()
    {
        View view = new () { Width = 0, Height = 0 };
        view.BeginInit ();
        view.EndInit ();
        Assert.True (view.NeedsDraw);

        //Assert.False (view.SubViewNeedsDraw);
    }

    [Fact]
    public void NeedsDraw_True_Initially_If_Width_Height_Not_Zero ()
    {
        View superView = new () { Width = 1, Height = 1 };
        View view1 = new () { Width = 1, Height = 1 };
        View view2 = new () { Width = 1, Height = 1 };

        superView.Add (view1, view2);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.True (superView.NeedsDraw);
        Assert.True (superView.SubViewNeedsDraw);
        Assert.True (view1.NeedsDraw);
        Assert.True (view2.NeedsDraw);

        superView.Layout (); // NeedsDraw is always false if Layout is needed

        superView.Draw ();

        Assert.False (superView.NeedsDraw);
        Assert.False (superView.SubViewNeedsDraw);
        Assert.False (view1.NeedsDraw);
        Assert.False (view2.NeedsDraw);

        superView.SetNeedsDraw ();

        Assert.True (superView.NeedsDraw);
        Assert.True (superView.SubViewNeedsDraw);
        Assert.True (view1.NeedsDraw);
        Assert.True (view2.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_True_After_Constructor ()
    {
        var view = new View { Width = 2, Height = 2 };
        Assert.True (view.NeedsDraw);

        view = new() { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_True_After_BeginInit ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.NeedsDraw = false;

        view.BeginInit ();
        Assert.True (view.NeedsDraw); // Because layout is still needed

        view.Layout ();
        Assert.False (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_False_After_EndInit ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.EndInit ();
        Assert.True (view.NeedsDraw);

        view = new() { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.NeedsDraw = false;
        view.EndInit ();
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_After_SetLayoutNeeded ()
    {
        var view = new View { Width = 2, Height = 2 };
        Assert.True (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        view.Draw ();
        Assert.False (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        view.SetNeedsLayout ();
        Assert.True (view.NeedsDraw);
        Assert.True (view.NeedsLayout);
    }

    [Fact]
    public void NeedsDraw_False_After_SetRelativeLayout_Absolute_Dims ()
    {
        var view = new View { Width = 2, Height = 2 };
        Assert.True (view.NeedsDraw);

        view.Draw ();
        Assert.False (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        // SRL won't change anything since the view is Absolute
        view.SetRelativeLayout (Application.Screen.Size);
        Assert.False (view.NeedsDraw);

        view.SetNeedsLayout ();

        // SRL won't change anything since the view is Absolute
        view.SetRelativeLayout (Application.Screen.Size);
        Assert.True (view.NeedsDraw);

        view.NeedsDraw = false;

        // SRL won't change anything since the view is Absolute. However, Layout has not been called
        view.SetRelativeLayout (new (10, 10));
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_False_After_SetRelativeLayout_Relative_Dims ()
    {
        var view = new View { Width = Dim.Percent (50), Height = Dim.Percent (50) };

        View superView = new ()
        {
            Id = "superView",
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Assert.True (superView.NeedsDraw);

        superView.Add (view);
        Assert.True (view.NeedsDraw);
        Assert.True (superView.NeedsDraw);

        superView.BeginInit ();
        Assert.True (view.NeedsDraw);
        Assert.True (superView.NeedsDraw);

        superView.EndInit ();
        Assert.True (view.NeedsDraw);
        Assert.True (superView.NeedsDraw);

        superView.SetRelativeLayout (Application.Screen.Size);
        Assert.True (view.NeedsDraw);
        Assert.True (superView.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_False_After_SetRelativeLayout_10x10 ()
    {
        View superView = new ()
        {
            Id = "superView",
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Assert.True (superView.NeedsDraw);

        superView.Layout ();

        superView.NeedsDraw = false;
        superView.SetRelativeLayout (new (10, 10));
        Assert.True (superView.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_True_After_LayoutSubViews ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.EndInit ();
        Assert.True (view.NeedsDraw);

        view.SetRelativeLayout (Application.Screen.Size);
        Assert.True (view.NeedsDraw);

        view.LayoutSubViews ();
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_False_After_Draw ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.EndInit ();
        Assert.True (view.NeedsDraw);

        view.SetRelativeLayout (Application.Screen.Size);
        Assert.True (view.NeedsDraw);

        view.LayoutSubViews ();
        Assert.True (view.NeedsDraw);

        view.Draw ();
        Assert.False (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDrawRect_Is_Viewport_Relative ()
    {
        View superView = new ()
        {
            Id = "superView",
            Width = 10,
            Height = 10
        };
        Assert.Equal (new (0, 0, 10, 10), superView.Frame);
        Assert.Equal (new (0, 0, 10, 10), superView.Viewport);
        Assert.Equal (new (0, 0, 10, 10), superView._needsDrawRect);

        var view = new View
        {
            Id = "view"
        };

        view.Frame = new (0, 1, 2, 3);
        Assert.Equal (new (0, 1, 2, 3), view.Frame);
        Assert.Equal (new (0, 0, 2, 3), view.Viewport);
        Assert.Equal (new (0, 0, 2, 3), view._needsDrawRect);

        superView.Add (view);
        Assert.Equal (new (0, 0, 10, 10), superView.Frame);
        Assert.Equal (new (0, 0, 10, 10), superView.Viewport);
        Assert.Equal (new (0, 0, 10, 10), superView._needsDrawRect);
        Assert.Equal (new (0, 1, 2, 3), view.Frame);
        Assert.Equal (new (0, 0, 2, 3), view.Viewport);
        Assert.Equal (new (0, 0, 2, 3), view._needsDrawRect);

        view.Frame = new (3, 3, 5, 5);
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (0, 0, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view._needsDrawRect);

        view.Frame = new (3, 3, 6, 6); // Grow right/bottom 1
        Assert.Equal (new (3, 3, 6, 6), view.Frame);
        Assert.Equal (new (0, 0, 6, 6), view.Viewport);
        Assert.Equal (new (0, 0, 6, 6), view._needsDrawRect);

        view.Frame = new (3, 3, 5, 5); // Shrink right/bottom 1
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (0, 0, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view._needsDrawRect);

        view.SetContentSize (new (10, 10));
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (0, 0, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view._needsDrawRect);

        view.Viewport = new (1, 1, 5, 5); // Scroll up/left 1
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (1, 1, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view._needsDrawRect);

        view.Frame = new (3, 3, 6, 6); // Grow right/bottom 1
        Assert.Equal (new (3, 3, 6, 6), view.Frame);
        Assert.Equal (new (1, 1, 6, 6), view.Viewport);
        Assert.Equal (new (1, 1, 6, 6), view._needsDrawRect);

        view.Frame = new (3, 3, 5, 5);
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (1, 1, 5, 5), view.Viewport);
        Assert.Equal (new (1, 1, 5, 5), view._needsDrawRect);
    }
}
