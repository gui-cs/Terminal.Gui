using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AdornmentTests
{
    private readonly ITestOutputHelper _output;
    public AdornmentTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void BoundsToScreen_All_Adornments_With_Thickness ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };
        parent.Margin.Thickness = new Thickness (1);
        parent.Border.Thickness = new Thickness (1);
        parent.Padding.Thickness = new Thickness (1);

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rect (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rect (0, 0, 4, 4), parent.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rect (1, 1, 8, 8), parent.Margin.Bounds);
        Assert.Equal (new Rect (1, 1, 8, 8), parent.Border.Frame);
        Assert.Equal (new Rect (1, 1, 6, 6), parent.Border.Bounds);
        Assert.Equal (new Rect (2, 2, 6, 6), parent.Padding.Frame);
        Assert.Equal (new Rect (1, 1, 4, 4), parent.Padding.Bounds);

        Assert.Null (parent.Margin.SuperView);
        Rect boundsAsScreen = parent.BoundsToScreen (parent.Bounds);
        Assert.Equal (new Rect (4, 5, 4, 4), boundsAsScreen);
        boundsAsScreen = parent.Margin.BoundsToScreen (parent.Margin.Bounds);
        Assert.Equal (new Rect (2, 3, 8, 8), boundsAsScreen);
        boundsAsScreen = parent.Border.BoundsToScreen (parent.Border.Bounds);
        Assert.Equal (new Rect (2, 3, 6, 6), boundsAsScreen);
        boundsAsScreen = parent.Padding.BoundsToScreen (parent.Padding.Bounds);
        Assert.Equal (new Rect (2, 3, 4, 4), boundsAsScreen);
    }

    [Fact]
    public void BoundsToScreen_Uses_Parent_Not_SuperView ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rect (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Border.Frame);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Border.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Padding.Frame);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Padding.Bounds);

        Assert.Null (parent.Margin.SuperView);
        Rect boundsAsScreen = parent.BoundsToScreen (new Rect (1, 2, 5, 5));
        Assert.Equal (new Rect (2, 4, 5, 5), boundsAsScreen);
        boundsAsScreen = parent.Margin.BoundsToScreen (new Rect (1, 2, 5, 5));
        Assert.Equal (new Rect (2, 4, 5, 5), boundsAsScreen);
        boundsAsScreen = parent.Border.BoundsToScreen (new Rect (1, 2, 5, 5));
        Assert.Equal (new Rect (2, 4, 5, 5), boundsAsScreen);
        boundsAsScreen = parent.Padding.BoundsToScreen (new Rect (1, 2, 5, 5));
        Assert.Equal (new Rect (2, 4, 5, 5), boundsAsScreen);
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
        Assert.Equal ("(0,0,10,9)", parent.Margin.Frame.ToString ());
        Assert.Equal ("(1,1,8,7)", parent.Border.Frame.ToString ());
        Assert.Equal ("(2,2,6,5)", parent.Padding.Frame.ToString ());
        Assert.Equal ("(5,1,10,9)", parent.Frame.ToString ());
        Assert.Equal ("(1,1,8,7)", parent.Margin.Bounds.ToString ());
        Assert.Equal ("(1,1,6,5)", parent.Border.Bounds.ToString ());
        Assert.Equal ("(1,1,4,3)", parent.Padding.Bounds.ToString ());
        Assert.Equal ("(0,0,4,3)", parent.Bounds.ToString ());
        Assert.Equal ("(1,1,8,7)", parent.Margin.ContentArea.ToString ());
        Assert.Equal ("(1,1,6,5)", parent.Border.ContentArea.ToString ());
        Assert.Equal ("(1,1,4,3)", parent.Padding.ContentArea.ToString ());
        Assert.Equal ("(0,0,4,3)", parent.ContentArea.ToString ());

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
        Assert.Equal ("(0,0,20,11)", top.Margin.Frame.ToString ());
        Assert.Equal ("(0,0,20,11)", top.Border.Frame.ToString ());
        Assert.Equal ("(1,1,18,9)", top.Padding.Frame.ToString ());
        Assert.Equal ("(0,0,20,11)", top.Frame.ToString ());
        Assert.Equal ("(0,0,10,9)", parent.Margin.Frame.ToString ());
        Assert.Equal ("(1,1,8,7)", parent.Border.Frame.ToString ());
        Assert.Equal ("(2,2,6,5)", parent.Padding.Frame.ToString ());
        Assert.Equal ("(4,0,10,9)", parent.Frame.ToString ());
        Assert.Equal ("(1,1,8,7)", parent.Margin.Bounds.ToString ());
        Assert.Equal ("(1,1,6,5)", parent.Border.Bounds.ToString ());
        Assert.Equal ("(1,1,4,3)", parent.Padding.Bounds.ToString ());
        Assert.Equal ("(0,0,4,3)", parent.Bounds.ToString ());
        Assert.Equal ("(1,1,8,7)", parent.Margin.ContentArea.ToString ());
        Assert.Equal ("(1,1,6,5)", parent.Border.ContentArea.ToString ());
        Assert.Equal ("(1,1,4,3)", parent.Padding.ContentArea.ToString ());
        Assert.Equal ("(0,0,4,3)", parent.ContentArea.ToString ());

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
    public void FrameToScreen_All_Adornments_With_Thickness ()
    {
        var parent = new View { X = 1, Y = 2, Width = 10, Height = 10 };
        parent.Margin.Thickness = new Thickness (1);
        parent.Border.Thickness = new Thickness (1);
        parent.Padding.Thickness = new Thickness (1);

        parent.BeginInit ();
        parent.EndInit ();

        Assert.Equal (new Rect (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rect (0, 0, 4, 4), parent.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rect (1, 1, 8, 8), parent.Margin.Bounds);
        Assert.Equal (new Rect (1, 1, 8, 8), parent.Border.Frame);
        Assert.Equal (new Rect (1, 1, 6, 6), parent.Border.Bounds);
        Assert.Equal (new Rect (2, 2, 6, 6), parent.Padding.Frame);
        Assert.Equal (new Rect (1, 1, 4, 4), parent.Padding.Bounds);

        Assert.Null (parent.Margin.SuperView);
        Assert.Equal (new Rect (1, 2, 10, 10), parent.FrameToScreen ());
        Assert.Equal (new Rect (1, 2, 10, 10), parent.Margin.FrameToScreen ());
        Assert.Equal (new Rect (2, 3, 8, 8), parent.Border.FrameToScreen ());
        Assert.Equal (new Rect (3, 4, 6, 6), parent.Padding.FrameToScreen ());
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

        Assert.Equal (new Rect (0, 0, 12, 12), top.Frame);
        Assert.Equal (new Rect (0, 0, 12, 12), top.Bounds);
        Assert.Equal (new Rect (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rect (0, 0, 4, 4), parent.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rect (1, 1, 8, 8), parent.Margin.Bounds);
        Assert.Equal (new Rect (1, 1, 8, 8), parent.Border.Frame);
        Assert.Equal (new Rect (1, 1, 6, 6), parent.Border.Bounds);
        Assert.Equal (new Rect (2, 2, 6, 6), parent.Padding.Frame);
        Assert.Equal (new Rect (1, 1, 4, 4), parent.Padding.Bounds);

        Assert.Null (parent.Margin.SuperView);
        Assert.Equal (new Rect (0, 0, 12, 12), top.FrameToScreen ());
        Assert.Equal (new Rect (1, 2, 10, 10), parent.FrameToScreen ());
        Assert.Equal (new Rect (1, 2, 10, 10), parent.Margin.FrameToScreen ());
        Assert.Equal (new Rect (2, 3, 8, 8), parent.Border.FrameToScreen ());
        Assert.Equal (new Rect (3, 4, 6, 6), parent.Padding.FrameToScreen ());
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
    [InlineData (1, 2, "Padding", false)]
    [InlineData (1, 3, "Padding", false)]
    [InlineData (2, 3, "Padding", false)]
    [InlineData (3, 4, "Padding", true)]
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

        Assert.Equal (new Rect (1, 2, 10, 10), parent.Frame);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Frame);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Margin.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Border.Frame);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Border.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Padding.Frame);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Padding.Bounds);

        Assert.Null (parent.Margin.SuperView);
        Assert.Equal (new Rect (1, 2, 10, 10), parent.FrameToScreen ());
        Assert.Equal (new Rect (1, 2, 10, 10), parent.Margin.FrameToScreen ());
        Assert.Equal (new Rect (1, 2, 10, 10), parent.Border.FrameToScreen ());
        Assert.Equal (new Rect (1, 2, 10, 10), parent.Padding.FrameToScreen ());
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
    public void Setting_Bounds_Throws ()
    {
        var adornment = new Adornment (null);
        Assert.Throws<InvalidOperationException> (() => adornment.Bounds = new Rect (1, 2, 3, 4));
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

        Assert.Equal (new Rect (0, 0, 10, 10), parent.Frame);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Bounds);

        parent.Margin.Thickness = new Thickness (1);
        Assert.Equal (new Rect (0, 0, 10, 10), parent.Frame);
        Assert.Equal (new Rect (0, 0, 8, 8), parent.Bounds);
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
}
