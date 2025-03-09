#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

public partial class View // Drawing APIs
{
    /// <summary>
    ///     Draws a set of views.
    /// </summary>
    /// <param name="views">The peer views to draw.</param>
    /// <param name="force">If <see langword="true"/>, <see cref="View.SetNeedsDraw()"/> will be called on each view to force it to be drawn.</param>
    internal static void Draw (IEnumerable<View> views, bool force)
    {
        IEnumerable<View> viewsArray = views as View [] ?? views.ToArray ();

        // The draw context is used to track the region drawn by each view.
        DrawContext context = new DrawContext ();

        foreach (View view in viewsArray)
        {
            if (force)
            {
                view.SetNeedsDraw ();
            }

            view.Draw (context);
        }

        Margin.DrawMargins (viewsArray);
    }

    /// <summary>
    ///     Draws the view if it needs to be drawn.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The view will only be drawn if it is visible, and has any of <see cref="NeedsDraw"/>,
    ///         <see cref="SubViewNeedsDraw"/>,
    ///         or <see cref="NeedsLayout"/> set.
    ///     </para>
    ///     <para>
    ///         See the View Drawing Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/drawing.html"/>.
    ///     </para>
    /// </remarks>
    public void Draw (DrawContext? context = null)
    {
        if (!CanBeVisible (this))
        {
            return;
        }
        Region? originalClip = GetClip ();

        // TODO: This can be further optimized by checking NeedsDraw below and only
        // TODO: clearing, drawing text, drawing content, etc. if it is true.
        if (NeedsDraw || SubViewNeedsDraw)
        {
            // ------------------------------------
            // Draw the Border and Padding.
            // Note Margin is special-cased and drawn in a separate pass to support
            // transparent shadows.
            DoDrawBorderAndPadding (originalClip);
            SetClip (originalClip);

            // ------------------------------------
            // Clear the Viewport
            // By default, we clip to the viewport preventing drawing outside the viewport
            // We also clip to the content, but if a developer wants to draw outside the viewport, they can do
            // so via settings. SetClip honors the ViewportSettings.DisableVisibleContentClipping flag.
            // Get our Viewport in screen coordinates
            originalClip = AddViewportToClip ();

            // If no context ...
            context ??= new DrawContext ();

            // TODO: Simplify/optimize SetAttribute system.
            DoSetAttribute ();
            DoClearViewport ();

            // ------------------------------------
            // Draw the subviews first (order matters: Subviews, Text, Content)
            if (SubViewNeedsDraw)
            {
                DoSetAttribute ();
                DoDrawSubviews (context);
            }

            // ------------------------------------
            // Draw the text
            DoSetAttribute ();
            DoDrawText (context);

            // ------------------------------------
            // Draw the content
            DoSetAttribute ();
            DoDrawContent (context);

            // ------------------------------------
            // Draw the line canvas
            // Restore the clip before rendering the line canvas and adornment subviews
            // because they may draw outside the viewport.
            SetClip (originalClip);
            originalClip = AddFrameToClip ();
            DoRenderLineCanvas ();

            // ------------------------------------
            // Re-draw the border and padding subviews
            // HACK: This is a hack to ensure that the border and padding subviews are drawn after the line canvas.
            DoDrawBorderAndPaddingSubViews ();

            // ------------------------------------
            // Advance the diagnostics draw indicator
            Border?.AdvanceDrawIndicator ();

            ClearNeedsDraw ();
        }

        // ------------------------------------
        // This causes the Margin to be drawn in a second pass
        // PERFORMANCE: If there is a Margin, it will be redrawn each iteration of the main loop.
        Margin?.CacheClip ();

        // ------------------------------------
        // Reset the clip to what it was when we started
        SetClip (originalClip);

        // ------------------------------------
        // We're done drawing - The Clip is reset to what it was before we started.
        DoDrawComplete (context);
    }

    #region DrawAdornments

