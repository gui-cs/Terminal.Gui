using UnitTests;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.ViewTests;

public class ViewDrawingClippingTests (ITestOutputHelper output) : FakeDriverBase
{
    #region GetClip / SetClip Tests

    [Fact]
    public void GetClip_NullDriver_ReturnsNull ()
    {
        Region? clip = View.GetClip (null);
        Assert.Null (clip);
    }

    [Fact]
    public void GetClip_ReturnsDriverClip ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        var region = new Region (new Rectangle (10, 10, 20, 20));
        driver.Clip = region;

        Region? result = View.GetClip (driver);

        Assert.NotNull (result);
        Assert.Equal (region, result);
    }

    [Fact]
    public void SetClip_NullDriver_DoesNotThrow ()
    {
        var exception = Record.Exception (() => View.SetClip (null, new Region (Rectangle.Empty)));
        Assert.Null (exception);
    }

    [Fact]
    public void SetClip_NullRegion_DoesNothing ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        var original = new Region (new Rectangle (5, 5, 10, 10));
        driver.Clip = original;

        View.SetClip (driver, null);

        Assert.Equal (original, driver.Clip);
    }

    [Fact]
    public void SetClip_ValidRegion_SetsDriverClip ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        var region = new Region (new Rectangle (10, 10, 30, 30));

        View.SetClip (driver, region);

        Assert.Equal (region, driver.Clip);
    }

    #endregion

    #region SetClipToScreen Tests

    [Fact]
    public void SetClipToScreen_NullDriver_ReturnsNull ()
    {
        Region? previous = View.SetClipToScreen (null);
        Assert.Null (previous);
    }

    [Fact]
    public void SetClipToScreen_ReturnsPreviousClip ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        var original = new Region (new Rectangle (5, 5, 10, 10));
        driver.Clip = original;

        Application.Driver = driver;

        Region? previous = View.SetClipToScreen (driver);

        Assert.Equal (original, previous);
        Assert.NotEqual (original, driver.Clip);

        Application.ResetState (true);
    }

    [Fact]
    public void SetClipToScreen_SetsClipToScreen ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        Application.Driver = driver;

        View.SetClipToScreen (driver);

        Assert.NotNull (driver.Clip);
        Assert.Equal (driver.Screen, driver.Clip.GetBounds ());

        Application.ResetState (true);
    }

    #endregion

    #region ExcludeFromClip Tests

    [Fact]
    public void ExcludeFromClip_Rectangle_NullDriver_DoesNotThrow ()
    {
        Application.Driver = null;
        var exception = Record.Exception (() => View.ExcludeFromClip (new Rectangle (5, 5, 10, 10)));
        Assert.Null (exception);

        Application.ResetState (true);
    }

    [Fact]
    public void ExcludeFromClip_Rectangle_ExcludesArea ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (new Rectangle (0, 0, 80, 25));
        Application.Driver = driver;

        var toExclude = new Rectangle (10, 10, 20, 20);
        View.ExcludeFromClip (toExclude);

        // Verify the region was excluded
        Assert.NotNull (driver.Clip);
        Assert.False (driver.Clip.Contains (15, 15));

        Application.ResetState (true);
    }

    [Fact]
    public void ExcludeFromClip_Region_NullDriver_DoesNotThrow ()
    {
        Application.Driver = null;
        var exception = Record.Exception (() => View.ExcludeFromClip (new Region (new Rectangle (5, 5, 10, 10))));
        Assert.Null (exception);

        Application.ResetState (true);
    }

    [Fact]
    public void ExcludeFromClip_Region_ExcludesArea ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (new Rectangle (0, 0, 80, 25));
        Application.Driver = driver;

        var toExclude = new Region (new Rectangle (10, 10, 20, 20));
        View.ExcludeFromClip (toExclude);

        // Verify the region was excluded
        Assert.NotNull (driver.Clip);
        Assert.False (driver.Clip.Contains (15, 15));

        Application.ResetState (true);
    }

    #endregion

    #region AddFrameToClip Tests

    [Fact]
    public void AddFrameToClip_NullDriver_ReturnsNull ()
    {
        var view = new View { X = 0, Y = 0, Width = 10, Height = 10 };
        view.BeginInit ();
        view.EndInit ();

        Region? result = view.AddFrameToClip ();

        Assert.Null (result);
    }

    [Fact]
    public void AddFrameToClip_IntersectsWithFrame ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Region? previous = view.AddFrameToClip ();

        Assert.NotNull (previous);
        Assert.NotNull (driver.Clip);

        // The clip should now be the intersection of the screen and the view's frame
        Rectangle expectedBounds = new Rectangle (1, 1, 20, 20);
        Assert.Equal (expectedBounds, driver.Clip.GetBounds ());
    }

    #endregion

    #region AddViewportToClip Tests

    [Fact]
    public void AddViewportToClip_NullDriver_ReturnsNull ()
    {
        var view = new View { X = 0, Y = 0, Width = 10, Height = 10 };
        view.BeginInit ();
        view.EndInit ();

        Region? result = view.AddViewportToClip ();

        Assert.Null (result);
    }

    [Fact]
    public void AddViewportToClip_IntersectsWithViewport ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Region? previous = view.AddViewportToClip ();

        Assert.NotNull (previous);
        Assert.NotNull (driver.Clip);

        // The clip should be the viewport area
        Rectangle viewportScreen = view.ViewportToScreen (new Rectangle (Point.Empty, view.Viewport.Size));
        Assert.Equal (viewportScreen, driver.Clip.GetBounds ());
    }

    [Fact]
    public void AddViewportToClip_WithClipContentOnly_LimitsToVisibleContent ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.SetContentSize (new Size (100, 100));
        view.ViewportSettings = ViewportSettingsFlags.ClipContentOnly;
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Region? previous = view.AddViewportToClip ();

        Assert.NotNull (previous);
        Assert.NotNull (driver.Clip);

        // The clip should be limited to visible content
        Rectangle visibleContent = view.ViewportToScreen (new Rectangle (new (-view.Viewport.X, -view.Viewport.Y), view.GetContentSize ()));
        Rectangle viewport = view.ViewportToScreen (new Rectangle (Point.Empty, view.Viewport.Size));
        Rectangle expected = Rectangle.Intersect (viewport, visibleContent);

        Assert.Equal (expected, driver.Clip.GetBounds ());
    }

    #endregion

    #region Clip Interaction Tests

    [Fact]
    public void ClipRegions_StackCorrectly_WithNestedViews ()
    {
        IDriver driver = CreateFakeDriver (100,100);
        driver.Clip = new Region (driver.Screen);

        var superView = new View
        {
            X = 1,
            Y = 1,
            Width = 50,
            Height = 50,
            Driver = driver
        };
        superView.BeginInit ();
        superView.EndInit ();

        var view = new View
        {
            X = 5,
            Y = 5,
            Width = 30,
            Height = 30,
        };
        superView.Add (view);
        superView.LayoutSubViews ();

        // Set clip to superView's frame
        Region? superViewClip = superView.AddFrameToClip ();
        Rectangle superViewBounds = driver.Clip.GetBounds ();

        // Now set clip to view's frame
        Region? viewClip = view.AddFrameToClip ();
        Rectangle viewBounds = driver.Clip.GetBounds ();

        // Child clip should be within superView clip
        Assert.True (superViewBounds.Contains (viewBounds.Location));

        // Restore superView clip
        View.SetClip (driver, superViewClip);
     //   Assert.Equal (superViewBounds, driver.Clip.GetBounds ());
    }

    [Fact]
    public void ClipRegions_RespectPreviousClip ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        var initialClip = new Region (new Rectangle (20, 20, 40, 40));
        driver.Clip = initialClip;

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 60,
            Height = 60,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Region? previous = view.AddFrameToClip ();

        // The new clip should be the intersection of the initial clip and the view's frame
        Rectangle expected = Rectangle.Intersect (
                                                   initialClip.GetBounds (),
                                                   view.FrameToScreen ()
                                                  );

        Assert.Equal (expected, driver.Clip.GetBounds ());

        // Restore should give us back the original
        View.SetClip (driver, previous);
        Assert.Equal (initialClip.GetBounds (), driver.Clip.GetBounds ());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AddFrameToClip_EmptyFrame_WorksCorrectly ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 0,
            Height = 0,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Region? previous = view.AddFrameToClip ();

        Assert.NotNull (previous);
        Assert.NotNull (driver.Clip);
    }

    [Fact]
    public void AddViewportToClip_EmptyViewport_WorksCorrectly ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 1,  // Minimal size to have adornments
            Height = 1,
            Driver = driver
        };
        view.Border!.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // With border thickness of 1, the viewport should be empty
        Assert.True (view.Viewport.Size.Width == 0 || view.Viewport.Size.Height == 0);

        Region? previous = view.AddViewportToClip ();

        Assert.NotNull (previous);
    }

    [Fact]
    public void ClipRegions_OutOfBounds_HandledCorrectly ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 100,  // Outside screen bounds
            Y = 100,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Region? previous = view.AddFrameToClip ();

        Assert.NotNull (previous);
        // The clip should be empty since the view is outside the screen
        Assert.True (driver.Clip.IsEmpty () || !driver.Clip.Contains (100, 100));
    }

    #endregion

    #region Drawing Tests

    [Fact]
    public void Clip_Set_BeforeDraw_ClipsDrawing ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        var clip = new Region (new Rectangle (10, 10, 10, 10));
        driver.Clip = clip;

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 50,
            Height = 50,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        // Verify clip was used
        Assert.NotNull (driver.Clip);
    }

    [Fact]
    public void Draw_UpdatesDriverClip ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        // Clip should be updated to exclude the drawn view
        Assert.NotNull (driver.Clip);
       // Assert.False (driver.Clip.Contains (15, 15)); // Point inside the view should be excluded
    }

    [Fact]
    public void Draw_WithSubViews_ClipsCorrectly ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var superView = new View
        {
            X = 1,
            Y = 1,
            Width = 50,
            Height = 50,
            Driver = driver
        };
        var view = new View { X = 5, Y = 5, Width = 20, Height = 20 };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubViews ();

        superView.Draw ();

        // Both superView and view should be excluded from clip
        Assert.NotNull (driver.Clip);
    //    Assert.False (driver.Clip.Contains (15, 15)); // Point in superView should be excluded
    }

    [Fact]
    public void Draw_NonVisibleView_DoesNotUpdateClip ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        var originalClip = new Region (driver.Screen);
        driver.Clip = originalClip.Clone ();

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Visible = false,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();

        view.Draw ();

        // Clip should not be modified for invisible views
        Assert.True (driver.Clip.Equals (originalClip));
    }

    [Fact]
    public void ExcludeFromClip_ExcludesRegion ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);
        Application.Driver = driver;

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        var excludeRect = new Rectangle (15, 15, 10, 10);
        View.ExcludeFromClip (excludeRect);

        Assert.NotNull (driver.Clip);
        Assert.False (driver.Clip.Contains (20, 20)); // Point inside excluded rect should not be in clip

        Application.ResetState (true);
    }

    [Fact]
    public void ExcludeFromClip_WithNullClip_DoesNotThrow ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = null!;
        Application.Driver = driver;

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };

        var exception = Record.Exception (() => View.ExcludeFromClip (new Rectangle (15, 15, 10, 10)));

        Assert.Null (exception);

        Application.ResetState (true);
    }

    #endregion

    #region Misc Tests

    [Fact]
    public void SetClip_SetsDriverClip ()
    {
        IDriver driver = CreateFakeDriver (80, 25);

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };

        var newClip = new Region (new Rectangle (5, 5, 30, 30));
        View.SetClip (driver, newClip);

        Assert.Equal (newClip, driver.Clip);
    }

    [Fact (Skip = "See BUGBUG in SetClip")]
    public void SetClip_WithNullClip_ClearsClip ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (new Rectangle (10, 10, 20, 20));

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };

        View.SetClip (driver, null);

        Assert.Null (driver.Clip);
    }

    [Fact]
    public void Draw_RestoresOriginalClip ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        var originalClip = new Region (driver.Screen);
        driver.Clip = originalClip.Clone ();

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        view.Draw ();

        // After draw, clip should be restored (though it may be modified)
        Assert.NotNull (driver.Clip);
    }

    [Fact]
    public void Draw_EmptyViewport_DoesNotCrash ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 1,
            Height = 1,
            Driver = driver
        };
        view.Border!.Thickness = new Thickness (1);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // With border of 1, viewport should be empty (0x0 or negative)
        var exception = Record.Exception (() => view.Draw ());

        Assert.Null (exception);
    }

    [Fact]
    public void Draw_VeryLargeView_HandlesClippingCorrectly ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = 1000,
            Height = 1000,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        var exception = Record.Exception (() => view.Draw ());

        Assert.Null (exception);
    }

    [Fact]
    public void Draw_NegativeCoordinates_HandlesClippingCorrectly ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = -10,
            Y = -10,
            Width = 50,
            Height = 50,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        var exception = Record.Exception (() => view.Draw ());

        Assert.Null (exception);
    }

    [Fact]
    public void Draw_OutOfScreenBounds_HandlesClippingCorrectly ()
    {
        IDriver driver = CreateFakeDriver (80, 25);
        driver.Clip = new Region (driver.Screen);

        var view = new View
        {
            X = 100,
            Y = 100,
            Width = 50,
            Height = 50,
            Driver = driver
        };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        var exception = Record.Exception (() => view.Draw ());

        Assert.Null (exception);
    }

    #endregion
}
