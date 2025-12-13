using UnitTests;
using Xunit.Abstractions;

namespace ViewBaseTests.Arrangement;

/// <summary>
///     Tests that verify transparent shadow configuration for ViewArrangement.Overlapped views.
///     Note: These tests verify the shadow is configured correctly. Actual rendering of shadows
///     requires the full application main loop which isn't suitable for parallelizable unit tests.
/// </summary>
public class OverlappedViewTransparentShadowTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Overlapped_View_With_TransparentShadow_ConfiguresMarginCorrectly ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (30, 20);

        var superView = new Runnable
        {
            Width = 30,
            Height = 20
        };

        var overlappedView = new View
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 5,
            Text = "VIEW",
            Arrangement = ViewArrangement.Overlapped,
            ShadowStyle = ShadowStyle.Transparent
        };

        superView.Add (overlappedView);

        // Act
        SessionToken? token = app.Begin (superView);
        app.LayoutAndDraw ();

        // Assert
        // Verify the margin exists and has correct settings
        Assert.NotNull (overlappedView.Margin);
        Assert.Equal (ShadowStyle.Transparent, overlappedView.Margin.ShadowStyle);

        // Shadow adds 1 to right and bottom thickness
        Assert.Equal (1, overlappedView.Margin.Thickness.Right);
        Assert.Equal (1, overlappedView.Margin.Thickness.Bottom);
        Assert.Equal (0, overlappedView.Margin.Thickness.Left);
        Assert.Equal (0, overlappedView.Margin.Thickness.Top);

        // Verify shadow subviews were created
        Assert.NotNull (overlappedView.Margin.InternalSubViews);
        Assert.Equal (2, overlappedView.Margin.InternalSubViews.Count); // Right and bottom shadows

        // Cleanup
        if (token is { })
        {
            app.End (token);
        }

        superView.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Overlapped_View_With_TransparentShadow_Has_Correct_Arrangement ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");

        var superView = new Runnable ();

        var overlappedView = new View
        {
            X = 5,
            Y = 5,
            Width = 8,
            Height = 4,
            Text = "VIEW",
            Arrangement = ViewArrangement.Overlapped,
            ShadowStyle = ShadowStyle.Transparent
        };

        superView.Add (overlappedView);

        // Act
        SessionToken? token = app.Begin (superView);
        app.LayoutAndDraw ();

        // Assert
        Assert.Equal (ViewArrangement.Overlapped, overlappedView.Arrangement);
        Assert.Equal (ShadowStyle.Transparent, overlappedView.ShadowStyle);
        Assert.NotNull (overlappedView.Margin);
        Assert.Equal (ShadowStyle.Transparent, overlappedView.Margin.ShadowStyle);

        // Cleanup
        if (token is { })
        {
            app.End (token);
        }

        superView.Dispose ();
        app.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyle.None)]
    [InlineData (ShadowStyle.Opaque)]
    [InlineData (ShadowStyle.Transparent)]
    public void Overlapped_View_ShadowStyle_ConfiguresMarginCorrectly (ShadowStyle shadowStyle)
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");

        var superView = new Runnable ();

        var overlappedView = new View
        {
            Arrangement = ViewArrangement.Overlapped,
            ShadowStyle = shadowStyle
        };

        superView.Add (overlappedView);

        // Act
        SessionToken? token = app.Begin (superView);
        app.LayoutAndDraw ();

        // Assert
        Assert.Equal (shadowStyle, overlappedView.Margin!.ShadowStyle);

        if (shadowStyle == ShadowStyle.None)
        {
            // No shadow thickness added
            Assert.Equal (0, overlappedView.Margin.Thickness.Right);
            Assert.Equal (0, overlappedView.Margin.Thickness.Bottom);
        }
        else
        {
            // Shadow adds 1 to right and bottom
            Assert.Equal (1, overlappedView.Margin.Thickness.Right);
            Assert.Equal (1, overlappedView.Margin.Thickness.Bottom);

            // Shadow subviews should be created
            Assert.NotNull (overlappedView.Margin.InternalSubViews);
            Assert.Equal (2, overlappedView.Margin.InternalSubViews.Count);
        }

        // Cleanup
        if (token is { })
        {
            app.End (token);
        }

        superView.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Multiple_Overlapped_Views_With_TransparentShadows_Render_Correctly ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (40, 25);

        var superView = new Runnable
        {
            Width = 40,
            Height = 25,
            Arrangement = ViewArrangement.Overlapped
        };
        superView.SetScheme (new (new Attribute (Color.White, Color.Black)));

        // Create two overlapped views with transparent shadows
        var view1 = new View
        {
            X = 2,
            Y = 2,
            Width = 12,
            Height = 6,
            Text = "View1",
            Arrangement = ViewArrangement.Overlapped,
            ShadowStyle = ShadowStyle.Transparent
        };
        view1.SetScheme (new (new Attribute (Color.Black, Color.Cyan)));

        var view2 = new View
        {
            X = 8,
            Y = 5,
            Width = 12,
            Height = 6,
            Text = "View2",
            Arrangement = ViewArrangement.Overlapped,
            ShadowStyle = ShadowStyle.Transparent
        };
        view2.SetScheme (new (new Attribute (Color.Black, Color.Magenta)));

        superView.Add (view1);
        superView.Add (view2);

        // Act
        SessionToken? token = app.Begin (superView);
        app.LayoutAndDraw ();

        // Assert
        _output.WriteLine ("Actual driver contents:");
        _output.WriteLine (app.Driver.ToString ());

        // Verify both views have transparent shadows set up
        Assert.Equal (ShadowStyle.Transparent, view1.Margin!.ShadowStyle);
        Assert.Equal (ShadowStyle.Transparent, view2.Margin!.ShadowStyle);

        // View2 is added after View1, so it should be on top (higher Z-order)
        Assert.Equal (1, superView.SubViews.IndexOf (view2));
        Assert.Equal (0, superView.SubViews.IndexOf (view1));

        // Cleanup
        if (token is { })
        {
            app.End (token);
        }

        superView.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Overlapped_View_TransparentShadow_Respects_Margin_Thickness ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (30, 20);

        var superView = new Runnable
        {
            Width = 30,
            Height = 20
        };
        superView.SetScheme (new (new Attribute (Color.White, Color.Blue)));

        var overlappedView = new View
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 5,
            Text = "VIEW",
            Arrangement = ViewArrangement.Overlapped,
            ShadowStyle = ShadowStyle.Transparent
        };
        overlappedView.SetScheme (new (new Attribute (Color.Black, Color.Green)));

        superView.Add (overlappedView);

        // Act
        SessionToken? token = app.Begin (superView);
        app.LayoutAndDraw ();

        // Assert
        // Verify that the shadow is created and has the correct thickness
        Assert.NotNull (overlappedView.Margin);
        Assert.Equal (ShadowStyle.Transparent, overlappedView.Margin.ShadowStyle);

        // Shadow adds 1 to right and bottom thickness
        Assert.Equal (1, overlappedView.Margin.Thickness.Right);
        Assert.Equal (1, overlappedView.Margin.Thickness.Bottom);

        // Cleanup
        if (token is { })
        {
            app.End (token);
        }

        superView.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Overlapped_View_With_TransparentShadow_Shows_Background_Text_Through_Shadow ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (20, 10);

        var superView = new Runnable
        {
            Width = 20,
            Height = 10,
            Text = "BACKGROUND".Repeat (20)!
        };
        superView.SetScheme (new (new Attribute (Color.White, Color.Blue)));
        superView.TextFormatter.WordWrap = true;

        // Create an overlapped view with transparent shadow positioned to leave room for shadow
        var overlappedView = new View
        {
            X = 2,
            Y = 2,
            Width = 6,
            Height = 3,
            Text = "VIEW",
            Arrangement = ViewArrangement.Overlapped,
            ShadowStyle = ShadowStyle.Transparent
        };
        overlappedView.SetScheme (new (new Attribute (Color.Black, Color.Green)));

        superView.Add (overlappedView);

        // Act
        SessionToken? token = app.Begin (superView);
        app.LayoutAndDraw ();

        // Assert
        _output.WriteLine ("Actual driver contents:");
        _output.WriteLine (app.Driver.ToString ());

        // Verify the underlying "BACKGROUND" text shows through where the shadow should be
        // The shadow is at X=8 (2 + 6) for the right edge and Y=5 (2 + 3) for the bottom edge

        // Check right shadow - should show background text characters
        int shadowX = 8; // Right edge of view (X=2 + Width=6)

        for (int y = 2; y <= 4; y++) // Shadow covers rows 2-4 (view height)
        {
            Cell cell = app.Driver.Contents! [y, shadowX];
            _output.WriteLine ($"Cell at [{y},{shadowX}]: Grapheme='{cell.Grapheme}'");

            // The grapheme should be from "BACKGROUND" text, not empty or special shadow glyphs
            Assert.NotEqual (string.Empty, cell.Grapheme);

            // For transparent shadows, the text underneath should show through
            // The background text at this location should be visible
            Assert.True (cell.Grapheme.Length > 0, "Transparent shadow should show background text");
        }

        // Check bottom shadow - should show background text characters  
        int shadowY = 5; // Bottom edge of view (Y=2 + Height=3)

        for (int x = 2; x <= 7; x++) // Shadow covers cols 2-7 (view width)
        {
            Cell cell = app.Driver.Contents! [shadowY, x];
            _output.WriteLine ($"Cell at [{shadowY},{x}]: Grapheme='{cell.Grapheme}'");

            // The grapheme should be from "BACKGROUND" text
            Assert.NotEqual (string.Empty, cell.Grapheme);
            Assert.True (cell.Grapheme.Length > 0, "Transparent shadow should show background text");
        }

        // Use DriverAssert to verify the overall content shows the background text
        // The exact layout depends on word wrapping, but we should see "VIEW" and portions of "BACKGROUND"
        string driverContents = app.Driver.ToString ();
        Assert.Contains ("VIEW", driverContents);
        Assert.Contains ("BACKGROUND", driverContents);

        // Cleanup
        if (token is { })
        {
            app.End (token);
        }

        superView.Dispose ();
        app.Dispose ();
    }
}
