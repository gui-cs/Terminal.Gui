#nullable enable
using System.Text;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class NeedsDisplayTests ()
{
    [Fact]
    public void NeedsDisplay_False_If_Width_Height_Zero ()
    {
        View view = new () { Width = 0, Height = 0 };
        view.BeginInit ();
        view.EndInit ();
        Assert.False (view.NeedsDisplay);
        //Assert.False (view.SubViewNeedsDisplay);
    }


    [Fact]
    public void NeedsDisplay_True_Initially_If_Width_Height_Not_Zero ()
    {
        View superView = new () { Width = 1, Height = 1 };
        View view1 = new () { Width = 1, Height = 1 };
        View view2 = new () { Width = 1, Height = 1 };

        superView.Add (view1, view2);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.True (superView.NeedsDisplay);
        Assert.True (superView.SubViewNeedsDisplay);
        Assert.True (view1.NeedsDisplay);
        Assert.True (view2.NeedsDisplay);

        superView.Draw ();

        Assert.False (superView.NeedsDisplay);
        Assert.False (superView.SubViewNeedsDisplay);
        Assert.False (view1.NeedsDisplay);
        Assert.False (view2.NeedsDisplay);

        superView.SetNeedsDisplay ();

        Assert.True (superView.NeedsDisplay);
        Assert.True (superView.SubViewNeedsDisplay);
        Assert.True (view1.NeedsDisplay);
        Assert.True (view2.NeedsDisplay);
    }


    [Fact]
    public void NeedsDisplay_True_After_Constructor ()
    {
        var view = new View { Width = 2, Height = 2 };
        Assert.True (view.NeedsDisplay);

        view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDisplay);
    }

    [Fact]
    public void NeedsDisplay_True_After_BeginInit ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDisplay);

        view.BeginInit ();
        Assert.True (view.NeedsDisplay);

        view.NeedsDisplay = false;

        view.BeginInit ();
        Assert.True (view.NeedsDisplay); // Because layout is still needed

        view.Layout ();
        Assert.False (view.NeedsDisplay);
    }

    [Fact]
    public void NeedsDisplay_False_After_EndInit ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDisplay);

        view.BeginInit ();
        Assert.True (view.NeedsDisplay);

        view.EndInit ();
        Assert.True (view.NeedsDisplay);

        view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        view.BeginInit ();
        view.NeedsDisplay = false;
        view.EndInit ();
        Assert.False (view.NeedsDisplay);
    }


    [Fact]
    public void NeedsDisplay_After_SetLayoutNeeded ()
    {
        var view = new View { Width = 2, Height = 2 };
        Assert.True (view.NeedsDisplay);
        Assert.False (view.IsLayoutNeeded ());

        view.Draw ();
        Assert.False (view.NeedsDisplay);
        Assert.False (view.IsLayoutNeeded ());

        view.SetLayoutNeeded ();
        Assert.True (view.NeedsDisplay);
        Assert.True (view.IsLayoutNeeded ());
    }

    [Fact]
    public void NeedsDisplay_False_After_SetRelativeLayout ()
    {
        var view = new View { Width = 2, Height = 2 };
        Assert.True (view.NeedsDisplay);
        Assert.False (view.IsLayoutNeeded ());

        view.Draw ();
        Assert.False (view.NeedsDisplay);
        Assert.False (view.IsLayoutNeeded ());

        // SRL won't change anything since the view is Absolute
        view.SetRelativeLayout (Application.Screen.Size);
        Assert.False (view.NeedsDisplay);

        view.SetLayoutNeeded ();
        // SRL won't change anything since the view is Absolute
        view.SetRelativeLayout (Application.Screen.Size);
        Assert.True (view.NeedsDisplay);

        view.NeedsDisplay = false;
        // SRL won't change anything since the view is Absolute. However, Layout has not been called
        view.SetRelativeLayout (new (10, 10));
        Assert.True (view.NeedsDisplay);

        view = new View { Width = Dim.Percent (50), Height = Dim.Percent (50) };
        View superView = new ()
        {
            Id = "superView",
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        Assert.True (superView.NeedsDisplay);

        superView.Add (view);
        Assert.True (view.NeedsDisplay);
        Assert.True (superView.NeedsDisplay);

        superView.BeginInit ();
        Assert.True (view.NeedsDisplay);
        Assert.True (superView.NeedsDisplay);

        superView.EndInit ();
        Assert.True (view.NeedsDisplay);
        Assert.True (superView.NeedsDisplay);

        superView.SetRelativeLayout (Application.Screen.Size);
        Assert.True (view.NeedsDisplay);
        Assert.True (superView.NeedsDisplay);

        superView.NeedsDisplay = false;
        superView.SetRelativeLayout (new (10, 10));
        Assert.True (superView.NeedsDisplay);
        Assert.True (view.NeedsDisplay);

        superView.Layout ();

        view.SetRelativeLayout (new (11, 11));
        Assert.True (superView.NeedsDisplay);
        Assert.True (view.NeedsDisplay);

    }

    [Fact]
    public void NeedsDisplay_True_After_LayoutSubviews ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDisplay);

        view.BeginInit ();
        Assert.True (view.NeedsDisplay);

        view.EndInit ();
        Assert.True (view.NeedsDisplay);

        view.SetRelativeLayout (Application.Screen.Size);
        Assert.True (view.NeedsDisplay);

        view.LayoutSubviews ();
        Assert.True (view.NeedsDisplay);
    }

    [Fact]
    public void NeedsDisplay_False_After_Draw ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsDisplay);

        view.BeginInit ();
        Assert.True (view.NeedsDisplay);

        view.EndInit ();
        Assert.True (view.NeedsDisplay);

        view.SetRelativeLayout (Application.Screen.Size);
        Assert.True (view.NeedsDisplay);

        view.LayoutSubviews ();
        Assert.True (view.NeedsDisplay);

        view.Draw ();
        Assert.False (view.NeedsDisplay);
    }

    [Fact]
    public void NeedsDisplayRect_Is_Viewport_Relative ()
    {
        View superView = new ()
        {
            Id = "superView",
            Width = 10,
            Height = 10
        };
        Assert.Equal (new (0, 0, 10, 10), superView.Frame);
        Assert.Equal (new (0, 0, 10, 10), superView.Viewport);
        Assert.Equal (new (0, 0, 10, 10), superView._needsDisplayRect);

        var view = new View
        {
            Id = "view",
        };

        view.Frame = new (0, 1, 2, 3);
        Assert.Equal (new (0, 1, 2, 3), view.Frame);
        Assert.Equal (new (0, 0, 2, 3), view.Viewport);
        Assert.Equal (new (0, 0, 2, 3), view._needsDisplayRect);

        superView.Add (view);
        Assert.Equal (new (0, 0, 10, 10), superView.Frame);
        Assert.Equal (new (0, 0, 10, 10), superView.Viewport);
        Assert.Equal (new (0, 0, 10, 10), superView._needsDisplayRect);
        Assert.Equal (new (0, 1, 2, 3), view.Frame);
        Assert.Equal (new (0, 0, 2, 3), view.Viewport);
        Assert.Equal (new (0, 0, 2, 3), view._needsDisplayRect);

        view.Frame = new (3, 3, 5, 5);
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (0, 0, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view._needsDisplayRect);

        view.Frame = new (3, 3, 6, 6); // Grow right/bottom 1
        Assert.Equal (new (3, 3, 6, 6), view.Frame);
        Assert.Equal (new (0, 0, 6, 6), view.Viewport);
        Assert.Equal (new (0, 0, 6, 6), view._needsDisplayRect); 

        view.Frame = new (3, 3, 5, 5); // Shrink right/bottom 1
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (0, 0, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view._needsDisplayRect);

        view.SetContentSize (new (10, 10));
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (0, 0, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view._needsDisplayRect);

        view.Viewport = new (1, 1, 5, 5); // Scroll up/left 1
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (1, 1, 5, 5), view.Viewport);
        Assert.Equal (new (0, 0, 5, 5), view._needsDisplayRect);

        view.Frame = new (3, 3, 6, 6); // Grow right/bottom 1
        Assert.Equal (new (3, 3, 6, 6), view.Frame);
        Assert.Equal (new (1, 1, 6, 6), view.Viewport);
        Assert.Equal (new (1, 1, 6, 6), view._needsDisplayRect); 

        view.Frame = new (3, 3, 5, 5);
        Assert.Equal (new (3, 3, 5, 5), view.Frame);
        Assert.Equal (new (1, 1, 5, 5), view.Viewport);
        Assert.Equal (new (1, 1, 5, 5), view._needsDisplayRect);

    }
}