    private void DoDrawBorderAndPaddingSubViews ()
    {
        if (Border?.Subviews is { } && Border.Thickness != Thickness.Empty)
        {
            // PERFORMANCE: Get the check for DrawIndicator out of this somehow.
            foreach (View subview in Border.Subviews.Where (v => v.Visible || v.Id == "DrawIndicator"))
            {
                if (subview.Id != "DrawIndicator")
                {
                    subview.SetNeedsDraw ();
                }

                LineCanvas.Exclude (new (subview.FrameToScreen ()));
            }

            Region? saved = Border?.AddFrameToClip ();
            Border?.DoDrawSubviews ();
            SetClip (saved);
        }

        if (Padding?.Subviews is { } && Padding.Thickness != Thickness.Empty)
        {
            foreach (View subview in Padding.Subviews)
            {
                subview.SetNeedsDraw ();
            }

            Region? saved = Padding?.AddFrameToClip ();
            Padding?.DoDrawSubviews ();
            SetClip (saved);
        }
    }

    private void DoDrawBorderAndPadding (Region? originalClip)
    {
        if (this is Adornment)
        {
            AddFrameToClip ();
        }
        else
        {
            // Set the clip to be just the thicknesses of the adornments
            // TODO: Put this union logic in a method on View? 
            Region? clipAdornments = Margin!.Thickness.AsRegion (Margin!.FrameToScreen ());
            clipAdornments?.Combine (Border!.Thickness.AsRegion (Border!.FrameToScreen ()), RegionOp.Union);
            clipAdornments?.Combine (Padding!.Thickness.AsRegion (Padding!.FrameToScreen ()), RegionOp.Union);
            clipAdornments?.Combine (originalClip, RegionOp.Intersect);
            SetClip (clipAdornments);
        }

        if (Margin?.NeedsLayout == true)
        {
            Margin.NeedsLayout = false;
            // BUGBUG: This should not use ClearFrame as that clears the insides too
            Margin?.ClearFrame ();
            Margin?.Parent?.SetSubViewNeedsDraw ();
        }

        if (SubViewNeedsDraw)
        {
            // A Subview may add to the LineCanvas. This ensures any Adornment LineCanvas updates happen.
            Border?.SetNeedsDraw ();
            Padding?.SetNeedsDraw ();
        }

        if (OnDrawingBorderAndPadding ())
        {
            return;
        }

        // TODO: add event.

        DrawBorderAndPadding ();
    }

    /// <summary>
    ///     Causes <see cref="Border"/> and <see cref="Padding"/> to be drawn.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Margin"/> is drawn in a separate pass.
    ///     </para>
    /// </remarks>
    public void DrawBorderAndPadding ()
    {
        // We do not attempt to draw Margin. It is drawn in a separate pass.

        // Each of these renders lines to this View's LineCanvas 
        // Those lines will be finally rendered in OnRenderLineCanvas
        if (Border is { } && Border.Thickness != Thickness.Empty)
        {
            Border?.Draw ();
        }

        if (Padding is { } && Padding.Thickness != Thickness.Empty)
        {
            Padding?.Draw ();
        }

    }

