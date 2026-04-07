namespace Terminal.Gui.ViewBase;

/// <summary>
///     The lightweight Border settings for a <see cref="View"/>. Accessed via <see cref="View.Border"/>.
///     Stores <see cref="Thickness"/>, <see cref="LineStyle"/>, and <see cref="Settings"/> without creating a full View
///     unless rendering, arrangement, or SubViews require it.
/// </summary>
/// <remarks>
///     <para>
///         Border is one of three adornment layers (Margin → Border → Padding) that surround a View's content area.
///         It renders a border frame around the view using <see cref="LineStyle"/> line-drawing glyphs, and can display
///         the <see cref="View.Title"/> either inline on the border or in a tab header.
///     </para>
///     <para>
///         The rendering layer (<see cref="BorderView"/>) is created lazily via <see cref="AdornmentImpl.GetOrCreateView"/>
///         when <see cref="LineStyle"/>, <see cref="Thickness"/>, or <see cref="BorderSettings.Tab"/> is set.
///     </para>
///     <para>
///         The Border also provides keyboard and mouse support for moving and resizing the View via
///         <see cref="ViewArrangement"/>. See the
///         <see href="https://gui-cs.github.io/Terminal.Gui/docs/arrangement.html">Arrangement Deep Dive</see>.
///     </para>
///     <para>
///         <see cref="View.BorderStyle"/> is a convenience helper that sets <see cref="LineStyle"/> and
///         <see cref="Thickness"/> atomically; use <see cref="View.Border"/> directly for advanced configuration.
///     </para>
///     <para>
///         See <see href="https://gui-cs.github.io/Terminal.Gui/docs/borders.html"/> for the full deep dive.
///     </para>
///     <example>
///         Standard border with title (<c>BorderStyle = LineStyle.Single</c>, <c>Thickness.Top == 1</c>):
///         <code>
///         ┌┤Title├──┐
///         │         │
///         └─────────┘
///         </code>
///         Rounded border with thick top (<c>BorderStyle = LineStyle.Rounded</c>, <c>Thickness.Top == 3</c>):
///         <code>
///          ╭─────╮
///         ╭┤Title├──╮
///         │╰─────╯  │
///         │         │
///         ╰─────────╯
///         </code>
///         Tab-style border (<c>Settings = BorderSettings.Tab | BorderSettings.Title</c>,
///         <c>TabSide = Side.Top</c>, <c>Thickness = new (1, 3, 1, 1)</c>):
///         <code>
///         ╭───╮           ╭───╮
///         │Tab│           │Tab│
///         ├───┴───╮       │   ╰───╮
///         │content│       │content│
///         ╰───────╯       ╰───────╯
///         (unfocused)     (focused)
///         </code>
///     </example>
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
    ///     Gets or sets the style of the lines drawn in the <see cref="Border"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If not explicitly set, inherits from the <see cref="IAdornment.Parent"/>'s
    ///         <see cref="View.SuperView"/>'s <see cref="View.BorderStyle"/>. Returns <see langword="null"/>
    ///         if neither this Border nor any ancestor has a style set.
    ///     </para>
    ///     <para>
    ///         Setting this property to a non-null value causes the <see cref="BorderView"/> to be created
    ///         (via <see cref="AdornmentImpl.GetOrCreateView"/>).
    ///     </para>
    ///     <para>
    ///         Available styles: <see cref="Drawing.LineStyle.Single"/> (<c>┌─┐│└─┘</c>),
    ///         <see cref="Drawing.LineStyle.Double"/> (<c>╔═╗║╚═╝</c>),
    ///         <see cref="Drawing.LineStyle.Rounded"/> (<c>╭─╮│╰─╯</c>),
    ///         <see cref="Drawing.LineStyle.Heavy"/> (<c>┏━┓┃┗━┛</c>), and
    ///         dashed/dotted variants.
    ///     </para>
    /// </remarks>
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
    ///     Gets or sets the <see cref="BorderSettings"/> flags that control rendering behavior.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Defaults to <see cref="BorderSettings.Title"/>. Set to <see cref="BorderSettings.Tab"/> |
    ///         <see cref="BorderSettings.Title"/> to enable tab-style headers. Setting <see cref="BorderSettings.Tab"/>
    ///         causes the <see cref="BorderView"/> to be created and configures it for transparent rendering.
    ///     </para>
    ///     <para>
    ///         <see cref="BorderSettings.Gradient"/> enables gradient-filled borders using <see cref="GradientFill"/>.
    ///     </para>
    /// </remarks>
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
    ///     Gets or sets which side the tab header protrudes from. Defaults to <see cref="Side.Top"/>.
    ///     Only used when <see cref="Settings"/> includes <see cref="BorderSettings.Tab"/>.
    /// </summary>
    /// <remarks>
    ///     For <see cref="Side.Left"/> and <see cref="Side.Right"/>, the title text renders vertically.
    /// </remarks>
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
    ///     Gets or sets the offset along the border edge where the tab header starts (columns for
    ///     <see cref="Side.Top"/>/<see cref="Side.Bottom"/>, rows for <see cref="Side.Left"/>/<see cref="Side.Right"/>).
    ///     Only used when <see cref="Settings"/> includes <see cref="BorderSettings.Tab"/>.
    /// </summary>
    /// <remarks>
    ///     Can be positive (shifted right/down), zero (at the start), or negative (shifted left/up,
    ///     partially off-screen). The <see cref="TitleView"/> is clipped automatically by the View system's
    ///     natural viewport clipping.
    /// </remarks>
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
    ///     Gets or sets the total length of the tab header parallel to the border edge (including border cells).
    ///     If <see langword="null"/>, the length is auto-computed from the <see cref="View.Title"/> width plus the
    ///     <see cref="TitleView"/>'s border cells. Only used when <see cref="Settings"/> includes
    ///     <see cref="BorderSettings.Tab"/>.
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
    ///     Gets the effective tab length — either the explicit <see cref="TabLength"/> or
    ///     the <see cref="ITitleView.MeasuredTabLength"/> from the laid-out <see cref="TitleView"/>.
    ///     Returns 0 if no <see cref="TitleView"/> exists yet.
    /// </summary>
    internal int EffectiveTabLength
    {
        get
        {
            if (TabLength is { } explicitLength)
            {
                return explicitLength;
            }

            if (View is not BorderView { TitleView: ITitleView itv and View tv })
            {
                return 0;
            }

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
    }

    /// <summary>
    ///     Gets the column or row of the last cell of the tab header (columns for
    ///     <see cref="Side.Top"/>/<see cref="Side.Bottom"/>, rows for
    ///     <see cref="Side.Left"/>/<see cref="Side.Right"/>). Computed as
    ///     <see cref="TabOffset"/> + <see cref="EffectiveTabLength"/> relative to the border frame origin.
    /// </summary>
    public int TabEnd =>
        TabSide switch
        {
            Side.Top or Side.Bottom => GetFrame ().X + TabOffset + EffectiveTabLength,
            Side.Left or Side.Right => GetFrame ().Y + TabOffset + EffectiveTabLength,
            _ => 0
        };
}
