#nullable enable

namespace Terminal.Gui.ViewBase;

public partial class View // Adornments
{
    /// <summary>
    ///     Initializes the Adornments of the View. Called by the constructor.
    /// </summary>
    private void SetupAdornments ()
    {
        // TODO: Move this to Adornment as a static factory method
        if (this is not Adornment)
        {
            // TODO: Make the Adornments Lazy and only create them when needed
            Margin = new (this);
            Border = new (this);
            Padding = new (this);
        }
    }

    private void BeginInitAdornments ()
    {
        Margin?.BeginInit ();
        Border?.BeginInit ();
        Padding?.BeginInit ();
    }

    private void EndInitAdornments ()
    {
        Margin?.EndInit ();
        Border?.EndInit ();
        Padding?.EndInit ();
    }

    private void DisposeAdornments ()
    {
        Margin?.Dispose ();
        Margin = null;
        Border?.Dispose ();
        Border = null;
        Padding?.Dispose ();
        Padding = null;
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
    public Margin? Margin { get; private set; }

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

            if (Margin is { })
            {
                Margin.ShadowStyle = value;
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
    public Border? Border { get; private set; }

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
        get => Border?.LineStyle ?? LineStyle.Single;
        set
        {
            if (Border is null)
            {
                return;
            }

            SetBorderStyle (value);
            OnBorderStyleChanged ();
            BorderStyleChanged?.Invoke (this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Called when the <see cref="BorderStyle"/> has changed.
    /// </summary>
    protected virtual bool OnBorderStyleChanged () { return false; }

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
        if (style != LineStyle.None)
        {
            if (Border!.Thickness == Thickness.Empty)
            {
                Border.Thickness = new (1);
            }
        }
        else
        {
            Border!.Thickness = new (0);
        }

        Border.LineStyle = style;

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
    public Padding? Padding { get; private set; }

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
        Thickness result = Thickness.Empty;

        if (Margin is { })
        {
            result += Margin.Thickness;
        }

        if (Border is { })
        {
            result += Border.Thickness;
        }

        if (Padding is { })
        {
            result += Padding.Thickness;
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

        if (Margin is { })
        {
            Margin!.Frame = Rectangle.Empty with { Size = Frame.Size };
        }

        if (Border is { } && Margin is { })
        {
            Border!.Frame = Margin!.Thickness.GetInside (Margin!.Frame);
        }

        if (Padding is { } && Border is { })
        {
            Padding!.Frame = Border!.Thickness.GetInside (Border!.Frame);
        }
    }
}
