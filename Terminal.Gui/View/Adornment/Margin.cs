#nullable enable
using static Unix.Terminal.Curses;

namespace Terminal.Gui;

/// <summary>The Margin for a <see cref="View"/>.</summary>
/// <remarks>
///     <para>See the <see cref="Adornment"/> class.</para>
/// </remarks>
public class Margin : Adornment
{
    /// <inheritdoc/>
    public Margin ()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public Margin (View parent) : base (parent)
    {
        /* Do nothing; View.CreateAdornment requires a constructor that takes a parent */

        HighlightStyle |= HighlightStyle.Pressed;
        Highlight += Margin_Highlight;
        LayoutStarted += Margin_LayoutStarted;
    }

    private void Margin_LayoutStarted (object? sender, LayoutEventArgs e)
    {
        // Adjust the shadow such that it is drawn aligned with the Border
        if (_shadow && _rightShadow is { } && _bottomShadow is { })
        {
            _rightShadow.Y = Parent.Border.Thickness.Top > 0 ? Parent.Border.Thickness.Top - (Parent.Border.Thickness.Top > 2 && Parent.Border.ShowTitle ? 1 : 0) : 1;
            _bottomShadow.X = Parent.Border.Thickness.Left > 0 ? Parent.Border.Thickness.Left : 1;
        }
    }

    private bool _pressed;
    private void Margin_Highlight (object? sender, HighlightEventArgs e)
    {
        if (_shadow)
        {
            if (_pressed && e.HighlightStyle == HighlightStyle.None)
            {
                Thickness = new (Thickness.Left - 1, Thickness.Top, Thickness.Right + 1, Thickness.Bottom);

                if (_rightShadow is { })
                {
                    _rightShadow.Visible = true;
                }

                if (_bottomShadow is { })
                {
                    _bottomShadow.Visible = true;
                }

                _pressed = false;
                return;
            }

            if (!_pressed && (e.HighlightStyle.HasFlag (HighlightStyle.Pressed) /*|| e.HighlightStyle.HasFlag (HighlightStyle.PressedOutside)*/))
            {
                Thickness = new (Thickness.Left + 1, Thickness.Top, Thickness.Right - 1, Thickness.Bottom);
                _pressed = true;
                if (_rightShadow is { })
                {
                    _rightShadow.Visible = false;
                }

                if (_bottomShadow is { })
                {
                    _bottomShadow.Visible = false;
                }
            }
        }

    }

    public override void OnDrawContent (Rectangle viewport)
    {
        Rectangle screen = ViewportToScreen (viewport);
        Attribute normalAttr = GetNormalColor ();
        Driver.SetAttribute (normalAttr);

        // This just draws/clears the thickness, not the insides.
        if (Parent?.Shadow == true)
        {
            screen = Rectangle.Inflate (screen, -1, -1);
        }
        Thickness.Draw (screen, ToString ());

        if (Subviews.Count > 0)
        {
            // Draw subviews
            // TODO: Implement OnDrawSubviews (cancelable);
            if (Subviews is { } && SubViewNeedsDisplay)
            {
                IEnumerable<View> subviewsNeedingDraw = Subviews.Where (
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
    }

    /// <summary>
    ///     The color scheme for the Margin. If set to <see langword="null"/>, gets the <see cref="Adornment.Parent"/>'s
    ///     <see cref="View.SuperView"/> scheme. color scheme.
    /// </summary>
    public override ColorScheme ColorScheme
    {
        get
        {
            if (base.ColorScheme is { })
            {
                return base.ColorScheme;
            }

            return (Parent?.SuperView?.ColorScheme ?? Colors.ColorSchemes ["TopLevel"])!;
        }
        set
        {
            base.ColorScheme = value;
            Parent?.SetNeedsDisplay ();
        }
    }

    private bool _shadow;

    /// <summary>
    ///     Gets or sets whether the Margin includes a shadow effect. The shadow is drawn on the right and bottom sides of the
    ///     Margin.
    /// </summary>
    public bool EnableShadow (bool enable)
    {
        if (_shadow == enable)
        {
            return _shadow;
        }

        if (_shadow)
        {
            Thickness = new (Thickness.Left, Thickness.Top, Thickness.Right - 1, Thickness.Bottom - 1);
        }

        _shadow = enable;

        if (_shadow)
        {
            Thickness = new (Thickness.Left, Thickness.Top, Thickness.Right + 1, Thickness.Bottom + 1);
        }

        if (_rightShadow is { })
        {
            _rightShadow.Visible = _shadow;
        }

        if (_bottomShadow is { })
        {
            _bottomShadow.Visible = _shadow;
        }
        return _shadow;
    }

    private View? _bottomShadow;
    private View? _rightShadow;

    /// <inheritdoc/>
    public override void BeginInit ()
    {
        base.BeginInit ();

        if (Parent is null)
        {
            return;
        }

        Attribute attr = Parent.GetNormalColor ();

        Add (
             _rightShadow = new ShadowView
             {
                 X = Pos.AnchorEnd (1),
                 Y = 0,
                 Width = 1,
                 Height = Dim.Fill (),
                 Visible = _shadow,
                 Orientation = Orientation.Vertical
             },
             _bottomShadow = new ShadowView
             {
                 X = 0,
                 Y = Pos.AnchorEnd (1),
                 Width = Dim.Fill (),
                 Height = 1,
                 Visible = _shadow,
                 Orientation = Orientation.Horizontal
             }
            );
    }
}

/// <summary>
///     Draws a shadow on the right or bottom of the view.
/// </summary>
internal class ShadowView : View
{
    // TODO: Add these to CM.Glyphs
    private readonly char VERTICAL_START_GLYPH = '\u258C'; // Half: '\u2596';
    private readonly char VERTICAL_GLYPH = '\u258C';
    private readonly char HORIZONTAL_START_GLYPH = '\u2580'; // Half: '\u259d';
    private readonly char HORIZONTAL_GLYPH = '\u2580';
    private readonly char HORIZONTAL_END_GLYPH = '\u2598';

    /// <summary>
    ///     Gets or sets the orientation of the shadow.
    /// </summary>
    public Orientation Orientation { get; set; }

    /// <inheritdoc />
    public override Attribute GetNormalColor ()
    {
        if (SuperView is Adornment adornment)
        {
            if (adornment.Parent.SuperView is { })
            {
                Attribute attr = adornment.Parent.SuperView.GetNormalColor ();
                return new (new Attribute (attr.Foreground.GetDarkerColor (), attr.Background));
            }
            else
            {
                Attribute attr = Application.Top.GetNormalColor ();
                return new (new Attribute (attr.Foreground.GetDarkerColor (), attr.Background));
            }
        }
        return base.GetNormalColor ();
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        //base.OnDrawContent (viewport);

        if (Orientation == Orientation.Vertical)
        {
            DrawVerticalShadow (viewport);
        }
        else
        {
            DrawHorizontalShadow (viewport);
        }
    }

    private void DrawHorizontalShadow (Rectangle rectangle)
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

    private void DrawVerticalShadow (Rectangle viewport)
    {
        // Draw the start glyph
        AddRune (0, 0, (Rune)VERTICAL_START_GLYPH);

        // Fill the rest of the rectangle with the glyph
        for (var i = 1; i < viewport.Height; i++)
        {
            AddRune (0, i, (Rune)VERTICAL_GLYPH);
        }
    }
}
