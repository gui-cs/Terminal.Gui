using System.ComponentModel;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.ViewBase;

public partial class View // Drawing APIs
{
    /// <summary>
    ///     Draws a set of peer views (views that share the same SuperView).
    /// </summary>
    /// <param name="views">The peer views to draw.</param>
    /// <param name="force">
    ///     If <see langword="true"/>, <see cref="View.SetNeedsDraw()"/> will be called on each view to force
    ///     it to be drawn.
    /// </param>
    internal static void Draw (IEnumerable<View> views, bool force)
    {
        // **Snapshot once** — every recursion level gets its own frozen array
        View [] viewsArray = views.Snapshot ();

        // The draw context is used to track the region drawn by each view.
        var context = new DrawContext ();

        foreach (View view in viewsArray)
        {
            if (force)
            {
                view.SetNeedsDraw ();
            }

            Trace.Draw (view.ToIdentifyingString (), "Draw", $"force={force}");
            view.Draw (context);
        }

        // Draw Transparent margins last to ensure they are drawn on top of the content.
        MarginView.DrawMargins (viewsArray);

        // DrawMargins may have caused some views have NeedsDraw/NeedsSubViewDraw set; clear them all.
        foreach (View view in viewsArray)
        {
            view.ClearNeedsDraw ();
        }

        // After all peer views have been drawn and cleared, we can now clear the SuperView's SubViewNeedsDraw flag.
        // ClearNeedsDraw() does not clear SuperView.SubViewNeedsDraw (by design, to avoid premature clearing
        // when peer subviews still need drawing), so we must do it here after ALL peers are processed.
        // We only clear the flag if ALL the SuperView's SubViews no longer need drawing.
        View? lastSuperView = null;

        foreach (View view in viewsArray)
        {
            if (view is AdornmentView || view.SuperView is null || view.SuperView == lastSuperView)
            {
                continue;
            }

            // Check if ANY subview of this SuperView still needs drawing
            bool anySubViewNeedsDrawing = view.SuperView.InternalSubViews.Any (v => v.NeedsDraw || v.SubViewNeedsDraw);

            if (!anySubViewNeedsDrawing)
            {
                view.SuperView.SubViewNeedsDraw = false;
            }

            lastSuperView = view.SuperView;
        }
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
    ///         See the View Drawing Deep Dive for more information:
    ///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/drawing.html"/>.
    ///     </para>
    /// </remarks>
    public void Draw (DrawContext? context = null)
    {
        if (!CanBeVisible (this))
        {
            return;
        }

        Trace.Draw (this.ToIdentifyingString (), "Start", $"NeedsDraw={NeedsDraw}, SubViewNeedsDraw={SubViewNeedsDraw}");

        Region? originalClip = GetClip ();

        // TODO: This can be further optimized by checking NeedsDraw below and only
        // TODO: clearing, drawing text, drawing content, etc. if it is true.
        if (NeedsDraw || SubViewNeedsDraw)
        {
            // ------------------------------------
            // Draw the Border and Padding Adornments.
            // Note: Margin with a Shadow is special-cased and drawn in a separate pass to support
            // transparent shadows.
            Trace.Draw (this.ToIdentifyingString (), "Adornments");
            DoDrawAdornments (originalClip);
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

            // Per-view context tracks only what THIS view draws (text + content).
            // Used for CachedDrawnRegion (TransparentMouse hit-testing) so that a
            // transparent view's hit region reflects only its own draws, not its
            // SuperView's ClearViewport or peer SubViews' content.
            // This follows the same pattern as DrawAdornments(), which creates
            // per-adornment DrawContexts for the same reason.
            _localDrawContext = new DrawContext ();

            SetAttributeForRole (Enabled ? VisualRole.Normal : VisualRole.Disabled);
            DoClearViewport (context);

            // ------------------------------------
            // Draw the SubViews first (order matters: SubViews, Text, Content)
            if (SubViewNeedsDraw)
            {
                Trace.Draw (this.ToIdentifyingString (), "SubViews");
                DoDrawSubViews (context);
            }

            // Add the ClearViewport rect to the shared context AFTER SubViews have drawn.
            // This ensures SubViews' DoDrawComplete doesn't see the SuperView's cleared area
            // in the context (which would over-exclude for overlapping views like Tabs).
            if (_lastClearedViewport is { } clearedRect)
            {
                context.AddDrawnRectangle (clearedRect);
                _lastClearedViewport = null;
            }

            // ------------------------------------
            // Draw the text — tracked in both shared (clip exclusion) and local (hit-testing) contexts
            Trace.Draw (this.ToIdentifyingString (), "Text");
            SetAttributeForRole (Enabled ? VisualRole.Normal : VisualRole.Disabled);
            DoDrawText (_localDrawContext);

            // ------------------------------------
            // Draw the content — tracked in both shared (clip exclusion) and local (hit-testing) contexts
            Trace.Draw (this.ToIdentifyingString (), "Content");
            DoDrawContent (_localDrawContext);

            // Merge this view's own draws into the shared context so the SuperView
            // can track the aggregate for clip exclusion.
            context.AddDrawnRegion (_localDrawContext.GetDrawnRegion ());

            // ------------------------------------
            // Draw adornment SubViews BEFORE rendering LineCanvas so their lines
            // (merged via LineCanvas.Merge) participate in auto-join.
            // Restore the clip because adornment subviews may draw outside the viewport.
            SetClip (originalClip);
            originalClip = AddFrameToClip ();
            Trace.Draw (this.ToIdentifyingString (), "AdornmentSubViews");
            DoDrawAdornmentsSubViews (context);

            // ------------------------------------
            // Draw the line canvas (includes merged lines from adornment SubViews)
            Trace.Draw (this.ToIdentifyingString (), "LineCanvas");
            DoRenderLineCanvas (context);

            // ------------------------------------
            // Advance the diagnostics draw indicator
            (Border.View as BorderView)?.AdvanceDrawIndicator ();

            ClearNeedsDraw ();
        }

        // ------------------------------------
        // This causes the Margin to be drawn in a second pass if it has a ShadowStyle
        (Margin.View as MarginView)?.CacheClip ();

        // ------------------------------------
        // Reset the clip to what it was when we started
        SetClip (originalClip);

        // ------------------------------------
        // We're done drawing - The Clip is reset to what it was before we started
        // But the context contains the region that was drawn by this view
        DoDrawComplete (context);

        Trace.Draw (this.ToIdentifyingString (), "End");

        // When DoDrawComplete returns, Driver.Clip has been updated to exclude this view's area.
        // The next view drawn (earlier in Z-order, typically a peer view or the SuperView) will see
        // a clip with "holes" where this view (and any SubViews drawn before it) are located.
    }

    // DrawAdornments region (DoDrawAdornmentsSubViews, DoDrawAdornments, DrawAdornments,
    // OnDrawingAdornments) is in View.Drawing.Adornments.cs.

    #region ClearViewport

    internal void DoClearViewport (DrawContext? context = null)
    {
        if (!NeedsDraw || ViewportSettings.FastHasFlags (ViewportSettingsFlags.Transparent) || OnClearingViewport ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty, context);
        ClearingViewport?.Invoke (this, dev);

        if (dev.Cancel)
        {
            // BUGBUG: We should add the Viewport to context.DrawRegion here?
            SetNeedsDraw ();

            return;
        }

        ClearViewport (context);
        OnClearedViewport ();
        ClearedViewport?.Invoke (this, new DrawEventArgs (Viewport, Viewport, null));
    }

    /// <summary>
    ///     Called when the <see cref="Viewport"/> is to be cleared.
    /// </summary>
    /// <returns><see langword="true"/> to stop further clearing.</returns>
    protected virtual bool OnClearingViewport () => false;

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
    ///         If <see cref="ViewportSettings"/> has <see cref="ViewBase.ViewportSettingsFlags.ClearContentOnly"/> only
    ///         the portion of the content
    ///         area that is visible within the <see cref="View.Viewport"/> will be cleared. This is useful for views that have
    ///         a
    ///         content area larger than the Viewport (e.g. when <see cref="ViewportSettingsFlags.AllowNegativeLocation"/> is
    ///         enabled) and want
    ///         the area outside the content to be visually distinct.
    ///     </para>
    /// </remarks>
    public void ClearViewport (DrawContext? context = null)
    {
        if (Driver is null)
        {
            return;
        }

        // Get screen-relative coords
        Rectangle toClear = ViewportToScreen (Viewport with { Location = new Point (0, 0) });

        if (ViewportSettings.FastHasFlags (ViewportSettingsFlags.ClearContentOnly))
        {
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new Point (-Viewport.X, -Viewport.Y), GetContentSize ()));
            toClear = Rectangle.Intersect (toClear, visibleContent);
        }

