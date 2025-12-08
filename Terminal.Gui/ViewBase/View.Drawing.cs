using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

public partial class View // Drawing APIs
{
    /// <summary>
    ///     Draws a set of views.
    /// </summary>
    /// <param name="views">The peer views to draw.</param>
    /// <param name="force">If <see langword="true"/>, <see cref="View.SetNeedsDraw()"/> will be called on each view to force it to be drawn.</param>
    internal static void Draw (IEnumerable<View> views, bool force)
    {
        // **Snapshot once** — every recursion level gets its own frozen array
        View [] viewsArray = views.Snapshot ();

        // The draw context is used to track the region drawn by each view.
        DrawContext context = new DrawContext ();

        foreach (View view in viewsArray.Reverse ())
        {
            if (force)
            {
                view.SetNeedsDraw ();
            }

            view.Draw (context);
        }

        // DrawMargins may have caused some views have NeedsDraw/NeedsSubViewDraw set; clear them all.
        foreach (View view in viewsArray)
        {
            view.ClearNeedsDraw ();
        }

        // After all peer views have been drawn and cleared, we can now clear the SuperView's SubViewNeedsDraw flag.
        // ClearNeedsDraw() does not clear SuperView.SubViewNeedsDraw (by design, to avoid premature clearing
        // when siblings still need drawing), so we must do it here after ALL peers are processed.
        // We only clear the flag if ALL the SuperView's subviews no longer need drawing.
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
    ///         See the View Drawing Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.Gui/docs/drawing.html"/>.
    ///     </para>
    /// </remarks>
    public void Draw (DrawContext? context = null)
    {
        if (!CanBeVisible (this))
        {
            return;
        }
        Region? originalClip = GetClip ();

        if (SuperView is null && Driver is { } && originalClip?.GetRectangles().Length == 0)
        {
            originalClip = new (new (Driver.Screen.Location, Driver.Screen.Size));
        }

        // TODO: This can be further optimized by checking NeedsDraw below and only
        // TODO: clearing, drawing text, drawing content, etc. if it is true.
        if (NeedsDraw || SubViewNeedsDraw)
        {
            // ------------------------------------
            // Draw the Border and Padding.
            // Note Margin with a Shadow is special-cased and drawn in a separate pass to support
            // transparent shadows.
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
            // Draw the subviews first (order matters: SubViews, Text, Content)
            if (SubViewNeedsDraw)
            {
                DoDrawSubViews (context);
            }

            // ------------------------------------
            // Draw the text
            SetAttributeForRole (Enabled ? VisualRole.Normal : VisualRole.Disabled);
            DoDrawText (context);

            // ------------------------------------
            // Draw the content
            DoDrawContent (context);

            // ------------------------------------
            // Draw the line canvas
            // Restore the clip before rendering the line canvas and adornment subviews
            // because they may draw outside the viewport.
            SetClip (originalClip);
            originalClip = AddFrameToClip ();
            DoRenderLineCanvas (context);

            // ------------------------------------
            // Re-draw the border and padding subviews
            // HACK: This is a hack to ensure that the border and padding subviews are drawn after the line canvas.
            DoDrawAdornmentsSubViews ();

            // ------------------------------------
            // Advance the diagnostics draw indicator
            Border?.AdvanceDrawIndicator ();

            ClearNeedsDraw ();

            if (this is not Adornment && SuperView is not Adornment)
            {
                // Parent
                Debug.Assert (Margin!.Parent == this);
                Debug.Assert (Border!.Parent == this);
                Debug.Assert (Padding!.Parent == this);

                // SubViewNeedsDraw is set to false by ClearNeedsDraw.
                Debug.Assert (SubViewNeedsDraw == false);
                Debug.Assert (Margin!.SubViewNeedsDraw == false);
                Debug.Assert (Border!.SubViewNeedsDraw == false);
                Debug.Assert (Padding!.SubViewNeedsDraw == false);

                // NeedsDraw is set to false by ClearNeedsDraw.
                Debug.Assert (NeedsDraw == false);
                Debug.Assert (Margin!.NeedsDraw == false);
                Debug.Assert (Border!.NeedsDraw == false);
                Debug.Assert (Padding!.NeedsDraw == false);
            }
        }

        // ------------------------------------
        // This causes the Margin to be drawn in a second pass if it has a ShadowStyle
        Margin?.CacheClip ();

        // ------------------------------------
        // Reset the clip to what it was when we started
        SetClip (originalClip);

        // ------------------------------------
        // We're done drawing - The Clip is reset to what it was before we started.
        DoDrawComplete (context);
    }

    #region DrawAdornments

    private void DoDrawAdornmentsSubViews ()
    {
        // NOTE: We do not support subviews of Margin?

        if (Margin?.SubViews is { } && Margin.Thickness != Thickness.Empty && Margin.NeedsDraw)
        {
            foreach (View subview in Margin.SubViews)
            {
                subview.SetNeedsDraw ();
            }

            Region? saved = Margin?.AddFrameToClip ();
            Margin?.DoDrawSubViews ();
            SetClip (saved);
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
            Region? clipAdornments = Margin!.Thickness.AsRegion (Margin!.FrameToScreen ());
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

        if (Margin is { } && Margin.Thickness != Thickness.Empty/* && Margin.ShadowStyle == ShadowStyle.None*/)
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
    ///     <see cref="LineCanvas"/> of this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> be prepared to be rendered.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of the Adornments.</returns>
    protected virtual bool OnDrawingAdornments () { return false; }

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
        var drawRect = new Rectangle (ContentToScreen (Point.Empty), GetContentSize ());

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

        // We assume that the text has been drawn over the entire area; ensure that the subviews are redrawn.
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
    /// <remarks>
    ///     <para>
    ///         Override this method to draw custom content for your View.
    ///     </para>
    ///     <para>
    ///         <b>Transparency Support:</b> If your View has <see cref="ViewportSettings"/> with <see cref="ViewportSettingsFlags.Transparent"/>
    ///         set, you should report the exact regions you draw to via the <paramref name="context"/> parameter. This allows
    ///         the transparency system to exclude only the drawn areas from the clip region, letting views beneath show through
    ///         in the areas you didn't draw.
    ///     </para>
    ///     <para>
    ///         Use <see cref="DrawContext.AddDrawnRectangle"/> for simple rectangular areas, or <see cref="DrawContext.AddDrawnRegion"/>
    ///         for complex, non-rectangular shapes. All coordinates passed to these methods must be in <b>screen-relative coordinates</b>.
    ///         Use <see cref="View.ViewportToScreen(in Rectangle)"/> or <see cref="View.ContentToScreen(in Point)"/> to convert from
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
    protected virtual bool OnDrawingContent (DrawContext? context) { return false; }

    /// <summary>Raised when the View's content is to be drawn.</summary>
    /// <remarks>
    ///     <para>
    ///         Subscribe to this event to draw custom content for the View. Use the drawing methods available on <see cref="View"/>
    ///         such as <see cref="View.AddRune(int, int, Rune)"/>, <see cref="View.AddStr(string)"/>, and <see cref="View.FillRect(Rectangle, Rune)"/>.
    ///     </para>
    ///     <para>
    ///         The event is invoked after <see cref="ClearingViewport"/> and <see cref="Text"/> have been drawn, but before any <see cref="SubViews"/> are drawn.
    ///     </para>
    ///     <para>
    ///         <b>Transparency Support:</b> If the View has <see cref="ViewportSettings"/> with <see cref="ViewportSettingsFlags.Transparent"/>
    ///         set, use the <see cref="DrawEventArgs.DrawContext"/> to report which areas were actually drawn. This enables proper transparency
    ///         by excluding only the drawn areas from the clip region. See <see cref="DrawContext"/> for details on reporting drawn regions.
    ///     </para>
    ///     <para>
    ///         The <see cref="DrawEventArgs.NewViewport"/> property provides the view-relative rectangle describing the currently visible viewport into the View.
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
    protected virtual bool OnDrawingSubViews (DrawContext? context) { return false; }

    /// <summary>
    ///     Called when the <see cref="SubViews"/> are to be drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="SubViews"/>.</returns>
    protected virtual bool OnDrawingSubViews () { return false; }

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

        // Draw the subviews in reverse order to leverage clipping.
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
    protected virtual bool OnRenderingLineCanvas () { return false; }

    /// <summary>The canvas that any line drawing that is to be shared by subviews of this view should add lines to.</summary>
    /// <remarks><see cref="Border"/> adds border lines to this LineCanvas.</remarks>
    public LineCanvas LineCanvas { get; } = new ();

    /// <summary>
    ///     Gets or sets whether this View will use it's SuperView's <see cref="LineCanvas"/> for rendering any
    ///     lines. If <see langword="true"/> the rendering of any borders drawn by this Frame will be done by its parent's
    ///     SuperView. If <see langword="false"/> (the default) this View's <see cref="OnDrawingAdornments"/> method will
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
    /// <param name="context"></param>
    public void RenderLineCanvas (DrawContext? context)
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
                    SetAttribute (p.Value.Value.Attribute ?? GetAttributeForRole (VisualRole.Normal));
                    Driver.Move (p.Key.X, p.Key.Y);

                    // TODO: #2616 - Support combining sequences that don't normalize
                    AddStr (p.Value.Value.Grapheme);

                    // Add each drawn cell to the context
                    context?.AddDrawnRectangle (new Rectangle (p.Key, new (1, 1)) );
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
            if (ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent))
            {
                // context!.DrawnRegion is the region that was drawn by this view. It may include regions outside
                // the Viewport. We need to clip it to the Viewport.
                context!.ClipDrawnRegion (ViewportToScreen (Viewport));

                // Exclude the drawn region from the clip
                ExcludeFromClip (context.GetDrawnRegion ());

                // Exclude the Border and Padding from the clip
                ExcludeFromClip (Border?.Thickness.AsRegion (Border.FrameToScreen ()));
                ExcludeFromClip (Padding?.Thickness.AsRegion (Padding.FrameToScreen ()));
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

}
