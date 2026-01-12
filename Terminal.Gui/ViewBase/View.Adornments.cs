namespace Terminal.Gui.ViewBase;

public partial class View // Adornments
{
    /// <summary>
    ///     Initializes the Adornments of the View. Called by the constructor.
    /// </summary>
    private void SetupAdornments ()
    {
        // Adornments are now created lazily when first accessed
        // Nothing to do here
    }

    /// <summary>
    ///     Ensures the Margin is created if it doesn't exist.
    /// </summary>
    private Margin EnsureMargin ()
    {
        if (_margin is null && this is not Adornment)
        {
            // Create a placeholder to prevent re-entrance during construction
            var margin = new Margin (this);
            _margin = margin;
        }
        return _margin!;
    }

    /// <summary>
    ///     Ensures the Border is created if it doesn't exist.
    /// </summary>
    private Border EnsureBorder ()
    {
        if (_border is null && this is not Adornment)
        {
            // Create a placeholder to prevent re-entrance during construction
            var border = new Border (this);
            _border = border;
        }
        return _border!;
    }

    /// <summary>
    ///     Ensures the Padding is created if it doesn't exist.
    /// </summary>
    private Padding EnsurePadding ()
    {
        if (_padding is null && this is not Adornment)
        {
            // Create a placeholder to prevent re-entrance during construction
            var padding = new Padding (this);
            _padding = padding;
        }
        return _padding!;
    }

    private void BeginInitAdornments ()
    {
        _margin?.BeginInit ();
        _border?.BeginInit ();
        _padding?.BeginInit ();
    }

    private void EndInitAdornments ()
    {
        _margin?.EndInit ();
        _border?.EndInit ();
        _padding?.EndInit ();
    }

    private void DisposeAdornments ()
    {
        _margin?.Dispose ();
        _margin = null;
        _border?.Dispose ();
        _border = null;
        _padding?.Dispose ();
        _padding = null;
    }

    /// <summary>
    ///     The <see cref="Adornment"/> that enables separation of a View from other SubViews of the same
    ///     SuperView. The margin offsets the <see cref="Viewport"/> from the <see cref="Frame"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The margin is typically transparent. This can be overriden by explicitly setting <see cref="Scheme"/>.
    ///     </para>
    ///     <para>
    ///         Enabling <see cref="ShadowStyle"/> will change the Thickness of the Margin to include the shadow.
    ///     </para>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of an adornment (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of <see cref="Frame"/> which will call <see cref="SetNeedsLayout"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="SubViews"/>.
    ///     </para>
    /// </remarks>
    private Margin? _margin;
    
    /// <inheritdoc cref="_margin"/>
    public Margin? Margin
    {
        get
        {
            if (_margin is null && this is not Adornment)
            {
                _margin = new Margin (this);
            }
            return _margin;
        }
        private set => _margin = value;
    }

    private ShadowStyle _shadowStyle;

    /// <summary>
    ///     Gets or sets whether the View is shown with a shadow effect. The shadow is drawn on the right and bottom sides of
    ///     the
    ///     Margin.
    /// </summary>
    /// <remarks>
    ///     Setting this property to <see langword="true"/> will add a shadow to the right and bottom sides of the Margin.
    ///     The View 's <see cref="Frame"/> will be expanded to include the shadow.
    /// </remarks>
    public virtual ShadowStyle ShadowStyle
    {
        get => _shadowStyle;
        set
        {
            if (_shadowStyle == value)
            {
                return;
            }

            _shadowStyle = value;

            // Adornments don't have adornments, so don't try to create a Margin
            if (this is Adornment)
            {
                return;
            }

            // Only propagate to margin if it already exists or if we're setting a non-None value
            // This avoids creating a margin just to set it to None
            if (value != ShadowStyle.None)
            {
                // Create margin if needed and set the shadow style
                Margin margin = EnsureMargin();
                margin.ShadowStyle = value;
            }
            else if (_margin is { })
            {
                // Margin already exists, just update it
                _margin.ShadowStyle = value;
            }
        }
    }

    /// <summary>
    ///     The <see cref="Adornment"/> that offsets the <see cref="Viewport"/> from the <see cref="Margin"/>.
    ///     <para>
    ///         The Border provides the space for a visual border (drawn using
    ///         line-drawing glyphs) and the Title. The Border expands inward; in other words if `Border.Thickness.Top == 2`
    ///         the
    ///         border and title will take up the first row and the second row will be filled with spaces.
    ///     </para>
    ///     <para>
    ///         The Border provides the UI for mouse and keyboard arrangement of the View. See <see cref="Arrangement"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para><see cref="BorderStyle"/> provides a simple helper for turning a simple border frame on or off.</para>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of an adornment (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of <see cref="Frame"/> which will call <see cref="SetNeedsLayout"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="SubViews"/>.
    ///     </para>
    /// </remarks>
    private Border? _border;
    
