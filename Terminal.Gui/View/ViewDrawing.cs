﻿namespace Terminal.Gui;

public partial class View
{
    // The view-relative region that needs to be redrawn. Marked internal for unit tests.
    internal Rectangle _needsDisplayRect = Rectangle.Empty;
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
    ///     Gets or sets whether this View will use it's SuperView's <see cref="LineCanvas"/> for rendering any border
    ///     lines. If <see langword="true"/> the rendering of any borders drawn by this Frame will be done by it's parent's
    ///     SuperView. If <see langword="false"/> (the default) this View's <see cref="OnDrawAdornments"/> method will be
    ///     called to render the borders.
    /// </summary>
    public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

    /// <summary>Displays the specified character in the specified column and row of the View.</summary>
    /// <param name="col">Column (view-relative).</param>
    /// <param name="row">Row (view-relative).</param>
    /// <param name="ch">Ch.</param>
    public void AddRune (int col, int row, Rune ch)
    {
        if (row < 0 || col < 0)
        {
            return;
        }

        if (row > _frame.Height - 1 || col > _frame.Width - 1)
        {
            return;
        }

        Move (col, row);
        Driver.AddRune (ch);
    }

    /// <summary>Clears <see cref="ContentArea"/> with the normal background.</summary>
    /// <remarks></remarks>
    public void Clear () { Clear (ContentArea); }

    /// <summary>Clears the specified <see cref="Bounds"/>-relative rectangle with the normal background.</summary>
    /// <remarks></remarks>
    /// <param name="contentArea">The Bounds-relative rectangle to clear.</param>
    public void Clear (Rectangle contentArea)
    {
        if (Driver is null)
        {
            return;
        }

        Attribute prev = Driver.SetAttribute (GetNormalColor ());

        // Clamp the region to the bounds of the view
        contentArea = Rectangle.Intersect (contentArea, ContentArea);
        Driver.FillRect (BoundsToScreen (contentArea));
        Driver.SetAttribute (prev);
    }

    /// <summary>Expands the <see cref="ConsoleDriver"/>'s clip region to include <see cref="ContentArea"/>.</summary>
    /// <returns>
    ///     The current screen-relative clip region, which can be then re-applied by setting
    ///     <see cref="ConsoleDriver.Clip"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         If <see cref="ConsoleDriver.Clip"/> and <see cref="ContentArea"/> do not intersect, the clip region will be set
    ///         to
    ///         <see cref="Rectangle.Empty"/>.
    ///     </para>
    /// </remarks>
    public Rectangle ClipToBounds ()
    {
        if (Driver is null)
        {
            return Rectangle.Empty;
        }

        Rectangle previous = Driver.Clip;
        Driver.Clip = Rectangle.Intersect (previous, BoundsToScreen (GetVisibleContentArea ()));

        return previous;
    }

    /// <summary>
    ///     Draws the view. Causes the following virtual methods to be called (along with their related events):
    ///     <see cref="OnDrawContent"/>, <see cref="OnDrawContentComplete"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Always use <see cref="ContentArea"/> (view-relative) when calling <see cref="OnDrawContent(Rectangle)"/>, NOT
    ///         <see cref="Frame"/> (superview-relative).
    ///     </para>
    ///     <para>
    ///         Views should set the color that they want to use on entry, as otherwise this will inherit the last color that
    ///         was set globally on the driver.
    ///     </para>
    ///     <para>
    ///         Overrides of <see cref="OnDrawContent(Rectangle)"/> must ensure they do not set <c>Driver.Clip</c> to a clip
    ///         region larger than the <ref name="Bounds"/> property, as this will cause the driver to clip the entire region.
    ///     </para>
    /// </remarks>
    public void Draw ()
    {
        if (!CanBeVisible (this))
        {
            return;
        }

        OnDrawAdornments ();

        Rectangle prevClip = ClipToBounds ();

        if (ColorScheme is { })
        {
            //Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
            Driver?.SetAttribute (GetNormalColor ());
        }

        // Invoke DrawContentEvent
        var dev = new DrawEventArgs (ContentArea);
        DrawContent?.Invoke (this, dev);

        if (!dev.Cancel)
        {
            OnDrawContent (ContentArea);
        }

        if (Driver is { })
        {
            Driver.Clip = prevClip;
        }

        OnRenderLineCanvas ();

        // BUGBUG: v2 - We should be able to use View.SetClip here and not have to resort to knowing Driver details.
        ClearLayoutNeeded ();
        ClearNeedsDisplay ();

        // Invoke DrawContentCompleteEvent
        dev = new (ContentArea);
        DrawContentComplete?.Invoke (this, dev);

        if (!dev.Cancel)
        {
            OnDrawContentComplete (ContentArea);
        }
    }

