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
            HighlightStates = MouseState.Pressed,
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
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (5, 3);

        // Force 16-bit colors off to get predictable RGB output
        app.Driver.Force16Colors = false;

        var superView = new Runnable
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = "ABC".Repeat (40)!
        };
        superView.SetScheme (new (new Attribute (Color.White, Color.Blue)));
        superView.TextFormatter.WordWrap = true;

        // Create an overlapped view with transparent shadow
        var overlappedView = new View
        {
            Width = 4,
            Height = 2,
            Text = "123",
            Arrangement = ViewArrangement.Overlapped,
            ShadowStyle = ShadowStyle.Transparent
        };
        overlappedView.SetScheme (new (new Attribute (Color.Black, Color.Green)));

        superView.Add (overlappedView);

        // Act
        SessionToken? token = app.Begin (superView);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        // Assert
        _output.WriteLine ("Actual driver contents:");
        _output.WriteLine (app.Driver.ToString ());
        _output.WriteLine ("\nActual driver output:");
        string? output = app.Driver.GetOutput ().GetLastOutput ();
        _output.WriteLine (output);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[38;2;0;0;0m\x1b[48;2;0;128;0m123\x1b[38;2;0;0;0m\x1b[48;2;189;189;189mA\x1b[38;2;0;0;255m\x1b[48;2;255;255;255mBC\x1b[38;2;0;0;0m\x1b[48;2;189;189;189mABC\x1b[38;2;0;0;255m\x1b[48;2;255;255;255mABCABC
                                           """, _output, app.Driver);

        // The output should contain ANSI color codes for the transparent shadow
        // which will have dimmed colors compared to the original
        Assert.Contains ("\x1b[38;2;", output); // Should have RGB foreground color codes
        Assert.Contains ("\x1b[48;2;", output); // Should have RGB background color codes

        // Verify driver contents show the background text in shadow areas
        int shadowX = overlappedView.Frame.X + overlappedView.Frame.Width;
        int shadowY = overlappedView.Frame.Y + overlappedView.Frame.Height;

        Cell shadowCell = app.Driver.Contents! [shadowY, shadowX];
        _output.WriteLine ($"\nShadow cell at [{shadowY},{shadowX}]: Grapheme='{shadowCell.Grapheme}', Attr={shadowCell.Attribute}");

        // The grapheme should be from background text
        Assert.NotEqual (string.Empty, shadowCell.Grapheme);
        Assert.Contains (shadowCell.Grapheme, "ABC"); // Should be one of the background characters

        // Cleanup
        if (token is { })
        {
            app.End (token);
        }

        superView.Dispose ();
        app.Dispose ();
    }

}
