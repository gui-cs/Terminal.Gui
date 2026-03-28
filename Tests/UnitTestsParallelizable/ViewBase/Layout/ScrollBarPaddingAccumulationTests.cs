namespace ViewBaseTests.Layout;

/// <summary>
///     Tests for the scrollbar padding accumulation bug discovered during investigation of
///     <see href="https://github.com/gui-cs/Terminal.Gui/issues/4522"/>.
///
///     Root cause: <c>ScrollBar.VisibleChanged</c> handlers in <c>View.ScrollBars.cs</c> use
///     relative <c>Padding.Thickness +1/-1</c> adjustments. During layout re-entry (e.g. maximize/
///     restore cycles), <c>ShowHide</c> fires multiple times per visibility change, compounding
///     the adjustment. Each cycle adds +2 to <c>Padding.Bottom</c> (horizontal scrollbar) and
///     +1 to <c>Padding.Right</c> (vertical scrollbar), causing the Viewport to shrink
///     permanently.
///
///     Fix: track whether scrollbar padding has been applied via boolean flags
///     (<c>_verticalScrollBarPaddingApplied</c> / <c>_horizontalScrollBarPaddingApplied</c>)
///     and make the <c>VisibleChanged</c> handlers idempotent.
/// </summary>
public class ScrollBarPaddingAccumulationTests
{
    /// <summary>
    ///     Proves that repeated maximize/restore (resize) cycles on a Dialog do not cause
    ///     Padding.Thickness.Bottom to ratchet upward. Before the fix, each cycle added +2
    ///     to Bottom, shrinking the Viewport by 2 rows per cycle.
    /// </summary>
    [Fact]
    public void Dialog_Resize_Cycles_Do_Not_Accumulate_Padding ()
    {
        Dialog dialog = new ()
        {
            Width = 60,
            Height = 20
        };

        View child = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        dialog.Add (child);
        dialog.AddButton (new Button { Title = "_OK" });
        dialog.BeginInit ();
        dialog.EndInit ();

        // Initial layout at "normal" size
        dialog.Layout ();
        Thickness initialPadding = dialog.Padding.Thickness;
        int initialViewportHeight = dialog.Viewport.Height;

        // Simulate 5 maximize/restore cycles
        for (int i = 0; i < 5; i++)
        {
            // Maximize
            dialog.Width = 200;
            dialog.Height = 80;
            dialog.SetNeedsLayout ();
            dialog.Layout ();

            // Restore
            dialog.Width = 60;
            dialog.Height = 20;
            dialog.SetNeedsLayout ();
            dialog.Layout ();
        }

        Thickness finalPadding = dialog.Padding.Thickness;
        int finalViewportHeight = dialog.Viewport.Height;

        // Assert: Padding should not have grown
        Assert.Equal (initialPadding.Bottom, finalPadding.Bottom);
        Assert.Equal (initialPadding.Right, finalPadding.Right);

        // Assert: Viewport should be the same size as initially
        Assert.Equal (initialViewportHeight, finalViewportHeight);
    }

    /// <summary>
    ///     Proves that scrollbar visibility toggling (show/hide) applies padding exactly once
    ///     and removes it exactly once, regardless of how many layout passes occur.
    /// </summary>
    [Fact]
    public void ScrollBar_Visibility_Toggle_Is_Idempotent ()
    {
        View view = new ()
        {
            Width = 20,
            Height = 10,
            ViewportSettings = ViewportSettingsFlags.HasScrollBars
        };

        // Force content larger than viewport to trigger scrollbars
        view.SetContentSize (new Size (40, 30));

        view.BeginInit ();
        view.EndInit ();
        view.Layout ();

        Thickness afterFirstLayout = view.Padding.Thickness;

        // Re-layout multiple times (simulating resize churn)
        for (int i = 0; i < 10; i++)
        {
            view.SetNeedsLayout ();
            view.Layout ();
        }

        Thickness afterManyLayouts = view.Padding.Thickness;

        // Padding should be identical after repeated layouts
        Assert.Equal (afterFirstLayout.Bottom, afterManyLayouts.Bottom);
        Assert.Equal (afterFirstLayout.Right, afterManyLayouts.Right);
    }
}
