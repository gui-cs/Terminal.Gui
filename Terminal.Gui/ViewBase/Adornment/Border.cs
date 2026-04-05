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

    /// <inheritdoc/>
    public override Rectangle GetFrame () => Parent is { } ? Parent.Margin.Thickness.GetInside (Parent.Margin.GetFrame ()) : Rectangle.Empty;

    /// <inheritdoc/>
    protected override void OnThicknessChanged ()
    {
        base.OnThicknessChanged ();

        if (Thickness == Thickness.Empty)
        {
            return;
        }

        if (LineStyle is null)
        {
            return;
        }

        GetOrCreateView ();
    }

    /// <summary>
    ///     Sets the style of the lines drawn in the <see cref="Border"/>. If not set, will inherit the style from
    ///     the <see cref="IAdornment.Parent"/>'s <see cref="View.SuperView"/>'s <see cref="View.BorderStyle"/>. If set, will
    ///     cause <see cref="IAdornment.View"/>
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

            if (field is { })
            {
                GetOrCreateView ();
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

            if (field.HasFlag (BorderSettings.Tab))
            {
                GetOrCreateView ();
            }

            SettingsChanged?.Invoke (this, EventArgs.Empty);
            Parent?.SetNeedsLayout ();
        }
    } = BorderSettings.Title;

    /// <summary>Fired when <see cref="Settings"/> changes.</summary>
    public event EventHandler? SettingsChanged;

    /// <summary>
    ///     Gets or sets which side the Tab protrudes from. Only used when <see cref="BorderSettings.Tab"/> is set.
    /// </summary>
    public Side TabSide
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
    } = Side.Top;

    /// <summary>
    ///     Gets or sets the offset along the border edge where the Tab starts (columns for Top/Bottom,
    ///     rows for Left/Right). Only used when <see cref="BorderSettings.Tab"/> is set.
    /// </summary>
    public int TabOffset
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
    }

    /// <summary>
    ///     Gets or sets the total length of the tab parallel to the border edge (including border cells).
    ///     If <see langword="null"/> the length will be determined from the <see cref="TitleView"/>'s laid-out frame.
    ///     Only used when <see cref="BorderSettings.Tab"/> is set.
    /// </summary>
    public int? TabLength
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
    }

    /// <summary>
    ///     Gets offset along the border edge where the Tab ends (columns for Top/Bottom, rows for Left/Right).
    /// </summary>
    /// <summary>
    ///     Gets the effective tab length — either the explicit <see cref="TabLength"/> or
    ///     the <see cref="ITitleView.MeasuredTabLength"/> from the laid-out TitleView.
    /// </summary>
    internal int EffectiveTabLength
    {
        get
        {
            if (TabLength is { } explicitLength)
            {
                return explicitLength;
            }

            if (View is BorderView { TitleView: ITitleView itv and View tv })
            {
                if (itv.MeasuredTabLength > 0)
                {
                    return itv.MeasuredTabLength;
                }

                // TitleView hasn't been laid out yet — set text and orientation, then measure.
                tv.Text = Parent?.Title ?? string.Empty;
                itv.Orientation = TabSide is Side.Left or Side.Right ? Orientation.Vertical : Orientation.Horizontal;

                int measured = TabSide is Side.Top or Side.Bottom ? tv.GetAutoWidth () : tv.GetAutoHeight ();
                itv.MeasuredTabLength = measured;

                return measured;
            }

            return 0;
        }
    }

    /// <summary>>
    ///     Gets the column or row of the last cell of the tab.
    /// </summary>
    public int TabEnd =>
        TabSide switch
        {
            Side.Top or Side.Bottom => GetFrame ().X + TabOffset + EffectiveTabLength,
            Side.Left or Side.Right => GetFrame ().Y + TabOffset + EffectiveTabLength,
            _ => 0
        };
}
