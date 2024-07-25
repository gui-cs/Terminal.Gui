using System.Drawing;

namespace Terminal.Gui;

public partial class View // Drawing APIs
{
    private ColorScheme _colorScheme;

    /// <summary>The color scheme for this view, if it is not defined, it returns the <see cref="SuperView"/>'s color scheme.</summary>
    public virtual ColorScheme ColorScheme
    {
        get
        {
            if (_colorScheme is null)
            {
                return SuperView?.ColorScheme;
            }

            return _colorScheme;
        }
        set
        {
            if (_colorScheme != value)
            {
                _colorScheme = value;
                SetNeedsDisplay ();
            }
        }
    }

    /// <summary>The canvas that any line drawing that is to be shared by subviews of this view should add lines to.</summary>
    /// <remarks><see cref="Border"/> adds border lines to this LineCanvas.</remarks>
    public LineCanvas LineCanvas { get; } = new ();

    // The view-relative region that needs to be redrawn. Marked internal for unit tests.
    internal Rectangle _needsDisplayRect = Rectangle.Empty;

    /// <summary>Gets or sets whether the view needs to be redrawn.</summary>
    public bool NeedsDisplay
    {
        get => _needsDisplayRect != Rectangle.Empty;
        set
        {
            if (value)
            {
                SetNeedsDisplay ();
            }
            else
            {
                ClearNeedsDisplay ();
            }
        }
    }

    /// <summary>Gets whether any Subviews need to be redrawn.</summary>
    public bool SubViewNeedsDisplay { get; private set; }

    /// <summary>
    ///     Gets or sets whether this View will use it's SuperView's <see cref="LineCanvas"/> for rendering any 
    ///     lines. If <see langword="true"/> the rendering of any borders drawn by this Frame will be done by its parent's
    ///     SuperView. If <see langword="false"/> (the default) this View's <see cref="OnDrawAdornments"/> method will be
    ///     called to render the borders.
    /// </summary>
    public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

    /// <summary>Draws the specified character in the specified viewport-relative column and row of the View.</summary>
    /// <para>
    ///     If the provided coordinates are outside the visible content area, this method does nothing.
    /// </para>
    /// <remarks>
    ///     The top-left corner of the visible content area is <c>ViewPort.Location</c>.
    /// </remarks>
    /// <param name="col">Column (viewport-relative).</param>
    /// <param name="row">Row (viewport-relative).</param>
    /// <param name="rune">The Rune.</param>
    public void AddRune (int col, int row, Rune rune)
    {
        if (Move (col, row))
        {
            Driver.AddRune (rune);
        }
    }

