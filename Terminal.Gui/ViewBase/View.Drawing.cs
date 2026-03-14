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

        // Draw shadows last to ensure they are drawn on top of the content.
        Margin.DrawShadows (viewsArray);

        // DrawShadows may have caused some views have NeedsDraw/NeedsSubViewDraw set; clear them all.
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
            if (view is not Adornment && view.SuperView is { } && view.SuperView != lastSuperView)
            {
                // Check if ANY subview of this SuperView still needs drawing
                bool anySubViewNeedsDrawing = view.SuperView.InternalSubViews.Any (v => v.NeedsDraw || v.SubViewNeedsDraw);

                if (!anySubViewNeedsDrawing)
                {
                    view.SuperView.SubViewNeedsDraw = false;
                }

                lastSuperView = view.SuperView;
            }
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
            context ??= new ();

            SetAttributeForRole (Enabled ? VisualRole.Normal : VisualRole.Disabled);
            DoClearViewport (context);

            // ------------------------------------
            // Draw the SubViews first (order matters: SubViews, Text, Content)
            if (SubViewNeedsDraw)
            {
                Trace.Draw (this.ToIdentifyingString (), "SubViews");
                DoDrawSubViews (context);
            }

            // ------------------------------------
            // Draw the text
            Trace.Draw (this.ToIdentifyingString (), "Text");
            SetAttributeForRole (Enabled ? VisualRole.Normal : VisualRole.Disabled);
            DoDrawText (context);

            // ------------------------------------
            // Draw the content
            Trace.Draw (this.ToIdentifyingString (), "Content");
            DoDrawContent (context);

            // ------------------------------------
            // Draw the line canvas
            // Restore the clip before rendering the line canvas and adornment subviews
            // because they may draw outside the viewport.
            SetClip (originalClip);
            originalClip = AddFrameToClip ();
            Trace.Draw (this.ToIdentifyingString (), "LineCanvas");
            DoRenderLineCanvas (context);

            // ------------------------------------
            // Re-draw the Border and Padding Adornment SubViews
            // HACK: This is a hack to ensure that the Border and Padding Adornment SubViews are drawn after the line canvas.
            DoDrawAdornmentsSubViews ();

            // ------------------------------------
            // Advance the diagnostics draw indicator
            Border?.AdvanceDrawIndicator ();

            ClearNeedsDraw ();

            //if (this is not Adornment && SuperView is not Adornment)
            //{
            //    // Parent
            //    Debug.Assert (Margin!.Parent == this);
            //    Debug.Assert (Border!.Parent == this);
            //    Debug.Assert (Padding!.Parent == this);

            //    // SubViewNeedsDraw is set to false by ClearNeedsDraw.
            //    Debug.Assert (SubViewNeedsDraw == false);
            //    Debug.Assert (Margin!.SubViewNeedsDraw == false);
            //    Debug.Assert (Border!.SubViewNeedsDraw == false);
            //    Debug.Assert (Padding!.SubViewNeedsDraw == false);

            //    // NeedsDraw is set to false by ClearNeedsDraw.
            //    Debug.Assert (NeedsDraw == false);
            //    Debug.Assert (Margin!.NeedsDraw == false);
            //    Debug.Assert (Border!.NeedsDraw == false);
            //    Debug.Assert (Padding!.NeedsDraw == false);
            //}
        }

        // ------------------------------------
        // This causes the Margin to be drawn in a second pass if it has a ShadowStyle
        Margin?.CacheClip ();

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

    #region DrawAdornments

    private void DoDrawAdornmentsSubViews ()
    {
        // Only SetNeedsDraw on Margin here if it is not Transparent. Margins with shadows are drawn in a separate pass in the static View.Draw
        // via Margin.DrawShadows.
        if (Margin is { NeedsDraw: true } && !Margin.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent) && Margin.Thickness != Thickness.Empty)
        {
            foreach (View subview in Margin.SubViews)
            {
                subview.SetNeedsDraw ();
            }

            // NOTE: We do not support arbitrary SubViews of Margin (only ShadowView)
            // NOTE: so we do not call DoDrawSubViews on Margin.
        }

        if (Border?.SubViews is { } && Border.Thickness != Thickness.Empty && Border.NeedsDraw)
        {
            // PERFORMANCE: Get the check for DrawIndicator out of this somehow.
            foreach (View subview in Border.SubViews.Where (v => v.Visible || v.Id == "DrawIndicator"))
            {
                if (subview.Id != "DrawIndicator")
                {
                    subview.SetNeedsDraw ();
                }

                LineCanvas.Exclude (new (subview.FrameToScreen ()));
            }

            Region? saved = Border?.AddFrameToClip ();
            Border?.DoDrawSubViews ();
            SetClip (saved);
        }

        if (Padding?.SubViews is { } && Padding.Thickness != Thickness.Empty && Padding.NeedsDraw)
        {
            foreach (View subview in Padding.SubViews)
            {
                subview.SetNeedsDraw ();
            }

            Region? saved = Padding?.AddFrameToClip ();
            Padding?.DoDrawSubViews ();
            SetClip (saved);
        }
    }

    internal void DoDrawAdornments (Region? originalClip)
    {
        if (this is Adornment)
        {
            AddFrameToClip ();
        }
        else
        {
            // Set the clip to be just the thicknesses of the adornments
            // TODO: Put this union logic in a method on View?
            Region clipAdornments = Margin!.Thickness.AsRegion (Margin!.FrameToScreen ());
            clipAdornments.Combine (Border!.Thickness.AsRegion (Border!.FrameToScreen ()), RegionOp.Union);
            clipAdornments.Combine (Padding!.Thickness.AsRegion (Padding!.FrameToScreen ()), RegionOp.Union);
            clipAdornments.Combine (originalClip, RegionOp.Intersect);
            SetClip (clipAdornments);
        }

        if (Margin?.NeedsLayout == true)
        {
            Margin.NeedsLayout = false;
            Margin?.Thickness.Draw (Driver, FrameToScreen ());
            Margin?.Parent?.SetSubViewNeedsDrawDownHierarchy ();
        }

        if (SubViewNeedsDraw)
        {
            // A SubView may add to the LineCanvas. This ensures any Adornment LineCanvas updates happen.
            Border?.SetNeedsDraw ();
            Padding?.SetNeedsDraw ();
            Margin?.SetNeedsDraw ();
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
        // Only draw Margin here if it is not Transparent. Margins with shadows are drawn in a separate pass in the static View.Draw
        // via Margin.DrawShadows.
        if (Margin is { } && !Margin.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent) && Margin.Thickness != Thickness.Empty)
        {
            Margin?.Draw ();
        }

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

        if (Margin is { } && Margin.Thickness != Thickness.Empty /* && Margin.ShadowStyle == ShadowStyle.None*/)
        {
            //Margin?.Draw ();
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

        Attribute prev = SetAttribute (GetAttributeForRole (VisualRole.Normal));
        Driver.FillRect (toClear);
        SetAttribute (prev);
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Called when the View's Adornments are to be drawn. Prepares <see cref="View.LineCanvas"/>. If
    ///     <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's SubViews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> be prepared to be rendered.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of the Adornments.</returns>
    protected virtual bool OnDrawingAdornments () => false;

    #endregion DrawAdornments

    #region ClearViewport

    internal void DoClearViewport (DrawContext? context = null)
    {
        if (!NeedsDraw || ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent) || OnClearingViewport ())
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

        if (!ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent))
        {
            ClearViewport (context);
            OnClearedViewport ();
            ClearedViewport?.Invoke (this, new (Viewport, Viewport, null));
        }
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
        Rectangle toClear = ViewportToScreen (Viewport with { Location = new (0, 0) });

        if (ViewportSettings.HasFlag (ViewportSettingsFlags.ClearContentOnly))
        {
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new (-Viewport.X, -Viewport.Y), GetContentSize ()));
            toClear = Rectangle.Intersect (toClear, visibleContent);
        }

        Driver.FillRect (toClear);

        // context.AddDrawnRectangle (toClear);

        SetNeedsDraw ();
    }

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
        Rectangle drawRect = new Rectangle (ContentToScreen (Point.Empty), GetContentSize ());

        // Use GetDrawRegion to get precise drawn areas
        Region textRegion = TextFormatter.GetDrawRegion (drawRect);

        // Report the drawn area to the context
        context?.AddDrawnRegion (textRegion);

        if (Driver is { })
        {
            TextFormatter.Draw (
                                Driver,
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
    ///             if (ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent))
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

    private void DoDrawSubViews (DrawContext? context = null)
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

        // Draw the SubViews in reverse Z-order to leverage clipping.
        // SubViews earlier in the collection are drawn last (on top).
        foreach (View view in InternalSubViews.Snapshot ().Where (v => v.Visible).Reverse ())
        {
            // TODO: HACK - This forcing of SetNeedsDraw with SuperViewRendersLineCanvas enables auto line join to work, but is brute force.
            if (view.SuperViewRendersLineCanvas || view.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent))
            {
                //view.SetNeedsDraw ();
            }

            view.Draw (context);

            if (view.SuperViewRendersLineCanvas)
            {
                LineCanvas.Merge (view.LineCanvas);
                view.LineCanvas.Clear ();
            }
        }
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

    /// <summary>
    ///     Called when the <see cref="View.LineCanvas"/> is to be rendered. See <see cref="RenderLineCanvas"/>.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="LineCanvas"/>.</returns>
    protected virtual bool OnRenderingLineCanvas () => false;

    /// <summary>The canvas that any line drawing that is to be shared by subviews of this view should add lines to.</summary>
    /// <remarks><see cref="Border"/> adds lines to this LineCanvas.</remarks>
    public LineCanvas LineCanvas { get; } = new ();

    /// <summary>
    ///     Gets or sets whether this View will use its SuperView's <see cref="LineCanvas"/> for rendering any
    ///     lines. If <see langword="true"/> the rendering of any borders drawn by this view will be done by its
    ///     SuperView. If <see langword="false"/> (the default) this View's <see cref="OnDrawingAdornments"/> method will
    ///     be called to render the borders.
    /// </summary>
    public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

    /// <summary>
    ///     Causes the contents of <see cref="LineCanvas"/> to be drawn.
    ///     If <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's SubViews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> to be rendered.
    /// </summary>
    /// <param name="context"></param>
    public void RenderLineCanvas (DrawContext? context)
    {
        if (Driver is null)
        {
            return;
        }

        if (!SuperViewRendersLineCanvas && LineCanvas.Bounds != Rectangle.Empty)
        {
            // Get both cell map and Region in a single pass through the canvas
            (Dictionary<Point, Cell?> cellMap, Region lineRegion) = LineCanvas.GetCellMapWithRegion ();

            foreach (KeyValuePair<Point, Cell?> p in cellMap)
            {
                // Get the entire map
                if (p.Value is { })
                {
                    SetAttribute (p.Value.Value.Attribute ?? GetAttributeForRole (VisualRole.Normal));
                    Driver.Move (p.Key.X, p.Key.Y);

                    // TODO: #2616 - Support combining sequences that don't normalize
                    AddStr (p.Value.Value.Grapheme);
                }
            }

            // Report the drawn region for transparency support
            // Region was built during the GetCellMapWithRegion() call above
            if (context is { } && cellMap.Count > 0)
            {
                context.AddDrawnRegion (lineRegion);
            }

            LineCanvas.Clear ();
        }
    }

    #endregion DrawLineCanvas

    #region DrawComplete

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
        DrawComplete?.Invoke (this, new (Viewport, Viewport, context));

        // Phase 2: Update Driver.Clip to exclude this view's drawn area
        // This prevents views "behind" this one (earlier in draw order/Z-order) from drawing over it.
        // Adornments (Margin, Border, Padding) are handled by their Adornment.Parent view and don't exclude themselves.
        if (this is not Adornment)
        {
            if (ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent))
            {
                // Transparent View Path:
                // Only exclude the regions that were actually drawn, allowing views beneath
                // to show through in areas where nothing was drawn.

                // The context.DrawnRegion may include areas outside the Viewport (e.g., if content
                // was drawn with ViewportSettingsFlags.AllowContentOutsideViewport). We need to clip
                // it to the Viewport bounds to prevent excluding areas that aren't visible.
                context!.ClipDrawnRegion (ViewportToScreen (Viewport));

                // Exclude the actually-drawn region from Driver.Clip
                ExcludeFromClip (context.GetDrawnRegion ());

                // Border and Padding are always opaque (they draw lines/fills), so exclude them too
                ExcludeFromClip (Border?.Thickness.AsRegion (Border.FrameToScreen ()));
                ExcludeFromClip (Padding?.Thickness.AsRegion (Padding.FrameToScreen ()));
            }
            else
            {
                // Opaque View Path (default):
                // Exclude the entire view area from Driver.Clip. This is the typical case where
                // the view is considered fully opaque.

                // Start with the Frame in screen coordinates
                Rectangle borderFrame = FrameToScreen ();

                // If there's a Border, use its frame instead (includes the border thickness)
                if (Border is { })
                {
                    borderFrame = Border.FrameToScreen ();
                }

                // Exclude this view's entire area (Border inward, but not Margin) from the clip.
                // This prevents any view drawn after this one from drawing in this area.
                ExcludeFromClip (borderFrame);

                // Update the DrawContext to track that we drew this entire rectangle.
                // This allows our SuperView (if any) to know what area we occupied,
                // which is important for transparency calculations at higher levels.
                context?.AddDrawnRectangle (borderFrame);
            }
        }

        // When this method returns, Driver.Clip has been updated to exclude this view's area.
        // The next view drawn (earlier in Z-order, typically a peer view or the SuperView) will see
        // a clip with "holes" where this view (and any SubViews drawn before it) are located.
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