    private void ClearFrame ()
    {
        if (Driver is null)
        {
            return;
        }

        // Get screen-relative coords
        Rectangle toClear = FrameToScreen ();

        Attribute prev = SetAttribute (GetNormalColor ());
        Driver.FillRect (toClear);
        SetAttribute (prev);
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Called when the View's Adornments are to be drawn. Prepares <see cref="View.LineCanvas"/>. If
    ///     <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> be prepared to be rendered.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of the Adornments.</returns>
    protected virtual bool OnDrawingBorderAndPadding () { return false; }

    #endregion DrawAdornments

    #region SetAttribute

    private void DoSetAttribute ()
    {
        if (OnSettingAttribute ())
        {
            return;
        }

        var args = new CancelEventArgs ();
        SettingAttribute?.Invoke (this, args);

        if (args.Cancel)
        {
            return;
        }

        SetNormalAttribute ();
    }

    /// <summary>
    ///     Called when the normal attribute for the View is to be set. This is called before the View is drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop default behavior.</returns>
    protected virtual bool OnSettingAttribute () { return false; }

    /// <summary>Raised  when the normal attribute for the View is to be set. This is raised before the View is drawn.</summary>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop default behavior.
    /// </returns>
    public event EventHandler<CancelEventArgs>? SettingAttribute;

    /// <summary>
    ///     Sets the attribute for the View. This is called before the View is drawn.
    /// </summary>
    public void SetNormalAttribute ()
    {
        if (ColorScheme is { })
        {
            SetAttribute (GetNormalColor ());
        }
    }

    #endregion

    #region ClearViewport

    internal void DoClearViewport ()
    {
        if (ViewportSettings.HasFlag (ViewportSettings.Transparent))
        {
            return;
        }

        if (OnClearingViewport ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty, null);
        ClearingViewport?.Invoke (this, dev);

        if (dev.Cancel)
        {
            SetNeedsDraw ();
            return;
        }

        ClearViewport ();

        OnClearedViewport ();
        ClearedViewport?.Invoke (this, new (Viewport, Viewport, null));
    }

    /// <summary>
    ///     Called when the <see cref="Viewport"/> is to be cleared.
    /// </summary>
    /// <returns><see langword="true"/> to stop further clearing.</returns>
    protected virtual bool OnClearingViewport () { return false; }

    /// <summary>Event invoked when the <see cref="Viewport"/> is to be cleared.</summary>
    /// <remarks>
    ///     <para>Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.</para>
    ///     <para>
    ///         Rect provides the view-relative rectangle describing the currently visible viewport into the
    ///         <see cref="View"/> .
    ///     </para>
    /// </remarks>
    public event EventHandler<DrawEventArgs>? ClearingViewport;

    /// <summary>
    ///     Called when the <see cref="Viewport"/> has been cleared
    /// </summary>
    protected virtual void OnClearedViewport () { }

    /// <summary>Event invoked when the <see cref="Viewport"/> has been cleared.</summary>
    public event EventHandler<DrawEventArgs>? ClearedViewport;

    /// <summary>Clears <see cref="Viewport"/> with the normal background.</summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="ViewportSettings"/> has <see cref="Gui.ViewportSettings.ClearContentOnly"/> only
    ///         the portion of the content
    ///         area that is visible within the <see cref="View.Viewport"/> will be cleared. This is useful for views that have
    ///         a
    ///         content area larger than the Viewport (e.g. when <see cref="ViewportSettings.AllowNegativeLocation"/> is
    ///         enabled) and want
    ///         the area outside the content to be visually distinct.
    ///     </para>
    /// </remarks>
    public void ClearViewport ()
    {
        if (Driver is null)
        {
            return;
        }

        // Get screen-relative coords
        Rectangle toClear = ViewportToScreen (Viewport with { Location = new (0, 0) });

        if (ViewportSettings.HasFlag (ViewportSettings.ClearContentOnly))
        {
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new (-Viewport.X, -Viewport.Y), GetContentSize ()));
            toClear = Rectangle.Intersect (toClear, visibleContent);
        }

