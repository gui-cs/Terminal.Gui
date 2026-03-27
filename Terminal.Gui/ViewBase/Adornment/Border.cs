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
    public override Rectangle GetFrame () => Parent is { } ? Parent.Margin.Thickness.GetInside (Parent!.Margin.GetFrame ()) : Rectangle.Empty;

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
    ///     If <see langword="null"/> the length will be determined based on <see cref="View.Title"/>.
    ///     Only used when <see cref="BorderSettings.Tab"/> is set.
    /// </summary>
    public int? TabLength
    {
        get
        {
            if (field is { } || !Settings.HasFlag (BorderSettings.Tab))
            {
                return field;
            }

            int titleColumns = Settings.HasFlag (BorderSettings.Title) ? Parent?.TitleTextFormatter.FormatAndGetSize ().Width ?? 0 : 0;

            // Two vertical border lines + title text width (2 when no title)
            return titleColumns + 2;
        }
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
    public int TabEnd =>
        TabSide switch
        {
            Side.Top or Side.Bottom => GetFrame ().X + TabOffset + (TabLength ?? 0),
            Side.Left or Side.Right => GetFrame ().Y + TabOffset + (TabLength ?? 0),
            _ => 0
        };
}