    /// <summary>Clears <see cref="Viewport"/> with the normal background.</summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="ViewportSettings"/> has <see cref="Gui.ViewportSettings.ClearContentOnly"/> only
    ///         the portion of the content
    ///         area that is visible within the <see cref="View.Viewport"/> will be cleared. This is useful for views that have a
    ///         content area larger than the Viewport (e.g. when <see cref="ViewportSettings.AllowNegativeLocation"/> is
    ///         enabled) and want
    ///         the area outside the content to be visually distinct.
    ///     </para>
    /// </remarks>
    public void Clear ()
    {
        if (Driver is null)
        {
            return;
        }

        // Get screen-relative coords
        Rectangle toClear = ViewportToScreen (Viewport with { Location = new (0, 0) });

        Rectangle prevClip = Driver.Clip;

        if (ViewportSettings.HasFlag (ViewportSettings.ClearContentOnly))
        {
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new (-Viewport.X, -Viewport.Y), GetContentSize ()));
            toClear = Rectangle.Intersect (toClear, visibleContent);
        }

        Attribute prev = Driver.SetAttribute (GetNormalColor ());
        Driver.FillRect (toClear);
        Driver.SetAttribute (prev);

        Driver.Clip = prevClip;
    }

    /// <summary>Fills the specified <see cref="Viewport"/>-relative rectangle with the specified color.</summary>
    /// <param name="rect">The Viewport-relative rectangle to clear.</param>
    /// <param name="color">The color to use to fill the rectangle. If not provided, the Normal background color will be used.</param>
    public void FillRect (Rectangle rect, Color? color = null)
    {
        if (Driver is null)
        {
            return;
        }

        // Get screen-relative coords
        Rectangle toClear = ViewportToScreen (rect);

        Rectangle prevClip = Driver.Clip;

        Driver.Clip = Rectangle.Intersect (prevClip, ViewportToScreen (Viewport with { Location = new (0, 0) }));

        Attribute prev = Driver.SetAttribute (new (color ?? GetNormalColor ().Background));
        Driver.FillRect (toClear);
        Driver.SetAttribute (prev);

        Driver.Clip = prevClip;
    }

    /// <summary>Sets the <see cref="ConsoleDriver"/>'s clip region to <see cref="Viewport"/>.</summary>
    /// <remarks>
    /// <para>
    ///     By default, the clip rectangle is set to the intersection of the current clip region and the
    ///     <see cref="Viewport"/>. This ensures that drawing is constrained to the viewport, but allows
    ///     content to be drawn beyond the viewport.
    /// </para>
    /// <para>
    ///     If <see cref="ViewportSettings"/> has <see cref="Gui.ViewportSettings.ClipContentOnly"/> set, clipping will be
    ///     applied to just the visible content area.
    /// </para>
    /// </remarks>
    /// <returns>
    ///     The current screen-relative clip region, which can be then re-applied by setting
    ///     <see cref="ConsoleDriver.Clip"/>.
    /// </returns>
    public Rectangle SetClip ()
    {
        if (Driver is null)
        {
            return Rectangle.Empty;
        }

        Rectangle previous = Driver.Clip;

        // Clamp the Clip to the entire visible area
        Rectangle clip = Rectangle.Intersect (ViewportToScreen (Viewport with { Location = Point.Empty }), previous);

        if (ViewportSettings.HasFlag (ViewportSettings.ClipContentOnly))
        {
            // Clamp the Clip to the just content area that is within the viewport
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new (-Viewport.X, -Viewport.Y), GetContentSize ()));
            clip = Rectangle.Intersect (clip, visibleContent);
        }

        Driver.Clip = clip;

        return previous;
    }

    /// <summary>
    ///     Draws the view. Causes the following virtual methods to be called (along with their related events):
    ///     <see cref="OnDrawContent"/>, <see cref="OnDrawContentComplete"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Always use <see cref="Viewport"/> (view-relative) when calling <see cref="OnDrawContent(Rectangle)"/>, NOT
    ///         <see cref="Frame"/> (superview-relative).
    ///     </para>
    ///     <para>
    ///         Views should set the color that they want to use on entry, as otherwise this will inherit the last color that
    ///         was set globally on the driver.
    ///     </para>
    ///     <para>
    ///         Overrides of <see cref="OnDrawContent(Rectangle)"/> must ensure they do not set <c>Driver.Clip</c> to a clip
    ///         region larger than the <ref name="Viewport"/> property, as this will cause the driver to clip the entire
    ///         region.
    ///     </para>
    /// </remarks>
    public void Draw ()
    {
        if (!CanBeVisible (this))
        {
            return;
        }

        OnDrawAdornments ();

        if (ColorScheme is { })
        {
            //Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
            Driver?.SetAttribute (GetNormalColor ());
        }

        // By default, we clip to the viewport preventing drawing outside the viewport
        // We also clip to the content, but if a developer wants to draw outside the viewport, they can do
        // so via settings. SetClip honors the ViewportSettings.DisableVisibleContentClipping flag.
        Rectangle prevClip = SetClip ();

        // Invoke DrawContentEvent
        var dev = new DrawEventArgs (Viewport, Rectangle.Empty);
        DrawContent?.Invoke (this, dev);

        if (!dev.Cancel)
        {
            OnDrawContent (Viewport);
        }

        if (Driver is { })
        {
            Driver.Clip = prevClip;
        }

        OnRenderLineCanvas ();

        // TODO: This is a hack to force the border subviews to draw.
        if (Border?.Subviews is { })
        {
            foreach (View view in Border.Subviews)
            {
                view.SetNeedsDisplay ();
                view.Draw ();
            }
        }

        // Invoke DrawContentCompleteEvent
        OnDrawContentComplete (Viewport);

        // BUGBUG: v2 - We should be able to use View.SetClip here and not have to resort to knowing Driver details.
        ClearLayoutNeeded ();
        ClearNeedsDisplay ();
    }

    /// <summary>Event invoked when the content area of the View is to be drawn.</summary>
    /// <remarks>
    ///     <para>Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.</para>
    ///     <para>
    ///         Rect provides the view-relative rectangle describing the currently visible viewport into the
    ///         <see cref="View"/> .
    ///     </para>
    /// </remarks>
    public event EventHandler<DrawEventArgs> DrawContent;

    /// <summary>Event invoked when the content area of the View is completed drawing.</summary>
    /// <remarks>
    ///     <para>Will be invoked after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.</para>
    ///     <para>
    ///         Rect provides the view-relative rectangle describing the currently visible viewport into the
    ///         <see cref="View"/> .
    ///     </para>
    /// </remarks>
    public event EventHandler<DrawEventArgs> DrawContentComplete;

    /// <summary>Utility function to draw strings that contain a hotkey.</summary>
    /// <param name="text">String to display, the hotkey specifier before a letter flags the next letter as the hotkey.</param>
    /// <param name="hotColor">Hot color.</param>
    /// <param name="normalColor">Normal color.</param>
    /// <remarks>
    ///     <para>
    ///         The hotkey is any character following the hotkey specifier, which is the underscore ('_') character by
    ///         default.
    ///     </para>
    ///     <para>The hotkey specifier can be changed via <see cref="HotKeySpecifier"/></para>
    /// </remarks>
    public void DrawHotString (string text, Attribute hotColor, Attribute normalColor)
    {
        Rune hotkeySpec = HotKeySpecifier == (Rune)0xffff ? (Rune)'_' : HotKeySpecifier;
        Application.Driver?.SetAttribute (normalColor);

        foreach (Rune rune in text.EnumerateRunes ())
        {
            if (rune == new Rune (hotkeySpec.Value))
            {
                Application.Driver?.SetAttribute (hotColor);

                continue;
            }

            Application.Driver?.AddRune (rune);
            Application.Driver?.SetAttribute (normalColor);
        }
    }

    /// <summary>
    ///     Utility function to draw strings that contains a hotkey using a <see cref="ColorScheme"/> and the "focused"
    ///     state.
    /// </summary>
    /// <param name="text">String to display, the underscore before a letter flags the next letter as the hotkey.</param>
    /// <param name="focused">
    ///     If set to <see langword="true"/> this uses the focused colors from the color scheme, otherwise
    ///     the regular ones.
    /// </param>
    /// <param name="scheme">The color scheme to use.</param>
    public void DrawHotString (string text, bool focused, ColorScheme scheme)
    {
        if (focused)
        {
            DrawHotString (text, scheme.HotFocus, scheme.Focus);
        }
        else
        {
            DrawHotString (
                           text,
                           Enabled ? scheme.HotNormal : scheme.Disabled,
                           Enabled ? scheme.Normal : scheme.Disabled
                          );
        }
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetFocusColor ()
    {
        ColorScheme cs = ColorScheme;
        if (cs is null)
        {
            cs = new ();
        }

        return Enabled ? cs.Focus : cs.Disabled;
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="Terminal.Gui.ColorScheme.HotNormal"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetHotNormalColor ()
    {
        ColorScheme cs = ColorScheme;

        if (cs is null)
        {
            cs = new ();
        }

        return Enabled ? cs.HotNormal : cs.Disabled;
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="Terminal.Gui.ColorScheme.Normal"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetNormalColor ()
    {
        ColorScheme cs = ColorScheme;

        if (cs is null)
        {
            cs = new ();
        }

        return Enabled ? cs.Normal : cs.Disabled;
    }

    /// <summary>Moves the drawing cursor to the specified <see cref="Viewport"/>-relative location in the view.</summary>
    /// <remarks>
    ///     <para>
    ///         If the provided coordinates are outside the visible content area, this method does nothing.
    ///     </para>
    ///     <para>
    ///         The top-left corner of the visible content area is <c>ViewPort.Location</c>.
    ///     </para>
    /// </remarks>
    /// <param name="col">Column (viewport-relative).</param>
    /// <param name="row">Row (viewport-relative).</param>
    public bool Move (int col, int row)
    {
        if (Driver is null || Driver?.Rows == 0)
        {
            return false;
        }

        if (col < 0 || row < 0 || col >= Viewport.Width || row >= Viewport.Height)
        {
            return false;
        }

        var screen = ViewportToScreen (new Point (col, row));
        Driver?.Move (screen.X, screen.Y);

        return true;
    }

    // TODO: Make this cancelable
    /// <summary>
    ///     Prepares <see cref="View.LineCanvas"/>. If <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> be prepared to be rendered.
    /// </summary>
    /// <returns></returns>
    public virtual bool OnDrawAdornments ()
    {
        if (!IsInitialized)
        {
            return false;
        }

        // Each of these renders lines to either this View's LineCanvas 
        // Those lines will be finally rendered in OnRenderLineCanvas
        Margin?.OnDrawContent (Margin.Viewport);
        Border?.OnDrawContent (Border.Viewport);
        Padding?.OnDrawContent (Padding.Viewport);

        return true;
    }

    /// <summary>
    ///     Draws the view's content, including Subviews.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <paramref name="viewport"/> parameter is provided as a convenience; it has the same values as the
    ///         <see cref="Viewport"/> property.
    ///     </para>
    ///     <para>
    ///         The <see cref="Viewport"/> Location and Size indicate what part of the View's content, defined
    ///         by <see cref="GetContentSize ()"/>, is visible and should be drawn. The coordinates taken by <see cref="Move"/> and
    ///         <see cref="AddRune"/> are relative to <see cref="Viewport"/>, thus if <c>ViewPort.Location.Y</c> is <c>5</c>
    ///         the 6th row of the content should be drawn using <c>MoveTo (x, 5)</c>.
    ///     </para>
    ///     <para>
    ///         If <see cref="GetContentSize ()"/> is larger than <c>ViewPort.Size</c> drawing code should use <see cref="Viewport"/>
    ///         to constrain drawing for better performance.
    ///     </para>
    ///     <para>
    ///         The <see cref="ConsoleDriver.Clip"/> may define smaller area than <see cref="Viewport"/>; complex drawing code
    ///         can be more
    ///         efficient by using <see cref="ConsoleDriver.Clip"/> to constrain drawing for better performance.
    ///     </para>
    ///     <para>
    ///         Overrides should loop through the subviews and call <see cref="Draw"/>.
    ///     </para>
    /// </remarks>
    /// <param name="viewport">
    ///     The rectangle describing the currently visible viewport into the <see cref="View"/>; has the same value as
    ///     <see cref="Viewport"/>.
    /// </param>
    public virtual void OnDrawContent (Rectangle viewport)
    {
        if (NeedsDisplay)
        {
            if (SuperView is { })
            {
                Clear ();
            }

            if (!string.IsNullOrEmpty (TextFormatter.Text))
            {
                if (TextFormatter is { })
                {
                    TextFormatter.NeedsFormat = true;
                }
            }

            // This should NOT clear 
            // TODO: If the output is not in the Viewport, do nothing
            var drawRect = new Rectangle (ContentToScreen (Point.Empty), GetContentSize ());

            TextFormatter?.Draw (
                                 drawRect,
                                 HasFocus ? GetFocusColor () : GetNormalColor (),
                                 HasFocus ? ColorScheme.HotFocus : GetHotNormalColor (),
                                 Rectangle.Empty
                                );
            SetSubViewNeedsDisplay ();
        }

        // TODO: Move drawing of subviews to a separate OnDrawSubviews virtual method
        // Draw subviews
        // TODO: Implement OnDrawSubviews (cancelable);
        if (_subviews is { } && SubViewNeedsDisplay)
        {
            IEnumerable<View> subviewsNeedingDraw = _subviews.Where (
                                                                     view => view.Visible
                                                                             && (view.NeedsDisplay || view.SubViewNeedsDisplay || view.LayoutNeeded)
                                                                    );
            foreach (View view in subviewsNeedingDraw)
            {
                if (view.LayoutNeeded)
                {
                    view.LayoutSubviews ();
                }
                view.Draw ();
            }
        }
    }

    /// <summary>
    ///     Called after <see cref="OnDrawContent"/> to enable overrides.
    /// </summary>
    /// <param name="viewport">
    ///     The viewport-relative rectangle describing the currently visible viewport into the
    ///     <see cref="View"/>
    /// </param>
    public virtual void OnDrawContentComplete (Rectangle viewport) { DrawContentComplete?.Invoke (this, new (viewport, Rectangle.Empty)); }

    // TODO: Make this cancelable
    /// <summary>
    ///     Renders <see cref="View.LineCanvas"/>. If <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> to be rendered.
    /// </summary>
    /// <returns></returns>
    public virtual bool OnRenderLineCanvas ()
    {
        if (!IsInitialized || Driver is null)
        {
            return false;
        }

        // If we have a SuperView, it'll render our frames.
        if (!SuperViewRendersLineCanvas && LineCanvas.Viewport != Rectangle.Empty)
        {
            foreach (KeyValuePair<Point, Cell?> p in LineCanvas.GetCellMap ())
            {
                // Get the entire map
                if (p.Value is { })
                {
                    Driver.SetAttribute (p.Value.Value.Attribute ?? ColorScheme.Normal);
                    Driver.Move (p.Key.X, p.Key.Y);

                    // TODO: #2616 - Support combining sequences that don't normalize
                    Driver.AddRune (p.Value.Value.Rune);
                }
            }

            LineCanvas.Clear ();
        }

        if (Subviews.Any (s => s.SuperViewRendersLineCanvas))
        {
            foreach (View subview in Subviews.Where (s => s.SuperViewRendersLineCanvas))
            {
                // Combine the LineCanvas'
                LineCanvas.Merge (subview.LineCanvas);
                subview.LineCanvas.Clear ();
            }

            foreach (KeyValuePair<Point, Cell?> p in LineCanvas.GetCellMap ())
            {
                // Get the entire map
                if (p.Value is { })
                {
                    Driver.SetAttribute (p.Value.Value.Attribute ?? ColorScheme.Normal);
                    Driver.Move (p.Key.X, p.Key.Y);

                    // TODO: #2616 - Support combining sequences that don't normalize
                    Driver.AddRune (p.Value.Value.Rune);
                }
            }

            LineCanvas.Clear ();
        }

        return true;
    }

    /// <summary>Sets the area of this view needing to be redrawn to <see cref="Viewport"/>.</summary>
    /// <remarks>
    ///     If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), this method
    ///     does nothing.
    /// </remarks>
    public void SetNeedsDisplay ()
    {
        SetNeedsDisplay (Viewport);
    }

    /// <summary>Expands the area of this view needing to be redrawn to include <paramref name="region"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         The location of <paramref name="region"/> is relative to the View's content, bound by <c>Size.Empty</c> and
    ///         <see cref="GetContentSize ()"/>.
    ///     </para>
    ///     <para>
    ///         If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), the area to be
    ///         redrawn will be the <paramref name="region"/>.
    ///     </para>
    /// </remarks>
    /// <param name="region">The content-relative region that needs to be redrawn.</param>
    public void SetNeedsDisplay (Rectangle region)
    {
        if (_needsDisplayRect.IsEmpty)
        {
            _needsDisplayRect = region;
        }
        else
        {
            int x = Math.Min (_needsDisplayRect.X, region.X);
            int y = Math.Min (_needsDisplayRect.Y, region.Y);
            int w = Math.Max (_needsDisplayRect.Width, region.Width);
            int h = Math.Max (_needsDisplayRect.Height, region.Height);
            _needsDisplayRect = new (x, y, w, h);
        }

        Margin?.SetNeedsDisplay ();
        Border?.SetNeedsDisplay ();
        Padding?.SetNeedsDisplay ();

        SuperView?.SetSubViewNeedsDisplay ();

        foreach (View subview in Subviews)
        {
            if (subview.Frame.IntersectsWith (region))
            {
                Rectangle subviewRegion = Rectangle.Intersect (subview.Frame, region);
                subviewRegion.X -= subview.Frame.X;
                subviewRegion.Y -= subview.Frame.Y;
                subview.SetNeedsDisplay (subviewRegion);
            }
        }
    }

    /// <summary>Sets <see cref="SubViewNeedsDisplay"/> to <see langword="true"/> for this View and all Superviews.</summary>
    public void SetSubViewNeedsDisplay ()
    {
        SubViewNeedsDisplay = true;

        if (this is Adornment adornment)
        {
            adornment.Parent?.SetSubViewNeedsDisplay ();
        }

        if (SuperView is { SubViewNeedsDisplay: false })
        {
            SuperView.SetSubViewNeedsDisplay ();

            return;
        }
    }

    /// <summary>Clears <see cref="NeedsDisplay"/> and <see cref="SubViewNeedsDisplay"/>.</summary>
    protected void ClearNeedsDisplay ()
    {
        _needsDisplayRect = Rectangle.Empty;
        SubViewNeedsDisplay = false;

        Margin?.ClearNeedsDisplay ();
        Border?.ClearNeedsDisplay ();
        Padding?.ClearNeedsDisplay ();

        foreach (View subview in Subviews)
        {
            subview.ClearNeedsDisplay ();
        }
    }
}
