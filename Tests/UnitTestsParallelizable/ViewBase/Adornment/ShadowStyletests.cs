using UnitTests;
using Xunit.Abstractions;

namespace ViewBaseTests.Adornments;

[Collection ("Global Test Setup")]

public class ShadowStyleTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

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
            MouseHighlightStates = MouseState.Pressed,
            ShadowStyle = style,
            CanFocus = true
        };

        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (new (expectedLeft, expectedTop, expectedRight, expectedBottom), view.Margin!.Thickness);
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

    [Fact]
    public void TransparentShadow_Draws_Transparent_At_Driver_Output ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (2, 1);
        app.Driver.Force16Colors = true;

        using Runnable superView = new ();
        superView.Width = Dim.Fill ();
        superView.Height = Dim.Fill ();
        superView.Text = "AB";
        superView.TextFormatter.WordWrap = true;
        superView.SetScheme (new (new Attribute (Color.Black, Color.White)));

        // Create view with transparent shadow
        View viewWithShadow = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "*",
            ShadowStyle = ShadowStyle.Transparent
        };
        // Make it so the margin is only on the right for simplicity
        viewWithShadow.Margin!.Thickness = new (0, 0, 1, 0);
        viewWithShadow.SetScheme (new (new Attribute (Color.Black, Color.White)));

        superView.Add (viewWithShadow);

        // Act
        app.Begin (superView);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        // Assert
        _output.WriteLine ("Actual driver contents:");
        _output.WriteLine (app.Driver.ToString ());
        _output.WriteLine ("\nActual driver output:");
        string? output = app.Driver.GetOutput ().GetLastOutput ();
        _output.WriteLine (output);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[30m\x1b[107m*\x1b[90m\x1b[100mB
                                           """, _output, app.Driver);

    }

}
