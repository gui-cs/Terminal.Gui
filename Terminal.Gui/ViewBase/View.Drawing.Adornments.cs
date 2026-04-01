namespace Terminal.Gui.ViewBase;

public partial class View
{
    private void DoDrawAdornmentsSubViews (DrawContext? context)
    {
        // Only process Margin here if it is not Transparent. Transparent Margins are drawn in a separate pass in the static View.Draw
        // via Margin.DrawTransparentMargins.
        if (Margin.View is { } marginView && !Margin.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent) && Margin.Thickness != Thickness.Empty)
        {
            marginView.SetNeedsDraw ();

            foreach (View subview in marginView.SubViews)
            {
                subview.SetNeedsDraw ();
            }

            // NOTE: We do not support arbitrary SubViews of Margin (only ShadowView)
            // NOTE: so we do not call DoDrawSubViews on Margin.
        }

        if (Border.View is { SubViews.Count: > 0 } borderView && Border.Thickness != Thickness.Empty)
        {
            borderView.SetNeedsDraw ();

            // PERFORMANCE: Get the check for DrawIndicator out of this somehow.
            foreach (View subview in borderView.SubViews.Where (v => v.Visible || v.Id == "DrawIndicator"))
            {
                if (subview.Id != "DrawIndicator")
                {
                    subview.SetNeedsDraw ();
                }

                // Only Exclude SubViews that don't merge their LC into the parent.
                // SuperViewRendersLineCanvas SubViews contribute LC lines via Merge, and
                // excluding them would prevent those merged lines from rendering.
                if (!subview.SuperViewRendersLineCanvas)
                {
                    LineCanvas.Exclude (new Region (subview.FrameToScreen ()));
                }
            }

            Region? saved = borderView.AddFrameToClip ();
            borderView.DoDrawSubViews ();

            // Merge any LineCanvas lines from Border's SubViews into this View's LineCanvas.
            // This ensures auto-join works between adornment subview borders and the view's own border.
            if (borderView.LineCanvas.Bounds != Rectangle.Empty)
            {
                LineCanvas.Merge (borderView.LineCanvas);
                borderView.LineCanvas.Clear ();
            }

            SetClip (saved);

            // Track drawn subview areas so DoDrawComplete can exclude them from clip
            // even when Border is transparent.
            foreach (View subview in borderView.SubViews.Where (v => v.Visible && v.Id != "DrawIndicator"))
            {
                context?.AddDrawnRectangle (subview.FrameToScreen ());
            }
        }

        if (Padding.View is not { SubViews.Count: > 0 } paddingView || Padding.Thickness == Thickness.Empty)
        {
            return;
        }

        paddingView.SetNeedsDraw ();

        foreach (View subview in paddingView.SubViews)
        {
            subview.SetNeedsDraw ();
        }

        Region? savedPadding = paddingView.AddFrameToClip ();
        paddingView.DoDrawSubViews ();

        // Merge any LineCanvas lines from Padding's SubViews (e.g., Tabs tab headers)
        // into this View's LineCanvas. This ensures auto-join works between adornment subview
        // borders and the view's own border.
        if (paddingView.LineCanvas.Bounds != Rectangle.Empty)
        {
            LineCanvas.Merge (paddingView.LineCanvas);
            paddingView.LineCanvas.Clear ();
        }

        SetClip (savedPadding);

