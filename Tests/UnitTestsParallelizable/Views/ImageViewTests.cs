namespace ViewsTests;

/// <summary>
///     Unit tests for <see cref="ImageView"/>.
/// </summary>
public class ImageViewTests
{
    #region Construction and Defaults

    [Fact]
    public void Defaults_AreExpected ()
    {
        ImageView imageView = new ();

        Assert.Null (imageView.Image);
        Assert.True (imageView.UseSixel);
        Assert.Null (imageView.SixelEncoder);
        Assert.False (imageView.IsUsingSixel); // No driver, so sixel not available

        imageView.Dispose ();
    }

    #endregion Construction and Defaults

    #region Image Property

    [Fact]
    public void Image_Set_SetsNeedsDraw ()
    {
        ImageView imageView = new () { Width = 10, Height = 10 };
        View host = new () { Width = 10, Height = 10 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        imageView.ClearNeedsDraw ();
        Assert.False (imageView.NeedsDraw);

        Color [,] pixels = CreateSolidImage (5, 5, new Color (255, 0, 0));
        imageView.Image = pixels;

        Assert.True (imageView.NeedsDraw);

        host.Dispose ();
    }

    [Fact]
    public void Image_SetNull_ClearsState ()
    {
        ImageView imageView = new ();

        Color [,] pixels = CreateSolidImage (5, 5, new Color (255, 0, 0));
        imageView.Image = pixels;
        Assert.NotNull (imageView.Image);

        imageView.Image = null;
        Assert.Null (imageView.Image);

        imageView.Dispose ();
    }

    [Fact]
    public void Image_Set_ClearsCachedScaledImage ()
    {
        ImageView imageView = new () { Width = 10, Height = 10, UseSixel = false };
        View host = new () { Width = 10, Height = 10 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Set an initial image
        Color [,] pixels1 = CreateSolidImage (5, 5, new Color (255, 0, 0));
        imageView.Image = pixels1;

        // Set a different image — should clear cached scaled image
        Color [,] pixels2 = CreateSolidImage (3, 3, new Color (0, 255, 0));
        imageView.Image = pixels2;

        Assert.Same (pixels2, imageView.Image);

        host.Dispose ();
    }

    #endregion Image Property

    #region UseSixel Property

    [Fact]
    public void UseSixel_DefaultsToTrue ()
    {
        ImageView imageView = new ();
        Assert.True (imageView.UseSixel);
        imageView.Dispose ();
    }

    [Fact]
    public void IsUsingSixel_FalseWhenUseSixelFalse ()
    {
        ImageView imageView = new () { UseSixel = false };
        Assert.False (imageView.IsUsingSixel);
        imageView.Dispose ();
    }

    [Fact]
    public void IsUsingSixel_FalseWhenNoDriver ()
    {
        ImageView imageView = new () { UseSixel = true };

        // No App/Driver available, so sixel shouldn't be active
        Assert.False (imageView.IsUsingSixel);
        imageView.Dispose ();
    }

    #endregion UseSixel Property

    #region SixelEncoder Property

    [Fact]
    public void SixelEncoder_DefaultsToNull ()
    {
        ImageView imageView = new ();
        Assert.Null (imageView.SixelEncoder);
        imageView.Dispose ();
    }

    [Fact]
    public void SixelEncoder_CanBeSet ()
    {
        SixelEncoder encoder = new () { AvoidBottomScroll = true };
        ImageView imageView = new () { SixelEncoder = encoder };

        Assert.Same (encoder, imageView.SixelEncoder);

        imageView.Dispose ();
    }

    #endregion SixelEncoder Property

    #region ScaleNearestNeighbor

    [Fact]
    public void ScaleNearestNeighbor_IdentityScale_PreservesPixels ()
    {
        Color [,] source = CreateGradientImage (4, 4);
        Color [,] destination = new Color [4, 4];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (4, destination.GetLength (0));
        Assert.Equal (4, destination.GetLength (1));

        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                Assert.Equal (source [x, y], destination [x, y]);
            }
        }
    }

