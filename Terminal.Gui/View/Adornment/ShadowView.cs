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
        if (SuperView is Adornment adornment)
        {
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
        Rectangle screen = ViewportToScreen (viewport);

        // Fill the rest of the rectangle - note we skip the last since vertical will draw it
        for (int i = Math.Max(0, screen.X + 1); i < screen.X + screen.Width - 1; i++)
        {
            Driver.Move (i, screen.Y);

            if (i < Driver.Contents!.GetLength (1) && screen.Y < Driver.Contents.GetLength (0))
            {
                Driver.AddRune (Driver.Contents [screen.Y, i].Rune);
            }
        }
    }

    private void DrawVerticalShadowOpaque (Rectangle viewport)
    {
        // Draw the start glyph
        AddRune (0, 0, Glyphs.ShadowVerticalStart);

        // Fill the rest of the rectangle with the glyph
        for (var i = 1; i < viewport.Height; i++)
        {
            AddRune (0, i, Glyphs.ShadowVertical);
        }
    }

    private void DrawVerticalShadowTransparent (Rectangle viewport)
    {
        Rectangle screen = ViewportToScreen (viewport);

        // Fill the rest of the rectangle
        for (int i = Math.Max (0, screen.Y); i < screen.Y + viewport.Height; i++)
        {
            Driver.Move (screen.X, i);

            if (Driver.Contents is { } && screen.X < Driver.Contents.GetLength (1) && i < Driver.Contents.GetLength (0))
            {
                Driver.AddRune (Driver.Contents [i, screen.X].Rune);
            }
        }
    }
}
