#nullable disable
using System.Text;
using UnitTests;

namespace ViewBaseTests.Draw;

// Claude - Opus 4.7
/// <summary>
///     Issue #5358: ClearViewport narrows the clear to NeedsDrawRect when an explicit partial
///     region has been set (via SetNeedsDraw(Rectangle)), and otherwise preserves the existing
///     full-clear contract. View.Layout invalidates the SuperView for the union of the old and
///     new Frame when Frame changes, so stale uncovered cells get cleared.
/// </summary>
public class RegionAwareClearViewportTests : TestDriverBase
{
    /// <summary>
    ///     Direct caller (NeedsDrawRect empty) gets the full-clear contract — backward compatible
    ///     with ClearViewport_FillsViewportArea and code paths like Code.OnClearingViewport.
    /// </summary>
    [Fact]
    public void DirectCaller_EmptyNeedsDrawRect_ClearsFullViewport ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View view = new () { Driver = driver, X = 5, Y = 5, Width = 10, Height = 5 };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();
        view.Draw ();

        Assert.True (view.NeedsDrawRect.IsEmpty);

        driver.FillRect (driver.Screen, new Rune ('X'));

        view.ClearViewport ();

        Rectangle screen = view.ViewportToScreen (view.Viewport with { Location = new Point (0, 0) });

        for (int y = screen.Y; y < screen.Y + screen.Height; y++)
        {
            for (int x = screen.X; x < screen.X + screen.Width; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }

        view.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     When NeedsDrawRect is set to a strictly smaller region, ClearViewport narrows the
    ///     clear to that region — cells outside the dirty region are not touched.
    /// </summary>
    [Fact]
    public void PartialNeedsDrawRect_ClearsOnlyDirtyRegion ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View view = new () { Driver = driver, X = 5, Y = 5, Width = 10, Height = 5 };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        driver.FillRect (driver.Screen, new Rune ('X'));

        // Sanity: FillRect filled the view area with 'X'.
        Assert.Equal ("X", driver.Contents [5, 5].Grapheme);

        // Reset NeedsDrawRect to Empty so SetNeedsDraw below sets it to exactly the partial
        // region (otherwise the init/layout-time NeedsDraw accumulates and we don't get a
        // strictly-smaller-than-viewport rect to trigger narrowing).
        view.ClearNeedsDraw ();
        Assert.True (view.NeedsDrawRect.IsEmpty);

        view.SetNeedsDraw (new Rectangle (1, 1, 3, 2));
        Assert.Equal (new Rectangle (1, 1, 3, 2), view.NeedsDrawRect);

        view.ClearViewport ();

        // The dirty region (viewport-local (1,1,3,2) → screen (6,6,3,2)) is cleared.
        for (var y = 6; y < 8; y++)
        {
            for (var x = 6; x < 9; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }

        // Cells outside the dirty region inside the viewport remain 'X'.
        Assert.Equal ("X", driver.Contents [5, 5].Grapheme);
        Assert.Equal ("X", driver.Contents [9, 14].Grapheme);

        view.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     SetNeedsDraw() (no-arg) sets NeedsDrawRect = Viewport. ClearViewport must still
    ///     clear the full viewport in this case — narrowing only fires when the rect is
    ///     strictly smaller than the viewport.
    /// </summary>
    [Fact]
    public void FullViewportNeedsDrawRect_ClearsFullViewport ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View view = new () { Driver = driver, X = 5, Y = 5, Width = 10, Height = 5 };
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();
        view.Draw ();

        Assert.True (view.NeedsDrawRect.IsEmpty);

        driver.FillRect (driver.Screen, new Rune ('X'));

        // SetNeedsDraw() no-arg sets NeedsDrawRect to Viewport (full).
        view.SetNeedsDraw ();
        Assert.Equal (view.Viewport, view.NeedsDrawRect);

        view.ClearViewport ();

        Rectangle screen = view.ViewportToScreen (view.Viewport with { Location = new Point (0, 0) });

        for (int y = screen.Y; y < screen.Y + screen.Height; y++)
        {
            for (int x = screen.X; x < screen.X + screen.Width; x++)
            {
                Assert.Equal (" ", driver.Contents [y, x].Grapheme);
            }
        }

        view.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     When a subview's Frame shrinks, View.Layout invalidates the SuperView for the union
    ///     of the old and new frames. This is the foundation for clearing stale uncovered cells
    ///     after geometry changes (without forcing the whole SuperView viewport to redraw).
    /// </summary>
    [Fact]
    public void FrameShrink_InvalidatesSuperViewWithUnionOfOldAndNewFrames ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View superView = new () { Driver = driver, Width = 20, Height = 10 };
        View view = new () { X = 0, Y = 0, Width = 7, Height = 4 };
        superView.Add (view);

        superView.Layout ();
        superView.Draw ();

        Assert.True (superView.NeedsDrawRect.IsEmpty);
        Assert.True (view.NeedsDrawRect.IsEmpty);

        // Shrink the subview's frame.
        view.Frame = new Rectangle (0, 0, 3, 2);

        // SuperView's NeedsDrawRect must include the OLD frame area so stale cells get cleared.
        Assert.False (superView.NeedsDrawRect.IsEmpty);
        Assert.True (superView.NeedsDrawRect.Contains (new Rectangle (0, 0, 7, 4)));

        superView.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Pure scroll (Viewport.X/Y change without Frame change) must NOT invalidate the
    ///     SuperView. Otherwise we re-introduce the per-scroll draw fan-out that issue #5358
    ///     is fixing for overlapping tab content.
    /// </summary>
    [Fact]
    public void Scroll_DoesNotInvalidateSuperView ()
    {
        IDriver driver = CreateTestDriver (40, 20);
        driver.Clip = new Region (driver.Screen);

        View superView = new () { Driver = driver, Width = 20, Height = 10 };
        View view = new () { X = 0, Y = 0, Width = 10, Height = 5 };
        view.SetContentSize (new Size (100, 100));
        superView.Add (view);

        superView.Layout ();
        superView.Draw ();

        Assert.True (superView.NeedsDrawRect.IsEmpty);

        // Pure scroll — Viewport location changes, Frame does not.
        view.Viewport = view.Viewport with { Y = 5 };

        // SuperView must NOT have been invalidated by the scroll alone.
        Assert.True (superView.NeedsDrawRect.IsEmpty);

        superView.Dispose ();
        driver.Dispose ();
    }
}
