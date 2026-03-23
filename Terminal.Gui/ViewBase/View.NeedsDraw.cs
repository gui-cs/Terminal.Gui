namespace Terminal.Gui.ViewBase;

public partial class View
{
    // NOTE: NeedsDrawRect is not currently used to clip drawing to only the invalidated region.
    //       It is only used within SetNeedsDraw to propagate redraw requests to subviews.
    // NOTE: Consider changing NeedsDrawRect from Rectangle to Region for more precise invalidation
    //       NeedsDraw is already efficiently cached via NeedsDrawRect. It checks:
    //       1. NeedsDrawRect (cached by SetNeedsDraw/ClearNeedsDraw)
    //       2. Adornment NeedsDraw flags (each cached separately)
    /// <summary>
    ///     INTERNAL: Gets the viewport-relative region that needs to be redrawn.
    /// </summary>
    internal Rectangle NeedsDrawRect { get; private set; } = Rectangle.Empty;

    /// <summary>Gets whether the view needs to be redrawn.</summary>
    /// <remarks>
    ///     <para>
    ///         Will be <see langword="true"/> if the <see cref="NeedsLayout"/> property is <see langword="true"/> or if
    ///         any part of the view's <see cref="Viewport"/> needs to be redrawn.
    ///     </para>
    /// </remarks>
    public bool NeedsDraw =>
        Visible && (NeedsDrawRect != Rectangle.Empty || Margin.View?.NeedsDraw == true || Border.View?.NeedsDraw == true || Padding.View?.NeedsDraw == true);

    /// <summary>
    ///     Sets <see cref="NeedsDraw"/> to <see langword="true"/> indicating the <see cref="Viewport"/> of this View needs to
    ///     be redrawn.
    /// </summary>
    /// <remarks>
    ///     If the view is not visible (<see cref="Visible"/> is <see langword="false"/>), this method
    ///     does nothing.
    /// </remarks>
    public void SetNeedsDraw ()
    {
        Rectangle viewport = Viewport;

        if (!Visible || (NeedsDrawRect != Rectangle.Empty && viewport.IsEmpty))
        {
            // This handles the case where the view has not been initialized yet
            return;
        }

        SetNeedsDraw (viewport);
    }

    /// <summary>Expands the area of this view needing to be redrawn to include <paramref name="viewPortRelativeRegion"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         The location of <paramref name="viewPortRelativeRegion"/> is relative to the View's <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), the area to be
    ///         redrawn will be the <paramref name="viewPortRelativeRegion"/>.
    ///     </para>
    /// </remarks>
    /// <param name="viewPortRelativeRegion">The <see cref="Viewport"/>relative region that needs to be redrawn.</param>
    public void SetNeedsDraw (Rectangle viewPortRelativeRegion)
    {
        // Invalidate the cached drawn region used for TransparentMouse hit-testing.
        // It will be repopulated on the next Draw() pass.
        CachedDrawnRegion = null;

        // If we are at the top of the hierarchy, we're a runnable,
        // and we need to ensure any other Runnables get redrawn.
        if (App?.TopRunnableView == this && App is { })
        {
            //App?.ClearScreenNextIteration = true;
            List<View?> runnables = [.. App?.SessionStack?.Select (r => r.Runnable as View).Where (v => v != this && v?.NeedsDraw == false)!];

            foreach (View? runnable in runnables)
            {
                runnable?.SetNeedsDraw ();
            }
        }

        if (!Visible)
        {
            return;
        }

        if (NeedsDrawRect.IsEmpty)
        {
            NeedsDrawRect = viewPortRelativeRegion;
        }
        else
        {
            int x = Math.Min (Viewport.X, viewPortRelativeRegion.X);
            int y = Math.Min (Viewport.Y, viewPortRelativeRegion.Y);
            int w = Math.Max (Viewport.Width, viewPortRelativeRegion.Width);
            int h = Math.Max (Viewport.Height, viewPortRelativeRegion.Height);
            NeedsDrawRect = new Rectangle (x, y, w, h);
        }

        // Do not set on Margin - it will be drawn in a separate pass.

        if (Border.Thickness != Thickness.Empty)
        {
            Border.View?.SetNeedsDraw ();
        }

        if (Padding.Thickness != Thickness.Empty)
        {
            Padding.View?.SetNeedsDraw ();
        }

        SuperView?.SetSubViewNeedsDrawDownHierarchy ();

        if (this is AdornmentView adornment)
        {
            adornment.Adornment?.Parent?.SetSubViewNeedsDrawDownHierarchy ();
        }

        foreach (View subview in InternalSubViews.Snapshot ())
        {
            if (!subview.Frame.IntersectsWith (viewPortRelativeRegion))
            {
                continue;
            }
            Rectangle subviewRegion = Rectangle.Intersect (subview.Frame, viewPortRelativeRegion);
            subviewRegion.X -= subview.Frame.X;
            subviewRegion.Y -= subview.Frame.Y;
            subview.SetNeedsDraw (subviewRegion);
        }
    }

    /// <summary>INTERNAL: Clears <see cref="NeedsDraw"/> and <see cref="SubViewNeedsDraw"/> for this view and all SubViews.</summary>
    /// <remarks>
    ///     See <see cref="SubViewNeedsDraw"/> is a cached value that is set when any subview or adornment requests a redraw.
    ///     It may not always be in sync with the actual state of the subviews.
    /// </remarks>
    internal void ClearNeedsDraw ()
    {
        NeedsDrawRect = Rectangle.Empty;

        Margin.View?.ClearNeedsDraw ();
        Border.View?.ClearNeedsDraw ();
        Padding.View?.ClearNeedsDraw ();

        foreach (View subview in InternalSubViews.Snapshot ())
        {
            subview.ClearNeedsDraw ();
        }

        SubViewNeedsDraw = false;

        // This ensures LineCanvas' get redrawn
        if (!SuperViewRendersLineCanvas)
        {
            LineCanvas.Clear ();
        }
    }

    // NOTE: SubViewNeedsDraw is decoupled from the actual state of the subviews (and adornments).
    //       It is a performance optimization to avoid having to traverse all subviews and adornments to check if any need redraw.
    //       As a result the code is fragile and can get out of sync; care must be taken to ensure it is set and cleared correctly.
    /// <summary>
    ///     INTERNAL: Gets whether any SubViews need to be redrawn.
    /// </summary>
    /// <remarks>
    ///     See <see cref="SubViewNeedsDraw"/> is a cached value that is set when any subview or adornment requests a redraw.
    ///     It may not always be in sync with the actual state of the subviews.
    /// </remarks>
    internal bool SubViewNeedsDraw { get; private set; }

    /// <summary>INTERNAL: Sets <see cref="SubViewNeedsDraw"/> to <see langword="true"/> for this View and all Superviews.</summary>
    /// <remarks>
    ///     See <see cref="SubViewNeedsDraw"/> is a cached value that is set when any subview or adornment requests a redraw.
    ///     It may not always be in sync with the actual state of the subviews.
    /// </remarks>
    internal void SetSubViewNeedsDrawDownHierarchy ()
    {
        if (!Visible)
        {
            return;
        }

        SubViewNeedsDraw = true;

        if (this is AdornmentView adornment)
        {
            adornment.Adornment?.Parent?.SetSubViewNeedsDrawDownHierarchy ();
        }

        if (SuperView is { SubViewNeedsDraw: false })
        {
            SuperView.SetSubViewNeedsDrawDownHierarchy ();
        }
    }
}
