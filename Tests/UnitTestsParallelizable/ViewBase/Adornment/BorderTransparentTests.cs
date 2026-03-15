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
            Height = Dim.Auto (),
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
            Height = Dim.Auto (),
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
    ///     Currently, fails because the blanket TransparentMouse flag removes the Border
    ///     from ALL hit-testing, including border line cells.
    /// </summary>
    [Fact (Skip = "Not yet implemented — Issue #4834")]
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
