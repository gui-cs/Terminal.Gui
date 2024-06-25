#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Draws a shadow on the right or bottom of the view. Used internally by <see cref="Margin"/>.
/// </summary>
internal class ShadowView : View
{
    // TODO: Add these to CM.Glyphs
    private readonly char VERTICAL_START_GLYPH = '\u258C'; // Half: '\u2596';
    private readonly char VERTICAL_GLYPH = '\u258C';
    private readonly char HORIZONTAL_START_GLYPH = '\u2580'; // Half: '\u259d';
    private readonly char HORIZONTAL_GLYPH = '\u2580';
    private readonly char HORIZONTAL_END_GLYPH = '\u2598';

    /// <inheritdoc/>
    public override Attribute GetNormalColor ()
    {
        if (SuperView is Adornment adornment)
        {
            Attribute attr = Attribute.Default;
            if (adornment.Parent.SuperView is { })
            {
                attr = adornment.Parent.SuperView.GetNormalColor ();
            }
            else
            {
                attr = Application.Top.GetNormalColor ();
            }
            return new (new Attribute (ShadowStyle == ShadowStyle.Opaque ? Color.Black : attr.Foreground.GetDarkerColor (),
                                       ShadowStyle == ShadowStyle.Opaque ? attr.Background : attr.Background.GetDarkerColor()));
        }

        return base.GetNormalColor ();
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        //base.OnDrawContent (viewport);
        switch (ShadowStyle)
        {
            case ShadowStyle.Opaque:
                if (Orientation == Orientation.Vertical)
                {
                    DrawVerticalShadowOpaque (viewport);
                }
                else
                {
                    DrawHorizontalShadowOpaque (viewport);
                }

                break;

            case ShadowStyle.Transparent:
                //Attribute prevAttr = Driver.GetAttribute ();
                //var attr = new Attribute (prevAttr.Foreground, prevAttr.Background);
                //Driver.SetAttribute (attr);

                if (Orientation == Orientation.Vertical)
                {
                    DrawVerticalShadowTransparent (viewport);
                }
                else
                {
                    DrawHorizontalShadowTransparent (viewport);
                }

                //Driver.SetAttribute (prevAttr);

                break;
        }
    }

    /// <summary>
    ///     Gets or sets the orientation of the shadow.
    /// </summary>
    public Orientation Orientation { get; set; }

    private ShadowStyle _shadowStyle;
    public override ShadowStyle ShadowStyle
    {
        get => _shadowStyle;
        set
        {
            Visible = value != ShadowStyle.None;
            _shadowStyle = value;
        }
    }

    private void DrawHorizontalShadowOpaque (Rectangle rectangle)
    {
        // Draw the start glyph
        AddRune (0, 0, (Rune)HORIZONTAL_START_GLYPH);

        // Fill the rest of the rectangle with the glyph
        for (var i = 1; i < rectangle.Width - 1; i++)
        {
            AddRune (i, 0, (Rune)HORIZONTAL_GLYPH);
        }

        // Last is special
        AddRune (rectangle.Width - 1, 0, (Rune)HORIZONTAL_END_GLYPH);
    }

    private void DrawHorizontalShadowTransparent (Rectangle viewport)
    {
        Rectangle screen = ViewportToScreen (viewport);

        for (int i = screen.X; i < screen.X + screen.Width - 1; i++)
        {
            Driver.Move (i, screen.Y);
            Driver.AddRune (Driver.Contents [screen.Y, i].Rune);
        }
    }

    private void DrawVerticalShadowOpaque (Rectangle viewport)
    {
        // Draw the start glyph
        AddRune (0, 0, (Rune)VERTICAL_START_GLYPH);

        // Fill the rest of the rectangle with the glyph
        for (var i = 1; i < viewport.Height; i++)
        {
            AddRune (0, i, (Rune)VERTICAL_GLYPH);
        }
    }

    private void DrawVerticalShadowTransparent (Rectangle viewport)
    {
        Rectangle screen = ViewportToScreen (viewport);

        // Fill the rest of the rectangle with the glyph
        for (int i = screen.Y; i < screen.Y + viewport.Height; i++)
        {
            Driver.Move (screen.X, i);
            Driver.AddRune (Driver.Contents [i, screen.X].Rune);
        }
    }
}