    /// <summary>Event invoked when the adornments area of the View is to be drawn.</summary>
    /// <remarks>
    ///     <para>Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.</para>
    ///     <para>
    ///         Rectangle provides the view-relative rectangle describing the currently visible viewport into the
    ///         <see cref="View"/> .
    ///     </para>
    /// </remarks>
    public event EventHandler<DrawEventArgs> DrawAdornments;

    /// <summary>Event invoked when the content area of the View is to be drawn.</summary>
    /// <remarks>
    ///     <para>Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.</para>
    ///     <para>
    ///         Rectangle provides the view-relative rectangle describing the currently visible viewport into the
    ///         <see cref="View"/> .
    ///     </para>
    /// </remarks>
    public event EventHandler<DrawEventArgs> DrawContent;

    /// <summary>Event invoked when the content area of the View is completed drawing.</summary>
    /// <remarks>
    ///     <para>Will be invoked after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.</para>
    ///     <para>
    ///         Rectangle provides the view-relative rectangle describing the currently visible viewport into the
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
        Application.Driver.SetAttribute (normalColor);

        foreach (Rune rune in text.EnumerateRunes ())
        {
            if (rune == new Rune (hotkeySpec.Value))
            {
                Application.Driver.SetAttribute (hotColor);

                continue;
            }

            Application.Driver.AddRune (rune);
            Application.Driver.SetAttribute (normalColor);
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
    ///     <see cref="Terminal.Gui.ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetFocusColor ()
    {
        ColorScheme cs = ColorScheme;

        if (ColorScheme is null)
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

        if (ColorScheme is null)
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

        if (ColorScheme is null)
        {
            cs = new ();
        }

        return Enabled ? cs.Normal : cs.Disabled;
    }

    /// <summary>This moves the cursor to the specified column and row in the view.</summary>
    /// <returns>The move.</returns>
    /// <param name="col">The column to move to, in view-relative coordinates.</param>
    /// <param name="row">the row to move to, in view-relative coordinates.</param>
    public void Move (int col, int row)
    {
        if (Driver is null || Driver?.Rows == 0)
        {
            return;
        }

        Rectangle screen = BoundsToScreen (new (col, row, 0, 0));
        Driver?.Move (screen.X, screen.Y);
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

        DrawAdornments?.Invoke (this, new DrawEventArgs (ContentArea));

        // Each of these renders lines to either this View's LineCanvas 
        // Those lines will be finally rendered in OnRenderLineCanvas
        Margin?.OnDrawContent (Margin.ContentArea);
        Border?.OnDrawContent (Border.ContentArea);
        Padding?.OnDrawContent (Padding.ContentArea);

        return true;
    }

    /// <summary>Enables overrides to draw infinitely scrolled content and/or a background behind added controls.</summary>
    /// <param name="contentArea">
    ///     The view-relative rectangle describing the currently visible viewport into the
    ///     <see cref="View"/>
    /// </param>
    /// <remarks>This method will be called before any subviews added with <see cref="Add(View)"/> have been drawn.</remarks>
    public virtual void OnDrawContent (Rectangle contentArea)
    {
        if (NeedsDisplay)
        {
            if (SuperView is { })
            {
                Clear (contentArea);
            }

            if (!string.IsNullOrEmpty (TextFormatter.Text))
            {
                if (TextFormatter is { })
                {
                    TextFormatter.NeedsFormat = true;
                }
            }

            // This should NOT clear
            TextFormatter?.Draw (
                                 BoundsToScreen (contentArea),
                                 HasFocus ? GetFocusColor () : GetNormalColor (),
                                 HasFocus ? ColorScheme.HotFocus : GetHotNormalColor (),
                                 Rectangle.Empty
                                );
            SetSubViewNeedsDisplay ();
        }

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
                //view.Frame.IntersectsWith (bounds)) {
                // && (view.Frame.IntersectsWith (bounds) || bounds.X < 0 || bounds.Y < 0)) {
                if (view.LayoutNeeded)
                {
                    view.LayoutSubviews ();
                }

                // Draw the subview
                // Use the view's bounds (view-relative; Location will always be (0,0)
                //if (view.Visible && view.Frame.Width > 0 && view.Frame.Height > 0) {
                view.Draw ();

                //}
            }
        }
    }

    /// <summary>
    ///     Enables overrides after completed drawing infinitely scrolled content and/or a background behind removed
    ///     controls.
    /// </summary>
    /// <param name="contentArea">
    ///     The view-relative rectangle describing the currently visible viewport into the
    ///     <see cref="View"/>
    /// </param>
    /// <remarks>
    ///     This method will be called after any subviews removed with <see cref="Remove(View)"/> have been completed
    ///     drawing.
    /// </remarks>
    public virtual void OnDrawContentComplete (Rectangle contentArea) { }

    // TODO: Make this cancelable
    /// <summary>
    ///     Renders <see cref="View.LineCanvas"/>. If <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> to be rendered.
    /// </summary>
    /// <returns></returns>
    public virtual bool OnRenderLineCanvas ()
    {
        if (!IsInitialized)
        {
            return false;
        }

        // If we have a SuperView, it'll render our frames.
        if (!SuperViewRendersLineCanvas && LineCanvas.Bounds != Rectangle.Empty)
        {
            foreach (KeyValuePair<Point, Cell> p in LineCanvas.GetCellMap ())
            {
                // Get the entire map
                Driver.SetAttribute (p.Value.Attribute ?? ColorScheme.Normal);
                Driver.Move (p.Key.X, p.Key.Y);

                // TODO: #2616 - Support combining sequences that don't normalize
                Driver.AddRune (p.Value.Rune);
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

            foreach (KeyValuePair<Point, Cell> p in LineCanvas.GetCellMap ())
            {
                // Get the entire map
                Driver.SetAttribute (p.Value.Attribute ?? ColorScheme.Normal);
                Driver.Move (p.Key.X, p.Key.Y);

                // TODO: #2616 - Support combining sequences that don't normalize
                Driver.AddRune (p.Value.Rune);
            }

            LineCanvas.Clear ();
        }

        return true;
    }

    /// <summary>Sets the area of this view needing to be redrawn to <see cref="ContentArea"/>.</summary>
    /// <remarks>
    ///     If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), this method
    ///     does nothing.
    /// </remarks>
    public void SetNeedsDisplay ()
    {
        if (IsInitialized)
        {
            SetNeedsDisplay (ContentArea);
        }
    }

    /// <summary>Expands the area of this view needing to be redrawn to include <paramref name="region"/>.</summary>
    /// <remarks>
    ///     If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), the area to be
    ///     redrawn will be the <paramref name="region"/>.
    /// </remarks>
    /// <param name="region">The Bounds-relative region that needs to be redrawn.</param>
    public virtual void SetNeedsDisplay (Rectangle region)
    {
        if (!IsInitialized)
        {
            _needsDisplayRect = region;
            return;
        }

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

        _superView?.SetSubViewNeedsDisplay ();

        Margin?.SetNeedsDisplay (Margin.ContentArea);
        Border?.SetNeedsDisplay (Border.ContentArea);
        Padding?.SetNeedsDisplay (Padding.ContentArea);

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

    /// <summary>Indicates that any Subviews (in the <see cref="Subviews"/> list) need to be repainted.</summary>
    public void SetSubViewNeedsDisplay ()
    {
        SubViewNeedsDisplay = true;

        if (_superView is { } && !_superView.SubViewNeedsDisplay)
        {
            _superView.SetSubViewNeedsDisplay ();
        }
    }

    /// <summary>Clears <see cref="NeedsDisplay"/> and <see cref="SubViewNeedsDisplay"/>.</summary>
    protected virtual void ClearNeedsDisplay ()
    {
        _needsDisplayRect = Rectangle.Empty;
        SubViewNeedsDisplay = false;

        if (Margin is { })
        {
            Margin._needsDisplayRect = Rectangle.Empty;
            Margin.SubViewNeedsDisplay = false;
            Border._needsDisplayRect = Rectangle.Empty;
            Border.SubViewNeedsDisplay = false;
            Padding._needsDisplayRect = Rectangle.Empty;
            Padding.SubViewNeedsDisplay = false;
        }
    }

    // INTENT: Isn't this just intersection? It isn't used anyway.
    // Clips a rectangle in screen coordinates to the dimensions currently available on the screen
    internal Rectangle ScreenClip (Rectangle regionScreen)
    {
        int x = regionScreen.X < 0 ? 0 : regionScreen.X;
        int y = regionScreen.Y < 0 ? 0 : regionScreen.Y;

        int w = regionScreen.X + regionScreen.Width >= Driver.Cols
                    ? Driver.Cols - regionScreen.X
                    : regionScreen.Width;

        int h = regionScreen.Y + regionScreen.Height >= Driver.Rows
                    ? Driver.Rows - regionScreen.Y
                    : regionScreen.Height;

        return new (x, y, w, h);
    }
}
