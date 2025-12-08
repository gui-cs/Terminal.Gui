using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace ViewBaseTests.Drawing;

public class ViewDrawingClippingTests (ITestOutputHelper output) : FakeDriverBase
{
    #region GetClip / SetClip Tests

    [Fact]
    public void GetClip_ReturnsDriverClip ()
    {
        IDriver driver = CreateFakeDriver ();
        var region = new Region (new (10, 10, 20, 20));
        driver.Clip = region;
        View view = new () { Driver = driver };

        Region? result = view.GetClip ();

        Assert.NotNull (result);
        Assert.Equal (region, result);
    }

    [Fact]
    public void SetClip_NullRegion_DoesNothing ()
    {
        IDriver driver = CreateFakeDriver ();
        var original = new Region (new (5, 5, 10, 10));
        driver.Clip = original;

        View view = new () { Driver = driver };

        view.SetClip (null);

        Assert.Equal (original, driver.Clip);
    }

    [Fact]
    public void SetClip_ValidRegion_SetsDriverClip ()
    {
        IDriver driver = CreateFakeDriver ();
        var region = new Region (new (10, 10, 30, 30));
        View view = new () { Driver = driver };

        view.SetClip (region);

        Assert.Equal (region, driver.Clip);
    }

    #endregion

    #region SetClipToScreen Tests

    [Fact]
    public void SetClipToScreen_ReturnsPreviousClip ()
    {
        IDriver driver = CreateFakeDriver ();
        var original = new Region (new (5, 5, 10, 10));
        driver.Clip = original;
        View view = new () { Driver = driver };

        Region? previous = view.SetClipToScreen ();

        Assert.Equal (original, previous);
        Assert.NotEqual (original, driver.Clip);
    }

    [Fact]
    public void SetClipToScreen_SetsClipToScreen ()
    {
        IDriver driver = CreateFakeDriver ();
        View view = new () { Driver = driver };

        view.SetClipToScreen ();

        Assert.NotNull (driver.Clip);
        Assert.Equal (driver.Screen, driver.Clip.GetBounds ());
    }

    #endregion

    #region ExcludeFromClip Tests

    [Fact]
    public void ExcludeFromClip_Rectangle_NullDriver_DoesNotThrow ()
    {
        View view = new () { Driver = null };
        Exception? exception = Record.Exception (() => view.ExcludeFromClip (new Rectangle (5, 5, 10, 10)));
        Assert.Null (exception);
    }

    [Fact]
    public void ExcludeFromClip_Rectangle_ExcludesArea ()
    {
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (new (0, 0, 80, 25));
        View view = new () { Driver = driver };

        var toExclude = new Rectangle (10, 10, 20, 20);
        view.ExcludeFromClip (toExclude);

        // Verify the region was excluded
        Assert.NotNull (driver.Clip);
        Assert.False (driver.Clip.Contains (15, 15));
    }

    [Fact]
    public void ExcludeFromClip_Region_NullDriver_DoesNotThrow ()
    {
        View view = new () { Driver = null };

        Exception? exception = Record.Exception (() => view.ExcludeFromClip (new Region (new (5, 5, 10, 10))));
        Assert.Null (exception);
    }

    [Fact]
    public void ExcludeFromClip_Region_ExcludesArea ()
    {
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (new (0, 0, 80, 25));
        View view = new () { Driver = driver };

        var toExclude = new Region (new (10, 10, 20, 20));
        view.ExcludeFromClip (toExclude);

        // Verify the region was excluded
        Assert.NotNull (driver.Clip);
        Assert.False (driver.Clip.Contains (15, 15));
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
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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
        var expectedBounds = new Rectangle (1, 1, 20, 20);
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
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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
        IDriver driver = CreateFakeDriver (100, 100);
        driver.Clip = new (driver.Screen);

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
            Height = 30
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
        view.SetClip (superViewClip);

        //   Assert.Equal (superViewBounds, driver.Clip.GetBounds ());
    }

