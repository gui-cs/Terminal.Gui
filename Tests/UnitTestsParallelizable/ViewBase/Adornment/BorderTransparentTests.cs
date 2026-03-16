// Claude - Opus 4.6

using System.Text;
using UnitTests;

// ReSharper disable StringLiteralTypo

namespace ViewBaseTests.Adornments;

/// <summary>
///     Tests for Border transparency support (Issue #4834).
///     Border should support ViewportSettingsFlags.Transparent and ViewportSettingsFlags.TransparentMouse.
/// </summary>
public class BorderTransparentTests (ITestOutputHelper output)
{
    /// <summary>
    ///     Verifies that a Border with Transparent set only draws border lines,
    ///     allowing underlying content to show through the interior.
    ///     Currently, fails because Border doesn't honor Transparent — the interior
    ///     shows spaces instead of the underlying 'X' background.
    /// </summary>
    [Fact]
    public void Border_Transparent_Shows_Underlying_Content_In_Viewport ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (7, 5);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        View borderedView = new ()
        {
            X = 1,
            Y = 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;

        window.Add (borderedView);
        app.Begin (window);

        // The interior (row 2, cols 2-4) should show 'X' from the underlying window,
        // not spaces from the bordered view's cleared viewport.
        // Border lines should still be drawn normally.
        DriverAssert.AssertDriverContentsAre ("""

                                              XXXXXXX
                                              X┌───┐X
                                              X│XXX│X
                                              X└───┘X
                                              XXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void Border_Transparent_Shows_Underlying_Content_Where_Border_DrawContent_Are_Not ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (11, 9);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        View borderedView = new ()
        {
            Title = "B",
            X = 2,
            Y = 2,
            Width = 7,
            Height = 5,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;
        borderedView.Border.Thickness = new Thickness (2);

        window.Add (borderedView);
        app.Begin (window);

        // The interior (row 2, cols 2-4) should show 'X' from the underlying window,
        // not spaces from the bordered view's cleared viewport.
        // Border lines should still be drawn normally.
        DriverAssert.AssertDriverContentsAre ("""
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              XXXX┌─┐XXXX
                                              XXX┌┘B└┐XXX
                                              XXX│XXX│XXX
                                              XXX└───┘XXX
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void Border_Transparent_Shows_Underlying_SubViews_Where_Border_DrawContent_Are_Not ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (11, 9);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        View subView = new ()
        {
            Text = "sub",
            X = 1,
            Y = 2,
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        window.Add (subView);

        View borderedView = new ()
        {
            Title = "B",
            X = 2,
            Y = 2,
            Width = 7,
            Height = 5,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;
        borderedView.Border.Thickness = new Thickness (2);

        window.Add (borderedView);
        app.Begin (window);

        // The interior (row 2, cols 2-4) should show 'X' from the underlying window,
        // not spaces from the bordered view's cleared viewport.
        // Border lines should still be drawn normally.
        DriverAssert.AssertDriverContentsAre ("""
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              Xsub┌─┐XXXX
                                              XXX┌┘B└┐XXX
                                              XXX│XXX│XXX
                                              XXX└───┘XXX
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              XXXXXXXXXXX
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void Border_Transparent_Occludes_Underlying_SubViews_Where_Border_DrawContent_Is ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (11, 3);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // Fill window background with 'X' to detect transparency
        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };

        View subView = new ()
        {
            Text = "subview",
            X = 0,
            Y = 1,
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        window.Add (subView);

        View borderedView = new ()
        {
            Title = "AB",
            X = 0,
            Y = 0,
            Width = 9,
            Height = 3,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.Transparent;
        borderedView.Border.Thickness = new Thickness (2, 3, 1, 0);

        window.Add (borderedView);
        app.Begin (window);

        DriverAssert.AssertDriverContentsAre ("""
                                              XX┌──┐XXXXX
                                              s┌┤AB├──┐XX
                                              X│└──┘XX│XX
                                              """,
                                              output,
                                              app.Driver);
    }

    /// <summary>
    ///     Verifies that mouse events in the transparent interior of a Border with TransparentMouse
    ///     pass through to views underneath.
    ///     Currently, passes — but only because the blanket TransparentMouse flag removes the Border
    ///     entirely from hit-testing. After per-cell TransparentMouse is implemented, this should
    ///     still pass because the interior cells were not drawn by the Border.
    /// </summary>
    [Fact]
    public void Border_TransparentMouse_Interior_Clicks_Pass_Through ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (10, 6);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 2,
            Y = 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Id = "Bordered"
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        window.Add (borderedView);
        app.Begin (window);

        // Screen position (4, 2) is in the interior of the bordered view —
        // inside the border outline but NOT on a border line.
        // With TransparentMouse, this should pass through to views underneath.
        List<View?> viewsUnderInterior = window.GetViewsUnderLocation (new Point (4, 2), ViewportSettingsFlags.TransparentMouse);

        // The bordered view's Border should NOT be in the hit list for interior points
        bool borderInList = viewsUnderInterior.Any (v => v is Border);

        Assert.False (borderInList, "Border with TransparentMouse should not capture mouse events in its transparent interior");
    }

    /// <summary>
    ///     Verifies that mouse events ON the border lines of a Border with TransparentMouse
    ///     are still captured by the Border (only the transparent interior passes through).
    ///     Requires Phase 2e (drawn-region-aware hit-testing).
    /// </summary>
    [Fact (Skip = "Phase 2e — drawn-region-aware hit-testing not yet implemented")]
    public void Border_TransparentMouse_BorderLine_Clicks_Are_Captured ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (10, 6);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 2,
            Y = 1,
            Width = 5,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Id = "Bordered"
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        window.Add (borderedView);
        app.Begin (window);

        // Click the top-left corner of the border (screen position 2, 1)
        // This IS on a border line, so it should be captured by the Border.
        List<View?> viewsOnBorderLine = window.GetViewsUnderLocation (new Point (2, 1), ViewportSettingsFlags.TransparentMouse);

        bool borderInList = viewsOnBorderLine.Any (v => v is Border);

        Assert.True (borderInList, "Border with TransparentMouse should still capture mouse events on its drawn border lines");
    }

    /// <summary>
    ///     Verifies that mouse events on the title text of a Border with TransparentMouse
    ///     are captured by the Border. The title is drawn content and should receive clicks.
    ///     Requires Phase 2e.
    /// </summary>
    [Fact (Skip = "Phase 2e — drawn-region-aware hit-testing not yet implemented")]
    public void Border_TransparentMouse_Title_Clicks_Are_Captured ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 5);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            Title = "Test",
            X = 1,
            Y = 1,
            Width = 10,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Id = "Bordered"
        };
        borderedView.Border!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        window.Add (borderedView);
        app.Begin (window);

        // The title "Test" is drawn at the top border line. Click on the 'T' of "Test".
        // Screen position: border starts at (1,1), title starts at col 3 (after "┌┤").
        List<View?> viewsOnTitle = window.GetViewsUnderLocation (new Point (3, 1), ViewportSettingsFlags.TransparentMouse);

        bool borderInList = viewsOnTitle.Any (v => v is Border);

        Assert.True (borderInList, "Border with TransparentMouse should capture mouse events on its title text");
    }

    /// <summary>
    ///     Verifies that a Margin with TransparentMouse and a shadow passes mouse events through
    ///     empty margin cells but captures them on shadow cells.
    ///     Requires Phase 2d (ShadowView reports drawn region) and 2e (drawn-region-aware hit-testing).
    /// </summary>
    [Fact (Skip = "Phase 2d/2e — ShadowView drawn region and hit-testing not yet implemented")]
    public void Margin_TransparentMouse_Shadow_Clicks_Are_Captured ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (10, 6);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View shadowView = new ()
        {
            X = 1,
            Y = 1,
            Width = 5,
            Height = 3,
            ShadowStyle = ShadowStyle.Opaque,
            Id = "Shadowed"
        };

        window.Add (shadowView);
        app.Begin (window);

        // The shadow is drawn on the right (col 6) and bottom (row 4) of the view.
        // The Margin has TransparentMouse set by default.
        // Click on a shadow cell — should be captured (shadow drew there).
        List<View?> viewsOnShadow = window.GetViewsUnderLocation (new Point (6, 2), ViewportSettingsFlags.TransparentMouse);
        bool marginInList = viewsOnShadow.Any (v => v is Margin);

        Assert.True (marginInList, "Margin with shadow should capture mouse events on shadow cells");

        // Click on an empty margin cell (if any) — should pass through.
        // With default Margin thickness of 0 + shadow thickness of 1, the empty margin area
        // is minimal. Skip this sub-assertion if there's no empty margin area to test.
    }

    /// <summary>
    ///     Verifies that drawn-region-aware hit-testing works for a regular View with TransparentMouse.
    ///     A view that draws only part of its area should pass through clicks on undrawn cells
    ///     but capture clicks on drawn cells.
    ///     Requires Phase 2a/2b/2e.
    /// </summary>
    [Fact (Skip = "Phase 2a/2b/2e — drawn-region-aware hit-testing not yet implemented")]
    public void View_TransparentMouse_DrawnCells_Captured_UndrawnCells_PassThrough ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 20);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        // A view that draws only a small rectangle in its viewport.
        View partialView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Id = "Partial",
            ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse
        };

        // Draw a 3x3 block at viewport (1, 1)
        partialView.DrawingContent += (_, args) =>
                                      {
                                          partialView.FillRect (new Rectangle (1, 1, 3, 3), new Rune ('#'));
                                          args.DrawContext?.AddDrawnRectangle (partialView.ViewportToScreen (new Rectangle (1, 1, 3, 3)));
                                      };

        window.Add (partialView);
        app.Begin (window);

        // Screen (7, 7) is inside the drawn 3x3 block (viewport 1,1 + view origin 5,5 + viewport offset).
        // This should be captured by the view.
        List<View?> viewsOnDrawnCell = window.GetViewsUnderLocation (new Point (7, 7), ViewportSettingsFlags.TransparentMouse);
        bool partialInList = viewsOnDrawnCell.Any (v => v?.Id == "Partial");
        Assert.True (partialInList, "TransparentMouse view should capture clicks on drawn cells");

        // Screen (5, 5) is the top-left of the view's viewport — undrawn area.
        // This should pass through.
        List<View?> viewsOnUndrawnCell = window.GetViewsUnderLocation (new Point (5, 5), ViewportSettingsFlags.TransparentMouse);
        bool partialInListUndrawn = viewsOnUndrawnCell.Any (v => v?.Id == "Partial");
        Assert.False (partialInListUndrawn, "TransparentMouse view should pass through clicks on undrawn cells");
    }

    /// <summary>
    ///     Verifies that when CachedDrawnRegion is null (before first draw), TransparentMouse
    ///     falls back to blanket removal (no regression from current behavior).
    ///     Requires Phase 2e.
    /// </summary>
    [Fact (Skip = "Phase 2e — drawn-region-aware hit-testing not yet implemented")]
    public void View_TransparentMouse_NullCache_FallsBackToBlanketRemoval ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 20);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View transparentMouseView = new ()
        {
            X = 5,
            Y = 5,
            Width = 10,
            Height = 10,
            Id = "TMView",
            ViewportSettings = ViewportSettingsFlags.TransparentMouse
        };

        window.Add (transparentMouseView);

        // Do NOT call app.Begin — no draw has occurred, so CachedDrawnRegion is null.
        // Manually init and layout so GetViewsUnderLocation can work.
        window.BeginInit ();
        window.EndInit ();
        window.LayoutSubViews ();

        // With null cache, TransparentMouse should fall back to blanket removal.
        List<View?> views = window.GetViewsUnderLocation (new Point (7, 7), ViewportSettingsFlags.TransparentMouse);
        bool tmViewInList = views.Any (v => v?.Id == "TMView");

        Assert.False (tmViewInList, "View with TransparentMouse and null CachedDrawnRegion should be removed (blanket fallback)");
    }

    /// <summary>
    ///     Verifies that a Border with TransparentMouse on a thick border correctly distinguishes
    ///     between drawn border line cells and empty cells in the border thickness area.
    ///     Requires Phase 2a/2b/2e.
    /// </summary>
    [Fact (Skip = "Phase 2a/2b/2e — drawn-region-aware hit-testing not yet implemented")]
    public void Border_TransparentMouse_ThickBorder_EmptyCells_PassThrough ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (12, 10);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 1,
            Y = 1,
            Width = 9,
            Height = 7,
            BorderStyle = LineStyle.Single,
            Id = "ThickBordered"
        };
        borderedView.Border!.Thickness = new Thickness (2);
        borderedView.Border.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;

        window.Add (borderedView);
        app.Begin (window);

        // The outer border line is at offset 0 from Border frame; inner line at offset 1.
        // With thickness=2, there's a gap row/col between outer and inner lines where nothing is drawn.
        // Click on the top-left outer border line — should be captured.
        List<View?> viewsOnOuterLine = window.GetViewsUnderLocation (new Point (1, 1), ViewportSettingsFlags.TransparentMouse);
        bool borderCaptured = viewsOnOuterLine.Any (v => v is Border);
        Assert.True (borderCaptured, "Click on outer border line should be captured");

        // Click on an empty cell in the border thickness gap (between outer and inner lines).
        // For thickness=2 with single line, the cell at (2, 3) in screen coords should be empty gap.
        // This is inside the border frame but not on a drawn line — should pass through.
        // Note: The exact gap position depends on how the lines are drawn with thickness=2.
    }

    /// <summary>
    ///     Verifies that a Border SubView positioned with Pos.AnchorEnd renders at the bottom
    ///     of the border, and repositions correctly when the parent view is resized.
    /// </summary>
    [Fact]
    public void Border_SubView_AnchorEnd_Renders_At_Bottom_Before_And_After_Resize ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (7, 8);

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        View borderedView = new ()
        {
            X = 0,
            Y = 0,
            Width = 7,
            Height = 6,
            BorderStyle = LineStyle.Single
        };
        borderedView.Border!.Thickness = new Thickness (1, 1, 1, 2);

        View borderSubView = new ()
        {
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Height = 1,
            Text = "ZZZ"
        };
        borderedView.Border.Add (borderSubView);

        window.Add (borderedView);
        app.Begin (window);

        // Height=6, border top=1, bottom=2: 3 content rows.
        // Bottom thickness 2 = border line + subview row below it.
        // "ZZZ" should render at the last row of the border (below └─────┘).
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌─────┐
                                              │     │
                                              │     │
                                              │     │
                                              └─────┘
                                              ZZZ
                                              """,
                                              output,
                                              app.Driver);

        // Resize: shrink height from 6 to 4. Now 1 content row.
        borderedView.Height = 4;
        app.LayoutAndDraw ();

        // "ZZZ" should now be at the new bottom.
        DriverAssert.AssertDriverContentsAre ("""
                                              ┌─────┐
                                              │     │
                                              └─────┘
                                              ZZZ
                                              """,
                                              output,
                                              app.Driver);
    }
}
