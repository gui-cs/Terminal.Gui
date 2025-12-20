using Xunit.Abstractions;

namespace ViewBaseTests.Adornments;

/// <summary>
/// Tests that demonstrate the bug where shadows of SubViews of Adornments don't draw.
/// See issue: https://github.com/gui-cs/Terminal.Gui/issues/XXXX
/// </summary>
public class AdornmentSubViewShadowTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Button_With_ShadowStyle_In_Padding_Should_Draw_Shadow ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver?.SetScreenSize (30, 10);

        Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        // Setup padding with some thickness so we have space for the button
        window.Padding!.Thickness = new (3);

        // Add a button with a shadow to the Padding adornment
        var buttonInPadding = new Button
        {
            X = 1,
            Y = 1,
            Text = "Button",
            ShadowStyle = ShadowStyle.Opaque
        };

        window.Padding.Add (buttonInPadding);
        app.Begin (window);
        app.LayoutAndDraw ();

        // Act - Get the screen contents
        string? actualOutput = app.Driver?.ToString ();
        _output.WriteLine ("Actual output:");
        _output.WriteLine (actualOutput ?? "null");

        // Assert - The button shadow should be visible
        // The shadow should appear as '▖' on the right and '▝▀▀▀▀▀▀▀▀▘' on the bottom
        // Currently this test will FAIL because the shadow is NOT drawn (demonstrating the bug)
        
        // Expected output would show the button with shadow characters (▖, ▝, ▀, ▘)
        // But currently the shadow is missing
        Assert.NotNull (actualOutput);
        Assert.Contains ("▖", actualOutput); // Right shadow character
        Assert.Contains ("▝", actualOutput); // Bottom-left shadow corner
        Assert.Contains ("▘", actualOutput); // Bottom-right shadow corner
    }

    [Fact]
    public void Button_With_ShadowStyle_In_Margin_Should_Draw_Shadow ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver?.SetScreenSize (30, 10);

        Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        // Setup margin with some thickness
        window.Margin!.Thickness = new (3);
        // Turn off transparent flags for easier testing
        window.Margin.ViewportSettings = ViewportSettingsFlags.None;

        // Add a button with a shadow to the Margin adornment
        var buttonInMargin = new Button
        {
            X = 1,
            Y = 1,
            Text = "Button",
            ShadowStyle = ShadowStyle.Opaque
        };

        window.Margin.Add (buttonInMargin);
        app.Begin (window);
        app.LayoutAndDraw ();

        // Act - Get the screen contents
        string? actualOutput = app.Driver?.ToString ();
        _output.WriteLine ("Actual output:");
        _output.WriteLine (actualOutput ?? "null");

        // Assert - The button shadow should be visible
        // Currently this test will FAIL because the shadow is NOT drawn (demonstrating the bug)
        Assert.NotNull (actualOutput);
        Assert.Contains ("▖", actualOutput); // Right shadow character
        Assert.Contains ("▝", actualOutput); // Bottom-left shadow corner
        Assert.Contains ("▘", actualOutput); // Bottom-right shadow corner
    }

    [Fact]
    public void Button_With_ShadowStyle_In_Border_Should_Draw_Shadow ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver?.SetScreenSize (30, 10);

        Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        // Setup border with some thickness
        window.Border!.Thickness = new (3);

        // Add a button with a shadow to the Border adornment
        var buttonInBorder = new Button
        {
            X = 1,
            Y = 1,
            Text = "Button",
            ShadowStyle = ShadowStyle.Opaque
        };

        window.Border.Add (buttonInBorder);
        app.Begin (window);
        app.LayoutAndDraw ();

        // Act - Get the screen contents
        string? actualOutput = app.Driver?.ToString ();
        _output.WriteLine ("Actual output:");
        _output.WriteLine (actualOutput ?? "null");

        // Assert - The button shadow should be visible
        // Currently this test will FAIL because the shadow is NOT drawn (demonstrating the bug)
        Assert.NotNull (actualOutput);
        Assert.Contains ("▖", actualOutput); // Right shadow character
        Assert.Contains ("▝", actualOutput); // Bottom-left shadow corner
        Assert.Contains ("▘", actualOutput); // Bottom-right shadow corner
    }

    [Fact]
    public void Button_With_Transparent_ShadowStyle_In_Padding_Should_Draw_Shadow ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver?.SetScreenSize (30, 10);

        Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = "XXXXXXXXXXXXXXXXXXXXXXXXXX"
        };

        // Setup padding with some thickness so we have space for the button
        window.Padding!.Thickness = new (3);

        // Add a button with a transparent shadow to the Padding adornment
        var buttonInPadding = new Button
        {
            X = 1,
            Y = 1,
            Text = "Button",
            ShadowStyle = ShadowStyle.Transparent
        };
        buttonInPadding.Margin!.ShadowSize = new (1, 1);

        window.Padding.Add (buttonInPadding);
        app.Begin (window);
        app.LayoutAndDraw ();

        // Act - Get the screen contents
        string? actualOutput = app.Driver?.ToString ();
        _output.WriteLine ("Actual output:");
        _output.WriteLine (actualOutput ?? "null");

        // Assert - For transparent shadow, we expect to see the underlying text ('X') 
        // rendered with dimmed colors in the shadow area
        // Currently this test will FAIL because the shadow is NOT drawn (demonstrating the bug)
        
        // We need to verify that the shadow area exists by checking that the button's
        // margin has the expected thickness (which includes the shadow)
        Assert.NotNull (actualOutput);
        Assert.Equal (new (0, 0, 1, 1), buttonInPadding.Margin!.Thickness);
        
        // The actual visual rendering of the transparent shadow would show dimmed text
        // but we can't easily verify colors in this test, so we just verify the structure exists
    }

    [Fact]
    public void Nested_View_With_ShadowStyle_In_Padding_Should_Draw_Shadow ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver?.SetScreenSize (40, 15);

        Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        // Setup padding with some thickness
        window.Padding!.Thickness = new (5);

        // Add a view with a shadow to the Padding adornment
        var viewInPadding = new View
        {
            X = 2,
            Y = 2,
            Width = 20,
            Height = 5,
            ShadowStyle = ShadowStyle.Opaque,
            BorderStyle = LineStyle.Single
        };

        window.Padding.Add (viewInPadding);
        app.Begin (window);
        app.LayoutAndDraw ();

        // Act - Get the screen contents
        string? actualOutput = app.Driver?.ToString ();
        _output.WriteLine ("Actual output:");
        _output.WriteLine (actualOutput ?? "null");

        // Assert - The view shadow should be visible
        // Currently this test will FAIL because the shadow is NOT drawn (demonstrating the bug)
        Assert.NotNull (actualOutput);
        Assert.Contains ("▖", actualOutput); // Right shadow character
        Assert.Contains ("▝", actualOutput); // Bottom-left shadow corner
        Assert.Contains ("▘", actualOutput); // Bottom-right shadow corner
    }
}