    [Fact]
    public void ScaleNearestNeighbor_Upscale_CorrectDimensions ()
    {
        Color [,] source = CreateSolidImage (2, 2, new Color (100, 100, 100));
        Color [,] destination = new Color [6, 6];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (6, destination.GetLength (0));
        Assert.Equal (6, destination.GetLength (1));

        // All pixels should be the same color since source is solid
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                Assert.Equal (new Color (100, 100, 100), destination [x, y]);
            }
        }
    }

    [Fact]
    public void ScaleNearestNeighbor_Downscale_CorrectDimensions ()
    {
        Color [,] source = CreateSolidImage (10, 10, new Color (50, 50, 50));
        Color [,] destination = new Color [3, 3];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (3, destination.GetLength (0));
        Assert.Equal (3, destination.GetLength (1));

        // All pixels should still be solid
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                Assert.Equal (new Color (50, 50, 50), destination [x, y]);
            }
        }
    }

    [Fact]
    public void ScaleNearestNeighbor_1x1_Target_ProducesOnePixel ()
    {
        Color [,] source = CreateGradientImage (10, 10);
        Color [,] destination = new Color [1, 1];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (1, destination.GetLength (0));
        Assert.Equal (1, destination.GetLength (1));

        // Should be the top-left pixel (nearest neighbor from 0,0)
        Assert.Equal (source [0, 0], destination [0, 0]);
    }

    [Fact]
    public void ScaleNearestNeighbor_NonSquare_ScalesCorrectly ()
    {
        Color [,] source = CreateSolidImage (4, 2, new Color (200, 100, 50));
        Color [,] destination = new Color [8, 4];
        ImageView.ScaleNearestNeighbor (source, destination);

        Assert.Equal (8, destination.GetLength (0));
        Assert.Equal (4, destination.GetLength (1));

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                Assert.Equal (new Color (200, 100, 50), destination [x, y]);
            }
        }
    }

    #endregion ScaleNearestNeighbor

    #region Cell-Based Rendering

    [Fact]
    public void CellBasedRendering_DrawsBackgroundColors ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        ImageView imageView = new ()
        {
            Width = 4,
            Height = 2,
            UseSixel = false
        };

        Color red = new (255, 0, 0);
        imageView.Image = CreateSolidImage (4, 2, red);

        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Verify the cells have background color set to red (spaces with red background)
        Cell [,]? contents = app.Driver!.Contents;
        Assert.NotNull (contents);

        // Check that the first cell in the image area has the correct background color
        Attribute attr = contents! [0, 0].Attribute!.Value;
        Assert.Equal (red, attr.Background);

        runnable.Dispose ();
    }

    [Fact]
    public void CellBasedRendering_WithNullImage_DrawsNothing ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 10, Height = 10 };
        app.Begin (runnable);

        ImageView imageView = new ()
        {
            Width = 4,
            Height = 2,
            UseSixel = false
        };

        // Image is null — should not throw
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        runnable.Dispose ();
    }

    [Fact]
    public void CellBasedRendering_ScalesImageToViewport ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 20, Height = 20 };
        app.Begin (runnable);

        // Create a 10x10 image but display in a 5x5 viewport
        ImageView imageView = new ()
        {
            Width = 5,
            Height = 5,
            UseSixel = false
        };

        Color blue = new (0, 0, 255);
        imageView.Image = CreateSolidImage (10, 10, blue);

        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Verify cells have the blue background (image was scaled down)
        Cell [,]? contents = app.Driver!.Contents;
        Assert.NotNull (contents);
        Assert.Equal (blue, contents! [0, 0].Attribute!.Value.Background);

        runnable.Dispose ();
    }

    #endregion Cell-Based Rendering

    #region Dispose

    [Fact]
    public void Dispose_CleansUpSixelData ()
    {
        ImageView imageView = new ();
        Color [,] pixels = CreateSolidImage (5, 5, new Color (128, 128, 128));
        imageView.Image = pixels;

        // Should not throw
        imageView.Dispose ();

        // Accessing after dispose is a code smell but should not crash
        Assert.NotNull (imageView.Image); // Still set; just disposed
    }

    [Fact]
    public void Dispose_WithNoImage_DoesNotThrow ()
    {
        ImageView imageView = new ();
        imageView.Dispose (); // Should not throw
    }

    #endregion Dispose

    #region IDesignable

    [Fact]
    public void EnableForDesign_SetsTestImage ()
    {
        ImageView imageView = new ();
        bool result = ((IDesignable)imageView).EnableForDesign ();

        Assert.True (result);
        Assert.NotNull (imageView.Image);
        Assert.Equal (20, imageView.Image!.GetLength (0)); // width
        Assert.Equal (10, imageView.Image.GetLength (1));  // height

        imageView.Dispose ();
    }

    [Fact]
    public void EnableForDesign_CreatesGradientImage ()
    {
        ImageView imageView = new ();
        ((IDesignable)imageView).EnableForDesign ();

        Color [,] image = imageView.Image!;

        // Top-left should be (0, 0, 128)
        Assert.Equal (new Color (0, 0, 128), image [0, 0]);

        // Bottom-right should be (255, 255, 128)
        Assert.Equal (new Color (255, 255, 128), image [19, 9]);

        imageView.Dispose ();
    }

    #endregion IDesignable

    #region SixelToRender IsDirty Flag

    [Fact]
    public void SixelToRender_IsDirty_DefaultsToTrue ()
    {
        SixelToRender sixel = new ();
        Assert.True (sixel.IsDirty);
    }

    [Fact]
    public void SixelToRender_IsDirty_CanBeSetToFalse ()
    {
        SixelToRender sixel = new () { IsDirty = false };
        Assert.False (sixel.IsDirty);
    }

    [Fact]
    public void SixelToRender_AlwaysRender_DefaultsToFalse ()
    {
        SixelToRender sixel = new ();
        Assert.False (sixel.AlwaysRender);
    }

    #endregion SixelToRender IsDirty Flag

    #region ViewportToScreenInPixels
    [Fact]
    public void ViewportToScreenInPixels_Throws_WhenNoSixelSupport ()
    {
        ImageView imageView = new () { Width = 10, Height = 5 };
        View host = new () { Width = 20, Height = 20 };
        host.Add (imageView);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // No App/Driver — should throw
        Assert.Throws<InvalidOperationException> (() => imageView.ViewportToScreenInPixels ());

        host.Dispose ();
    }

    [Fact]
    public void ViewportToScreenInPixels_ReturnsCorrectPixelRect ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        // Configure sixel support with 10x20 pixel resolution per cell
        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        ImageView imageView = new () { Width = 8, Height = 4, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Rectangle pixelRect = imageView.ViewportToScreenInPixels ();

        // Width: 8 cells * 10 px/cell = 80 px
        Assert.Equal (80, pixelRect.Width);

        // Height: 4 cells * 20 px/cell = 80 px (via GetHeightInPixels)
        Assert.Equal (80, pixelRect.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void ViewportToScreenInPixels_AccountsForViewPosition ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        ImageView imageView = new () { X = 3, Y = 2, Width = 5, Height = 3, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Rectangle pixelRect = imageView.ViewportToScreenInPixels ();

        // X: 3 cells * 10 px/cell = 30 px
        Assert.Equal (30, pixelRect.X);

        // Y: 2 cells * 20 px/cell = 40 px
        Assert.Equal (40, pixelRect.Y);

        // Width: 5 cells * 10 px/cell = 50 px
        Assert.Equal (50, pixelRect.Width);

        // Height: 3 cells * 20 px/cell = 60 px
        Assert.Equal (60, pixelRect.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void ViewportToScreenInPixels_DifferentResolution ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        // Use a non-default resolution (8x16)
        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (8, 16) });

        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Rectangle pixelRect = imageView.ViewportToScreenInPixels ();

        // Width: 10 cells * 8 px/cell = 80 px
        Assert.Equal (80, pixelRect.Width);

        // Height: 5 cells * 16 px/cell = 80 px
        Assert.Equal (80, pixelRect.Height);

        runnable.Dispose ();
    }

    #endregion ViewportToScreenInPixels

    #region FitImageInViewportInPixels

    [Fact]
    public void FitImageInViewportInPixels_ZeroSizeImage_ReturnsEmpty ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        Size result = imageView.FitImageInViewportInPixels (new Size (0, 0));
        Assert.Equal (Size.Empty, result);

        result = imageView.FitImageInViewportInPixels (new Size (100, 0));
        Assert.Equal (Size.Empty, result);

        result = imageView.FitImageInViewportInPixels (new Size (0, 100));
        Assert.Equal (Size.Empty, result);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_ImageFitsExactly ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells * 10 px = 100 px wide, 5 cells * 20 px = 100 px tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Image is exactly the viewport pixel size
        Size result = imageView.FitImageInViewportInPixels (new Size (100, 100));

        Assert.Equal (100, result.Width);
        Assert.Equal (100, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_WideImage_ScalesToFitWidth ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells * 10 px = 100 px wide, 5 cells * 20 px = 100 px tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Image is 200x100 (2:1 aspect) — width-constrained
        // Scale = min(100/200, 100/100) = min(0.5, 1.0) = 0.5
        // Result: 200*0.5 = 100 wide, 100*0.5 = 50 tall
        Size result = imageView.FitImageInViewportInPixels (new Size (200, 100));

        Assert.Equal (100, result.Width);
        Assert.Equal (50, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_TallImage_ScalesToFitHeight ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells * 10 px = 100 px wide, 5 cells * 20 px = 100 px tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Image is 100x200 (1:2 aspect) — height-constrained
        // Scale = min(100/100, 100/200) = min(1.0, 0.5) = 0.5
        // Result: 100*0.5 = 50 wide, 200*0.5 = 100 tall
        Size result = imageView.FitImageInViewportInPixels (new Size (100, 200));

        Assert.Equal (50, result.Width);
        Assert.Equal (100, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_SmallImage_ScalesUp ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // Viewport: 10 cells * 10 px = 100 px wide, 5 cells * 20 px = 100 px tall
        ImageView imageView = new () { Width = 10, Height = 5, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Image is 10x20 (1:2 aspect) — height-constrained
        // Scale = min(100/10, 100/20) = min(10, 5) = 5
        // Result: 10*5 = 50 wide, 20*5 = 100 tall
        Size result = imageView.FitImageInViewportInPixels (new Size (10, 20));

        Assert.Equal (50, result.Width);
        Assert.Equal (100, result.Height);

        runnable.Dispose ();
    }

    [Fact]
    public void FitImageInViewportInPixels_VerySmallImage_ClampsToMinimumOne ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable runnable = new () { Width = 40, Height = 20 };
        app.Begin (runnable);

        DriverImpl driver = (DriverImpl)app.Driver!;
        driver.SetSixelSupport (new SixelSupportResult { IsSupported = true, Resolution = new Size (10, 20) });

        // 1x1 viewport = 10x20 pixel viewport
        ImageView imageView = new () { Width = 1, Height = 1, SixelEncoder = new SixelEncoder () };
        runnable.Add (imageView);
        app.LayoutAndDraw ();

        // Even with a 1000x1 image scaled to fit 10x20, width and height should be >= 1
        Size result = imageView.FitImageInViewportInPixels (new Size (1000, 1));

        Assert.True (result.Width >= 1);
        Assert.True (result.Height >= 1);

        runnable.Dispose ();
    }

    #endregion FitImageInViewportInPixels

    #region Helper Methods

    /// <summary>Creates a solid-color image of the specified dimensions.</summary>
    private static Color [,] CreateSolidImage (int width, int height, Color color)
    {
        Color [,] image = new Color [width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                image [x, y] = color;
            }
        }

        return image;
    }

    /// <summary>Creates a gradient image where pixel color varies by position.</summary>
    private static Color [,] CreateGradientImage (int width, int height)
    {
        Color [,] image = new Color [width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                byte r = (byte)(x * 255 / Math.Max (1, width - 1));
                byte g = (byte)(y * 255 / Math.Max (1, height - 1));
                image [x, y] = new Color (r, g, 128);
            }
        }

        return image;
    }

    #endregion Helper Methods
}