        // Track drawn subview areas for Padding transparency support.
        foreach (View subview in paddingView.SubViews.Where (v => v.Visible))
        {
            context?.AddDrawnRectangle (subview.FrameToScreen ());
        }
    }

    internal void DoDrawAdornments (Region? originalClip)
    {
        if (this is AdornmentView)
        {
            AddFrameToClip ();

            return;
        }

        // Set the clip to be just the thicknesses of the adornments
        // TODO: Put this union logic in a method on View?
        Region clipAdornments = Margin.Thickness.AsRegion (Margin.FrameToScreen ());
        clipAdornments.Combine (Border.Thickness.AsRegion (Border.FrameToScreen ()), RegionOp.Union);
        clipAdornments.Combine (Padding.Thickness.AsRegion (Padding.FrameToScreen ()), RegionOp.Union);
        clipAdornments.Combine (originalClip, RegionOp.Intersect);
        SetClip (clipAdornments);

        if (Margin.View is { NeedsLayout: true } marginView)
        {
            marginView.NeedsLayout = false;

            if (Driver is { })
            {
                Margin.Thickness.Draw (Driver, FrameToScreen ());
            }

            SetSubViewNeedsDrawDownHierarchy ();
        }

        // When parent is drawing, always ensure adornment Views are marked for redraw.
        Border.View?.SetNeedsDraw ();
        Padding.View?.SetNeedsDraw ();
        Margin.View?.SetNeedsDraw ();

        // Ensure NeedsDraw is true for the rest of the draw pipeline (DoClearViewport, DoDrawText, etc.)
        // When adornment Views are null (lightweight), their NeedsDraw doesn't contribute to the parent's
        // NeedsDraw property. But if we're here, the parent IS drawing, so we must set NeedsDrawRect.
        if (NeedsDrawRect == Rectangle.Empty)
        {
            NeedsDrawRect = Viewport;
        }

        if (OnDrawingAdornments ())
        {
            return;
        }

        // TODO: add event.

        DrawAdornments ();
    }

    /// <summary>
    ///     Causes <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/> to be drawn.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Margin"/> is drawn in a separate pass if <see cref="ShadowStyle"/> is set.
    ///     </para>
    /// </remarks>
    public void DrawAdornments ()
    {
        // Only draw Margin here if it is not Transparent. Transparent Margins are drawn in a separate pass
        // in the static View.Draw via MarginView.DrawMargins (designed for shadow compositing).
        // Non-shadow transparent margin rendering is not yet supported in the first pass.
        if (!Margin.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent) && Margin.Thickness != Thickness.Empty)
        {
            if (Margin.View is { } marginView)
            {
                DrawContext marginContext = new ();
                marginView.Draw (marginContext);
                Margin.LastDrawnRegion = marginContext.GetDrawnRegion ().Clone ();
            }
            else if (Margin.Thickness != Thickness.Empty)
            {
                Margin.Thickness.Draw (Driver, Margin.FrameToScreen (), Margin.Diagnostics);
                Margin.LastDrawnRegion = null;
            }
        }
        else
        {
            Margin.LastDrawnRegion = null;
        }

        // Each of these renders lines to this View's LineCanvas
        // Those lines will be finally rendered in OnRenderLineCanvas
        if (Border.Thickness != Thickness.Empty)
        {
            if (Border.View is { } borderView)
            {
                DrawContext borderContext = new ();
                borderView.Draw (borderContext);
                Border.LastDrawnRegion = borderContext.GetDrawnRegion ().Clone ();
            }
            else if (Border.Thickness != Thickness.Empty)
            {
                Border.Thickness.Draw (Driver, Border.FrameToScreen (), Border.Diagnostics);
                Border.LastDrawnRegion = null;
            }
        }
        else
        {
            Border.LastDrawnRegion = null;
        }

        if (Padding.Thickness != Thickness.Empty)
        {
            if (Padding.View is { } paddingView)
            {
                DrawContext paddingContext = new ();
                paddingView.Draw (paddingContext);
                Padding.LastDrawnRegion = paddingContext.GetDrawnRegion ().Clone ();
            }
            else if (Padding.Thickness != Thickness.Empty)
            {
                Padding.Thickness.Draw (Driver, Padding.FrameToScreen (), Padding.Diagnostics);
                Padding.LastDrawnRegion = null;
            }
        }
        else
        {
            Padding.LastDrawnRegion = null;
        }

        if (Margin.Thickness != Thickness.Empty /* && Margin.ShadowStyle == ShadowStyle.None*/)
        {
            //Margin.Draw ();
        }
    }

    /// <summary>
    ///     Called when the View's Adornments are to be drawn. Prepares <see cref="View.LineCanvas"/>. If
    ///     <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's SubViews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> be prepared to be rendered.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of the Adornments.</returns>
    protected virtual bool OnDrawingAdornments () => false;
}
