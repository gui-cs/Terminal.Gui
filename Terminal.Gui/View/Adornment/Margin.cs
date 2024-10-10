#nullable enable

namespace Terminal.Gui;

/// <summary>The Margin for a <see cref="View"/>. Accessed via <see cref="View.Margin"/></summary>
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

        // BUGBUG: We should not set HighlightStyle.Pressed here, but wherever it is actually needed
       // HighlightStyle |= HighlightStyle.Pressed;
        Highlight += Margin_Highlight;
        LayoutStarted += Margin_LayoutStarted;

        // Margin should not be focusable
        CanFocus = false;
    }

    private bool _pressed;

    private ShadowView? _bottomShadow;
    private ShadowView? _rightShadow;

    /// <inheritdoc/>
    public override void BeginInit ()
    {
        base.BeginInit ();

        if (Parent is null)
        {
            return;
        }

        ShadowStyle = base.ShadowStyle;

        Add (
             _rightShadow = new ()
             {
                 X = Pos.AnchorEnd (1),
                 Y = 0,
                 Width = 1,
                 Height = Dim.Fill (),
                 ShadowStyle = ShadowStyle,
                 Orientation = Orientation.Vertical
             },
             _bottomShadow = new ()
             {
                 X = 0,
                 Y = Pos.AnchorEnd (1),
                 Width = Dim.Fill (),
                 Height = 1,
                 ShadowStyle = ShadowStyle,
                 Orientation = Orientation.Horizontal
             }
            );
    }

    /// <summary>
    ///     The color scheme for the Margin. If set to <see langword="null"/>, gets the <see cref="Adornment.Parent"/>'s
    ///     <see cref="View.SuperView"/> scheme. color scheme.
    /// </summary>
    public override ColorScheme? ColorScheme
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

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        if (!NeedsDisplay)
        {
            return;
        }

        Rectangle screen = ViewportToScreen (viewport);
        Attribute normalAttr = GetNormalColor ();

        Driver?.SetAttribute (normalAttr);

        if (ShadowStyle != ShadowStyle.None)
        {
            screen = Rectangle.Inflate (screen, -1, -1);
        }

        // This just draws/clears the thickness, not the insides.
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
    ///     Sets whether the Margin includes a shadow effect. The shadow is drawn on the right and bottom sides of the
    ///     Margin.
    /// </summary>
    public ShadowStyle SetShadow (ShadowStyle style)
    {
        if (ShadowStyle == style)
        {
            // return style;
        }

        if (ShadowStyle != ShadowStyle.None)
        {
            // Turn off shadow
            Thickness = new (Thickness.Left, Thickness.Top, Thickness.Right - 1, Thickness.Bottom - 1);
        }

        if (style != ShadowStyle.None)
        {
            // Turn on shadow
            Thickness = new (Thickness.Left, Thickness.Top, Thickness.Right + 1, Thickness.Bottom + 1);
        }

        if (_rightShadow is { })
        {
            _rightShadow.ShadowStyle = style;
        }

        if (_bottomShadow is { })
        {
            _bottomShadow.ShadowStyle = style;
        }

        return style;
    }

    /// <inheritdoc/>
    public override ShadowStyle ShadowStyle
    {
        get => base.ShadowStyle;
        set => base.ShadowStyle = SetShadow (value);
    }

    private const int PRESS_MOVE_HORIZONTAL = 1;
    private const int PRESS_MOVE_VERTICAL = 0;

    private void Margin_Highlight (object? sender, CancelEventArgs<HighlightStyle> e)
    {
        if (ShadowStyle != ShadowStyle.None)
        {
            if (_pressed && e.NewValue == HighlightStyle.None)
            {
                // If the view is pressed and the highlight is being removed, move the shadow back.
                // Note, for visual effects reasons, we only move horizontally.
                // TODO: Add a setting or flag that lets the view move vertically as well.
                Thickness = new (Thickness.Left - PRESS_MOVE_HORIZONTAL, Thickness.Top - PRESS_MOVE_VERTICAL, Thickness.Right + PRESS_MOVE_HORIZONTAL, Thickness.Bottom + PRESS_MOVE_VERTICAL);

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

            if (!_pressed && e.NewValue.HasFlag (HighlightStyle.Pressed))
            {
                // If the view is not pressed and we want highlight move the shadow
                // Note, for visual effects reasons, we only move horizontally.
                // TODO: Add a setting or flag that lets the view move vertically as well.
                Thickness = new (Thickness.Left + PRESS_MOVE_HORIZONTAL, Thickness.Top+ PRESS_MOVE_VERTICAL, Thickness.Right - PRESS_MOVE_HORIZONTAL, Thickness.Bottom - PRESS_MOVE_VERTICAL);
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

    private void Margin_LayoutStarted (object? sender, LayoutEventArgs e)
    {
        // Adjust the shadow such that it is drawn aligned with the Border
        if (_rightShadow is { } && _bottomShadow is { })
        {
            switch (ShadowStyle)
            {
                case ShadowStyle.Transparent:
                    // BUGBUG: This doesn't work right for all Border.Top sizes - Need an API on Border that gives top-right location of line corner.
                    _rightShadow.Y = Parent!.Border.Thickness.Top > 0 ? ScreenToViewport (Parent.Border.GetBorderRectangle ().Location).Y + 1 : 0;
                    break;

                case ShadowStyle.Opaque:
                    // BUGBUG: This doesn't work right for all Border.Top sizes - Need an API on Border that gives top-right location of line corner.
                    _rightShadow.Y = Parent!.Border.Thickness.Top > 0 ? ScreenToViewport (Parent.Border.GetBorderRectangle ().Location).Y + 1 : 0;
                    _bottomShadow.X = Parent.Border.Thickness.Left > 0 ? ScreenToViewport (Parent.Border.GetBorderRectangle ().Location).X + 1 : 0;
                    break;

                case ShadowStyle.None:
                default:
                    _rightShadow.Y = 0;
                    _bottomShadow.X = 0;
                    break;
            }
        }
    }
}