        Driver.FillRect (toClear);

        // NOTE: ClearViewport does NOT add to the context here. The cleared viewport rect is
        // added to the shared context in Draw() AFTER DoDrawSubViews completes. This prevents
        // the SuperView's ClearViewport from leaking into SubViews' DoDrawComplete exclusion
        // calculations — which would cause overlapping SubViews (e.g., Tab views that all use
        // Dim.Fill) to exclude the entire frame and prevent peer SubViews from drawing.
        _lastClearedViewport = toClear;

        SetNeedsDraw ();
    }

    /// <summary>
    ///     Stores the last viewport rectangle cleared by <see cref="ClearViewport"/>. Added to the shared
    ///     <see cref="DrawContext"/> in <see cref="Draw(DrawContext?)"/> after SubViews have drawn, so that
    ///     SubViews' <see cref="DoDrawComplete"/> doesn't see the SuperView's cleared area in the context.
    /// </summary>
    private Rectangle? _lastClearedViewport;

    #endregion ClearViewport

    #region DrawText

    private void DoDrawText (DrawContext? context = null)
    {
        if (!NeedsDraw)
        {
            return;
        }

        if (!string.IsNullOrEmpty (TextFormatter.Text))
        {
            TextFormatter.NeedsFormat = true;
        }

        if (OnDrawingText (context))
        {
            return;
        }

        // TODO: Get rid of this vf in lieu of the one above
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

        OnDrewText ();
        DrewText?.Invoke (this, EventArgs.Empty);
    }

    /// <summary>
    ///     Called when the <see cref="Text"/> of the View is to be drawn.
    /// </summary>
    /// <param name="context">The draw context to report drawn areas to.</param>
    /// <returns><see langword="true"/> to stop further drawing of  <see cref="Text"/>.</returns>
    protected virtual bool OnDrawingText (DrawContext? context) => false;

    /// <summary>
    ///     Called when the <see cref="Text"/> of the View is to be drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of  <see cref="Text"/>.</returns>
    protected virtual bool OnDrawingText () => false;

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
        // BUGBUG: This excludes the draw region even if there's no text
        var drawRect = new Rectangle (ContentToScreen (Point.Empty), GetContentSize ());

        // Use GetDrawRegion to get precise drawn areas
        Region textRegion = TextFormatter.GetDrawRegion (drawRect);

        // Report the drawn area to the context
        context?.AddDrawnRegion (textRegion);

        if (Driver is { })
        {
            TextFormatter.Draw (Driver,
                                drawRect,
                                HasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal),
                                HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal),
                                Rectangle.Empty);
        }

        // We assume that the text has been drawn over the entire area; ensure that the SubViews are redrawn.
        SetSubViewNeedsDrawDownHierarchy ();
    }

    /// <summary>
    ///     Called when the <see cref="Text"/> of the View has been drawn.
    /// </summary>
    protected virtual void OnDrewText () { }

    /// <summary>Raised when the <see cref="Text"/> of the View has been drawn.</summary>
    public event EventHandler? DrewText;

    #endregion DrawText

    #region DrawContent

    private void DoDrawContent (DrawContext? context = null)
    {
        if (!NeedsDraw || OnDrawingContent (context))
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty, context);
        DrawingContent?.Invoke (this, dev);

        if (dev.Cancel)
        { }

        // No default drawing; let event handlers or overrides handle it
    }

    /// <summary>
    ///     Called when the View's content is to be drawn. The default implementation does nothing.
    /// </summary>
    /// <param name="context">The draw context to report drawn areas to.</param>
    /// <returns><see langword="true"/> to stop further drawing content.</returns>
    /// <remarks>
    ///     <para>
    ///         Override this method to draw custom content for your View.
    ///     </para>
    ///     <para>
    ///         <b>Transparency Support:</b> If your View has <see cref="ViewportSettings"/> with
    ///         <see cref="ViewportSettingsFlags.Transparent"/>
    ///         set, you should report the exact regions you draw to via the <paramref name="context"/> parameter. This allows
    ///         the transparency system to exclude only the drawn areas from the clip region, letting views beneath show
    ///         through
    ///         in the areas you didn't draw.
    ///     </para>
    ///     <para>
    ///         Use <see cref="DrawContext.AddDrawnRectangle"/> for simple rectangular areas, or
    ///         <see cref="DrawContext.AddDrawnRegion"/>
    ///         for complex, non-rectangular shapes. All coordinates passed to these methods must be in
    ///         <b>screen-relative coordinates</b>.
    ///         Use <see cref="View.ViewportToScreen(in Rectangle)"/> or <see cref="View.ContentToScreen(in Point)"/> to
    ///         convert from
    ///         viewport-relative or content-relative coordinates.
    ///     </para>
    ///     <para>
    ///         Example of drawing custom content with transparency support:
    ///     </para>
    ///     <code>
    ///         protected override bool OnDrawingContent (DrawContext? context)
    ///         {
    ///             base.OnDrawingContent (context);
    ///             
    ///             // Draw content in viewport-relative coordinates
    ///             Rectangle rect1 = new Rectangle (5, 5, 10, 3);
    ///             Rectangle rect2 = new Rectangle (8, 8, 4, 7);
    ///             FillRect (rect1, Glyphs.BlackCircle);
    ///             FillRect (rect2, Glyphs.BlackCircle);
    ///             
    ///             // Report drawn region in screen-relative coordinates for transparency
    ///             if (ViewportSettings.FastHasFlags (ViewportSettingsFlags.Transparent))
    ///             {
    ///                 Region drawnRegion = new Region (ViewportToScreen (rect1));
    ///                 drawnRegion.Union (ViewportToScreen (rect2));
    ///                 context?.AddDrawnRegion (drawnRegion);
    ///             }
    ///             
    ///             return true;
    ///         }
    ///     </code>
    /// </remarks>
    protected virtual bool OnDrawingContent (DrawContext? context) => false;

    /// <summary>Raised when the View's content is to be drawn.</summary>
    /// <remarks>
    ///     <para>
    ///         Subscribe to this event to draw custom content for the View. Use the drawing methods available on
    ///         <see cref="View"/>
    ///         such as <see cref="View.AddRune(int, int, Rune)"/>, <see cref="View.AddStr(string)"/>, and
    ///         <see cref="View.FillRect(Rectangle, Rune)"/>.
    ///     </para>
    ///     <para>
    ///         The event is invoked after <see cref="ClearingViewport"/> and <see cref="Text"/> have been drawn, but after
    ///         <see cref="SubViews"/> have been drawn.
    ///     </para>
    ///     <para>
    ///         <b>Transparency Support:</b> If the View has <see cref="ViewportSettings"/> with
    ///         <see cref="ViewportSettingsFlags.Transparent"/>
    ///         set, use the <see cref="DrawEventArgs.DrawContext"/> to report which areas were actually drawn. This enables
    ///         proper transparency
    ///         by excluding only the drawn areas from the clip region. See <see cref="DrawContext"/> for details on reporting
    ///         drawn regions.
    ///     </para>
    ///     <para>
    ///         The <see cref="DrawEventArgs.NewViewport"/> property provides the view-relative rectangle describing the
    ///         currently visible viewport into the View.
    ///     </para>
    /// </remarks>
    public event EventHandler<DrawEventArgs>? DrawingContent;

    #endregion DrawContent

    #region DrawSubViews

    internal void DoDrawSubViews (DrawContext? context = null)
    {
        if (!NeedsDraw || OnDrawingSubViews (context))
        {
            return;
        }

        // TODO: Get rid of this vf in lieu of the one above
        if (OnDrawingSubViews ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty, context);
        DrawingSubViews?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        if (!SubViewNeedsDraw)
        {
            return;
        }

        DrawSubViews (context);
    }

    /// <summary>
    ///     Called when the <see cref="SubViews"/> are to be drawn.
    /// </summary>
    /// <param name="context">The draw context to report drawn areas to, or null if not tracking.</param>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="SubViews"/>.</returns>
    protected virtual bool OnDrawingSubViews (DrawContext? context) => false;

    /// <summary>
    ///     Called when the <see cref="SubViews"/> are to be drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="SubViews"/>.</returns>
    protected virtual bool OnDrawingSubViews () => false;

    /// <summary>Raised when the <see cref="SubViews"/> are to be drawn.</summary>
    /// <remarks>
    /// </remarks>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop further drawing of
    ///     <see cref="SubViews"/>.
    /// </returns>
    public event EventHandler<DrawEventArgs>? DrawingSubViews;

    /// <summary>
    ///     Draws the <see cref="SubViews"/>.
    /// </summary>
    /// <param name="context">The draw context to report drawn areas to, or null if not tracking.</param>
    public void DrawSubViews (DrawContext? context = null)
    {
        if (InternalSubViews.Count == 0)
        {
            return;
        }

        List<(Dictionary<Point, Cell?> CellMap, HashSet<Point>? Reserved)>? overlappedCellMaps = null;

        // Draw the SubViews in reverse Z-order to leverage clipping.
        // SubViews earlier in the collection are drawn last (on top).
        // NOTE: Do not use SubViews or GetSubViews() here as GetSubViews can be overridden to return a different set of views or ordering.
        // NOTE: We need to draw exactly the views in InternalSubViews.
        foreach (View view in InternalSubViews.Snapshot ().Where (v => v.Visible).Reverse ())
        {
            view.Draw (context);

            if (!view.SuperViewRendersLineCanvas)
            {
                continue;
            }

            if (view.Arrangement.FastHasFlags (ViewArrangement.Overlapped))
            {
                // Overlapped views: resolve independently so their lines don't
                // auto-join with other Z-levels. Deferred for painters' algorithm
                // compositing in RenderLineCanvas.
                Dictionary<Point, Cell?>? resolvedCells = null;

                if (view.LineCanvas.Bounds != Rectangle.Empty)
                {
                    resolvedCells = view.LineCanvas.GetCellMap ();
                }

                overlappedCellMaps ??= [];
                overlappedCellMaps.Add ((resolvedCells ?? [], view.LineCanvas.GetReservedCells ()));
            }
            else
            {
                // Tiled views: flat merge preserves cross-view auto-join.
                LineCanvas.Merge (view.LineCanvas);
            }
            view.LineCanvas.Clear ();
        }

        // Store for compositing during RenderLineCanvas.
        // List is ordered highest-Z first (matching the iteration order above).
        _pendingOverlappedCellMaps = overlappedCellMaps;
    }

    #endregion DrawSubViews

    #region DrawLineCanvas

    private void DoRenderLineCanvas (DrawContext? context)
    {
        // TODO: Add context to OnRenderingLineCanvas
        if (!NeedsDraw || OnRenderingLineCanvas ())
        {
            return;
        }

        // TODO: Add event

        RenderLineCanvas (context);
    }

    #endregion DrawLineCanvas

    #region DrawComplete

    /// <summary>
    ///     Gets the cached drawn region from the last draw pass. Populated during
    ///     <see cref="DoDrawComplete"/> for views with <see cref="ViewportSettingsFlags.TransparentMouse"/> set.
    ///     Used by mouse hit-testing to determine which cells should receive mouse events.
    ///     Returns <see langword="null"/> if not drawn yet or TransparentMouse not set.
    ///     Invalidated by <see cref="SetNeedsDraw()"/>.
    /// </summary>
    internal Region? CachedDrawnRegion { get; set; }

    /// <summary>
    ///     The line canvas region from the last <see cref="DoRenderLineCanvas"/> call. Used to build
    ///     <see cref="CachedDrawnRegion"/> for the Border adornment (which draws via merged LineCanvas).
    /// </summary>
    private Region? _lastLineCanvasRegion;

    /// <summary>
    ///     Cell maps from overlapped SubViews' independently-resolved <see cref="LineCanvas"/>es,
    ///     collected during <see cref="DrawSubViews"/>. Composited via painters' algorithm
    ///     in <see cref="RenderLineCanvas"/>: highest-Z cells take priority over lower-Z cells
    ///     at the same screen position. Each entry also includes reserved cells (intentional gaps)
    ///     that suppress lower-Z cells without rendering anything.
    ///     Ordered highest-Z first (matching DrawSubViews iteration order).
    /// </summary>
    private List<(Dictionary<Point, Cell?> CellMap, HashSet<Point>? Reserved)>? _pendingOverlappedCellMaps;

    /// <summary>
    ///     Per-view <see cref="DrawContext"/> that tracks only what THIS view drew (text + content),
    ///     isolated from the shared context. Used to compute <see cref="CachedDrawnRegion"/> for
    ///     <see cref="ViewportSettingsFlags.TransparentMouse"/> hit-testing.
    /// </summary>
    private DrawContext? _localDrawContext;

    /// <summary>
    ///     Called at the end of <see cref="Draw(DrawContext)"/> to finalize drawing and update the clip region.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="DrawContext"/> tracking what regions were drawn by this view and its subviews.
    ///     May be <see langword="null"/> if not tracking drawn regions.
    /// </param>
    private void DoDrawComplete (DrawContext? context)
    {
        // Phase 1: Notify that drawing is complete
        // Raise virtual method first, then event. This allows subclasses to override behavior
        // before subscribers see the event.
        OnDrawComplete (context);
        DrawComplete?.Invoke (this, new DrawEventArgs (Viewport, Viewport, context));

        // Phase 2: Update Driver.Clip to exclude this view's drawn area
        // This prevents views "behind" this one (earlier in draw order/Z-order) from drawing over it.
        // Adornments (Margin, Border, Padding) are handled by their Adornment.Parent view and don't exclude themselves.
        if (this is AdornmentView)
        {
            return;
        }

        // Cache drawn regions for adornments with TransparentMouse BEFORE clip exclusion.
        // Each adornment's LastDrawnRegion was populated during DrawAdornments() using per-adornment
        // DrawContexts. We combine with _lastLineCanvasRegion (rendered by the parent) for Border.
        // All three adornment types are handled uniformly.
        Border.UpdateCachedDrawnRegion (_lastLineCanvasRegion);
        Margin.UpdateCachedDrawnRegion (null);
        Padding.UpdateCachedDrawnRegion (null);

        bool marginTransparent = Margin.ViewportSettings.FastHasFlags (ViewportSettingsFlags.Transparent);
        bool borderTransparent = Border.ViewportSettings.FastHasFlags (ViewportSettingsFlags.Transparent);
        bool paddingTransparent = Padding.ViewportSettings.FastHasFlags (ViewportSettingsFlags.Transparent);
        bool viewTransparent = ViewportSettings.FastHasFlags (ViewportSettingsFlags.Transparent);

        if (!marginTransparent && !borderTransparent && !paddingTransparent && !viewTransparent)
        {
            // Fast path: All layers opaque — exclude the entire view area as one rectangle.
            // Use Margin frame if Margin has thickness, otherwise Border frame.
            Rectangle fullFrame = Margin.Thickness != Thickness.Empty ? Margin.FrameToScreen () : Border.FrameToScreen ();
            ExcludeFromClip (fullFrame);
            context?.AddDrawnRectangle (fullFrame);

            // Cache for TransparentMouse hit-testing (opaque = entire frame).
            if (ViewportSettings.FastHasFlags (ViewportSettingsFlags.TransparentMouse))
            {
                CachedDrawnRegion = new Region (fullFrame);
            }

            return;
        }

        // Per-layer clip exclusion: Each layer (Margin, Border, Padding, View content) is independently
        // transparent. Opaque layers exclude their full area. Transparent layers exclude only
        // the cells that were actually drawn (tracked via AdornmentImpl.LastDrawnRegion).
        Region exclusion = new ();

        // For each OPAQUE layer, add its full area to the exclusion.
        // For each TRANSPARENT layer, add only the cells that were actually drawn.
        if (!marginTransparent)
        {
            exclusion.Combine (Margin.Thickness.AsRegion (Margin.FrameToScreen ()), RegionOp.Union);
        }
        else
        {
            Margin.AddDrawnRegionTo (exclusion, null);
        }

        if (!borderTransparent)
        {
            exclusion.Combine (Border.Thickness.AsRegion (Border.FrameToScreen ()), RegionOp.Union);
        }
        else
        {
            Border.AddDrawnRegionTo (exclusion, _lastLineCanvasRegion);
        }

        if (!paddingTransparent)
        {
            exclusion.Combine (Padding.Thickness.AsRegion (Padding.FrameToScreen ()), RegionOp.Union);
        }
        else
        {
            Padding.AddDrawnRegionTo (exclusion, null);
        }

        if (!viewTransparent)
        {
            exclusion.Combine (ViewportToScreen (Viewport), RegionOp.Union);
        }

        // For transparent layers, also include context drawn regions (text, content, subviews)
        // clipped to the border frame. This ensures transparent view/adornment drawn cells are
        // excluded from the clip so they don't get overdrawn by the SuperView.
        if (context is { })
        {
            Region contentDrawn = context.GetDrawnRegion ().Clone ();
            contentDrawn.Intersect (Border.FrameToScreen ());
            exclusion.Combine (contentDrawn, RegionOp.Union);
        }

        ExcludeFromClip (exclusion);

        // Cache the view's own drawn region for TransparentMouse hit-testing.
        // Uses _localDrawContext (per-view) rather than the shared context, so that only
        // cells THIS view drew (text + content) are captured — not the SuperView's
        // ClearViewport fill or peer SubViews' content.
        if (ViewportSettings.FastHasFlags (ViewportSettingsFlags.TransparentMouse))
        {
            if (viewTransparent || borderTransparent)
            {
                CachedDrawnRegion = _localDrawContext?.GetDrawnRegion ();
            }
            else
            {
                // Opaque view with TransparentMouse — cache the entire border frame.
                CachedDrawnRegion = new Region (Border.FrameToScreen ());
            }
        }

        // Report the exclusion to the parent's DrawContext so SuperViews can track what we covered.
        context?.AddDrawnRegion (exclusion);
    }

    /// <summary>
    ///     Called when the View has completed drawing and is about to update the clip region.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="DrawContext"/> containing the regions that were drawn by this view and its subviews.
    ///     May be <see langword="null"/> if not tracking drawn regions.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method is called at the very end of <see cref="Draw(DrawContext)"/>, after all drawing
    ///         (adornments, content, text, subviews, line canvas) has completed but before the view's area
    ///         is excluded from <see cref="IDriver.Clip"/>.
    ///     </para>
    ///     <para>
    ///         Use this method to:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Perform any final drawing operations that need to happen after SubViews are drawn</description>
    ///         </item>
    ///         <item>
    ///             <description>Inspect what was drawn via the <paramref name="context"/> parameter</description>
    ///         </item>
    ///         <item>
    ///             <description>Add additional regions to the <paramref name="context"/> if needed</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <b>Important:</b> At this point, <see cref="IDriver.Clip"/> has been restored to the state
    ///         it was in when <see cref="Draw(DrawContext)"/> began. After this method returns, the view's
    ///         area will be excluded from the clip (see <see cref="DoDrawComplete"/> for details).
    ///     </para>
    ///     <para>
    ///         <b>Transparency Support:</b> If <see cref="ViewportSettings"/> includes
    ///         <see cref="ViewportSettingsFlags.Transparent"/>, the <paramref name="context"/> parameter
    ///         contains the actual regions that were drawn. You can inspect this to see what areas
    ///         will be excluded from the clip, and optionally add more regions if needed.
    ///     </para>
    /// </remarks>
    /// <seealso cref="DrawComplete"/>
    /// <seealso cref="Draw(DrawContext)"/>
    /// <seealso cref="DoDrawComplete"/>
    protected virtual void OnDrawComplete (DrawContext? context) { }

    /// <summary>Raised when the View has completed drawing and is about to update the clip region.</summary>
    /// <remarks>
    ///     <para>
    ///         This event is raised at the very end of <see cref="Draw(DrawContext)"/>, after all drawing
    ///         operations have completed but before the view's area is excluded from <see cref="IDriver.Clip"/>.
    ///     </para>
    ///     <para>
    ///         The <see cref="DrawEventArgs.DrawContext"/> property provides information about what regions
    ///         were drawn by this view and its subviews. This is particularly useful for views with
    ///         <see cref="ViewportSettingsFlags.Transparent"/> enabled, as it shows exactly which areas
    ///         will be excluded from the clip.
    ///     </para>
    ///     <para>
    ///         Use this event to:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Perform any final drawing operations</description>
    ///         </item>
    ///         <item>
    ///             <description>Inspect what was drawn</description>
    ///         </item>
    ///         <item>
    ///             <description>Track drawing statistics or metrics</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <b>Note:</b> This event fires <i>after</i> <see cref="OnDrawComplete(DrawContext)"/>. If you need
    ///         to override the behavior, prefer overriding the virtual method in a subclass rather than
    ///         subscribing to this event.
    ///     </para>
    /// </remarks>
    /// <seealso cref="OnDrawComplete(DrawContext)"/>
    /// <seealso cref="Draw(DrawContext)"/>
    public event EventHandler<DrawEventArgs>? DrawComplete;

    #endregion DrawComplete
}