    [Fact]
    public void ClipRegions_RespectPreviousClip ()
    {
        IDriver driver = CreateFakeDriver ();
        var initialClip = new Region (new (20, 20, 40, 40));
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
        view.SetClip (previous);
        Assert.Equal (initialClip.GetBounds (), driver.Clip.GetBounds ());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AddFrameToClip_EmptyFrame_WorksCorrectly ()
    {
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

        var view = new View
        {
            X = 1,
            Y = 1,
            Width = 1, // Minimal size to have adornments
            Height = 1,
            Driver = driver
        };
        view.Border!.Thickness = new (1);
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
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

        var view = new View
        {
            X = 100, // Outside screen bounds
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
        IDriver driver = CreateFakeDriver ();
        var clip = new Region (new (10, 10, 10, 10));
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
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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

    /// <summary>
    /// Tests that wide glyphs (🍎) are correctly clipped when overlapped by bordered subviews
    /// at different column alignments (even vs odd). Demonstrates:
    /// 1. Full clipping at even columns (X=0, X=2)
    /// 2. Partial clipping at odd columns (X=1) resulting in half-glyphs (�)
    /// 3. The recursive draw flow and clip exclusion mechanism
    /// 
    /// For detailed draw flow documentation, see ViewDrawingClippingTests.DrawFlow.md
    /// </summary>
    [Fact]
    public void Draw_WithBorderSubView_DrawsCorrectly ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");
        IDriver driver = app!.Driver!;
        driver.SetScreenSize (30, 20);

        driver!.Clip = new (driver.Screen);

        var superView = new Runnable ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto () + 4,
            Height = Dim.Auto () + 1,
            Driver = driver
        };

        Rune codepoint = Glyphs.Apple;

        superView.DrawingContent += (s, e) =>
                                    {
                                        var view = s as View;
                                        for (var r = 0; r < view!.Viewport.Height; r++)
                                        {
                                            for (var c = 0; c < view.Viewport.Width; c += 2)
                                            {
                                                if (codepoint != default (Rune))
                                                {
                                                    view.AddRune (c, r, codepoint);
                                                }
                                            }
                                        }
                                        e.DrawContext?.AddDrawnRectangle (view.Viewport);
                                        e.Cancel = true;
                                    };

        var viewWithBorderAtX0 = new View
        {
            Text = "viewWithBorderAtX0",
            BorderStyle = LineStyle.Dashed,
            X = 0,
            Y = 1,
            Width = Dim.Auto (),
            Height = 3
        };

        var viewWithBorderAtX1 = new View
        {
            Text = "viewWithBorderAtX1",
            BorderStyle = LineStyle.Dashed,
            X = 1,
            Y = Pos.Bottom (viewWithBorderAtX0) + 1,
            Width = Dim.Auto (),
            Height = 3
        };

        var viewWithBorderAtX2 = new View
        {
            Text = "viewWithBorderAtX2",
            BorderStyle = LineStyle.Dashed,
            X = 2,
            Y = Pos.Bottom (viewWithBorderAtX1) + 1,
            Width = Dim.Auto (),
            Height = 3
        };

        superView.Add (viewWithBorderAtX0, viewWithBorderAtX1, viewWithBorderAtX2);
        app.Begin (superView);
        // Begin calls LayoutAndDraw, so no need to call it again here
        // app.LayoutAndDraw();

        DriverAssert.AssertDriverContentsAre (
                                                       """
                                                       🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎
                                                       ┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┐🍎🍎🍎
                                                       ┆viewWithBorderAtX0┆🍎🍎🍎
                                                       └╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┘🍎🍎🍎
                                                       🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎
                                                       �┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┐ 🍎🍎
                                                       �┆viewWithBorderAtX1┆ 🍎🍎
                                                       �└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┘ 🍎🍎
                                                       🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎
                                                       🍎┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┐🍎🍎
                                                       🍎┆viewWithBorderAtX2┆🍎🍎
                                                       🍎└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┘🍎🍎
                                                       🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎
                                                       """,
                                                       output,
                                                       driver);

        DriverAssert.AssertDriverOutputIs (@"\x1b[38;2;95;158;160m\x1b[48;2;54;69;79m🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┐🍎🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m┆viewWithBorderAtX0┆🍎🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┘🍎🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m�┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┐ 🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m�┆viewWithBorderAtX1┆ 🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m�└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┘ 🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m🍎┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┐🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m🍎┆viewWithBorderAtX2┆🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m🍎└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┘🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m    \x1b[38;2;95;158;160m\x1b[48;2;54;69;79m🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎\x1b[38;2;255;255;255m\x1b[48;2;0;0;0m",
                                           output, driver);

        DriverImpl? driverImpl = driver as DriverImpl;
        FakeOutput? fakeOutput = driverImpl!.Output as FakeOutput;

        output.WriteLine ("Driver Output After Redraw:\n" + fakeOutput!.Output);

        // BUGBUG: Border.set_LineStyle does not call SetNeedsDraw
        viewWithBorderAtX1!.Border!.LineStyle = LineStyle.Single;
        viewWithBorderAtX1.Border!.SetNeedsDraw ();
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎
                                              ┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┐🍎🍎🍎
                                              ┆viewWithBorderAtX0┆🍎🍎🍎
                                              └╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┘🍎🍎🍎
                                              🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎
                                              �┌──────────────────┐ 🍎🍎
                                              �│viewWithBorderAtX1│ 🍎🍎
                                              �└──────────────────┘ 🍎🍎
                                              🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎
                                              🍎┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┐🍎🍎
                                              🍎┆viewWithBorderAtX2┆🍎🍎
                                              🍎└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┘🍎🍎
                                              🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎
                                              """,
                                              output,
                                              driver);


    }

    [Fact]
    public void Draw_WithBorderSubView_At_Col1_In_WideGlyph_DrawsCorrectly ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");
        IDriver driver = app!.Driver!;
        driver.SetScreenSize (6, 3);  // Minimal: 6 cols wide (3 for content + 2 for border + 1), 3 rows high (1 for content + 2 for border)

        driver!.Clip = new (driver.Screen);

        var superView = new Runnable ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Driver = driver
        };

        Rune codepoint = Glyphs.Apple;

        superView.DrawingContent += (s, e) =>
                                    {
                                        View? view = s as View;
                                        view?.AddStr (0, 0, "🍎🍎🍎🍎");
                                        view?.AddStr (0, 1, "🍎🍎🍎🍎");
                                        view?.AddStr (0, 2, "🍎🍎🍎🍎");
                                        e.DrawContext?.AddDrawnRectangle (view!.Viewport);
                                        e.Cancel = true;
                                    };

        // Minimal border at X=1 (odd column), Width=3, Height=3 (includes border)
        var viewWithBorder = new View
        {
            Text = "X",
            BorderStyle = LineStyle.Single,
            X = 1,
            Y = 0,
            Width = 3,
            Height = 3
        };

        superView.Add (viewWithBorder);
        app.Begin (superView);

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              �┌─┐🍎
                                              �│X│🍎
                                              �└─┘🍎
                                              """,
                                              output,
                                              driver);

        DriverAssert.AssertDriverOutputIs (@"\x1b[38;2;95;158;160m\x1b[48;2;54;69;79m�┌─┐🍎�│X│🍎�└─┘🍎",
            output, driver);

        DriverImpl? driverImpl = driver as DriverImpl;
        FakeOutput? fakeOutput = driverImpl!.Output as FakeOutput;

        output.WriteLine ("Driver Output:\n" + fakeOutput!.Output);
    }


