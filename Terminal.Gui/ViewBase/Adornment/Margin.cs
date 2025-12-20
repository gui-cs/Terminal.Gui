namespace Terminal.Gui.ViewBase;

/// <summary>The Margin for a <see cref="View"/>. Accessed via <see cref="View.Margin"/></summary>
/// <remarks>
///     <para>
///         The Margin is transparent by default. This can be overriden by explicitly setting <see cref="Scheme"/>.
///     </para>
///     <para>
///         Margins are drawn after all other Views in the application View hierarchy are drawn.
///     </para>
///     <para>
///         Margins have <see cref="ViewportSettingsFlags.TransparentMouse"/> enabled by default and are thus
///         transparent to the mouse. This can be overridden by explicitly setting <see cref="ViewportSettingsFlags"/>.
///     </para>
///     <para>See the <see cref="Adornment"/> class.</para>
/// </remarks>
public class Margin : Adornment
{
    private const int PRESS_MOVE_HORIZONTAL = 1;
    private const int PRESS_MOVE_VERTICAL = 0;

    /// <inheritdoc/>
    public Margin()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public Margin(View parent) : base(parent)
    {
        SubViewLayout += Margin_LayoutStarted;
        ThicknessChanged += OnThicknessChanged;

        // Margin should not be focusable
        CanFocus = false;

        // Margins are transparent by default
        ViewportSettings |= ViewportSettingsFlags.Transparent;

        // Margins are transparent to mouse by default
        ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
    }

    private void OnThicknessChanged(object? sender, EventArgs e)
    {
        if (!_isThicknessChanging)
        {
            _originalThickness = new(Thickness.Left, Thickness.Top, Thickness.Right, Thickness.Bottom);
            SetShadow(ShadowStyle);
        }
    }

    // When the Parent is drawn, we cache the clip region so we can draw the Margin after all other Views
    // QUESTION: Why can't this just be the NeedsDisplay region?
    private Region? _cachedClip;

    internal Region? GetCachedClip() { return _cachedClip; }

    internal void ClearCachedClip() { _cachedClip = null; }

    internal void CacheClip()
    {
        if (Thickness != Thickness.Empty && ShadowStyle != ShadowStyle.None)
        {
            // PERFORMANCE: How expensive are these clones?
            _cachedClip = GetClip()?.Clone();
        }
    }

