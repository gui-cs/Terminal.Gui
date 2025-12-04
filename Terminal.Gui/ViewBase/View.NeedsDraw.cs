namespace Terminal.Gui.ViewBase;

public partial class View
{
    // TODO: Change NeedsDraw to use a Region instead of Rectangle
    // TODO: Make _needsDrawRect nullable instead of relying on Empty
    //      TODO: If null, it means ?
    //      TODO: If Empty, it means no need to redraw
    //      TODO: If not Empty, it means the region that needs to be redrawn

    /// <summary>
    ///     The viewport-relative region that needs to be redrawn. Marked internal for unit tests.
    /// </summary>
    internal Rectangle NeedsDrawRect { get; set; } = Rectangle.Empty;

    /// <summary>Gets or sets whether the view needs to be redrawn.</summary>
    /// <remarks>
    ///     <para>
    ///         Will be <see langword="true"/> if the <see cref="NeedsLayout"/> property is <see langword="true"/> or if
    ///         any part of the view's <see cref="Viewport"/> needs to be redrawn.
    ///     </para>
    ///     <para>
    ///         Setting has no effect on <see cref="NeedsLayout"/>.
    ///     </para>
    /// </remarks>
    public bool NeedsDraw
    {
        get => Visible && (NeedsDrawRect != Rectangle.Empty || Margin?.NeedsDraw == true || Border?.NeedsDraw == true || Padding?.NeedsDraw == true);
        set
        {
            if (value)
            {
                SetNeedsDraw ();
            }
            else
            {
                ClearNeedsDraw ();
            }
        }
    }

    // TODO: This property is decoupled from the actual state of the subviews (and adornments)
    // TODO: It is a 'cache' that is set when any subview or adornment requests a redraw
    // TODO: As a result the code is fragile and can get out of sync. 
    // TODO: Consider making this a computed property that checks all subviews and adornments for their NeedsDraw state
    // TODO: But that may have performance implications.

    /// <summary>Gets whether any SubViews need to be redrawn.</summary>
    public bool SubViewNeedsDraw { get; private set; }

    /// <summary>Sets that the <see cref="Viewport"/> of this View needs to be redrawn.</summary>
    /// <remarks>
    ///     If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), this method
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
            NeedsDrawRect = new (x, y, w, h);
        }

        // Do not set on Margin - it will be drawn in a separate pass.

        if (Border is { } && Border.Thickness != Thickness.Empty)
        {
            Border?.SetNeedsDraw ();
        }

        if (Padding is { } && Padding.Thickness != Thickness.Empty)
        {
            Padding?.SetNeedsDraw ();
        }

        SuperView?.SetSubViewNeedsDraw ();

        if (this is Adornment adornment)
        {
            adornment.Parent?.SetSubViewNeedsDraw ();
        }

        // There was multiple enumeration error here, so calling new snapshot collection - probably a stop gap
        foreach (View subview in InternalSubViews.Snapshot ())
        {
            if (subview.Frame.IntersectsWith (viewPortRelativeRegion))
            {
                Rectangle subviewRegion = Rectangle.Intersect (subview.Frame, viewPortRelativeRegion);
                subviewRegion.X -= subview.Frame.X;
                subviewRegion.Y -= subview.Frame.Y;
                subview.SetNeedsDraw (subviewRegion);
            }
        }
    }

    /// <summary>Sets <see cref="SubViewNeedsDraw"/> to <see langword="true"/> for this View and all Superviews.</summary>
    public void SetSubViewNeedsDraw ()
    {
        if (!Visible)
        {
            return;
        }

        SubViewNeedsDraw = true;

        if (this is Adornment adornment)
        {
            adornment.Parent?.SetSubViewNeedsDraw ();
        }

        if (SuperView is { SubViewNeedsDraw: false })
        {
            SuperView.SetSubViewNeedsDraw ();
        }
    }

    /// <summary>Clears <see cref="NeedsDraw"/> and <see cref="SubViewNeedsDraw"/>.</summary>
    protected void ClearNeedsDraw ()
    {
        NeedsDrawRect = Rectangle.Empty;

        Margin?.ClearNeedsDraw ();
        Border?.ClearNeedsDraw ();
        Padding?.ClearNeedsDraw ();

        // There was multiple enumeration error here, so calling new snapshot collection - probably a stop gap
        foreach (View subview in InternalSubViews.Snapshot ())
        {
            subview.ClearNeedsDraw ();
        }

        SubViewNeedsDraw = false;

        // DO NOT clear SuperView.SubViewNeedsDraw here!
        // The SuperView is responsible for clearing its own SubViewNeedsDraw flag.
        // Previously this code cleared it:
        //if (SuperView is { })
        //{
        //    SuperView.SubViewNeedsDraw = false;
        //}
        // This caused a bug where drawing one subview would incorrectly clear the SuperView's
        // SubViewNeedsDraw flag even when sibling subviews still needed drawing.
        //
        // The SuperView will clear its own SubViewNeedsDraw after all its subviews are drawn,
        // either via:
        // 1. The superview's own Draw() method calling ClearNeedsDraw()
        // 2. The static View.Draw(peers) method calling ClearNeedsDraw() on all peers

        // This ensures LineCanvas' get redrawn
        if (!SuperViewRendersLineCanvas)
        {
            LineCanvas.Clear ();
        }
    }
}
