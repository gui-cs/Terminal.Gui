namespace Terminal.Gui.ViewBase;

public partial class View // Adornments
{
    /// <summary>
    ///     Initializes the Adornments of the View. Called by the constructor.
    /// </summary>
    private void SetupAdornments ()
    {
        if (this is not AdornmentView)
        {
            Margin.Parent = this;
            Border.Parent = this;
            Padding.Parent = this;
        }
    }

    private void BeginInitAdornments ()
    {
        Margin.View?.BeginInit ();
        Border.View?.BeginInit ();
        Padding.View?.BeginInit ();
    }

    private void EndInitAdornments ()
    {
        Margin.View?.EndInit ();
        Border.View?.EndInit ();
        Padding.View?.EndInit ();
    }

    private void DisposeAdornments ()
    {
        Margin.View?.Dispose ();
        Margin.View = null;
        Border.View?.Dispose ();
        Border.View = null;
        Padding.View?.Dispose ();
        Padding.View = null;
    }

    /// <summary>
    ///     The <see cref="IAdornment"/> that enables separation of a View from other SubViews of the same
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
    public Margin Margin { get; } = new ();

    /// <summary>
    ///     Gets or sets the shadow effect that will be drawn on the right and bottom sides of the
    ///     Margin.
    /// </summary>
    /// <remarks>
    ///     <see langword="null"/> will disable the shadow. All other values will add a shadow to the right and bottom sides of
    ///     the Margin.
    ///     The View 's <see cref="Frame"/> will be expanded to include the shadow.
    /// </remarks>
    public virtual ShadowStyles? ShadowStyle
    {
        get => Margin.ShadowStyle ?? null;
        set
        {
            SetShadowStyle (value);
            OnShadowStyleChanged ();
            ShadowStyleChanged?.Invoke (this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Called when the <see cref="ShadowStyle"/> has changed.
    /// </summary>
    protected virtual bool OnShadowStyleChanged () => false;

    /// <summary>
    ///     Fired when the <see cref="ShadowStyle"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs>? ShadowStyleChanged;

    /// <summary>
    ///     Sets the <see cref="ShadowStyle"/> of the view to the specified value.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ShadowStyle"/> is a helper for manipulating the view's <see cref="Margin"/>. Setting this property
    ///         to any value other
    ///         than <see cref="ShadowStyles.None"/> is equivalent to setting <see cref="Margin"/>'s
    ///         <see cref="IAdornment.Thickness"/> and <see cref="ShadowStyle"/> to the value.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="ShadowStyles.None"/> is equivalent to setting <see cref="Margin"/>'s
    ///         <see cref="IAdornment.Thickness"/> to `0` and <see cref="ShadowStyle"/> to <see cref="ShadowStyles.None"/>.
    ///     </para>
    ///     <para>For more advanced customization of the view's margin, manipulate see <see cref="Margin"/> directly.</para>
    /// </remarks>
    /// <param name="style"></param>
    internal void SetShadowStyle (ShadowStyles? style)
    {
        Margin.ShadowStyle = style;

        SetNeedsLayout ();
    }

    /// <summary>
    ///     The <see cref="IAdornment"/> that offsets the <see cref="Viewport"/> from the <see cref="Margin"/>.
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
    public Border Border { get; } = new ();

    /// <summary>Gets or sets whether the view has a one row/col thick border.</summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper for manipulating the view's <see cref="Border"/>. Setting this property to any value other
    ///         than <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="IAdornment.Thickness"/> to `1` and <see cref="BorderStyle"/> to the value.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="IAdornment.Thickness"/> to `0` and <see cref="BorderStyle"/> to <see cref="LineStyle.None"/>.
    ///     </para>
    ///     <para>
    ///         Raises <see cref="OnBorderStyleChanged"/> and raises <see cref="BorderStyleChanged"/>, which allows change
    ///         to be cancelled.
    ///     </para>
    ///     <para>For more advanced customization of the view's border, manipulate see <see cref="Border"/> directly.</para>
    /// </remarks>
    public LineStyle? BorderStyle
    {
        get => Border.LineStyle ?? null;
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
    ///         <see cref="IAdornment.Thickness"/> to `1` and <see cref="BorderStyle"/> to the value.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="IAdornment.Thickness"/> to `0` and <see cref="BorderStyle"/> to <see cref="LineStyle.None"/>.
    ///     </para>
    ///     <para>For more advanced customization of the view's border, manipulate see <see cref="Border"/> directly.</para>
    /// </remarks>
    /// <param name="style"></param>
    internal void SetBorderStyle (LineStyle? style)
    {
        if (style is null or LineStyle.None)
        {
            Border.Thickness = new Thickness (0);
        }
        else
        {
            if (Border.Thickness == Thickness.Empty)
            {
                Border.Thickness = new Thickness (1);
            }
        }

        Border.LineStyle = style;

        SetNeedsLayout ();
    }

    /// <summary>
    ///     The <see cref="IAdornment"/> inside of the view that offsets the <see cref="Viewport"/>
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
    public Padding Padding { get; } = new ();

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

        result += Margin.Thickness;
        result += Border.Thickness;
        result += Padding.Thickness;

        return result;
    }

    /// <summary>Sets the Frame's of the Margin, Border, and Padding.</summary>
    internal void SetAdornmentFrames ()
    {
        if (this is AdornmentView)
        {
            // AdornmentViews do not have Adornments
            return;
        }

        // Border and Padding dynamically update based on Margin's View's Frame changing
        Margin.View?.Frame = Frame with { Location = Point.Empty };
    }
}