        Attribute prev = SetAttribute (GetNormalColor ());
        Driver.FillRect (toClear);
        SetAttribute (prev);
        SetNeedsDraw ();
    }

    #endregion ClearViewport

    #region DrawText

    private void DoDrawText (DrawContext? context = null)
    {
        if (OnDrawingText (context))
        {
            return;
        }

        if (OnDrawingText ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty, context);
        DrawingText?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        DrawText (context);
    }

    /// <summary>
    ///     Called when the <see cref="Text"/> of the View is to be drawn.
    /// </summary>
    /// <param name="context">The draw context to report drawn areas to.</param>
    /// <returns><see langword="true"/> to stop further drawing of  <see cref="Text"/>.</returns>
    protected virtual bool OnDrawingText (DrawContext? context) { return false; }

    /// <summary>
    ///     Called when the <see cref="Text"/> of the View is to be drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of  <see cref="Text"/>.</returns>
    protected virtual bool OnDrawingText () { return false; }

    /// <summary>Raised when the <see cref="Text"/> of the View is to be drawn.</summary>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop further drawing of
    ///     <see cref="Text"/>.
    /// </returns>
    public event EventHandler<DrawEventArgs>? DrawingText;

    /// <summary>
    ///     Draws the <see cref="Text"/> of the View using the <see cref="TextFormatter"/>.
    /// </summary>
    /// <param name="context">The draw context to report drawn areas to.</param>
    public void DrawText (DrawContext? context = null)
    {
        if (!string.IsNullOrEmpty (TextFormatter.Text))
        {
            TextFormatter.NeedsFormat = true;
        }

        var drawRect = new Rectangle (ContentToScreen (Point.Empty), GetContentSize ());

        // Use GetDrawRegion to get precise drawn areas
        Region textRegion = TextFormatter.GetDrawRegion (drawRect);

        // Report the drawn area to the context
        context?.AddDrawnRegion (textRegion);

        if (!NeedsDraw)
        {
            return;
        }

        TextFormatter?.Draw (
                             drawRect,
                             HasFocus ? GetFocusColor () : GetNormalColor (),
                             HasFocus ? GetHotFocusColor () : GetHotNormalColor (),
                             Rectangle.Empty
                            );

        // We assume that the text has been drawn over the entire area; ensure that the subviews are redrawn.
        SetSubViewNeedsDraw ();
    }

    #endregion DrawText
    #region DrawContent

    private void DoDrawContent (DrawContext? context = null)
    {
        if (OnDrawingContent (context))
        {
            return;
        }

        // TODO: Upgrade all overrides of OnDrawingContent to use DrawContext and remove this override
        if (OnDrawingContent ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty, context);
        DrawingContent?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        // No default drawing; let event handlers or overrides handle it
    }

    /// <summary>
    ///     Called when the View's content is to be drawn. The default implementation does nothing.
    /// </summary>
    /// <param name="context">The draw context to report drawn areas to.</param>
    /// <returns><see langword="true"/> to stop further drawing content.</returns>
    protected virtual bool OnDrawingContent (DrawContext? context) { return false; }

    /// <summary>
    ///     Called when the View's content is to be drawn. The default implementation does nothing.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing content.</returns>
    protected virtual bool OnDrawingContent () { return false; }

    /// <summary>Raised when the View's content is to be drawn.</summary>
    /// <remarks>
    ///     <para>Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.</para>
    ///     <para>
    ///         Rect provides the view-relative rectangle describing the currently visible viewport into the
    ///         <see cref="View"/> .
    ///     </para>
    /// </remarks>
    public event EventHandler<DrawEventArgs>? DrawingContent;

    #endregion DrawContent

    #region DrawSubviews

    private void DoDrawSubviews (DrawContext? context = null)
    {
        if (OnDrawingSubviews (context))
        {
            return;
        }

        if (OnDrawingSubviews ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty, context);
        DrawingSubviews?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        if (!SubViewNeedsDraw)
        {
            return;
        }

        DrawSubviews (context);
    }

    /// <summary>
    ///     Called when the <see cref="Subviews"/> are to be drawn.
    /// </summary>
    /// <param name="context">The draw context to report drawn areas to, or null if not tracking.</param>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="Subviews"/>.</returns>
    protected virtual bool OnDrawingSubviews (DrawContext? context) { return false; }

    /// <summary>
    ///     Called when the <see cref="Subviews"/> are to be drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="Subviews"/>.</returns>
    protected virtual bool OnDrawingSubviews () { return false; }

    /// <summary>Raised when the <see cref="Subviews"/> are to be drawn.</summary>
    /// <remarks>
    /// </remarks>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop further drawing of
    ///     <see cref="Subviews"/>.
    /// </returns>
    public event EventHandler<DrawEventArgs>? DrawingSubviews;

    /// <summary>
    ///     Draws the <see cref="Subviews"/>.
    /// </summary>
    /// <param name="context">The draw context to report drawn areas to, or null if not tracking.</param>
    public void DrawSubviews (DrawContext? context = null)
    {
        if (_subviews is null)
        {
            return;
        }

        // Draw the subviews in reverse order to leverage clipping.
        foreach (View view in _subviews.Where (view => view.Visible).Reverse ())
        {
            // TODO: HACK - This forcing of SetNeedsDraw with SuperViewRendersLineCanvas enables auto line join to work, but is brute force.
            if (view.SuperViewRendersLineCanvas || view.ViewportSettings.HasFlag (ViewportSettings.Transparent))
            {
                view.SetNeedsDraw ();
            }
            view.Draw (context);

            if (view.SuperViewRendersLineCanvas)
            {
                LineCanvas.Merge (view.LineCanvas);
                view.LineCanvas.Clear ();
            }
        }
    }

    #endregion DrawSubviews

    #region DrawLineCanvas

    private void DoRenderLineCanvas ()
    {
        if (OnRenderingLineCanvas ())
        {
            return;
        }

        // TODO: Add event

        RenderLineCanvas ();
    }

    /// <summary>
    ///     Called when the <see cref="View.LineCanvas"/> is to be rendered. See <see cref="RenderLineCanvas"/>.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="LineCanvas"/>.</returns>
    protected virtual bool OnRenderingLineCanvas () { return false; }

    /// <summary>The canvas that any line drawing that is to be shared by subviews of this view should add lines to.</summary>
    /// <remarks><see cref="Border"/> adds border lines to this LineCanvas.</remarks>
    public LineCanvas LineCanvas { get; } = new ();

    /// <summary>
    ///     Gets or sets whether this View will use it's SuperView's <see cref="LineCanvas"/> for rendering any
    ///     lines. If <see langword="true"/> the rendering of any borders drawn by this Frame will be done by its parent's
    ///     SuperView. If <see langword="false"/> (the default) this View's <see cref="OnDrawingBorderAndPadding"/> method will
    ///     be
    ///     called to render the borders.
    /// </summary>
    public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

    /// <summary>
    ///     Causes the contents of <see cref="LineCanvas"/> to be drawn.
    ///     If <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> to be rendered.
    /// </summary>
    public void RenderLineCanvas ()
    {
        if (Driver is null)
        {
            return;
        }

        if (!SuperViewRendersLineCanvas && LineCanvas.Bounds != Rectangle.Empty)
        {
            foreach (KeyValuePair<Point, Cell?> p in LineCanvas.GetCellMap ())
            {
                // Get the entire map
                if (p.Value is { })
                {
                    SetAttribute (p.Value.Value.Attribute ?? ColorScheme!.Normal);
                    Driver.Move (p.Key.X, p.Key.Y);

                    // TODO: #2616 - Support combining sequences that don't normalize
                    Driver.AddRune (p.Value.Value.Rune);
                }
            }

            LineCanvas.Clear ();
        }
    }

    #endregion DrawLineCanvas

    #region DrawComplete

    private void DoDrawComplete (DrawContext? context)
    {
        OnDrawComplete (context);
        DrawComplete?.Invoke (this, new (Viewport, Viewport, context));

        // Now, update the clip to exclude this view (not including Margin)
        if (this is not Adornment)
        {
            if (ViewportSettings.HasFlag (ViewportSettings.Transparent))
            {
                // context!.DrawnRegion is the region that was drawn by this view. It may include regions outside
                // the Viewport. We need to clip it to the Viewport.
                context!.ClipDrawnRegion (ViewportToScreen (Viewport));

                // Exclude the drawn region from the clip
                ExcludeFromClip (context!.GetDrawnRegion ());

                // Exclude the Border and Padding from the clip
                ExcludeFromClip (Border?.Thickness.AsRegion (FrameToScreen ()));
                ExcludeFromClip (Padding?.Thickness.AsRegion (FrameToScreen ()));
            }
            else
            {
                // Exclude this view (not including Margin) from the Clip
                Rectangle borderFrame = FrameToScreen ();

                if (Border is { })
                {
                    borderFrame = Border.FrameToScreen ();
                }

                // In the non-transparent (typical case), we want to exclude the entire view area (borderFrame) from the clip
                ExcludeFromClip (borderFrame);

                // Update context.DrawnRegion to include the entire view (borderFrame), but clipped to our SuperView's viewport
                // This enables the SuperView to know what was drawn by this view.
                context?.AddDrawnRectangle (borderFrame);
            }
        }

        // TODO: Determine if we need another event that conveys the FINAL DrawContext
    }

    /// <summary>
    ///     Called when the View is completed drawing.
    /// </summary>
    /// <remarks>
    ///     The <paramref name="context"/> parameter provides the drawn region of the View.
    /// </remarks>
    protected virtual void OnDrawComplete (DrawContext? context) { }

    /// <summary>Raised when the View is completed drawing.</summary>
    /// <remarks>
    /// </remarks>
    public event EventHandler<DrawEventArgs>? DrawComplete;

    #endregion DrawComplete

    #region NeedsDraw

    // TODO: Change NeedsDraw to use a Region instead of Rectangle
    // TODO: Make _needsDrawRect nullable instead of relying on Empty
    //      TODO: If null, it means ?
    //      TODO: If Empty, it means no need to redraw
    //      TODO: If not Empty, it means the region that needs to be redrawn
    // The viewport-relative region that needs to be redrawn. Marked internal for unit tests.
    internal Rectangle _needsDrawRect = Rectangle.Empty;

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
        // TODO: Figure out if we can decouple NeedsDraw from NeedsLayout.
        get => Visible && (_needsDrawRect != Rectangle.Empty || NeedsLayout);
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

    /// <summary>Gets whether any Subviews need to be redrawn.</summary>
    public bool SubViewNeedsDraw { get; private set; }

    /// <summary>Sets that the <see cref="Viewport"/> of this View needs to be redrawn.</summary>
    /// <remarks>
    ///     If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), this method
    ///     does nothing.
    /// </remarks>
    public void SetNeedsDraw ()
    {
        Rectangle viewport = Viewport;

        if (!Visible || (_needsDrawRect != Rectangle.Empty && viewport.IsEmpty))
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

        if (_needsDrawRect.IsEmpty)
        {
            _needsDrawRect = viewPortRelativeRegion;
        }
        else
        {
            int x = Math.Min (Viewport.X, viewPortRelativeRegion.X);
            int y = Math.Min (Viewport.Y, viewPortRelativeRegion.Y);
            int w = Math.Max (Viewport.Width, viewPortRelativeRegion.Width);
            int h = Math.Max (Viewport.Height, viewPortRelativeRegion.Height);
            _needsDrawRect = new (x, y, w, h);
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

        // There was multiple enumeration error here, so calling ToArray - probably a stop gap
        foreach (View subview in Subviews.ToArray ())
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
        _needsDrawRect = Rectangle.Empty;
        SubViewNeedsDraw = false;

        if (Margin is { } && Margin.Thickness != Thickness.Empty)
        {
            Margin?.ClearNeedsDraw ();
        }

        if (Border is { } && Border.Thickness != Thickness.Empty)
        {
            Border?.ClearNeedsDraw ();
        }

        if (Padding is { } && Padding.Thickness != Thickness.Empty)
        {
            Padding?.ClearNeedsDraw ();
        }

        foreach (View subview in Subviews)
        {
            subview.ClearNeedsDraw ();
        }

        if (SuperView is { })
        {
            SuperView.SubViewNeedsDraw = false;
        }

        // This ensures LineCanvas' get redrawn
        if (!SuperViewRendersLineCanvas)
        {
            LineCanvas.Clear ();
        }

    }

    #endregion NeedsDraw
}
