using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AdornmentTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Bounds_Location_Always_Empty_Size_Correct ()
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
        Assert.Equal (new (0, 0, 20, 20), view.ContentArea);

        var marginThickness = 1;
        view.Margin.Thickness = new  (marginThickness);
        Assert.Equal (new (0, 0, 18, 18), view.ContentArea);

        var borderThickness = 2;
        view.Border.Thickness = new (borderThickness);
        Assert.Equal (new (0, 0, 14, 14), view.ContentArea);

        var paddingThickness = 3;
        view.Padding.Thickness = new Thickness (paddingThickness);
        Assert.Equal (new (0, 0, 8, 8), view.ContentArea);

        Assert.Equal (new (0, 0, view.Margin.Frame.Width, view.Margin.Frame.Height), view.Margin.ContentArea);

        Assert.Equal (new (0, 0, view.Border.Frame.Width, view.Border.Frame.Height), view.Border.ContentArea);

        Assert.Equal (new (0, 0, view.Padding.Frame.Width , view.Padding.Frame.Height), view.Padding.ContentArea);
    }

    // Test that Adornment.Bounds_get override returns Frame.Size minus Thickness
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
    public void Bounds_Width_Is_Frame_Width (int thickness, int x, int y, int w, int h)
    {
        var adornment = new Adornment (null);
        adornment.Thickness = new Thickness (thickness);
        adornment.Frame = new Rectangle (x, y, w, h);
        Assert.Equal (new Rectangle (x, y, w, h), adornment.Frame);

        var expectedBounds = new Rectangle (0, 0, w, h);
        Assert.Equal (expectedBounds, adornment.ContentArea);
    }

    // Test that Adornment.Bounds_get override uses Parent not SuperView
    [Fact]
    public void BoundsToScreen_All_Adornments_With_Thickness ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };
        parent.Margin.Thickness = new Thickness (1);
        parent.Border.Thickness = new Thickness (1);
        parent.Padding.Thickness = new Thickness (1);

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 4, 4), parent.ContentArea);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.ContentArea);
        Assert.Equal (new Rectangle (1, 1, 8, 8), parent.Border.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 8), parent.Border.ContentArea);
        Assert.Equal (new Rectangle (2, 2, 6, 6), parent.Padding.Frame);
        Assert.Equal (new Rectangle (0, 0, 6, 6), parent.Padding.ContentArea);

        Assert.Null (parent.Margin.SuperView);
        Rectangle boundsAsScreen = parent.BoundsToScreen (parent.ContentArea);
        Assert.Equal (new Rectangle (4, 5, 4, 4), boundsAsScreen);
        boundsAsScreen = parent.Margin.BoundsToScreen (parent.Margin.ContentArea);
        Assert.Equal (new Rectangle (1, 2, 10, 10), boundsAsScreen);
        boundsAsScreen = parent.Border.BoundsToScreen (parent.Border.ContentArea);
        Assert.Equal (new Rectangle (2, 3, 8, 8), boundsAsScreen);
        boundsAsScreen = parent.Padding.BoundsToScreen (parent.Padding.ContentArea);
        Assert.Equal (new Rectangle (3, 4, 6, 6), boundsAsScreen);
    }

    [Fact]
    public void BoundsToScreen_Uses_Parent_Not_SuperView ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.ContentArea);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.ContentArea);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Border.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Border.ContentArea);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Padding.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Padding.ContentArea);

        Assert.Null (parent.Margin.SuperView);
        Rectangle boundsAsScreen = parent.BoundsToScreen (new Rectangle (1, 2, 5, 5));
        Assert.Equal (new Rectangle (2, 4, 5, 5), boundsAsScreen);
        boundsAsScreen = parent.Margin.BoundsToScreen (new Rectangle (1, 2, 5, 5));
        Assert.Equal (new Rectangle (2, 4, 5, 5), boundsAsScreen);
        boundsAsScreen = parent.Border.BoundsToScreen (new Rectangle (1, 2, 5, 5));
        Assert.Equal (new Rectangle (2, 4, 5, 5), boundsAsScreen);
        boundsAsScreen = parent.Padding.BoundsToScreen (new Rectangle (1, 2, 5, 5));
        Assert.Equal (new Rectangle (2, 4, 5, 5), boundsAsScreen);
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Centered_View_With_Thickness_One_On_All_Adornments ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (20, 11);

        Attribute [] attrs =
        [
            Attribute.Default,
            new Attribute (ColorName.White, ColorName.Blue),
            new Attribute (ColorName.Black, ColorName.DarkGray),
            new Attribute (ColorName.White, ColorName.Red),
            new Attribute (ColorName.Black, ColorName.Gray)
        ];

        var top = new View { Width = 20, Height = 11, ColorScheme = new ColorScheme { Normal = attrs [0] } };

        var parent = new View
        {
            X = Pos.Center (), Y = Pos.Center (), Width = 10, Height = 9,
            ColorScheme = new ColorScheme { Normal = attrs [4] },
            BorderStyle = LineStyle.Single, Text = "Test",
            TextAlignment = TextAlignment.Centered, VerticalTextAlignment = VerticalTextAlignment.Middle
        };

        parent.Margin.ColorScheme = new ColorScheme
            { Normal = attrs [1] };
        parent.Margin.Thickness = new Thickness (1);

        parent.Border.ColorScheme = new ColorScheme
            { Normal = attrs [2] };

        parent.Padding.ColorScheme = new ColorScheme
            { Normal = attrs [3] };
        parent.Padding.Thickness = new Thickness (1);
        top.Add (parent);
        top.BeginInit ();
        top.EndInit ();

        top.Draw ();
        Assert.Equal ("{X=0,Y=0,Width=10,Height=9}", parent.Margin.Frame.ToString ());
        Assert.Equal ("{X=1,Y=1,Width=8,Height=7}", parent.Border.Frame.ToString ());
        Assert.Equal ("{X=2,Y=2,Width=6,Height=5}", parent.Padding.Frame.ToString ());
        Assert.Equal ("{X=5,Y=1,Width=10,Height=9}", parent.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=10,Height=9}", parent.Margin.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=7}", parent.Border.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=6,Height=5}", parent.Padding.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=4,Height=3}", parent.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=10,Height=9}", parent.Margin.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=7}", parent.Border.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=6,Height=5}", parent.Padding.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=4,Height=3}", parent.GetVisibleContentArea ().ToString ());

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
      ┌──────┐
      │      │
      │      │
      │ Test │
      │      │
      │      │
      └──────┘",
                                                      _output);

        TestHelpers.AssertDriverAttributesAre (
                                               @"
00000000000000000000
00000111111111100000
00000122222222100000
00000123333332100000
00000123444432100000
00000123444432100000
00000123444432100000
00000123333332100000
00000122222222100000
00000111111111100000
00000000000000000000",
                                               null,
                                               attrs);
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Centered_View_With_Thickness_One_On_All_Adornments_Inside_Another_Container ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (20, 11);

        Attribute [] attrs =
        [
            new Attribute (ColorName.White, ColorName.Magenta),
            new Attribute (ColorName.White, ColorName.Blue),
            new Attribute (ColorName.Black, ColorName.DarkGray),
            new Attribute (ColorName.White, ColorName.Red),
            new Attribute (ColorName.Black, ColorName.Gray)
        ];

        var superTop = new View { Width = 20, Height = 11 };

        var parent = new View
        {
            X = Pos.Center (), Y = Pos.Center (), Width = 10, Height = 9,
            ColorScheme = new ColorScheme { Normal = attrs [4] },
            BorderStyle = LineStyle.Single, Text = "Test",
            TextAlignment = TextAlignment.Centered, VerticalTextAlignment = VerticalTextAlignment.Middle
        };

        parent.Margin.ColorScheme = new ColorScheme
            { Normal = attrs [1] };
        parent.Margin.Thickness = new Thickness (1);

        parent.Border.ColorScheme = new ColorScheme
            { Normal = attrs [2] };

        parent.Padding.ColorScheme = new ColorScheme
            { Normal = attrs [3] };
        parent.Padding.Thickness = new Thickness (1);
        var top = new View { Width = Dim.Fill (), Height = Dim.Fill (), ColorScheme = new ColorScheme { Normal = attrs [0] }, BorderStyle = LineStyle.Single };
        top.Add (parent);
        superTop.Add (top);
        superTop.BeginInit ();
        superTop.EndInit ();

        superTop.Draw ();
        Assert.Equal ("{X=0,Y=0,Width=20,Height=11}", top.Margin.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=20,Height=11}", top.Border.Frame.ToString ());
        Assert.Equal ("{X=1,Y=1,Width=18,Height=9}", top.Padding.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=20,Height=11}", top.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=10,Height=9}", parent.Margin.Frame.ToString ());
        Assert.Equal ("{X=1,Y=1,Width=8,Height=7}", parent.Border.Frame.ToString ());
        Assert.Equal ("{X=2,Y=2,Width=6,Height=5}", parent.Padding.Frame.ToString ());
        Assert.Equal ("{X=4,Y=0,Width=10,Height=9}", parent.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=10,Height=9}", parent.Margin.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=7}", parent.Border.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=6,Height=5}", parent.Padding.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=4,Height=3}", parent.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=10,Height=9}", parent.Margin.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=7}", parent.Border.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=6,Height=5}", parent.Padding.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=4,Height=3}", parent.GetVisibleContentArea ().ToString ());

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│                  │
│     ┌──────┐     │
│     │      │     │
│     │      │     │
│     │ Test │     │
│     │      │     │
│     │      │     │
│     └──────┘     │
│                  │
└──────────────────┘",
                                                      _output);

        TestHelpers.AssertDriverAttributesAre (
                                               @"
00000000000000000000
00000111111111100000
00000122222222100000
00000123333332100000
00000123444432100000
00000123444432100000
00000123444432100000
00000123333332100000
00000122222222100000
00000111111111100000
00000000000000000000",
                                               null,
                                               attrs);
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
        Assert.Equal (new Rectangle (0, 0, 8, 19), view.ContentArea);

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
    public void FrameToScreen_All_Adornments_With_Thickness ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };
        parent.Margin.Thickness = new Thickness (1);
        parent.Border.Thickness = new Thickness (1);
        parent.Padding.Thickness = new Thickness (1);

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 4, 4), parent.ContentArea);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.ContentArea);
        Assert.Equal (new Rectangle (1, 1, 8, 8), parent.Border.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 8), parent.Border.ContentArea);
        Assert.Equal (new Rectangle (2, 2, 6, 6), parent.Padding.Frame);
        Assert.Equal (new Rectangle (0, 0, 6, 6), parent.Padding.ContentArea);

        Assert.Null (parent.Margin.SuperView);
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.FrameToScreen ());
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Margin.FrameToScreen ());
        Assert.Equal (new Rectangle (2, 3, 8, 8), parent.Border.FrameToScreen ());
        Assert.Equal (new Rectangle (3, 4, 6, 6), parent.Padding.FrameToScreen ());
    }

    [Fact]
    public void FrameToScreen_All_Adornments_With_Thickness_With_SuperView ()
    {
        var top = new View { Id = "top", Width = 12, Height = 12 };
        var parent = new View { Id = "parent", X = 1, Y = 2, Width = 10, Height = 10 };
        top.Add (parent);
        parent.Margin.Thickness = new Thickness (1);
        parent.Border.Thickness = new Thickness (1);
        parent.Padding.Thickness = new Thickness (1);

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 12, 12), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 12, 12), top.ContentArea);
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 4, 4), parent.ContentArea);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.ContentArea);
        Assert.Equal (new Rectangle (1, 1, 8, 8), parent.Border.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 8), parent.Border.ContentArea);
        Assert.Equal (new Rectangle (2, 2, 6, 6), parent.Padding.Frame);
        Assert.Equal (new Rectangle (0, 0, 6, 6), parent.Padding.ContentArea);

        Assert.Null (parent.Margin.SuperView);
        Assert.Equal (new Rectangle (0, 0, 12, 12), top.FrameToScreen ());
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.FrameToScreen ());
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Margin.FrameToScreen ());
        Assert.Equal (new Rectangle (2, 3, 8, 8), parent.Border.FrameToScreen ());
        Assert.Equal (new Rectangle (3, 4, 6, 6), parent.Padding.FrameToScreen ());
    }

    [Theory]
    [InlineData (0, 0, "Margin", false)]
    [InlineData (1, 1, "Margin", false)]
    [InlineData (1, 2, "Margin", true)]
    [InlineData (1, 4, "Margin", true)]
    [InlineData (1, 1, "Border", false)]
    [InlineData (1, 2, "Border", false)]
    [InlineData (2, 2, "Border", false)]
    [InlineData (2, 3, "Border", true)]
    [InlineData (2, 4, "Border", true)]
    [InlineData (3, 4, "Border", false)]
    [InlineData (1, 2, "Padding", false)]
    [InlineData (1, 3, "Padding", false)]
    [InlineData (2, 3, "Padding", false)]
    [InlineData (3, 4, "Padding", true)]
    [InlineData (4, 5, "Padding", false)]
    [InlineData (5, 4, "Padding", true)]
    [InlineData (5, 6, "Padding", false)]
    public void FrameToScreen_Find_Adornment_By_Location (int x, int y, string adornment, bool expectedBool)
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };
        parent.Margin.Thickness = new Thickness (1);
        parent.Border.Thickness = new Thickness (1);
        parent.Padding.Thickness = new Thickness (1);

        parent.BeginInit ();
        parent.EndInit ();

        switch (adornment)
        {
            case "Margin":
                Assert.Equal (parent.Margin?.Thickness.Contains (parent.Margin.FrameToScreen (), x, y) ?? false, expectedBool);

                break;
            case "Border":
                Assert.Equal (parent.Border?.Thickness.Contains (parent.Border.FrameToScreen (), x, y) ?? false, expectedBool);

                break;
            case "Padding":
                Assert.Equal (parent.Padding?.Thickness.Contains (parent.Padding.FrameToScreen (), x, y) ?? false, expectedBool);

                break;
        }
    }

    [Fact]
    public void FrameToScreen_Uses_Parent_Not_SuperView ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.ContentArea);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Margin.ContentArea);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Border.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Border.ContentArea);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Padding.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Padding.ContentArea);

        Assert.Null (parent.Margin.SuperView);
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.FrameToScreen ());
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Margin.FrameToScreen ());
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Border.FrameToScreen ());
        Assert.Equal (new Rectangle (1, 2, 10, 10), parent.Padding.FrameToScreen ());
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
    public void GetAdornmentsThickness_On_Adornments ()
    {
        var view = new View { Width = 10, Height = 10 };
        view.Margin.Thickness = new Thickness (1);
        view.Border.Thickness = new Thickness (1);
        view.Padding.Thickness = new Thickness (1);

        Assert.Equal (new Thickness (3, 3, 3, 3), view.GetAdornmentsThickness ());
        Assert.Equal (Thickness.Empty, view.Margin.GetAdornmentsThickness ());
        Assert.Equal (new Thickness (1), view.Border.GetAdornmentsThickness ());
        Assert.Equal (new Thickness (2), view.Padding.GetAdornmentsThickness ());
        view.Dispose ();
    }

    [Fact]
    public void Setting_Bounds_Throws ()
    {
        var adornment = new Adornment (null);
        Assert.Throws<InvalidOperationException> (() => adornment.ContentArea = new Rectangle (1, 2, 3, 4));
    }

    [Fact]
    public void Setting_SuperView_Throws ()
    {
        var adornment = new Adornment (null);
        Assert.Throws<NotImplementedException> (() => adornment.SuperView = new View ());
    }

    [Fact]
    public void Setting_SuperViewRendersLineCanvas_Throws ()
    {
        var adornment = new Adornment (null);
        Assert.Throws<NotImplementedException> (() => adornment.SuperViewRendersLineCanvas = true);
    }

    [Fact]
    public void Setting_Thickness_Changes_Parent_Bounds ()
    {
        var parent = new View { Width = 10, Height = 10 };
        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.ContentArea);

        parent.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rectangle (0, 0, 10, 10), parent.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 8), parent.ContentArea);
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
                                          Assert.Equal (Thickness.Empty, e.PreviousThickness);
                                          Assert.Equal (new Thickness (1, 2, 3, 4), e.Thickness);
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
        view.BeginInit();
        view.EndInit();

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
}
