#nullable enable

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Draws a shadow on the right or bottom of the view. Used internally by <see cref="Margin"/>.
/// </summary>
internal class ShadowView : View
{
    private ShadowStyle _shadowStyle;

    /// <inheritdoc/>
    protected override bool OnDrawingText () { return true; }

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        // Prevent clearing (so we can have transparency)
        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        switch (ShadowStyle)
        {
            case ShadowStyle.Opaque:
                if (Orientation == Orientation.Vertical)
                {
                    DrawVerticalShadowOpaque (Viewport);
                }
                else
                {
                    DrawHorizontalShadowOpaque (Viewport);
                }

                break;

            case ShadowStyle.Transparent:
                if (Orientation == Orientation.Vertical)
                {
                    DrawVerticalShadowTransparent (Viewport);
                }
                else
                {
                    DrawHorizontalShadowTransparent (Viewport);
                }

                break;
        }

        return true;
    }

    /// <summary>
    ///     Gets or sets the orientation of the shadow.
    /// </summary>
    public Orientation Orientation { get; set; }

    public override ShadowStyle ShadowStyle
    {
        get => _shadowStyle;
        set
        {
            Visible = value != ShadowStyle.None;
            _shadowStyle = value;

            ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        }
    }

    private void DrawHorizontalShadowOpaque (Rectangle rectangle)
    {
        // Draw the start glyph
        SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (0, 0))));
        AddRune (0, 0, Glyphs.ShadowHorizontalStart);

        // Fill the rest of the rectangle with the glyph - note we skip the last since vertical will draw it
        for (var i = 1; i < rectangle.Width - 1; i++)
        {
            SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (i, 0))));
            AddRune (i, 0, Glyphs.ShadowHorizontal);
        }

        // Last is special
        SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (rectangle.Width - 1, 0))));
        AddRune (rectangle.Width - 1, 0, Glyphs.ShadowHorizontalEnd);
    }

    private void DrawHorizontalShadowTransparent (Rectangle viewport)
    {
        Rectangle screen = ViewportToScreen (Viewport);

        for (int r = Math.Max (0, screen.Y); r < screen.Y + screen.Height; r++)
        {
            for (int c = Math.Max (0, screen.X + 1); c < screen.X + screen.Width; c++)
            {
                Driver?.Move (c, r);
                SetAttribute (GetAttributeUnderLocation (new (c, r)));

                if (c < Driver?.Contents!.GetLength (1) && r < Driver?.Contents?.GetLength (0))
                {
                    Driver.AddRune (Driver.Contents [r, c].Rune);
                }
            }
        }
    }

    private void DrawVerticalShadowOpaque (Rectangle viewport)
    {
        // Draw the start glyph
        SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (0, 0))));
        AddRune (0, 0, Glyphs.ShadowVerticalStart);

        // Fill the rest of the rectangle with the glyph
        for (var i = 1; i < viewport.Height - 1; i++)
        {
            SetAttribute (GetAttributeUnderLocation (ViewportToScreen (new Point (0, i))));
            AddRune (0, i, Glyphs.ShadowVertical);
        }
    }

    private void DrawVerticalShadowTransparent (Rectangle viewport)
    {
        Rectangle screen = ViewportToScreen (Viewport);

        // Fill in the rest of the rectangle
        for (int c = Math.Max (0, screen.X); c < screen.X + screen.Width; c++)
        {
            for (int r = Math.Max (0, screen.Y); r < screen.Y + viewport.Height; r++)
            {
                Driver?.Move (c, r);
                SetAttribute (GetAttributeUnderLocation (new (c, r)));

                if (Driver?.Contents is { } && screen.X < Driver.Contents.GetLength (1) && r < Driver.Contents.GetLength (0))
                {
                    Driver.AddRune (Driver.Contents [r, c].Rune);
                }
            }
        }
    }

    private Attribute GetAttributeUnderLocation (Point location)
    {
        if (SuperView is not Adornment adornment
            || location.X < 0
            || location.X >= Application.Screen.Width
            || location.Y < 0
            || location.Y >= Application.Screen.Height)
        {
            return Attribute.Default;
        }

        Attribute attr = Driver!.Contents! [location.Y, location.X].Attribute!.Value;

        var newAttribute =
            new Attribute (
                           ShadowStyle == ShadowStyle.Opaque ? Color.Black : attr.Foreground.GetDimColor (),
                           ShadowStyle == ShadowStyle.Opaque ? attr.Background : attr.Background.GetDimColor (0.05),
                           attr.Style);

        // If the BG is DarkGray, GetDimColor gave up. Instead of using the attribute in the Driver under the shadow,
        // use the Normal attribute from the View under the shadow.
        if (newAttribute.Background == Color.DarkGray)
        {
            List<View?> currentViewsUnderMouse = View.GetViewsUnderLocation (location, ViewportSettingsFlags.Transparent);
            View? underView = currentViewsUnderMouse!.LastOrDefault ();
            attr = underView?.GetAttributeForRole (VisualRole.Normal) ?? Attribute.Default;

            newAttribute = new (
                                ShadowStyle == ShadowStyle.Opaque ? Color.Black : attr.Background.GetDimColor (),
                                ShadowStyle == ShadowStyle.Opaque ? attr.Background : attr.Foreground.GetDimColor (0.25),
                                attr.Style);
        }

        return newAttribute;
    }
}