    /// <inheritdoc cref="_border"/>
    public Border? Border
    {
        get
        {
            if (_border is null && this is not Adornment)
            {
                _border = new Border (this);
            }
            return _border;
        }
        private set => _border = value;
    }

    // TODO: Make BorderStyle nullable https://github.com/gui-cs/Terminal.Gui/issues/4021
    /// <summary>Gets or sets whether the view has a one row/col thick border.</summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper for manipulating the view's <see cref="Border"/>. Setting this property to any value other
    ///         than <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="Adornment.Thickness"/> to `1` and <see cref="BorderStyle"/> to the value.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="Adornment.Thickness"/> to `0` and <see cref="BorderStyle"/> to <see cref="LineStyle.None"/>.
    ///     </para>
    ///     <para>
    ///         Raises <see cref="OnBorderStyleChanged"/> and raises <see cref="BorderStyleChanged"/>, which allows change
    ///         to be cancelled.
    ///     </para>
    ///     <para>For more advanced customization of the view's border, manipulate see <see cref="Border"/> directly.</para>
    /// </remarks>
    public LineStyle BorderStyle
    {
        get => _border?.LineStyle ?? LineStyle.Single;
        set
        {
            SetBorderStyle (value);
            OnBorderStyleChanged ();
            BorderStyleChanged?.Invoke (this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Called when the <see cref="BorderStyle"/> has changed.
    /// </summary>
    protected virtual bool OnBorderStyleChanged () => false;

    /// <summary>
    ///     Fired when the <see cref="BorderStyle"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs>? BorderStyleChanged;

    /// <summary>
    ///     Sets the <see cref="BorderStyle"/> of the view to the specified value.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="BorderStyle"/> is a helper for manipulating the view's <see cref="Border"/>. Setting this property
    ///         to any value other
    ///         than <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="Adornment.Thickness"/> to `1` and <see cref="BorderStyle"/> to the value.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="Adornment.Thickness"/> to `0` and <see cref="BorderStyle"/> to <see cref="LineStyle.None"/>.
    ///     </para>
    ///     <para>For more advanced customization of the view's border, manipulate see <see cref="Border"/> directly.</para>
    /// </remarks>
    /// <param name="style"></param>
    internal void SetBorderStyle (LineStyle style)
    {
        Border border = EnsureBorder();
        
        if (style != LineStyle.None)
        {
            if (border.Thickness == Thickness.Empty)
            {
                border.Thickness = new (1);
            }
        }
        else
        {
            border.Thickness = new (0);
        }

        border.LineStyle = style;

        SetAdornmentFrames ();
        SetNeedsLayout ();
    }

    /// <summary>
    ///     The <see cref="Adornment"/> inside of the view that offsets the <see cref="Viewport"/>
    ///     from the <see cref="Border"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of an adornment (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of <see cref="Frame"/> which will call <see cref="SetNeedsLayout"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="SubViews"/>.
    ///     </para>
    /// </remarks>
    private Padding? _padding;
    
    /// <inheritdoc cref="_padding"/>
    public Padding? Padding
    {
        get
        {
            if (_padding is null && this is not Adornment)
            {
                _padding = new Padding (this);
            }
            return _padding;
        }
        private set => _padding = value;
    }

    /// <summary>
    ///     <para>Gets the thickness describing the sum of the Adornments' thicknesses.</para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Viewport"/> is offset from the <see cref="Frame"/> by the thickness returned by this method.
    ///     </para>
    /// </remarks>
    /// <returns>A thickness that describes the sum of the Adornments' thicknesses.</returns>
    public Thickness GetAdornmentsThickness ()
    {
        var result = Thickness.Empty;

        if (_margin is { })
        {
            result += _margin.Thickness;
        }

        if (_border is { })
        {
            result += _border.Thickness;
        }

        if (_padding is { })
        {
            result += _padding.Thickness;
        }

        return result;
    }

    /// <summary>Sets the Frame's of the Margin, Border, and Padding.</summary>
    internal void SetAdornmentFrames ()
    {
        if (this is Adornment)
        {
            // Adornments do not have Adornments
            return;
        }

        if (_margin is { })
        {
            _margin.Frame = Rectangle.Empty with { Size = Frame.Size };
        }

        if (_border is { } && _margin is { })
        {
            _border.Frame = _margin.Thickness.GetInside (_margin.Frame);
        }

        if (_padding is { } && _border is { })
        {
            _padding.Frame = _border.Thickness.GetInside (_border.Frame);
        }
    }
}
