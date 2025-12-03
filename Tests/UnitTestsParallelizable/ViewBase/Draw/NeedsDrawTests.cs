#nullable enable
using UnitTests;

namespace ViewBaseTests.Drawing;

[Trait ("Category", "Output")]
public class NeedsDrawTests : FakeDriverBase
{
    [Fact]
    public void NeedsDraw_False_If_Width_Height_Zero ()
    {
        View view = new () { Width = 0, Height = 0 };
        view.BeginInit ();
        view.EndInit ();
        Assert.False (view.NeedsDraw);

        //Assert.False (view.SubViewNeedsDraw);
    }

    [Fact]
    public void NeedsDraw_True_Initially_If_Width_Height_Not_Zero ()
    {
        View superView = new () { Driver = CreateFakeDriver (), Width = 1, Height = 1 };
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

        view = new () { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
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
        Assert.False (view.NeedsDraw); // Because layout is still needed

        view.Layout ();
        // NeedsDraw is true after layout and NeedsLayout is false if SubViewsLaidOut doesn't call SetNeedsLayout
        Assert.True (view.NeedsDraw);
        Assert.False (view.NeedsLayout);
    }

    [Fact]
    public void NeedsDraw_True_After_EndInit_Where_Call_Layout ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.EndInit ();
        Assert.True (view.NeedsDraw);

        view = new () { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.NeedsDraw = false;
        view.EndInit ();
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_After_SetLayoutNeeded_And_Layout ()
    {
        var view = new View { Driver = CreateFakeDriver (), Width = 2, Height = 2 };
        Assert.True (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        view.Draw ();
        Assert.False (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        view.SetNeedsLayout ();
        Assert.False (view.NeedsDraw);
        Assert.True (view.NeedsLayout);

        view.Layout ();
        Assert.True (view.NeedsDraw);
        Assert.False (view.NeedsLayout);
    }

    [Fact]
    public void NeedsDraw_False_After_SetRelativeLayout_Absolute_Dims ()
    {
        var view = new View { Driver = CreateFakeDriver (), Width = 2, Height = 2 };
        Assert.True (view.NeedsDraw);

        view.Draw ();
        Assert.False (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        // SRL won't change anything since the view frame wasn't changed
        view.SetRelativeLayout (new (100, 100));
        Assert.False (view.NeedsDraw);

        view.SetNeedsLayout ();

        // SRL won't change anything since the view frame wasn't changed
        // SRL doesn't depend on NeedsLayout, but LayoutSubViews does
        view.SetRelativeLayout (new (100, 100));
        Assert.False (view.NeedsDraw);
        Assert.True (view.NeedsLayout);

        view.Layout ();
        Assert.True (view.NeedsDraw);
        Assert.False (view.NeedsLayout);

        view.NeedsDraw = false;

        // SRL won't change anything since the view frame wasn't changed. However, Layout has not been called
        view.SetRelativeLayout (new (10, 10));
        Assert.False (view.NeedsDraw);
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

        // A layout wasn't called yet, so NeedsDraw is still empty
        Assert.False (superView.NeedsDraw);

        superView.Add (view);
        // A layout wasn't called yet, so NeedsDraw is still empty
        Assert.False (view.NeedsDraw);
        Assert.False (superView.NeedsDraw);

        superView.BeginInit ();
        Assert.False (view.NeedsDraw);
        Assert.False (superView.NeedsDraw);

        superView.EndInit (); // Call Layout
        Assert.True (view.NeedsDraw);
        Assert.True (superView.NeedsDraw);

        superView.SetRelativeLayout (new (100, 100));
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
        Assert.False (superView.NeedsDraw);

        superView.Layout ();
        Assert.True (superView.NeedsDraw);

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

        view.SetRelativeLayout (new (100, 100));
        Assert.True (view.NeedsDraw);

        view.LayoutSubViews ();
        Assert.True (view.NeedsDraw);
    }

    [Fact]
    public void NeedsDraw_False_After_Draw ()
    {
        var view = new View { Driver = CreateFakeDriver (), Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDraw);

        view.BeginInit ();
        Assert.True (view.NeedsDraw);

        view.EndInit ();
        Assert.True (view.NeedsDraw);

        view.SetRelativeLayout (new (100, 100));
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
        Assert.Equal (new (0, 0, 10, 10), superView.NeedsDrawRect);

        var view = new View
        {
            Id = "view"
        };

        view.Frame = new (0, 1, 2, 3);
        Assert.Equal (new (0, 1, 2, 3), view.Frame);
        Assert.Equal (new (0, 0, 2, 3), view.Viewport);
        Assert.Equal (new (0, 0, 2, 3), view.NeedsDrawRect);

        superView.Add (view);
        Assert.Equal (new (0, 0, 10, 10), superView.Frame);
        Assert.Equal (new (0, 0, 10, 10), superView.Viewport);
        Assert.Equal (new (0, 0, 10, 10), superView.NeedsDrawRect);
        Assert.Equal (new (0, 1, 2, 3), view.Frame);
        Assert.Equal (new (0, 0, 2, 3), view.Viewport);
        Assert.Equal (new (0, 0, 2, 3), view.NeedsDrawRect);

        view.Frame = new (3, 3, 5, 5);
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (0, 0, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view.NeedsDrawRect);

        view.Frame = new (3, 3, 6, 6); // Grow right/bottom 1
        Assert.Equal (new (3, 3, 6, 6), view.Frame);
        Assert.Equal (new (0, 0, 6, 6), view.Viewport);
        Assert.Equal (new (0, 0, 6, 6), view.NeedsDrawRect);

        view.Frame = new (3, 3, 5, 5); // Shrink right/bottom 1
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (0, 0, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view.NeedsDrawRect);

        view.SetContentSize (new (10, 10));
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (0, 0, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view.NeedsDrawRect);

        view.Viewport = new (1, 1, 5, 5); // Scroll up/left 1
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (1, 1, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view.NeedsDrawRect);

        view.Frame = new (3, 3, 6, 6); // Grow right/bottom 1
        Assert.Equal (new (3, 3, 6, 6), view.Frame);
        Assert.Equal (new (1, 1, 6, 6), view.Viewport);
        Assert.Equal (new (1, 1, 6, 6), view.NeedsDrawRect);

        view.Frame = new (3, 3, 5, 5);
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (1, 1, 5, 5), view.Viewport);
        Assert.Equal (new (1, 1, 5, 5), view.NeedsDrawRect);
    }

    [Fact]
    public void ClearNeedsDraw_WithSiblings_DoesNotClearSuperViewSubViewNeedsDraw ()
    {
        // This test verifies the fix for the bug where a subview clearing its NeedsDraw
        // would incorrectly clear the superview's SubViewNeedsDraw flag, even if other siblings
        // still needed drawing.

        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = 50,
            Height = 50,
            Driver = driver
        };

        var subView1 = new View { X = 0, Y = 0, Width = 10, Height = 10, Id = "SubView1" };
        var subView2 = new View { X = 0, Y = 10, Width = 10, Height = 10, Id = "SubView2" };
        var subView3 = new View { X = 0, Y = 20, Width = 10, Height = 10, Id = "SubView3" };

        superView.Add (subView1, subView2, subView3);
        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubViews ();

        // All subviews should need drawing initially
        Assert.True (subView1.NeedsDraw);
        Assert.True (subView2.NeedsDraw);
        Assert.True (subView3.NeedsDraw);
        Assert.True (superView.SubViewNeedsDraw);

        // Draw subView1 - this will call ClearNeedsDraw() on subView1
        subView1.Draw ();

        // SubView1 should no longer need drawing
        Assert.False (subView1.NeedsDraw);

        // But subView2 and subView3 still need drawing
        Assert.True (subView2.NeedsDraw);
        Assert.True (subView3.NeedsDraw);

        // THE BUG: Before the fix, subView1.ClearNeedsDraw() would set superView.SubViewNeedsDraw = false
        // even though subView2 and subView3 still need drawing.
        // After the fix, superView.SubViewNeedsDraw should still be true because subView2 and subView3 need drawing.
        Assert.True (superView.SubViewNeedsDraw, "SuperView's SubViewNeedsDraw should still be true because subView2 and subView3 still need drawing");

        // Now draw subView2
        subView2.Draw ();
        Assert.False (subView2.NeedsDraw);
        Assert.True (subView3.NeedsDraw);

        // SuperView should still have SubViewNeedsDraw = true because subView3 needs drawing
        Assert.True (superView.SubViewNeedsDraw, "SuperView's SubViewNeedsDraw should still be true because subView3 still needs drawing");

        // Now draw subView3
        subView3.Draw ();
        Assert.False (subView3.NeedsDraw);

        // SuperView should STILL have SubViewNeedsDraw = true because it hasn't been cleared by the superview itself
        // Only the superview's own ClearNeedsDraw() should clear this flag
        Assert.True (superView.SubViewNeedsDraw, "SuperView's SubViewNeedsDraw should only be cleared by superView.ClearNeedsDraw(), not by subviews");

        // Finally, draw the superview - this will clear SubViewNeedsDraw
        superView.Draw ();
        Assert.False (superView.SubViewNeedsDraw, "SuperView's SubViewNeedsDraw should now be false after superView.Draw()");
        Assert.False (subView1.NeedsDraw);
        Assert.False (subView2.NeedsDraw);
        Assert.False (subView3.NeedsDraw);
    }

    [Fact]
    public void ClearNeedsDraw_ClearsOwnFlags ()
    {
        // Verify that ClearNeedsDraw properly clears the view's own flags
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (view.NeedsDraw);
        Assert.Equal (view.Viewport, view.NeedsDrawRect);

        view.Draw ();

        Assert.False (view.NeedsDraw);
        Assert.Equal (Rectangle.Empty, view.NeedsDrawRect);
        Assert.False (view.SubViewNeedsDraw);
    }

    [Fact]
    public void ClearNeedsDraw_ClearsAdornments ()
    {
        // Verify that ClearNeedsDraw clears adornment flags
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.Border!.Thickness = new Thickness (1);
        view.Padding!.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (view.Border!.NeedsDraw);
        Assert.True (view.Padding!.NeedsDraw);

        view.Draw ();

        Assert.False (view.Border!.NeedsDraw);
        Assert.False (view.Padding!.NeedsDraw);
    }

    [Fact]
    public void ClearNeedsDraw_PropagatesDownToAllSubViews ()
    {
        // Verify that ClearNeedsDraw clears flags on all descendants
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var topView = new View
        {
            X = 0,
            Y = 0,
            Width = 100,
            Height = 100,
            Driver = driver
        };

        var middleView = new View { X = 10, Y = 10, Width = 50, Height = 50 };
        var bottomView = new View { X = 5, Y = 5, Width = 20, Height = 20 };

        topView.Add (middleView);
        middleView.Add (bottomView);
        topView.BeginInit ();
        topView.EndInit ();
        topView.LayoutSubViews ();

        Assert.True (topView.NeedsDraw);
        Assert.True (middleView.NeedsDraw);
        Assert.True (bottomView.NeedsDraw);

        topView.Draw ();

        Assert.False (topView.NeedsDraw);
        Assert.False (topView.SubViewNeedsDraw);
        Assert.False (middleView.NeedsDraw);
        Assert.False (middleView.SubViewNeedsDraw);
        Assert.False (bottomView.NeedsDraw);
    }
}