    /// <summary>
    ///     INTERNAL API - Draws the transparent margins for the specified views. This is called from <see cref="View.Draw(DrawContext)"/> on each
    ///     iteration of the main loop after all Views have been drawn.
    /// </summary>
    /// <remarks>
    ///     Non-transparent margins are drawn as-normal in <see cref="View.DrawAdornments"/>.
    /// </remarks>
    /// <param name="views"></param>
    /// <returns><see langword="true"/></returns>
    internal static bool DrawMargins(IEnumerable<View> views)
    {
        Stack<View> stack = new(views);

        while (stack.Count > 0)
        {
            View view = stack.Pop();

            if (view.Margin is { } margin
                && margin.Thickness != Thickness.Empty
                && margin.ViewportSettings.HasFlag(ViewportSettingsFlags.Transparent)
                && margin.GetCachedClip() != null)
            {
                margin.SetNeedsDraw();
                Region? saved = view.GetClip();
                view.SetClip(margin.GetCachedClip());
                margin.Draw();
                view.SetClip(saved);
                margin.ClearCachedClip();
            }

            foreach (View subview in view.GetSubViews(includePadding: true)
                                         .OrderBy(v => v.ShadowStyle != ShadowStyle.None)
                                         .Reverse())
            {
                stack.Push(subview);
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override void BeginInit()
    {
        base.BeginInit();

        if (Parent is null)
        {
            return;
        }

        ShadowStyle = base.ShadowStyle;

        Parent.MouseStateChanged += OnParentOnMouseStateChanged;
    }

    /// <inheritdoc/>
    protected override bool OnClearingViewport()
    {
        if (Thickness == Thickness.Empty)
        {
            return true;
        }

        Rectangle screen = ViewportToScreen(Viewport);

        if (Diagnostics.HasFlag(ViewDiagnosticFlags.Thickness) || HasScheme)
        {
            // This just draws/clears the thickness, not the insides.
            // TODO: This is a hack. See https://github.com/gui-cs/Terminal.Gui/issues/4016
            //SetAttribute (GetAttributeForRole (VisualRole.Normal));
            Thickness.Draw(Driver, screen, Diagnostics, ToString());
        }

        if (ShadowStyle != ShadowStyle.None)
        {
            // Don't clear where the shadow goes
            screen = Rectangle.Inflate(screen, -ShadowSize.Width, -ShadowSize.Height);
        }

        return true;
    }

    /// <inheritdoc />
    protected override bool OnDrawingText()
    {
        return ViewportSettings.HasFlag(ViewportSettingsFlags.Transparent);
    }

    #region Shadow

    // private bool _pressed;
    private ShadowView? _bottomShadow;
    private ShadowView? _rightShadow;
    private bool _isThicknessChanging;
    private Thickness? _originalThickness;

    /// <summary>
    ///     Sets whether the Margin includes a shadow effect. The shadow is drawn on the right and bottom sides of the
    ///     Margin.
    /// </summary>
    public ShadowStyle SetShadow(ShadowStyle style)
    {
        if (_rightShadow is { })
        {
            Remove(_rightShadow);
            _rightShadow.Dispose();
            _rightShadow = null;
        }

        if (_bottomShadow is { })
        {
            Remove(_bottomShadow);
            _bottomShadow.Dispose();
            _bottomShadow = null;
        }

        _originalThickness ??= Thickness;

        if (ShadowStyle != ShadowStyle.None)
        {
            // Turn off shadow
            _originalThickness = new(Thickness.Left, Thickness.Top, Math.Max(Thickness.Right - ShadowSize.Width, 0), Math.Max(Thickness.Bottom - ShadowSize.Height, 0));
        }

        if (style != ShadowStyle.None)
        {
            // Turn on shadow
            _isThicknessChanging = true;
            Thickness = new(_originalThickness.Value.Left, _originalThickness.Value.Top, _originalThickness.Value.Right + ShadowSize.Width, _originalThickness.Value.Bottom + ShadowSize.Height);
            _isThicknessChanging = false;
        }

        if (style != ShadowStyle.None)
        {
            _rightShadow = new()
            {
                X = Pos.AnchorEnd(ShadowSize.Width),
                Y = 0,
                Width = ShadowSize.Width,
                Height = Dim.Fill(),
                ShadowStyle = style,
                Orientation = Orientation.Vertical
            };

            _bottomShadow = new()
            {
                X = 0,
                Y = Pos.AnchorEnd(ShadowSize.Height),
                Width = Dim.Fill(),
                Height = ShadowSize.Height,
                ShadowStyle = style,
                Orientation = Orientation.Horizontal
            };
            Add(_rightShadow, _bottomShadow);
        }
        else if (Thickness != _originalThickness)
        {
            _isThicknessChanging = true;
            Thickness = new(_originalThickness.Value.Left, _originalThickness.Value.Top, _originalThickness.Value.Right, _originalThickness.Value.Bottom);
            _isThicknessChanging = false;
        }

        return style;
    }

    /// <inheritdoc/>
    public override ShadowStyle ShadowStyle
    {
        get => base.ShadowStyle;
        set
        {
            if (value == ShadowStyle.Opaque || (value == ShadowStyle.Transparent && (ShadowSize.Width == 0 || ShadowSize.Height == 0)))
            {
                if (ShadowSize.Width != 1)
                {
                    ShadowSize = ShadowSize with { Width = 1 };
                }

                if (ShadowSize.Height != 1)
                {
                    ShadowSize = ShadowSize with { Height = 1 };
                }
            }

            base.ShadowStyle = SetShadow(value);
        }
    }

    private Size _shadowSize;

    /// <summary>
    ///     Gets or sets the size of the shadow effect.
    /// </summary>
    public Size ShadowSize
    {
        get => _shadowSize;
        set
        {
            if (TryValidateShadowSize(_shadowSize, value, out Size result))
            {
                _shadowSize = value;
                SetShadow(ShadowStyle);
            }
            else
            {
                _shadowSize = result;
            }
        }
    }

    private bool TryValidateShadowSize(Size originalValue, in Size newValue, out Size result)
    {
        result = newValue;

        bool wasValid = true;

        if (newValue.Width < 0)
        {
            result = ShadowStyle is ShadowStyle.Opaque or ShadowStyle.Transparent ? result with { Width = 1 } : originalValue;

            wasValid = false;
        }


        if (newValue.Height < 0)
        {
            result = ShadowStyle is ShadowStyle.Opaque or ShadowStyle.Transparent ? result with { Height = 1 } : originalValue;

            wasValid = false;
        }

        if (!wasValid)
        {
            return false;
        }

        bool wasUpdated = false;

        if ((ShadowStyle == ShadowStyle.Opaque && newValue.Width != 1) || (ShadowStyle == ShadowStyle.Transparent && newValue.Width < 1))
        {
            result = result with { Width = 1 };

            wasUpdated = true;
        }

        if ((ShadowStyle == ShadowStyle.Opaque && newValue.Height != 1) || (ShadowStyle == ShadowStyle.Transparent && newValue.Height < 1))
        {
            result = result with { Height = 1 };

            wasUpdated = true;
        }

        return !wasUpdated;
    }

    private void OnParentOnMouseStateChanged(object? sender, EventArgs<MouseState> args)
    {
        if (sender is not View parent || Thickness == Thickness.Empty || ShadowStyle == ShadowStyle.None)
        {
            return;
        }

        bool pressed = args.Value.HasFlag(MouseState.Pressed) && parent.HighlightStates.HasFlag(MouseState.Pressed);
        bool pressedOutside = args.Value.HasFlag(MouseState.PressedOutside) && parent.HighlightStates.HasFlag(MouseState.PressedOutside);

        if (pressedOutside)
        {
            pressed = false;
        }

        if (MouseState.HasFlag(MouseState.Pressed) && !pressed)
        {
            // If the view is pressed and the highlight is being removed, move the shadow back.
            // Note, for visual effects reasons, we only move horizontally.
            // TODO: Add a setting or flag that lets the view move vertically as well.
            _isThicknessChanging = true;
            Thickness = new(
                             Thickness.Left - PRESS_MOVE_HORIZONTAL,
                             Thickness.Top - PRESS_MOVE_VERTICAL,
                             Thickness.Right + PRESS_MOVE_HORIZONTAL,
                             Thickness.Bottom + PRESS_MOVE_VERTICAL);
            _isThicknessChanging = false;

            if (_rightShadow is { })
            {
                _rightShadow.Visible = true;
            }

            if (_bottomShadow is { })
            {
                _bottomShadow.Visible = true;
            }

            MouseState &= ~MouseState.Pressed;

            return;
        }

        if (!MouseState.HasFlag(MouseState.Pressed) && pressed)
        {
            // If the view is not pressed, and we want highlight move the shadow
            // Note, for visual effects reasons, we only move horizontally.
            // TODO: Add a setting or flag that lets the view move vertically as well.
            _isThicknessChanging = true;
            Thickness = new(
                             Thickness.Left + PRESS_MOVE_HORIZONTAL,
                             Thickness.Top + PRESS_MOVE_VERTICAL,
                             Thickness.Right - PRESS_MOVE_HORIZONTAL,
                             Thickness.Bottom - PRESS_MOVE_VERTICAL);
            _isThicknessChanging = false;

            MouseState |= MouseState.Pressed;

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

    private void Margin_LayoutStarted(object? sender, LayoutEventArgs e)
    {
        // Adjust the shadow such that it is drawn aligned with the Border
        if (_rightShadow is { } && _bottomShadow is { })
        {
            switch (ShadowStyle)
            {
                case ShadowStyle.Transparent:
                    // BUGBUG: This doesn't work right for all Border.Top sizes - Need an API on Border that gives top-right location of line corner.
                    _rightShadow.Y = Parent!.Border!.Thickness.Top > 0 ? ScreenToViewport(Parent.Border!.GetBorderRectangle().Location).Y + 1 : 0;

                    break;

                case ShadowStyle.Opaque:
                    // BUGBUG: This doesn't work right for all Border.Top sizes - Need an API on Border that gives top-right location of line corner.
                    _rightShadow.Y = Parent!.Border!.Thickness.Top > 0 ? ScreenToViewport(Parent.Border!.GetBorderRectangle().Location).Y + 1 : 0;
                    _bottomShadow.X = Parent.Border!.Thickness.Left > 0 ? ScreenToViewport(Parent.Border!.GetBorderRectangle().Location).X + 1 : 0;

                    break;

                case ShadowStyle.None:
                default:
                    _rightShadow.Y = 0;
                    _bottomShadow.X = 0;

                    break;
            }
        }
    }

    #endregion Shadow

}
