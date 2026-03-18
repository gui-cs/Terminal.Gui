namespace Terminal.Gui.ViewBase;

/// <summary>
///     The lightweight Border settings for a <see cref="View"/>. Accessed via <see cref="View.Border"/>.
///     Stores <see cref="Thickness"/>, <see cref="LineStyle"/>, and <see cref="Settings"/> without creating a full View
///     unless rendering, arrangement, or SubViews require it.
/// </summary>
/// <remarks>
///     <para>
///         Renders a border around the view with the <see cref="View.Title"/>. A border using <see cref="LineStyle"/>
///         will be drawn on the sides of <see cref="Drawing.Thickness"/> that are greater than zero.
///     </para>
///     <para>
///         The Border provides keyboard and mouse support for moving and resizing the View. See
///         <see cref="ViewArrangement"/>.
///     </para>
/// </remarks>
public class Border : AdornmentImpl
{
    /// <inheritdoc/>
    protected override AdornmentView CreateView ()
    {
        BorderView bv = new (this);

        return bv;
    }

    /// <inheritdoc />
    public override Rectangle GetFrame ()
    {
        if (Parent is { })
        {
            return Parent.Margin.Thickness.GetInside (Parent!.Margin.GetFrame ());
        }
        else
        {
            return Rectangle.Empty;
        }
    }

    /// <inheritdoc />
    protected override void OnThicknessChanged ()
    {
        base.OnThicknessChanged ();

        if (Thickness == Thickness.Empty)
        {
            return;
        }

        if (Parent?.SuperView?.BorderStyle is null)
        {
            return;
        }

        EnsureView ();
    }

    /// <summary>
    ///     Sets the style of the lines drawn in the <see cref="Border"/>. If not set, will inherit the style from
    ///     the <see cref="Border.Parent"/>'s <see cref="View.SuperView"/>'s <see cref="View.BorderStyle"/>. If set, will cause <see cref="IAdornment.View"/>
    ///     to be created.
    /// </summary>
    public LineStyle? LineStyle
    {
        get => field ?? Parent?.SuperView?.BorderStyle ?? null;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            if (field is not null)
            {
                EnsureView ();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the settings for the border.
    /// </summary>
    public BorderSettings Settings
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;

            Parent?.SetNeedsLayout ();
        }
    } = BorderSettings.Title;
}
