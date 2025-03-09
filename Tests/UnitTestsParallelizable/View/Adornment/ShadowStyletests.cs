namespace Terminal.Gui.ViewTests;

public class ShadowStyleTests
{
    [Fact]
    public void Default_None ()
    {
        var view = new View ();
        Assert.Equal (ShadowStyle.None, view.ShadowStyle);
        Assert.Equal (ShadowStyle.None, view.Margin!.ShadowStyle);
        view.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyle.None)]
    [InlineData (ShadowStyle.Opaque)]
    [InlineData (ShadowStyle.Transparent)]
    public void Set_View_Sets_Margin (ShadowStyle style)
    {
        var view = new View ();

        view.ShadowStyle = style;
        Assert.Equal (style, view.ShadowStyle);
        Assert.Equal (style, view.Margin!.ShadowStyle);
        view.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyle.None, 0, 0, 0, 0)]
    [InlineData (ShadowStyle.Opaque, 1, 0, 0, 1)]
    [InlineData (ShadowStyle.Transparent, 1, 0, 0, 1)]
    public void ShadowStyle_Button1Pressed_Causes_Movement (ShadowStyle style, int expectedLeft, int expectedTop, int expectedRight, int expectedBottom)
    {
        var superView = new View
        {
            Height = 10, Width = 10
        };

        View view = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "0123",
            HighlightStyle = HighlightStyle.Pressed,
            ShadowStyle = style,
            CanFocus = true
        };

        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        Thickness origThickness = view.Margin!.Thickness;
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed, Position = new (0, 0) });
        Assert.Equal (new (expectedLeft, expectedTop, expectedRight, expectedBottom), view.Margin.Thickness);

        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Released, Position = new (0, 0) });
        Assert.Equal (origThickness, view.Margin.Thickness);
    }

    [Theory]
    [InlineData (ShadowStyle.None, 0, 0, 0, 0)]
    [InlineData (ShadowStyle.Opaque, 0, 0, 1, 1)]
    [InlineData (ShadowStyle.Transparent, 0, 0, 1, 1)]
    public void ShadowStyle_Margin_Thickness (ShadowStyle style, int expectedLeft, int expectedTop, int expectedRight, int expectedBottom)
    {
        var superView = new View
        {
            Height = 10, Width = 10
        };

        View view = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "0123",
            HighlightStyle = HighlightStyle.Pressed,
            ShadowStyle = style,
            CanFocus = true
        };

        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (new (expectedLeft, expectedTop, expectedRight, expectedBottom), view.Margin.Thickness);
    }


    [Theory]
    [InlineData (ShadowStyle.None, 3)]
    [InlineData (ShadowStyle.Opaque, 4)]
    [InlineData (ShadowStyle.Transparent, 4)]
    public void Style_Changes_Margin_Thickness (ShadowStyle style, int expected)
    {
        var view = new View ();
        view.Margin!.Thickness = new (3);
        view.ShadowStyle = style;
        Assert.Equal (new (3, 3, expected, expected), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyle.None;
        Assert.Equal (new (3), view.Margin.Thickness);
        view.Dispose ();
    }

}
