namespace Terminal.Gui;
public partial class View
{
    private void CreateAdornments ()
    {
        Margin = CreateAdornment (typeof (Margin)) as Margin;
        Border = CreateAdornment (typeof (Border)) as Border;
        Padding = CreateAdornment (typeof (Padding)) as Padding;
    }

    // TODO: Move this to Adornment as a static factory method
    /// <summary>
    ///     This internal method is overridden by Adornment to do nothing to prevent recursion during View construction.
    ///     And, because Adornments don't have Adornments. It's internal to support unit tests.
    /// </summary>
    /// <param name="adornmentType"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    internal virtual Adornment CreateAdornment (Type adornmentType)
    {
        void ThicknessChangedHandler (object sender, EventArgs e)
        {
            if (IsInitialized)
            {
                LayoutAdornments ();
            }

            SetNeedsLayout ();
            SetNeedsDisplay ();
        }

        Adornment adornment;

        adornment = Activator.CreateInstance (adornmentType, this) as Adornment;
        adornment.ThicknessChanged += ThicknessChangedHandler;

        return adornment;
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
    ///     SuperView. The margin offsets the <see cref="Bounds"/> from the <see cref="Frame"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of an adornment (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of <see cref="Frame"/> and trigger <see cref="LayoutSubviews"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="Subviews"/>.
    ///     </para>
    /// </remarks>
    public Margin Margin { get; private set; }

    /// <summary>
    ///     The <see cref="Adornment"/> that offsets the <see cref="Bounds"/> from the <see cref="Margin"/>.
    ///     The Border provides the space for a visual border (drawn using
    ///     line-drawing glyphs) and the Title. The Border expands inward; in other words if `Border.Thickness.Top == 2` the
    ///     border and title will take up the first row and the second row will be filled with spaces.
    /// </summary>
    /// <remarks>
    ///     <para><see cref="BorderStyle"/> provides a simple helper for turning a simple border frame on or off.</para>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of a frame (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of the <see cref="Frame"/> and trigger <see cref="LayoutSubviews"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="Subviews"/>.
    ///     </para>
    /// </remarks>
    public Border Border { get; private set; }

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

            if (value != LineStyle.None)
            {
                Border.Thickness = new (1);
            }
            else
            {
                Border.Thickness = new (0);
            }

            Border.LineStyle = value;
            LayoutAdornments ();
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     The <see cref="Adornment"/> inside of the view that offsets the <see cref="Bounds"/>
    ///     from the <see cref="Border"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of a frame (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of the <see cref="Frame"/> and trigger <see cref="LayoutSubviews"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="Subviews"/>.
    ///     </para>
    /// </remarks>
    public Padding Padding { get; private set; }

    /// <summary>
    ///     <para>Gets the thickness describing the sum of the Adornments' thicknesses.</para>
    /// </summary>
    /// <returns>A thickness that describes the sum of the Adornments' thicknesses.</returns>
    public Thickness GetAdornmentsThickness () { return Margin.Thickness + Border.Thickness + Padding.Thickness; }

    /// <summary>Overriden by <see cref="Adornment"/> to do nothing, as the <see cref="Adornment"/> does not have adornments.</summary>
    internal virtual void LayoutAdornments ()
    {
        if (Margin is null)
        {
            return; // CreateAdornments () has not been called yet
        }

        if (Margin.Frame.Size != Frame.Size)
        {
            Margin._frame = Rectangle.Empty with { Size = Frame.Size };
            Margin.X = 0;
            Margin.Y = 0;
            Margin.Width = Frame.Size.Width;
            Margin.Height = Frame.Size.Height;
        }

        Margin.SetNeedsLayout ();
        Margin.SetNeedsDisplay ();

        if (IsInitialized)
        {
            Margin.LayoutSubviews ();
        }

        Rectangle border = Margin.Thickness.GetInside (Margin.Frame);

        if (border != Border.Frame)
        {
            Border._frame = border;
            Border.X = border.Location.X;
            Border.Y = border.Location.Y;
            Border.Width = border.Size.Width;
            Border.Height = border.Size.Height;
        }

        Border.SetNeedsLayout ();
        Border.SetNeedsDisplay ();

        if (IsInitialized)
        {
            Border.LayoutSubviews ();
        }

        Rectangle padding = Border.Thickness.GetInside (Border.Frame);

        if (padding != Padding.Frame)
        {
            Padding._frame = padding;
            Padding.X = padding.Location.X;
            Padding.Y = padding.Location.Y;
            Padding.Width = padding.Size.Width;
            Padding.Height = padding.Size.Height;
        }

        Padding.SetNeedsLayout ();
        Padding.SetNeedsDisplay ();

        if (IsInitialized)
        {
            Padding.LayoutSubviews ();
        }
    }
}