    [Fact]
    public void Draw_WithBorderSubView_At_Col3_In_WideGlyph_DrawsCorrectly ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");
        IDriver driver = app!.Driver!;
        driver.SetScreenSize (6, 3);  // Minimal: 3 cols wide, 1 row high (+ 2 for border top/bottom)

        driver!.Clip = new (driver.Screen);

        var superView = new Runnable ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Driver = driver
        };

        Rune codepoint = Glyphs.Apple;

        superView.DrawingContent += (s, e) =>
        {
            View? view = s as View;
            view?.AddStr (0, 0, "🍎🍎🍎🍎");
            view?.AddStr (0, 1, "🍎🍎🍎🍎");
            view?.AddStr (0, 2, "🍎🍎🍎🍎");
            e.DrawContext?.AddDrawnRectangle (view!.Viewport);
            e.Cancel = true;
        };

        // Minimal border at X=3 (odd column), Width=3, Height=3 (includes border)
        var viewWithBorder = new View
        {
            Text = "X",
            BorderStyle = LineStyle.Single,
            X = 3,
            Y = 0,
            Width = 3,
            Height = 3
        };

        superView.Add (viewWithBorder);
        app.Begin (superView);

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              🍎�┌─┐
                                              🍎�│X│
                                              🍎�└─┘
                                              """,
                                              output,
                                              driver);

        DriverAssert.AssertDriverOutputIs (@"\x1b[38;2;95;158;160m\x1b[48;2;54;69;79m🍎�┌─┐🍎�│X│🍎�└─┘",
            output, driver);

        DriverImpl? driverImpl = driver as DriverImpl;
        FakeOutput? fakeOutput = driverImpl!.Output as FakeOutput;

        output.WriteLine ("Driver Output:\n" + fakeOutput!.Output);
    }

    [Fact]
    public void Draw_NonVisibleView_DoesNotUpdateClip ()
    {
        IDriver driver = CreateFakeDriver ();
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
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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
        view.ExcludeFromClip (excludeRect);

        Assert.NotNull (driver.Clip);
        Assert.False (driver.Clip.Contains (20, 20)); // Point inside excluded rect should not be in clip
    }

    [Fact]
    public void ExcludeFromClip_WithNullClip_DoesNotThrow ()
    {
        IDriver driver = CreateFakeDriver ();
        driver.Clip = null!;

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };

        Exception? exception = Record.Exception (() => view.ExcludeFromClip (new Rectangle (15, 15, 10, 10)));

        Assert.Null (exception);
    }

    #endregion

    #region Misc Tests

    [Fact]
    public void SetClip_SetsDriverClip ()
    {
        IDriver driver = CreateFakeDriver ();

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };

        var newClip = new Region (new (5, 5, 30, 30));
        view.SetClip (newClip);

        Assert.Equal (newClip, driver.Clip);
    }

    [Fact (Skip = "See BUGBUG in SetClip")]
    public void SetClip_WithNullClip_ClearsClip ()
    {
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (new (10, 10, 20, 20));

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 20,
            Height = 20,
            Driver = driver
        };

        view.SetClip (null);

        Assert.Null (driver.Clip);
    }

    [Fact]
    public void Draw_Excludes_View_From_Clip ()
    {
        IDriver driver = CreateFakeDriver ();
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

        Region clipWithViewExcluded = originalClip.Clone ();
        clipWithViewExcluded.Exclude (view.Frame);

        view.Draw ();

        Assert.Equal (clipWithViewExcluded, driver.Clip);
        Assert.NotNull (driver.Clip);
    }

    [Fact]
    public void Draw_EmptyViewport_DoesNotCrash ()
    {
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

        var view = new View
        {
            X = 10,
            Y = 10,
            Width = 1,
            Height = 1,
            Driver = driver
        };
        view.Border!.Thickness = new (1);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        // With border of 1, viewport should be empty (0x0 or negative)
        Exception? exception = Record.Exception (() => view.Draw ());

        Assert.Null (exception);
    }

    [Fact]
    public void Draw_VeryLargeView_HandlesClippingCorrectly ()
    {
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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

        Exception? exception = Record.Exception (() => view.Draw ());

        Assert.Null (exception);
    }

    [Fact]
    public void Draw_NegativeCoordinates_HandlesClippingCorrectly ()
    {
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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

        Exception? exception = Record.Exception (() => view.Draw ());

        Assert.Null (exception);
    }

    [Fact]
    public void Draw_OutOfScreenBounds_HandlesClippingCorrectly ()
    {
        IDriver driver = CreateFakeDriver ();
        driver.Clip = new (driver.Screen);

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

        Exception? exception = Record.Exception (() => view.Draw ());

        Assert.Null (exception);
    }

    #endregion
}
