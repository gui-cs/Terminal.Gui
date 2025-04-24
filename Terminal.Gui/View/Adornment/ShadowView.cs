#nullable enable
using Microsoft.VisualBasic;
using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     Draws a shadow on the right or bottom of the view. Used internally by <see cref="Margin"/>.
/// </summary>
internal class ShadowView : View
{
    private ShadowStyle _shadowStyle;

    /// <inheritdoc/>
    public override Attribute GetNormalColor ()
    {
        if (SuperView is not Adornment adornment)
        {
            return base.GetNormalColor ();
        }

        var attr = Attribute.Default;

        if (adornment.Parent?.SuperView is { })
        {
            attr = adornment.Parent.SuperView.GetNormalColor ();
        }
        else if (Application.Top is { })
        {
            attr = Application.Top.GetNormalColor ();
        }

        return new (
                    new Attribute (
                                   ShadowStyle == ShadowStyle.Opaque ? Color.Black : attr.Foreground.GetDarkerColor (),
                                   ShadowStyle == ShadowStyle.Opaque ? attr.Background : attr.Background.GetDarkerColor ()));

    }

    /// <inheritdoc />
    /// <inheritdoc />
    protected override bool OnDrawingText ()
    {
        return true;
    }

    /// <inheritdoc />
    protected override bool OnClearingViewport ()
    {
        // Prevent clearing (so we can have transparency)
        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        SetAttribute (GetNormalColor ());
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
                //Attribute prevAttr = Driver.GetAttribute ();
                //var attr = new Attribute (prevAttr.Foreground, prevAttr.Background);
                //SetAttribute (attr);

                if (Orientation == Orientation.Vertical)
                {
                    DrawVerticalShadowTransparent (Viewport);
                }
                else
                {
                    DrawHorizontalShadowTransparent (Viewport);
                }

                //SetAttribute (prevAttr);

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
        }
    }

    private void DrawHorizontalShadowOpaque (Rectangle rectangle)
    {
        // Draw the start glyph
        AddRune (0, 0, Glyphs.ShadowHorizontalStart);

        // Fill the rest of the rectangle with the glyph - note we skip the last since vertical will draw it
        for (var i = 1; i < rectangle.Width - 1; i++)
        {
            AddRune (i, 0, Glyphs.ShadowHorizontal);
        }

        // Last is special
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
        AddRune (0, 0, Glyphs.ShadowVerticalStart);

        // Fill the rest of the rectangle with the glyph
        for (var i = 1; i < viewport.Height - 1; i++)
        {
            AddRune (0, i, Glyphs.ShadowVertical);
        }
    }

    private void DrawVerticalShadowTransparent (Rectangle viewport)
    {
        Rectangle screen = ViewportToScreen (Viewport);

        // Fill the rest of the rectangle
        for (int c = Math.Max (0, screen.X); c < screen.X + screen.Width; c++)
        {
            for (int r = Math.Max (0, screen.Y); r < screen.Y + viewport.Height; r++)
            {
                Driver?.Move (c, r);

                if (Driver?.Contents is { } && screen.X < Driver.Contents.GetLength (1) && r < Driver.Contents.GetLength (0))
                {
                    Driver.AddRune (Driver.Contents [r, c].Rune);
                }
            }
        }
    }
}
