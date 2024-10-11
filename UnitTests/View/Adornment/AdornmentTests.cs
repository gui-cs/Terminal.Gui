﻿using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AdornmentTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Viewport_Location_Always_Empty_Size_Correct ()
    {
        var view = new View
        {
            X = 1,
            Y = 2,
            Width = 20,
            Height = 20
        };

        view.BeginInit ();
        view.EndInit ();

        Assert.Equal (new (1, 2, 20, 20), view.Frame);
        Assert.Equal (new (0, 0, 20, 20), view.Viewport);

        var marginThickness = 1;
        view.Margin.Thickness = new (marginThickness);
        Assert.Equal (new (0, 0, 18, 18), view.Viewport);

        var borderThickness = 2;
        view.Border.Thickness = new (borderThickness);
        Assert.Equal (new (0, 0, 14, 14), view.Viewport);

        var paddingThickness = 3;
        view.Padding.Thickness = new Thickness (paddingThickness);
        Assert.Equal (new (0, 0, 8, 8), view.Viewport);

        Assert.Equal (new (0, 0, view.Margin.Frame.Width, view.Margin.Frame.Height), view.Margin.Viewport);

        Assert.Equal (new (0, 0, view.Border.Frame.Width, view.Border.Frame.Height), view.Border.Viewport);

        Assert.Equal (new (0, 0, view.Padding.Frame.Width, view.Padding.Frame.Height), view.Padding.Viewport);
    }

    // Test that Adornment.Viewport_get override returns Frame.Size minus Thickness
    [Theory]
    [InlineData (0, 0, 0, 0, 0)]
    [InlineData (0, 0, 0, 1, 1)]
    [InlineData (0, 0, 0, 1, 0)]
    [InlineData (0, 0, 0, 0, 1)]
    [InlineData (1, 0, 0, 0, 0)]
    [InlineData (1, 0, 0, 1, 1)]
    [InlineData (1, 0, 0, 1, 0)]
    [InlineData (1, 0, 0, 0, 1)]
    [InlineData (1, 0, 0, 4, 4)]
    [InlineData (1, 0, 0, 4, 0)]
    [InlineData (1, 0, 0, 0, 4)]

    [InlineData (0, 1, 0, 0, 0)]
    [InlineData (0, 1, 0, 1, 1)]
    [InlineData (0, 1, 0, 1, 0)]
    [InlineData (0, 1, 0, 0, 1)]
    [InlineData (1, 1, 0, 0, 0)]
    [InlineData (1, 1, 0, 1, 1)]
    [InlineData (1, 1, 0, 1, 0)]
    [InlineData (1, 1, 0, 0, 1)]
    [InlineData (1, 1, 0, 4, 4)]
    [InlineData (1, 1, 0, 4, 0)]
    [InlineData (1, 1, 0, 0, 4)]

    [InlineData (0, 1, 1, 0, 0)]
    [InlineData (0, 1, 1, 1, 1)]
    [InlineData (0, 1, 1, 1, 0)]
    [InlineData (0, 1, 1, 0, 1)]
    [InlineData (1, 1, 1, 0, 0)]
    [InlineData (1, 1, 1, 1, 1)]
    [InlineData (1, 1, 1, 1, 0)]
    [InlineData (1, 1, 1, 0, 1)]
    [InlineData (1, 1, 1, 4, 4)]
    [InlineData (1, 1, 1, 4, 0)]
    [InlineData (1, 1, 1, 0, 4)]
    public void Viewport_Width_Is_Frame_Width (int thickness, int x, int y, int w, int h)
    {
        var adornment = new Adornment (null);
        adornment.Thickness = new Thickness (thickness);
        adornment.Frame = new Rectangle (x, y, w, h);
        Assert.Equal (new Rectangle (x, y, w, h), adornment.Frame);

        var expectedBounds = new Rectangle (0, 0, w, h);
        Assert.Equal (expectedBounds, adornment.Viewport);
    }

    // Test that Adornment.Viewport_get override uses Parent not SuperView
    [Fact]
    public void BoundsToScreen_Uses_Parent_Not_SuperView ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Viewport);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.Viewport);

        Assert.Null (parent.Margin.SuperView);
        Rectangle boundsAsScreen = parent.Margin.ViewportToScreen (new Rectangle (1, 2, 5, 5));
        Assert.Equal (new Rectangle (2, 4, 5, 5), boundsAsScreen);
    }

    [Fact]
    public void Frames_are_Parent_SuperView_Relative ()
    {
        var view = new View
        {
            X = 1,
            Y = 2,
            Width = 20,
            Height = 31
        };

        var marginThickness = 1;
        view.Margin.Thickness = new Thickness (marginThickness);

        var borderThickness = 2;
        view.Border.Thickness = new Thickness (borderThickness);

        var paddingThickness = 3;
        view.Padding.Thickness = new Thickness (paddingThickness);

        view.BeginInit ();
        view.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 20, 31), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 19), view.Viewport);

        // Margin.Frame is always the same as the view frame
        Assert.Equal (new Rectangle (0, 0, 20, 31), view.Margin.Frame);

        // Border.Frame is View.Frame minus the Margin thickness 
        Assert.Equal (
                      new Rectangle (marginThickness, marginThickness, view.Frame.Width - marginThickness * 2, view.Frame.Height - marginThickness * 2),
                      view.Border.Frame);

        // Padding.Frame is View.Frame minus the Border thickness plus Margin thickness
        Assert.Equal (
                      new Rectangle (
                                     marginThickness + borderThickness,
                                     marginThickness + borderThickness,
                                     view.Frame.Width - (marginThickness + borderThickness) * 2,
                                     view.Frame.Height - (marginThickness + borderThickness) * 2),
                      view.Padding.Frame);
    }

    // Test that Adornment.FrameToScreen override retains Frame.Size
    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (0, 1, 1)]
    [InlineData (0, 10, 10)]
    [InlineData (1, 0, 0)]
    [InlineData (1, 1, 1)]
    [InlineData (1, 10, 10)]
    public void FrameToScreen_Retains_Frame_Size (int marginThickness, int w, int h)
    {
        var parent = new View { X = 1, Y = 2, Width = w, Height = h };
        parent.Margin.Thickness = new Thickness (marginThickness);

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, w, h), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, w, h), parent.Margin.Frame);

        Assert.Equal (parent.Frame, parent.Margin.FrameToScreen ());
    }

    // Test that Adornment.FrameToScreen override returns Frame if Parent is null
    [Fact]
    public void FrameToScreen_Returns_Frame_If_Parent_Is_Null ()
    {
        var a = new Adornment
        {
            X = 1,
            Y = 2,
            Width = 3,
            Height = 4
        };

        Assert.Null (a.Parent);
        Assert.Equal (a.Frame, a.FrameToScreen ());
    }

    // Test that Adornment.FrameToScreen override returns correct location
    [Theory]
    [InlineData (0, 0, 0, 0)]
    [InlineData (0, 0, 1, 1)]
    [InlineData (0, 0, 10, 10)]
    [InlineData (1, 0, 0, 0)]
    [InlineData (1, 0, 1, 1)]
    [InlineData (1, 0, 10, 10)]
    [InlineData (0, 1, 0, 0)]
    [InlineData (0, 1, 1, 1)]
    [InlineData (0, 1, 10, 10)]
    [InlineData (1, 1, 0, 0)]
    [InlineData (1, 1, 1, 1)]
    [InlineData (1, 1, 10, 10)]
    public void FrameToScreen_Returns_Screen_Location (int marginThickness, int borderThickness, int x, int y)
    {
        var superView = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20
        };
        superView.Margin.Thickness = new Thickness (marginThickness);
        superView.Border.Thickness = new Thickness (borderThickness);

        var view = new View { X = x, Y = y, Width = 1, Height = 1 };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (new Rectangle (x, y, 1, 1), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 20, 20), superView.Margin.Frame);

        Assert.Equal (
                      new Rectangle (marginThickness, marginThickness, 20 - marginThickness * 2, 20 - marginThickness * 2),
                      superView.Border.Frame
                     );

        Assert.Equal (
                      new Rectangle (superView.Frame.X + marginThickness, superView.Frame.Y + marginThickness, 20 - marginThickness * 2, 20 - marginThickness * 2),
                      superView.Border.FrameToScreen ()
                     );
    }

    // Test that Adornment.FrameToScreen override uses Parent not SuperView
    [Fact]
    public void FrameToScreen_Uses_Parent_Not_SuperView ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Viewport);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.Viewport);

        Assert.Null (parent.Margin.SuperView);
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Margin.FrameToScreen ());
    }

    [Fact]
    public void GetAdornmentsThickness ()
    {
        var view = new View ();
        Assert.Equal (Thickness.Empty, view.GetAdornmentsThickness ());

        view.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Thickness (1), view.GetAdornmentsThickness ());

        view.Border.Thickness = new Thickness (1);
        Assert.Equal (new Thickness (2), view.GetAdornmentsThickness ());

        view.Padding.Thickness = new Thickness (1);
        Assert.Equal (new Thickness (3), view.GetAdornmentsThickness ());

        view.Padding.Thickness = new Thickness (2);
        Assert.Equal (new Thickness (4), view.GetAdornmentsThickness ());

        view.Padding.Thickness = new Thickness (1, 2, 3, 4);
        Assert.Equal (new Thickness (3, 4, 5, 6), view.GetAdornmentsThickness ());

        view.Margin.Thickness = new Thickness (1, 2, 3, 4);
        Assert.Equal (new Thickness (3, 5, 7, 9), view.GetAdornmentsThickness ());
        view.Dispose ();
    }

    [Fact]
    public void Setting_Viewport_Throws ()
    {
        var adornment = new Adornment (null);
        Assert.Throws<InvalidOperationException> (() => adornment.Viewport = new Rectangle (1, 2, 3, 4));
    }

    [Fact]
    public void Setting_SuperView_Throws ()
    {
        var adornment = new Adornment (null);
        Assert.Throws<InvalidOperationException> (() => adornment.SuperView = new View ());
    }

    [Fact]
    public void Setting_SuperViewRendersLineCanvas_Throws ()
    {
        var adornment = new Adornment (null);
        Assert.Throws<InvalidOperationException> (() => adornment.SuperViewRendersLineCanvas = true);
    }

    [Fact]
    public void Setting_Thickness_Changes_Parent_Bounds ()
    {
        var parent = new View { Width = 10, Height = 10 };
        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Viewport);

        parent.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 8), parent.Viewport);
    }

    [Fact]
    public void Setting_Thickness_Raises_ThicknessChanged ()
    {
        var adornment = new Adornment (null);
        var super = new View ();
        var raised = false;
        adornment.ThicknessChanged += (s, e) =>
                                      {
                                          raised = true;
                                          Assert.Equal (new Thickness (1, 2, 3, 4), adornment.Thickness);
                                      };
        adornment.Thickness = new Thickness (1, 2, 3, 4);
        Assert.True (raised);
    }


    [Fact]
    public void Setting_Thickness_Causes_Parent_Layout ()
    {
        var view = new View ();
        var raised = false;
        view.BeginInit ();
        view.EndInit ();

        view.LayoutStarted += LayoutStarted;
        view.Margin.Thickness = new Thickness (1, 2, 3, 4);
        Assert.True (raised);

        return;
        void LayoutStarted (object sender, LayoutEventArgs e)
        {
            raised = true;
        }

    }

    [Fact]
    public void Setting_Thickness_Causes_Adornment_Layout ()
    {
        var view = new View ();
        var raised = false;
        view.BeginInit ();
        view.EndInit ();

        view.Margin.LayoutStarted += LayoutStarted;
        view.Margin.Thickness = new Thickness (1, 2, 3, 4);
        Assert.True (raised);

        return;
        void LayoutStarted (object sender, LayoutEventArgs e)
        {
            raised = true;
        }
    }

    [Fact]
    public void Set_Viewport_Throws ()
    {
        View view = new ();

        view.BeginInit ();
        view.EndInit ();
        view.Padding.Thickness = new (2, 2, 2, 2);
        Assert.Throws<InvalidOperationException> (() => view.Padding.Viewport = view.Padding.Viewport with { Location = new (1, 1) });
    }

    // Contains tests

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 1, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 1, 0, 0, true)]
    [InlineData (0, 0, 1, 2, 0, 0, true)]

    [InlineData (1, 1, 0, 0, 0, 0, false)]
    [InlineData (1, 1, 0, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 1, false)]
    [InlineData (1, 1, 1, 1, 1, 0, false)]
    [InlineData (1, 1, 1, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 1, 1, 1, true)]
    [InlineData (1, 1, 1, 2, 1, 1, true)]
    public void Contains_Left_Only (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        Adornment adornment = new () { Id = "adornment" };
        adornment.Parent = new View () { Id = "parent" };
        adornment.Parent.Frame = new Rectangle (x, y, width, height);
        adornment.Thickness = new (1, 0, 0, 0);
        adornment.Frame = adornment.Parent.Frame with { Location = Point.Empty };

        bool result = adornment.Contains (new (pointX, pointY));
        Assert.Equal (expected, result);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 1, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 1, 0, 0, true)]
    [InlineData (0, 0, 1, 2, 0, 0, true)]

    [InlineData (1, 1, 0, 0, 0, 0, false)]
    [InlineData (1, 1, 0, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 1, false)]
    [InlineData (1, 1, 1, 1, 1, 0, false)]
    [InlineData (1, 1, 1, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 1, 1, 1, true)]
    [InlineData (1, 1, 1, 2, 1, 1, true)]
    public void Contains_Right_Only (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        Adornment adornment = new () { Id = "adornment" };
        adornment.Parent = new View () { Id = "parent" };
        adornment.Parent.Frame = new Rectangle (x, y, width, height);
        adornment.Thickness = new (0, 0, 1, 0);
        adornment.Frame = adornment.Parent.Frame with { Location = Point.Empty };

        bool result = adornment.Contains (new (pointX, pointY));
        Assert.Equal (expected, result);
    }


    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 1, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 1, 0, 0, true)]
    [InlineData (0, 0, 1, 2, 0, 0, true)]

    [InlineData (1, 1, 0, 0, 0, 0, false)]
    [InlineData (1, 1, 0, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 1, false)]
    [InlineData (1, 1, 1, 1, 1, 0, false)]
    [InlineData (1, 1, 1, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 1, 1, 1, true)]
    [InlineData (1, 1, 1, 2, 1, 1, true)]
    public void Contains_Top_Only (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        Adornment adornment = new () { Id = "adornment" };
        adornment.Parent = new View () { Id = "parent" };
        adornment.Parent.Frame = new Rectangle (x, y, width, height);
        adornment.Thickness = new (0, 1, 0, 0);
        adornment.Frame = adornment.Parent.Frame with { Location = Point.Empty };

        bool result = adornment.Contains (new (pointX, pointY));
        Assert.Equal (expected, result);
    }


    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0, false)]
    [InlineData (0, 0, 0, 1, 0, 0, false)]
    [InlineData (0, 0, 1, 0, 0, 0, false)]
    [InlineData (0, 0, 1, 1, 0, 0, true)]
    [InlineData (0, 0, 1, 2, 0, 0, true)]

    [InlineData (1, 1, 0, 0, 0, 0, false)]
    [InlineData (1, 1, 0, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 0, false)]
    [InlineData (1, 1, 1, 0, 0, 1, false)]
    [InlineData (1, 1, 1, 1, 1, 0, false)]
    [InlineData (1, 1, 1, 1, 0, 0, false)]
    [InlineData (1, 1, 1, 1, 1, 1, true)]
    [InlineData (1, 1, 1, 2, 1, 1, true)]
    public void Contains_TopLeft_Only (int x, int y, int width, int height, int pointX, int pointY, bool expected)
    {
        Adornment adornment = new () { Id = "adornment" };
        adornment.Parent = new View () { Id = "parent" };
        adornment.Parent.Frame = new Rectangle (x, y, width, height);
        adornment.Thickness = new (1, 1, 0, 0);
        adornment.Frame = adornment.Parent.Frame with { Location = Point.Empty };

        bool result = adornment.Contains (new (pointX, pointY));
        Assert.Equal (expected, result);
    }
}
